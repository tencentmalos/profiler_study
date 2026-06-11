# Tracy 兼容接入设计

## 目标

这份设计聚焦 ProfilerStudy 对 Tracy 的优先接入，覆盖两个核心能力：

- MCP 和 UI 可以按协议选择连接 profiler target，协议名称固定为 `study`、`tracy`、`perfetto`。默认仍使用 `study`，对应当前已有的 ProfilerStudy 原始实现；显式选择 `tracy` 时直连 Tracy 端口。
- MCP 和 UI 可以打开、加载并分析 `.tracy` 文件，且 live capture 产物和文件导入产物都能通过 Trace Workspace / Artifact Store 被复用。

Tracy 实现必须是完整 C# 版本，不能依赖 C++/CLI、C++ native helper、外部 bridge 进程或平台相关打包步骤。原因是 ProfilerStudy 需要在 macOS 构建和打包 Windows 产物，native bridge 会让跨平台 packaging 和发布链路变复杂。

第一版锁定 Azahar 当前使用的 Tracy `0.10.0`。后续版本通过 version adapter 扩展，不在第一版做自动兼容。

## 非目标

- 不在第一版支持任意 Tracy 版本；非 `0.10.0` 文件或 target 必须返回明确 unsupported diagnostics。
- 不把 Tracy 原始事件完整映射成 ProfilerStudy legacy `Session`。
- 不要求 WinForms 第一阶段直接打开 `.tracy`。
- 不让 MCP 依赖 Avalonia UI 是否正在运行，也不读取 UI selection、viewport 或 active document。
- 不在第一阶段实现 Tracy GPU、locks、allocations、callstacks、messages 的完整查询；这些事件先进入 diagnostics 或后续阶段。
- 不在主产品构建中编译 Tracy viewer、capture 工具或任何 C++ 代码。

## 现有基础和约束

当前 MCP 主要围绕 legacy `Session`：

- `capture_profile` 解析 `pc://host:port` 或 `android://...` 后调用 `Session.ConnectToTcp` / `Session.ConnectToAndroid`。
- `analyze_session_file` 和 `load_session_file` 只读取 `.profiler`、`.profiler_recording`、`.profiler_dump`。
- `ProfilerCaptureTarget` 只表达 Android forward 和普通 TCP，不表达 trace protocol。

Azahar 参考路径：

```text
/Users/bytedance/workspace/azahar/foundation/basic/modules/implements/profiler/private/spatial/profiler/tracy
```

该目录下的 SDK 是 target-side Tracy client 集成，当前版本来自 `tracy/common/TracyVersion.hpp`，为 `0.10.0`。第一版 C# reader/capture 必须按该版本实现。

## 方案选择

推荐采用 **ProfilerStudyCore 内置纯 C# Tracy 0.10.0 reader/capture + submodule 保存原始 Tracy/viewer 源码作参考**。

```text
ProfilerStudy.McpServer / ProfilerStudy.Avalonia
  -> TraceWorkspace
  -> TracyTraceImporter / TracyCaptureService
  -> ProfilerStudyCore.Tracy
       Tracy010FileReader
       Tracy010LiveCaptureClient
       Tracy010EventDecoder
       TracyNormalizer
       TracyTraceQuerySession
  -> TraceDocument / Artifact Store
```

这个方案的关键点：

- 产品运行时只加载 C# assemblies。
- `.tracy` 文件读取和 Tracy socket capture 都在 `ProfilerStudyCore` 内完成。
- MCP 和 UI 不知道 Tracy 协议细节，只消费 `ITraceQuerySession`。
- 原始 Tracy/viewer 源码只用于协议对照、fixture 生成和人工验证，不参与默认 build。

不再采用外部 `profiler-tracy-bridge` 可执行文件。原设计中的 bridge CLI、bridge binary resolve、bridge status、CMake build plumbing 全部废弃。

## Submodule 布局

保留 submodule：

```text
tools/profiler_tracy_bridge
  remote: git@github.com:tencentmalos/profiler_tracy_bridge.git
```

