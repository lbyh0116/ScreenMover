# v0.2.0 · 2026-10-18

[下载正式版 / Download](https://github.com/lbyh0116/ScreenMover/releases/tag/v0.2.0)

## 简体中文

首个正式发行版，提供 Windows x64 便携包。

- 显示器与软件窗口静态预览、相册网格、搜索、多选、拖拽排序。
- 窗口跨屏拖放、批量移动、撤销，以及已适配系统上的隐藏与找回。
- 托盘快捷面板、可配置全局快捷键、主题与设置自动保存。
- GitHub 更新检查、SHA-256 校验、独立安装与文件替换失败回退；正式渠道在 API 不可用时通过 GitHub 的 latest 入口读取稳定版本。

升级：退出旧版，完整解压后运行 ScreenMover.exe；已有 Preview 6 可从设置的软件更新中安装。数据继续保留。

兼容：Windows 11 x64；Win10 22H2 提供基础兼容路径，尚待实机验证。独立虚拟桌面隐藏限构建 26100 / 26200。当前不含 x86/ARM64 包、英文界面或 Microsoft Store 版。

## English

First stable release, distributed as a portable Windows x64 package.

- Static monitor/window previews, gallery browsing, search, multi-selection, and persistent drag ordering.
- Cross-monitor drag-and-drop, batch moves, undo, and hiding/recovery on supported Windows builds.
- Tray quick panel, configurable global hotkeys, appearance options, and automatically saved settings.
- GitHub release checks, SHA-256 verification, an independent updater, and attempted rollback after file replacement failures. Stable checks can use GitHub's official latest-release redirect when the API is unavailable.

Upgrade: exit the old version, extract the entire archive, and run ScreenMover.exe. Preview 6 users can install through the in-app updater. Local settings and recovery records are retained.

Compatibility: Windows 11 x64. A basic Windows 10 22H2 path is included but still needs device validation. Separate virtual-desktop hiding is limited to builds 26100 / 26200. No x86/ARM64 packages, English UI, or Store edition are included.

## 验证 / Validation

构建、无界面回归、临时副本更新助手重启及发行包完整性检查；未使用 computer use。更广泛的设备测试仍在进行。

Build, headless regressions, updater restart using a temporary fixture, and release-package integrity checks. No computer-use verification. Broader hardware testing remains ongoing.
