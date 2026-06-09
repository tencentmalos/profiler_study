# 工具栏按钮与命令入口

## 用户场景

用户通过菜单和 toolbar 完成高频操作：连接、断开、Android 工具、跳转帧、切换面板、查找、scope 着色、callstack 开关。该控件族的核心不是按钮长什么样，而是同一命令在 WinForms 和 Avalonia 中的入口、可用状态、选中状态保持一致。

## 当前实现映射

### WinForms

主要文件：

- `ProfilerStudy/LegacyWinForms/MainForm.cs`
- `ProfilerStudy/LegacyWinForms/ProfilerStudyButton.cs`
- `ProfilerStudy/LegacyWinForms/ViewButton.cs`
- `ProfilerStudy/LegacyWinForms/CheckButton.cs`
- `ProfilerStudy/LegacyWinForms.Properties/Resources.cs`
- `ProfilerStudy/LegacyWinForms/Properties.Resources.resx`

主要控件：

- 连接：`m_ConnectButton`、`m_ConnectAndroidButton`、`m_DisconnectButton`、`m_ConnectSettingsButton`。
- 时间线导航：`m_GotoStartButton`、`m_TrackEndButton`、`m_GotoEndButton`、`m_GotoPrevSpikeButton`、`m_GotoNextSpikeButton`、`m_GotoMaxFrameButton`。
- 视图显隐：`m_InfoViewButton`、`m_FramesViewButton`、`m_ScopeViewButton`、`m_CoresViewButton`、`m_CustomStatsGraphButton`、`m_DataGridViewButton`。
- 分析工具：`m_ScopeColourModeButton`、`m_CallstackButton`、`m_ConditionalScopeTimeSlider`、`m_FindControl`。

WinForms toolbar 使用固定像素布局和 bitmap 资源，按钮行为由 `MainForm` 中的事件 handler 和状态更新方法统一维护。

### Avalonia

主要文件：

- `ProfilerStudy.Avalonia/App/Shell/MainWindow.axaml`
- `ProfilerStudy.Avalonia/App/Shell/MainWindow.axaml.cs`
- `ProfilerStudy.Avalonia/App/Shell/MainWindowViewModel.cs`
- `ProfilerStudy.Avalonia/Commands/RelayCommand.cs`
- `ProfilerStudy.Avalonia/Assets/Toolbar/*`

主要控件：

- Toolbar 使用 icon `Button` / `ToggleButton`。
- 图标主要来自 Material Icons，部分 legacy bitmap 在 `Assets/Toolbar`。
- Command 由 `MainWindowViewModel` 暴露。
- Native menu mirror 在 `MainWindow.axaml.cs` 中生成。

## 行为契约

- 菜单命令和 toolbar 命令语义一致。
- WinForms enabled/checked 状态与 Avalonia `CanExecute` / bound boolean 含义一致。
- Toggle 类按钮只表达显示/模式状态，不直接修改 Core 数据。
- 连接、断开、Android、track end、scope colour、callstack 等状态要在两个 UI 中使用同一术语。
- Avalonia toolbar 优先使用 icon + tooltip，不回退成大段文字按钮。

## 对齐状态与目标

- WinForms 是当前行为权威，尤其是连接状态、active view、Threads 面板显隐、track end。
- Avalonia 已有对应 menu/toolbar 和 command，但部分 command 仍是 shell 级占位或轻量实现；补齐时应优先对齐 WinForms 行为语义。
- 图标不要求 1:1：WinForms 可继续用 bitmap，Avalonia 可用 Material icon；但 tooltip/命令名必须表达同一含义。

## 数据流与状态归属

- 按钮 click 只发命令。
- enable/checked 由 shell/view model 根据 session、active view、panel visibility 计算。
- 具体数据操作在 `Session`、view、feature service 中完成。
- 不在按钮控件内读取 session 或写 settings。

## 验收清单

改工具栏或命令入口前：

1. 更新本文的控件映射。
2. 如果命令影响 shell 生命周期，同步更新 `../legacy-winforms-shell.md` 或 `../avalonia-shell.md`。
3. 检查 WinForms `UpdateButtonStates` 和 Avalonia command/binding 状态是否都需要调整。
4. 验证菜单入口和 toolbar 入口都能触发同一行为。
5. 验证无 session、已加载文件、live connected、active view 切换后的状态。
