namespace ScreenMover.Services;

public sealed record PlatformSupport(int Build,bool CanHide,string Description) {
    public static PlatformSupport Current {get;}=ForBuild(Environment.OSVersion.Version.Build);
    public static PlatformSupport ForBuild(int build)=>new(build,build is 26100 or 26200,
        build>=22000?$"Windows 11 · {build}":$"Windows 10 · {build}");
    public string HideStatus=>CanHide?L.T("独立虚拟桌面隐藏可用"):L.T("此系统尚未适配独立虚拟桌面隐藏；预览、移动、最小化和快捷面板可用");
}
