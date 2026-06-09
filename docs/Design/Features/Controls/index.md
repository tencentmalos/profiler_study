# 基础控件对齐索引

## 目标

基础控件按“控件族”独立维护文档。每个文档同时记录 Legacy WinForms 与 Avalonia 的对应实现、行为语义、视觉对齐要求和修改流程，方便后续逐步做 Avalonia 与 WinForms 对齐。

## 控件族文档

- `toolbar-buttons.md`：主工具栏按钮、toggle、菜单命令入口。
- `timeline-frame-graph.md`：时间线、frame strip、frame graph、帧选择/hover。
- `scope-thread-lanes.md`：线程 lane、scope block、flame chart、scope 着色。
- `session-navigation-scroll.md`：session scrollbar、viewport、track end、pan/zoom。
- `find-filter-selection.md`：Find、scope/thread filter、选区、highlight。
- `stats-counters-plots.md`：custom stats、counter、曲线图、plot 控件。
- `core-context-switch.md`：CPU/core 轨道、context switch、Android context switch 展示。
- `output-overlay-dialogs.md`：output window、hover、overlay、基础对话框。

## 修改规则

修改基础控件前，先找到对应控件族文档并更新：

1. 写清楚 WinForms 当前行为。
2. 写清楚 Avalonia 对应实现或缺口。
3. 说明本次对齐目标是行为、视觉、交互、数据流中的哪一类。
4. 再落地代码。

如果一个改动横跨多个控件族，应同时更新多个文档，而不是只改 Shell 文档。
