# MCP 外部 Trace Translator 可行性分析

## 背景

当前 MCP server 以 FramePro session 为核心：

- `capture_profile` 通过 `Session.ConnectToTcp` 或 Android adb forward 连接 FramePro target。
- `analyze_session_file` 和 `load_session_file` 通过 `Session.Read` 读取 `.profiler`、`.profiler_recording`、`.profiler_dump`。
- `ProfilerAnalysisService` 直接查询 `FramePro.Session`，输出 frame、scope hotspot、单帧 flame graph、counter 和 thread 摘要。

这条路径适合 FramePro，因为 `ProfilerStudyCore/FramePro/Session.cs` 同时拥有 packet ingestion 和查询模型。Tracy、Perfetto、systrace 不共享 FramePro packet 格式。如果把它们硬塞成新的 FramePro packet 类型，维护成本和行为风险都会很高。更合适的方向是增加 translator 层，把外部 trace 归一化成 MCP 可查询的模型，而不是假装它们是原生 FramePro capture。

## 可行性摘要

两个方向都可行，但应该通过同一条 translator 边界实现。

| 来源 | 可行性 | 推荐第一版实现 | 主要风险 |
| --- | --- | --- | --- |
| Tracy live socket | 中 | 复用 Tracy 官方 C++ capture/worker 代码做 bridge，再输出 normalized JSON/IR | Tracy wire protocol 是源码定义的私有协议，会随 Tracy 版本变化 |
| Perfetto `.perfetto-trace` / `.pftrace` | 高 | 调用 Perfetto Trace Processor，查询标准 SQL 表 | 外部二进制管理和 schema 版本漂移 |
| Android systrace HTML/text | 高 | 同样走 Perfetto Trace Processor；它已经支持 systrace/ftrace text import | 旧 systrace 数据常缺少明确 frame 语义 |

推荐架构不是“translator 生成 FramePro 文件”，而是：

```text
MCP tool
  -> source detector
  -> format translator
  -> NormalizedTraceSession
  -> analysis/query facade
  -> 现有 MCP report shape
```

FramePro 也可以成为同一 facade 的一个 adapter，但保留现有 `Session` 路径，避免破坏 legacy 行为。

## 一手资料结论

Tracy 是实时 remote telemetry profiler，覆盖 CPU、GPU、memory、lock、context switch、sampling 等数据。其 upstream repository 将它描述为 nanosecond-resolution remote telemetry profiler，并随项目提供 capture/profiler 工具。

相关 upstream 文件：

- `https://github.com/wolfpld/tracy`
- `https://raw.githubusercontent.com/wolfpld/tracy/master/public/common/TracyProtocol.hpp`
- `https://raw.githubusercontent.com/wolfpld/tracy/master/server/TracyWorker.cpp`
- `https://raw.githubusercontent.com/wolfpld/tracy/master/capture/src/capture.cpp`

当前 Tracy `TracyProtocol.hpp` 定义了：

- `ProtocolVersion = 79`
- handshake shibboleth 字节串 `TracyPrf`
- 约 256 KiB 的 LZ4 target frame 常量
- `ServerQueryPacket` 类型，用于请求 string、source location、symbol、source code、frame name、plot、data transfer 等 metadata

`TracyWorker.cpp` 展示的 live connection 流程是：

1. 连接 host/port。
2. 发送 `HandshakeShibboleth`。
3. 发送 `ProtocolVersion`。
4. 读取 `HandshakeStatus`。
5. 读取 `WelcomeMessage`。
6. 开始读取 LZ4-compressed stream chunk。
7. 在发现缺失 string/source/symbol metadata 时发送 `ServerQueryPacket` 回查。

`capture.cpp` 展示 standalone capture utility 的默认行为：

- address 默认 `127.0.0.1`
- port 默认 `8086`
- output 为 `.tracy`
- 支持 duration 和 memory limit
- 通过 `Worker.Write` 保存 captured trace

Perfetto 官方 Trace Processor architecture 文档说明其 pipeline 是：raw trace、format-specific reader、timestamp sorter、storage、SQL engine。它明确覆盖 proto、JSON、systrace、perf、Gecko、Fuchsia 和 archive-like formats。Perfetto external-format 文档说明 Android systrace 是 legacy 格式，但 Trace Processor 能把 systrace report 中的 textual ftrace 数据导入到 `slice`、`sched_slice`、`ftrace_event` 等标准 SQL 表。Trace Processor CLI 可以打开 trace 并运行 SQL；`traceconv` 可以把 Perfetto protobuf trace 转成 Chrome JSON、systrace 等格式。

