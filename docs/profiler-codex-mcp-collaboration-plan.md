# Profiler 与 Codex 协同改造方案

## 背景

当前 profiler 是一个 Visual Studio 2022 / .NET Framework 4.7.2 的 WinForms 桌面应用。主要结构如下：

- `ProfilerStudy/`：WinForms UI、工具栏、视图、对话框、文件操作和用户交互。
- `ProfilerStudyCore/`：FramePro session、packet、frame、thread、scope、custom stat、文件读写和统计逻辑。
- `CanvasDataGrid/`、`ProfilerCanvas/`、`docker/`：WinForms 相关控件和布局基础设施。
- `docs/framepro-android-unix-socket.md`：当前 Android fixed `localfilesystem` socket 连接链路记录。

如果目标是让 Codex 更好地参与 profiling 分析、自动排障、生成报告、辅助定位性能问题，最合适的方向不是直接把 WinForms UI 改成 AI UI，而是在现有核心数据层旁边增加一个 **MCP 服务层**。MCP 官方协议以 JSON-RPC 为基础，服务器可以暴露 `Resources`、`Tools` 和 `Prompts`，其中工具用于让模型调用外部系统，资源用于给模型读取上下文数据。参考：

- MCP Base Protocol: https://modelcontextprotocol.io/specification/2025-03-26/basic/index
- MCP Resources: https://modelcontextprotocol.io/specification/2025-06-18/server/resources
- MCP Tools: https://modelcontextprotocol.io/specification/2025-06-18/server/tools

## 目标

让 Codex 能够围绕 profiler session 做这些事情：

- 读取当前或指定的 `.profiler` / recording 文件摘要。
- 查询 frame、thread、scope、custom stat、event、wait/context switch 等结构化数据。
- 自动找出慢帧、尖峰、异常 scope、线程阻塞和 custom stat 异常。
- 生成可复制的性能分析报告。
- 在必要时驱动 profiler 执行受控操作，例如打开文件、连接 Android、导出 CSV。
- 将“用户看到的 UI 状态”和“Codex 可读的分析上下文”对齐。

非目标：

- 不在第一阶段重写 WinForms UI。
- 不让 MCP 工具直接执行任意 shell 命令。
- 不把所有 session 原始数据一次性塞给模型。
- 不让 AI 默认修改 profiler 文件或设置。

## 推荐总体架构

推荐采用 **Sidecar MCP Server + Core Facade** 方案。

```text
Codex / MCP Client
        |
        | JSON-RPC over stdio / local process
        v
ProfilerStudy.McpServer
        |
        | stable DTO / query API
        v
ProfilerStudyCore.AnalysisFacade
        |
        | existing Session / Frame / Thread / Stat logic
        v
ProfilerStudyCore

ProfilerStudy WinForms UI
        |
        | optional local IPC bridge, later phase
        v
ProfilerStudy.McpServer
```

### 为什么推荐 Sidecar

Sidecar MCP Server 是一个独立进程，优先使用 `stdio` transport，适合 Codex 启动和管理。它可以引用 `ProfilerStudyCore`，但不依赖 WinForms UI。这比把 MCP server 嵌进 WinForms 更稳妥：

- 不影响现有 UI 线程和窗口生命周期。
- MCP 崩溃不会拖垮 profiler UI。
- 更容易做命令行测试和自动化回归。
- 后续可同时服务 WinForms、CLI、Avalonia 或 CI 分析。

## 分层设计

### 1. `ProfilerStudyCore.AnalysisFacade`

新增一个薄封装层，避免 MCP 直接调用 `Session` 的复杂内部 API。

职责：

- 加载 session 文件。
- 管理 session handle。
- 提供只读查询接口。
- 将 legacy 类型转换为稳定 DTO。
- 做分页、采样、范围限制和单位换算。

建议命名：

```text
ProfilerStudyCore/FramePro/Analysis/
  ProfilerAnalysisService.cs
  ProfilerSessionHandle.cs
  ProfilerSummaryDto.cs
  FrameQueryDto.cs
  ScopeQueryDto.cs
  ThreadQueryDto.cs
  CustomStatQueryDto.cs
```

第一阶段不要直接暴露 `Frame`、`TimeSpan`、`CustomStatSessionData` 这类内部对象，避免把 legacy 数据结构锁死成外部 API。

### 2. `ProfilerStudy.McpServer`

新增一个控制台项目，目标优先建议：

- 短期：`net472` 或 `net8.0-windows`，取决于 MCP SDK 兼容性和现有 core 引用难度。
- 中期：将纯分析 core 逐步迁到 `netstandard2.0` / `net8.0` 兼容层，MCP server 使用现代 .NET。

职责：

- 实现 MCP server lifecycle。
- 注册 resources、tools、prompts。
- 做输入校验、权限限制、错误映射。
- 输出结构化 JSON 和精简文本摘要。

### 3. WinForms 可选桥接

第二阶段再考虑让运行中的 WinForms 暴露当前 session 状态。桥接方式可以是：

