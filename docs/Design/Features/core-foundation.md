# Core 基础能力

## 设计目标

`ProfilerStudyCore` 是所有 UI 和 MCP 的共同基础。它负责把 ProfilerStudy 协议数据转成稳定的 session 模型，并提供不依赖 UI 的查询、统计和读写能力。

设计目标：

- 保持现有 `.profiler` / recording / dump 行为兼容。
- 让 WinForms、Avalonia、MCP 共享同一份 session 语义。
- 把协议、transport、model、analysis 与 UI 控件隔离。
- 为 Perfetto、systrace、Tracy 等外部 trace importer 预留 query adapter 接入点。

外部 trace 格式不应强行伪装成 ProfilerStudy packet。正确方向是新增共享 trace document/query 层，再让 ProfilerStudy `Session` 作为其中一个 adapter。

## 目录职责

- `Model/`：帧、线程、scope、counter、context switch、wait event、source/module/log 等内存模型。
- `Sessions/`：`Session` 生命周期、连接、读写、packet 处理、选区复制、session 级统计和保存状态。
- `Protocol/Packets/`：线协议和文件协议 packet 类型、枚举、分配器、发送/接收包装。
- `Transport/`：TCP、adb forward、receive stream、Android context switch 文件加载。
- `Analysis/`：scope/wait event 迭代器、session 统计、帧维度统计。
- `Events/`：Core 对 UI/MCP 暴露的事件 delegate。
- `Infrastructure/`：计时、锁、数组、设置接口、序列化、颜色、平台辅助。

当前 namespace 仍保留 `ProfilerStudy`，这是兼容旧 WinForms 和 MCP 的约束。不要在普通功能修改中顺手迁移 namespace。

## 核心边界

Core 可以做：

- 连接、接收、反序列化、处理 ProfilerStudy packet。
- 保存和读取 ProfilerStudy session 文件。
- 维护帧、线程、scope、counter、context switch、log 等模型。
- 提供 UI/MCP 可复用的查询和统计。
- 通过事件通知外部 session 状态变化。

Core 不应该做：

- 打开 WinForms/Avalonia 对话框。
- 直接依赖具体 UI 控件或窗口。
- 生成 MCP markdown 或 Avalonia row model。
- 在绘制路径中临时计算 UI 专用布局。
- 把外部 trace 格式硬塞进 ProfilerStudy packet 处理链。

## Session 聚合模型

`Session` 是 Core 的根对象，负责持有和维护：

- Frames：`FrameArray`，包含帧起止时间、耗时、每帧 counter 聚合和派生指标。
- Threads：`ThreadInfo`、线程顺序、线程命名和显示元数据。
- Scopes：每线程 `TimeSpanList`、`TimeSpanInfoSet`、`TimeSpanFrameStats`。
- Counters：`CustomStatSessionData`、逐帧 custom stat、per-second 数组、graph/unit/colour 元数据。
- Context switches / waits：`ContextSwitchArray`、`WaitEvent`、进程名、线程状态。
- Logs/events/modules/source info：用于 UI 展示和源码跳转的 packet 派生元数据。

时间单位以采集端 timer tick 为主。UI 和 MCP 输出必须通过 `TimerFrequency`、`FrameXToTime`、`TimeToFrameX`、已有单位换算工具转换，不要直接假定毫秒。

## 数据流

### 实时连接

实时抓取入口：

- `Session.Connect()`：普通 TCP 连接。
- `Session.ConnectToAndroid(...)`：Android adb forward 后连接本地 TCP。

实时连接主要线程/队列：

- receive path 从 `Connection` / `ReceiveStream` 读取原始数据。
- packet 反序列化为 `ReceivedPacket`。
- `ProcessEventsThreadMain` 从队列批量取 packet，并调用 `ProcessPacket`。
- `ProcessPacket` 分发到 `Handle*Packet` 方法修改模型。
- send queue 用于发送 callstack 开关、string 请求、custom stat 颜色等控制 packet。

修改实时处理时必须保留现有线程安全语义：`ReadWriteLock`、`lock`、packet queue、dispose/join 顺序都是行为约束。

实时连接数据流：

```text
Connection/ReceiveStream
  -> ReceivedPacket
  -> Session.ProcessPacket
  -> Frame/Thread/Scope/Counter/ContextSwitch model
  -> Core events
  -> WinForms / Avalonia / MCP query
```

### 文件读取

`Session.Read(...)` 从保存文件重建模型，并按需恢复 `SessionViewSaveData`。

当前一等支持格式：

- `.profiler`
- `.profiler_recording`
- `.profiler_dump`

新增格式读取时，优先新增 trace importer/query abstraction；只有确实与 ProfilerStudy packet 等价时才扩展现有 packet 读写。

### 文件保存

