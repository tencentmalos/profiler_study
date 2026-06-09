# Perfetto/Tracy 可视化与 MCP 一体化方案

## 目标澄清

这份方案聚焦 **ProfilerStudy 直接打开 Perfetto/Tracy 抓取文件，并让 MCP 访问同一份文件、同一个打开中的 document、同一个可视化选区**。

这里的核心目标不是单独增加一个 MCP translator，而是让可视化操作和 MCP 分析尽量一体化：

- 用户在 ProfilerStudy UI 中打开 `.perfetto-trace`、`.pftrace`、systrace、`.tracy` 文件后，能像打开 FramePro 文件一样看 timeline、thread tracks、slices、counters、frames/slow frames。
- MCP 能看到 UI 当前打开了哪些 trace、当前 active document、当前 visible range、selected frame/slice/thread/counter。
- MCP 能基于 UI 的当前选区做分析，而不是让用户重复输入文件路径和 frame range。
- MCP 在 UI 未运行时仍能直接读取指定抓取文件，作为 headless fallback。
- Perfetto/Tracy 支持先锁定为这次一体化工作的第一目标；FramePro 继续保留现有能力。

## 当前代码基础

当前已有三条相关链路：

1. **WinForms legacy UI**
   - `ProfilerStudy/FramePro/MainForm.cs` 的 `Read(string filename)` 只读取 FramePro session/recording。
   - UI 视图普遍直接持有 `FramePro.Session`。

2. **Avalonia UI**
   - `ProfilerStudy.Avalonia/Sessions/SessionLoader.cs` 只通过 `FramePro.Session.Read` 加载 FramePro 文件。
   - `SessionDocument` 已经包含 `Id`、`SourcePath`、`Session`、`Summary`、`FrameSamples`、`Selection`、`Viewport`。
   - `MainWindowViewModel` 已有 open/recent/live connection、shared `TimelineSelection`、shared `TimelineViewport`、多视图刷新逻辑。
   - 这条链路更适合扩展成 Perfetto/Tracy 统一可视化 document。

3. **MCP server**
   - `ProfilerStudy.McpServer` 当前是独立 stdio server。
   - `ProfilerAnalysisService` 自己维护 `LoadedSession` 字典，直接持有 `FramePro.Session`。
   - MCP 可以独立读取 FramePro 文件，但不知道 UI 当前打开了什么，也不知道 UI selection/viewport。

因此，一体化不应该只改 MCP，也不应该只让 UI shell out 到外部 Perfetto/Tracy UI。合理方向是抽出一个 **ProfilerStudy Trace Workspace**，让 UI 和 MCP 使用同一套 document/query/import 概念。

## 推荐总体架构

推荐采用 **共享 Trace Workspace + 可选 UI Bridge + MCP headless fallback**。

```text
                     ┌─────────────────────────────┐
                     │ ProfilerStudy.Avalonia UI    │
                     │ - open files                 │
                     │ - timeline/threads/counters  │
                     │ - selection/viewport         │
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
└─────────────────────┬───────────────────────────┬──────────────┘
                      │                           │
                      │ local bridge              │ direct library use
                      v                           v
        ┌────────────────────────┐     ┌───────────────────────────┐
        │ UI Bridge              │     │ ProfilerStudy.McpServer    │
        │ - list open docs       │     │ - stdio MCP tools          │
        │ - current selection    │     │ - attach UI doc if present │
        │ - reveal/select range  │     │ - load file if headless    │
        └────────────────────────┘     └───────────────────────────┘
```

关键点：

- UI 是用户可视化操作的 primary surface。
- Trace Workspace 是数据和查询边界，不属于 UI 控件，也不属于 MCP server。
- UI Bridge 只在 UI 运行时提供 “MCP 附着到当前 UI 状态” 的能力。
- MCP server 仍然可以独立运行；UI 不运行时，它直接用 Trace Workspace 加载文件。
- Perfetto/Tracy importer 的输出是 `TraceDocument`/`ITraceQuerySession`，不是 FramePro packet，也不是只给 MCP 的一次性 JSON。

## 方案对比

| 方案 | 说明 | 优点 | 问题 | 结论 |
| --- | --- | --- | --- | --- |
| UI 和 MCP 各自读取文件 | UI 打开一份，MCP 再用 path 读一份 | 最容易实现 | selection/viewport 不共享；用户需要重复描述上下文 | 只能作为 fallback |
| MCP server 做唯一 backend，UI 也连 MCP | UI 所有查询走 MCP server | 状态天然统一 | UI 强依赖 MCP 生命周期；调试和发布复杂度上升 | 后续可评估，不适合第一阶段 |
| 共享 Trace Workspace + UI Bridge | UI 和 MCP 共用核心库；UI 运行时 MCP 可附着 UI 状态 | 一体化程度高，UI/headless 都可用 | 需要定义 document/bridge/query 边界 | 推荐 |

