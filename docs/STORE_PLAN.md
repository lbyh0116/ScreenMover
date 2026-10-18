# GitHub 原版与 Microsoft Store 版计划

状态：发行设计与官方要求核对，尚未创建商店商品、收费或提交 MSIX。

## 两个发行渠道，共享一套核心源码

| 项目 | GitHub 原版 | Store 版 |
| --- | --- | --- |
| 发行形式 | Windows x64 便携 ZIP | 已签名的 MSIX，Microsoft Store 分发 |
| 核心功能 | 显示屏与窗口预览、移动、隐藏、快捷面板、快捷键 | 相同核心功能 |
| 更新 | GitHub Releases | 商店管理更新 |
| Copilot 键系统选择项 | 当前便携 EXE 没有包标识，不能直接注册 | 在包清单中注册硬件键应用扩展，由用户在 Windows 设置中选择 |
| 收费计划 | 维持原版下载方式 | 目标 0.99 美元一次性购买；提交时以 Partner Center 可选价格档和地区设置为准 |

Copilot 键能力的技术前提是“已打包、已签名、正确注册”，不是“必须付费”。GitHub 若将来也提供满足这些要求的签名 MSIX，同样可以支持。商店版可凭安装便利、签名、系统集成与自动更新提供价值，不应把系统接口包装成付费专属资格。

## Copilot 键实现路径

1. 取得正式包名称、Publisher 和 Store identity，添加 MSIX 发行项目；便携版保持原构建路径。
2. 在包清单中声明 `uap3:AppExtension`，Name 为 `com.microsoft.windows.copilotkeyprovider`，并完成包签名。
3. 路由系统激活到快捷面板，复用现有实例，避免每次按键打开第二个主窗口。需要单独实现包激活/协议与进程间消息；当前便携版尚未提供此功能。
4. 提供一次性引导入口，打开 `ms-settings:personalization-textinput-copilot-hardwarekey`，让用户自行选择 ScreenMover；不直接改写用户的按键选择。
5. 在受支持 Windows 构建及具备该设置的设备上验收：首次启动、常驻唤醒、单击面板开关、窗口焦点与多显示器行为。

应用扩展的基础注册要求自 Windows Build 22621 起提供；具体键状态/长按等增强协议有自己的系统要求，不应混为一谈。首版建议只做按键打开快捷面板，暂不把长按和录屏等无关功能加入范围。

## 上架前剩余工作

- 选定原创代码许可证；当前公开仓库不等同于已经采用开源许可证，第三方许可继续保留。
- 定稿应用图标和商店素材，补充隐私说明、支持入口与发行说明。
- 验证包内虚拟桌面辅助程序、全局快捷键、开机启动、管理员重启、数据升级/卸载行为。
- 完成包身份、签名、安装更新、权限声明和 Store 认证测试；技术可行不代表已经获批。
- 在 Partner Center 设置实际价格、地区与发布范围；价格不能由本地源码直接生效。

## 官方资料

- [Copilot 硬件键 Provider：打包、扩展与签名要求](https://learn.microsoft.com/en-us/windows/apps/develop/windows-integration/microsoft-copilot-key-provider)
- [Windows 设置 URI：Copilot 硬件键选择页](https://learn.microsoft.com/en-us/windows/apps/develop/launch/launch-settings)
- [MSIX 应用价格与可用性](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/price-and-availability)
