# ProfilerStudy Avalonia 实现设计

## 目标

`ProfilerStudy.Avalonia` 的目标是成为 Windows 和 macOS 原生可用的 profiler 桌面客户端。旧版 WinForms `ProfilerStudy` 继续保留为 Windows legacy 客户端；Avalonia 版本不逐行翻译 WinForms 控件，而是复用现有 profiler core 数据能力，重新实现跨平台 UI、交互和绘制层。

第一版可用版本的目标不是功能完全等价，而是覆盖日常性能分析闭环：

- 打开 `.profiler`、`.profiler_recording`、`.profiler_dump` 文件。
- 展示 session 摘要、frame timeline、thread/scope 视图、scope 详情。
- 支持 timeline 缩放、水平滚动、hover、选中 frame/time range。
- 支持 scope 搜索、慢帧定位、热点 scope 排序。
- 支持 custom stat/counter 曲线和单帧 counter 查看。
- 支持 VSCode/CLion 源码跳转。
- Windows/macOS 均可构建、启动、加载同一份样本文件。

暂不作为第一版目标：

- 完整复刻 WinForms dock 拖拽系统。
- 完整迁移所有设置窗口。
- Windows ETL/context switch 采集。
- Demo simulator/recording player 的原生替代。
- 在线抓取和 Android adb 工作流的完整 UI 化。

## 当前状态

当前仓库已有：

- `ProfilerStudy`：WinForms legacy 客户端。
- `ProfilerStudyCore`：session、packet、frame、scope、custom stat 等核心逻辑，已经支持 `net8.0`。
- `ProfilerStudy.McpServer`：基于 core 的 MCP 分析服务。
- `ProfilerStudy.Avalonia`：已创建的 Avalonia + SukiUI + SkiaSharp 骨架。

当前 Avalonia 骨架具备：

- `SukiWindow` 主窗口。
- SukiUI 主题。
- 打开文件入口。
- 生成 sample data 入口。
- session 基础摘要。
- `FrameTimelineControl`，通过 SkiaSharp 绘制帧耗时柱状图。

当前缺口：

- ViewModel 仍是临时 preview 模型，不足以支撑完整交互。
- Skia 控件还没有统一坐标系统、缓存、输入处理和虚拟化。
- 没有线程、scope、custom stat 的 Avalonia 视图模型。
- 没有平台服务层，源码跳转、剪贴板、文件选择等还散落在 UI 侧。
- 没有自动化回归测试覆盖 Avalonia 读取同一 session 后的数据一致性。

## 总体架构

采用四层结构：

```text
ProfilerStudyCore
  Session / Frame / Packet / TimeSpan / CustomStat / file IO

ProfilerStudy.Avalonia.Core
  查询模型、projection、selection state、timeline math、view data adapters

ProfilerStudy.Avalonia.Platform
  文件选择、剪贴板、源码跳转、应用路径、外部工具、平台检测

ProfilerStudy.Avalonia
  Avalonia views, SukiUI shell, SkiaSharp controls, view models
```

初期可以先把 `Avalonia.Core` 和 `Avalonia.Platform` 作为 `ProfilerStudy.Avalonia` 内的文件夹实现；当边界稳定后再拆成独立项目。

建议目录：

```text
ProfilerStudy.Avalonia/
  App.axaml
  MainWindow.axaml
  Shell/
    MainWindowViewModel.cs
    NavigationItemViewModel.cs
    StatusBarViewModel.cs
  Sessions/
    SessionDocument.cs
    SessionLoader.cs
    SessionSummaryViewModel.cs
    SessionQueryService.cs
  Timeline/
    TimelineView.axaml
    TimelineViewModel.cs
    TimelineCanvas.cs
    TimelineViewport.cs
    FrameSample.cs
  Threads/
    ThreadsView.axaml
    ThreadsViewModel.cs
    ThreadRowViewModel.cs
  Scopes/
    ScopesView.axaml
    ScopesViewModel.cs
    ScopeRowViewModel.cs
    ScopeDetailViewModel.cs
  Counters/
    CountersView.axaml
    CountersViewModel.cs
    CounterSeries.cs
    CounterGraphCanvas.cs
  Rendering/
    SkiaCanvasControl.cs
    SkiaRenderCache.cs
    ProfilerPalette.cs
    TextMeasurer.cs
  Platform/
    IFileDialogService.cs
    ISourceViewerLauncher.cs
    IClipboardService.cs
    IAppPaths.cs
    SourcePathMapper.cs
```

