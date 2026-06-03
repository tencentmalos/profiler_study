# Profiler MCP Tool Reference

## Session Lifecycle

| Tool | Use |
| --- | --- |
| `capture_android_profile` | Capture Android FramePro data from the fixed debug/release socket. Use `keep_session=true` for follow-ups. |
| `analyze_session_file` | Load a `.profiler`, `.profiler_recording`, or `.profiler_dump` for a one-shot summary. |
| `load_session_file` | Load a profiler file and keep it in memory, returning `sessionId`. |
| `list_sessions` | Show retained sessions. |
| `close_session` | Release a retained session. |

## Analysis

| Tool | Key args |
| --- | --- |
| `get_session_summary` | `session_id` |
| `find_slow_frames` | `session_id`, `top`, optional `threshold_ms` |
| `find_scope_hotspots` | `session_id`, `top`, optional `start_frame`, `end_frame` |
| `analyze_frame` | `session_id`, `frame_index`, `top`, `neighbor_count` |
| `analyze_frame_detail` | `session_id`, `frame_index`, optional `max_nodes` default 300, `max_depth` default 12, `min_duration_ms` default 0.01 |
| `analyze_time_range` | `session_id`, `start_frame`, `end_frame`, `top`, optional `threshold_ms` |
| `list_counters` | `session_id`, optional `top`, `filter` |
| `query_counter` | `session_id`, `counter_name`, optional `start_frame`, `end_frame`, `accumulated`, `max_samples` |

## Prompt Patterns

Existing file:

```text
Use profiler-study load_session_file for C:\workspace\captures\run.profiler, then summarize slow frames, scope hotspots, and counters.
```

Counter investigation:

```text
For session s1, list counters filtered by GPU, then query the most relevant counter across frames 1200-1300.
```

Android capture:

```text
Capture target=debug for 30 seconds with keep_session=true, then analyze slow frames and the worst frame.
```

Single-frame detail:

```text
For session s1 frame 842, call analyze_frame_detail, then explain the main thread hierarchy, frameCounters, and largest self-time gaps.
```
