# v0.2.1 验证说明 / Verification notes

本轮未使用 computer use，也没有操作用户的游戏或虚拟桌面。预览版的隐藏功能已由用户在本机验收通过；不同游戏模式和设备仍可能存在差异。

## 本轮修正

- 快捷面板整行响应拖拽；忽略内部控件向面板交接鼠标捕获时的失焦事件。
- 两列按内容确定宽度，长窗口列表不再横向撑大面板。软件名宽度采用实测文字宽度的第 75 百分位，并限制上下限；名称只在尾部省略。
- 托盘图标沿用原图形，缩小留白，增加 16/20/24/32/40/48/64 像素资源。
- 普通 Windows 托盘提示显示 `ScreenMover` 和 `当前桌面：名称`。提示最多 127 个 UTF-16 单元，长名称安全截断。名称从 Explorer 的本地桌面元数据读取；无法读取时显示“名称不可用”，不猜测桌面名称。
- 隐藏助手不再因最小化失败撤销已经完成的跨桌面移动。观察 2.5 秒，区分已最小化、仅移入独立桌面、窗口返回可见桌面、原窗口关闭等结果。失败信息包含阶段及错误码，保留恢复记录。
- 刷新时检查隐藏记录的窗口身份和实际桌面。窗口被手动移离隐藏桌面后更新记录；仅切换到隐藏桌面查看不会误清理。关闭/重启的窗口标记失效，不计入隐藏数量。权限不足或桌面状态未知时保留记录。
- 隐藏记录按卡片排版，名称、桌面、状态、时间分层显示，增加行距及条目间距。

- 滚动内容右侧预留独立间距，避免滚动条覆盖窗口卡片、记录文字、设置控件和快捷键选择栏。快捷操作成功后不再显示浮动提示；设置页移除自动保存说明，保存逻辑不变。

## 手动验收

1. 在快捷面板每一行的图标、文字、空白及四周边缘开始拖拽；释放到空白处不得误移动窗口。
2. 比较 1 / 5 / 多个窗口及一个很长软件名的情况；切换不同数量、纵横比例的显示器，确认面板没有多余右侧空白，列表仍可滚动。
3. 悬停托盘图标，切换/重命名虚拟桌面后再悬停。检查两行原生提示及不同缩放比例下的图标。
4. 隐藏三角洲行动，等待至少 5 秒，检查游戏是否仍在独立桌面、原桌面是否保持；尝试找回。分别记录全屏/无边框模式的结果。程序化测试无法证明游戏本身不会在观察期之后重新激活。
5. 隐藏普通窗口后，在系统任务视图中手动移回。等待一次刷新（通常约 2 秒），确认隐藏数量减少、已打开的记录列表同步更新。
6. 只切换到隐藏桌面，不移动窗口；确认恢复记录保留。关闭被隐藏的软件，确认失效条目保留且可以移除，其他条目不受影响。

## 自动验证与实现依据

纯逻辑回归覆盖：内容宽度与超长名称、负坐标/高 DPI 边界；模拟 2 秒后游戏自恢复；拒绝最小化；窗口销毁；手动跨桌面移动与单纯切换桌面的区别；未知权限状态、持久化与两行提示的 Unicode 上限。构建验证 WinUI XAML 及桌面助手。

- [Windows 原生托盘提示的 NIF_SHOWTIP 行为](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-notifyicondataw)
- [Windows 查询窗口所在桌面的公开接口](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-ivirtualdesktopmanager-getwindowdesktopid)

## English

Verification notes for v0.2.1. The user confirmed hiding behavior on their machine. The quick panel now uses content-based sizing, full-row drag handling and trailing ellipses. A larger tray glyph and a two-line native tooltip replace the missing hover information. Hiding no longer exposes a successfully moved window merely because minimization was refused; a 2.5-second observation reports delayed returns separately. Recovery records reconcile manual desktop moves and closed windows, with a spaced card layout. Actual game behavior, pointer interaction and visual appearance still require manual acceptance. No computer-use validation was performed.
