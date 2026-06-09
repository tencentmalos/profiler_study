# 时间线与 Frame Graph

## 用户场景

用户通过时间线观察帧耗时、定位 spike、选择当前帧、缩放/滚动可视范围，并把后续 Threads/Scopes/Counters 视图同步到同一时间上下文。

## 当前实现映射

### WinForms

主要文件：

- `ProfilerStudy/LegacyWinForms/Timeline.cs`
- `ProfilerStudy/LegacyWinForms/FrameGraphPanel.cs`
- `ProfilerStudy/LegacyWinForms/FrameGraphYAxis.cs`
- `ProfilerStudy/LegacyWinForms/FrameGraphYAxisScaleChangedHandler.cs`
- `ProfilerStudy/LegacyWinForms/FrameGraphYAxisTargetMSChangedHandler.cs`
- `ProfilerStudy/LegacyWinForms/FrameInfoPanel.cs`
- `ProfilerStudy/LegacyWinForms/TimeRange.cs`
- `ProfilerStudy/LegacyWinForms/VisibleRangeChangedHandler.cs`

数据来源：

- `Session.GetFrame`
- `Session.GetFrameStartEndTime`
- `Session.GetFrameIndex`
- `Session.FrameXToTime`
- `Session.TimeToFrameX`
- `CoreUtils.GetFrameTimeCategory`

### Avalonia

主要文件：

- `ProfilerStudy.Avalonia/Controls/FrameTimelineControl.cs`
- `ProfilerStudy.Avalonia/Controls/FrameTimelineRenderModel.cs`
- `ProfilerStudy.Avalonia/Models/FrameSample.cs`
- `ProfilerStudy.Avalonia/Timeline/TimelineSelection.cs`
- `ProfilerStudy.Avalonia/Timeline/TimelineViewport.cs`
- `ProfilerStudy.Avalonia/Features/Sessions/SessionQueryService.cs`
- `ProfilerStudy.Avalonia/App/Shell/MainWindowViewModel.cs`
- `third_party/ScottPlot/`：Avalonia plotting 的源码级定制入口。

`FrameTimelineControl` 应只接收已经准备好的 `FrameSample`、viewport、selection，不直接读取文件或遍历完整 session。

## 行为契约

- 帧颜色分类与 WinForms 保持一致：正常、warning、alert、target line。
- Hover、selected frame、visible range 的语义一致。
- `Home` / `End` / prev spike / next spike / max frame 行为一致。
- 目标帧耗时来自 settings/session 语义，不由控件硬编码。
- 大 session 下 Avalonia 可以采样绘制，但采样不能改变选择和导出语义。

## 对齐状态与目标

- WinForms 的 `Timeline` + `FrameGraphPanel` 是视觉参考：上方 timeline、frame strip、目标线、warning/alert 颜色。
- Avalonia 的 `FrameTimelineControl` 是对齐承载点，应逐步合并 frame strip 和 graph 的用户语义。
- Avalonia 使用 `third_party/ScottPlot/` 中的源码级 ScottPlot，而不是 NuGet `ScottPlot.Avalonia`。如果现有 ScottPlot API 无法表达逐帧 frame strip、固定像素高度、hover/selection 覆盖层，应优先在子仓做有边界的定制。
- 当前可接受差异：绘制技术不同，Avalonia 可用不同图标/字体；不可接受差异：同一帧在两个 UI 中分类不同、选中范围不同。

## 本轮 Avalonia 对齐方案