`Session.Write(...)` 保存 session 和 view save data。Core 不直接读取 WinForms/Avalonia 控件状态；UI 层负责收集 `SessionViewSaveData` 并传入 Core。

### 选区复制

`Session.CopyTo(...)` 将时间范围复制成新 session。它复制帧、scope、counter、线程元数据、context switch、进程名，并重新计算 session stats。该行为支撑 UI 的 "Create Session from Selection"，必须保持确定性。

## 线程与锁约定

Core 当前不是不可变模型。读写大型集合时必须遵守现有锁约定：

- `m_FramesLock` 保护 frame 集合。
- `m_ThreadsLock` 保护 thread metadata。
- `m_TimeSpansLock` 保护每线程 scope 列表。
- `m_TimeSpanInfoSetLock` 保护 scope metadata。
- `m_CustomStatSessionInfoLock` 和相关 lock 保护 counter 元数据和值。
- context switch arrays 和 cache 使用各自 lock。

新增查询方法优先返回 snapshot、只读 list 或 DTO。不要把内部可变集合直接暴露给新 UI。

## Packet 扩展规则

新增或修改 packet 时：

1. 在 `Protocol/Packets/` 增加 packet 类型。
2. 更新 packet allocator / 反序列化路径。
3. 更新 `Session.ProcessPacket` 分发和 handler。
4. 兼容 deprecated packet，除非明确决定废弃旧格式。
5. 先更新 `docs/Design/Features` 中对应设计文档，再改代码。

## 查询与分析基础能力

Core 当前提供的低层查询能力：

- 帧索引和时间转换：`GetFrame`、`GetFrameIndex`、`GetFrameStartEndTime`、`FrameXToTime`、`TimeToFrameX`。
- Scope 遍历：`TimeSpanIterator`、`RootFirstTimeSpanIterator`、`WaitEventIterator`。
- Scope 统计：`ScopeSessionStats`、`TimeSpanFrameStats`、`TimeSpanInfoSet`。
- Counter 采样：`GetCustomStats`、`GetCustomStatsPerSec`、custom stat min/max、graph/unit 查询。
- 慢帧辅助：上一/下一 alert frame、最大帧、平均帧、budget 计算。
- Context switch：Android 文件加载、session 内 context switch 查询和缓存。

UI 无关且可被 MCP 复用的分析逻辑放在 `ProfilerStudyCore/Analysis`。MCP markdown/JSON 格式化放在 `ProfilerStudy.McpServer`。Avalonia 行模型和控件状态放在 `ProfilerStudy.Avalonia`。

## 与 UI/MCP 的协作方式

WinForms 当前直接持有 `Session`，这是 legacy 行为。Avalonia 应通过 `SessionDocument`、`SessionQueryService`、feature analyzer 读取 Core 数据。MCP 应通过 server-side analysis service 读取 Core 数据并输出结构化结果。

新共享分析能力的推荐路径：

1. 在 Core 中提供 UI 无关查询或统计。
2. 在 Avalonia/MCP 分别做展示层转换。
3. WinForms 如需复用，再从 legacy view 调用 Core 查询。

不要为了某个 UI 的表格列，把 UI 文案或排序状态写进 Core。

## 外部 Trace 预留方向

Perfetto/Tracy/systrace 接入时建议新增：

- `TraceDocument`：统一描述 trace 文件、时间范围、线程、slice、counter。
- `ITraceQuerySession`：提供按时间范围、线程、counter、slice 查询。
- `ProfilerStudyTraceQuerySession`：用现有 `Session` 适配统一 query。
- `PerfettoTraceQuerySession` / `TracyTraceQuerySession`：外部格式 importer 的输出。

这样 WinForms/Avalonia/MCP 可以逐步迁移到共享 query，而不是把所有格式塞进 `Session`。

## 修改原则

- 不在 Core 中添加 WinForms/Avalonia 控件引用。
- 不直接枚举可变集合，除非持有匹配读锁或使用已有 snapshot 方法。
- 不从 Core 弹 UI 对话框；Core 应返回错误或触发 UI 无关事件。
- 长耗时读写/复制通过 `ThreadJobContext` 或等价 UI 无关进度机制报告。
- 涉及协议、文件兼容、线程模型的改动必须有明确验证方案。

## 验收清单

Core 改动至少选择匹配的验证项：

- `dotnet build ProfilerStudy.McpServer/ProfilerStudy.McpServer.csproj -c Debug -p:TargetFrameworks=net8.0`
- `dotnet run --project ProfilerStudy.McpServer/ProfilerStudy.McpServer.csproj -c Debug -p:TargetFrameworks=net8.0 -- --self-test`
- Avalonia session load smoke test。
- Windows 上完整 solution build。
- 使用真实 `.profiler` / `.profiler_recording` 文件做读写回归。
- 如果改 live capture，验证普通 TCP 和 Android adb forward 两条路径。
