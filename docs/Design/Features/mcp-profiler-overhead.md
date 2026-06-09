# MCP Profiler 自身开销

## 背景

MCP 之前会在单帧结果里零散返回 `bytesSent`、`waitForSendCompleteMs`、`prevFrameSendMs`，也会把 profiler 采集到的 custom stats 当作 counter 暴露。但这些都不是“ProfilerStudy profiler 自身开销”的稳定入口，调用方需要自己理解 FramePro/ProfilerStudy 内部字段才能判断采集开销。

这会造成两个问题：

- 常规 `get_session_summary` 无法直接回答“当前 profiler 对目标程序造成了多少 overhead”。
- MCP 使用者可能只看 scope hotspots 或 custom stats，忽略 profiler 发送缓冲、字符串表、每帧等待发送完成等采集成本。

## 数据来源

第一阶段只使用 `.profiler` session 中已经持久化或实时接收到的数据，不做外部推断：

- `Session.SendBufferSize`、`Session.StringMemorySize`、`Session.MiscMemorySize`：目标进程内 profiler 运行时内存开销，来自 `SessionStatsPacket`。
- `Session.RecordingFileSize`：recording 文件体积，作为采集副产物大小。
- `Frame.BytesSent`：该帧 profiler 发送/记录的数据量。
- `Frame.WaitForSendCompleteTime`：目标进程等待 profiler 发送完成的时间，按 session timer 转换为毫秒。
- `Frame.PrevFrameSendTime`：上一帧 profiler 发送耗时，按 session timer 转换为毫秒。

这些字段比 custom stat 更适合作为 profiler overhead 的权威来源，因为它们由 profiler 协议和 session reader 维护，而不是目标程序自定义 counter。

## MCP 输出

新增专门工具：

```text
get_profiler_overhead(session_id, start_frame?, end_frame?, top?)
```

输出结构：

- `memoryOverhead`：send buffer、string、misc、total、recording file 字节数。
- `frameOverhead`：指定 frame range 内的总帧数、总 frame 时间、总 bytes、平均/最大 bytes、等待发送完成总时间/平均/最大、上一帧发送总时间/平均/最大、等待发送完成占 frame 时间比例。
- `topWaitFrames`：等待发送完成时间最高的帧。
- `topBytesFrames`：bytes sent 最高的帧。
- `diagnostics`：是否存在可观测 profiler wait、是否存在 profiler 数据发送、按平均帧时长判定的 wait stall 数量。

`get_session_summary`、`load_session_file`、`analyze_session_file`、`capture_profile` 这类 session 级结果也应包含同一份 `profilerOverhead`，避免默认摘要漏报。

## 判定口径

- `waitForSendCompleteMs` 是最直接的目标进程 stall 线索。它为 0 不代表 profiler 没有成本，只代表 session 中没有记录到等待发送完成。
- `prevFrameSendMs` 表示发送耗时，但不一定全部阻塞目标帧执行，因此单独汇总，不与 wait 合并成一个总 overhead。
- `waitPercentOfFrameTime` 使用 `totalWaitForSendCompleteMs / totalFrameMs * 100`，只表达观测到的等待发送完成占比。
- `stalledFrameCount` 沿用 legacy `Session.ProfilerStudyStall` 的语义：当 frame 的 `WaitForSendCompleteTime` 大于 session 平均帧时长时，认为 profiler wait 明显异常。

## 非目标

- 不从 scope 名称、线程名或 custom stat 名称猜测 profiler 内部开销。
- 不把 MCP 工具本身的 `captureTelemetry.totalToolMs` 混入目标程序 profiler overhead；前者是 Codex/MCP 侧捕获成本，后者是被测进程内 profiler 成本。
