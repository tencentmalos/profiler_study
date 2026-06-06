# ProfilerStudy.Avalonia Release Checklist

This checklist tracks the first cross-platform Avalonia client release gate. It is intended to be run before sharing a build with users or testing on a non-development machine.

## Build gates

Run from the repository root:

```bash
/Users/bytedance/.dotnet/dotnet build ProfilerStudy.Avalonia/ProfilerStudy.Avalonia.csproj -c Debug
ProfilerStudy.Avalonia/bin/Debug/net8.0/ProfilerStudy.Avalonia --smoke-test
```

Expected result:

- Build exits with code `0`.
- Smoke test exits with code `0`.
- Existing warnings are acceptable if they are unchanged and are not introduced by the release change.

## Publish profiles

Publish profiles are available under `ProfilerStudy.Avalonia/Properties/PublishProfiles/`:

- `win-x64.pubxml`
- `osx-x64.pubxml`
- `osx-arm64.pubxml`

Run as needed:

```bash
/Users/bytedance/.dotnet/dotnet publish ProfilerStudy.Avalonia/ProfilerStudy.Avalonia.csproj -p:PublishProfile=win-x64
/Users/bytedance/.dotnet/dotnet publish ProfilerStudy.Avalonia/ProfilerStudy.Avalonia.csproj -p:PublishProfile=osx-x64
/Users/bytedance/.dotnet/dotnet publish ProfilerStudy.Avalonia/ProfilerStudy.Avalonia.csproj -p:PublishProfile=osx-arm64
```

Current profiles are framework-dependent and keep trimming, single-file publish, and ReadyToRun disabled to avoid Avalonia, ScottPlot, and native resource issues during the first release.

## macOS app bundle

After publishing a macOS runtime, package it as a `.app` bundle:

```bash
scripts/avalonia/package_macos_app.sh osx-arm64
scripts/avalonia/package_macos_app.sh osx-x64
```

The script creates `ProfilerStudy.Avalonia/bin/Release/net8.0/publish/<rid>/ProfilerStudy.app` with:

- `Contents/Info.plist`
- `Contents/MacOS/ProfilerStudy.Avalonia`
- `Contents/Resources/AppIcon.icns`

The bundle is not signed or notarized. Gatekeeper behavior must be checked before external distribution.

## Manual smoke checklist

Use at least one real `.profiler`, `.profiler_recording`, or `.profiler_dump` file.

1. Start the application normally.
2. Open a profiler file with `Open`.
3. Confirm session summary updates: frame count, frame time summary, thread count, and source file name.
4. Confirm the top Skia frame timeline renders frame bars and supports wheel zoom and pan.
5. Click `Find Slowest` and confirm the selected frame marker moves and the viewport centers around the slow frame.
6. Confirm the ScottPlot profiler stats panel follows the selected viewport.
7. Filter `Scope Hotspots` by a known scope name and confirm the result count changes without losing total-time ordering.
8. Click sortable headers in `Scope Hotspots` and confirm scope, total time, call count, average, and max columns reorder rows.
9. Select a frame and confirm `Selected Frame Scopes` updates with thread, scope, duration, and source columns.
10. Confirm the selected-frame flame chart renders colored scope bars above the scope table.
11. Click sortable headers in `Selected Frame Scopes` and confirm thread, scope, start, duration, and source columns reorder rows.
12. Select a frame with custom stats and confirm `Selected Frame Counters` shows graph, counter, value, count, and unit.
13. Click sortable headers in `Selected Frame Counters` and confirm graph, counter, value, count, and unit columns reorder rows.
14. If source paths exist locally, click `Open` in `Selected Frame Scopes` and confirm the configured editor or system opener launches.
15. If captured source paths differ from local paths, set `Captured root` and `Local root`, then confirm `Open` resolves the mapped local file.
16. Close and reopen the app, then confirm the file appears in `Recent files` and `Open Recent` reloads it.
17. Start the application with a profiler file path as the first non-option argument and confirm it loads on startup.

## Source launch behavior

Source launch uses this order:

1. `PROFILER_STUDY_SOURCE_VIEWER` environment variable if set.
2. `code -g file:line`.
3. Platform file opener: `open` on macOS, shell execute on Windows, `xdg-open` on Linux.

If the source path is missing or does not exist, the status bar must show an actionable error instead of throwing.

## Known degraded or deferred features

These are intentionally not first-release complete:

- No WinForms-style dockable window system.
- No full settings UI; only recent files are persisted.
- macOS `.app` bundles are not signed or notarized.
- No Android adb capture workflow in the Avalonia UI.
- No ETL/context-switch capture UI.
- No recording player or demo simulator replacement.
- No full thread timeline/flame graph parity with WinForms; current first-release detail is a selected-frame flame chart plus scope table.
- No virtualized DataGrid for scope and counter tables; current tables are sortable `ItemsControl` projections.
- Publish profiles are framework-dependent; non-development machines need a compatible .NET runtime installed.

## First-release acceptance

The Avalonia client is ready for a first user trial when:

- Debug build and smoke test pass.
- At least one publish profile has been published and launched on its target platform.
- A real profiler file can be opened from file picker, recent file, and startup file argument.
- The user can complete the analysis loop: open file, locate slow frame, inspect scope hotspots, inspect selected-frame scopes/counters, and attempt source launch.