## UI Shell 设计

SukiUI 用于应用外壳和常规 UI，不用于高密度 profiler 绘图。

## 视觉还原策略

Avalonia 版本的主工作区必须以旧版 WinForms `ProfilerStudy/LegacyWinForms` 的视觉语言为基准，而不是另起一套现代 dashboard 风格。SukiUI 只补足窗口、按钮、对话框、导航等通用外壳；用户真正用于分析的区域需要尽量还原旧版 ProfilerStudy 的工程化界面。

还原优先级：

1. **Timeline/Frame Graph 优先**：旧版 `Timeline` 是上方时间标尺 + 下方连续 frame strip，旧版 `FrameGraphPanel` 是浅灰背景、绿色/橙色/红色 frame bars、目标帧耗时线、选中/hover 覆盖层。Avalonia 版需要用 SkiaSharp 直接绘制这些元素，避免普通 Avalonia chart 或卡片式布局带来的视觉偏差。
2. **颜色沿用旧版 `Colours`**：优先映射 `FrameGraphBackground`、`FrameGraphFrameBar`、`FrameGraphFrameBarWarning`、`FrameGraphFrameBarAlert`、`FrameGraphTargetLine`、`FrameGraphSelectionFill`、`FrameGraphSelectionLine`、`TimelieBackground`、`TimeLineFrameFillColour`、`TimeLineFrameTextColour`、`FrameLine` 等配色。
3. **密度和边界沿用旧版**：顶部 timeline 保持约 50px 高度，其中标尺约 30px、frame strip 约 20px；frame strip 使用深灰填充、白色 frame 边界，空间足够时显示 `Frame: N` 或 `N`。
4. **Profiler 工作区保持工具感**：主窗口背景、侧栏、状态栏使用浅灰和细边框，减少卡片、圆角、渐变和装饰色。SukiUI 控件可以保留，但不应喧宾夺主。
5. **SkiaSharp 作为还原核心**：旧版 GDI+ 绘制逻辑迁移时优先翻译为 SkiaSharp 的坐标、笔刷、裁剪、文本测量和 overlay；Avalonia 控件只负责承载输入和布局。

当前第一步还原范围：

- `FrameTimelineControl` 改为一体化 Skia 工作区：顶部时间标尺、顶部连续帧条、下方 frame graph、目标线、hover/selection marker、旧版配色。
- `MainWindow.axaml` 调整为浅灰工具壳，保留 SukiUI 但去掉深色 dashboard 风格。
- 后续 Threads/Scopes/Counters 也应沿用旧版 `TimeSpanGraph`、`ScopeDataGrid`、`CustomStatsGraph` 的密度、颜色和交互语义。

SukiUI 负责：

- 主窗口 `SukiWindow`。
- 左侧导航。
- 顶部工具栏。
- 状态栏。
- 对话框、toast、InfoBar。
- 设置页、表单、普通按钮、下拉、输入控件。

SkiaSharp 负责：

- frame timeline。
- thread time span graph。
- scope flame/timeline blocks。
- custom stat/counter 曲线。
- core/context switch 只读图。
- 高密度 hover overlay 和 selection overlay。

主界面布局：

```text
┌──────────────────────────────────────────────────────────┐
│ Toolbar: Open | Recent | Search | Capture disabled hint   │
├──────────────┬───────────────────────────────────────────┤
│ Navigation   │ Active analysis workspace                  │
│ - Summary    │  ┌ Timeline / Frame Graph ┐                │
│ - Timeline   │  ├ Threads / Scopes       ┤                │
│ - Scopes     │  ├ Details / Counters     ┤                │
│ - Counters   │  └ Logs / Diagnostics     ┘                │
├──────────────┴───────────────────────────────────────────┤
│ Status: file, frame range, selected frame, load progress   │
└──────────────────────────────────────────────────────────┘
```

