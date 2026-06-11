# MCP CPU+GPU 综合分析迭代规划

## 背景

当前 MCP 已经具备三类 Tracy 能力：

- `capture_profile(protocol=tracy)` 支持 `pc://` 和 `android://` live capture，并能解码 Tracy 0.10.0 的 CPU zones、frames、plots、GPU contexts、GPU zones。
- `.tracy` 文件可以通过 `load_trace_file` / `open_file` 进入统一 trace session，但文件侧 GPU section 仍以 diagnostics 记录为主，尚未导出完整 GPU 查询模型。
- `list_gpu_zones` 能返回 GPU context、zone 摘要、按名称聚合 hotspot，以及 `gpu-time` / `cpu-submit-time` 两种 time source 的区分。

从实机 Tracy GPU profiler 场景看，现有 MCP 更像“GPU zone 可见性验证”，还不是 CPU+GPU 综合性能分析入口。下一阶段应把能力从单个 flat GPU zone list 扩展为“以 frame 为中心”的综合视图，让 Codex 可以回答以下问题：

- 哪些帧慢，慢在 CPU 还是 GPU。
- 慢帧里最重的 CPU scope、GPU pass、counter 变化分别是什么。
- GPU 时间是否来自真实 `GpuTime`，还是 fallback 到 CPU submit 时间。
- GPU zone 是否能追溯到 context、submit、command buffer、source location 和 runtime counter。

## 对实机缺失清单的合理性评估

| 编号 | 结论 | 当前状态 | 规划处理 |
| --- | --- | --- | --- |
| 1. 按 frame 聚合 GPU 时间 | 合理，且优先级高 | `list_gpu_zones` 可按 frame range 过滤，但没有 per-frame summary | P0 增加 `analyze_gpu_frames` 或纳入 `analyze_frame_detail.gpuSummary` |
| 2. CPU/GPU 同帧关联 | 合理，核心目标 | `analyze_time_range` 已嵌套 GPU 概览，但 `analyze_frame_detail` 只返回 CPU flame graph + counters | P0 把 frame detail 扩展为 CPU、GPU、counters 同页结构 |
| 3. GPU critical path | 合理，但不能只靠现有数据承诺准确 | 现有 GPU zone 缺 queue/submit 边界，只能做 overlap/serial 近似 | P2 在 queue/submit 模型完整后提供，当前先输出 confidence/limits |
| 4. GPU zone hierarchy | 合理 | live decoder 内部有 GPU stack，但 public summary 是 flat | P1 在模型中保留 parent/children/depth，查询层输出 tree 和 flat timeline |
| 5. submit / command buffer / queue 关联 | 合理，但依赖 runtime 事件是否上报 | 当前 `TracyGpuZoneSummary` 只有 context/query/thread/source/time | P1 设计字段，P2 根据 Azahar runtime 或 Tracy event 扩展填充 |
| 6. source location 解析 | 合理，优先级高 | CPU/GPU 输出仍暴露 `sourceLocation` 数字，name 有部分解析，缺 file/line/function 结构 | P0 增加 source location table，并在 CPU/GPU/counter 结果中引用展开字段 |
| 7. fallback 诊断细节 | 合理，优先级高 | summary 有 fallback 数量，缺 zone 级明细 | P0 增加 fallback 列表和采样上限，列出 name/source/frame/query/context |
| 8. GPU percentile | 合理 | hotspot 只有 total/max/count | P0 增加 P50/P90/P99 和 avg |
| 9. frame range GPU timeline | 合理 | `list_gpu_zones` 返回 top duration，不适合作时间顺序复盘 | P0 增加 `order=start` 或独立 `get_gpu_timeline` |
| 10. counters 与 GPU join | 合理 | frame detail 有 counters，GPU 查询没有 counter join | P0 在 frame detail 和 GPU frame summary 中返回同帧 counters |
| 11. Tracy hardware sample 解码 | 合理但不应阻塞 CPU+GPU 首轮 | 当前 unsupported | P2 单独作为硬件采样子阶段，避免扩大首轮风险 |
| 12. 动态 zone name 支持 | 合理 | diagnostics 中仍可能出现 `ZoneName unsupported` | P1 补 Tracy dynamic name event，先保证 diagnostics 可定位 |
| 13. 多 GPU context 命名 | 合理，优先级中高 | live `GpuContextName` 已支持，但实机可能名称回填不足或 runtime 上报不完整 | P0 加强 context name diagnostics；P1 与 runtime 命名约定对齐 |

