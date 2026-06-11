# Tracy 兼容接入设计

## 目标

这份设计聚焦 ProfilerStudy 对 Tracy 的优先接入，覆盖两个核心能力：

- MCP 和 UI 可以按协议选择连接 profiler target，默认仍使用现有 `profiler_study` 协议，显式选择 `tracy` 时直连 Tracy 端口。
- MCP 和 UI 可以打开、加载并分析 `.tracy` 文件，且 live capture 产物和文件导入产物都能通过 Trace Workspace / Artifact Store 被复用。

Tracy 支持作为可选能力接入。未初始化 Tracy submodule、未构建 bridge、或运行环境缺少 bridge 可执行文件时，现有 ProfilerStudy 文件读取、TCP 捕获、Android adb forward 捕获和 MCP self-test 都不能受影响。

## 非目标

- 不在 C# 中重写 Tracy socket protocol 或 `.tracy` 文件格式解析。
- 不把 Tracy 原始事件完整映射成 ProfilerStudy legacy `Session`。
- 不要求 WinForms 第一阶段直接打开 `.tracy`。
- 不让 MCP 依赖 Avalonia UI 是否正在运行，也不读取 UI selection、viewport 或 active document。
- 不在第一阶段实现 Tracy GPU、locks、allocations、callstacks、messages 的完整查询；这些事件先进入 diagnostics 或后续阶段。

## 现有基础和约束

当前 MCP 主要围绕 legacy `Session`：

- `capture_profile` 解析 `pc://host:port` 或 `android://...` 后调用 `Session.ConnectToTcp` / `Session.ConnectToAndroid`。
- `analyze_session_file` 和 `load_session_file` 只读取 `.profiler`、`.profiler_recording`、`.profiler_dump`。
- `ProfilerCaptureTarget` 只表达 Android forward 和普通 TCP，不表达 trace protocol。

Azahar 参考路径：

```text
/Users/bytedance/workspace/azahar/foundation/basic/modules/implements/profiler/private/spatial/profiler/tracy
```

该目录下的 SDK 是 target-side Tracy client 集成，当前版本来自 `tracy/common/TracyVersion.hpp`，为 `0.10.0`。它可以作为 target 端编译开关、协议版本和运行行为参考，但不包含 ProfilerStudy 侧所需的 server/capture/file reader 能力。因此 ProfilerStudy 侧需要独立 bridge，复用同版本 Tracy upstream 的 server/capture/savefile 逻辑。

## 方案选择

推荐采用 **独立 profiler_tracy_bridge submodule + 主仓库可选消费**。

```text
ProfilerStudy / MCP / Avalonia
  -> TracyCaptureService / TracyTraceImporter
  -> tools/profiler_tracy_bridge/profiler-tracy-bridge
       capture
       import
       info
  -> normalized trace files
  -> TracyTraceQuerySession
```

不推荐 C# 直写 Tracy client 或 `.tracy` parser。Tracy live protocol 包含 handshake、protocol version、LZ4 stream、server query 和保存格式细节，随 Tracy 版本变化有维护成本。Bridge 应复用 Tracy upstream C++ 侧已有 worker/capture/file 代码，把 ProfilerStudy 主仓库和 Tracy 内部实现隔离开。

## Submodule 布局

新增 submodule：

```text
tools/profiler_tracy_bridge
  remote: git@github.com:tencentmalos/profiler_tracy_bridge.git
```

主仓库职责：

- 在 `.gitmodules` 中登记 submodule。
- 在设计文档、构建文档和 MCP diagnostics 中说明 Tracy 是 optional feature。
- C# 侧只依赖 bridge CLI contract 和 normalized schema。
- 不在主仓库复制 Tracy upstream C++ 源码。

Bridge 仓库职责：

- 持有 `profiler-tracy-bridge` C++ CLI。
- 持有或引用 Tracy upstream `0.10.0` 相关 server/capture/file reader 源码。
- 维护跨平台 CMake 构建、版本输出和最小自测。
- 产出稳定 CLI 和 normalized 文件格式。

Submodule 更新规则：

- 修改 bridge 实现时先提交到 `profiler_tracy_bridge` 仓库。
- 主仓库只更新 submodule revision 和消费侧代码。
- 主仓库 PR 必须说明 bridge revision、Tracy upstream version、CLI contract 是否变化。

