# Mac/Windows 跨平台改造可行性分析

## 结论

当前实现不能直接改成“原生 macOS + Windows 共用”的小改版本。主程序和大部分 UI 库基于 WinForms、`.NET Framework 4.7.2`、`Microsoft.NET.Sdk.WindowsDesktop`、`System.Drawing` 和 Win32 P/Invoke。Microsoft 官方文档也明确 `.NET Framework` 是 Windows-only，WinForms/WPF 在现代 .NET 中也只在 Windows 上受支持。

可以做成两种目标：

1. **兼容运行版**：继续使用现有 WinForms 程序，在 macOS 通过 Wine/CrossOver/包装脚本运行。成本低，适合短期交付。
2. **原生跨平台版**：抽离核心协议/数据模型，重写 UI 到 Avalonia 或 MAUI。成本高，但是真正的 Windows/macOS 双平台方案。

推荐先做兼容运行版，稳定源代码跳转、路径映射和打包；再评估是否投入原生 UI 重写。

## 当前平台耦合点

### 项目和框架

- `ProfilerStudy/ProfilerStudy.csproj`、`ProfilerCanvas/ProfilerCanvas.csproj`、`CanvasDataGrid/CanvasDataGrid.csproj`、`docker/docker.csproj`、`CoreUtils/CoreUtils.csproj` 都使用 `Microsoft.NET.Sdk.WindowsDesktop`、`UseWindowsForms=True`、`TargetFramework=net472`。
- `ProfilerStudyCore/ProfilerStudyCore.csproj` 虽然是普通 SDK 项目，但仍引用 `System.Windows.Forms`，并依赖 `CoreUtils`。
- `ProfilerStudy` 是 `WinExe`，入口在 `ProfilerStudy/LegacyWinForms/Program.cs`，直接调用 `Application.Run(new MainForm(...))`。

### Win32 和 Windows 专用 API

- `CoreUtils/SCLCoreCLR/Time.cs` 使用 `Kernel32.dll` 的 `QueryPerformanceCounter/Frequency`，macOS 原生不可用。
- `CoreUtils/SCLCoreCLR/ControlTaskDispatcher.cs` 使用 `user32.dll` `PostMessage`。
- `docker/Docker/DockManager.cs`、`docker/Docker/NativeMethods.cs`、`ProfilerStudy/LegacyWinForms/HoverBox.cs` 使用 `user32.dll`。
- `ProfilerStudyCore/Infrastructure/Platform.cs` 通过 `ntdll.dll` 的 `wine_get_version` 判断 Wine，这说明当前 macOS 方案已经偏向 Wine，而非原生 macOS。

### 外部工具和路径

- `ProfilerStudy/LegacyWinForms/Utils.cs` 打开源码时在 Windows 下调用 `cmd.exe /C code`、`clion64.exe` 或 `VisualStudioOpenFileAndLine.exe`。
- Wine 分支会调用 `open_source_vscode.sh`、`open_source_clion.sh`，这是当前最接近 macOS 可用的部分。
- `ProfilerStudy/LegacyWinForms/MainForm.cs` 启动 `Profiler_GameSimulator.exe`、`Profiler_RecordingPlayer.exe`，这两个是 Windows 二进制。
- 多处代码假设 Windows 路径，例如 `\\cache\\`、`platform-tools\\adb.exe`、`FramePro\\FramePro.etl`。

## 方案 A：Wine 兼容运行版

### 目标

保持现有 WinForms 代码和 `.NET Framework 4.7.2`，让同一套 Windows 程序在 Windows 原生运行，在 macOS 通过 Wine 运行。

### 改造内容

1. 将平台检测从 `PlatformTool.IsRunOnWine()` 扩展为 `RuntimeEnvironment` 类，提供 `IsWindowsNative`、`IsWine`、`IsMacHostViaWine`。
2. 抽出源码打开逻辑，新增 `ISourceViewerLauncher`：
   - Windows：使用 `code --goto`、`clion64.exe --line`、`VisualStudioOpenFileAndLine.exe`。
   - Wine/macOS：调用 bundled `.sh`，并统一把 Wine 路径转换成 macOS 路径。
3. 集中处理路径映射：
   - 支持 `Z:\Users\...` 到 `/Users/...`。
   - 保留 `settings.SourceRoots` 的搜索逻辑。
   - 所有新代码使用 `Path.Combine`，减少硬编码 `\\`。
4. 对 Windows-only 功能做降级：
   - `Profiler_GameSimulator.exe`、`Profiler_RecordingPlayer.exe` 在 Wine 下可尝试运行，失败时显示清晰错误。
   - ETL/Win32 context switch 相关功能只在 Windows 或目标 SDK 支持时启用。
