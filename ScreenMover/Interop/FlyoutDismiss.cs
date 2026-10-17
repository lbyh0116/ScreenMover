using System.Runtime.InteropServices;
using static ScreenMover.Interop.Native;
namespace ScreenMover.Interop;

public sealed class FlyoutDismiss : IDisposable {
    delegate nint MouseProc(int code,nint message,nint data);
    [DllImport("user32.dll")] static extern nint SetWindowsHookExW(int type,MouseProc callback,nint module,uint thread);
    [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")] static extern nint CallNextHookEx(nint hook,int code,nint message,nint data);
    [DllImport("user32.dll")] static extern nint WindowFromPoint(POINT point);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern nint GetModuleHandleW(string? name);
    readonly MouseProc callback;
    readonly nint hook;
    bool disposed;
    public FlyoutDismiss(nint panel,Action dismiss){
        callback=(code,message,data)=>{
            if(!disposed&&code>=0&&(message.ToInt32() is 0x201 or 0x204 or 0x207)){
                var point=Marshal.PtrToStructure<POINT>(data);
                if(GetWindowRect(panel,out var r)&&!(point.X>=r.Left&&point.X<r.Right&&point.Y>=r.Top&&point.Y<r.Bottom)){
                    bool ownPopup=false;
                    nint owner=WindowFromPoint(point);
                    for(int i=0;owner!=0&&i<16;i++,owner=GetWindow(owner,4))if(owner==panel){ownPopup=true;break;}
                    if(!ownPopup)dismiss();
                }
            }
            return CallNextHookEx(0,code,message,data);
        };
        hook=SetWindowsHookExW(14,callback,GetModuleHandleW(null),0);
    }
    public void Dispose(){if(disposed)return;disposed=true;if(hook!=0)UnhookWindowsHookEx(hook);}
}
