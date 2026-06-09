# Session 导航、滚动与 Viewport

## 用户场景

用户需要在长 session 中快速定位时间范围：跳到开头/结尾、跟随最新帧、缩放、平移、拖动 scrollbar，并从当前选区创建新 session。

## 当前实现映射

### WinForms

主要文件：

- `ProfilerStudy/LegacyWinForms/SessionScrollBar.cs`
- `ProfilerStudy/LegacyWinForms/SessionScrollBarPanel.cs`
- `ProfilerStudy/LegacyWinForms/SessionScrollBarChangedHandler.cs`
- `ProfilerStudy/LegacyWinForms/TimeRange.cs`
- `ProfilerStudy/LegacyWinForms/RangeChangedHandler.cs`
- `ProfilerStudy/LegacyWinForms/TrackEndChangedHandler.cs`
- `ProfilerStudy/LegacyWinForms/MainSessionView.cs`
- `ProfilerStudy/LegacyWinForms/MainForm.cs`

### Avalonia

主要文件：

- `ProfilerStudy.Avalonia/Controls/SessionScrollbarControl.cs`
- `ProfilerStudy.Avalonia/Timeline/TimelineViewport.cs`
- `ProfilerStudy.Avalonia/Timeline/TimelineSelection.cs`
- `ProfilerStudy.Avalonia/App/Shell/MainWindowViewModel.cs`

## 行为契约

- Visible range 表示当前可视时间/帧范围。
- Track end 打开时，新帧到达后视图跟随末尾；用户手动跳转/滚动时按既有行为关闭或保持。
- Pan/zoom 不修改 session 数据，只修改 viewport。
- 从 selection 创建 session 使用 Core `Session.CopyTo`，不能只复制 UI 行。
- WinForms `TimeRange` 与 Avalonia `TimelineViewport` 的概念变化要在本文记录。

## 对齐状态与目标

- WinForms 的 `SessionScrollBar` / `TimeRange` 是当前行为参考。
- Avalonia 的 `TimelineViewport` 是新模型，允许更明确表达 frame range 与 time range，但必须与 WinForms 用户操作语义一致。
- 对齐重点是“操作结果一致”，不是 scrollbar 像素实现一致。
- Avalonia 的 `SessionScrollbarControl` 不能只是 viewport 滑块；它应接收 `FrameSample` 和 target frame ms，在 track 底部绘制全局 frame strip，使用正常、warning、alert 三类颜色帮助用户在全局 session 中定位 spike。该 strip 是视觉采样，不能改变 viewport/selection 的真实 frame index 语义。
- frame strip、visible window 和拖拽命中都必须约束在 track rect 内；非整数像素宽度下允许按像素列聚合，但最后一列不能绘制到 track 外部。

## 数据流与状态归属

Viewport 是 UI 状态，Selection 是用户分析上下文，Session 是只读数据来源。Create Session from Selection 是唯一会基于 selection 生成新 Core session 的路径。

## 验收清单

改导航/滚动前：

1. 更新本文。
2. 明确变化影响 frame index、time tick、viewport fraction 还是 UI selection。
3. 验证空 session、短 session、长 session、live session。
4. 如果影响选区复制，同步检查 Core 文档。
5. 验证 track end 与手动滚动/跳转之间的切换。