## 编译开关和可选能力

主仓库构建必须分为默认路径和 Tracy-enabled 路径。

默认路径：

```powershell
dotnet build ProfilerForStudy.sln -c Debug
```

要求：

- 不要求初始化 `tools/profiler_tracy_bridge`。
- 不构建 C++ bridge。
- MCP 和 Avalonia 编译通过。
- MCP 始终暴露通用 trace tools 和 `get_tracy_bridge_status`，但执行 `protocol=tracy` 或 `.tracy` import 时如果 bridge 不可用，必须返回 `BridgeUnavailable` diagnostics。

Tracy-enabled 路径：

```powershell
git submodule update --init tools/profiler_tracy_bridge
dotnet build ProfilerForStudy.sln -c Debug -p:EnableTracyBridge=true
```

建议 MSBuild 属性：

```text
EnableTracyBridge=false
TracyBridgePath=
TracyBridgeBuildConfiguration=Release
```

行为约定：

- `EnableTracyBridge=false`：不尝试构建 submodule，不复制 bridge binary。运行时仅按 `TracyBridgePath`、环境变量或默认搜索路径探测现成 binary。
- `EnableTracyBridge=true`：要求 submodule 存在，构建或验证 bridge binary，并复制到 MCP/Avalonia 输出目录。
- `TracyBridgePath` 非空时优先使用该路径，方便本地调试独立 bridge 仓库。
- 运行时环境变量 `PROFILER_STUDY_TRACY_BRIDGE` 可覆盖默认 bridge 路径。
- 如果 `EnableTracyBridge=false` 但 `TracyBridgePath` 或 `PROFILER_STUDY_TRACY_BRIDGE` 指向可用 binary，运行时可以启用 Tracy；该模式只是不由主仓库构建 bridge。

运行时状态通过统一诊断返回：

```text
TracyBridgeStatus
  IsEnabledByBuild
  IsAvailable
  BridgePath
  BridgeVersion
  TracyVersion
  Reason
```

即使未启用 Tracy，`protocol=profiler_study` 的所有现有路径也不能读取这些开关或受其影响。

## Bridge CLI Contract

Bridge 提供三个稳定命令。

### `info`

```text
profiler-tracy-bridge info --json
```

输出：

```json
{
  "bridgeVersion": "0.1.0",
  "tracyVersion": "0.10.0",
  "supportedCommands": ["capture", "import", "info"],
  "normalizedSchemaVersion": 1
}
```

用途：

- MCP self-test 或 diagnostics 检查 bridge 是否存在。
- UI 在 Tracy 连接面板显示可用性。
- Artifact manifest 记录导入工具版本。

### `capture`

```text
profiler-tracy-bridge capture \
  --host 127.0.0.1 \
  --port 8086 \
  --seconds 30 \
  --output source.tracy \
  --normalized normalized \
  --diagnostics import-diagnostics.json
```

职责：

- 连接 Tracy target。
- 完成 Tracy protocol handshake、version check、stream receive 和 server query。
- 保存原始 `.tracy`。
- 同步生成 normalized 输出。
- 退出码和 diagnostics 明确区分 connect failed、protocol mismatch、capture timeout、write failed、unsupported event。

### `import`

```text
profiler-tracy-bridge import \
  --input capture.tracy \
  --normalized normalized \
  --diagnostics import-diagnostics.json
```

职责：

- 读取已有 `.tracy` 文件。
- 生成 normalized 输出。
- 不修改原始 `.tracy`。
- 对不兼容版本返回明确 diagnostics。

## Normalized 输出格式

第一版使用目录 + NDJSON，避免单个巨大 JSON object 带来的内存峰值。

```text
normalized/
  manifest.json
  threads.ndjson
  frames.ndjson
  cpu_zones.ndjson
  plots.ndjson
  diagnostics.json
```

`manifest.json`：

```json
{
  "schemaVersion": 1,
  "sourceFormat": "tracy",
  "sourcePath": "source.tracy",
  "tracyVersion": "0.10.0",
  "bridgeVersion": "0.1.0",
  "startTimeNs": 0,
  "endTimeNs": 1234567890,
  "timeBase": "trace-relative-ns",
  "frameSource": "tracy-frame-mark",
  "threadCount": 8,
  "zoneCount": 120000,
  "plotCount": 12
}
```

