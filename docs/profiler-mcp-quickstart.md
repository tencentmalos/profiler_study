# Profiler MCP Quickstart

## Build

```powershell
dotnet build ProfilerStudy.McpServer\ProfilerStudy.McpServer.csproj -c Release -nologo -v minimal
```

## Codex MCP 配置

将下面的 stdio server 配到 Codex 的 MCP 配置中，命令路径按你的构建输出调整：

```json
{
  "mcpServers": {
    "profiler-study": {
      "command": "C:\\workspace\\profiler_legacy\\ProfilerStudy.McpServer\\bin\\Release\\net472\\ProfilerStudy.McpServer.exe",
      "args": []
    }
  }
}
```

如果仓库上传到 public GitHub，更推荐通过 Codex plugin marketplace 安装。直接把 GitHub 仓库 URL 传给 `codex mcp add --url` 不可行，因为 `--url` 只适用于已经运行的 Streamable HTTP MCP server；本项目是本地 stdio MCP server。

public 仓安装方式：

```powershell
codex plugin marketplace add https://github.com/<owner>/<repo> --ref main
codex plugin add profiler-study --marketplace profiler-study
codex mcp list
```

安装后插件会注册 `profiler-study` MCP server。server 启动时会从 marketplace source 定位或拉取仓库源码，构建 `ProfilerStudy.McpServer` Release 版，然后启动 stdio MCP 进程。

## Codex Skill

仓库内维护版位于：

```text
ProfilerStudy.McpServer\skills\profiler-mcp
```

这个目录是 `profiler-mcp` skill 的 source of truth，包含 `SKILL.md`、`agents/openai.yaml` 和 `references/tools.md`。安装到当前用户 Codex skill 目录时，复制整个 `profiler-mcp` 文件夹到：

```text
C:\Users\Admin\.codex\skills\profiler-mcp
```

更新 skill 后用校验脚本检查：

```powershell
python C:\Users\Admin\.codex\skills\.system\skill-creator\scripts\quick_validate.py ProfilerStudy.McpServer\skills\profiler-mcp
```

public GitHub plugin 安装会使用 `.agents/plugins/plugins/profiler-study/skills/profiler-mcp` 中的随插件分发副本。更新 skill 时，需要同步维护这两个位置：

- `ProfilerStudy.McpServer\skills\profiler-mcp`
- `.agents\plugins\plugins\profiler-study\skills\profiler-mcp`

## 可用工具

### `capture_profile`

通过 URL 连接 FramePro target，抓取一段时间并返回摘要分析。

支持的 URL：

- `android://{forward_name}`：执行 `adb forward tcp:<local_port> <endpoint>`，再连接本地转发端口。默认把普通路径解析为 `localfilesystem:{forward_name}`，也支持显式 `localfilesystem:<path>`、`localabstract:<name>`、`tcp:<port>`，裸数字端口会解析为 `tcp:<port>`。例如 `android:///data/user_de/0/org.azahar_emu.azahar.debug/files/framepro`、`android://localabstract:azahar-framepro`、`android://tcp:8428`。
- `pc://{ip}:{port}`：直接通过 TCP 连接 PC target。例如 `pc://127.0.0.1:8428`。

参数：

- `url`: target URL
- `duration_seconds`: 抓取秒数，默认 `60`
- `top`: 返回慢帧和热点条数，默认 `10`
- `keep_session`: 是否把本次采集保留在 MCP server 内存中并返回 `sessionId`，默认 `false`

示例请求意图：

```text
请调用 profiler-study 的 capture_profile，url=pc://127.0.0.1:8428，duration_seconds=60，然后分析慢帧和热点 scope。
```

Android 示例：

```text
请调用 profiler-study 的 capture_profile，url=android:///data/user_de/0/org.azahar_emu.azahar.debug/files/framepro，duration_seconds=60，然后分析慢帧和热点 scope。
```

如果要继续追问某个慢帧，使用 `keep_session=true`：

```text
请调用 profiler-study 的 capture_profile，url=pc://192.168.1.20:8428，duration_seconds=30，keep_session=true，然后对最慢帧调用 analyze_frame。
```

### `capture_android_profile`

兼容旧工具，连接固定 Android Debug / Release FramePro socket。新调用优先使用 `capture_profile`。

参数：

