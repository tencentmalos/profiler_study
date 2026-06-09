---
name: profiler-mcp
description: Use when analyzing ProfilerStudy or ProfilerStudy captures through the profiler-study MCP server, including Android captures, .profiler files, slow frames, scope hotspots, loaded sessions, custom stats, counters, and performance bug reports.
---

# Profiler MCP

## Overview

Use the `profiler-study` MCP server as the first choice for ProfilerStudy / ProfilerStudy analysis. Prefer structured tool results over ad hoc file parsing or guessing from screenshots.

## Setup Check

Before analysis, verify the MCP server is available when tools are visible in the session. If not available, inspect `C:\Users\Admin\.codex\config.toml` and ensure it contains `mcp_servers.profiler-study` pointing at:

```text
C:\workspace\profiler_legacy\ProfilerStudy.McpServer\bin\Release\net8.0\ProfilerStudy.McpServer.dll
```

If the binary is stale or missing, build from `C:\workspace\profiler_legacy`:

```powershell
dotnet build ProfilerStudy.McpServer\ProfilerStudy.McpServer.csproj -c Release -nologo -v minimal
```

## Workflow

1. For an existing capture file, call `load_session_file` when follow-up questions are likely; call `analyze_session_file` only for one-shot summaries.
2. For live captures, call `capture_profile` with a target URL and `keep_session=true` when the user may ask follow-up questions. Use `android://{forward_name}` for adb forward to a device `localfilesystem`, `localabstract`, or `tcp` endpoint and `pc://{ip}:{port}` for direct TCP.
3. Start broad: `get_session_summary`, then `find_slow_frames`, then `find_scope_hotspots`.
4. Narrow by evidence: use `analyze_frame` for a suspicious frame, `analyze_frame_detail` when hierarchical single-frame flame graph data and same-frame counter samples are needed, and `analyze_time_range` for a slow-frame cluster or comparison window.
5. For custom stat / counter questions, call `list_counters` first, then `query_counter` using the exact returned `counter_name`.
6. Close retained sessions with `close_session` when analysis is complete or when many sessions are loaded.

## Interpretation Rules

- Treat counters as ProfilerStudy custom stats. Use their `valueType`, `unit`, `totalCount`, `maxValuePerFrame`, and per-frame samples to explain behavior.
- Treat `analyze_frame_detail.frameCounters` as the current frame's custom stat samples; use it before issuing separate counter queries for a single suspicious frame.
- Do not infer missing scope time from nested totals. The server's diagnostics use conservative broad-unattributed estimates.
- Keep raw output bounded: use `top`, frame ranges, `max_nodes`, `max_depth`, `min_duration_ms`, and `max_samples` instead of requesting whole traces.
- Preserve paths and session ids exactly as returned by tools.
- For live targets, prefer `capture_profile` URLs. Do not run arbitrary adb commands through this skill; Android capture only forwards the requested `localfilesystem:<path>`, `localabstract:<name>`, or `tcp:<port>` endpoint.

## Tool Reference

For parameter details and suggested prompt shapes, read `references/tools.md` only when you need exact schemas or examples.
