# Custom Stats、Counters 与 Plot 控件

## 用户场景

用户通过 custom stats/counter 曲线观察随帧或随时间变化的指标，例如内存、任务数、GPU/CPU 统计、自定义采样值，并在选中帧附近查看 counter 明细。

## 当前实现映射

### WinForms

主要文件：

- `ProfilerStudy/LegacyWinForms/CustomStatsGraph.cs`
- `ProfilerStudy/LegacyWinForms/CustomStatsView.cs`
- `ProfilerStudy/LegacyWinForms/CustomStatSelector.cs`
- `ProfilerStudy/LegacyWinForms/CustomStatVisibilityChangedHandler.cs`
- `ProfilerStudy/LegacyWinForms/GetGraphValuesPerFrameFunction.cs`
- `ProfilerStudy/LegacyWinForms/GetGraphValuesPerSecFunction.cs`
- `ProfilerStudy/LegacyWinForms/LineGraph.cs`
- `ProfilerStudy/LegacyWinForms/LineGraphWindow.cs`
- `ProfilerStudy/LegacyWinForms/ValueGraphTimeRangeChangedHandler.cs`

### Avalonia

主要文件：

- `ProfilerStudy.Avalonia/Features/Counters/SelectedFrameCounterAnalyzer.cs`
- `ProfilerStudy.Avalonia/Features/Counters/SelectedFrameCounterRow.cs`
- `ProfilerStudy.Avalonia/Features/ProfilerStats/ucProfilerStats.axaml`
- `ProfilerStudy.Avalonia/Features/ProfilerStats/ProfilerStatsController.cs`
- `ProfilerStudy.Avalonia/Features/ProfilerStats/ProfilerStatsDocumentAnalyzer.cs`
- `ProfilerStudy.Avalonia/Features/ProfilerStats/ProfilerTimelineAdapter.cs`
- `ProfilerStudy.Avalonia/Features/ProfilerStats/ProfilerTimelinePlotModel.cs`
- `ProfilerStudy.Avalonia/Features/ProfilerStats/Timeline/*`
- `ProfilerStudy.Avalonia/Features/TimelineScope/Generators/Curve2DGenerator.cs`
- `third_party/ScottPlot/`：ProfilerStats / TimelineScope plotting 的源码级定制入口。

Core 数据来源：

- `Session.GetCustomStats`
- `Session.GetCustomStatsPerSec`
- `CustomStatSessionData`
- `CustomStatValueType`
- `CustomStatXAxisMode`
- Tracy `TraceDocument.QuerySession` / `TracyTraceQuerySession.EventStream.Plots`

## 行为契约

- Counter graph/unit/name 语义来自 Core custom stat 元数据。
- Trace-backed Tracy document 中，Tracy plots 作为 counter 曲线输入；graph 缺省为 `tracy`，单位未知时保持空/`-`，不伪造成 ProfilerStudy custom stat。
- Selected-frame counter 在 Tracy document 中以 Tracy frame set metadata 裁剪 plot samples；没有 frame metadata 或 frame 范围内没有 sample 时返回空集合和可降级 UI。
- 支持 frame、time、accumulated time 等 x-axis 模式时，两个 UI 的单位说明一致。
- Colour 修改或默认生成规则保持一致。
- Detail plot 可以是 Avalonia 增强，但不能改变 Core counter 语义。
- 大量 counter 时需要可控采样和可见性管理。

## 对齐状态与目标

- WinForms custom stats 是 legacy 行为参考，尤其是 graph/unit、可见性、x-axis mode。
- Avalonia `ProfilerStats` / `TimelineScope` 是更适合后续 Perfetto/Tracy counter 的承载点。
- Avalonia 使用 `third_party/ScottPlot/` 中的源码级 ScottPlot，而不是 NuGet `ScottPlot.Avalonia`。plot 交互、axis、tooltip、marker 或性能问题可以在子仓内做针对性调整。
- ScottPlot 源项目的目标框架声明必须允许 Avalonia shell 在只安装 .NET 8 SDK 的 macOS/Rider 环境中 restore/build；更高 SDK 专用目标应按 SDK 版本条件化，不能阻塞 `ProfilerStudy.Avalonia` 的 `net8.0` 加载。
- 对齐优先级：数据语义 > x-axis/单位 > 选择/hover > 视觉样式。

## 数据流与状态归属

Counter 数据来自 Core 或 trace query，曲线采样和可见性是 UI/feature 状态。控件可以选择哪些曲线显示、plot 高度、detail 展开状态，但不能修改 counter 原始值。ProfilerStudy custom stat 和 Tracy plot 共享 row model，但数据来源必须通过 `SessionDocument.Session` 或 `SessionDocument.TraceDocument.QuerySession` 明确分支。

## 验收清单

改 stats/counter plot 前：

1. 更新本文。
2. 明确影响 per-frame、per-second、accumulated 还是 selected-frame counter。
3. 检查 WinForms `CustomStatSelector` 与 Avalonia detail plot visibility 是否需要同步。
4. 验证 int64/double counter、缺 unit、缺 graph、超大 session。
5. 验证曲线采样不改变 min/max/selected frame 明细语义。
6. 如果修改 ScottPlot 子仓，记录子仓提交并验证 Avalonia 项目使用的是源码引用。
