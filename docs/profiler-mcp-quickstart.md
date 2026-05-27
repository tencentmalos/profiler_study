# Profiler MCP Quickstart

## Build

```powershell
dotnet build ProfilerStudy.McpServer\ProfilerStudy.McpServer.csproj -c Debug -nologo -v minimal
```

## Codex MCP 配置

将下面的 stdio server 配到 Codex 的 MCP 配置中，命令路径按你的构建输出调整：

```json
{
  "mcpServers": {
    "profiler-study": {
      "command": "C:\\workspace\\profiler_legacy\\ProfilerStudy.McpServer\\bin\\Debug\\net472\\ProfilerStudy.McpServer.exe",
      "args": []
    }
  }
}
```

## 可用工具

### `capture_android_profile`

连接固定 Android FramePro socket，抓取一段时间并返回摘要分析。

参数：

- `target`: `debug` 或 `release`
- `duration_seconds`: 抓取秒数，默认 `60`
- `top`: 返回慢帧和热点条数，默认 `10`

示例请求意图：

```text
请调用 profiler-study 的 capture_android_profile，target=debug，duration_seconds=60，然后分析慢帧和热点 scope。
```

### `analyze_session_file`

加载已有 `.profiler`、`.profiler_recording` 或 `.profiler_dump` 文件并返回摘要分析。

参数：

- `path`: 文件路径
- `top`: 返回条数，默认 `10`

## 当前限制

- MCP server 是 Windows / .NET Framework 侧车进程，复用当前 `ProfilerStudyCore`。
- 第一版只暴露两个工具，不直接控制 WinForms UI。
- Android 采集使用当前固定 socket：
  - Debug: `/data/user_de/0/org.azahar_emu.azahar.debug/files/framepro`
  - Release: `/data/user_de/0/org.azahar_emu.azahar/files/framepro`
- 工具不会提供任意 shell / adb 执行能力。
