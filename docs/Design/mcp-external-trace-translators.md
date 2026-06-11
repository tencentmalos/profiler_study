# Perfetto/Tracy 文件访问与 ProfilerStudy/MCP 共享方案

## 目标澄清

这份方案聚焦 **ProfilerStudy 直接打开 Perfetto/Tracy 抓取文件，并让 MCP 访问同一份源文件或同一个 trace artifact**。

这里的核心目标不是单独增加一个 MCP translator，而是让可视化 UI 和 MCP 共享同一套 trace 文件导入、artifact 管理和查询模型。这里的“共享”不表示 MCP 与可视化工具进程互动。

- 用户在 ProfilerStudy UI 中打开 `.perfetto-trace`、`.pftrace`、systrace、`.tracy` 文件后，能像打开 FramePro 文件一样看 timeline、thread tracks、slices、counters、frames/slow frames。
- MCP 能用同一套 importer/query model 读取用户指定的 trace 文件，或读取 ProfilerStudy 登记过的 trace artifact。
- live capture 产生的原始抓取、normalized cache、diagnostics 应进入 artifact store，让 UI 和 MCP 能引用同一个结果。
- MCP 不依赖 UI 是否正在运行，也不与 UI 进程通信。
- Perfetto/Tracy 支持先锁定为这次一体化工作的第一目标；FramePro 继续保留现有能力。

明确非目标：

- 不设计 MCP attach 当前 UI document。
- 不设计 MCP 读取 UI selection、visible range 或 active document。
- 不设计 UI 触发 MCP 分析或 MCP 控制 UI 跳转。

## 当前代码基础

当前已有三条相关链路：

1. **WinForms legacy UI**
   - `ProfilerStudy/LegacyWinForms/MainForm.cs` 的 `Read(string filename)` 只读取 FramePro session/recording。
   - UI 视图普遍直接持有 legacy `Session`。

2. **Avalonia UI**
   - `ProfilerStudy.Avalonia/Features/Sessions/SessionLoader.cs` 只通过 `Session.Read` 加载 FramePro 文件。
   - `SessionDocument` 已经包含 `Id`、`SourcePath`、`Session`、`Summary`、`FrameSamples`、`Selection`、`Viewport`。
   - `MainWindowViewModel` 已有 open/recent/live connection、shared `TimelineSelection`、shared `TimelineViewport`、多视图刷新逻辑。
   - 这条链路更适合扩展成 Perfetto/Tracy 统一可视化 document；selection/viewport 留在 UI 层，不进入 MCP contract。

3. **MCP server**
   - `ProfilerStudy.McpServer` 当前是独立 stdio server。
   - `ProfilerAnalysisService` 自己维护 `LoadedSession` 字典，直接持有 `Session`。
   - MCP 可以独立读取 FramePro 文件，但不知道 UI 当前打开了什么。本阶段接受这一点，优先统一“文件/Artifact 可访问性”和“查询模型”。

因此，一体化不应该只改 MCP，也不应该只让 UI shell out 到外部 Perfetto/Tracy UI。合理方向是抽出一个 **ProfilerStudy Trace Workspace**，让 UI 和 MCP 使用同一套 document/query/import 概念。

## 推荐总体架构

推荐采用 **共享 Trace Workspace + Trace Artifact Store + MCP direct file/artifact access**。

```text
        ┌─────────────────────────────┐
        │ ProfilerStudy.Avalonia UI    │
        │ - open trace files           │
        │ - visualize timeline/slices  │
        │ - create live artifacts      │
        └──────────────┬──────────────┘
                       │ in-process
                       v
┌────────────────────────────────────────────────────────────────┐
│ ProfilerStudy.TraceWorkspace                                    │
│ - TraceDocument registry                                        │
│ - TraceArtifactStore                                            │
│ - ITraceQuerySession                                            │
│ - FramePro / Perfetto / Tracy importers                         │
│ - shared diagnostics/query DTOs                                 │
└─────────────────────┬──────────────────────────────────────────┘
                      │ direct library use
                      v
        ┌───────────────────────────┐
        │ ProfilerStudy.McpServer    │
        │ - stdio MCP tools          │
        │ - load explicit file path  │
        │ - load registered artifact │
        └───────────────────────────┘
```

关键点：

- UI 是用户可视化操作的 primary surface。
- Trace Workspace 是数据和查询边界，不属于 UI 控件，也不属于 MCP server。
- Trace Artifact Store 是 UI live capture、normalized cache、import diagnostics 和 MCP artifact 访问的共享索引。
- MCP server 通过显式文件路径或 artifact id 加载 trace，不依赖 UI 当前状态，也不连接 UI 进程。
- Perfetto/Tracy importer 的输出是 `TraceDocument`/`ITraceQuerySession`，不是 FramePro packet，也不是只给 MCP 的一次性 JSON。

