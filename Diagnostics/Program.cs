using System.Runtime.InteropServices;
using ScreenMover.Services;
using static ScreenMover.Interop.Native;
if(args.Length==2&&args[0]=="--tray"){
if(!TrayIcon.TryGetBounds((nint)long.Parse(args[1]),out var trayRect))throw new Exception("Tray icon is not registered");
Console.WriteLine("PASS tray icon registered: "+trayRect);return;
}
Console.WriteLine($"Timestamp {DateTimeOffset.Now:O}");
Console.WriteLine($"PATH_INFO={Marshal.SizeOf<PathInfo>()} MODE_INFO={Marshal.SizeOf<ModeInfo>()}");
Regression.Pure();
if(args.Length==1&&args[0]=="--pure")return;
var d=new DisplayService().Read();
Console.WriteLine($"AllPaths={d.AllPaths} ActivePaths={d.ActivePaths} DesktopCount={d.DesktopCount} Inactive={d.Displays.Count(x=>!x.Active)}");
foreach(var m in d.Displays) Console.WriteLine($"{(m.Active?"ACTIVE":"NOT-IN-USE")} Key={m.Key} Gdi={m.Gdi} Name={m.Name} Driver={m.Driver} Bounds={m.Bounds} Work={m.Work} Hz={m.Hertz}");
foreach(var w in new WindowService().Read())Console.WriteLine($"WINDOW 0x{w.Handle:X} | {w.Gdi} | {w.Title} | {w.Details}");
var fit=WindowService.Fit(new RECT{Right=3000,Bottom=2000},new RECT{Left=-1200,Top=1152,Right=0,Bottom=1952});
if(fit.Left!=-1140||fit.Top!=1212||fit.Right>0||fit.Bottom>1952)throw new Exception("Fit regression");
Console.WriteLine("PASS Fit: negative desktop coordinates, proportional shrink, physical 60px margin");
if(args.Length>0) {
string exe=Path.GetFullPath(args[0]);
using var fixture=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe,"--fixture"){UseShellExecute=false})!;
try {
var service=new WindowService(); ScreenMover.Models.WindowInfo? w=null;
for(int i=0;i<100;i++){Thread.Sleep(100);w=service.Read().FirstOrDefault(x=>x.Pid==fixture.Id);if(w!=null)break;}
if(w==null)throw new Exception("Fixture did not start");
var target=d.Displays.FirstOrDefault(x=>x.Active&&x.Gdi!=w.Gdi)??d.Displays.First(x=>x.Active);
Console.WriteLine($"Fixture PID={fixture.Id}; destination={target.Gdi}; mode={(d.DesktopCount>1?"CROSS-SCREEN AVAILABLE":"SAME-SCREEN ONLY")}");
foreach(int cmd in new[]{9,2,3}) {
var state=service.ChangeState(w,cmd); if(!state.Success)throw new Exception(state.Message);
bool before=IsIconic(w.Handle);
var move=service.Move(w,target);
bool after=IsIconic(w.Handle);
var p=WindowService.Placement(w.Handle);GetWindowRect(w.Handle,out var rect);
Console.WriteLine($"Command={cmd}; move={move.Success}; beforeIconic={before}; afterIconic={after}; show={p.showCmd}; restore={p.rcNormalPosition}; actual={rect}; {move.Message}");
if(!move.Success||(cmd==2&&(!before||!after))||(cmd==3&&p.showCmd!=3))throw new Exception("Movement regression");
}
var helper=Path.Combine(Path.GetDirectoryName(exe)!,"Helpers","ScreenMover.DesktopHelper.exe");
if(File.Exists(helper)){
var psi=new System.Diagnostics.ProcessStartInfo(helper){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true};
foreach(var arg in new[]{"selftest",w.Handle.ToInt64().ToString(),w.Pid.ToString(),"ScreenMover 隐藏测试"})psi.ArgumentList.Add(arg);
using var child=System.Diagnostics.Process.Start(psi)!;
Console.WriteLine(child.StandardOutput.ReadToEnd());
if(!child.WaitForExit(15000)||child.ExitCode!=0)throw new Exception("Virtual desktop regression");
Thread.Sleep(300);
if(WindowService.Placement(w.Handle).showCmd!=3)throw new Exception("Virtual desktop restore state regression");
Console.WriteLine("PASS virtual desktop create, rename, exact-window move, minimize, rollback and cleanup");
}
Regression.Fixture(w,helper);
var gone=w with {Handle=0}; var failure=service.Move(gone,target);
if(failure.Success)throw new Exception("Invalid window accepted");
Console.WriteLine($"PASS stale-window failure: {failure.Message}");
} finally {fixture.CloseMainWindow();if(!fixture.WaitForExit(3000))fixture.Kill();}
}
