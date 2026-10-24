using System.Reflection;
namespace ScreenMover.Services;
public static class BuildInfo {
    public static string FullVersion=>typeof(BuildInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion??L.T("开发版");
    public static string DisplayVersion=>FullVersion.Split('+')[0];
}
