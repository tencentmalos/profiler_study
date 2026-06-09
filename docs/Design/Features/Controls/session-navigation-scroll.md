# Session 导航、滚动与 Viewport

## 范围

本文维护 session scrollbar、visible range、track end、pan/zoom、跳转 start/end、选区复制相关的 WinForms/Avalonia 对齐。

## WinForms 实现

主要文件：

- `ProfilerStudy/LegacyWinForms/SessionScrollBar.cs`
- `ProfilerStudy/LegacyWinForms/SessionScrollBarPanel.cs`
- `ProfilerStudy/LegacyWinForms/SessionScrollBarChangedHandler.cs`
- `ProfilerStudy/LegacyWinForms/TimeRange.cs`
- `ProfilerStudy/LegacyWinForms/RangeChangedHandler.cs`
- `ProfilerStudy/LegacyWinForms/TrackEndChangedHandler.cs`
- `ProfilerStudy/LegacyWinForms/MainSessionView.cs`
- `ProfilerStudy/LegacyWinForms/MainForm.cs`

## Avalonia 实现

主要文件：

- `ProfilerStudy.Avalonia/Controls/SessionScrollbarControl.cs`
- `ProfilerStudy.Avalonia/Timeline/TimelineViewport.cs`
- `ProfilerStudy.Avalonia/Timeline/TimelineSelection.cs`
- `ProfilerStudy.Avalonia/App/Shell/MainWindowViewModel.cs`

## 对齐要求

- Visible range 表示当前可视时间/帧范围。
- Track end 打开时，新帧到达后视图跟随末尾；用户手动跳转/滚动时按既有行为关闭或保持。
- Pan/zoom 不修改 session 数据，只修改 viewport。
- 从 selection 创建 session 使用 Core `Session.CopyTo`，不能只复制 UI 行。
- WinForms `TimeRange` 与 Avalonia `TimelineViewport` 的概念变化要在本文记录。

## 修改流程

改导航/滚动前：

1. 更新本文。
2. 明确变化影响 frame index、time tick、viewport fraction 还是 UI selection。
3. 验证空 session、短 session、长 session、live session。
4. 如果影响选区复制，同步检查 Core 文档。