`threads.ndjson` 每行：

```json
{"threadId":123,"name":"RenderThread","processId":0}
```

`frames.ndjson` 每行：

```json
{"frameIndex":42,"name":"Frame","startNs":1000000,"endNs":2666666,"durationNs":1666666}
```

`cpu_zones.ndjson` 每行：

```json
{"sliceId":"z123","threadId":123,"name":"Renderer::Draw","startNs":1100000,"endNs":1500000,"depth":2,"sourceLocation":"Renderer.cpp:88"}
```

`plots.ndjson` 每行：

```json
{"counterName":"GPU Memory","timeNs":1200000,"value":512.0,"unit":"MB"}
```

`diagnostics.json`：

```json
{
  "warnings": [
    "GPU zones are present but not exported by schema v1.",
    "Allocations are present but not exported by schema v1."
  ],
  "unsupportedEventCounts": {
    "gpuZone": 128,
    "allocation": 4096
  }
}
```

时间统一为 trace-relative nanoseconds。C# 查询层再按 UI/MCP 需要转换成毫秒或帧索引。

## Core 数据模型接入

新增 Trace Workspace 相关模型应放在 `ProfilerStudyCore`，保持 UI/MCP 无关：

```text
TraceDocument
  Id
  SourcePath
  ArtifactId?
  SourceFormat: ProfilerStudy | Tracy | Perfetto | Systrace
  DisplayName
  CreatedUtc
  ImportDiagnostics
  QuerySession: ITraceQuerySession
```

```text
ITraceQuerySession
  GetSummary()
  GetFrames(range)
  GetThreads(filter)
  GetThreadSlices(time range, thread filter)
  GetScopeHotspots(time/frame range, top, filter)
  GetCounters(filter)
  GetCounterSamples(counter, time/frame range)
  GetFrameDetail(frame index)
  GetSliceDetail(slice id)
```

Tracy importer：

```text
TracyTraceImporter
  -> Resolve TracyBridgeStatus
  -> Run profiler-tracy-bridge import if source is .tracy
  -> Read normalized manifest + ndjson streams
  -> Create TracyTraceQuerySession
  -> Return TraceDocument
```

Tracy live capture：

```text
TracyCaptureService
  -> Resolve TracyBridgeStatus
  -> Allocate artifact directory
  -> Run profiler-tracy-bridge capture
  -> Register source.tracy + normalized + diagnostics
  -> Create TracyTraceQuerySession
```

第一版 `TracyTraceQuerySession` 只需要支持：

- summary
- frames，如果 `.tracy` 中存在 frame marks
- threads
- CPU zones as slices
- zone hotspots
- plots as counters
- time range analysis

没有 frame marks 时：

- `find_slow_frames` 返回 `framesUnavailable=true` 和 `frameSource=none`。
- `analyze_frame_detail` 返回明确错误或 diagnostics。
- `analyze_time_range` 和 `find_scope_hotspots` 仍可用。

## MCP Contract

现有 `capture_profile` 增加协议选择：

```json
{
  "url": "pc://127.0.0.1:8086",
  "protocol": "tracy",
  "duration_seconds": 30,
  "top": 10,
  "keep_session": true
}
```

字段：

```text
protocol = profiler_study | tracy
default = profiler_study
```

兼容要求：

- 未传 `protocol` 时完全走现有 ProfilerStudy path。
- `protocol=profiler_study` 时不探测 Tracy bridge。
- `protocol=tracy` 只支持 TCP host/port；Android Tracy target 需要用户先建立 adb forward，再传 `pc://127.0.0.1:<forwarded-port>`。后续可扩展 `android://tcp:<port>`。

新增通用 trace tools：

```text
load_trace_file(path, format=auto, top=10)
load_trace_artifact(artifact_id, top=10)
list_trace_artifacts(format?, since?, limit?)
get_import_diagnostics(session_id?|artifact_id?)
get_tracy_bridge_status()
```

保留兼容 alias：

- `analyze_session_file`
- `load_session_file`

它们继续表示 ProfilerStudy/FramePro legacy session 文件，不强行承担 `.tracy`。用户加载 `.tracy` 应使用 `load_trace_file`。