新的定位是 **Tracy source reference submodule**，不是产品 bridge。

主仓库职责：

- 在 `.gitmodules` 中登记 submodule。
- 在设计文档、构建文档和 MCP diagnostics 中说明 submodule 是参考源码，不是运行时依赖。
- C# 侧实现 Tracy `0.10.0` reader/capture。
- 产品 build、MCP self-test、Avalonia publish 都不能要求初始化该 submodule。

Submodule 仓库职责：

- 保存 Tracy upstream `0.10.0` 相关源码、viewer、capture/server/file reader 参考实现。
- 提供用于对照 C# 行为的原始 `.tracy` fixture 或生成说明。
- 后续 Tracy 版本升级时保存对应 upstream source 和 viewer 验证资料。

Submodule 更新规则：

- 更新 Tracy upstream 或 viewer 参考资料时先提交到 `profiler_tracy_bridge` 仓库。
- 主仓库只更新 submodule revision 和文档说明。
- 主仓库 PR 必须说明 Tracy upstream version、C# adapter 是否同步更新、fixture 是否重新验证。

## 构建和打包约束

Tracy C# 实现是 ProfilerStudyCore 的内建能力，不再保留单独的 Tracy 编译开关。

保留版本常量和参考源码路径配置，但它们不控制功能启停：

```text
TracyLockedVersion=0.10.0
TracySourceReferencePath=tools/profiler_tracy_bridge
```

默认路径：

```powershell
dotnet build ProfilerForStudy.sln -c Debug
```

要求：

- 不要求初始化 `tools/profiler_tracy_bridge`。
- 不构建任何 C++ 代码。
- MCP 和 Avalonia 编译通过。
- MCP 暴露 `protocol=tracy`、`.tracy` import 和 `get_tracy_status`。
- `protocol=study` 不初始化 Tracy reader/capture，也不受 Tracy 版本、fixture 或 submodule 状态影响。

Tracy 实现状态通过统一诊断返回：

```text
TracyStatus
  LockedVersion
  SupportedVersions
  SourceReferencePath
  SourceReferenceAvailable
  Reason
```

`SourceReferenceAvailable=false` 只表示参考源码 submodule 未初始化，不影响产品内置 C# Tracy 功能运行。

## Version Adapter 设计

第一版只注册 `0.10.0` adapter：

```text
ITracyVersionAdapter
  Version
  CanReadFile(header)
  CanConnect(welcome/protocolVersion)
  CreateFileReader()
  CreateLiveCaptureClient()
  CreateEventDecoder()
  CreateNormalizer()
```

```text
TracyVersionRegistry
  RegisteredAdapters: Tracy010VersionAdapter
  ResolveFileAdapter(path/header)
  ResolveLiveAdapter(protocolVersion)
```

行为要求：

- `.tracy` 文件版本不是 `0.10.0` 时，返回 `TracyUnsupportedFileVersion`。
- live target protocol version 不匹配时，返回 `TracyProtocolMismatch`。
- 所有 diagnostics 都要包含 detected version、supported versions、locked version。
- 后续支持新 Tracy 版本时，只增加 `Tracy0xxVersionAdapter` 和对应 fixture，不改 MCP tool contract。

## C# Tracy 0.10.0 组件

### `Tracy010FileReader`

职责：