相关 Perfetto 文档：

- `https://perfetto.dev/docs/design-docs/trace-processor-architecture`
- `https://perfetto.dev/docs/getting-started/other-formats`
- `https://perfetto.dev/docs/analysis/trace-processor`
- `https://perfetto.dev/docs/analysis/trace-summary`
- `https://perfetto.dev/docs/quickstart/traceconv`
- `https://perfetto.dev/docs/reference/trace-packet-proto`

## 架构建议

### 1. 引入 Normalized Trace Model

新增一个 MCP-facing model，表达现有工具需要的公共数据子集：

```text
NormalizedTraceSession
  Metadata: source path/url, source format, process names, clock domain, capture time
  Frames: index, name, startNs, endNs, durationNs, optional source
  Threads: stable id, process id, name, source thread id
  Slices: thread id, name, category, startNs, durationNs, depth, source location
  Counters: name, unit, value type, samples(timeNs, value, count)
  Scheduling: optional cpu, thread id, startNs, durationNs, state
  Diagnostics: import warnings, dropped/ambiguous events, dependency versions
```

内部统一使用 nanoseconds 作为 canonical time unit。FramePro adapter 通过 `TimerFrequency` 把 tick 转成 ns；外部 import 保留原生 ns timestamp。MCP 输出可以继续保持当前兼容形态，例如 `durationMs`、`frameIndex`、`thread`、`counter`。

### 2. 在 Normalized Model 之上增加 Analysis Facade

增加一个可以由 `FramePro.Session` 或 `NormalizedTraceSession` 支撑的小接口：

```text
ITraceAnalysisSession
  Summary
  GetFrames(range)
  GetThreads()
  GetSlices(range, thread filter)
  GetCounters(filter/range)
  GetFrameDetail(frame index)
```

不要一次性重写 `ProfilerAnalysisService`。第一步只抽出可在 neutral DTO 上运行的查询/报告逻辑：

- summary
- slow frames
- scope hotspots
- frame detail flame graph
- counters
- range analysis

随后保留 `FrameProTraceAnalysisSession` adapter 包住现有 `Session`。外部 translator 则产生 `NormalizedTraceAnalysisSession`。

### 3. Source Detection 和 MCP Tool 形态

保持现有 tool 稳定，同时增加更通用入口：

- `load_trace_file(path, format=auto, top=10)`：支持 FramePro、Perfetto、systrace，后续支持 `.tracy`。
- `capture_profile(url, ...)`：保留现有 `pc://` 和 `android://` 作为 FramePro target。
- 增加 `tracy://host:port` 到 `capture_profile`，或新增 `capture_tracy_profile`。建议使用独立 `tracy://` scheme，避免 Tracy TCP server 和 FramePro TCP server 混淆。

不要让模型传任意 SQL。所有查询应该是代码内固定 SQL，只允许 frame range、process name、thread name、top count 等经过校验的参数。

## Tracy Socket 支持

### 推荐路径：Tracy Bridge Helper

用一个 native helper 复用或链接 Tracy 官方代码来实现 live Tracy 支持：

```text
MCP capture_profile tracy://127.0.0.1:8086
  -> profiler-tracy-bridge --host 127.0.0.1 --port 8086 --seconds 30 --format normalized-json
  -> bridge 使用 Tracy Worker/capture 逻辑
  -> 输出 NormalizedTraceSession JSON
  -> MCP 读取 normalized JSON 并暴露 session_id
```

从用户视角看，这仍然是 MCP 直接连接 Tracy target socket。区别只是 protocol 实现位于一个小 bridge，而不是手写 C# decoder。

原因：

- Tracy wire protocol 不是稳定公开 interchange format，而是源码定义、版本绑定的协议。
- live capture 不只需要 LZ4 stream decoding，还需要 server-query metadata round trip，用来补齐 string、frame name、source location、symbol、source code。
- 复用 Tracy 代码可以覆盖 protocol update、`.tracy` save/load 行为、on-demand mode、source transfer、missing metadata 等 edge case。

