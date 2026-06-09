# 查找、筛选、选区与 Highlight

## 用户场景

用户通过 Find 快速定位 scope/thread/source/counter，通过 thread filter 减少噪声，通过 selection/highlight 在多个视图之间保持当前分析上下文。

## 当前实现映射

### WinForms

主要文件：

- `ProfilerStudy/LegacyWinForms/FindControl.cs`
- `ProfilerStudy/LegacyWinForms/FindControlTextChangedHandler.cs`
- `ProfilerStudy/LegacyWinForms/FindControlGotoNextHandler.cs`
- `ProfilerStudy/LegacyWinForms/FindControlGotoPrevHandler.cs`
- `ProfilerStudy/LegacyWinForms/ThreadFilter.cs`
- `ProfilerStudy/LegacyWinForms/ThreadFilterForm.cs`
- `ProfilerStudy/LegacyWinForms/ThreadFilterRow.cs`
- `ProfilerStudy/LegacyWinForms/SelectedRangeChangedHandler.cs`
- `ProfilerStudy/LegacyWinForms/HighlightedTimeSpanCountChangedHandler.cs`
- `ProfilerStudy/LegacyWinForms/HighlightScopeHandler.cs`

### Avalonia

主要文件：

- `ProfilerStudy.Avalonia/App/Shell/MainWindow.axaml`
- `ProfilerStudy.Avalonia/App/Shell/MainWindowViewModel.cs`
- `ProfilerStudy.Avalonia/Timeline/TimelineSelection.cs`
- `ProfilerStudy.Avalonia/Features/Scopes/ScopeHotspotAnalyzer.cs`
- `ProfilerStudy.Avalonia/Features/TimelineScope/ThreadTimelineScopeAnalyzer.cs`

## 行为契约

- Find 文本由 shell 控件收集，具体匹配由当前 view/feature 决定。
- Next/Prev scope 语义与当前 visible range 和 selected scope 一致。
- Thread filter/hide/collapse 只影响 UI 显示，不删除 Core 数据。
- Highlight 与 selection 分离，避免清除 selection 时误清 highlight。
- 空匹配时两个 UI 都应给出非破坏性反馈。

## 对齐状态与目标

- WinForms 的 FindControl 是 toolbar 内嵌控件，行为由 active view 解释。
- Avalonia 当前以 shell find box + view model command 为主，应继续保持“输入在 shell，匹配在 feature”的分层。
- 未来如果支持多类型搜索，应显式记录搜索范围，而不是让不同 view 暗自解释同一文本。

## 数据流与状态归属

Find text、thread filter、hidden/collapsed threads、highlight、selection 都是 UI 状态。它们可以影响查询结果和绘制结果，但不能写回 Core session。

## 验收清单

改查找/筛选前：

1. 更新本文。
2. 明确匹配对象是 scope name、thread name、source file、counter 还是 log。
3. 检查 WinForms 和 Avalonia 的 keyboard shortcut。
4. 验证大小写、空字符串、多匹配、无匹配。
5. 验证过滤线程后 selection/highlight 不指向不可见或错误对象。