5. 增加 macOS 包装脚本：
   - 启动 Wine。
   - 设置 `WINEPREFIX`。
   - 复制/定位 `open_source_*.sh`。
   - 输出日志到用户目录。

### 优点

- 改动集中，风险较低。
- 能复用现有 UI、Dock、Grid 和绘图代码。
- 对已有 Windows 用户影响最小。

### 局限

- 不是原生 macOS 应用。
- UI 字体、DPI、剪贴板、文件对话框和外部进程行为可能与 Windows 不一致。
- 仍依赖 Wine 对 WinForms、GDI+ 和 Win32 调用的兼容程度。

## 方案 B：原生跨平台版

### 目标

将核心逻辑迁移到现代 .NET，UI 使用跨平台框架，在 Windows 和 macOS 上原生构建运行。

### 推荐技术路线

优先考虑 **Avalonia UI**。本项目大量自绘图表、Grid、Dock、Timeline 和 profiler 视图更接近桌面工具，Avalonia 的自绘、布局和跨平台窗口模型比继续迁移 WinForms 更合适。MAUI 也支持 Windows/macOS，但对这种复杂桌面分析工具的自绘和 Dock 体验改造成本未必更低。

### 分阶段计划

1. **拆 Core**
   - 新建 `ProfilerStudy.Core`，目标 `net8.0` 或当前 LTS。
   - 移入协议、packet、session、统计、文件读写、路径解析等非 UI 逻辑。
   - 移除 `System.Windows.Forms`、`Application.ExecutablePath`、`MessageBox` 等 UI 依赖。
2. **封装平台服务**
   - `IClock` 替换 `Kernel32.QueryPerformanceCounter`，默认实现用 `Stopwatch.GetTimestamp()`。
   - `IUiDispatcher` 替换 `PostMessage`。
   - `IFileDialogService`、`IClipboardService`、`ISourceViewerLauncher` 替换 WinForms 直接调用。
   - `IExternalToolRunner` 管理 simulator、recording player、ADB 和 source viewer。
3. **迁移 UI**
   - 先实现只读 session 加载、Timeline、Scopes、Threads、Cores 基础视图。
   - 再迁移设置、源码跳转、context switch 展示、Android trace 工作流。
   - Dock 系统不建议逐行移植 `docker/`，应基于目标 UI 框架重建布局。
4. **多目标构建**
   - 保留旧 `ProfilerStudy` 作为 Windows legacy。
   - 新增 `ProfilerStudy.Desktop`，目标 `net8.0`，平台发布 `win-x64`、`osx-arm64`、`osx-x64`。
5. **测试与验证**
   - 给 Core 增加单元测试，覆盖 packet 解析、session 保存/加载、路径映射、时间换算。
   - 在 Windows 和 macOS CI 上分别执行 build/test。
   - 用真实 `.framepro`/recording 样本做回归验证。

### 优点

- 真正原生双平台。
- 可以逐步清理 Win32、路径和外部工具耦合。
- 长期维护性最好。

### 成本和风险

- UI 重写成本高，尤其是 `ProfilerStudy/LegacyWinForms/*`、`CanvasDataGrid/`、`ProfilerCanvas/`、`docker/`。
- 需要重新验证大量交互细节：缩放、滚动、选择、拖拽、Dock、绘图性能。
- 不能一次性替换全部功能，必须分阶段交付。

## 推荐落地顺序

1. 先做 **方案 A**，把现有 Wine/macOS 运行链路产品化。
2. 同时开始抽离无 UI 的 Core，避免继续把平台逻辑写进 `Utils.cs`、`Session.cs`、`CoreSettings.cs`。
3. 只有在确认需要长期维护 macOS 原生体验后，再启动 **方案 B** 的 Avalonia 原生客户端。

## 验收标准

### Wine 兼容版

- Windows 上 `dotnet build ProfilerForStudy.sln -c Release` 通过。
- macOS 通过包装脚本能启动主程序。
- VSCode/CLion 源码跳转在 Windows 和 macOS/Wine 下都能打开正确文件和行号。
- Windows-only 工具不可用时给出明确提示，不导致主程序崩溃。

### 原生跨平台版

- Core 项目不引用 WinForms、WindowsDesktop、`System.Drawing.Common` 的 Windows-only 能力或 Win32 DLL。
- Windows/macOS 都能构建、加载同一个 recording/session 样本。
- 基础 profiler 视图在两个平台上的时间轴、线程、scope、custom stat 数据一致。

## 参考

- Microsoft Learn: Windows Forms overview - https://learn.microsoft.com/dotnet/desktop/winforms/overview/
- Microsoft .NET Framework support policy - https://dotnet.microsoft.com/platform/support/policy/dotnet-framework
- Microsoft .NET target frameworks - https://learn.microsoft.com/dotnet/standard/frameworks
