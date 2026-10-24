using Microsoft.Win32;
using System.Diagnostics;
namespace ScreenMover.Services;

public static class TrayTip {
    const string Explorer=@"Software\Microsoft\Windows\CurrentVersion\Explorer";
    public static string Read(){
        try{
            using var root=Registry.CurrentUser.OpenSubKey(Explorer+@"\VirtualDesktops");
            using var process=Process.GetCurrentProcess();
            using var session=Registry.CurrentUser.OpenSubKey(Explorer+@"\SessionInfo\"+process.SessionId+@"\VirtualDesktops");
            var current=(Environment.OSVersion.Version.Build>=22000?root:session)?.GetValue("CurrentVirtualDesktop") as byte[]
                ??root?.GetValue("CurrentVirtualDesktop") as byte[]??session?.GetValue("CurrentVirtualDesktop") as byte[];
            if(current?.Length!=16)return Format(null);
            var id=new Guid(current);
            using var desktop=root?.OpenSubKey(@"Desktops\"+id.ToString("B"));
            var name=desktop?.GetValue("Name") as string;
            if(string.IsNullOrWhiteSpace(name)&&root?.GetValue("VirtualDesktopIDs") is byte[] ids){
                for(int offset=0;offset+16<=ids.Length;offset+=16)
                    if(new Guid(ids.AsSpan(offset,16))==id){name=L.F("桌面 {0}", offset/16+1);break;}
            }
            return Format(name);
        }catch{return Format(null);}
    }
    public static string Format(string? name){
string prefix=L.T("ScreenMover\n当前桌面：");
        name=string.IsNullOrWhiteSpace(name)?L.T("名称不可用"):string.Concat(name.Select(c=>char.IsControl(c)?' ':c)).Trim();
        int available=127-prefix.Length;
        if(name.Length>available){int end=available-1;if(char.IsHighSurrogate(name[end-1]))end--;name=name[..end]+"…";}
        return prefix+name;
    }
}