- `target`: `debug` 或 `release`
- `duration_seconds`: 抓取秒数，默认 `60`
- `top`: 返回慢帧和热点条数，默认 `10`
- `keep_session`: 是否把本次采集保留在 MCP server 内存中并返回 `sessionId`，默认 `false`

### `analyze_session_file`

加载已有 `.profiler`、`.profiler_recording` 或 `.profiler_dump` 文件并返回摘要分析。

参数：

- `path`: 文件路径
- `top`: 返回条数，默认 `10`

### `load_session_file`

加载 session 文件并保留在 MCP server 内存中，返回 `sessionId`。后续查询工具都使用这个 id。

参数：

- `path`: 文件路径
- `top`: 初始摘要返回条数，默认 `10`

### `list_sessions`

列出当前 MCP server 内存中保留的 session。

### `close_session`

关闭由 `load_session_file` 或 `capture_android_profile keep_session=true` 创建的 session。

参数：

- `session_id`: session id

### `get_session_summary`

返回已加载 session 的摘要、慢帧、热点、线程、慢帧模式诊断和基础未归因时间诊断。

参数：

- `session_id`: session id

### `find_slow_frames`

查找慢帧，并返回慢帧周期 / 聚类诊断。

参数：

- `session_id`: session id
- `top`: 返回条数，默认 `10`
- `threshold_ms`: 慢帧阈值；为 `0` 或省略时自动使用 `max(33.333ms, averageFrameMs * 1.5)`

### `find_scope_hotspots`

查找 scope 热点。可限制在指定 frame range 内，适合比较慢帧簇和正常区间。

参数：

- `session_id`: session id
- `top`: 返回条数，默认 `10`
- `start_frame`: 起始 frame，可省略
- `end_frame`: 结束 frame，可省略

### `list_counters`

列出已加载 session 中的 counter/custom stat，方便后续按名称查询明细。

参数：

- `session_id`: session id
- `top`: 返回条数，默认 `20`
- `filter`: counter 名称的大小写不敏感子串过滤，可省略

### `query_counter`

查询一个 counter 在指定 frame range 内的逐帧样本。

参数：

- `session_id`: session id
- `counter_name`: counter 名称，通常先通过 `list_counters` 获取
- `start_frame`: 起始 frame，可省略
- `end_frame`: 结束 frame，可省略
- `accumulated`: 是否返回 accumulated 值，默认 `false`
- `max_samples`: 最大返回样本数，默认 `200`

### `analyze_frame`

分析单帧，返回该帧局部 scope hotspots、邻近帧上下文和未归因时间诊断。

参数：

- `session_id`: session id
- `frame_index`: frame index
- `top`: 返回条数，默认 `10`
- `neighbor_count`: 邻近帧数量，默认 `3`

### `analyze_frame_detail`

抽取单帧的按线程层级火焰图数据，并返回该帧有样本的 custom stat/counter 值，用于深入分析一帧内部的 parent/children 调用结构和同帧 counter 状态。`threadFlameGraphs` 是主输出，保留每个线程的根节点和子节点；`frameCounters` 是当前帧的 counter 样本；`topSpans` 只是辅助索引，不代表层级。

参数：

- `session_id`: session id
- `frame_index`: frame index
- `max_nodes`: 最大返回层级节点数，默认 `300`
- `max_depth`: 最大展开层级，默认 `12`
- `min_duration_ms`: 过滤低于该耗时的节点，默认 `0.01`

### `analyze_time_range`

分析 frame range，返回 range 摘要、慢帧、热点、慢帧模式和未归因时间诊断。

参数：

- `session_id`: session id
- `start_frame`: 起始 frame
- `end_frame`: 结束 frame
- `top`: 返回条数，默认 `10`
- `threshold_ms`: 慢帧阈值；为 `0` 或省略时自动选择

## 当前限制

- MCP server 是 .NET 侧车进程，复用当前 `ProfilerStudyCore`。
- 目前工具仍是不控制 WinForms UI 的 sidecar 模式。
- live 采集优先使用 URL：Android 使用 `android://{forward_name}`，PC 使用 `pc://{ip}:{port}`。
- 工具不会提供任意 shell / adb 执行能力。
- 读取文件限制在当前工作区和 `C:\workspace` 下，避免 MCP 变成任意文件读取通道。
- `analyze_frame` 的未归因时间是保守估算：用 frame scope 或 frame duration 减去最大非 frame scope，避免嵌套 scope 双重计数。
