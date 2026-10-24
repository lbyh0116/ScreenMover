# v0.2.2 · 2026-10-24

[下载正式版 / Download](https://github.com/lbyh0116/ScreenMover/releases/tag/v0.2.2)

## 简体中文

- 新增简体中文、英语、德语、法语和西班牙语界面，覆盖主窗口、快捷面板、设置、托盘菜单及操作提示。
- 默认跟随 Windows 界面语言，不支持的语言回退到英语。在设置中选择语言后自动保存，点击“立即重启”生效。
- 检查更新后，如果已安装最新版，“更新说明”显示“已是最新版”；新版没有说明时提供明确提示。
- 窗口标题、桌面名称和已有操作记录保留原文；更换语言不会影响隐藏窗口找回。

升级：从托盘退出旧版，完整解压新包后运行 `ScreenMover.exe`，或使用设置中的软件更新。已有设置、排序和恢复记录继续保留。

兼容范围不变：Windows x64；独立虚拟桌面隐藏限构建 26100 / 26200。Win10 22H2 基础兼容仍待实机验证。

## English

- Added Simplified Chinese, English, German, French and Spanish across the main window, quick panel, settings, tray menu and action messages.
- The app follows your Windows display language by default, falling back to English for unsupported languages. Choose a language in Settings, then select **Restart now** to apply it. Your choice is saved automatically.
- When an update check confirms you are up to date, release notes now say so. New releases without notes also show a clear fallback message.
- Window titles, desktop names and existing activity logs retain their original text. Changing languages does not affect hidden-window recovery.

Upgrade: exit the old version from its tray menu, extract the entire archive and run `ScreenMover.exe`, or use the in-app updater. Settings, window order and recovery records are preserved.

Compatibility is unchanged: Windows x64; separate-desktop hiding on builds 26100 / 26200 only. Windows 10 22H2 device validation remains pending.

## 验证 / Validation

构建与无界面回归覆盖五种语言、区域语言回退、翻译完整性和占位符、设置往返、更新说明及既有功能。未使用 computer use；各语言的最终排版仍需手动验收。

Build and headless regressions cover all five languages, regional fallbacks, translation coverage and format arguments, settings round trips, update notes and existing features. No computer-use validation; final layouts in each language still need manual acceptance.
