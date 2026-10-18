using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ScreenMover.Models;
using static ScreenMover.Interop.Native;
namespace ScreenMover.Services;
public sealed class WindowService {
public static WINDOWPLACEMENT Placement(nint h) {
var p=new WINDOWPLACEMENT{length=Marshal.SizeOf<WINDOWPLACEMENT>()};
if(!GetWindowPlacement(h,ref p)) throw new Win32Exception(Marshal.GetLastWin32Error());
return p;
}
public static WINDOWPLACEMENT StablePlacement(nint h) {
var last=Placement(h);int stable=0;
for(int i=0;i<25;i++){
Thread.Sleep(40);var current=Placement(h);
stable=current.showCmd==last.showCmd&&current.rcNormalPosition.Equals(last.rcNormalPosition)?stable+1:0;
last=current;if(stable>=5)return current;
}
return last;
}
public static string MonitorName(RECT r) {
var h=MonitorFromPoint(new POINT{X=r.Left+r.Width/2,Y=r.Top+r.Height/2},2);
var m=new MONITORINFOEX{cbSize=Marshal.SizeOf<MONITORINFOEX>()};
return GetMonitorInfoW(h,ref m)?m.szDevice:"";
}
public List<WindowInfo> Read() {
var result=new List<WindowInfo>();
EnumWindows((h,l)=>{
try {
if(!IsWindowVisible(h)||GetWindowTextLengthW(h)==0||GetWindow(h,4)!=0||(GetWindowLongPtr(h,-20).ToInt64()&0x80)!=0) return true;
if(DwmGetWindowAttribute(h,14,out int cloaked,4)==0&&cloaked!=0) return true;
GetWindowThreadProcessId(h,out int pid); if(pid==Environment.ProcessId) return true;
var cls=new StringBuilder(256); GetClassNameW(h,cls,256);
if(cls.ToString() is "Progman" or "WorkerW" or "Shell_TrayWnd") return true;
var title=new StringBuilder(GetWindowTextLengthW(h)+2); GetWindowTextW(h,title,title.Capacity);
var p=Placement(h); string process;long started=0;
try {using var proc=Process.GetProcessById(pid); process=proc.ProcessName;started=proc.StartTime.ToUniversalTime().Ticks;} catch {process="已退出 / 不可访问";}
result.Add(new(h,pid,process,title.ToString(),p.showCmd,p.rcNormalPosition,MonitorNameForWindow(h)){ProcessStartedTicks=started});
} catch(Win32Exception) {}
return true;},0);
return result.OrderBy(w=>w.ProcessName).ThenBy(w=>w.Title).ToList();
}
public static RECT Fit(RECT current,RECT work,int margin=60,bool shrink=true) {
int mx=Math.Min(Math.Max(0,margin),Math.Max(0,work.Width/4)),my=Math.Min(Math.Max(0,margin),Math.Max(0,work.Height/4));
int aw=Math.Max(1,work.Width-2*mx),ah=Math.Max(1,work.Height-2*my);
double width=current.Width>0?current.Width:aw,height=current.Height>0?current.Height:ah;
double scale=shrink?Math.Min(1,Math.Min(aw/width,ah/height)):1;
int w=Math.Max(1,(int)(width*scale)),h=Math.Max(1,(int)(height*scale));
return new(){Left=work.Left+mx,Top=work.Top+my,Right=work.Left+mx+w,Bottom=work.Top+my+h};
}
public static string MonitorNameForWindow(nint h) {
var m=new MONITORINFOEX{cbSize=Marshal.SizeOf<MONITORINFOEX>()};
return GetMonitorInfoW(MonitorFromWindow(h,2),ref m)?m.szDevice:"";
}
public static RECT ToScreen(RECT r,DisplayInfo d)=>Offset(r,d.Work.Left-d.Bounds.Left,d.Work.Top-d.Bounds.Top);
public static RECT ToWorkspace(RECT r,DisplayInfo d)=>Offset(r,d.Bounds.Left-d.Work.Left,d.Bounds.Top-d.Work.Top);
static RECT Offset(RECT r,int dx,int dy)=>new(){Left=r.Left+dx,Right=r.Right+dx,Top=r.Top+dy,Bottom=r.Bottom+dy};
public static RECT ClampToWork(RECT r,RECT work) {
int width=Math.Min(Math.Max(1,r.Width),Math.Max(1,work.Width)),height=Math.Min(Math.Max(1,r.Height),Math.Max(1,work.Height));
int left=Math.Clamp(r.Left,work.Left,Math.Max(work.Left,work.Right-width)),top=Math.Clamp(r.Top,work.Top,Math.Max(work.Top,work.Bottom-height));
return new(){Left=left,Top=top,Right=left+width,Bottom=top+height};
}
static void CheckIdentity(WindowInfo w) {
GetWindowThreadProcessId(w.Handle,out int pid);
if(!IsWindow(w.Handle)||pid!=w.Pid) throw new InvalidOperationException("窗口已关闭或已被替换");
if(w.ProcessStartedTicks>0){using var process=Process.GetProcessById(w.Pid);if(process.StartTime.ToUniversalTime().Ticks!=w.ProcessStartedTicks)throw new InvalidOperationException("原进程已重启，未操作替代窗口");}
}
static string Error(int e)=>e==5?"权限不足，请以管理员身份重启":e==0?"应用拒绝操作或未确认状态":new Win32Exception(e).Message+$" ({e})";
public OperationResult Move(WindowInfo w,DisplayInfo requested,int margin=60,bool shrink=true) {
try {
CheckIdentity(w);
var target=new DisplayService().Read().Displays.FirstOrDefault(x=>x.Key==requested.Key&&x.Active);
if(target is null) return new(w.Title,false,"目标屏幕已断开或未启用");
var p=Placement(w.Handle); var rect=Fit(p.rcNormalPosition,target.Work,margin,shrink); bool ok; int err;
if(p.showCmd is 2 or 7) {
p.rcNormalPosition=ToWorkspace(rect,target);p.flags|=4; ok=SetWindowPlacement(w.Handle,ref p); err=Marshal.GetLastWin32Error();
if(ok&&!IsIconic(w.Handle)) return new(w.Title,false,"应用改变了最小化状态");
} else {
bool maximized=p.showCmd==3;
if(maximized) {ShowWindowAsync(w.Handle,9); if(!WaitFor(()=>Placement(w.Handle).showCmd!=3)) return new(w.Title,false,"还原超时或权限不足");}
ok=SetWindowPos(w.Handle,0,rect.Left,rect.Top,rect.Width,rect.Height,0x4|0x10|0x4000);
err=Marshal.GetLastWin32Error();
if(ok) ok=WaitFor(()=>{var actual=ToScreen(Placement(w.Handle).rcNormalPosition,target); return Math.Abs(actual.Left-rect.Left)<=4&&Math.Abs(actual.Top-rect.Top)<=4&&MonitorNameForWindow(w.Handle)==target.Gdi;});
if(maximized) {ShowWindowAsync(w.Handle,3); if(ok) ok=WaitFor(()=>Placement(w.Handle).showCmd==3);}
}
if(!ok) return new(w.Title,false,Error(err));
if(!WaitFor(()=>MonitorNameForWindow(w.Handle)==target.Gdi)) return new(w.Title,false,"窗口未落到目标屏幕（应用拒绝或权限不足）");
return new(w.Title,true,$"已移至屏幕 {target.Number}");
} catch(Exception e) {return new(w.Title,false,e.Message);}
}
static bool WaitFor(Func<bool> condition) {for(int i=0;i<20;i++){if(condition()) return true; Thread.Sleep(25);} return false;}
public OperationResult ChangeState(WindowInfo w,int command) {
try {CheckIdentity(w); if(!ShowWindowAsync(w.Handle,command)) return new(w.Title,false,Error(Marshal.GetLastWin32Error()));
bool ok=WaitFor(()=>command==2?IsIconic(w.Handle):command==3?Placement(w.Handle).showCmd==3:!IsIconic(w.Handle)&&Placement(w.Handle).showCmd!=3);
if(ok)StablePlacement(w.Handle);
return new(w.Title,ok,ok?"状态已更新":"状态未改变（权限不足或应用拒绝）");
} catch(Exception e){return new(w.Title,false,e.Message);}
}
public OperationResult Activate(WindowInfo w) {
try {CheckIdentity(w); if(IsIconic(w.Handle)) {var r=ChangeState(w,9); if(!r.Success)return r;}
bool ok=SetForegroundWindow(w.Handle); return new(w.Title,ok,ok?"已激活":"系统限制了前台切换，请点击任务栏窗口");}
catch(Exception e){return new(w.Title,false,e.Message);}
}
}
