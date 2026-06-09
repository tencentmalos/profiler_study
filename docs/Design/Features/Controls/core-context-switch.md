# CPU Core 与 Context Switch 控件

## 范围

本文维护 CPU/core 轨道、context switch 显示、Android context switch 文件加载后的展示对齐。

## WinForms 实现

主要文件：

- `ProfilerStudy/LegacyWinForms/CoreGraph.cs`
- `ProfilerStudy/LegacyWinForms/CoreGraphPanel.cs`
- `ProfilerStudy/LegacyWinForms/CorePanel.cs`
- `ProfilerStudy/LegacyWinForms/CoresView.cs`
- `ProfilerStudy/LegacyWinForms/ContextSwitchesVisibleChangedHandler.cs`
- `ProfilerStudy/LegacyWinForms/WaitEventsVisibleChangedHandler.cs`
- `ProfilerStudy/LegacyWinForms/AndroidContextSwitchRecordingDialog.cs`
- `ProfilerStudy/LegacyWinForms/RecordingAndroidContextSwitchesForm.cs`
- `ProfilerStudy/LegacyWinForms/NoContextSwitchesWarningDialog.cs`

Core 数据来源：

- `ProfilerStudyCore/Model/ContextSwitch.cs`
- `ProfilerStudyCore/Model/ContextSwitchArray.cs`
- `ProfilerStudyCore/Transport/AndroidContextSwitchFileLoader.cs`
- `ProfilerStudyCore/Sessions/Session.cs`

## Avalonia 实现

主要文件：

- `ProfilerStudy.Avalonia/Controls/CoreTimelineStripControl.cs`
- `ProfilerStudy.Avalonia/Models/CoreSummaryRow.cs`
- `ProfilerStudy.Avalonia/Features/Sessions/SessionQueryService.cs`
- `ProfilerStudy.Avalonia/App/Shell/MainWindowViewModel.cs`

## 对齐要求

- Core id、thread id、process name、thread state 的显示语义一致。
- 没有 context switch 数据时保持清晰降级，不误导为“无 CPU 活动”。
- Android context switch 文件加载后应进入同一展示语义。
- Core graph 的可视时间范围必须跟 frame/scope timeline 对齐。

## 修改流程

改 core/context switch 控件前：

1. 更新本文。
2. 明确数据来源是实时 packet、ETL/legacy、还是 Android 文件。
3. 验证无 context switch、有 context switch、多 core、大量线程。
4. 如果改变 Android 加载流程，同步更新 shell 和 Core 文档。
