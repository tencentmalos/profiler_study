# Features 设计文档入口

## 目的

`docs/Design/Features` 是功能级设计文档目录。这里的文档不是代码索引，也不是实现流水账；它们用于约束后续迭代：

- 改 Core 前，先确认数据模型、线程模型、文件/协议兼容边界。
- 改 Legacy WinForms 或 Avalonia shell 前，先确认 shell 职责和状态归属。
- 改基础控件前，先确认 WinForms 现有行为、Avalonia 对齐目标、数据流和验收点。
- 新增 Perfetto/Tracy 等 trace 能力时，先明确是 Core importer/query、UI 控件、MCP tool，还是三者共享的能力。

## 文档分层

### Core

- `core-foundation.md`：ProfilerStudy Core 的 session、packet、transport、model、analysis 基础能力。

### UI Shell

- `legacy-winforms-shell.md`：WinForms `MainForm`、菜单、toolbar、docking、session 生命周期。
- `legacy-winforms-profiler-views.md`：WinForms 中 Threads/Scopes/Cores/CustomStats 等分析视图的组织。
- `avalonia-shell.md`：Avalonia `MainWindow`、native menu、toolbar、overlay、status/output shell、ViewModel 边界。

### 控件族

控件族文档在 `Controls/` 下。每份文档同时维护 Legacy WinForms 与 Avalonia 的对应实现，用于做行为和视觉对齐。

## 修改约定

任何子功能调整都先改设计文档，再落地代码。

设计文档更新至少要说明：

- 当前行为是否以 WinForms 为权威，还是 Avalonia 已经成为新主线。
- 本次改动影响行为、视觉、交互、数据流、性能还是兼容性。
- 是否需要同步 MCP 或 Core query。
- 验证方式：构建、self-test、样本文件、手工 UI 路径、截图或性能检查。

如果无法确定改动应归属哪个文档，优先更新最接近的控件族文档，并在文档中记录待拆分的边界问题。