- 读取官方 `.tracy` 文件。
- 校验 magic、file version、endianness、section layout 和压缩块。
- 在 event decoder 完整落地前，先支持遍历 LZ4 block stream，产出 block 数、压缩字节数、解压字节数、payload 字节数和 header diagnostics，作为后续事件解码和大文件边界控制的基础。
- 保存格式前段 metadata 需要优先解码：`delay/resolution/timerMul/lastTime/frameOffset/pid/samplingPeriod/cpuArch/cpuId/cpuManufacturer/onDemand/captureName/captureProgram/captureTime/executableTime/hostInfo`，以及 frame set 的 `name/continuous/frameCount/firstFrameStart/lastFrameEnd`。这些字段应进入 `TracyEventStream.Metadata` 和 MCP summary。
- 当 `.tracy` 保存格式中已经包含 frame set metadata 时，`TracyTraceQuerySession` 可以先用 frame set 作为 `frameSource=tracy-frame-set-metadata`，让 `get_session_summary` 和 `find_slow_frames` 返回基础 frame 信息；后续完整 event decoder 再把 frame marks 和 frame images 细化到同一模型。
- CPU zone 第一阶段按保存格式中的 `sourceLocationPayload` 和 thread timeline 解码，生成 `TracyCpuZoneSummary(threadId, sourceLocation, name, start, end)`；`find_scope_hotspots` 按 zone name 聚合，`analyze_time_range` 先用 frame metadata 推导 frame range 的时间范围。
- Plot 第一阶段按 Tracy `0.10.0` 保存格式中的 plot section 解码，生成 `TracyPlotSummary(name, type, format, min, max, sum, samples)`；`list_counters` 和 `query_counter` 使用该数据返回 Tracy counters。
- 解码 Tracy `0.10.0` 保存格式中的 CPU zones、thread names、frame marks、plots 和基础 metadata。
- 对第一版不导出的事件累计 unsupported counts。
- 输出 `TracyEventStream`，供 normalizer 使用。

输入：

```text
Tracy010FileReader.Read(path, cancellationToken)
```

输出：

```text
TracyEventStream
  Version
  Metadata
  Threads
  Frames
  CpuZones
  Plots
  Diagnostics
```

### `Tracy010LiveCaptureClient`

职责：

- 使用 `TcpClient` 直连 Tracy target 端口。
- 完成 Tracy `0.10.0` handshake、protocol version 校验、server query 和 LZ4 stream 解码。
- 按 fixed duration 采集事件。
- 把 live stream 解码为同一个 `TracyEventStream`。
- 采集完成后写入 artifact store。
- 第一阶段可以先完成 handshake + `WelcomeMessage` 解析，生成 metadata-only `TracyEventStream` 并进入 MCP loaded session；如果 duration 内还没有事件流 decoder，返回 `eventsDecoded=false` diagnostics，但 `capture_profile(protocol=tracy)` 必须能连接 Tracy 端口并返回 Tracy session。

输入：

```text
Tracy010LiveCaptureClient.Capture(host, port, durationSeconds, cancellationToken)
```

输出：

```text
TracyCaptureResult
  EventStream
  RawCapturePath?
  Diagnostics
  CaptureTelemetry
```

第一版 artifact 的权威数据是 normalized cache。是否同时写出官方 `.tracy` 文件由 `Tracy010FileWriter` 的验证状态决定：

- 如果 `Tracy010FileWriter` 已通过 viewer 兼容验证，live capture 写出 `source.tracy`。
- 如果 writer 未完成验证，live capture 写出 `raw-stream.bin` 或 `capture.ndjson` 作为调试材料，并在 manifest 中标记 `sourceKind=tracy-live-normalized-only`。
- MCP 查询不依赖 `source.tracy`，只依赖 normalized cache。

### `Tracy010EventDecoder`

职责：

- 把 file reader 和 live capture client 读到的 Tracy event payload 解码成统一 C# 事件模型。
- 对 string/source location/thread metadata 做 request/resolve。
- 保留 event source id，方便 diagnostics 追踪。

### `TracyNormalizer`

职责：

- 把 `TracyEventStream` 转成 Trace Workspace normalized 模型。
- CPU zones 转为 thread slices。
- frame marks 转为 frames。
- plots 转为 counters。
- unsupported event 进入 diagnostics。

### `TracyTraceQuerySession`

职责：

- 实现 `ITraceQuerySession`。
- 支持 summary、frames、threads、thread slices、zone hotspots、plots/counters、time range analysis。
- frame marks 不存在时，frame-centric 工具返回明确 diagnostics，time-range 工具继续可用。

## Normalized 输出格式

