# 时间线与 Frame Graph

## 范围

本文维护全局时间线、frame strip、frame graph、目标帧耗时线、帧 hover/selection、帧 spike 导航的 WinForms/Avalonia 对齐。

## WinForms 实现

主要文件：

- `ProfilerStudy/LegacyWinForms/Timeline.cs`
- `ProfilerStudy/LegacyWinForms/FrameGraphPanel.cs`
- `ProfilerStudy/LegacyWinForms/FrameGraphYAxis.cs`
- `ProfilerStudy/LegacyWinForms/FrameGraphYAxisScaleChangedHandler.cs`
- `ProfilerStudy/LegacyWinForms/FrameGraphYAxisTargetMSChangedHandler.cs`
- `ProfilerStudy/LegacyWinForms/FrameInfoPanel.cs`
- `ProfilerStudy/LegacyWinForms/TimeRange.cs`
- `ProfilerStudy/LegacyWinForms/VisibleRangeChangedHandler.cs`

数据来源：

- `Session.GetFrame`
- `Session.GetFrameStartEndTime`
- `Session.GetFrameIndex`
- `Session.FrameXToTime`
- `Session.TimeToFrameX`
- `CoreUtils.GetFrameTimeCategory`

## Avalonia 实现

主要文件：

- `ProfilerStudy.Avalonia/Controls/FrameTimelineControl.cs`
- `ProfilerStudy.Avalonia/Models/FrameSample.cs`
- `ProfilerStudy.Avalonia/Timeline/TimelineSelection.cs`
- `ProfilerStudy.Avalonia/Timeline/TimelineViewport.cs`
- `ProfilerStudy.Avalonia/Features/Sessions/SessionQueryService.cs`
- `ProfilerStudy.Avalonia/App/Shell/MainWindowViewModel.cs`

`FrameTimelineControl` 应只接收已经准备好的 `FrameSample`、viewport、selection，不直接读取文件或遍历完整 session。

## 对齐要求

- 帧颜色分类与 WinForms 保持一致：正常、warning、alert、target line。
- Hover、selected frame、visible range 的语义一致。
- `Home` / `End` / prev spike / next spike / max frame 行为一致。
- 目标帧耗时来自 settings/session 语义，不由控件硬编码。
- 大 session 下 Avalonia 可以采样绘制，但采样不能改变选择和导出语义。

## 修改流程

改 timeline/frame graph 前：

1. 更新本文。
2. 明确改动影响的是绘制、交互、帧分类、viewport 还是 selection。
3. 检查 WinForms 和 Avalonia 是否都需要同步。
4. 验证 loaded session、live session、空 session 三种状态。