- 命名管道：WinForms 作为本地状态 provider。
- 本地 TCP loopback：便于调试，但要限制只监听 `127.0.0.1`。
- 文件快照：UI 将当前 selection / active session 写到临时 JSON，MCP server 读取。

第一阶段建议先支持“从文件加载分析”，不要绑定 UI 状态。

## MCP 能力设计

### Resources

Resources 适合暴露可读上下文，URI 设计建议使用自定义 scheme：

| URI | 内容 |
| --- | --- |
| `framepro://sessions` | 当前 MCP server 已加载的 session 列表 |
| `framepro://session/{id}/summary` | session 摘要、帧数、线程数、时间范围 |
| `framepro://session/{id}/threads` | 线程列表和线程名 |
| `framepro://session/{id}/frames?start=0&count=120` | 分页 frame 摘要 |
| `framepro://session/{id}/custom-stats` | custom stat 名称、单位、类型 |
| `framepro://session/{id}/events?start_time=...&end_time=...` | 指定时间范围内事件 |
| `framepro://session/{id}/hotspots?top=50` | 预计算热点摘要 |

Resources 应该默认返回精简 JSON。大数据必须分页或采样，避免模型上下文被原始 trace 撑爆。

### Tools

Tools 适合执行明确动作。建议第一阶段只做只读和低风险工具：

| Tool | 输入 | 输出 | 风险 |
| --- | --- | --- | --- |
| `load_session_file` | `path` | `session_id`、摘要 | 读文件 |
| `close_session` | `session_id` | 状态 | 低 |
| `get_session_summary` | `session_id` | 结构化摘要 | 低 |
| `find_slow_frames` | `session_id`、阈值、top | 慢帧列表 | 低 |
| `find_scope_hotspots` | `session_id`、时间范围、top | scope 排名 | 低 |
| `find_thread_stalls` | `session_id`、阈值 | stall 候选 | 低 |
| `query_time_range` | `session_id`、start/end | frames、threads、events 摘要 | 低 |
| `query_custom_stat` | `session_id`、name、范围 | 曲线采样 | 低 |
| `export_analysis_report` | `session_id`、格式 | markdown/json 文件路径 | 写文件 |

第二阶段再考虑操作类工具：

| Tool | 说明 | 额外保护 |
| --- | --- | --- |
| `connect_android_debug` | 复用当前 fixed socket 连接逻辑 | 用户确认 |
| `connect_android_release` | 复用当前 fixed socket 连接逻辑 | 用户确认 |
| `export_frame_graph_csv` | 调用现有 `WriteFrameGraphToCSV` | 输出路径白名单 |
| `open_source_location` | 跳转到源码 | 用户确认 |

不要提供 `run_adb`、`run_command`、`delete_file` 这类泛化工具。

### Prompts

Prompts 用于把常见分析流程标准化：

| Prompt | 用途 |
| --- | --- |
| `summarize_session` | 生成 session 总览 |
| `investigate_frame_spike` | 给定 frame index，分析尖峰来源 |
| `compare_two_sessions` | 对比两个 session 的 frame / scope / stat 差异 |
| `android_connection_diagnosis` | 分析 Android 连接日志和 forward 状态 |
| `prepare_perf_bug_report` | 输出可提交 issue / PR 的性能报告 |

这些 prompt 应要求模型先调用查询工具，不要基于猜测直接下结论。

## 数据模型建议

### Session Summary

```json
{
  "sessionId": "s1",
  "filename": "capture.profiler",
  "frameCount": 1234,
  "threadCount": 12,
  "timerFrequency": 1000000,
  "firstFrameTime": 100000,
  "lastFrameEndTime": 200000,
  "averageFrameMs": 16.7,
  "maxFrameMs": 48.2,
  "framesInBudget": 1150,
  "platform": "Android"
}
```

### Slow Frame

```json
{
  "frameIndex": 321,
  "startTime": 123456789,
  "endTime": 123505000,
  "durationMs": 48.2,
  "topScopes": [
    {
      "name": "Renderer::DrawFrame",
      "threadId": 42,
      "durationMs": 31.4,
      "count": 1
    }
  ]
}
```

### Scope Hotspot

```json
{
  "name": "VideoCore::Submit",
  "totalMs": 512.3,
  "averageMs": 1.8,
  "maxMs": 18.4,
  "count": 284,
  "threads": ["MainThread", "RenderThread"]
}
```

## 安全边界

MCP 工具会被模型自动发现和调用，因此必须按最小权限设计。

必须做：

- 所有文件路径必须限制在用户显式允许的 roots 内。
- 写文件工具默认只允许输出到工作区或用户指定目录。
- Android connect、source jump、导出文件等操作需要用户确认。
- 所有 tool input 使用 JSON Schema 校验。
- 大查询必须有 `limit`、`start`、`count`、`time_range`。
- tool 输出中不要包含任意环境变量、用户目录扫描结果或无关文件内容。
- 错误输出要结构化，不把完整异常堆栈默认发给模型。

建议记录审计日志：

```text
timestamp, client, tool, arguments_summary, result_status, duration_ms
```

## 分阶段实施计划

### Phase 0：API 盘点和 DTO 边界

