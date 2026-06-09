# Code Organization

## Goals

This layout separates the current profiler code by responsibility so future trace support can be added without extending legacy catch-all folders.

## Projects

### `ProfilerStudyCore`

Core code is independent of UI and MCP transport.

- `Model/`: capture data types such as frames, threads, scopes, counters, modules, sources, context switches, and wait events.
- `Sessions/`: session loading, saving, lifecycle, processing state, and session-level metadata.
- `Protocol/Packets/`: profiler packet types, packet allocation, send/receive packet wrappers, and packet enums.
- `Transport/`: TCP, adb forwarding, connection, receive stream, and platform transport helpers.
- `Analysis/`: iterators, scope/session statistics, and analysis-oriented data structures.
- `Events/`: delegate and event handler types used by sessions and UI consumers.
- `Infrastructure/`: settings, platform helpers, timing, locking, collections, serialisation helpers, colours, and core bootstrap.

### `ProfilerStudy.McpServer`

MCP code is grouped around server responsibilities.

- `Program.cs`: stdio JSON-RPC loop and MCP method dispatch.
- `Tools/`: MCP tool registry, schemas, argument coercion, and tool dispatch to services.
- `Analysis/`: profiler session analysis service and loaded-session management.
- `Capture/`: capture target parsing and target-specific connection metadata.
- `Diagnostics/`: self-test and diagnostic checks.
- `Reporting/`: markdown formatting for structured MCP results.

### `tools/profiler_study_sdk`

ProfilerStudy SDK 代码通过 git submodule 引入，远端为：

```text
git@github.com:tencentmalos/profiler_study_sdk.git
```

主仓库不再维护内嵌的 `tools/sdk source` 副本；后续 SDK 修复应先提交到 `profiler_study_sdk` 仓库，再在主仓库更新 submodule revision。这样 Azahar、ProfilerStudy 和其它项目可以共享同一份 SDK 源码，避免不同项目各自拷贝 `FramePro.cpp` / `FramePro.h` 后产生协议或 Android socket 行为漂移。

### `ProfilerStudy`

The WinForms UI is legacy code and is intentionally isolated.

- `LegacyWinForms/`: legacy WinForms application, views, dialogs, custom controls, and UI-specific helpers.
- `LegacyWinForms.Properties/`: generated resource/settings wrappers used by the legacy UI.
- `Utils/`: non-UI helpers used by the WinForms project.

### `ProfilerStudy.Avalonia`

The Avalonia client is the active cross-platform UI surface.

- `App/`: application bootstrap, theme service, smoke test, and shell window.
- `App/Shell/`: main window and main window view model.
- `Controls/`: reusable drawing and timeline controls.
- `Common/`: observable and sorting helpers.
- `Commands/`: command helpers.
- `Models/`: UI row/view data models.
- `Platform/`: settings and source viewer platform integration.
- `Timeline/`: shared timeline selection and viewport state.
- `Features/Sessions/`: session document loading/querying/summary adapters.
- `Features/Counters/`: selected-frame counter analysis and rows.
- `Features/Scopes/`: scope hotspot and frame detail analysis.
- `Features/ProfilerStats/`: profiler statistics UI and timeline adapters.
- `Features/TimelineScope/`: ScottPlot timeline-scope control, plotting generators, and detail regions.

## Placement Rules

- New trace file importers should start in `ProfilerStudyCore` and expose UI/MCP-neutral query contracts.
- MCP-only schemas and tool dispatch stay under `ProfilerStudy.McpServer/Tools`.
- Avalonia view code stays under `ProfilerStudy.Avalonia/Features` or `Controls`; it should not depend on MCP server types.
- Legacy WinForms changes should stay under `ProfilerStudy/LegacyWinForms` unless extracting UI-neutral behavior into `ProfilerStudyCore`.
