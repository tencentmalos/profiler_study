# Custom Stats、Counters 与 Plot 控件

## 范围

本文维护 custom stats/counter 列表、曲线图、detail plot、per-frame/per-time 数据点的 WinForms/Avalonia 对齐。

## WinForms 实现

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

## Avalonia 实现

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

## 对齐要求

- Counter graph/unit/name 语义来自 Core custom stat 元数据。
- 支持 frame、time、accumulated time 等 x-axis 模式时，两个 UI 的单位说明一致。
- Colour 修改或默认生成规则保持一致。
- Detail plot 可以是 Avalonia 增强，但不能改变 Core counter 语义。
- 大量 counter 时需要可控采样和可见性管理。

## 修改流程

改 stats/counter plot 前：

1. 更新本文。
2. 明确影响 per-frame、per-second、accumulated 还是 selected-frame counter。
3. 检查 WinForms `CustomStatSelector` 与 Avalonia detail plot visibility 是否需要同步。
4. 验证 int64/double counter、缺 unit、缺 graph、超大 session。
