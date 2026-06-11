---
name: profiler-mcp
description: Use when analyzing ProfilerStudy captures through the profiler-study MCP server, including Android captures, .profiler files, slow frames, scope hotspots, loaded sessions, custom stats, counters, profiler overhead, and performance bug reports.
---

# Profiler MCP

## Overview

Use the `profiler-study` MCP server as the first choice for ProfilerStudy analysis. Prefer structured tool results over ad hoc file parsing or guessing from screenshots.

## Setup Check

Before analysis, verify the MCP server is available when tools are visible in the session. If not available, inspect `C:\Users\Admin\.codex\config.toml` and ensure it contains `mcp_servers.profiler-study` pointing at:

```text
C:\workspace\profiler_legacy\ProfilerStudy.McpServer\publish\codex\ProfilerStudy.McpServer.dll
```

If the binary is stale or missing, build from `C:\workspace\profiler_legacy`:

```powershell
dotnet publish ProfilerStudy.McpServer\ProfilerStudy.McpServer.csproj -c Release -nologo -v minimal -p:TargetFrameworks=net8.0 -o ProfilerStudy.McpServer\publish\codex
```

## Workflow

1. For existing ProfilerStudy capture files, call `load_session_file` when follow-up questions are likely; call `analyze_session_file` only for one-shot summaries.
2. For existing `.tracy` files, call `load_trace_file` with `format=auto` or `format=tracy`; use `keep_session=true` for follow-up analysis or keep the returned `artifactId` for later `load_trace_artifact`.
3. For live captures, call `capture_profile` with a target URL and `keep_session=true` when the user may ask follow-up questions. Use default `protocol=study` for ProfilerStudy targets. Use explicit `protocol=tracy` only for Tracy 0.10.0 TCP targets and `pc://{ip}:{port}` URLs.
4. Start broad: `get_session_summary`, then `get_profiler_overhead`, then `find_slow_frames`, then `find_scope_hotspots`.
5. Narrow by evidence: use `analyze_frame` for a suspicious frame, `analyze_frame_detail` when hierarchical single-frame flame graph data and same-frame counter samples are needed, and `analyze_time_range` for a slow-frame cluster or comparison window.
6. For custom stat / counter questions, call `list_counters` first, then `query_counter` using the exact returned `counter_name`.
7. Use `get_import_diagnostics` with `session_id` or `artifact_id` when import compatibility, Tracy version support, or partial decoding status matters.
8. Close retained sessions with `close_session` when analysis is complete or when many sessions are loaded.

## Interpretation Rules

- Treat counters as ProfilerStudy custom stats. Use their `valueType`, `unit`, `totalCount`, `maxValuePerFrame`, and per-frame samples to explain behavior.
- Treat Tracy counters as Tracy plots. Tracy sessions use `sourceFormat=tracy`; not every ProfilerStudy-only analysis field is available.
- Treat `get_profiler_overhead` as the authoritative target-side ProfilerStudy overhead entry point; on Tracy sessions it returns an unsupported capability result. Do not infer profiler overhead from arbitrary scope or counter names.
- Treat `analyze_frame_detail.frameCounters` as the current frame's custom stat samples; use it before issuing separate counter queries for a single suspicious frame.
- Do not infer missing scope time from nested totals. The server's diagnostics use conservative broad-unattributed estimates.
- Keep raw output bounded: use `top`, frame ranges, `max_nodes`, `max_depth`, `min_duration_ms`, and `max_samples` instead of requesting whole traces.
- Preserve paths and session ids exactly as returned by tools.
- Preserve artifact ids exactly as returned by `load_trace_file`, `capture_profile(protocol=tracy)`, and `list_trace_artifacts`.
- For live targets, prefer `capture_profile` URLs. Do not run arbitrary adb commands through this skill; Android capture only forwards the requested `localfilesystem:<path>`, `localabstract:<name>`, or `tcp:<port>` endpoint.

## Tool Reference

For parameter details and suggested prompt shapes, read `references/tools.md` only when you need exact schemas or examples.
