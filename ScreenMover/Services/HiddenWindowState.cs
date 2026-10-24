using System.Runtime.InteropServices;
using ScreenMover.Interop;
namespace ScreenMover.Services;

public enum HiddenObservation { Unknown, Hidden, Returned, Closed }
public sealed class HiddenWindowState : IDisposable {
    [ComImport,Guid("A5CD92FF-29BE-454C-8D04-D82879FB3F1B"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDesktopManager {
        [return:MarshalAs(UnmanagedType.Bool)] bool IsWindowOnCurrentVirtualDesktop(nint window);
        Guid GetWindowDesktopId(nint window);
        void MoveWindowToDesktop(nint window,ref Guid desktop);
    }
    IDesktopManager? manager;
    public HiddenWindowState(){
        try{manager=(IDesktopManager)Activator.CreateInstance(Type.GetTypeFromCLSID(new("AA509086-5CA9-4C25-8F95-589D3C07B48A"))!)!;}catch{}
    }
    public HiddenObservation Read(HiddenWindow entry){
        try{
            nint handle=(nint)entry.Saved.Handle;
            Native.GetWindowThreadProcessId(handle,out int pid);
            if(!Native.IsWindow(handle)||pid!=entry.Saved.Pid)return HiddenObservation.Closed;
            using var process=System.Diagnostics.Process.GetProcessById(pid);
            if(process.StartTime.ToUniversalTime().Ticks!=entry.Saved.ProcessStarted)return HiddenObservation.Closed;
            if(manager==null)return HiddenObservation.Unknown;
            Guid actual=manager.GetWindowDesktopId(handle);
            bool current=manager.IsWindowOnCurrentVirtualDesktop(handle);
            return Classify(entry,actual,current,Native.IsIconic(handle));
        }catch{return HiddenObservation.Unknown;} // Access denied is not evidence that a window closed.
    }
    public static HiddenObservation Classify(HiddenWindow entry,Guid actual,bool current,bool minimized){
        if(actual==Guid.Empty)return HiddenObservation.Unknown;
        if(Guid.TryParse(entry.DesktopId,out var expected))
            return actual==expected?HiddenObservation.Hidden:HiddenObservation.Returned;
        // Legacy failed attempts have no destination ID. Do not discard uncertain recovery intents.
        if(entry.Status.StartsWith("ERROR|",StringComparison.Ordinal)&&current&&!minimized)return HiddenObservation.Returned;
        return HiddenObservation.Unknown;
    }
    public void Dispose(){if(manager!=null){Marshal.ReleaseComObject(manager);manager=null;}}
}
