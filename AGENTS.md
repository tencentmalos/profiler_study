# Repository Guidelines

## Project Structure & Module Organization

This is a Visual Studio 2022 C# Windows Forms profiler UI targeting `.NET Framework 4.7.2`. The root solution is `ProfilerForStudy.sln`.

- `ProfilerStudy/` contains the main WinForms application and `FramePro` UI views.
- `ProfilerStudyCore/` contains core FramePro session, packet, timing, and data model logic.
- `ProfilerCanvas/` and `CanvasDataGrid/` contain reusable editor, canvas, and grid controls.
- `CoreUtils/` contains shared utility types under `SCLCoreCLR`.
- `docker/` contains docking and MDI UI infrastructure.
- `tools/` and checked-in `.exe` files support source navigation and profiler demos.

## Build, Test, and Development Commands

Use Windows with Visual Studio 2022 or matching .NET SDK/MSBuild tooling.

```powershell
dotnet restore ProfilerForStudy.sln
dotnet build ProfilerForStudy.sln -c Debug
dotnet build ProfilerForStudy.sln -c Release
```

`dotnet restore` restores NuGet packages. `dotnet build` compiles all projects. To run locally, start `ProfilerStudy` from Visual Studio or execute the built `ProfilerStudy.exe`.

## Coding Style & Naming Conventions

Use C# 11 syntax only where compatible with `net472`. Follow the existing style: braces on new lines, project-local indentation preserved, PascalCase for types and public members, camelCase for locals and parameters, and event handler delegates named with a `Handler` suffix. Keep namespaces and folders aligned with `FramePro`, `SCL`, and `SCLCoreCLR`.

## Testing Guidelines

There is no dedicated test project. Before submitting changes, build the full solution and manually exercise affected UI paths in `ProfilerStudy`. Existing lightweight checks appear in `ProfilerStudy/FramePro/Tests.cs`; keep new diagnostics similarly scoped unless adding a formal test project. Name verification helpers after the behavior they validate.

## Commit & Pull Request Guidelines

Recent history uses short, imperative messages prefixed with `refactor:`, for example `refactor: fix for mac open files`. Keep commits focused and use a clear prefix such as `fix:`, `refactor:`, or `docs:`.

Pull requests should include a concise description, the affected projects or UI areas, build/test evidence, and screenshots or recordings for visible UI changes. Link related issues when available. Do not commit local `.csproj.user`, `.vs/`, or machine-specific settings changes.

## Agent-Specific Instructions

Preserve legacy behavior unless the task explicitly calls for a redesign. Avoid broad refactors across UI projects when a targeted change in one module is sufficient. Treat checked-in binary tools as project assets and do not replace them without a clear reason.

After each code change that successfully builds, automatically create a focused git commit for the verified changes before reporting completion. Include the build evidence in the response. Do not include unrelated dirty or untracked files in that commit.
