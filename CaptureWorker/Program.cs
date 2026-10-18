using System.Diagnostics;
using System.Runtime.InteropServices;
using ScreenMover.Services;

if(args is ["--self-test"]){
    var icon=Probe.LoadIconW(0,(nint)32512);
    for(int i=0;i<20;i++)SnapshotService.Icon(icon);
    GC.Collect();GC.WaitForPendingFinalizers();
    using var process=Process.GetCurrentProcess();
    uint before=Probe.GetGuiResources(process.Handle,0);
    long start=GC.GetTotalMemory(true);
    for(int i=0;i<2000;i++){
        var frame=SnapshotService.Icon(icon);
        if(frame?.Pixels.Length!=48*48*4)throw new Exception("Icon frame invalid");
    }
    GC.Collect();GC.WaitForPendingFinalizers();
    long end=GC.GetTotalMemory(true);uint after=Probe.GetGuiResources(process.Handle,0);process.Refresh();
    Console.WriteLine($"PASS capture worker: 2000 GDI captures; handles {before}->{after}; managed delta {end-start} bytes; private {process.PrivateMemorySize64/1048576.0:F1} MiB; working set {process.WorkingSet64/1048576.0:F1} MiB; no WinUI loaded");
    if(after>before+1||end-start>1024*1024)throw new Exception("Capture resources grew unexpectedly");
    if(process.Modules.Cast<ProcessModule>().Any(m=>m.ModuleName.Equals("Microsoft.UI.Xaml.dll",StringComparison.OrdinalIgnoreCase)))throw new Exception("Capture worker loaded WinUI");
    return 0;
}
return args.Length is 2 or 3?SnapshotService.CaptureWorker(args):7;

static class Probe {
    [DllImport("user32.dll")] public static extern nint LoadIconW(nint module,nint id);
    [DllImport("user32.dll")] public static extern uint GetGuiResources(nint process,uint flags);
}
