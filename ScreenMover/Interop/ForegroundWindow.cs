using System.Runtime.InteropServices;
namespace ScreenMover.Interop;

public static class ForegroundWindow {
    [DllImport("user32.dll")] static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] static extern nint GetAncestor(nint hwnd,uint flags);
    public static (nint Handle,nint Owner,int Pid) Capture(){
        var handle=GetForegroundWindow();Native.GetWindowThreadProcessId(handle,out int pid);
        return (handle,GetAncestor(handle,3),pid);
    }
}
