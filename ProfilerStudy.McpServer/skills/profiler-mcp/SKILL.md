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

1. For an existing file when the format can be inferred, call `open_file` so the server opens it as the newest retained session and returns `sessionId`.
2. For existing ProfilerStudy capture files where a one-shot summary is enough, call `analyze_session_file`; use `load_session_file` only when explicitly targeting the legacy study loader.
3. For existing `.tracy` files where artifact-only reuse is desired, call `load_trace_file` with `keep_session=false`; otherwise prefer `open_file`.
4. For live captures, call `capture_profile` with a target URL and `keep_session=true` when the user may ask follow-up questions. Use default `protocol=study` for ProfilerStudy targets. Use explicit `protocol=tracy` for Tracy 0.10.0 targets; both `pc://{ip}:{port}` and Android `android://localabstract:<name>` / `android://localfilesystem:<path>` / `android://tcp:<port>` are supported.
5. Start broad: `get_session_summary`, then `get_profiler_overhead`, then `find_slow_frames`, then `find_scope_hotspots`.
6. Narrow by evidence: use `analyze_frame` for a suspicious frame, `analyze_frame_detail` when hierarchical single-frame flame graph data and same-frame counter samples are needed, and `analyze_time_range` for a slow-frame cluster or comparison window. Tracy sessions may use `start_time_ns` / `end_time_ns` when frame metadata is unavailable or timestamp precision is required.
7. For Tracy GPU questions, prefer `analyze_gpu_frames` first to get per-frame CPU/GPU/counter correlation, then use `analyze_frame_detail` on a suspicious frame. Use `get_gpu_timeline` when frame-order GPU zone sequencing matters, and use `list_gpu_zones` for lightweight context/hotspot checks.
8. For custom stat / counter questions, call `list_counters` first, then `query_counter` using the exact returned `counter_name`.
9. Use `get_import_diagnostics` with `session_id` or `artifact_id` when import compatibility, Tracy version support, or partial decoding status matters.
10. Save retained sessions with `save_session_file` only when the user asks for a file artifact. The server corrects the output suffix from the session's actual protocol; do not infer protocol from the requested save path.
11. Close retained sessions with `close_session` when analysis is complete or when many sessions are loaded.

## Interpretation Rules

- Treat counters as ProfilerStudy custom stats. Use their `valueType`, `unit`, `totalCount`, `maxValuePerFrame`, and per-frame samples to explain behavior.
- Treat Tracy counters as Tracy plots. Tracy sessions use `sourceFormat=tracy`; not every ProfilerStudy-only analysis field is available.
- Treat Tracy GPU zones separately from counters. `analyze_gpu_frames`, `analyze_frame_detail.gpuSummary`, and `list_gpu_zones.summary` report both resolved GPU-time zones and CPU submit fallback zones; prefer GPU-time zones for precise GPU profiler conclusions.
- Treat `cpu-submit-time` GPU zones as fallback diagnostics. They are useful for locating missing timestamp data and rough ordering, but not for precise GPU critical path claims.
- Treat `get_profiler_overhead` as the authoritative target-side ProfilerStudy overhead entry point; on Tracy sessions it returns an unsupported capability result. Do not infer profiler overhead from arbitrary scope or counter names.
- For Tracy sessions, `analyze_frame` and `analyze_frame_detail` may return partial frame metadata results or `supported=false` diagnostics when frame metadata or hierarchy data is unavailable.
- Treat `analyze_frame_detail.frameCounters` as the current frame's custom stat samples; use it before issuing separate counter queries for a single suspicious frame.
- Do not infer missing scope time from nested totals. The server's diagnostics use conservative broad-unattributed estimates.
- Keep raw output bounded: use `top`, frame ranges, `max_nodes`, `max_depth`, `min_duration_ms`, and `max_samples` instead of requesting whole traces.
- Preserve paths and session ids exactly as returned by tools.
- Preserve artifact ids exactly as returned by `load_trace_file`, `capture_profile(protocol=tracy)`, and `list_trace_artifacts`.
- For Tracy `save_session_file`, distinguish `saveMode=source-copy` / `source-artifact-copy` from `saveMode=tracy-writer`. The writer path still produces a `.tracy` file, but it is materialized from decoded Tracy 0.10.0 data and reports `writerCoverage` instead of byte-exact source preservation.
- For live targets, prefer `capture_profile` URLs. Do not run arbitrary adb commands through this skill; Android capture only forwards the requested `localfilesystem:<path>`, `localabstract:<name>`, or `tcp:<port>` endpoint.

## Tool Reference

For parameter details and suggested prompt shapes, read `references/tools.md` only when you need exact schemas or examples.
