# 兼容范围

本轮按用户选择的 A 档实现：Windows 10 22H2 x64 基础兼容，Win11 保持现有功能。以下区分“代码路径已实现”和“实机已验收”。

| 平台 | 材质与标题栏 | 预览 / 移动 / 托盘 / 面板快捷键 | 独立虚拟桌面隐藏 |
| --- | --- | --- | --- |
| Windows 10 22H2 / 19045 | 支持时桌面亚克力，否则纯色；系统原生标题栏 | 基础兼容路径已实现，待 Win10 实机验收 | 禁用，不注册隐藏快捷键 |
| Windows 11 22000 / 22621 / 22631 | 优先云母，不可用时亚克力或纯色 | 保持可用，需相应设备回归 | 暂未适配，禁用 |
| Windows 11 26100 / 26200 | 云母或运行时回退 | 保持可用 | 已适配构建，继续回归隐藏/找回 |
| 后续未知构建 | 运行时探测材质与 API | 不因隐藏接口未知而阻止主界面使用 | 默认禁用，待验证后适配 |

WinUI 3 与 Windows App SDK 具备从 Windows 10 1809 起的向后兼容能力，但此项目本轮选择的 Win10 发行目标是 22H2 x64，不据此承诺所有旧系统、设备或依赖版本都已验收。[微软说明](https://learn.microsoft.com/en-au/windows/apps/get-started/windows-developer-faq)

`MicaController.IsSupported()` / `DesktopAcrylicController.IsSupported()` 决定材质，无法建立材质时保留主题纯色背景。系统透明效果、远程会话、图形能力和节能状态可能使材质降级。[官方材质文档](https://learn.microsoft.com/en-us/windows/apps/develop/ui/materials)

Windows 10 的标题栏配色 API 不生效，因此保留原生标题栏；Win11 仍使用融合内容的标题栏，不强行模拟不支持的原生特效。[官方标题栏说明](https://learn.microsoft.com/en-us/windows/apps/develop/title-bar)

虚拟桌面“创建、命名、移动到新桌面”的实现与系统构建有关，独立于 WinUI 界面能否运行。UI 与实际执行服务双重限制未适配构建；Win10 不偷偷把“隐藏到新桌面”降级成普通最小化。

本地当前没有 Win10 实机/虚拟机，未把 Win10 的实际材质、托盘、权限、混合 DPI 或热插拔标为通过。后续按 MANUAL_TEST.md 验收。