Loaded session 管理需要从只持有 `Session` 迁移到持有统一 trace query：

```text
LoadedTrace
  Id
  Source
  SourceFormat
  CreatedUtc
  LastAccessUtc
  QuerySession
  LegacySession?
  ImportDiagnostics
```

现有 MCP 分析工具兼容策略：

- `get_session_summary`：支持 Tracy，返回 `sourceFormat=Tracy`。
- `find_scope_hotspots`：Tracy 下聚合 CPU zones。
- `list_counters` / `query_counter`：Tracy 下读取 plots。
- `analyze_time_range`：Tracy 下优先支持 time range；如果用户只传 frame range，则要求 frames 存在。
- `find_slow_frames` / `analyze_frame_detail`：frames 存在时支持；不存在时返回结构化 diagnostics。
- `get_profiler_overhead`：仅 ProfilerStudy session 支持；Tracy 下返回 unsupported capability。

## Avalonia 接入

第一阶段 UI 只要求可打开和可见，不追求复刻 Tracy viewer。

Open dialog：

- 支持 `.tracy`。
- 调用 `TracyTraceImporter`。
- 打开后生成 `TraceDocument`，进入 recent files。

Connection panel：

```text
Protocol: ProfilerStudy | Tracy
Host
Port
Duration seconds
```

行为：

- `ProfilerStudy` 协议继续走当前 live `Session.ConnectToTcp`，保持持续连接和现有刷新模式。
- `Tracy` 协议走 fixed-duration capture。Capture 完成后打开 artifact 中的 `TraceDocument`。
- Bridge 不可用时，Tracy 选项显示不可用原因，但不影响 ProfilerStudy 连接。

Tracy document 第一版视图：

- Summary：source、duration、threads、zones、plots、frames、diagnostics。
- Timeline：thread lanes + CPU zone slices。
- Scopes/Hotspots：按 zone name 聚合。
- Counters：plots。
- Frames：仅在 frame marks 存在时启用。

## Artifact Store

Live Tracy capture 必须进入 artifact store：

```text
~/.profilerstudy/traces/captures/2026-06-11-153000-tracy/
  source.tracy
  normalized/
    manifest.json
    threads.ndjson
    frames.ndjson
    cpu_zones.ndjson
    plots.ndjson
    diagnostics.json
  import-diagnostics.json
  manifest.json
```

Artifact manifest：

```json
{
  "artifactId": "2026-06-11-153000-tracy",
  "sourceFormat": "tracy",
  "sourceKind": "live-capture",
  "sourcePath": "source.tracy",
  "normalizedPath": "normalized",
  "createdUtc": "2026-06-11T07:30:00Z",
  "bridgeVersion": "0.1.0",
  "tracyVersion": "0.10.0",
  "capture": {
    "host": "127.0.0.1",
    "port": 8086,
    "durationSeconds": 30
  }
}
```

用户显式打开外部 `.tracy` 文件时，默认不复制原文件，只创建 normalized cache 和 manifest，manifest 记录原始绝对路径。MCP 只能列出 ProfilerStudy 登记过的 artifact，不扫描用户目录。

## 错误处理

Bridge process 必须有：

- timeout
- cancellation
- stderr capture size cap
- normalized output size diagnostics
- exit code mapping

错误类型建议：

```text
BridgeUnavailable
BridgeVersionUnsupported
TracyProtocolMismatch
TracyConnectFailed
TracyCaptureTimeout
TracyImportFailed
TracyUnsupportedFileVersion
TracyNormalizedSchemaUnsupported
```

MCP 返回 `isError=true` 时仍提供结构化内容，至少包含：

```text
errorCode
message
bridgeStatus
diagnosticsPath?
logTail
```

UI 则显示简短错误，并在 diagnostics panel 展示详细日志。

## 安全和资源边界

- MCP 不扫描用户目录，只访问用户显式 path 或 artifact store manifest。
- Bridge command 参数必须通过 `ProcessStartInfo.ArgumentList` 传入，不拼接 shell command。
- Capture duration 有上限，MCP 默认沿用 1 到 300 秒。
- Normalized reader 应流式读取 NDJSON，避免一次性加载超大 trace。
- Artifact store 后续需要清理策略；第一阶段先只记录 size 和 created time，不自动删除。

