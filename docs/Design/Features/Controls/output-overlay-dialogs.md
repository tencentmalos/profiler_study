# Output、Hover、Overlay 与基础对话框

## 用户场景

用户需要查看连接/加载/adb/错误日志，查看 hover 详情，打开 connect/settings/android/about 等辅助界面，并在关闭或保存时得到一致的确认体验。

## 当前实现映射

### WinForms

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

### Avalonia

主要文件：

- `ProfilerStudy.Avalonia/App/Shell/MainWindow.axaml`
- `ProfilerStudy.Avalonia/App/Shell/MainWindowViewModel.cs`
- `ProfilerStudy.Avalonia/Platform/AppSettingsService.cs`
- `ProfilerStudy.Avalonia/Platform/SourceViewerLauncher.cs`
- `ProfilerStudy.Avalonia/Platform/SourcePathMapper.cs`

Avalonia 当前多用 overlay panel 和 bound state 表达 settings/connect/android/help/about 等面板，不直接复用 WinForms dialog。

## 行为契约

- Output window 显隐、高度/大小、日志追加行为可对齐，但实现可以不同。
- Hover 内容应来自调用控件准备好的文本/数据，不在 hover 控件中做业务查询。
- Connect/Android/Settings 面板字段含义与 WinForms dialog 保持一致。
- Save/close prompt 行为需要一致，尤其是 dirty session 的关闭路径。
- Avalonia 不调用 WinForms dialog 或 legacy `Utils.cs`。

## 对齐状态与目标

- WinForms 使用 modal dialog 和底部 output panel。
- Avalonia 使用 overlay panel、bound state 和平台服务。
- 对齐目标是字段语义、错误信息、确认流程一致；不要求 modal/overlay 形式一致。

## 数据流与状态归属

Output log 属于 shell 诊断状态；hover 内容属于触发控件准备的临时 UI 状态；settings/connect/android form state 属于 shell/view model 或 platform service。Core 返回错误，不直接弹窗。

## 验收清单

改 output/overlay/dialog 前：

1. 更新本文。
2. 明确是 shell overlay、基础 dialog、output log 还是 hover 行为。
3. 检查 settings 持久化位置。
4. 验证打开/关闭、取消、错误信息、重复打开等路径。
5. 验证 dirty session 关闭、连接失败、Android adb 失败三个错误路径。
