# Avalonia Shell

## 设计定位

Avalonia 是后续跨平台 UI 和外部 trace 工作流的主线 shell。本文维护应用启动、`MainWindow` 结构、native menu、toolbar、overlay、status/output shell 和 `MainWindowViewModel` 职责边界。基础控件和功能控件按控件族维护在 `Controls/`。

Avalonia 要对齐 legacy WinForms 的 profiler 语义，但不复制 WinForms 的 docking、GDI 控件或固定像素实现。

## 应用入口

- `App/Program.cs`：进程入口。
- `App/App.axaml` / `App.axaml.cs`：Avalonia app、资源、主题初始化。
- `App/AppThemeService.cs`：Suki/Avalonia 主题切换。
- `App/AvaloniaSmokeTest.cs`：轻量冒烟入口。

## MainWindow 布局

`App/Shell/MainWindow.axaml` 使用 `SukiWindow`，主布局为 profiler 工作区：

- Row 0：菜单栏，包含 File/View/Theme/Connection/Tools/Help。
- Row 1：toolbar，包含连接、Android、timeline 导航、panel toggle、find。
- Row 2：主工作区，包含 toolbox、splitter、当前分析视图、overlay panel。
- Row 3：output window。
- Row 4：status bar。

Shell 应始终以可用 profiler 工作区为首屏，不做 landing page。

## Shell 职责边界

Avalonia shell 可以负责：

- 应用窗口、菜单、toolbar、status/output/overlay 布局。
- 组合 feature 控件和 view model 状态。
- 路由 command 到 feature service/controller。
- 管理 theme、平台 source viewer、app settings。
- 同步 native menu 和 Avalonia menu 的 checked/enabled 状态。

Avalonia shell 不应该负责：

- 在 `MainWindow.axaml.cs` 中读取 profiler 文件或计算分析结果。
- 在 XAML code-behind 中直接遍历 Core `Session`。
- 引用 WinForms 控件、dialog 或 `docker/`。
- 把一个具体 feature 的大量状态继续塞进 shell，而不拆 service/controller。

## Code-behind 职责

`MainWindow.axaml.cs` 只处理窗口层 glue：

- 加载 XAML 和 named controls。
- 构建 native menu mirror。
- attach/detach `MainWindowViewModel`。
- attach/detach `TimelineViewport`、`TimelineSelection`。
- 同步 `ucProfilerStats` 的 document、viewport、selection。
- 应用主题变化。

不要把 feature 查询、文件读取、scope/counter 分析写进 code-behind。

## MainWindowViewModel 职责

`MainWindowViewModel` 是 shell 级状态和命令汇聚点：

- Session open/save/close/recent files。
- Live connection 和 Android panel 状态。
- Timeline viewport、selection、pan/zoom、frame/spike navigation。
- Threads/Cores/Scopes active view。
- Thread panel 显隐、线程 focus/hide/order/collapse。
- Output/toolbox/main dock 的显隐、minimize、floating 状态。
- Settings、callstacks、help、registration、update、about overlay。
- Theme base skin / color theme。
- Source viewer 入口。

当新功能逻辑超过简单命令转发时，应拆到 `Features/*` 的 service/controller/analyzer，`MainWindowViewModel` 只负责组合和发布状态。

## Session 数据流

Avalonia session 数据流应保持单向：

```text
SessionLoader
  -> SessionDocument
  -> SessionQueryService / Feature Analyzer
  -> Row model / plot model / control input
  -> TimelineSelection / TimelineViewport / command
```

控件不直接打开文件，不直接保存 session，不在 render 中重新跑完整分析。需要共享给 MCP 的分析逻辑，不应只写在 Avalonia row model 中。

## 菜单和 Toolbar

Avalonia menu 与 native menu 应保持语义一致：

- File：Open、Save、Save As、Close、Export、Recent、Exit。
- View：Threads/Cores/Scopes、Threads 子面板、Scope Colouring、Output Window。
- Theme：Light/Dark 和 color theme。
- Connection：Connect/Disconnect。
- Tools：Find、Create Session from Selection、Android、Settings。
- Help：Registration、Update、Demo、Startup、About。

Toolbar 使用 icon button / toggle button，并通过 tooltip 解释。不要把工具栏变成文字按钮矩阵。

## Overlay 与 Dock 状态

当前 Avalonia shell 用绑定状态模拟 toolbox/output/main session dock 的显示、最小化、floating overlay。它不是 WinForms `docker/` 的移植。

规则：

- Shell 状态在 view model 中维护。
- 具体 overlay 内容可以拆到 feature 控件。
- 不引用 WinForms docking 代码。

## 与 WinForms 对齐

必须对齐的语义：

- File/View/Connection/Tools/Help 分组和核心命令。
- 连接/断开、Android 工具、recent files、create session from selection。
- Threads/Cores/Scopes active view。
- Timeline navigation、track end、scope colouring、find。
- Output window 作为诊断日志区域。

可以不同的实现：

- Avalonia 使用 overlay 和 bound state，不复刻 WinForms MDI dock。
- Avalonia toolbar 可使用 Material icon，不强制使用 legacy bitmap。
- Avalonia 可以以跨平台文件 picker/source viewer 替代 WinForms 对话框。

## 验收清单

修改 Avalonia shell 前先更新本文；实现后按影响范围验证：

- `dotnet build ProfilerStudy.Avalonia/ProfilerStudy.Avalonia.csproj -c Debug -p:TargetFrameworks=net8.0`
- 如果当前环境需要，带上 `AvaloniaBuildTasksLocation` workaround。
- 桌面和窄窗口下菜单、toolbar、status/output 不重叠。
- native menu 和 XAML menu 的 command/checked 状态一致。
- 打开 session、切换 active view、timeline navigation、output toggle 的基本路径可用。
