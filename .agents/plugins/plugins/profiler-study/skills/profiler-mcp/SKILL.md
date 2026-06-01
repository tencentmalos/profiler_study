---
name: profiler-mcp
description: Use when analyzing FramePro or ProfilerStudy captures through the profiler-study MCP server, including Android captures, .profiler files, slow frames, scope hotspots, loaded sessions, custom stats, counters, and performance bug reports.
---

# Profiler MCP

## Overview

Use the `profiler-study` MCP server as the first choice for ProfilerStudy / FramePro analysis. Prefer structured tool results over ad hoc file parsing or guessing from screenshots.

## Setup Check

Before analysis, verify the MCP server is available when tools are visible in the session. If not available, inspect `C:\Users\Admin\.codex\config.toml` and ensure the `profiler-study` plugin or MCP server is installed.

If the binary is stale or missing in a local checkout, build from the repository root:

```powershell
dotnet build ProfilerStudy.McpServer\ProfilerStudy.McpServer.csproj -c Release -nologo -v minimal
```

## Workflow

1. For an existing capture file, call `load_session_file` when follow-up questions are likely; call `analyze_session_file` only for one-shot summaries.
2. For live Android captures, call `capture_android_profile` with `keep_session=true` when the user may ask follow-up questions.
3. Start broad: `get_session_summary`, then `find_slow_frames`, then `find_scope_hotspots`.
4. Narrow by evidence: use `analyze_frame` for a suspicious frame and `analyze_time_range` for a slow-frame cluster or comparison window.
5. For custom stat / counter questions, call `list_counters` first, then `query_counter` using the exact returned `counter_name`.
6. Close retained sessions with `close_session` when analysis is complete or when many sessions are loaded.

## Interpretation Rules

- Treat counters as FramePro custom stats. Use their `valueType`, `unit`, `totalCount`, `maxValuePerFrame`, and per-frame samples to explain behavior.
- Do not infer missing scope time from nested totals. The server's diagnostics use conservative broad-unattributed estimates.
- Keep raw output bounded: use `top`, frame ranges, and `max_samples` instead of requesting whole traces.
- Preserve paths and session ids exactly as returned by tools.
- For Android, use `target=debug` or `target=release`; do not run arbitrary adb commands through this skill.

## Tool Reference

For parameter details and suggested prompt shapes, read `references/tools.md` only when you need exact schemas or examples.
