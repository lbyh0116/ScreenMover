# 架构开发准备

当前仅支持并发行 win-x64。本轮只整理构建入口，不实现或发布 x86/ARM64，不改变现有功能。

## 构建约定

- 主程序与 CaptureWorker 共用 Directory.Build.props 的 ScreenMoverArchitecture、RuntimeIdentifier 和 PlatformTarget，默认 x64，不随构建主机变成不同发行架构。
- build.ps1 提供 -Architecture 参数；CI 使用架构矩阵，目前只有 x64。CaptureWorker 打包路径跟随目标 RID。
- Directory.Build.targets 和脚本会明确拒绝未适配架构，避免只切换 RID 后打包进 x64 虚拟桌面助手。声明可选平台不表示实现完成。
- 将来启用新架构时，先完成下面清单，再同时放开脚本与 MSBuild 保护。使用独立输出目录（例如 dist/ScreenMover-arm64），不混合各架构文件。

## x86 启用前

1. GetWindowLongPtrW/SetWindowLongPtrW 按指针宽度分派，32 位调用 GetWindowLongW/SetWindowLongW。
2. 审计 P/Invoke 结构布局、指针转换、窗口句柄序列化及恢复；验证 x86 进程管理其他架构窗口的权限与信息读取降级。
3. 替换 Framework64 编译器固定路径及 /platform:x64；决定 x86 的虚拟桌面功能支持范围，不能复制 x64 助手。
4. 在 Win10 22H2 x86 验证运行时、亚克力回退、截图组件、移动、托盘、快捷键、安装/升级及低内存场景。

## ARM64 启用前

1. 在 x64 主机交叉编译主程序及截图组件，核对每个 PE 文件架构与运行时依赖；不依赖 x64 仿真冒充原生支持。
2. 虚拟桌面助手当前仍是 .NET Framework x64 程序。先决定迁移路径及 COM 兼容范围；基础版可显式禁用该功能，但必须同时移除其打包要求和无效调用。
   更新助手目前也以 x64 编译，启用 ARM64 原生包前需适配其运行时/架构；不得把 x64 更新助手误标为 ARM64 原生组件。
3. 将隐藏功能能力判定从仅 OS build 扩展为 OS build + 进程/系统架构 + 助手能力，不因 ARM64 系统 build 相同就启用。
4. ARM64 环境运行回归；重点测试 x86/x64/ARM64 目标窗口混用。交叉构建不能代替执行测试。
5. 实机检查多屏、DPI、托盘位置、快照、动画、睡眠唤醒、长时间内存及虚拟桌面恢复；虚拟机仅覆盖部分功能。

## 发行边界

每个启用架构分别执行构建、回归、发布、PE/资源检查及校验和生成。跨架构构建主机不能直接执行 ARM64 测试程序，应交给对应 runner/设备；CI 在完成该流程前继续只生成 x64。

架构准备不等同于 Win10 或 ARM 实机验收，也不代表已完成 Store/MSIX 打包。