## 方案对比

| 方案 | 说明 | 优点 | 问题 | 结论 |
| --- | --- | --- | --- | --- |
| UI 和 MCP 各自实现 parser | UI 和 MCP 分别维护 Perfetto/Tracy 读取逻辑 | 初期看似最容易 | 双份解析逻辑会漂移；diagnostics/cache 不一致 | 不推荐 |
| 共享 Trace Workspace + Artifact Store | UI 和 MCP 共用 importer/query；MCP 按 path 或 artifact id 访问 | 文件/Artifact 一致，UI/headless 都可用，实现边界清晰 | 不共享 UI selection/viewport | 推荐 |

## Trace Workspace 数据模型

新增一个 UI/MCP 共用模型，不再让所有消费者直接依赖 `Session`：

```text
TraceDocument
  Id
  SourcePath
  ArtifactId?
  SourceFormat: study | tracy | perfetto | systrace
  DisplayName
  CreatedUtc
  ImportDiagnostics
  QuerySession: ITraceQuerySession
```

`ITraceQuerySession` 是核心查询接口：

```text
ITraceQuerySession
  GetSummary()
  GetFrames(range)
  GetThreads(filter)
  GetThreadSlices(time range, thread filter)
  GetScopeHotspots(time/frame range, top, filter)
  GetCounters(filter)
  GetCounterSamples(counter, time/frame range)
  GetScheduling(time range)
  GetFrameDetail(frame index)
  GetSliceDetail(slice id)
```

现有 FramePro 通过 `FrameProTraceQuerySession` adapter 继续工作。Perfetto 和 Tracy importer 产出 `PerfettoTraceQuerySession`、`TracyTraceQuerySession`。

UI 层仍然需要 view state，但它是 UI 私有状态，不属于 MCP contract：

```text
TraceViewState
  DocumentId
  Selection: TraceSelection
  Viewport: TraceViewport

TraceSelection
  SelectedFrameIndex?
  SelectedSliceId?
  SelectedThreadId?
  SelectedCounterName?
  SelectedTimeStartNs?
  SelectedTimeEndNs?

TraceViewport
  StartTimeNs
  EndTimeNs
  StartFrameIndex?
  EndFrameIndex?
```

原因：FramePro 很强 frame-centric；Perfetto 和 Tracy 更接近 time/slice-centric。不能要求所有 trace 都有可靠 frame index。UI 必须能表达“当前可视时间范围”和“当前选中的 slice/thread/counter”，但 MCP 只通过显式参数、path、artifact id 查询。

## UI 支持设计

### 1. 直接打开 Perfetto/Tracy 文件

Avalonia 的 `OpenSessionAsync` 应扩展为 `OpenTraceAsync`：

- FramePro: `.profiler`、`.profiler_recording`、`.profiler_dump`
- Perfetto: `.perfetto-trace`、`.pftrace`
- Systrace: `.html`、`.systrace`、`.txt`，仅在 importer 能识别时接受
- Tracy: `.tracy`

打开后统一生成 `TraceDocument`，加入 document registry 和 recent files。

WinForms legacy 可以暂时只保留 FramePro 支持。等 Trace Workspace 稳定后，如果仍需要 WinForms 打开 Perfetto/Tracy，可以让 WinForms 也通过 workspace adapter 接入；但第一优先级应放在 Avalonia，因为它已经有跨平台 UI 和 document/selection/viewport 雏形。

### 2. 统一可视化视图

第一阶段不要追求复刻 Perfetto UI 或 Tracy UI 的全部能力。ProfilerStudy 只需要覆盖当前性能分析闭环：

- Summary：source format、duration、process/thread/frame/counter 数量、import warnings。
- Timeline：time ruler、frame bands（如果有）、thread lanes、slice blocks、selection overlay。
- Threads：thread list、visible lanes、slice count、total active time。
- Slices/Scopes：按 name 聚合热点，支持范围过滤。
- Counters：Perfetto counters、Tracy plots、FramePro custom stats 统一展示。
- Frame Detail：如果 trace 有 frame，显示 selected frame 内的线程层级；如果没有 frame，则显示 selected time range 内热点。
- Diagnostics：importer 版本、缺失 table、unsupported Tracy event、frame synthesis 规则。

这能让用户在同一个 ProfilerStudy 里查看 Perfetto/Tracy/FramePro 数据，同时 MCP 也能读取同一类抓取文件或 artifact；两者不需要运行时互动。

## MCP 支持设计

MCP server 增加文件和 artifact 访问能力，不与可视化 UI 进程通信。

### 文件读取

UI 是否运行无关：