整体判断：清单方向正确，但优先级需要拆开。1、2、6、7、8、9、10、13 能直接提高 MCP 对实机 trace 的解释能力，应先做。3、5、11、12 需要更完整的 Tracy/runtime 事件保真，不能用简单聚合假装“critical path”已经准确。

## 目标 MCP 视图

下一阶段 MCP 不再让使用者自己把 `find_slow_frames`、`analyze_frame_detail`、`list_gpu_zones`、`query_counter` 手工拼起来，而是提供以 frame/time range 为中心的综合结果。

### `analyze_frame_detail` 扩展

保留现有字段，并新增：

```text
gpuSummary
  totalMs
  maxZoneMs
  zoneCount
  passCount
  blitCount
  drawCount?
  contextCount
  gpuTimeZoneCount
  cpuSubmitFallbackZoneCount
  timeSource
  confidence

gpuTimeline
  context
  contextName
  queue?
  submitId?
  commandBuffer?
  queryId
  name
  sourceLocation
  source
  start
  end
  relativeStartMs
  relativeEndMs
  durationMs
  depth
  parentId?
  timeSource

gpuHotspots
  name
  count
  totalMs
  avgMs
  maxMs
  p50Ms
  p90Ms
  p99Ms
  contextCount
  fallbackCount

frameCounters
  # 保留现有结构，增加 category/sourceHint 后用于 GPU join

correlation
  cpuFrameMs
  gpuFrameMs
  cpuGpuRatio
  dominantSide: cpu | gpu | mixed | unknown
  notes
```

`timeSource` 和 `confidence` 必须明确：

- `gpu-time/high`：frame 内 GPU zones 均已收到 `GpuTime`，可用于真实 GPU duration 分析。
- `mixed/medium`：同帧同时存在 `gpu-time` 和 `cpu-submit-time`，只能用于方向判断。
- `cpu-submit-time/low`：只有 fallback，不能当作真实 GPU 执行时间。

### 新增 `analyze_gpu_frames`

用于批量找 GPU 慢帧，输入为 session、frame range、top、time_source、counter_filter：

```text
analyze_gpu_frames(session_id, start_frame?, end_frame?, top=20, time_source=any)
```

返回：

```text
frames[]
  frameIndex
  cpuFrameMs
  gpuTotalMs
  gpuMaxZoneMs
  gpuZoneCount
  passCount
  blitCount
  drawCount?
  dominantGpuZone
  counterHighlights[]
  fallbackCount
  confidence

hotspots[]
  name/count/totalMs/avgMs/maxMs/p50Ms/p90Ms/p99Ms

fallbackZones[]
  frameIndex
  context/contextName
  queryId
  name
  sourceLocation/source
  start/end/durationMs
```

这个工具回答“哪些帧 GPU 最重”和“fallback 是否影响可信度”，比 `list_gpu_zones` 更适合实机回归分析。

### 新增 `get_gpu_timeline`

用于查看某个 frame range 或 time range 中按时间排序的 GPU zone：

```text
get_gpu_timeline(session_id, start_frame?, end_frame?, start_time_ns?, end_time_ns?, max_zones=200, include_hierarchy=true)
```

返回按 `start` 排序的 zones，而不是按 duration top 排序。它服务于“这一帧 GPU 实际排了哪些 pass/draw/blit/marker”的问题。

### `list_gpu_zones` 保持兼容

`list_gpu_zones` 继续作为轻量查询入口，但增加：

- `order=duration|start`，默认保持 duration。
- hotspot percentile。
- fallback detail 的 bounded sample。
- source location 展开字段。

## 数据模型改造

### Source Location Table

新增统一 source location 模型，CPU/GPU zone 都只保存 id，同时查询输出可展开：

```text
TraceSourceLocation
  id
  name
  function
  file
  line
  color?
  dynamicName?
```

`TracyEventStream` 增加：

```text
SourceLocations: IReadOnlyList<TracySourceLocationSummary>
```

normalized artifact 增加：

```text
source_locations.ndjson
```

CPU/GPU 输出中保留 `sourceLocation` 数字字段，同时增加：

```text
source
  name
  function
  file
  line
  dynamicName
```

### GPU Zone 保真字段

`TracyGpuZoneSummary` 扩展为：

```text
id
context
queryId
threadId
sourceLocation
name
start
end
timeSource
depth
parentId?
queue?
submitId?
commandBuffer?
zoneKind: pass | blit | draw | marker | unknown
```

第一阶段可以只可靠填充 `id/context/queryId/threadId/sourceLocation/name/start/end/timeSource/depth/parentId`。`queue/submitId/commandBuffer/zoneKind` 允许为空，但 schema 要先固定下来，避免后续 breaking change。

