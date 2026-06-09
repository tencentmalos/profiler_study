# Output、Hover、Overlay 与基础对话框

## 范围

本文维护输出窗口、hover 提示、overlay panel、settings/connect/android/about 等基础对话框的 WinForms/Avalonia 对齐。

## WinForms 实现

主要文件：

- `ProfilerStudy/LegacyWinForms/MainForm.cs`
- `ProfilerStudy/LegacyWinForms/HoverBox.cs`
- `ProfilerStudy/LegacyWinForms/ConnectSettingsDialog.cs`
- `ProfilerStudy/LegacyWinForms/AndroidConnectDialog.cs`
- `ProfilerStudy/LegacyWinForms/SettingsDialog.cs`
- `ProfilerStudy/LegacyWinForms/SaveChangesDialog.cs`
- `ProfilerStudy/LegacyWinForms/SaveOnExitDialog.cs`
- `ProfilerStudy/LegacyWinForms/AboutDialog.cs`
- `ProfilerStudy/LegacyWinForms/ContextSwitchErrorBox.cs`
- `ProfilerStudy/LegacyWinForms/NotShowingContextSwitchesMessageBox.cs`

## Avalonia 实现

主要文件：

- `ProfilerStudy.Avalonia/App/Shell/MainWindow.axaml`
- `ProfilerStudy.Avalonia/App/Shell/MainWindowViewModel.cs`
- `ProfilerStudy.Avalonia/Platform/AppSettingsService.cs`
- `ProfilerStudy.Avalonia/Platform/SourceViewerLauncher.cs`
- `ProfilerStudy.Avalonia/Platform/SourcePathMapper.cs`

Avalonia 当前多用 overlay panel 和 bound state 表达 settings/connect/android/help/about 等面板，不直接复用 WinForms dialog。

## 对齐要求

- Output window 显隐、高度/大小、日志追加行为可对齐，但实现可以不同。
- Hover 内容应来自调用控件准备好的文本/数据，不在 hover 控件中做业务查询。
- Connect/Android/Settings 面板字段含义与 WinForms dialog 保持一致。
- Save/close prompt 行为需要一致，尤其是 dirty session 的关闭路径。
- Avalonia 不调用 WinForms dialog 或 legacy `Utils.cs`。

## 修改流程

改 output/overlay/dialog 前：

1. 更新本文。
2. 明确是 shell overlay、基础 dialog、output log 还是 hover 行为。
3. 检查 settings 持久化位置。
4. 验证打开/关闭、取消、错误信息、重复打开等路径。
