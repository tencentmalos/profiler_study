# Legacy WinForms 分析视图控件

## 范围

本文记录 legacy WinForms 中承载 profiler 分析语义的主要视图和绘图控件。基础按钮、搜索、滚动、hover 等控件按控件族维护在 `Controls/`。

## Session 容器

### `MainSessionView`

每个 loaded/live `Session` 对应一个 `MainSessionView`，并作为 `DockManager` 的 MDI child。

职责：

- 管理当前 session 的主 tabs/views。
- 保存/恢复 `SessionViewSaveData`。
- 向 MainForm 报告 active view 变化。
- 提供 `TrackEnd`、`GotoStart`、`GotoEnd`、`ActivateTab` 等 session 级 UI 操作。

### `SessionView`

具体 session view 的协调基类/容器。

规则：

- View 可以直接持有 `Session`，但不要绕过 Core 锁枚举可变集合。
- View 状态保存到 `SessionViewSaveData`，不要散落到全局 setting。

## Threads View

`ThreadsView` 是 legacy 主分析工作区，组合多个 profiler 面板：

- Info panel。
- Frame graph。
- Thread scope graph。
- CPU/core graph。
- Custom stats graph。
- Data grid。

相关控件：

- `FrameGraphPanel` / `FrameGraphYAxis`。
- `TimeSpanGraph` / `TimeSpanGraphView` / `ThreadTimeSpanGraph`。
- `ThreadRowPanel`。
- `CoreGraph` / `CoreGraphPanel` / `CorePanel`。
- `CustomStatsGraph`。
- `ScopeDataGrid`。

修改规则：

- 面板显隐必须同步 MainForm toolbar/menu checked 状态。
- 线程 hide/show/collapse/order 只影响 UI 显示，不删除 Core 数据。
- 时间范围、selection、track end 的联动要保持一致。

## Frame Graph

`FrameGraphPanel` 和 `FrameGraphYAxis` 显示帧耗时、目标线、warning/alert 颜色和选中/hover 状态。

规则：

- 帧分类使用 `CoreUtils.GetFrameTimeCategory` 和 settings target frame MS。
- 不在 paint 中做昂贵 session 全量统计。
- CSV 导出和图上显示应使用同一组 frame 数据语义。

## Scope / TimeSpan Graph

`TimeSpanGraph`、`TimeSpanGraphView`、`ThreadTimeSpanGraph` 负责绘制 scope 树和线程 lane。

规则：

- 绘制数据来自 `Session` 的 `TimeSpanList` 和 `TimeSpanInfoSet`。
- Scope 着色模式由 `ScopeColourMode` 控制：按线程或按 scope。
- 选中 scope、highlight scope、find next/prev 必须使用同一时间范围语义。
- callstack/source info 展示不能假设所有 scope 都有源码信息。

## Cores View

`CoresView` 与 `CoreGraph*` 控件展示 CPU/core timeline 和 context switch 信息。

规则：

- context switch 数据可能来自实时记录，也可能来自 Android context switch 文件。
- 无 context switch 数据时要保留现有 warning/降级行为。
- core/thread 关系展示不应修改 session thread order。

## Scopes View

`ScopesView` 以 scope 统计和 grid 浏览为主。

规则：

- Scope 统计来自 Core `ScopeSessionStats` / `TimeSpanFrameStats`。
- 排序、筛选、选择是 UI 行为，不应写回 Core。
- 大 session 下避免每次 paint 全量重算。

## Custom Stats View

`CustomStatsView`、`CustomStatsGraph`、`CustomStatSelector` 展示 custom stat/counter。

规则：

- 支持 per-frame、time、accumulated time 等 x 轴模式。
- graph/unit/colour 来自 session/settings。
- 颜色修改需要通过 session/settings 事件刷新相关视图。

## Log / Info / Session Data Grid

- `LogView` 展示 session log packet。
- `InfoPanel` / `FrameInfoPanel` 展示选中帧、session、scope 信息。
- `SessionInfoDataGrid`、`ScopeDataGrid` 展示结构化表格。

规则：

- 表格行模型应与当前 selection/range 同步。
- 显示文本可以是 UI 专用格式，但计算逻辑优先复用 Core helper。

## 修改流程

改分析视图前：

1. 更新本文对应 section。
2. 确认是否需要同步 Avalonia 对应文档和控件行为。
3. 检查 `MainForm` 菜单/toolbar checked 状态是否受影响。
4. 对大 session 场景评估 paint 和重算成本。
5. 在 Windows 环境手工验证受影响视图。
