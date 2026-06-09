# Core 基础能力

## 范围

本文记录 `ProfilerStudyCore` 的 UI 无关基础能力，包括 ProfilerStudy session 格式、实时抓取 packet 流、文件读写、核心数据模型、分析迭代器和跨 UI/MCP 共享查询能力。

Perfetto、systrace、Tracy 等外部 trace 格式后续应通过独立 importer/query adapter 接入，不应把外部格式强行伪装成现有 ProfilerStudy packet。

## 目录职责

- `Model/`：帧、线程、scope、counter、context switch、wait event、source/module/log 等内存模型。
- `Sessions/`：`Session` 生命周期、连接、读写、packet 处理、选区复制、session 级统计和保存状态。
- `Protocol/Packets/`：线协议和文件协议 packet 类型、枚举、分配器、发送/接收包装。
- `Transport/`：TCP、adb forward、receive stream、Android context switch 文件加载。
- `Analysis/`：scope/wait event 迭代器、session 统计、帧维度统计。
- `Events/`：Core 对 UI/MCP 暴露的事件 delegate。
- `Infrastructure/`：计时、锁、数组、设置接口、序列化、颜色、平台辅助。

当前 namespace 仍保留 `ProfilerStudy`，这是兼容旧 WinForms 和 MCP 的约束。不要在普通功能修改中顺手迁移 namespace。

## Session 聚合模型

`Session` 是 Core 的根对象，负责持有和维护：

- Frames：`FrameArray`，包含帧起止时间、耗时、每帧 counter 聚合和派生指标。
- Threads：`ThreadInfo`、线程顺序、线程命名和显示元数据。
- Scopes：每线程 `TimeSpanList`、`TimeSpanInfoSet`、`TimeSpanFrameStats`。
- Counters：`CustomStatSessionData`、逐帧 custom stat、per-second 数组、graph/unit/colour 元数据。
- Context switches / waits：`ContextSwitchArray`、`WaitEvent`、进程名、线程状态。
- Logs/events/modules/source info：用于 UI 展示和源码跳转的 packet 派生元数据。

时间单位以采集端 timer tick 为主。UI 和 MCP 输出必须通过 `TimerFrequency`、`FrameXToTime`、`TimeToFrameX`、已有单位换算工具转换，不要直接假定毫秒。

## 生命周期

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

## 修改原则

- 不在 Core 中添加 WinForms/Avalonia 控件引用。
- 不直接枚举可变集合，除非持有匹配读锁或使用已有 snapshot 方法。
- 不从 Core 弹 UI 对话框；Core 应返回错误或触发 UI 无关事件。
- 长耗时读写/复制通过 `ThreadJobContext` 或等价 UI 无关进度机制报告。
- 涉及协议、文件兼容、线程模型的改动必须有明确验证方案。