## 实施阶段

### Phase 1：文档与 contract

- 新增本设计文档。
- 在 `mcp-external-trace-translators.md` 链接 Tracy 专项设计。
- 明确 submodule 路径、CLI contract、normalized schema、编译开关。

验收：

- 文档说明默认构建不依赖 Tracy。
- 文档说明 `profiler_study` 协议默认不变。

### Phase 2：Submodule 和 build plumbing

- 添加 `tools/profiler_tracy_bridge` submodule。
- 增加 `EnableTracyBridge` / `TracyBridgePath` MSBuild 属性。
- 增加 bridge binary resolve 逻辑和 `get_tracy_bridge_status`。
- 默认 build 不要求 submodule。

验收：

- 不初始化 submodule 时，MCP/Avalonia 正常 build。
- `EnableTracyBridge=true` 且 submodule 存在时能找到或构建 bridge。

### Phase 3：Bridge import

- 在 bridge 仓库实现 `info` 和 `import`。
- 主仓库新增 `TracyTraceImporter` 和 normalized reader。
- MCP 新增 `load_trace_file(.tracy)`。

验收：

- 真实 `.tracy` 文件可加载 summary、threads、zones、plots。
- 无 frame mark 的 trace 返回明确 frame diagnostics。

### Phase 4：Bridge capture

- 在 bridge 仓库实现 `capture`。
- 主仓库新增 `TracyCaptureService`。
- MCP `capture_profile(protocol=tracy)` 返回 artifact id、summary、hotspots。

验收：

- 可直连 Tracy target 端口完成 fixed-duration capture。
- artifact 可被 `load_trace_artifact` 重载。
- `protocol` 未传时现有 ProfilerStudy capture 不回归。

### Phase 5：Avalonia 打开和连接

- Open dialog 支持 `.tracy`。
- Connection panel 增加 protocol 选择。
- Tracy capture 完成后打开 artifact document。
- Timeline/summary/hotspots/counters 提供最小视图。

验收：

- `.tracy` 文件可从 UI 打开。
- Tracy live capture 完成后 UI 展示同一个 artifact。
- Bridge 不可用时 UI 提示清楚且不影响 ProfilerStudy 连接。

### Phase 6：高级 Tracy 事件

- GPU zones。
- locks。
- allocations。
- callstacks。
- messages。

这些能力需要 normalized schema 升级，并保留 schema version 兼容。

## 验证矩阵

默认构建：

```powershell
dotnet build ProfilerForStudy.sln -c Debug
dotnet run --project ProfilerStudy.McpServer/ProfilerStudy.McpServer.csproj -c Debug -p:TargetFrameworks=net8.0 -- --self-test
```

Tracy-enabled 构建：

```powershell
git submodule update --init tools/profiler_tracy_bridge
dotnet build ProfilerForStudy.sln -c Debug -p:EnableTracyBridge=true
profiler-tracy-bridge info --json
```

MCP 回归：

- `capture_profile(url="pc://127.0.0.1:8428")` 仍默认 ProfilerStudy。
- `capture_profile(url="pc://127.0.0.1:8086", protocol="tracy")` 走 bridge。
- `load_trace_file(path="sample.tracy")` 走 Tracy importer。
- `analyze_session_file(path="sample.profiler")` 仍走 legacy session。

Artifact 回归：

- Tracy live capture 创建 `source.tracy`、`normalized/`、`manifest.json`。
- `list_trace_artifacts(format="tracy")` 能看到新 artifact。
- `load_trace_artifact(artifact_id)` 能重载同一数据。

UI 回归：

- ProfilerStudy TCP connection 仍可连接和断开。
- Bridge 不可用时 Tracy UI 状态可读。
- `.tracy` 文件打开后 summary/timeline/counters 不为空。

## 推荐结论

Tracy 兼容应以 `tools/profiler_tracy_bridge` submodule 为唯一 C++ bridge 来源，主仓库只消费 bridge CLI 和 normalized schema。默认构建保持 Tracy optional，`profiler_study` 协议和现有文件读取不受影响。Tracy live capture 和 `.tracy` import 都先落到 Artifact Store，再通过 `TracyTraceQuerySession` 进入 MCP 和 Avalonia，从而保证同一份 capture 能被 UI 和 MCP 共同访问。