### GPU Frame Summary

新增内部聚合模型：

```text
TraceGpuFrameSummary
  frameIndex
  frameStart
  frameEnd
  cpuFrameDuration
  gpuTotalDuration
  gpuCoveredDuration
  maxZoneDuration
  zoneCount
  passCount
  blitCount
  drawCount
  contextCount
  gpuTimeZoneCount
  fallbackZoneCount
  confidence
  dominantZone
  counterHighlights
```

`gpuTotalDuration` 第一版定义为 frame 内 GPU zone duration sum，`gpuCoveredDuration` 定义为合并重叠区间后的覆盖时间。文档和工具输出必须同时给出这两个值，避免把嵌套/重叠 zone 的 sum 误称为 critical path。

## 采集和解码要求

### Live Tracy

P0 要求：

- `GpuTime` 回填成功时，zone 的 `timeSource=gpu-time`。
- 未回填时，zone 必须保留 `queryId/context/thread/sourceLocation/name`，并进入 fallback 明细。
- `GpuContextName` 到达后回填 context name；未到达时输出 `contextNameStatus=missing`，不要把默认名误认为真实 Vulkan context。
- source location payload 和 queried source location 都进入 source location table。

P1 要求：

- 保存 GPU stack depth 和 parent relation。
- 支持 dynamic zone name，至少在 diagnostics 中列出动态名事件数量和未解析样例。
- 文件 `.tracy` reader 不再只 skip GPU zones，而是与 live 同构输出 GPU contexts/zones。

P2 要求：

- 解析硬件 sample，先输出 CPU cycle/cache miss/branch miss 的 sample summary 和热点关联。
- 根据 runtime 上报或 Tracy 可用事件补 submit/command buffer/queue 关联。
- 在 queue/submit 模型完整后提供 GPU critical path，输出算法、confidence 和无法判断的原因。

### Runtime 协作边界

如果 runtime 侧没有上报 queue、submit、command buffer 或真实 context name，MCP 不能猜。MCP 只做：

- 明确输出字段为空。
- 在 diagnostics 中列出缺失原因和样例。
- 给出 `runtimeDataRequired` 提示，例如 `GpuSubmitIdMissing`、`GpuCommandBufferMissing`、`GpuContextNameMissing`。

## 实施阶段

### P0：同帧 CPU+GPU 可用

目标：实机 trace 能直接回答“慢帧是 CPU 还是 GPU，GPU 里哪类 pass 最重，counter 是否同帧异常”。

当前落地状态：

- `analyze_frame_detail` 已扩展 `gpuSummary`、`gpuTimeline`、`gpuHotspots` 和 `correlation`，并继续保留 `threadFlameGraphs`、`topSpans` 和 `frameCounters`。
- 新增 `analyze_gpu_frames`，按 frame 聚合 GPU zones，并返回 `gpuTotalMs`、`gpuCoveredMs`、`gpuMaxZoneMs`、pass/blit/draw count、counter highlights、hotspot percentile 和 fallback zone 明细。
- 新增 `get_gpu_timeline`，按 trace-relative start time 返回 frame range 或 time range 内的 GPU zones。
- `list_gpu_zones` 的 hotspot 已包含 avg/P50/P90/P99/fallback count，zone 输出包含 source 展开、contextNameStatus、zoneKind、depth、parentId。
- live Tracy source location payload 已进入 `source_locations.ndjson`，artifact reload 后 source 展开不丢失。
- GPU zone 模型必须同时保留 GPU timeline 时间和 CPU submit 触发时间。`analyze_frame_detail` 以 frame 为入口时，应纳入 GPU 时间落在该帧内的 zone，也应纳入 CPU submit 触发时间落在该帧内的 zone，并在 `gpuTimeline` 中输出 `threadName`、`cpuSubmitStart`、`cpuSubmitEnd`、`cpuSubmitRelativeStartMs` 和 `cpuSubmitRelativeEndMs`。否则实机会出现“GPU zone 可见，但无法回到触发线程/触发帧”的分析断点。

补充优化状态：

- `ZoneName` live event 第一阶段不再进入 unsupported；先按 Tracy queue 主流程识别为动态 zone name 事件并累计 `dynamicZoneNameCount` / 样例。完整回填到最近 open zone 的行为放入 P1，避免在缺少 viewer 主流程对照时错误绑定。
- `HwSampleCpuCycle`、`HwSampleInstructionRetired`、`HwSampleCacheReference`、`HwSampleCacheMiss`、`BranchRetired`、`BranchMiss` 第一阶段按 fixed struct 解码为 hardware sample summary，输出 count、first/last time 和少量 IP 样例；暂不做 IP 符号化和 CPU zone 归因。
- `GpuTimeWithoutZone` 从单纯 unsupported count 升级为结构化 diagnostics，包含 context、queryId、resolvedGpuTime 样例，便于判断 runtime 是否发送了孤立 query timestamp 或 capture 窗口截断。

