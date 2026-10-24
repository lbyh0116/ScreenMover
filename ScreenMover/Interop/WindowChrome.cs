using System.Runtime.InteropServices;
using static ScreenMover.Interop.Native;
namespace ScreenMover.Interop;

public sealed class WindowChrome : IDisposable {
    [StructLayout(LayoutKind.Sequential)] struct MinMaxInfo {public POINT Reserved,MaxSize,MaxPosition,MinTrackSize,MaxTrackSize;}
    [DllImport("shcore.dll")] static extern int GetDpiForMonitor(nint monitor,int type,out uint x,out uint y);
    readonly nint window;
    readonly SubclassProc callback;
    public static double ScaleAt(POINT point) {
        return GetDpiForMonitor(MonitorFromPoint(point,2),0,out uint x,out _)==0?x/96.0:1;
    }
    public WindowChrome(nint hwnd) {
        window=hwnd;
        callback=(h,msg,w,l,id,data)=>{
            var result=DefSubclassProc(h,msg,w,l);
            if(msg==0x24&&l!=0) {
                var m=new MONITORINFOEX{cbSize=Marshal.SizeOf<MONITORINFOEX>()};
                if(GetMonitorInfoW(MonitorFromWindow(h,2),ref m)) {
                    double scale=Services.TrayIcon.GetDpiForWindow(h)/96.0;
                    var limits=Marshal.PtrToStructure<MinMaxInfo>(l);
                    limits.MinTrackSize=new(){X=Math.Min((int)(720*scale),m.rcWork.Width),Y=Math.Min((int)(640*scale),m.rcWork.Height)};
                    Marshal.StructureToPtr(limits,l,false);
                }
            }
            return result;
        };
        if(!SetWindowSubclass(window,callback,43,0))throw new InvalidOperationException(L.T("无法设置窗口尺寸约束"));
    }
    public void Dispose()=>RemoveWindowSubclass(window,callback,43);
}
