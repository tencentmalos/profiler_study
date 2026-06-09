# Legacy WinForms Shell 与 MainForm

## 范围

本文只记录 `ProfilerStudy/LegacyWinForms/MainForm.cs` 的主体构成、菜单/工具栏、session 生命周期和 docking/output shell。基础控件按控件族维护在 `Controls/`，具体分析视图见 `legacy-winforms-profiler-views.md`。

Legacy WinForms 是当前 Windows 兼容 UI，也是 Avalonia 行为对齐的重要参考。

## MainForm 主体区域

`MainForm` 是 WinForms `Form`，主要由以下区域组成：

- `menuStrip1`：File、View、Connection、Tools、Help 菜单。
- `panel1`：顶部大图标工具栏。
- `m_MainPanel`：中间填充区域，承载 `DockManager`。
- `m_DockManager`：MDI/docking 容器，管理每个 `MainSessionView`。
- `m_OutputWindowPanel` / `m_OutputWindowSplitter`：底部输出窗口和分隔条。
- `m_HoverBox`：跨 profiler 控件共享的 hover 提示。

初始化顺序包括设置加载、DPI 缩放、输出窗口、DockManager、定时器、启动文件/recording 参数和 update thread。

## 菜单职责

- File：打开、保存、另存、关闭、关闭全部、导出 CSV、最近文件、退出。
- View：Threads/Cores/Scopes 切换、Threads 子面板显隐、scope 着色、输出窗口。
- Connection：新连接、连接、断开。
- Tools：Find、从选区创建 session、Android context switch、Settings。
- Help：注册、检查更新、demo simulator/player、startup page、About。

菜单项和工具栏按钮应保持语义一致。新增功能如果同时出现在菜单和 toolbar，要同步更新 enable/check 状态。

## 工具栏职责

顶部工具栏由 `ProfilerStudyButton` 和 `ViewButton` 组成：

- 连接组：`m_ConnectButton`、`m_ConnectAndroidButton`、`m_DisconnectButton`、`m_ConnectSettingsButton`。
- 时间线导航：`m_GotoStartButton`、`m_TrackEndButton`、`m_GotoEndButton`、prev/next/max spike。
- 面板显隐：Info、Frames、Scope、Cores、Custom Stats、Data Grid。
- 分析工具：Find、conditional scope slider、scope colour mode、callstack button。

`UpdateButtonStates` 及相关方法是按钮 enabled/checked 的权威入口。不要在零散事件里重复写状态规则。

## Session 管理

`MainForm` 持有：

- `m_Sessions`：所有加载/连接中的 `Session`。
- `m_SessionViews`：每个 session 对应的 `MainSessionView`。
- `m_ActiveView`：当前活动 session view。

核心流程：

- `Connect()`：创建 `Session`，调用 `Session.Connect()`，成功后加入 docking。
- `ConnectToAndroid(...)`：创建 `Session`，执行 Android 连接流程，成功后加入 docking。
- `Read(filename)`：后台读文件，恢复 `SessionViewSaveData`，成功后加入 docking。
- `Write()`：从活动 view 收集 `SessionViewSaveData` 并保存。
- `CreateSessionFromSelection`：通过 `Session.CopyTo` 复制选区成新 session。
- Close/CloseAll：处理保存提示、事件解绑、DockManager child 移除和 active view 更新。

## Output Window

输出窗口用于汇总 Core log、连接信息、adb forward 信息和 UI 操作反馈。

约束：

- 日志通过 `LogLine` 进入 UI。
- 输出窗口隐藏时缓存日志，重新显示后补写。
- 显隐和高度写入 `Settings`。
- 不要让 Core 直接写 WinForms 控件。

## Android Shell 流程

Android 相关入口保留在 Tools 菜单和 Android toolbar button：

- `AndroidConnectDialog` 选择 Debug/Release endpoint。
- `Session.ConnectToAndroid` 执行 adb forward 和本地 TCP 连接。
- context switch recording/loading 由独立对话框和 `Session.LoadContextSwitchFile` 完成。

普通连接设置对话框不负责枚举 Android endpoint。

## 修改规则

修改 `MainForm` 前：

1. 如果改变菜单、工具栏、session 生命周期、docking/output 结构，先更新本文。
2. 如果改变基础控件行为，先更新 `Controls/` 下对应控件族文档。
3. 如果改变 Threads/Scopes/Cores 等分析视图，先更新 `legacy-winforms-profiler-views.md`。
4. 保留保存提示、最近文件、active view、button state 的既有行为。
5. WinForms 行为变更需要在 Windows 环境构建并手工验证受影响路径。