任务：

- 扩展 `analyze_frame_detail`，加入 `gpuSummary`、`gpuTimeline`、`gpuHotspots`、`correlation`。
- 新增 `analyze_gpu_frames`，按 frame 聚合 GPU 数据并输出 percentile。
- 新增或扩展 source location table，让 CPU/GPU zone 输出 file/line/function/name。
- 增加 fallback zone 明细，包含 name/source/frame/query/context/timeSource。
- 在 `list_gpu_zones` 中增加 percentile、source 展开和可选 start order。
- 把 counters 按 frame join 到 GPU frame summary，优先识别名称包含 `PICA`、`pass`、`draw`、`blit`、`display target` 的 counter。

验证：

- MCP self-test 构造含 CPU zone、frame、plot、GPU context、GPU zone、fallback zone 的 trace，验证 `analyze_frame_detail` 返回 CPU+GPU+counters。
- 使用实机 10 秒 Tracy capture 验证 `analyze_gpu_frames` 至少返回 frame summary、fallback 明细和 context diagnostics。
- `.tracy` artifact reload 后，source location、GPU frame summary、fallback diagnostics 不丢失。

### P1：GPU 结构保真

目标：让 MCP 可以解释 render pass 内部结构和多 context 命名。

任务：

- `TracyGpuZoneSummary` 增加 `id/depth/parentId`。
- live decoder 输出 GPU hierarchy；`get_gpu_timeline(include_hierarchy=true)` 返回 tree 和 flat list。
- `.tracy` file reader 解码 GPU contexts/zones，停止只记录 unsupported diagnostics。
- 支持 dynamic zone name，保留静态 source name 和动态 runtime name。
- context name 增加状态字段：`resolved/missing/defaulted`。

验证：

- 对照 Tracy viewer 主流程和 fixture，确认 GPU hierarchy、context name、dynamic name 与 viewer 展示一致。
- 多 context fixture 中 `contextNameStatus` 与真实命名一致。

### P2：队列、硬件采样与 critical path

目标：在数据足够时做更接近 Tracy viewer 的 GPU 队列分析。

任务：

- 增加 submit/command buffer/queue 字段的 runtime 事件或 Tracy event adapter。
- 解码 Tracy hardware samples，提供 `list_hardware_samples` 和 CPU zone 关联。
- 新增 `analyze_gpu_critical_path`，只在 queue/submit/gpu-time 数据足够时返回 `supported=true`。
- critical path 输出合并区间、队列串行段、frame 尾等待和无法判断的原因。

验证：

- 没有 queue/submit 数据时，critical path 返回 `supported=false` 和明确 diagnostics。
- 有完整数据的 fixture 中，critical path 不使用 zone sum 冒充真实队列耗时。

## MCP 使用建议

完成 P0 后，默认实机 GPU 分析流程调整为：

1. `capture_profile(protocol=tracy, url=android://localabstract:azahar-tracy, duration_seconds=10, keep_session=true)`
2. `get_session_summary(session_id)`
3. `analyze_gpu_frames(session_id, top=20, time_source=any)`
4. 对最可疑帧调用 `analyze_frame_detail(session_id, frame_index=...)`
5. 如需复盘 GPU 顺序，调用 `get_gpu_timeline(session_id, start_frame=..., end_frame=...)`
6. 如 fallback 数量高，先查看 `fallbackZones` 和 `get_import_diagnostics`，不要直接下 GPU 时间结论。

## 风险和边界

- GPU zone duration sum 不等于 critical path。P0/P1 只能提供 sum、max、covered duration 和 confidence；P2 才能做 critical path。
- fallback 到 CPU submit time 的 zone 只能用于定位和粗略排序，不能当作真实 GPU 执行时间。
- counter join 依赖 frame metadata。如果 frame marks 缺失，只能按 time range join。
- source location 解析必须继续遵守“优先结构化读取和 P/Invoke/Marshal 对齐原始结构体”的约束，不能用大量手写偏移堆代码绕过 Tracy 结构。
- runtime 未上报的信息必须显式缺失，不能在 MCP 侧凭名称猜 submit、command buffer 或 queue。