第一版 normalized cache 使用目录 + NDJSON，避免单个巨大 JSON object 带来的内存峰值。

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
  "implementation": "ProfilerStudyCore.Tracy",
  "readerVersion": "0.1.0",
  "tracyVersion": "0.10.0",
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
  SourceFormat: study | tracy | perfetto | systrace
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
  -> Resolve TracyVersionAdapter from .tracy header
  -> Read .tracy with Tracy010FileReader
  -> Normalize with TracyNormalizer
  -> Write normalized cache
  -> Create TracyTraceQuerySession
  -> Return TraceDocument
```

Tracy live capture：

```text
TracyCaptureService
  -> Resolve Tracy010VersionAdapter
  -> Allocate artifact directory
  -> Capture with Tracy010LiveCaptureClient
  -> Normalize with TracyNormalizer
  -> Register normalized + diagnostics + optional source.tracy/raw stream
  -> Create TracyTraceQuerySession
```

第一版 `TracyTraceQuerySession` 只需要支持：

- summary
- frames，如果 `.tracy` 或 live stream 中存在 frame marks
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
protocol = study | tracy | perfetto
default = study
```

兼容要求：

- 未传 `protocol` 时完全走现有 ProfilerStudy path。
- `protocol=study` 时不初始化或探测 Tracy reader。
- `protocol=tracy` 只支持 TCP host/port；Android Tracy target 需要用户先建立 adb forward，再传 `pc://127.0.0.1:<forwarded-port>`。后续可扩展 `android://tcp:<port>`。
- `protocol=perfetto` 是后续 Perfetto live/import 接入的保留名称；当前 Tracy 阶段只定义命名，不实现 Perfetto live capture。

新增通用 trace tools：

```text
load_trace_file(path, format=auto, top=10)
load_trace_artifact(artifact_id, top=10)
list_trace_artifacts(format?, since?, limit?)
get_import_diagnostics(session_id?|artifact_id?)
get_tracy_status()
```

`get_import_diagnostics` 支持 `session_id` 和 `artifact_id`。`session_id` 返回当前进程中 `TraceDocument.ImportDiagnostics` 挂载的结构化 diagnostics；`artifact_id` 从 artifact store 的 `manifest.json` 定位 `import-diagnostics.json`，不扫描用户目录。

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

Phase 4 的第一步允许先落内存级 `TraceDocument/ITraceQuerySession`，不强制立即写 normalized NDJSON cache。这个切片的目标是打通 MCP 可访问性：

- `load_trace_file(..., keep_session=true)` 返回 `session_id`。
- 该 `session_id` 进入同一套 `list_sessions`、`close_session`、`get_session_summary` 管理。
- Tracy 0.10.0 header 已验证但事件解码尚不完整时，`TracyTraceQuerySession` 必须返回 `sourceFormat=tracy`、`framesUnavailable=true`、`eventsDecoded=false` 和明确 diagnostics，不能假装是空的 ProfilerStudy session。
- legacy `Session` 仍由 adapter 路径处理，默认 `study` 行为不变。
- 在真正解码 zones/plots/frame marks 前，`find_slow_frames`、`find_scope_hotspots`、`list_counters`、`query_counter`、`analyze_time_range` 对 Tracy session 返回结构化空集合和 `eventsDecoded=false` diagnostics，不返回空引用或 legacy-only 错误；`get_profiler_overhead` 仍返回 unsupported capability，因为它只描述 ProfilerStudy target-side overhead。

现有 MCP 分析工具兼容策略：

- `get_session_summary`：支持 Tracy，返回 `sourceFormat=tracy`；当前 ProfilerStudy 原始实现返回 `sourceFormat=study`。
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
Protocol: study | tracy
Host
Port
Duration seconds
```

行为：

- `study` 协议继续走当前 live `Session.ConnectToTcp`，保持持续连接和现有刷新模式。
- `tracy` 协议走 fixed-duration C# capture。Capture 完成后打开 artifact 中的 `TraceDocument`。
- Tracy 选项始终可见；如果 target 或文件不是支持的 Tracy `0.10.0`，UI 显示版本不兼容 diagnostics，但不影响 Study 连接。

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
  source.tracy?            # C# writer 验证后启用
  raw-stream.bin?          # writer 未验证时的调试原始流
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
  "implementation": "ProfilerStudyCore.Tracy",
  "readerVersion": "0.1.0",
  "tracyVersion": "0.10.0",
  "capture": {
    "host": "127.0.0.1",
    "port": 8086,
    "durationSeconds": 30
  }
}
```

