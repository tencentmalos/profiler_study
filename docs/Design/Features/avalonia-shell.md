# Avalonia Shell

## 范围

本文记录 `ProfilerStudy.Avalonia` 的应用启动、`MainWindow` 主体结构、native menu、toolbar、overlay、status/output shell 和 `MainWindowViewModel` 的职责边界。基础控件和功能控件按控件族维护在 `Controls/`。

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

## 修改流程

修改 Avalonia shell 前：

1. 先更新本文。
2. 如果涉及控件实现，同步更新 `Controls/` 下对应控件族文档。
3. 保持 `MainWindow.axaml` 负责组合，业务计算放入 feature 层。
4. 构建 `ProfilerStudy.Avalonia`，并手工检查桌面和窄窗口布局。
