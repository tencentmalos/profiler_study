# 控件族对齐索引

## 定位

`Controls/` 下每份文档维护一个控件族，而不是维护一个项目或一个文件夹。这样做的目的是让后续 Avalonia 与 WinForms 对齐时，可以围绕同一个用户操作或视觉区域讨论，而不是在两套 UI 的目录之间来回跳。

每份控件族文档都应回答四个问题：

- WinForms 当前权威行为是什么？
- Avalonia 现在实现到什么程度，和 WinForms 的差异在哪里？
- 控件接收什么数据、产生什么状态变化，不能做什么？
- 改动完成后如何验证？

## 控件族文档

- `toolbar-buttons.md`：主工具栏按钮、toggle、菜单命令入口。
- `timeline-frame-graph.md`：时间线、frame strip、frame graph、帧选择/hover。
- `scope-thread-lanes.md`：线程 lane、scope block、flame chart、scope 着色。
- `session-navigation-scroll.md`：session scrollbar、viewport、track end、pan/zoom。
- `find-filter-selection.md`：Find、scope/thread filter、选区、highlight。
- `stats-counters-plots.md`：custom stats、counter、曲线图、plot 控件。
- `core-context-switch.md`：CPU/core 轨道、context switch、Android context switch 展示。
- `output-overlay-dialogs.md`：output window、hover、overlay、基础对话框。

## 文档模板

新增控件族文档时使用这个结构：

```text
# 控件族名称

## 用户场景
这个控件族解决什么 profiler 操作。

## 当前实现映射
WinForms 文件、Avalonia 文件、Core 数据来源。

## 行为契约
选择、hover、滚动、过滤、状态写入等必须保持的语义。

## WinForms 与 Avalonia 对齐
哪些行为已经对齐，哪些是差异，哪些明确不需要对齐。

## 数据流与边界
控件输入、输出、状态归属；哪些逻辑不能放进控件。

## 验收清单
构建、样本、手工操作、视觉/性能检查。
```

## 修改规则

修改基础控件前，先找到对应控件族文档并更新：

1. 写清楚 WinForms 当前行为。
2. 写清楚 Avalonia 对应实现或缺口。
3. 说明本次对齐目标是行为、视觉、交互、数据流中的哪一类。
4. 写清楚验收方式。
5. 再落地代码。

如果一个改动横跨多个控件族，应同时更新多个文档，而不是只改 Shell 文档。