第一版采用固定布局加 tabs/split panels，不迁移旧 `docker/` 的拖拽 dock。后续如果确实需要可停靠窗口，再评估 Avalonia dock 库或自研轻量 dock。

## Session 数据模型

Avalonia 客户端不直接把 `Session` 暴露给所有 ViewModel。使用 `SessionDocument` 作为 UI 文档模型：

```csharp
internal sealed class SessionDocument
{
    public string Id { get; }
    public string SourcePath { get; }
    public Session Session { get; }
    public SessionSummary Summary { get; }
    public TimelineSelection Selection { get; }
    public TimelineViewport Viewport { get; }
}
```

`SessionQueryService` 负责把 legacy core 数据转换成 UI-friendly projections：

- `GetFrameSamples(startFrame, endFrame, maxSamples)`
- `GetThreads(frameRange)`
- `GetScopeHotspots(frameRange, sortMode, filter)`
- `GetFrameScopeTree(frameIndex, threadId)`
- `GetCounterList(filter)`
- `GetCounterSamples(counterName, frameRange, maxSamples)`
- `GetSourceLocation(scopeId)`

这样可以避免每个 ViewModel 都直接读取 `Session` 内部结构，也方便之后把查询逻辑迁到独立 core 或 MCP-like 分析服务中。

## Timeline 与选择状态

Profiler UI 的核心状态是 timeline viewport 和 selection。

`TimelineViewport`：

- `StartFrame`
- `EndFrame`
- `StartTime`
- `EndTime`
- `PixelsPerFrame`
- `PixelsPerMillisecond`
- `HorizontalOffset`

`TimelineSelection`：

- `SelectedFrameIndex`
- `SelectedTimeStart`
- `SelectedTimeEnd`
- `SelectedThreadId`
- `SelectedScopeId`
- `HoveredFrameIndex`
- `HoveredScopeId`

所有视图共享同一个 `SessionDocument.Selection`：

- timeline 选中 frame 后，threads/scopes/counters 自动刷新。
- scopes 选中 scope 后，timeline 高亮同名 scope。
- counter hover frame 后，timeline 显示同一 frame marker。

## SkiaSharp 绘制控件设计

所有高密度绘图控件继承统一基类：

```csharp
internal abstract class SkiaCanvasControl : Control
{
    protected abstract void Render(SKCanvas canvas, SKRect bounds, double scale);
    protected virtual void OnPointerMoved(...);
    protected virtual void OnPointerPressed(...);
    protected virtual void OnPointerWheelChanged(...);
}
```

实现原则：

- 绘制数据提前投影为不可变 `RenderModel`，避免 paint 中读取复杂 session。
- paint 只做可视范围内绘制，不遍历完整大 session。
- 大数据曲线使用 downsampling。
- text measurement 和 brush/paint 复用缓存。
- hover/selection overlay 单独绘制，避免整图重算。
- 控件尺寸变化、viewport 变化、selection 变化分别触发不同级别 invalidation。

当前 `FrameTimelineControl` 可以演进为：

- `TimelineCanvas`
- `FrameGraphLayer`
- `ScopeBlockLayer`
- `SelectionLayer`
- `HoverTooltipLayer`

## 主要视图设计

### Summary

用途：

- 展示 session 基础信息。
- 展示 frame count、平均帧耗时、最大帧耗时、预算内帧数、线程数、counter 数。
- 展示文件路径、平台、进程名、录制时间。

实现：

- SukiUI 普通控件。
- 数据来自 `SessionDocument.Summary`。

### Timeline

用途：

- 显示 frame graph。
- 显示可见线程的 scope block。
- 支持缩放、拖动、hover、选中 frame/time range。

实现：

- SkiaSharp 自绘。
- 使用 `TimelineViewport` 控制坐标转换。
- 初期只画 frame graph；后续叠加 thread blocks。

交互：

- 鼠标滚轮：水平缩放。
- Shift + 鼠标滚轮：水平滚动。
- 拖拽空白区域：平移 viewport。
- 点击 frame：选中 frame。
- 拖拽 frame range：选中范围。

### Threads

用途：

- 展示线程列表、线程总耗时、可见性、排序。
- 与 timeline scope block 联动。

