# ScreenMover

**See your windows. Drag them to the right display.** A visual multi-monitor window organizer for Windows.

[简体中文](README.md) · [Latest release](https://github.com/lbyh0116/ScreenMover/releases/latest) · [Report an issue](https://github.com/lbyh0116/ScreenMover/issues/new/choose)

## Download and run

Latest stable release: **v0.2.0 · Windows x64 portable**.

1. Download [ScreenMover-v0.2.0-win-x64.zip](https://github.com/lbyh0116/ScreenMover/releases/download/v0.2.0/ScreenMover-v0.2.0-win-x64.zip).
2. Extract the entire archive and run `ScreenMover.exe`. Keep all bundled files together.
3. When upgrading, exit the old version from its tray menu first. Settings, window order, and hidden-window recovery records are retained.

The required runtimes are included; no separate .NET installation is needed. This package is not commercially code-signed. The Microsoft Store edition is not available yet. **The application UI is currently Simplified Chinese; this document provides English instructions.**

## Features

- **Visual overview:** static monitor and window previews in a gallery, with application icons when capture is unavailable.
- **Drag between displays:** move selected windows to a monitor card, including batch moves and undoing the last move.
- **Tray quick panel:** a compact window list beside monitor previews; closes when you click outside.
- **Park and recover windows:** on supported Windows builds, move a window to a separate virtual desktop named after its application and minimize it; recover it from the main window.
- **Personal preferences:** persistent window ordering, light/dark appearance, system or custom accent colors, configurable hotkeys, and automatically saved settings.
- **In-app updates:** check GitHub Releases, download and verify the matching package, then install and restart.

Previews use periodic snapshots, not continuous recording. By default, they refresh on startup, activation, every minute, or F5. Closing the main window normally keeps ScreenMover in the tray; use the tray menu to exit completely.

## Controls

| Action | Control |
| --- | --- |
| Move a window | Drag it onto another monitor card, or use the move buttons |
| Multi-select / range select | Ctrl / Shift + click |
| Activate a window | Double-click its card |
| Search / select all | Ctrl+F / Ctrl+A |
| Refresh previews | F5 |
| Cancel / dismiss the quick panel | Esc; click empty space to clear selection |
| Open the quick panel / hide the foreground window | Configure separate global hotkeys in settings |

In the quick panel, dropping onto the window's current monitor hides it; dropping onto a different monitor moves it. The drop overlay previews the action.

## Compatibility

- **Windows 11 x64:** uses Mica when available. Separate virtual-desktop hiding is currently enabled only on builds **26100 / 26200**.
- **Windows 10 22H2 x64:** a basic compatibility path with Acrylic when supported; Windows 10 device validation is still pending. Separate virtual-desktop hiding is disabled.
- **x86 / ARM64:** no release packages yet.

Protected, minimized, GPU-rendered, or unresponsive windows may not provide a preview. Managing elevated applications may require restarting ScreenMover as administrator. Mixed DPI, hot-plugging, and different graphics hardware still need broader device testing. See [compatibility details](docs/COMPATIBILITY.md) (Chinese).

## Updates and privacy

In settings, open **软件更新** (Software updates), choose **检查更新** (Check for updates), then **下载并安装** (Download and install). Downloads are checked with SHA-256. File replacement failures trigger an attempted rollback; power loss and runtime crashes in a new version are not automatically recovered. Checks are user-initiated; there is no background update polling. See [update details](docs/UPDATES.md) (Chinese).

Local data is stored in `%LOCALAPPDATA%\ScreenMover`. Screenshots remain in memory and are not uploaded. Operation logs and recovery records may contain window titles. Update backups are stored in the `updates` subfolder. See [privacy details](docs/PRIVACY.md) (Chinese).

## Build from source

Requires Windows x64, **.NET SDK 10.0.400**, and access to NuGet.

```powershell
git clone https://github.com/lbyh0116/ScreenMover.git
cd ScreenMover
.\build.ps1 -PureTest
```

Output: `dist\ScreenMover\ScreenMover.exe`. This builds, publishes, and runs headless regressions without manipulating your windows. [Roadmap](docs/ROADMAP.md) · [Manual test checklist](docs/MANUAL_TEST.md) (Chinese).

## Licensing and security

An open-source license has not yet been selected for the original code. Public source access does not grant an open-source license. Virtual desktop helper code comes from [MScholtes/VirtualDesktop](https://github.com/MScholtes/VirtualDesktop) and retains its [MIT license](https://github.com/lbyh0116/ScreenMover/blob/main/ScreenMover/Helpers/VirtualDesktop-LICENSE.txt); dependencies retain their respective licenses.

Please use [private vulnerability reporting](https://github.com/lbyh0116/ScreenMover/security/advisories/new) for security issues and GitHub Issues for general feedback. [Latest release notes](CHANGELOG.md).
