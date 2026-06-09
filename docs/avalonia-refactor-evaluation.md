# Avalonia 全量重构评估

## 目标判断

如果目标是让当前 profiler 在 Windows 和 macOS 上都以原生桌面应用运行，Avalonia 是比继续扩展 WinForms 更合理的方向。Avalonia 官方支持 Windows、macOS、Linux 等桌面平台，并且默认使用 Skia 渲染，适合本项目大量自绘时间轴、图表、Grid 和交互视图的场景。

但这不是“迁移项目文件 + 替换控件”的工作。当前代码把业务逻辑、WinForms 控件、绘制、窗口管理、外部工具调用和平台 API 混在一起。使用 Avalonia 重构基本等同于“保留协议/数据/算法，重建桌面客户端”。

## 总体复杂度

**复杂度：高。**

主要原因：

- `ProfilerStudy/LegacyWinForms/*` 是核心 UI，包含大量 WinForms 控件、自绘、事件处理、滚动、选择、浮窗和对话框。
- `CanvasDataGrid/`、`ProfilerCanvas/`、`docker/` 都是 WinForms 控件库，不能直接在 Avalonia 中复用。
- `CoreUtils/` 和 `ProfilerStudyCore/` 也混入了 `System.Windows.Forms`、Win32 P/Invoke、Windows 路径和进程调用。
- `Profiler_GameSimulator.exe`、`Profiler_RecordingPlayer.exe`、`VisualStudioOpenFileAndLine.exe` 是 Windows 二进制，macOS 原生版需要替代或降级。

建议按 **6-9 个月、2-3 名熟悉 C#/桌面 UI 的工程师** 估算一个可用的原生双平台版本。如果只做最小可用查看器，范围压缩到 session 加载、时间轴、scope/thread 查看和源码跳转，可能需要 **8-12 周**。

## 推荐架构

采用“双轨迁移”：

- 保留现有 `ProfilerStudy` 作为 Windows legacy 客户端。
- 新增 Avalonia 客户端，例如 `ProfilerStudy.Avalonia`。
- 新增平台无关核心库，例如 `ProfilerStudy.Core`。
- 逐步把 `ProfilerStudyCore` 中可复用逻辑迁入 Core，直到 Avalonia 客户端只依赖 Core 和平台服务接口。

目标结构：

```text
ProfilerStudy.Legacy/        # 可选：现有 WinForms 项目逐步改名或保留
ProfilerStudy.Core/          # 协议、Session、Packet、模型、文件 IO、计算逻辑
ProfilerStudy.Platform/      # 源码跳转、外部工具、剪贴板、文件选择、路径映射接口
ProfilerStudy.Avalonia/      # Avalonia UI、ViewModel、主题、窗口
ProfilerStudy.Tests/         # Core 和平台服务测试
```

## 可控实现路径

### 阶段 0：范围冻结

先明确第一版 Avalonia 不追求功能完全等价。建议第一版只包含：

- 打开/加载已有 profiler recording 或 session 文件。
- 展示 timeline、threads/scopes、custom stats 的核心只读视图。
- 基础搜索、选中、缩放、滚动。
- VSCode/CLion 源码跳转。
- Windows/macOS 构建和冒烟测试。

暂缓：

- 自定义 Dock 系统完全复刻。
- 内嵌所有设置窗口。
- Windows ETL/context switch 采集。
- demo recorder/player 原生替代。

### 阶段 1：拆出真正跨平台的 Core

优先处理非 UI 逻辑：

- Packet 读取、session 状态、时间换算、scope/thread/custom stat 数据结构。
- 文件保存/加载。
- 源码路径解析和 source root 匹配。

必须移除：

- `System.Windows.Forms`
- `Application.ExecutablePath`
- `MessageBox`
- `Kernel32.dll`、`user32.dll`、`ntdll.dll`
- 硬编码 Windows 路径分隔符

替代接口：

- `IClock`：用 `Stopwatch.GetTimestamp()`。
- `IAppPaths`：提供 config/cache/log 目录。
- `ILogger`：替代 UI 直接日志。
- `IPathMapper`：处理 Windows/macOS/Wine/source root 映射。

这一阶段完成后，即使 UI 还没迁移，也能用测试保证核心数据行为稳定。

### 阶段 2：建立 Avalonia 外壳

创建 Avalonia MVVM 项目，先做最小可运行客户端：

- `MainWindow`
- 左侧 session/file 区域
- 中央 timeline 占位控件
- 底部 log/status 区域
- 打开文件命令

此阶段只验证：

- Windows/macOS 都能启动。
- 能调用 Core 读取样本。
- ViewModel 不依赖平台 API。

### 阶段 3：迁移绘图视图

