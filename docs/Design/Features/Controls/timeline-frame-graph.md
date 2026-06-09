# 时间线与 Frame Graph

## 用户场景

用户通过时间线观察帧耗时、定位 spike、选择当前帧、缩放/滚动可视范围，并把后续 Threads/Scopes/Counters 视图同步到同一时间上下文。

## 当前实现映射

### WinForms

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

### Avalonia

主要文件：

- `ProfilerStudy.Avalonia/Controls/FrameTimelineControl.cs`
- `ProfilerStudy.Avalonia/Models/FrameSample.cs`
- `ProfilerStudy.Avalonia/Timeline/TimelineSelection.cs`
- `ProfilerStudy.Avalonia/Timeline/TimelineViewport.cs`
- `ProfilerStudy.Avalonia/Features/Sessions/SessionQueryService.cs`
- `ProfilerStudy.Avalonia/App/Shell/MainWindowViewModel.cs`
- `third_party/ScottPlot/`：Avalonia plotting 的源码级定制入口。

`FrameTimelineControl` 应只接收已经准备好的 `FrameSample`、viewport、selection，不直接读取文件或遍历完整 session。

## 行为契约

- 帧颜色分类与 WinForms 保持一致：正常、warning、alert、target line。
- Hover、selected frame、visible range 的语义一致。
- `Home` / `End` / prev spike / next spike / max frame 行为一致。
- 目标帧耗时来自 settings/session 语义，不由控件硬编码。
- 大 session 下 Avalonia 可以采样绘制，但采样不能改变选择和导出语义。

## 对齐状态与目标

- WinForms 的 `Timeline` + `FrameGraphPanel` 是视觉参考：上方 timeline、frame strip、目标线、warning/alert 颜色。
- Avalonia 的 `FrameTimelineControl` 是对齐承载点，应逐步合并 frame strip 和 graph 的用户语义。
- Avalonia 使用 `third_party/ScottPlot/` 中的源码级 ScottPlot，而不是 NuGet `ScottPlot.Avalonia`。如果现有 ScottPlot API 无法表达逐帧 frame strip、固定像素高度、hover/selection 覆盖层，应优先在子仓做有边界的定制。
- 当前可接受差异：绘制技术不同，Avalonia 可用不同图标/字体；不可接受差异：同一帧在两个 UI 中分类不同、选中范围不同。

## 数据流与状态归属

```text
Session
  -> SessionQueryService / FrameSample
  -> FrameTimelineControl
  -> TimelineSelection / TimelineViewport
  -> Threads/Scopes/Counters view refresh
```

控件可以改变 selection/viewport，不能修改 session frame 数据。

## 验收清单

改 timeline/frame graph 前：

1. 更新本文。
2. 明确改动影响的是绘制、交互、帧分类、viewport 还是 selection。
3. 检查 WinForms 和 Avalonia 是否都需要同步。
4. 验证 loaded session、live session、空 session 三种状态。
5. 验证长 session 下滚动/缩放不卡顿，选中帧精确。
6. 如果修改 ScottPlot 子仓，记录子仓提交并验证 Avalonia 项目使用的是源码引用。
