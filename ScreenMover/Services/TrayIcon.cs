using System.Runtime.InteropServices;
using static ScreenMover.Interop.Native;
namespace ScreenMover.Services;
public sealed class TrayIcon : IDisposable {
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
    struct NotifyData {
        public uint Size; public nint Window; public uint Id,Flags,Callback;public nint Icon;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] public string Tip;
        public uint State,StateMask;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=256)] public string Info;
        public uint Version;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=64)] public string InfoTitle;
        public uint InfoFlags;public Guid Guid;public nint Balloon;
    }
    [StructLayout(LayoutKind.Sequential)] struct IconId {public uint Size;public nint Window;public uint Id;public Guid Guid;}
    [DllImport("shell32.dll")] static extern int Shell_NotifyIconGetRect(ref IconId id,out RECT rect);
    public static bool TryGetBounds(nint window,out RECT rect){var id=new IconId{Size=(uint)Marshal.SizeOf<IconId>(),Window=window,Id=1};return Shell_NotifyIconGetRect(ref id,out rect)==0;}
    [DllImport("shell32.dll",CharSet=CharSet.Unicode)] static extern bool Shell_NotifyIconW(uint action,ref NotifyData data);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern uint RegisterWindowMessageW(string text);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT point);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(nint hwnd);
    [DllImport("user32.dll")] static extern nint CreatePopupMenu();
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern bool AppendMenuW(nint menu,uint flags,nuint id,string text);
    [DllImport("user32.dll")] static extern uint TrackPopupMenu(nint menu,uint flags,int x,int y,int reserved,nint hwnd,nint rect);
    [DllImport("user32.dll")] static extern bool DestroyMenu(nint menu);
    [DllImport("user32.dll")] static extern bool PostMessageW(nint h,uint msg,nint w,nint l);
    readonly nint hwnd;
    nint icon;
    readonly SubclassProc callback;
    readonly uint taskbarCreated;
    NotifyData data;
    bool disposed;
    long lastTip;
    public bool Installed {get;private set;}
    public TrayIcon(nint window,Action<bool> show,Action main,Action quit){
        hwnd=window;icon=IconService.Tray(window);
        data=new(){Size=(uint)Marshal.SizeOf<NotifyData>(),Window=hwnd,Id=1,Flags=1|2|4|0x80,Callback=0x8000+27,Icon=icon,Tip=TrayTip.Read(),Info="",InfoTitle=""};
        taskbarCreated=RegisterWindowMessageW("TaskbarCreated");
        callback=(h,msg,w,l,id,refData)=>{
            if(msg==taskbarCreated&&!disposed)Install();
            if(msg==0x02E0&&!disposed){var next=IconService.Tray(hwnd);if(next!=0){var old=icon;icon=next;data.Icon=next;Shell_NotifyIconW(1,ref data);if(old!=0)IconService.DestroyIcon(old);}}
            if(msg==data.Callback&&!disposed){
                int message=l.ToInt32()&0xFFFF;
                if(message==0x200&&Environment.TickCount64-lastTip>500){lastTip=Environment.TickCount64;RefreshTip();}
                // Version 4 supports keyboard activation and sends one selection event per click.
                if(message is 0x400 or 0x401)show(message==0x400);
                else if(message==0x7B){
                    GetCursorPos(out var point);var menu=CreatePopupMenu();
                    try{
                        AppendMenuW(menu,0,1,L.T("快捷操作"));AppendMenuW(menu,0,2,L.T("打开主界面"));AppendMenuW(menu,0x800,0,"");AppendMenuW(menu,0,3,L.T("退出 ScreenMover"));
                        SetForegroundWindow(hwnd);
                        uint selected=TrackPopupMenu(menu,0x100|0x2,point.X,point.Y,0,hwnd,0);
                        PostMessageW(hwnd,0,0,0);
                        if(selected==1)show(false);else if(selected==2)main();else if(selected==3)quit();
                    }finally{DestroyMenu(menu);}
                }
                return 0;
            }
            return DefSubclassProc(h,msg,w,l);
        };
        if(!SetWindowSubclass(hwnd,callback,42,0)){if(icon!=0)IconService.DestroyIcon(icon);throw new InvalidOperationException(L.T("无法创建托盘消息监听"));}
        Install();
    }
    void Install(){
        data.Tip=TrayTip.Read();
        Installed=Shell_NotifyIconW(0,ref data);
        if(Installed){data.Version=4;Shell_NotifyIconW(4,ref data);}
    }
    public void RefreshTip(){
        if(disposed)return;string tip=TrayTip.Read();if(data.Tip==tip)return;
        data.Tip=tip;if(Installed)Shell_NotifyIconW(1,ref data);
    }
    public void Dispose(){
        if(disposed)return;disposed=true;Shell_NotifyIconW(2,ref data);Installed=false;
        RemoveWindowSubclass(hwnd,callback,42);if(icon!=0)IconService.DestroyIcon(icon);
    }
}