## Trace Workspace 数据模型

新增一个 UI/MCP 共用模型，不再让所有消费者直接依赖 `FramePro.Session`：

```text
TraceDocument
  Id
  SourcePath
  SourceFormat: FramePro | Perfetto | Systrace | Tracy
  DisplayName
  CreatedUtc
  ImportDiagnostics
  QuerySession: ITraceQuerySession
  Selection: TraceSelection
  Viewport: TraceViewport
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

`TraceSelection` 和 `TraceViewport` 应同时支持 frame-based 和 time-based trace：

```text
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

原因：FramePro 很强 frame-centric；Perfetto 和 Tracy 更接近 time/slice-centric。不能要求所有 trace 都有可靠 frame index。UI 和 MCP 都要能表达“当前可视时间范围”和“当前选中的 slice/thread/counter”。

## UI 一体化设计

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

这能让用户在同一个 ProfilerStudy 里“看”和“问 MCP”，而不是在 Perfetto UI、Tracy UI、ProfilerStudy 三个工具之间切换上下文。

### 3. UI 触发 MCP 分析

后续 UI 可以增加轻量入口：

- “Ask MCP about selected frame/range”
- “Analyze visible range”
- “Explain selected slice”
- “Compare selected range with previous range”

第一阶段不用在 UI 内嵌 chat。只需要确保 UI Bridge 暴露当前 document/selection，Codex 侧 MCP tool 能读到即可。

## MCP 一体化设计

MCP server 增加两类能力：headless 文件读取和 UI 附着。

### Headless 文件读取

UI 未运行时：

- `load_trace_file(path, format=auto, top=10)`
- `list_sessions`
- `get_session_summary`
- `find_slow_frames`
- `find_scope_hotspots`
- `analyze_time_range`
- `analyze_frame_detail`
- `list_counters`
- `query_counter`

这些工具走 `TraceWorkspace` 直接加载文件。

### UI 附着工具

UI 运行时：

| Tool | 用途 |
| --- | --- |
| `list_ui_documents` | 列出 ProfilerStudy UI 当前打开的 trace documents |
| `get_active_ui_document` | 返回 active document id、source path、format、summary、visible range、selection |
| `attach_ui_document` | 将 MCP session 绑定到 UI document，后续分析工具可用这个 session id |
| `analyze_ui_selection` | 分析 UI 当前 selected frame/slice/time range |
| `analyze_ui_visible_range` | 分析 UI 当前 viewport |
| `reveal_frame_in_ui` | 可选：让 UI 跳到某个 frame |
| `reveal_time_range_in_ui` | 可选：让 UI 跳到某个 time range |

默认工具应以 read-only 为主。`reveal_*` 这种会改变 UI 状态的操作需要明确权限或用户确认。

### UI Bridge 协议

UI Bridge 可以是本地 JSON-RPC：

- Windows：named pipe。
- macOS/Linux：Unix domain socket。
- fallback：loopback TCP，仅监听 `127.0.0.1`。

Bridge 启动在 UI 进程内，由 UI 持有当前 document registry。MCP server 作为 client 连接 bridge。

Bridge 最小 API：

```text
workspace/listDocuments
workspace/getActiveDocument
workspace/getDocumentSummary
workspace/getViewport
workspace/getSelection
workspace/queryRangeSummary
workspace/queryFrameDetail
workspace/querySliceDetail
workspace/revealRange
workspace/revealFrame
```

不要通过 bridge 暴露任意文件系统访问，也不要暴露任意 SQL。

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

### `.tracy` 文件打开

推荐通过 Tracy bridge helper 读取 `.tracy`，输出 workspace 可消费的 normalized document：

```text
profiler-tracy-bridge import --input capture.tracy --output normalized.json
```

第一版导入：

- frames（如果 trace 中存在）
- CPU zones -> slices
- thread names
- plots -> counters
- capture metadata
- unsupported event warnings

### Tracy live socket

MCP 和 UI 都应使用同一 bridge：

```text
tracy://127.0.0.1:8086
  -> profiler-tracy-bridge capture --host 127.0.0.1 --port 8086 --seconds 30 --output capture.tracy --normalized normalized.json
  -> TraceArtifactStore register original + normalized
  -> UI open TraceDocument
  -> MCP attach same document
```

从用户角度看，ProfilerStudy 是直接连接 Tracy target 并打开 capture。实现上复用 Tracy 官方 worker/capture 逻辑，避免手写私有 wire protocol。

