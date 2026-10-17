using Microsoft.Win32;
namespace ScreenMover.Services;

public static class StartupService {
    const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
    public static string Command(string executable)=>"\""+Path.GetFullPath(executable)+"\" --start-in-tray";
    public static bool Enabled {
        get{using var key=Registry.CurrentUser.OpenSubKey(RunKey);return key?.GetValue("ScreenMover") is string value&&!string.IsNullOrWhiteSpace(value);}
    }
    public static bool? ReadState(out string? error){
        error=null;
        try{return Enabled;}catch(Exception ex)when(ex is UnauthorizedAccessException or System.Security.SecurityException or IOException){error="无法读取启动项："+ex.Message;return null;}
    }
    public static void SetEnabled(bool enabled){
        using var key=Registry.CurrentUser.CreateSubKey(RunKey,true);
        if(enabled)key.SetValue("ScreenMover",Command(Environment.ProcessPath!),RegistryValueKind.String);
        else key.DeleteValue("ScreenMover",false);
    }
}
