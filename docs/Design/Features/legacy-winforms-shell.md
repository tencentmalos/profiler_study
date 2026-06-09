# Legacy WinForms Shell 与 MainForm

## 设计定位

`MainForm` 是 legacy Windows UI 的 shell，也是当前最完整的 profiler 工作流参考。本文只维护 shell 级职责：菜单、toolbar、session 生命周期、docking、output window、Android 入口和全局状态协调。

基础控件按控件族维护在 `Controls/`，具体分析视图见 `legacy-winforms-profiler-views.md`。

## MainForm 组成

`MainForm` 是 WinForms `Form`，主要由以下区域组成：

- `menuStrip1`：File、View、Connection、Tools、Help 菜单。
- `panel1`：顶部大图标工具栏。
- `m_MainPanel`：中间填充区域，承载 `DockManager`。
- `m_DockManager`：MDI/docking 容器，管理每个 `MainSessionView`。
- `m_OutputWindowPanel` / `m_OutputWindowSplitter`：底部输出窗口和分隔条。
- `m_HoverBox`：跨 profiler 控件共享的 hover 提示。

初始化顺序包括设置加载、DPI 缩放、输出窗口、DockManager、定时器、启动文件/recording 参数和 update thread。

## Shell 职责边界

`MainForm` 可以负责：

- 创建、读取、保存、关闭 session。
- 创建 `MainSessionView` 并放入 `DockManager`。
- 维护 active session/view。
- 同步菜单和 toolbar 状态。
- 持久化全局 settings，例如最近文件、output window、连接设置。
- 处理 shell 级 Android、demo、about、settings 入口。

`MainForm` 不应该负责：

- 在 paint 路径中计算具体 graph 数据。
- 直接实现 scope/counter/core 分析算法。
- 绕开 `SessionViewSaveData` 保存具体 view 状态。
- 把 Avalonia 对齐逻辑写进 WinForms 控件。

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

## Session 生命周期

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

关键约束：

- 读写耗时操作走后台 `ThreadJob`，UI 线程只负责进度和结果处理。
- session close 必须先处理 dirty/save prompt。
- active view 变化必须同步按钮状态、菜单 checked 状态和 view-specific panel 状态。
- `SessionViewSaveData` 是 session view 布局和选择状态的持久化边界。

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

## 与 Avalonia 对齐

Avalonia shell 不复用 `MainForm` 或 `docker/`，但应对齐这些用户语义：

- File/View/Connection/Tools/Help 的命令分组。
- 连接、断开、Android 工具、Recent Files、Create Session from Selection。
- Output window 的可见性和日志角色。
- Threads/Cores/Scopes 的 active view 切换。
- Track end、frame navigation、scope colouring、callstack 入口。

如果 Avalonia 为跨平台体验改了呈现方式，应在 `avalonia-shell.md` 和对应控件族文档记录“语义对齐、视觉不完全一致”的原因。

## 验收清单

修改 `MainForm` 前先更新本文；实现后按影响范围验证：

- 菜单和 toolbar 两个入口都能触发同一命令。
- 打开文件、保存、关闭、关闭全部、最近文件行为不回退。
- 连接/断开/Android 连接状态正确更新按钮 enable。
- active view 切换后 toolbar checked 状态正确。
- Output window 显隐和高度持久化。
- Windows 环境完整 build，并手工验证受影响路径。
