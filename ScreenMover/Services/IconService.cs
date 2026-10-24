using System.Diagnostics;
using System.Runtime.InteropServices;
using ScreenMover.Models;
namespace ScreenMover.Services;
public static class IconService {
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern nint LoadImageW(nint instance,string path,uint type,int width,int height,uint flags);
    public static nint Tray(nint window){
        double scale=TrayIcon.TryGetBounds(window,out var bounds)?Interop.WindowChrome.ScaleAt(new(){X=bounds.Left,Y=bounds.Top}):TrayIcon.GetDpiForWindow(window)/96.0;
        int size=Math.Clamp((int)Math.Round(16*scale),16,64);
        var icon=LoadImageW(0,Path.Combine(AppContext.BaseDirectory,"Assets","TrayIcon.ico"),1,size,size,0x10);
        return icon!=0?icon:Extract(Environment.ProcessPath!);
    }
    [DllImport("user32.dll",SetLastError=true)] static extern nint SendMessageTimeoutW(nint h,uint msg,nint w,nint l,uint flags,uint timeout,out nint result);
    [DllImport("user32.dll",EntryPoint="GetClassLongPtrW")] static extern nint GetClassLongPtr(nint h,int index);
    [DllImport("user32.dll")] static extern nint CopyIcon(nint icon);
    [DllImport("user32.dll")] public static extern bool DestroyIcon(nint icon);
    [DllImport("user32.dll")] static extern nint LoadIconW(nint instance,nint name);
    [DllImport("shell32.dll",CharSet=CharSet.Unicode)] static extern uint ExtractIconExW(string path,int index,out nint large,out nint small,uint count);
    public static nint Extract(string path){
        ExtractIconExW(path,0,out var large,out var small,1);
        if(large!=0)DestroyIcon(large);
        return small!=0?small:CopyIcon(LoadIconW(0,32512));
    }
    public static SnapshotFrame? Read(WindowInfo window){
        nint owned=0;
        try{
            Interop.Native.GetWindowThreadProcessId(window.Handle,out int pid);
            if(pid!=window.Pid||!Interop.Native.IsWindow(window.Handle))return null;
            SendMessageTimeoutW(window.Handle,0x7F,1,0,2,100,out var icon);
            if(icon==0)SendMessageTimeoutW(window.Handle,0x7F,2,0,2,100,out icon);
            if(icon==0)icon=GetClassLongPtr(window.Handle,-14);
            if(icon!=0)owned=CopyIcon(icon);
            if(owned==0){using var p=Process.GetProcessById(pid);owned=Extract(p.MainModule!.FileName);}
            return SnapshotService.Icon(owned);
        }catch{
            if(owned!=0){DestroyIcon(owned);owned=0;}
            owned=CopyIcon(LoadIconW(0,32512));
            return owned==0?null:SnapshotService.Icon(owned);
        }finally{if(owned!=0)DestroyIcon(owned);}
    }
}