优先迁移价值最高、依赖最明确的视图：

1. 时间轴和 frame graph。
2. threads/scopes 列表。
3. scope 详情和选中范围。
4. custom stat 曲线。
5. cores/context switch 只读展示。

Avalonia 中建议使用自定义控件 + `DrawingContext`，必要时直接使用 Skia 级别渲染。不要逐行翻译 WinForms `OnPaint`，应先定义视图模型和坐标转换，再重建绘制层。

### 阶段 4：替换 Grid 和 Dock

`CanvasDataGrid` 和 `docker` 是最大风险点之一。

建议：

- Grid 第一版使用 Avalonia `DataGrid` 或轻量虚拟化列表，先满足数据查看。
- 对性能敏感的 scope/thread 表格再做自定义虚拟化控件。
- Dock 第一版不复刻拖拽 Dock，只提供固定布局、Tab 和可折叠面板。
- 等核心视图稳定后，再评估是否引入 Dock 库或自研。

这样可以避免在第一阶段陷入窗口系统和拖拽细节。

### 阶段 5：平台服务补齐

实现 Windows/macOS 各自服务：

- `ISourceViewerLauncher`
  - Windows：`code --goto file:line`、`clion64.exe --line line file`。
  - macOS：`code --goto file:line`、`open -a` 或 CLI 工具。
- `IExternalToolRunner`
  - Windows 保留现有 `.exe`。
  - macOS 对不可用工具显示“此平台暂不支持”。
- `IClipboardService`
- `IFileDialogService`
- `IAppPaths`

所有平台差异只允许出现在服务实现层，不要重新散落到 ViewModel 或 Core。

## 风险分级

| 模块 | 风险 | 原因 | 控制方式 |
| --- | --- | --- | --- |
| Session/Packet/Core 数据 | 中 | 逻辑多但可测试 | 先写样本回归测试 |
| Timeline/Graph 自绘 | 高 | 交互和性能要求高 | 先只读，再补交互 |
| CanvasDataGrid 替代 | 高 | 表格数据量和交互复杂 | 先用固定列/虚拟列表 |
| Dock 系统 | 高 | 当前 `docker/` 深度绑定 WinForms | 第一版固定布局 |
| Source jump | 中 | 路径映射复杂 | 独立服务 + 用例测试 |
| Context switch/ETL | 高 | Windows ETL/SDK 相关 | 原生版第一版只读或禁用采集 |
| 打包发布 | 中 | macOS 签名、公证、runtime id | 后期独立处理 |

## 里程碑建议

### M1：Core 可测试化

交付：

- 新 `ProfilerStudy.Core`
- 样本文件加载测试
- 路径映射测试
- 无 WinForms 依赖

验收：

- Windows/macOS `dotnet test` 都通过。

### M2：Avalonia 最小查看器

交付：

- Avalonia 主窗口
- 打开文件
- Session 基础信息展示
- 日志面板

验收：

- Windows/macOS 均可启动并加载同一样本。

### M3：核心分析视图

交付：

- Timeline 基础绘制
- Threads/scopes 列表
- 选中范围和详情
- 源码跳转

验收：

- 能完成最常用 profiling 分析闭环。

### M4：交互和性能

交付：

- 大 session 虚拟化
- 缩放、滚动、hover、选择优化
- custom stats 和 cores 视图

验收：

- 大样本打开、导航、缩放不卡顿到不可用。

### M5：发布级补齐

交付：

- 设置迁移
- macOS app bundle
- Windows installer 或 zip publish
- 平台功能降级提示

验收：

- 能给真实用户试用，不依赖开发机环境。

## 推荐策略

最可控的路径不是“一次性重写全部 WinForms UI”，而是：

1. 先把 Core 从 UI 和 Windows API 中切出来。
2. 用 Avalonia 做一个只读最小客户端。
3. 用真实 profiler 样本验证数据一致性。
4. 只迁移高频视图。
5. Dock、复杂设置、采集工具最后处理。

如果项目必须短期在 macOS 可用，应并行保留 Wine 兼容方案；Avalonia 原生版按新客户端推进，不要在旧 WinForms 项目里做大规模跨平台补丁。

## 参考

- Avalonia Docs: Platform-specific guides - https://docs.avaloniaui.net/docs/platform-specific-guides/linux
- Avalonia Docs: Graphics and animations - https://docs.avaloniaui.net/docs/guides/graphics-and-animation/graphics-and-animations
- Avalonia Docs: Custom rendering - https://docs.avaloniaui.net/docs/graphics-animation/custom-rendering
- Avalonia Docs: Architecture - https://docs.avaloniaui.net/docs/fundamentals/architecture
