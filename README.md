# ScreenMover

Windows 11 多屏窗口管理工具，使用 C# / WinUI 3 开发。目前为 **Demo / 预发布版本**。

## 下载运行

在 [Releases](https://github.com/lbyh0116/ScreenMover/releases) 下载 Windows x64 压缩包，完整解压后运行 `ScreenMover.exe`。请保留整个文件夹，不能只复制 exe。

发布包包含 .NET 与 Windows App SDK 运行时，不需要 MSIX 注册。当前面向 Windows 11 x64；隐藏到虚拟桌面功能适配系统构建 26100/26200，已在 26200 上测试。

## 功能

- 屏幕磁贴横向排列，显示静态截图；未启用屏幕以虚线标记。
- 窗口支持列表 / 相册布局、截图预览、图标回退、搜索和多选。
- 拖到屏幕磁贴移动窗口，支持批量移动及撤销上一次移动。
- 选中窗口以主题色边框高亮；拖拽调整顺序，刷新及重启 ScreenMover 后保留仍打开窗口的顺序。
- 隐藏窗口到以软件名命名的新虚拟桌面并最小化，可通过“已隐藏 / 找回”恢复。
- 托盘快捷面板：左侧窗口图标与标题，右侧屏幕预览；拖到其他屏幕移动，拖到所在屏幕隐藏。长标题保留首尾。
- 设置支持主题、布局、截图、刷新间隔、移动留白、窗口尺寸记忆与托盘常驻。

默认在启动、重新激活、每分钟及 F5 时更新截图。截图为静态采样，不录屏；普通窗口事件只更新列表。默认关闭主窗口后留在托盘，右键托盘图标选择“退出”可彻底退出。

### 快捷键

| 操作 | 快捷键 |
| --- | --- |
| 刷新列表和截图 | F5 |
| 搜索 | Ctrl+F |
| 全选窗口 | Ctrl+A |
| 多选 / 范围选择 | Ctrl / Shift + 单击 |
| 激活窗口 | 双击 |
| 清空搜索 / 收起快捷面板 | Esc |

## 从源码构建

需要 Windows 11 x64、.NET SDK **10.0.400**（允许同版本系列补丁升级）和 NuGet 网络访问。虚拟桌面辅助程序使用 Windows 自带的 .NET Framework C# 编译器构建。

```powershell
git clone https://github.com/lbyh0116/ScreenMover.git
cd ScreenMover
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

结果在 `dist\ScreenMover\ScreenMover.exe`。开发运行：

```powershell
dotnet run --project ScreenMover
```

固定依赖：Microsoft.WindowsAppSDK 2.5.1、Microsoft.Windows.SDK.BuildTools 10.0.28000.2705。目标框架为 `net10.0-windows10.0.26100.0`，非打包 x64 应用，使用 PerMonitorV2 DPI。

## 验证

```powershell
# 构建、发布并启动独立测试窗口执行回归
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test
```

测试会创建自己的窗口与临时虚拟桌面，并在正常完成时清理。诊断输出包含本机显示器信息和窗口标题，分享日志前请自行检查。

已验证几何换算、普通/最小化/最大化窗口移动与撤销、隐藏与找回、设置与排序持久化。实际机器目前只有一块启用的显示器，**真实跨屏、混合 DPI、多屏热插拔及管理员窗口权限场景仍待验收**。

## 本地数据与已知限制

- 数据保存于 `%LOCALAPPDATA%\ScreenMover`：设置、窗口顺序、隐藏恢复记录、操作记录及异常日志。操作记录可能包含窗口标题。
- 截图和软件图标仅保存在运行内存中，不保存截图文件、不上传。
- 窗口关闭后重新创建视为新窗口，追加到排序末尾。
- 最小化窗口优先保留历史截图，无截图时可显示软件图标。受保护、GPU 绘制或无响应的窗口可能无法截图。
- 隐藏功能依赖系统内部虚拟桌面接口，Windows 更新可能造成兼容问题。找回窗口不会自动删除虚拟桌面。
- 应用不会启用、禁用或修改显示器配置。部分应用的权限、最小尺寸和前台激活限制可能阻止操作。

## 第三方代码与授权

虚拟桌面辅助代码来自 [MScholtes/VirtualDesktop](https://github.com/MScholtes/VirtualDesktop) 1.21（Windows 11 24H2 版本），由 Markus Scholtes 提供，遵循 MIT 许可，原许可保留在 [VirtualDesktop-LICENSE.txt](ScreenMover/Helpers/VirtualDesktop-LICENSE.txt)。NuGet 依赖遵循各自的许可。

本项目当前先公开 Demo 源码，尚未为原创部分选定开源许可证；公开可见不代表已授予开源许可。