- 重点不是复刻 WinForms 外观，而是让 timeline-frame-graph 自成闭环：可见帧、帧分类、hover、selection、viewport、下游 scope/counter/timeline 刷新必须使用同一套帧索引语义。
- `FrameTimelineRenderModel` 是非可视闭环核心，负责从 `FrameSample`、viewport、target frame ms 生成可见 frame item；绘制和命中都应依赖它，避免 ScottPlot 调用与交互逻辑各自计算一套帧范围。
- `FrameTimelineControl` 不应把帧耗时只表达为连续折线；可见范围内的每个 `FrameSample` 应以独立 frame item 表达，避免折线插值弱化逐帧定位与命中语义。
- 全局 overview 的 session scrollbar 也应接入同一组 `FrameSample` 和 target frame ms，按 WinForms `SessionScrollBar.DrawFrameTimes` 的语义绘制 frame strip；否则用户只能在局部 graph 中看到逐帧分类，无法在全局范围内定位 spike。
- frame graph 的主绘制路径应采用 Avalonia 原生矩形绘制，而不是通用 plot chart；这样可以直接表达 WinForms `FrameGraphPanel` 的 frame rect、target line、selected/hover band、像素命中与拖拽滚动语义。
- 像素布局必须同时服务绘制、命中和滚轮缩放锚点；不能让 bar rect 使用一套 frame-to-x 映射，而 pointer/zoom 使用另一套近似映射。
- 帧数据重绘与交互覆盖层刷新需要分离：`Samples`、`TargetFrameMs`、`Viewport` 变化可以重建 bars、target line、坐标轴；`SelectedFrameIndex`、`HoveredFrameIndex` 变化只应替换 selected/hover overlay，不应重建所有 frame items。
- `FrameTimelineRenderModel` 构建可见 items 时应按 viewport 起点定位到首个可见 sample，不能在长 session 下每次从 samples 开头线性扫描到可见范围。
- 当可见帧数大于可用像素宽度时，frame graph 的视觉绘制可以按像素列聚合，但每列必须保留该列覆盖范围内最需要用户关注的帧：优先保留分类最严重、同类中耗时最高的 sample。聚合只影响绘制 bars 数量和颜色表达，不能改变 `FrameTimelineRenderModel` 中的真实 frame items。
- frame item 颜色按 WinForms `FrameGraphPanel.GetFrameBrush` 的语义分组：低于目标帧耗时为正常，达到目标帧耗时为 warning，达到两倍目标帧耗时为 alert。
- frame bar 高度应表达 frame duration 在当前显示 scale 下的比例，不能因为 target frame ms 变化而整体重新归一化；target frame ms 只影响颜色分类和 target line 位置。Avalonia 暂未实现 WinForms 可拖拽 y-axis scale 时，渲染模型至少要保证同一组可见帧在不同 target 下高度稳定。
- target line 保持横向参考线；selected frame 与 hovered frame 应明确绑定到整帧索引，保证状态栏、selected frame scopes、selected frame counters 和 thread timeline 能跟随变化。
- `TimelineSelection` 应以 frame index 和 frame duration 成对更新为默认入口；外部控件不应只写 index 或只写 duration，避免状态栏、ProfilerStats marker、selected frame scopes 使用不一致的选择状态。
- 本轮不新增 WinForms 的 selected range、time span overlay、event label；这些依赖额外 selection/event 状态，后续应在状态模型扩展后再补齐。
- 大范围显示仍允许绘制采样或像素列聚合后的 `FrameSample`，但鼠标命中和选择必须使用样本的真实 `Index`，不能用绘制数组下标或聚合列号替代帧号。

## 数据流与状态归属

```text
Session
  -> SessionQueryService / FrameSample
  -> FrameTimelineControl
  -> TimelineSelection / TimelineViewport
  -> Threads/Scopes/Counters view refresh
```

控件可以改变 selection/viewport，不能修改 session frame 数据。

## 验收清单

改 timeline/frame graph 前：

1. 更新本文。
2. 明确改动影响的是绘制、交互、帧分类、viewport 还是 selection。
3. 检查 WinForms 和 Avalonia 是否都需要同步。
4. 验证 loaded session、live session、空 session 三种状态。
5. 验证长 session 下滚动/缩放不卡顿，选中帧精确。
6. 如果修改 ScottPlot 子仓，记录子仓提交并验证 Avalonia 项目使用的是源码引用。