实现：

- 第一版用 Avalonia `DataGrid` 或虚拟化列表。
- 对每个线程显示名称、id、frame range 内耗时、scope count。

### Scopes

用途：

- 展示 hotspot scopes。
- 支持按 total time、self time、max frame time、count 排序。
- 支持名称过滤。
- 支持选中 scope 后 timeline 高亮。

实现：

- 第一版使用 SukiUI/DataGrid。
- 数据由 `SessionQueryService.GetScopeHotspots` 生成。
- 后续对大数据表格再改自定义虚拟化表格。

### Scope Detail

用途：

- 展示选中 frame / thread / scope 的层级信息。
- 类似 flame graph 或 call tree。

实现：

- 初期使用树形表格。
- 后续使用 SkiaSharp flame graph。

### Counters / Custom Stats

用途：

- 列出 custom stats。
- 查询指定 counter 在 frame range 内的样本。
- 显示曲线，与 frame selection 联动。

实现：

- 列表使用 SukiUI/DataGrid。
- 曲线使用 SkiaSharp。
- counter sample 限制最大点数并做 downsampling。

### Logs / Diagnostics

用途：

- 显示 session log。
- 显示加载错误、平台降级提示。
- 显示慢帧模式、未归因时间等诊断摘要。

实现：

- 普通 Avalonia text/list 控件。
- 诊断逻辑可复用 `ProfilerStudy.McpServer` 中已有的 `ProfilerDiagnostics` 思路，后续抽到共享 core。

## 平台服务设计

平台差异必须集中到服务层。

```csharp
internal interface ISourceViewerLauncher
{
    Task LaunchAsync(SourceLocation location, CancellationToken cancellationToken);
}

internal interface IPathMapper
{
    string MapToLocalPath(string sourcePath);
}

internal interface IAppPaths
{
    string ConfigDirectory { get; }
    string CacheDirectory { get; }
    string LogDirectory { get; }
}
```

源码跳转策略：

- Windows：
  - 优先 `code --goto file:line`
  - 其次 CLion CLI
  - Visual Studio 工具作为 legacy fallback
- macOS：
  - 优先 `code --goto file:line`
  - CLion 使用命令行 launcher 或 `open -a`
  - 对 Windows 路径通过 source root mapping 转换

## Core 抽离策略

短期：

- Avalonia 直接引用 `ProfilerStudyCore` 的 `net8.0` target。
- 所有 UI 查询通过 `SessionQueryService` 包装。
- 不在 Avalonia ViewModel 中新增对 WinForms/Win32 的依赖。

中期：

- 把 `ProfilerStudy.McpServer` 和 Avalonia 都需要的分析逻辑抽成共享项目，例如 `ProfilerStudy.Analysis`。
- 把 source path mapping、slow frame diagnostics、scope hotspot projection 放入共享项目。

长期：

- 将 `ProfilerStudyCore` 中仍然存在的平台耦合继续下沉或隔离。
- 让 `ProfilerStudy.Avalonia` 只依赖平台无关 core + 平台服务接口。

## 性能设计

目标：

- 小 session：打开后 1 秒内可交互。
- 中型 session：timeline 缩放滚动保持流畅。
- 大型 session：不因为完整绘制或完整表格 materialize 卡死 UI。

措施：

- 加载文件在后台线程执行。
- UI thread 只接收 projection result。
- timeline 按 viewport 查询可见数据。
- frame graph 对高密度数据做 min/max bucket downsampling。
- 表格使用分页或虚拟化。
- Skia render model 缓存到 viewport revision。
- 鼠标 hover overlay 避免触发完整 redraw。

## 错误处理和降级

- 文件读取失败：SukiUI dialog + status bar 显示错误。
- session 格式不支持：显示 save file version 和当前支持版本。
- 源码路径不存在：显示原始路径和 source root mapping 建议。
- 平台不支持的功能：按钮禁用并显示原因。
- long-running load：显示进度或 busy area，允许取消。

## 测试设计

新增测试项目建议：

```text
ProfilerStudy.Avalonia.Tests
```

覆盖：