用户显式打开外部 `.tracy` 文件时，默认不复制原文件，只创建 normalized cache 和 manifest，manifest 记录原始绝对路径。MCP 只能列出 ProfilerStudy 登记过的 artifact，不扫描用户目录。

当前实现阶段采用 file-backed artifact store：

- 默认根目录为 `~/.profilerstudy/traces/captures`，测试和 headless 环境可通过 `PROFILER_STUDY_TRACE_ARTIFACT_ROOT` 覆盖。
- `load_trace_file(format=auto|tracy)` 和 `capture_profile(protocol=tracy)` 都登记 `manifest.json` 与 `import-diagnostics.json`，并在 tool 返回中带 `artifactId`。
- 已解码的 Tracy metadata、frame set、CPU zones、plots 和 diagnostics 写入 `normalized/` 下的 `manifest.json`、`threads.ndjson`、`frames.ndjson`、`cpu_zones.ndjson`、`plots.ndjson`、`diagnostics.json`。
- `list_trace_artifacts(format, limit)` 只枚举 artifact store 中的 manifest；`format=tracy` 对应 Tracy 导入和 live capture。
- `load_trace_artifact(artifact_id, keep_session)` 从 artifact store 读取 normalized cache，重建 `TracyTraceQuerySession`，让 MCP 可以复用 import/live capture 产物。
- `get_import_diagnostics(artifact_id)` 从 artifact store 读取 `import-diagnostics.json`，用于 UI 进程退出后仍可追溯导入或 live capture 诊断。
- `source.tracy` writer 和高级 Tracy 事件仍属于后续阶段，不在当前 artifact store 中声明已完成。

## 错误处理

错误类型：

```text
TracyUnsupportedFileVersion
TracyProtocolMismatch
TracyConnectFailed
TracyCaptureTimeout
TracyImportFailed
TracyFileFormatInvalid
TracyLz4DecodeFailed
TracyParserInvariantFailed
TracyNormalizedSchemaUnsupported
```

MCP 返回 `isError=true` 时仍提供结构化内容，至少包含：

```text
errorCode
message
tracyStatus
diagnosticsPath?
logTail
```

UI 则显示简短错误，并在 diagnostics panel 展示详细日志。

## 安全和资源边界

- MCP 不扫描用户目录，只访问用户显式 path 或 artifact store manifest。
- Tracy live capture 必须有 connect timeout、capture timeout、cancellation 和最大输出大小限制。
- `.tracy` reader 必须校验文件大小、section size、compressed block size 和解压后大小。
- Capture duration 有上限，MCP 默认沿用 1 到 300 秒。
- Normalized reader 应流式读取 NDJSON，避免一次性加载超大 trace。
- Artifact store 后续需要清理策略；第一阶段先只记录 size 和 created time，不自动删除。

## 实施阶段

### Phase 1：文档与 contract

- 更新本设计文档，移除 bridge CLI 方案。
- 在 `mcp-external-trace-translators.md` 链接纯 C# Tracy 专项设计。
- 明确 submodule 只保存 Tracy/viewer 原始源码。
- 明确 `study` 协议默认不变，且 `study` 对应当前 ProfilerStudy 原始实现。

验收：

- 文档说明默认构建不依赖 native bridge。
- 文档说明第一版锁定 Tracy `0.10.0`。

### Phase 2：Tracy status 和版本注册

- 在 Core 增加 `TracyStatus`、`TracyVersionRegistry`、`Tracy010VersionAdapter`。
- MCP 增加 `get_tracy_status`。
- MCP self-test 覆盖默认 `study` 协议和 Tracy status contract。

验收：

