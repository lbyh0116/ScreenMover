# v0.2.1 · 2026-10-24

[下载正式版 / Download](https://github.com/lbyh0116/ScreenMover/releases/tag/v0.2.1)

## 简体中文

- 快捷面板整行可拖拽，宽度随软件名和显示器预览调整，长名称只在末尾省略。
- 托盘图标减少留白，并提供“ScreenMover / 当前桌面”的两行原生悬停提示。
- 修复部分游戏拒绝最小化后，隐藏操作被撤销、窗口返回的问题；窗口已移入独立桌面时保留隐藏位置，并检查短暂延迟后的状态。
- 自动更新手动移离隐藏桌面及已关闭窗口的记录；隐藏记录改为分层卡片排版。
- 为快捷面板、主窗口、隐藏记录、历史记录和设置控件预留滚动条间距，修复窄窗口下滚动条覆盖下拉按钮的问题。
- 移除快捷操作的成功浮动提示及设置页自动保存说明，错误反馈和自动保存功能继续保留。

升级：先从托盘退出旧版，完整解压新包后运行 `ScreenMover.exe`。已有设置、排序和恢复记录继续保留，也可通过设置中的软件更新安装。

兼容范围不变：Windows x64；独立虚拟桌面隐藏限构建 26100 / 26200。Win10 22H2 基础兼容仍待实机验证。

## English

- Full-row dragging and content-sized quick-panel columns, with trailing ellipses for long application names.
- A larger tray glyph and a two-line native tooltip showing ScreenMover and the current virtual desktop.
- Fixed hiding being rolled back when a game refused minimization. Windows already moved to a separate desktop stay there, with delayed state verification.
- Recovery records now reconcile manual moves and closed windows, displayed in spaced cards.
- Reserved scrollbar space in the quick panel, main gallery, recovery/history lists and settings, including narrow layouts and shortcut pickers.
- Removed successful quick-action popups and the autosave explanation. Error feedback and automatic saving remain available.

Upgrade: exit the old version from its tray menu, extract the entire archive, and run `ScreenMover.exe`, or use the in-app updater. Settings, ordering and recovery records are preserved.

Compatibility is unchanged: Windows x64; separate-desktop hiding on builds 26100 / 26200 only. Windows 10 22H2 device validation remains pending.

## 验证 / Validation

构建、无界面回归、截图助手资源检查及更新助手临时副本测试。用户已验证预览版的游戏隐藏行为；本轮未使用 computer use，最终滚动条间距仍需手动检查。

Build, headless regressions, capture-worker resource checks and updater tests using temporary fixtures. The user verified game hiding in the preview build. No computer-use validation; final scrollbar spacing requires manual visual acceptance.