- `SessionLoader` 能加载 sample session。
- `SessionQueryService` 的 frame summary 与 core 数据一致。
- frame time downsampling 保留 min/max。
- path mapper 的 Windows/macOS 路径转换。
- source launcher 命令构造。
- timeline coordinate conversion。

手工冒烟：

- Windows 启动 Avalonia 客户端。
- macOS 启动 Avalonia 客户端。
- 加载同一 profiler 文件。
- 切换 Summary/Timeline/Scopes/Counters。
- 选中慢帧，scope 表和 counter 曲线联动。
- 源码跳转打开正确文件行。

## 里程碑

### M1：真实 Session 文档模型

交付：

- `SessionDocument`
- `SessionLoader`
- `SessionQueryService`
- 后台加载、取消、错误处理
- Summary 使用真实 session metadata

验收：

- Avalonia 客户端能稳定加载真实 profiler 文件。
- Debug/Release build 通过。
- 至少一个 sample 文件加载后 summary 与 legacy/MCP 输出一致。

### M2：Timeline MVP

交付：

- `TimelineViewport`
- Skia frame graph
- 缩放、滚动、hover、选中 frame
- 与 Summary/Status 联动

验收：

- 选中任意 frame 后状态栏显示 frame index 和 frame time。
- 大量 frame 下滚动缩放不会明显阻塞。

### M3：Threads + Scopes

交付：

- thread 列表
- scope hotspot 表
- frame/range selection 驱动 scope 查询
- scope 搜索和排序

验收：

- 能定位慢帧中的热点 scope。
- 选中 scope 后 timeline 高亮同名 scope。

### M4：Scope Detail + Counters

交付：

- 单帧 scope tree/flame graph
- counter 列表
- counter 曲线
- counter 与 frame selection 联动

验收：

- 能分析单帧内部层级和同帧 counter 状态。

### M5：源码跳转和平台服务

交付：

- VSCode/CLion source launch
- source root mapping
- app paths/settings persistence
- 剪贴板、recent files

验收：

- Windows/macOS 均能从 scope 跳转源码。
- 缺失路径时给出可操作提示。

### M6：发布准备

交付：

- Windows/macOS publish profiles
- app icon / bundle metadata
- smoke test checklist
- 已知降级功能列表

验收：

- 非开发机可运行。
- 用户可完成打开文件、定位慢帧、查热点、跳源码的闭环。

## 实现顺序建议

1. 重构当前 preview 代码为 `Sessions/` 和 `Shell/` 结构。
2. 先实现 `SessionDocument` 和 `SessionQueryService`，停止让 ViewModel 直接操作 `Session`。
3. 将 `FrameTimelineControl` 改为基于 `TimelineViewport` 的 `TimelineCanvas`。
4. 增加 selection state，并让 Summary/Status/Timeline 联动。
5. 实现 Scopes hotspot 表。
6. 实现单帧 detail。
7. 实现 counters。
8. 最后补平台服务和发布。

## 风险和控制

| 风险 | 影响 | 控制 |
| --- | --- | --- |
| 旧 core 内部结构不适合 UI 查询 | 查询慢、ViewModel 复杂 | 用 `SessionQueryService` 做投影和缓存 |
| Skia 绘制性能不足 | timeline 卡顿 | viewport 裁剪、downsampling、render model 缓存 |
| DataGrid 大数据卡顿 | scopes/counters 不可用 | 先限制 top/filter，再做虚拟化 |
| WinForms 行为无法完全复刻 | 用户迁移成本 | 第一版固定分析闭环，后续按高频需求补齐 |
| macOS 源码路径映射复杂 | source jump 不可靠 | 独立 `IPathMapper` + 用例测试 |
| SukiUI 版本变动 | 样式/API 风险 | SukiUI 只用于 shell，核心绘制不依赖其内部控件 |

## 成功标准

Avalonia 版本达到可试用状态时，应满足：

- 可以在 Windows/macOS 原生启动。
- 可以打开真实 profiler session。
- 可以在 timeline 中定位慢帧。
- 可以查看慢帧相关 scope hotspots。
- 可以查看单帧 scope 详情。
- 可以查看 custom stat/counter 曲线。
- 可以跳转源码。
- 同一 session 的关键统计结果与 legacy/MCP 分析结果一致。
