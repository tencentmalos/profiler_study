# Scope、Thread Lane 与 Flame Chart

## 范围

本文维护线程 lane、scope block、scope 树、flame chart、scope 选择、scope 着色模式的 WinForms/Avalonia 对齐。

## WinForms 实现

主要文件：

- `ProfilerStudy/LegacyWinForms/TimeSpanGraph.cs`
- `ProfilerStudy/LegacyWinForms/TimeSpanGraphView.cs`
- `ProfilerStudy/LegacyWinForms/ThreadTimeSpanGraph.cs`
- `ProfilerStudy/LegacyWinForms/ThreadRowPanel.cs`
- `ProfilerStudy/LegacyWinForms/ThreadRowPanelMouseDownHandler.cs`
- `ProfilerStudy/LegacyWinForms/ThreadRowPanelMouseMoveHandler.cs`
- `ProfilerStudy/LegacyWinForms/ScopeColourMode.cs`
- `ProfilerStudy/LegacyWinForms/HighlightScopeHandler.cs`
- `ProfilerStudy/LegacyWinForms/HighlightSingleScopeHandler.cs`
- `ProfilerStudy/LegacyWinForms/SelectedTimeSpanChangedHandler.cs`

数据来源：

- `Session.GetTimeSpanInfo`
- `Session.FindPrevTimeSpan`
- `Session.FindNextTimeSpan`
- `TimeSpanList`
- `TimeSpanInfoSet`
- `RootFirstTimeSpanIterator`

## Avalonia 实现

主要文件：

- `ProfilerStudy.Avalonia/Controls/ThreadTimelineLaneControl.cs`
- `ProfilerStudy.Avalonia/Controls/SelectedFrameThreadLaneControl.cs`
- `ProfilerStudy.Avalonia/Controls/SelectedFrameFlameChartControl.cs`
- `ProfilerStudy.Avalonia/Features/TimelineScope/ThreadTimelineScopeAnalyzer.cs`
- `ProfilerStudy.Avalonia/Features/TimelineScope/ThreadTimelineScopeRow.cs`
- `ProfilerStudy.Avalonia/Features/Scopes/ScopeFrameDetailAnalyzer.cs`
- `ProfilerStudy.Avalonia/Features/Scopes/ScopeFrameDetailRow.cs`
- `ProfilerStudy.Avalonia/Features/Scopes/ScopeHotspotAnalyzer.cs`
- `ProfilerStudy.Avalonia/Features/Scopes/ScopeHotspotRow.cs`

## 对齐要求

- Scope block 的时间位置、嵌套层级、thread lane 归属一致。
- Scope 着色支持按 thread 和按 scope 两种语义。
- Selected scope、highlight scope、find next/prev 使用同一 selection/range 语义。
- Flame chart 输入来自选中帧 detail，不在 paint 中重扫 session。
- 源码信息可能缺失，UI 必须降级显示。

## 修改流程

改 scope/thread lane 前：

1. 更新本文。
2. 标明影响普通 thread timeline、selected-frame detail、flame chart 还是 hotspot 表。
3. 检查 WinForms `ScopeColourMode` 和 Avalonia `ScopeColorMode` 是否保持一致。
4. 验证多线程、大量嵌套 scope、无 source info 的 session。