bridge 可以作为可选小 executable 分发。缺少 executable 时，MCP tool 应返回清晰的 dependency error 和构建/安装指引，而不是在分析阶段才失败。

### 备选路径：纯 C# Tracy Client

技术上可行，但不建议作为第一版。

需要完成：

- handshake 和 protocol-version negotiation。
- LZ4 streaming frame 解码。
- 移植 Tracy queue item struct、compression/decompression helper、timestamp conversion 和 metadata resolution。
- 实现 string/source/symbol/frame name 的 server query flow。
- 持续跟踪 upstream Tracy protocol 兼容性。

这能得到完整 managed-code 部署，但会让本项目承担 Tracy protocol maintainer 的成本。除非 Tracy 成为核心一等目标，否则这个成本不划算。

### Tracy MVP Scope

第一版 Tracy MVP 建议支持：

- 通过 `tracy://host:port` capture
- duration-limited capture
- process/program name
- 可用时导入 frames
- CPU zones 作为 slices
- thread names
- plots 作为 counters
- 对暂不支持的 GPU、allocation、lock、context-switch、callstack、source-code 数据输出 import warnings

第二阶段再支持：

- 通过同一 bridge 读取 `.tracy` 文件
- sampling/callstack summary
- lock contention summary
- GPU zones
- memory allocation summary
- source location 和 symbol enrichment

## Perfetto 和 Systrace 文件支持

### 推荐路径：Perfetto Trace Processor

用 Perfetto Trace Processor 作为 parser/query engine：

```text
MCP load_trace_file /path/to/trace.perfetto-trace
  -> trace_processor_shell query fixed SQL
  -> rows -> NormalizedTraceSession
  -> MCP analysis tools
```

这条路径可以覆盖：

- native Perfetto protobuf traces (`.perfetto-trace`, `.pftrace`)
- Android systrace HTML/text
- 后续如果需要，也可以接受 Chrome JSON traces
- 其他 Trace Processor 已能识别且对 MCP 有价值的格式

这比在 C# 里实现 protobuf 和 systrace parser 更稳，因为 Perfetto 已经提供 format detection、chunked readers、timestamp sorting、SQL storage 和稳定分析层。

### 初始 SQL Extraction

normalized data 先通过固定 SQL 提取。

Frames：

```sql
SELECT ts, dur, name
FROM actual_frame_timeline_slice
WHERE dur > 0
ORDER BY ts;
```

Fallback frame candidates：

```sql
SELECT ts, dur, name
FROM slice
WHERE dur > 0
  AND (name LIKE '%Frame%' OR name LIKE '%Choreographer%' OR name LIKE '%doFrame%')
ORDER BY ts;
```

Slices：

```sql
SELECT s.ts, s.dur, s.name, s.depth, t.utid, th.tid, th.name AS thread_name,
       p.pid, p.name AS process_name
FROM slice s
JOIN track t ON s.track_id = t.id
LEFT JOIN thread_track tt ON tt.id = t.id
LEFT JOIN thread th ON th.utid = tt.utid
LEFT JOIN process p ON p.upid = th.upid
WHERE s.dur >= 0
ORDER BY s.ts;
```

Counters：

```sql
SELECT c.ts, c.value, ct.name
FROM counter c
JOIN counter_track ct ON c.track_id = ct.id
ORDER BY c.ts;
```

Scheduling：

```sql
SELECT ts, dur, cpu, utid, end_state
FROM sched_slice
ORDER BY ts;
```

具体 SQL 需要先检查 table 是否存在，因为 Perfetto table 会随 trace 类型和版本变化。缺失可选 table 应产出 warning，不应硬失败。

### Systrace 特定行为

Systrace 应被视为 import compatibility mode：

- 只在 Trace Processor 能识别内容时接受 `.html`、`.systrace`、`.txt`。
- 提取 ATrace slices 和 ftrace scheduling data。
- 优先使用明确 frame timeline 数据；不存在时，从 `Choreographer#doFrame`、`doFrame`、`DrawFrame` 或用户指定 frame-slice name 合成 frames。
- 输出中标记 synthesized frames，避免模型过度解读。

Perfetto 已取代 systrace 成为现代 Android tracing 的推荐方案。MCP 应支持读取旧 systrace 文件，但文档和错误提示应引导新 capture workflow 使用 Perfetto 或 FramePro。

### Dependency Strategy

