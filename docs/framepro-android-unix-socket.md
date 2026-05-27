# FramePro Android Unix Socket 连接改造记录

## 背景

Azahar / Foundation 在 Android 上运行时，FramePro profiler 不再适合只按 TCP 端口暴露。当前目标是对齐 RemoteMonitor 的使用方式：运行时在设备侧创建固定路径的 Unix socket，桌面端按 Debug / Release 选择固定 socket 路径，再使用 `adb forward tcp:<local_port> localfilesystem:<socket_path>` 转发到本地 TCP 端口，最后由 FramePro legacy 客户端按普通 TCP 连接读取数据。

本记录只覆盖 FramePro / profiler_legacy 连接链路。Editor 相关事项后续在独立命令行中处理。

## 当前运行时约定

Android 侧 FramePro endpoint 使用 `localfilesystem` 类型，而不是 `localabstract` 或固定 TCP 端口。运行时日志会以 `framepro` tag 输出创建 socket、连接、发送统计和 adb forward 提示，便于在 logcat 中确认状态。

当前设备侧 socket 路径固定为：

- Debug: `/data/user_de/0/org.azahar_emu.azahar.debug/files/framepro`
- Release: `/data/user_de/0/org.azahar_emu.azahar/files/framepro`

## profiler_legacy 改造内容

### 普通 Connect 保持原逻辑

`ProfilerStudy/FramePro/ConnectSettingsDialog.cs` 已恢复为传统 IP / Port 设置入口，只保留：

- `8428 (PC)`
- `4420 (XBox)`

这里不再混入 Android socket 枚举，也不再把 `adb:framepro` 或 `localfilesystem:*` 塞进原有 Port 下拉框。

### 新增 Android 工具栏按钮

`ProfilerStudy/FramePro/MainForm.cs` 新增独立工具栏按钮 `Android`。该按钮不复用普通 `Connect` 的设置对话框，而是进入专用 Android 连接流程：

1. 打开 Android socket 选择对话框。
2. 让用户选择 Debug 或 Release。
3. 按固定 socket 路径执行 adb forward。
4. 使用 `127.0.0.1:<allocated_port>` 创建普通 FramePro session。

普通 `Connect` 失败时仍提示原 IP / Port 相关错误；Android 连接失败时显示 adb forward / TCP connect 的具体错误文本。

### 新增 AndroidConnectDialog

`ProfilerStudy/FramePro/AndroidConnectDialog.cs` 是专用 Android 连接对话框。对话框只提供 Debug / Release 两个固定选项，不再枚举设备侧 socket。

### 新增 AdbSocketDiscovery

`ProfilerStudyCore/FramePro/AdbSocketDiscovery.cs` 集中处理 adb 相关逻辑：

- 解析和定位 adb 可执行文件。
- 提供 Debug / Release 固定 socket endpoint。
- 分配本地可用 TCP 端口。
- 执行和记录 adb 命令结果。

当前只保留 `localfilesystem:<socket_path>` 路径。`localabstract` socket 在 Android 上不可通过当前路径可靠查询，桌面端不再解析或 forward `localabstract:*` endpoint。分配本地端口时会避开 FramePro 默认 TCP 端口 `8428`。

### Session 分流

`ProfilerStudyCore/FramePro/Session.cs` 现在分为两条入口：

- `Connect()`：普通 IP / Port TCP 连接。
- `ConnectToAndroid(string adbEndpoint)`：Android 专用流程。

Android 流程会：

1. 校验传入的 endpoint 必须是 `localfilesystem:*`。
2. 执行 `adb forward tcp:<local_port> <adb_endpoint>`。
3. 记录 `adb forward --list` 输出，辅助确认映射是否存在。
4. 连接 `127.0.0.1:<local_port>`。
5. session 关闭时执行 `adb forward --remove tcp:<local_port>` 清理映射。

## 推荐手工验证流程

1. 启动 Android 端应用，并确认运行时 logcat 中有 `framepro` tag 的 socket 创建日志。
2. 打开 profiler_legacy，点击工具栏 `Android`。
3. 在弹出的对话框中选择 Debug 或 Release。
4. 点击 Connect。
5. 如果需要命令行确认，可检查对应固定 socket 路径是否存在。
6. 如果仍然没有数据帧，优先检查：
   - Android 运行时是否持续驱动 Foundation ModuleMgr / FramePro 模块。
   - logcat 中每 10 秒发送统计是否增长。
   - profiler_legacy 输出窗口中 `ADB forward command`、`ADB forward --list` 和 `TCP connected` 是否出现。
   - `adb forward --list` 是否包含 `tcp:<local_port> localfilesystem:<socket_path>`。

## 已验证项

本次 profiler_legacy 侧改造已通过本地编译验证：

```sh
dotnet build C:\workspace\profiler_legacy\ProfilerStudy\ProfilerStudy.csproj -c Debug -nologo -v minimal -p:OutputPath=C:\workspace\profiler_legacy\_codex_build\ProfilerStudy\
```

结果为 `0 errors`。构建中仍存在项目原有 warning，未在本次调整中处理。

## 注意事项

- 不要再把 Android socket endpoint 写入普通连接设置的 Port 字段。
- Android 对话框只提供 Debug / Release 固定路径，不再发现多个 `framepro` socket。
- 当前实现面向 adb 单设备默认路径；多设备场景后续可扩展为带 serial 的 adb 调用。
- `ProfilerStudy/ProfilerStudy.csproj.user` 属于本地 IDE 状态文件，当前不作为本次连接流程改造的一部分。
