# Repository Guidelines

## Project Structure & Module Organization

This is a Visual Studio 2022 C# Windows Forms profiler UI targeting `.NET Framework 4.7.2`. The root solution is `ProfilerForStudy.sln`.

- `ProfilerStudy/` contains the legacy WinForms application under `LegacyWinForms/`.
- `ProfilerStudyCore/` contains core session, packet, transport, timing, and data model logic grouped by feature.
- `ProfilerStudy.Avalonia/` contains the cross-platform Avalonia shell, reusable controls, and feature-grouped UI analyzers.
- `ProfilerCanvas/` and `CanvasDataGrid/` contain reusable editor, canvas, and grid controls.
- `CoreUtils/` contains shared utility types under `SCLCoreCLR`.
- `docker/` contains docking and MDI UI infrastructure.
- `tools/` and checked-in `.exe` files support source navigation and profiler demos.
- `third_party/ScottPlot/` is a source-level submodule for the customized ScottPlot used by Avalonia profiler controls.
- `docs/Design/Features/` contains feature design documents. Start with `docs/Design/Features/README.md`; `docs/Design/Features/Controls/` contains per-control-family WinForms/Avalonia alignment documents.

## Build, Test, and Development Commands

Use Windows with Visual Studio 2022 or matching .NET SDK/MSBuild tooling.

```powershell
dotnet restore ProfilerForStudy.sln
dotnet build ProfilerForStudy.sln -c Debug
dotnet build ProfilerForStudy.sln -c Release
```

`dotnet restore` restores NuGet packages. `dotnet build` compiles all projects. To run locally, start `ProfilerStudy` from Visual Studio or execute the built `ProfilerStudy.exe`.

## Coding Style & Naming Conventions

Use C# 11 syntax only where compatible with `net472`. Follow the existing style: braces on new lines, project-local indentation preserved, PascalCase for types and public members, camelCase for locals and parameters, and event handler delegates named with a `Handler` suffix. Keep namespaces and folders aligned with `ProfilerStudy`, `SCL`, and `SCLCoreCLR`.

## Testing Guidelines

There is no dedicated test project. Before submitting changes, build the full solution and manually exercise affected UI paths in `ProfilerStudy`. Existing lightweight checks appear in `ProfilerStudy/LegacyWinForms/Tests.cs`; keep new diagnostics similarly scoped unless adding a formal test project. Name verification helpers after the behavior they validate.

## Commit & Pull Request Guidelines

Recent history uses short, imperative messages prefixed with `refactor:`, for example `refactor: fix for mac open files`. Keep commits focused and use a clear prefix such as `fix:`, `refactor:`, or `docs:`.

Pull requests should include a concise description, the affected projects or UI areas, build/test evidence, and screenshots or recordings for visible UI changes. Link related issues when available. Do not commit local `.csproj.user`, `.vs/`, or machine-specific settings changes.

## Agent-Specific Instructions

Preserve legacy behavior unless the task explicitly calls for a redesign. Avoid broad refactors across UI projects when a targeted change in one module is sufficient. Treat checked-in binary tools as project assets and do not replace them without a clear reason.

Before changing a subfeature, update the corresponding design document first, then implement the code. Feature design documents under `docs/Design/Features/` should be written in Chinese and should describe behavior contracts, data/state ownership, WinForms/Avalonia alignment, and verification, not just list files. For Core behavior, use `docs/Design/Features/core-foundation.md`. For WinForms shell changes, use `docs/Design/Features/legacy-winforms-shell.md`. For Avalonia shell changes, use `docs/Design/Features/avalonia-shell.md`. For shared/basic UI controls, update the relevant per-control-family document under `docs/Design/Features/Controls/` so WinForms and Avalonia stay aligned.

Avalonia controls that use ScottPlot must reference the customized source projects from `third_party/ScottPlot/`, not the `ScottPlot.Avalonia` NuGet package. When timeline, frame graph, profiler stats, or TimelineScope behavior needs plotting changes, prefer targeted changes in the submodule plus the relevant control-family design document.

For Tracy protocol work, keep the C# implementation structurally aligned with the Tracy C++ SDK/viewer instead of hand-maintaining large offset tables or ad hoc decode/encode logic. Live Tracy queue events and `.tracy` file sections should prefer `StructLayout(Pack = 1)`, `Marshal.SizeOf<T>()`, explicit protocol structs, or equivalent structured binary readers that mirror the C++ definitions. When a Tracy C++ struct, enum, section, or wire event changes, update the corresponding C# protocol struct and size/field mapping together, and verify against the Tracy viewer/capture flow before adding broader query behavior.

For local PC live-capture validation, use port `8428` for FramePro / `study` protocol targets and port `8086` for Tracy / `tracy` protocol targets. Do not treat `8428` as a Tracy endpoint.

After each code change that successfully builds, automatically create a focused git commit for the verified changes before reporting completion. Include the build evidence in the response. Do not include unrelated dirty or untracked files in that commit.
