# ScreenMover

**看见窗口，拖到目标屏幕。** 面向 Windows 的多屏窗口整理工具。

[English](README.en.md) · [下载正式版](https://github.com/lbyh0116/ScreenMover/releases/latest) · [反馈问题](https://github.com/lbyh0116/ScreenMover/issues/new/choose)

## 下载与运行

最新正式版：**v0.2.1 · Windows x64 便携版**。

1. 下载 [ScreenMover-v0.2.1-win-x64.zip](https://github.com/lbyh0116/ScreenMover/releases/download/v0.2.1/ScreenMover-v0.2.1-win-x64.zip)。
2. 完整解压，运行 `ScreenMover.exe`；请保留同目录的所有文件。
3. 升级时先从托盘退出旧版。设置、排序和隐藏恢复记录会继续保留。

包含所需运行时，无需另装 .NET。当前包未进行商业代码签名；Microsoft Store 版尚未发布。界面语言目前为简体中文。

## 主要功能

- **可视化整理**：显示器和软件窗口的静态预览，主界面采用相册网格；无法截图时显示软件图标。
- **跨屏拖放**：把窗口卡片拖到目标显示器，支持多选、批量移动及撤销上一次移动。
- **托盘快捷面板**：紧凑的窗口列表与显示器预览，点开即用，点击外部自动收起。
- **窗口暂存与找回**：在已适配系统上，隐藏到以软件名命名的独立虚拟桌面，再从“已隐藏”找回；手动移回后自动更新记录。应用拒绝最小化时保留已完成的桌面移动。
- **个人偏好**：窗口拖拽排序、系统/自定义主题色、浅色/深色外观、快捷键预设与组合、设置自动保存。
- **软件内更新**：在设置中检查 GitHub Releases，下载、校验、原地安装后重启。

截图为静态采样，不录屏。默认在启动、重新激活、每分钟及按 F5 时更新。关闭主窗口默认保留托盘运行；彻底退出请右键托盘图标。

## 常用操作

| 操作 | 方式 |
| --- | --- |
| 移动窗口 | 拖到另一显示器卡片，或使用底部移动按钮 |
| 多选 / 范围选择 | Ctrl / Shift + 单击 |
| 激活窗口 | 双击窗口卡片 |
| 搜索 / 全选 | Ctrl+F / Ctrl+A |
| 刷新预览 | F5 |
| 取消 / 收起快捷面板 | Esc；点击空白取消选择 |
| 打开快捷面板 / 隐藏前台窗口 | 在设置中分别配置全局快捷键 |

快捷面板中，拖到窗口所在显示器会执行隐藏；拖到其他显示器会移动。拖放遮罩会预告操作。

## 兼容范围

- **Windows 11 x64**：优先使用云母材质；独立虚拟桌面隐藏目前仅在构建 **26100 / 26200** 启用。
- **Windows 10 22H2 x64**：提供基础兼容路径，支持时使用亚克力；仍待 Win10 实机验证。独立虚拟桌面隐藏禁用。
- **x86 / ARM64**：尚未提供发行包。

受保护、最小化、GPU 绘制或无响应窗口可能无法预览。操作其他高权限软件可能需要用户选择以管理员身份重启。混合 DPI、热插拔和不同显卡仍需设备验证。详见 [兼容说明](docs/COMPATIBILITY.md)。

## 更新与本地数据

设置 → **软件更新** → 检查更新 → 下载并安装。安装前校验 SHA-256，替换失败尝试回退；断电和新版运行期崩溃不保证自动恢复。检查由用户触发，不在后台联网。详见 [更新说明](docs/UPDATES.md)。

本地数据位于 `%LOCALAPPDATA%\ScreenMover`。截图仅在内存中处理，不上传；操作与隐藏记录可能包含窗口标题。更新备份保存在 `updates` 子目录。详见 [隐私说明](docs/PRIVACY.md)。

## 从源码构建

需要 Windows x64、.NET SDK **10.0.400** 和 NuGet 网络访问。

```powershell
git clone https://github.com/lbyh0116/ScreenMover.git
cd ScreenMover
.\build.ps1 -PureTest
```

输出：`dist\ScreenMover\ScreenMover.exe`。该命令构建、发布并运行无界面回归；不会操作用户窗口。[开发路线](docs/ROADMAP.md) · [测试清单](docs/MANUAL_TEST.md)。

## 授权与安全

原创代码尚未选定开源许可证，公开源码不等同于授予开源授权。虚拟桌面辅助代码来自 [MScholtes/VirtualDesktop](https://github.com/MScholtes/VirtualDesktop)，保留其 [MIT 许可](https://github.com/lbyh0116/ScreenMover/blob/main/ScreenMover/Helpers/VirtualDesktop-LICENSE.txt)；其他依赖遵循各自许可。

安全问题请使用 [私密漏洞报告](https://github.com/lbyh0116/ScreenMover/security/advisories/new)，普通问题请提交 Issue。[最新发行说明](CHANGELOG.md)。