- `load_trace_file(path, format=auto, top=10)`
- `load_trace_artifact(artifact_id, top=10)`
- `list_trace_artifacts(format?, since?, limit?)`
- `list_sessions`
- `get_session_summary`
- `find_slow_frames`
- `find_scope_hotspots`
- `analyze_time_range`
- `analyze_frame_detail`
- `list_counters`
- `query_counter`

这些工具走 `TraceWorkspace` 直接加载文件或 artifact。现有 `load_session_file` 可以保留为 FramePro-specific alias，保证兼容。

## Perfetto 支持方案

Perfetto 文件打开和 MCP 查询都应通过同一个 importer：

```text
PerfettoTraceImporter
  -> TraceProcessorRunner
  -> fixed SQL extraction
  -> PerfettoTraceQuerySession
  -> TraceDocument
```

推荐复用 Perfetto Trace Processor，而不是在 C# 中手写 protobuf/systrace parser。Trace Processor 已经提供格式识别、timestamp sorting、SQL storage 和多格式读取。

第一版提取：

- `slice`：线程 slice、ATRace section、应用自定义 trace section。
- `thread` / `process`：线程和进程名称。
- `counter` / `counter_track`：counter 曲线。
- `sched_slice`：调度状态，用于后续 blocking/stall 分析。
- `actual_frame_timeline_slice`：存在时作为 frame source。
- fallback frame heuristic：`Choreographer#doFrame`、`doFrame`、`DrawFrame`、用户配置的 frame slice name。

Perfetto trace 可视化要承认两个事实：

- 有些 trace 没有明确 frame，只能做 time range 分析。
- systrace 是 legacy input，很多数据只有 textual ftrace/ATRace；frame synthesis 必须在 diagnostics 里标注。

## Tracy 支持方案

Tracy 支持分成文件打开和 live socket capture。

Tracy 的细化设计见 `docs/Design/mcp-tracy-compatibility.md`。该专项设计要求 Tracy runtime 使用完整 C# 实现，第一版锁定 Azahar 当前 Tracy `0.10.0`；`tools/profiler_tracy_bridge` submodule 只保留 Tracy/viewer 原始源码和协议对照资料，不作为产品 bridge 或 native 构建依赖。协议名称统一为 `study`、`tracy`、`perfetto`，其中默认 `study` 对应当前 ProfilerStudy 原始实现，默认构建和默认 `study` 协议路径不依赖 Tracy。

### `.tracy` 文件打开

推荐通过 ProfilerStudyCore 内置的纯 C# Tracy 0.10.0 reader 读取 `.tracy`，输出 workspace 可消费的 normalized document：

```text
TracyTraceImporter.Load(capture.tracy)
  -> Tracy010FileReader
  -> TracyTraceQuerySession
  -> TraceArtifactStore register normalized cache + diagnostics
```

第一版导入：

- frames（如果 trace 中存在）
- CPU zones -> slices
- thread names
- plots -> counters
- capture metadata
- unsupported event warnings

### Tracy live socket

UI live capture 和 MCP headless capture 都应调用同一个 Core C# capture path：

```text
pc://127.0.0.1:8086 + protocol=tracy
  -> Tracy010LiveCaptureClient.Capture(host, port, seconds)
  -> TracyTraceQuerySession
  -> TraceArtifactStore register normalized cache + diagnostics
  -> UI open TraceDocument
  -> MCP load artifact_id or source path
```

从用户角度看，ProfilerStudy 是直接连接 Tracy target 并打开 capture。实现上锁定 Azahar 当前 Tracy `0.10.0`，由 C# reader/capture 实现 handshake、metadata 和已支持事件解析。

Tracy upstream 当前 protocol 包含 `TracyPrf` handshake、`ProtocolVersion`、LZ4 stream 和 server-query metadata round trip。`tools/profiler_tracy_bridge` 只保留 upstream source/viewer 参考，不作为产品 bridge、helper process 或 native build 依赖。

## Trace Artifact Store

为了让 UI 和 MCP 访问“相关抓取文件”，需要一个明确的 artifact store：

```text
~/.profilerstudy/traces/
  captures/
    2026-06-09-143000-tracy/
      source.tracy
      normalized.json
      import-diagnostics.json
      manifest.json
    2026-06-09-144500-perfetto/
      source.perfetto-trace
      normalized-cache.sqlite/json
      import-diagnostics.json
      manifest.json
```

作用：

- UI live capture 后把原始 capture 和 normalized cache 注册到 store。
- MCP 可以 `list_trace_artifacts`，找到最近抓取文件。
- Recent files 不只记录原始 path，也能记录 artifact id。
- Import diagnostics 和 dependency versions 可追溯。

