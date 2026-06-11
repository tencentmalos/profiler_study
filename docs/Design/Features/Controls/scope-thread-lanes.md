# Scope、Thread Lane 与 Flame Chart

## 用户场景

用户通过线程 lane 和 flame chart 找到单帧或时间范围内的主要耗时 scope，切换按线程/按 scope 着色，选中 scope 后查看源码、callstack 或上下文。

## 当前实现映射

### WinForms

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
- Tracy `TraceDocument.QuerySession` / `TracyTraceQuerySession.EventStream.CpuZones`

### Avalonia

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

## 行为契约

- Scope block 的时间位置、嵌套层级、thread lane 归属一致。
- Trace-backed Tracy document 中，CPU zones 进入 hotspot 表和后续 thread-lane 输入；当前阶段没有完整嵌套层级时，hotspot 使用 zone name 聚合并保留 diagnostics/降级语义。
- Scope 着色支持按 thread 和按 scope 两种语义。
- Selected scope、highlight scope、find next/prev 使用同一 selection/range 语义。
- Flame chart 输入来自选中帧 detail，不在 paint 中重扫 session。
- 源码信息可能缺失，UI 必须降级显示。

## 对齐状态与目标

- WinForms 的 `TimeSpanGraph*` 是 timeline lane 行为参考。
- Avalonia 的 lane/flame 控件应优先对齐选择、hover、着色和层级显示，不需要复刻 GDI 绘制代码。
- `ScopeHotspotAnalyzer` 和 `ScopeFrameDetailAnalyzer` 是 Avalonia 当前较清晰的分析边界；能复用给 MCP 的逻辑应上移到 Core analysis 或共享 query 层。

## 数据流与状态归属

Scope 控件输入应是已经裁剪/分析过的 row 或 block model。控件可以发出 selected scope/hover scope，不应自行遍历所有 thread 的完整 `TimeSpanList`。

线程 hide/collapse/order 是 UI 状态；Core session 数据不随之改变。

## 验收清单

改 scope/thread lane 前：

1. 更新本文。
2. 标明影响普通 thread timeline、selected-frame detail、flame chart 还是 hotspot 表。
3. 检查 WinForms `ScopeColourMode` 和 Avalonia `ScopeColorMode` 是否保持一致。
4. 验证多线程、大量嵌套 scope、无 source info 的 session。
5. 验证 find next/prev 与 selected/highlight scope 不互相污染。