Tracy upstream 当前 protocol 包含 `TracyPrf` handshake、`ProtocolVersion`、LZ4 stream 和 server-query metadata round trip。手写 C# client 可行但维护风险高，不建议第一阶段做。

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

- 定义 `TraceDocument`、`TraceSelection`、`TraceViewport`、`ITraceQuerySession`。
- 用 `FrameProTraceQuerySession` 包住现有 `FramePro.Session`。
- 将 Avalonia `SessionDocument` 迁移为或包裹为 `TraceDocument`。
- MCP `LoadedSession` 改为持有 `ITraceQuerySession`，FramePro 行为保持兼容。

验收：

- FramePro 文件仍能在 UI 和 MCP 中按原行为打开/分析。
- UI selection/viewport 能表示 frame range 和 time range。

### Phase 2：UI Bridge 和 MCP attach

- UI 进程启动 local bridge。
- MCP server 自动探测 bridge。
- 新增 `list_ui_documents`、`get_active_ui_document`、`attach_ui_document`、`analyze_ui_selection`。
- UI 未运行时工具返回明确 fallback 提示，不失败。

验收：

- UI 打开一个 FramePro 文件后，MCP 能列出它并分析当前选区。
- MCP 仍能独立 `load_trace_file(path)`。

### Phase 3：Perfetto/Systrace 直接打开

- 增加 `PerfettoTraceImporter` 和 `TraceProcessorRunner`。
- Avalonia open dialog 支持 `.perfetto-trace`、`.pftrace`、`.systrace`、`.html`、`.txt`。
- UI 展示 timeline/thread lanes/slices/counters。
- MCP 可以 attach UI document 或 headless load 同一文件。

验收：

- 同一个 Perfetto 文件可被 UI 打开，也可被 MCP 分析。
- UI 当前 visible range 可被 MCP `analyze_ui_visible_range` 使用。

### Phase 4：Tracy 文件和 live socket

- 增加 `profiler-tracy-bridge` 或文档化外部 bridge。
- UI 支持打开 `.tracy`。
- UI 支持 `tracy://host:port` duration capture，capture 后打开 document。
- MCP 支持 headless capture 和 attach UI Tracy document。

验收：

- Tracy target capture 后，UI 能看到 capture，MCP 能分析同一个 artifact。
- `.tracy` 文件可直接打开并进入 recent/artifact store。

### Phase 5：双向协作增强

- MCP `reveal_frame_in_ui`、`reveal_time_range_in_ui`。
- UI “Ask MCP about selected range” 入口。
- cross-format compare：FramePro vs Perfetto vs Tracy。
- richer diagnostics：Perfetto scheduling/blocking、Tracy lock/GPU/allocation/callstack。

## 安全和权限

- Bridge 只监听本机，并使用 per-run token 或随机 pipe/socket 名。
- MCP 默认 read-only 访问 UI document 状态。
- 改变 UI 状态的工具需要明确用户确认或配置允许。
- 不允许模型传任意 SQL；Perfetto 查询由代码内固定 SQL 实现。
- 不允许 MCP 扫描用户目录；只访问用户显式传入 path 或 artifact store manifest。
- bridge/helper process 必须有 timeout、output size cap 和 cancellation。
- diagnostics 可以返回 dependency versions、import warnings，但不要返回无关环境变量或目录列表。

## 资料依据

- Perfetto Trace Processor architecture：`https://perfetto.dev/docs/design-docs/trace-processor-architecture`
- Perfetto other trace formats / systrace import：`https://perfetto.dev/docs/getting-started/other-formats`
- Perfetto Trace Processor usage：`https://perfetto.dev/docs/analysis/trace-processor`
- Tracy upstream repository：`https://github.com/wolfpld/tracy`
- Tracy protocol/worker/capture 参考文件：`public/common/TracyProtocol.hpp`、`server/TracyWorker.cpp`、`capture/src/capture.cpp`

## 推荐结论

优先做 **Trace Workspace + UI Bridge**，再接 Perfetto，最后接 Tracy。

具体顺序：

1. 先把 FramePro 也包进 `ITraceQuerySession`，建立 UI/MCP 共享 document 语义。
2. 做 UI Bridge，让 MCP 能看到 ProfilerStudy 当前打开的文件和选区。
3. 接 Perfetto/systrace，因为 Trace Processor 能提供稳定解析能力，适合先验证统一 UI/MCP 工作流。
4. 接 Tracy 时复用官方 C++ worker/capture 代码做 bridge，避免维护私有 wire protocol。

这样 ProfilerStudy 会成为可视化主界面，MCP 成为同一 workspace 上的分析协作者，而不是另一个独立 trace 读取器。