注意：artifact store 不替代用户显式打开文件。MCP 不应扫描任意目录，只能列出 ProfilerStudy 自己登记过的 artifacts。

## 实施阶段

### Phase 1：Trace Workspace 抽象

- 定义 `TraceDocument`、`TraceArtifactManifest`、`ITraceQuerySession`。
- 用 `FrameProTraceQuerySession` 包住现有 `Session`。
- 将 Avalonia `SessionDocument` 迁移为或包裹为 `TraceDocument`；selection/viewport 作为 UI view state 保留。
- MCP `LoadedSession` 改为持有 `ITraceQuerySession`，FramePro 行为保持兼容。

验收：

- FramePro 文件仍能在 UI 和 MCP 中按原行为打开/分析。
- MCP 分析工具不再直接依赖 `Session`。

### Phase 2：Artifact Store 和 MCP 文件/Artifact 访问

- 增加 `TraceArtifactStore` manifest/index。
- UI live capture 后登记原始文件、normalized cache 和 diagnostics。
- MCP 新增 `load_trace_file`、`load_trace_artifact`、`list_trace_artifacts`、`get_import_diagnostics`。
- MCP 只访问显式 path 或 ProfilerStudy artifact store，不扫描用户目录。

验收：

- MCP 能通过 path 或 artifact id 加载同一类 trace 数据。
- UI 不运行时 MCP 文件/Artifact 查询仍可用。

### Phase 3：Perfetto/Systrace 直接打开

- 增加 `PerfettoTraceImporter` 和 `TraceProcessorRunner`。
- Avalonia open dialog 支持 `.perfetto-trace`、`.pftrace`、`.systrace`、`.html`、`.txt`。
- UI 展示 timeline/thread lanes/slices/counters。
- MCP 可以通过 path 或 artifact id 加载同一文件。

验收：

- 同一个 Perfetto 文件可被 UI 打开，也可被 MCP 分析。
- Perfetto import diagnostics 在 UI 和 MCP 中一致。

### Phase 4：Tracy 文件和 live socket

- 增加 ProfilerStudyCore 内置纯 C# Tracy 0.10.0 reader/capture。
- UI 支持打开 `.tracy`。
- UI 支持 `protocol=tracy` 的 host/port/duration capture，capture 后打开 document。
- MCP 支持 headless capture，并能通过 artifact id 或 path 读取同一 Tracy capture。

验收：

- Tracy target capture 后，UI 能看到 capture，MCP 能分析同一个 artifact。
- `.tracy` 文件可直接打开并进入 recent/artifact store。

### Phase 5：更深入的外部诊断

- cross-format compare：FramePro vs Perfetto vs Tracy。
- richer diagnostics：Perfetto scheduling/blocking、Tracy lock/GPU/allocation/callstack。

## 安全和权限

- 不允许模型传任意 SQL；Perfetto 查询由代码内固定 SQL 实现。
- 不允许 MCP 扫描用户目录；只访问用户显式传入 path 或 artifact store manifest。
- Tracy C# live capture 必须有 timeout、output size cap 和 cancellation。
- diagnostics 可以返回 dependency versions、import warnings，但不要返回无关环境变量或目录列表。

## 资料依据

- Perfetto Trace Processor architecture：`https://perfetto.dev/docs/design-docs/trace-processor-architecture`
- Perfetto other trace formats / systrace import：`https://perfetto.dev/docs/getting-started/other-formats`
- Perfetto Trace Processor usage：`https://perfetto.dev/docs/analysis/trace-processor`
- Tracy upstream repository：`https://github.com/wolfpld/tracy`
- Tracy protocol/worker/capture 参考文件：`public/common/TracyProtocol.hpp`、`server/TracyWorker.cpp`、`capture/src/capture.cpp`

## 推荐结论

优先做 **Trace Workspace + Artifact Store + MCP direct file/artifact access**，再接 Perfetto，最后接 Tracy。MCP 与可视化 UI 进程的互动不进入当前方案。

具体顺序：

1. 先把 FramePro 也包进 `ITraceQuerySession`，建立 UI/MCP 共享 document 语义。
2. 做 Artifact Store 和 MCP direct file/artifact access，让 UI 产物和 MCP 查询能引用同一批抓取文件。
3. 接 Perfetto/systrace，因为 Trace Processor 能提供稳定解析能力，适合先验证统一文件/Artifact 工作流。
4. 接 Tracy 时使用内置 C# 0.10.0 reader/capture，`tools/profiler_tracy_bridge` 仅保存官方源码和 viewer 参考。

这样 ProfilerStudy 会成为可视化主界面，MCP 成为同一批 trace 文件和 artifact 上的分析入口，而不是另一个独立 trace 读取器。