- `dotnet run --project ProfilerStudy.McpServer/ProfilerStudy.McpServer.csproj -c Debug -p:TargetFrameworks=net8.0 -- --self-test` 通过。
- `get_tracy_status` 返回 `LockedVersion=0.10.0`。

### Phase 3：`.tracy` 文件读取

- 实现 `Tracy010FileReader`、`Tracy010EventDecoder`、`TracyNormalizer`。
- 增加 `.tracy` fixture 或最小合成 fixture。
- MCP 新增 `load_trace_file(.tracy)`，返回 summary、threads、zones、plots。

验收：

- 真实或 fixture `.tracy` 文件可加载 summary、threads、zones、plots。
- 非 `0.10.0` 文件返回 `TracyUnsupportedFileVersion`。
- 无 frame mark 的 trace 返回明确 frame diagnostics。

### Phase 4：MCP query session

- 实现 `TracyTraceQuerySession`。
- 将 MCP loaded session 管理迁移到 `LoadedTrace`，保留 legacy `Session` adapter。
- 让 `get_session_summary`、`find_scope_hotspots`、`list_counters`、`query_counter`、`analyze_time_range` 支持 Tracy。

验收：

- `load_trace_file(path="sample.tracy", keep_session=true)` 返回 `session_id`。
- Tracy session 上可查询 summary、hotspots、counters、time range。
- `get_profiler_overhead` 在 Tracy session 上返回 unsupported capability。

### Phase 5：C# live capture

- 实现 `Tracy010LiveCaptureClient`。
- MCP `capture_profile(protocol=tracy)` 直连 Tracy target 端口。
- Capture 完成后写 normalized artifact，并返回 artifact id、summary、hotspots。

验收：

- 可直连 Tracy `0.10.0` target 端口完成 fixed-duration capture。
- artifact 可被 `load_trace_artifact` 重载。
- `protocol` 未传时现有 Study capture 不回归。

### Phase 6：Avalonia 打开和连接

- Open dialog 支持 `.tracy`。
- Connection panel 增加 protocol 选择。
- Tracy capture 完成后打开 artifact document。
- Timeline/summary/hotspots/counters 提供最小视图。

验收：

- `.tracy` 文件可从 UI 打开。
- Tracy live capture 完成后 UI 展示同一个 artifact。
- Tracy 版本不兼容时 UI 提示清楚且不影响 Study 连接。

### Phase 7：官方 `.tracy` writer 和高级 Tracy 事件

- `Tracy010FileWriter` 写出 viewer 可打开的 `source.tracy`。
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

MCP 回归：

- `capture_profile(url="pc://127.0.0.1:8428")` 仍默认 `study`。
- `capture_profile(url="pc://127.0.0.1:8086", protocol="tracy")` 走 C# Tracy live capture。
- `load_trace_file(path="sample.tracy")` 走 C# Tracy importer。
- `analyze_session_file(path="sample.profiler")` 仍走 legacy session。

Artifact 回归：

- Tracy live capture 创建 `normalized/`、`manifest.json` 和 diagnostics。
- writer 验证完成后 live capture 同时创建 viewer 可打开的 `source.tracy`。
- `list_trace_artifacts(format="tracy")` 能看到新 artifact。
- `load_trace_artifact(artifact_id)` 能重载同一数据。

UI 回归：

- Study TCP connection 仍可连接和断开。
- Tracy 版本不兼容时 UI 状态可读。
- `.tracy` 文件打开后 summary/timeline/counters 不为空。

## 推荐结论

Tracy 兼容应作为 `ProfilerStudyCore` 中的纯 C# reader/capture 能力实现，第一版锁定 Azahar 当前 Tracy `0.10.0`。`tools/profiler_tracy_bridge` submodule 保留为 Tracy/viewer 原始源码和协议对照资料，不参与产品构建、运行时加载或 macOS 打包 Windows 产物。Tracy live capture 和 `.tracy` import 都先落到 Artifact Store，再通过 `TracyTraceQuerySession` 进入 MCP 和 Avalonia，从而保证同一份 capture 能被 UI 和 MCP 共同访问。
