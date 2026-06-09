# CPU Core 与 Context Switch 控件

## 用户场景

用户通过 CPU/core 轨道观察线程在哪个 core 上运行、是否发生 context switch、等待/切换是否解释了某些帧 spike。Android context switch 文件加载后也应进入同一分析语义。

## 当前实现映射

### WinForms

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

### Avalonia

主要文件：

- `ProfilerStudy.Avalonia/Controls/CoreTimelineStripControl.cs`
- `ProfilerStudy.Avalonia/Models/CoreSummaryRow.cs`
- `ProfilerStudy.Avalonia/Features/Sessions/SessionQueryService.cs`
- `ProfilerStudy.Avalonia/App/Shell/MainWindowViewModel.cs`

## 行为契约

- Core id、thread id、process name、thread state 的显示语义一致。
- 没有 context switch 数据时保持清晰降级，不误导为“无 CPU 活动”。
- Android context switch 文件加载后应进入同一展示语义。
- Core graph 的可视时间范围必须跟 frame/scope timeline 对齐。

## 对齐状态与目标

- WinForms 的 `CoresView` / `CoreGraph*` 是当前完整行为参考。
- Avalonia 当前以 `CoreTimelineStripControl` 和 summary rows 为基础，需要逐步补齐 context switch 细节。
- 如果 context switch 数据缺失，两个 UI 都应明确显示“不具备数据”，而不是显示空白图造成误解。

## 数据流与状态归属

Context switch 数据属于 Core session。可见 core、可见线程、时间范围是 UI 状态。Android 文件加载是 session 数据增强路径，应通过 Core loader/session 方法进入模型。

## 验收清单

改 core/context switch 控件前：

1. 更新本文。
2. 明确数据来源是实时 packet、ETL/legacy、还是 Android 文件。
3. 验证无 context switch、有 context switch、多 core、大量线程。
4. 如果改变 Android 加载流程，同步更新 shell 和 Core 文档。
5. 验证 core graph 与 frame/scope visible range 同步。
