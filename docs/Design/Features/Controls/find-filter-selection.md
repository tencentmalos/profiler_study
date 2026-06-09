# 查找、筛选、选区与 Highlight

## 范围

本文维护 Find 控件、scope 搜索、thread filter、selected range、highlight scope/time span 的 WinForms/Avalonia 对齐。

## WinForms 实现

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

## Avalonia 实现

主要文件：

- `ProfilerStudy.Avalonia/App/Shell/MainWindow.axaml`
- `ProfilerStudy.Avalonia/App/Shell/MainWindowViewModel.cs`
- `ProfilerStudy.Avalonia/Timeline/TimelineSelection.cs`
- `ProfilerStudy.Avalonia/Features/Scopes/ScopeHotspotAnalyzer.cs`
- `ProfilerStudy.Avalonia/Features/TimelineScope/ThreadTimelineScopeAnalyzer.cs`

## 对齐要求

- Find 文本由 shell 控件收集，具体匹配由当前 view/feature 决定。
- Next/Prev scope 语义与当前 visible range 和 selected scope 一致。
- Thread filter/hide/collapse 只影响 UI 显示，不删除 Core 数据。
- Highlight 与 selection 分离，避免清除 selection 时误清 highlight。
- 空匹配时两个 UI 都应给出非破坏性反馈。

## 修改流程

改查找/筛选前：

1. 更新本文。
2. 明确匹配对象是 scope name、thread name、source file、counter 还是 log。
3. 检查 WinForms 和 Avalonia 的 keyboard shortcut。
4. 验证大小写、空字符串、多匹配、无匹配。
