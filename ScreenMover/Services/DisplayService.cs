using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using ScreenMover.Models;
using static ScreenMover.Interop.Native;
namespace ScreenMover.Services;
public sealed class DisplayService {
public static string Key(PathInfo p)=>$"{p.targetInfo.adapterId.HighPart}:{p.targetInfo.adapterId.LowPart}:{p.targetInfo.id}";
static PathInfo[] Query(uint flag) {
for(int attempt=0;attempt<3;attempt++) {
int e=GetDisplayConfigBufferSizes(flag,out uint pc,out uint mc);
if(e!=0) throw new Win32Exception(e);
var p=new PathInfo[pc]; var m=new ModeInfo[mc];
e=QueryDisplayConfig(flag,ref pc,p,ref mc,m,0);
if(e==0) return p.Take((int)pc).ToArray();
if(e!=122) throw new Win32Exception(e);
} throw new Win32Exception(122,"显示配置变化过快，请重试");
}
static string Name(PathInfo p) {
var n=new TargetDeviceName { header=new() {type=2,size=(uint)Marshal.SizeOf<TargetDeviceName>(),adapterId=p.targetInfo.adapterId,id=p.targetInfo.id}};
return DisplayConfigGetDeviceInfo(ref n)==0?n.monitorFriendlyDeviceName:"";
}
static string Gdi(PathInfo p) {
var n=new SourceDeviceName {header=new(){type=1,size=(uint)Marshal.SizeOf<SourceDeviceName>(),adapterId=p.sourceInfo.adapterId,id=p.sourceInfo.id}};
int e=DisplayConfigGetDeviceInfo(ref n); if(e!=0) throw new Win32Exception(e); return n.viewGdiDeviceName;
}
public DisplaySnapshot Read() {
var active=Query(2); var all=Query(1);
var monitors=new Dictionary<string,(nint h,MONITORINFOEX m)>();
EnumDisplayMonitors(0,0,(nint h,nint dc,ref RECT r,nint data)=>{
var m=new MONITORINFOEX {cbSize=Marshal.SizeOf<MONITORINFOEX>()};
if(GetMonitorInfoW(h,ref m)) monitors[m.szDevice]=(h,m); return true;},0);
var drivers=new Dictionary<string,string>();
for(uint i=0;;i++) {var d=new DISPLAY_DEVICE{cb=Marshal.SizeOf<DISPLAY_DEVICE>()}; if(!EnumDisplayDevices(0,i,ref d,0)) break; drivers[d.DeviceName]=d.DeviceString;}
var result=new List<DisplayInfo>(); var keys=active.Select(Key).ToHashSet();
foreach(var p in active.DistinctBy(Key)) {
string gdi=Gdi(p); if(!monitors.TryGetValue(gdi,out var mon)) throw new InvalidOperationException($"拓扑变化：{gdi} 尚无桌面几何，请刷新");
string name=Name(p); if(string.IsNullOrWhiteSpace(name)) name="虚拟 / 间接显示器";
int number=int.TryParse(Regex.Match(gdi,@"\d+$").Value,out int n)?n:0;
result.Add(new(Key(p),number,gdi,name,drivers.GetValueOrDefault(gdi,""),true,(mon.m.dwFlags&1)!=0,mon.m.rcMonitor,mon.m.rcWork,p.targetInfo.refreshRate.Denominator==0?0:(double)p.targetInfo.refreshRate.Numerator/p.targetInfo.refreshRate.Denominator,mon.h));
}
if(!result.Select(x=>x.Gdi).ToHashSet().SetEquals(monitors.Keys)) throw new InvalidOperationException("QueryDisplayConfig 与桌面显示器不一致，请刷新");
foreach(var p in all.Where(p=>p.targetInfo.targetAvailable!=0&&!keys.Contains(Key(p))).DistinctBy(Key)) {
string name=Name(p); if(!string.IsNullOrWhiteSpace(name)) result.Add(new(Key(p),0,"",name,"",false,false,default,default,0,0));
}
return new(result.OrderByDescending(x=>x.Active).ThenBy(x=>x.Number).ToList(),all.Length,active.Length,monitors.Count);
}
}
