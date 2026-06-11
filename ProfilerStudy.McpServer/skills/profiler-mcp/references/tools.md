# Profiler MCP Tool Reference

## Session Lifecycle

| Tool | Use |
| --- | --- |
| `capture_profile` | Capture data from a profiler target. Default `protocol=study` captures ProfilerStudy data from `android://{forward_name}` via adb forward or `pc://{ip}:{port}` via direct TCP. Explicit `protocol=tracy` captures a Tracy 0.10.0 TCP target from `pc://{ip}:{port}` and registers a trace artifact. `protocol=perfetto` is reserved. Use `keep_session=true` for follow-ups. |
| `capture_android_profile` | Compatibility wrapper for fixed Android debug/release sockets. Prefer `capture_profile` for new live captures. |
| `analyze_session_file` | Load a `.profiler`, `.profiler_recording`, or `.profiler_dump` for a one-shot summary. |
| `load_session_file` | Load a profiler file and keep it in memory, returning `sessionId`. |
| `load_trace_file` | Load a trace file through the unified trace path. Current direct trace import supports `.tracy` / `format=tracy`, returns `artifactId`, and can return `sessionId` with `keep_session=true`. |
| `load_trace_artifact` | Reopen a registered trace artifact from the artifact store by `artifact_id`; use `keep_session=true` for follow-up analysis. |
| `list_trace_artifacts` | List registered artifacts from the ProfilerStudy artifact store. Use `format=tracy` for Tracy imports/live captures, `format=all` for every registered format, and optional `since=<ISO-8601 UTC>` to return only newer artifacts. |
| `get_import_diagnostics` | Return import/live-capture diagnostics by `session_id` or by registered `artifact_id`. |
| `get_tracy_status` | Return built-in C# Tracy support status, locked version, and source reference submodule availability. |
| `list_sessions` | Show retained sessions. |
| `close_session` | Release a retained session. |

## Analysis

| Tool | Key args |
| --- | --- |
| `get_session_summary` | `session_id` |
| `get_profiler_overhead` | `session_id`, optional `start_frame`, `end_frame`, `top` |
| `find_slow_frames` | `session_id`, `top`, optional `threshold_ms` |
| `find_scope_hotspots` | `session_id`, `top`, optional `start_frame`, `end_frame` |
| `analyze_frame` | `session_id`, `frame_index`, `top`, `neighbor_count` |
| `analyze_frame_detail` | `session_id`, `frame_index`, optional `max_nodes` default 300, `max_depth` default 12, `min_duration_ms` default 0.01 |
| `analyze_time_range` | `session_id`, either `start_frame` + `end_frame` or `start_time_ns` + `end_time_ns`, `top`, optional `threshold_ms` |
| `list_counters` | `session_id`, optional `top`, `filter` |
| `query_counter` | `session_id`, `counter_name`, optional `start_frame`, `end_frame`, `accumulated`, `max_samples` |

## Prompt Patterns

Existing file:

```text
Use profiler-study load_session_file for C:\workspace\captures\run.profiler, then summarize profiler overhead, slow frames, scope hotspots, and counters.
```

Profiler overhead:

```text
For session s1, call get_profiler_overhead for frames 1200-1300 and explain target-side profiler memory, bytes sent, wait-for-send stalls, and previous-frame send time.
```

Counter investigation:

```text
For session s1, list counters filtered by GPU, then query the most relevant counter across frames 1200-1300.
```

Live PC capture:

```text
Capture url=pc://127.0.0.1:8428 for 30 seconds with keep_session=true, then analyze slow frames and the worst frame.
```

Tracy live capture:

```text
Capture url=pc://127.0.0.1:8086 protocol=tracy for 10 seconds with keep_session=true, then call get_import_diagnostics and list_counters.
```

Tracy time-window analysis:

```text
For Tracy session s1, call analyze_time_range with start_time_ns=1563554109559 and end_time_ns=1563572653674, then summarize the top CPU zones clipped to that timestamp range.
```

Tracy artifact reuse:

```text
Load trace file /captures/run.tracy with keep_session=false, then use the returned artifactId with load_trace_artifact and get_import_diagnostics. For live Tracy captures, artifact debug material is stored as capture.ndjson until source.tracy writer support is verified.
```

Recent Tracy artifacts:

```text
Call list_trace_artifacts with format=tracy and since=2026-06-11T00:00:00Z, then load the newest artifact with keep_session=true.
```

Live Android capture:

```text
Capture url=android:///data/user_de/0/org.azahar_emu.azahar.debug/files/framepro for 30 seconds with keep_session=true, then analyze slow frames and the worst frame.
```

Single-frame detail:

```text
For session s1 frame 842, call analyze_frame_detail, then explain the main thread hierarchy, frameCounters, and largest self-time gaps.
```