第一版不建议把大型 Perfetto binary vendoring 到仓库。

推荐查找顺序：

1. 查 `PERFETTO_TRACE_PROCESSOR` 环境变量。
2. 查 `PATH` 中的 `trace_processor_shell` 或 `trace_processor`。
3. 只有在用户显式操作或文档化 setup command 下，才通过 Perfetto 官方 wrapper 下载。
4. 在 import diagnostics 中记录 dependency version。

CI 和 self-test 应允许 fake trace-processor runner，这样 parser output 测试不依赖 native binary。

## MCP Tool 变更建议

建议新增或调整这些 tools：

| Tool | 用途 |
| --- | --- |
| `load_trace_file` | auto-detect 并读取 FramePro、Perfetto、systrace，后续支持 Tracy file |
| `capture_profile` with `tracy://host:port` | 通过 bridge 做 duration-limited live Tracy capture |
| `get_import_diagnostics` | 返回 translator warnings、dependency versions、table availability、dropped-event counts |
| Existing analysis tools | 继续支持 FramePro session 或 normalized external session |

现有 `load_session_file` 可以保留为 FramePro-specific alias，保证兼容。

## 错误处理和安全边界

必须保留这些约束：

- 延续当前显式 path 行为；不要扫描目录。
- 将 path resolve 到 full path，tool output 只包含用户请求的文件。
- bridge/helper process 必须有 duration 和 output size 上限。
- timeout 或 MCP cancellation 时杀掉 child process。
- 不通过 MCP 暴露任意 shell execution。
- 不允许模型传 SQL；只允许固定 SQL 和经过校验的参数。
- tool response 按 time range 和 top count 限制输出；更大的 normalized session 只保存在 server-side，并通过 `session_id` 访问。
- loaded-session metadata 中包含 `sourceFormat`、`sourcePath`、`importWarnings`、dependency versions。

## 实施阶段

### Phase 1：Normalized Analysis Boundary

- 定义 `NormalizedTraceSession` DTO。
- 增加 `ITraceAnalysisSession`。
- 用 `FrameProTraceAnalysisSession` 包住现有 FramePro `Session`。
- 将 slow-frame、hotspot、counter、range analysis 的公共逻辑迁移到接口之上。
- 保持现有 MCP tools 行为兼容。

### Phase 2：Perfetto/Systrace Import

- 增加 `load_trace_file`。
- 增加 `PerfettoTraceProcessorRunner`。
- 通过固定 SQL 提取 frames、slices、counters、threads、processes、scheduling。
- 增加 systrace import warning 和 frame-synthesis rules。
- 用小型 checked-in fixture 或生成的 Trace Processor JSON output 做测试。

建议先做这一阶段，因为它风险更低，并且可以快速提供 offline-file 价值。

### Phase 3：Tracy Bridge MVP

- 增加 `tracy://host:port` source parsing。
- 构建或文档化 `profiler-tracy-bridge`。
- 支持 duration-limited live capture 和 normalized JSON output。
- 导入 CPU zones、frames、thread names、plots。
- 增加 dependency detection 和清晰错误信息。

### Phase 4：更深入的外部诊断

- 增加 Perfetto frame timeline 和 sched-blockage diagnostics。
- 增加 Tracy callstack、lock、GPU、allocation summaries。
- 增加跨 FramePro、Perfetto、systrace、Tracy session 的 compare-session 支持。

## 待确认问题

- `docs/profiler-mcp-quickstart.md` 是否继续保持 FramePro-only，还是等 `load_trace_file` 落地后扩展所有 source scheme？
- Tracy bridge 应作为 solution 内项目构建，还是作为可选外部依赖？
- 是否明确支持 Chrome JSON？Perfetto 能读取它，Tracy 也有围绕 Chrome-style timeline event 的 import/export 路径。
- systrace 缺少 `actual_frame_timeline_slice` 时，默认 frame heuristic 应选哪组 slice name？

## 推荐结论

先通过 Perfetto Trace Processor 和 normalized trace model 实现 Perfetto/systrace 文件读取。随后通过复用 upstream Tracy protocol 代码的 bridge helper 增加 Tracy live socket 支持。除非未来强需求是单 binary 部署，否则不建议第一版手写纯 C# Tracy protocol。

这条路线能快速扩展 MCP 的 trace 支持范围，同时最大程度保持现有 FramePro 行为稳定。
