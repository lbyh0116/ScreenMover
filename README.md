# ScreenMover

Windows 11 多屏窗口管理工具，使用 C# / WinUI 3 开发。目前为 **Demo / 预发布版本**。

## 下载运行

在 [Releases](https://github.com/lbyh0116/ScreenMover/releases) 下载最新 Windows x64 预发布压缩包，完整解压后运行 `ScreenMover.exe`。请保留整个文件夹，不能只复制 exe。

发布包包含 .NET 与 Windows App SDK 运行时，不需要 MSIX 注册。当前面向 Windows 11 x64；隐藏到虚拟桌面功能适配系统构建 26100/26200，已在 26200 上测试。

## 功能

- 屏幕磁贴横向排列，显示静态截图；未启用屏幕以虚线标记。
- 主窗口固定为相册网格，支持截图预览、图标回退、搜索和多选；快捷面板使用图标与标题列表。
- 拖到屏幕磁贴移动窗口，支持批量移动及撤销上一次移动。
- 选中窗口以主题色边框高亮；拖拽调整顺序，刷新及重启 ScreenMover 后保留仍打开窗口的顺序。
- 隐藏窗口到以软件名命名的新虚拟桌面并最小化，可通过“已隐藏 / 找回”恢复。
- 托盘快捷面板：左侧窗口图标与标题，右侧屏幕预览；拖到其他屏幕移动，拖到所在屏幕隐藏。长标题保留首尾。云母面板点击外部自动收起，右上角可打开主界面。
- 设置支持主题、截图、刷新间隔、移动留白、窗口尺寸记忆与托盘常驻。

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

## 2026-10-17 界面修复（待人工验收）

改用 WinUI 原生选中效果；修复尺寸单位，主窗口固定相册网格且列宽随窗口调整，矮窗口使用紧凑显示器预览。快捷面板参考 Twinkle Tray 的常驻行为与面板样式，按托盘所在显示器和任务栏边缘定位，使用系统云母、圆角、主题资源及短过渡；尊重 Windows 动画效果设置。

本轮不使用 computer use 验收。代码和几何测试完成后，请按 [手动验收清单](docs/MANUAL_TEST.md) 检查实际观感与交互。纯逻辑回归可执行 `dotnet run --project Diagnostics -- --pure`，不会启动或操作应用窗口。


## 2026-10-17 交互完善与常驻功能（待人工验收）

- 屏幕、窗口卡片和快捷列表统一主题色选中边框与 8 DIP 圆角；点击空白取消窗口选择。
- 按住缩小/变暗；拖动时使用实际卡片/窗口行渲染图，原位置淡化，多选显示数量。
- 拖到屏幕显示半透明主题色遮罩，预告移动、隐藏、当前屏幕或未启用；取消和移开后清除。
- 快捷面板按内容缩短，移除外圈描边、底栏和设置按钮；右上角打开主界面，操作结果短暂显示。操作说明集中到设置。
- 重复启动会唤醒已有实例，截图和测试进程仍独立运行。
- 设置可选“登录 Windows 后启动到托盘”，默认关闭；使用当前用户 Run 启动项，不需要管理员权限。移动/删除程序前应关闭此选项。

当前本地预览位于 `dist\ScreenMover-preview-2\ScreenMover.exe`，最新公开预发布为 v0.2.0-preview.2，v0.1.0-demo 为较早版本。见 [开发清单](docs/ROADMAP.md) 与 [手动验收清单](docs/MANUAL_TEST.md)。


## 继续完善：快捷键、偏好备份与诊断

当前构建标记为 `0.2.0-preview.2`，尚待人工界面验收。

- 设置中可录入快捷面板的全局组合键；默认关闭，冲突或保存失败会保留原绑定，退出释放。
- “设置备份与诊断”可导出已保存偏好、读取偏好文件；导入后保存并应用才生效。设备尺寸、窗口排序、启动项和隐藏记录不随偏好文件迁移。
- 可生成诊断 ZIP，包含版本/构建编号、系统信息、匿名显示器几何和设置。使用白名单重新生成 JSON，不收集截图、原始日志、窗口标题、显示器名称或用户路径。
- 同时修复重复释放单实例资源、启动项读取受限导致设置打不开、设置保存后状态阻挡快捷操作的问题。

本轮没有注册实际全局快捷键、修改开机启动或启动图形界面验收；这些入口只在运行软件后由使用者操作。测试使用模拟注册器验证冲突与回退，使用临时文件验证偏好和诊断包。


## 0.2.0-preview.2：紧凑快捷面板

快捷面板随窗口数与屏幕数变化，接近 4:3；少量内容约 350 × 258 DIP，较多内容最多约 480 × 360 DIP 并滚动。软件名列保持紧凑，只显示 Word、ChatGPT 等软件名，完整窗口标题放在悬停提示里。拖拽预览不再叠加外层边框。原生白边增加非客户区与样式处理，实际观感仍需要人工确认。

```powershell
# 构建、发布、纯逻辑回归（不启动软件窗口）
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -PureTest
# 输出到指定目录，避免覆盖正在运行的文件
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -PureTest -OutputPath dist\ScreenMover-preview-2
```

每次 push 到 main 或提交 PR，GitHub Actions 会执行 Windows 构建、无界面回归并保存便携包产物。它不会代替视觉和实体多屏验收。更新详情见 [CHANGELOG](CHANGELOG.md)。