目标：不改 UI，只梳理可安全暴露的数据。

任务：

- 列出 `Session` 中可复用查询方法。
- 定义 `ProfilerAnalysisService` 和 DTO。
- 给 `.profiler` 样本文件准备最小回归测试。
- 明确时间单位、frame index、thread id、string id 的外部表示。

验收：

- 能从文件加载 session。
- 能输出 session summary JSON。
- 不依赖 WinForms。

### Phase 1：只读 MCP Server

目标：Codex 可以读取和分析 profiler 文件。

任务：

- 新增 `ProfilerStudy.McpServer`。
- 实现 `load_session_file`、`get_session_summary`、`find_slow_frames`。
- 暴露 `framepro://session/{id}/summary` 和 threads / frames resources。
- 添加 path allowlist。

验收：

- Codex 可以加载指定 profiler 文件并生成摘要。
- 大 session 查询不会一次返回全量数据。
- 普通 WinForms 构建不受影响。

### Phase 2：分析工具扩展

目标：让 Codex 能做真实性能定位。

任务：

- 实现 scope hotspot、thread stall、custom stat 查询。
- 实现 `investigate_frame_spike` prompt。
- 实现 markdown report 导出。
- 增加结果排序、阈值、分页和采样。

验收：

- 给定一个慢帧，Codex 能输出主要耗时 scope、线程、相关事件和 custom stat。
- 生成的报告可直接贴到 issue / PR。

### Phase 3：与运行中 UI 协同

目标：Codex 能理解用户当前正在看的 session 和选择范围。

任务：

- WinForms 增加可选本地状态 bridge。
- 暴露 active session、active frame、selection time range。
- MCP server 增加 `get_active_ui_context`。
- 支持从 UI 当前选择生成分析报告。

验收：

- 用户在 UI 里选中一个范围后，Codex 能读取该范围并分析。
- UI bridge 可关闭，默认不暴露敏感数据。

### Phase 4：受控操作工具

目标：Codex 能辅助执行低风险操作。

任务：

- `connect_android_debug` / `connect_android_release`。
- `export_frame_graph_csv`。
- `open_source_location`。
- 操作前确认和审计日志。

验收：

- 所有写操作或外部进程操作都有明确确认。
- 工具不能变成任意 shell 执行通道。

## 推荐优先级

推荐先做 Phase 0 + Phase 1，理由：

- 价值高：Codex 很快能读 session、找慢帧、生成摘要。
- 风险低：不触碰现有 WinForms UI 交互。
- 可测试：核心查询可以用样本文件回归。
- 可扩展：后续 UI bridge 和 Android 操作可以自然接上。

不建议第一步就做“完整 MCP 化 WinForms UI”。那会把 UI 状态、线程模型、MCP lifecycle 和分析能力混在一起，风险高，也不利于后续跨平台演进。

## 代码组织建议

```text
ProfilerStudyCore/
  FramePro/
    Analysis/
      ProfilerAnalysisService.cs
      ProfilerAnalysisOptions.cs
      Dtos/

ProfilerStudy.McpServer/
  ProfilerStudy.McpServer.csproj
  Program.cs
  McpTools/
    SessionTools.cs
    AnalysisTools.cs
    ReportTools.cs
  McpResources/
    SessionResources.cs
  McpPrompts/
    AnalysisPrompts.cs
  Security/
    PathAllowlist.cs
    ToolAuditLog.cs

ProfilerStudy.McpServer.Tests/
  SessionLoadingTests.cs
  SlowFrameAnalysisTests.cs
  PathAllowlistTests.cs
```

## 关键设计原则

- **Core first**：MCP 只消费稳定 core API，不直接抓 UI 控件。
- **Read-only first**：先让 Codex 看懂数据，再允许它触发动作。
- **Small results**：所有工具默认返回摘要，详细数据通过分页 resource 查询。
- **Explicit handles**：工具返回 `session_id`，后续查询都基于 handle。
- **No shell passthrough**：任何外部操作都必须是明确命名的窄工具。
- **Human in the loop**：连接设备、导出文件、打开源码等操作需要用户确认。

## 未决问题

1. MCP server 的目标运行时选择：继续贴近 `net472`，还是为 MCP 选择现代 .NET 并逐步拆 core。
2. 第一版是否只支持加载文件，还是也要读取正在运行的 WinForms active session。
3. 是否已有稳定的 sample profiler 文件可以纳入回归测试。
4. 报告输出是否需要固定模板，例如 Android frame spike report。
5. Codex 使用场景偏向“本地交互分析”还是“CI 自动性能回归分析”。

## 建议下一步

先执行 Phase 0：

1. 挑选一个小型 `.profiler` 样本作为测试输入。
2. 新增 `ProfilerAnalysisService`，只实现 `LoadSessionFile` 和 `GetSessionSummary`。
3. 写最小测试确认 summary 输出稳定。
4. 再基于这个 facade 接入 MCP server。

这个路径能最快验证“Codex 是否真的能通过结构化数据帮助分析 profiler session”，同时最大限度保护现有 legacy UI。
