using System;
using System.Runtime.InteropServices;
using System.Threading;
using VirtualDesktop;
using ScreenMover.DesktopLogic;
class Program {
 [DllImport("user32.dll")] static extern bool IsWindow(IntPtr h);
 [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h,out int pid);
 [DllImport("user32.dll")] static extern bool IsIconic(IntPtr h);
 [DllImport("user32.dll")] static extern bool ShowWindowAsync(IntPtr h,int cmd);
 [StructLayout(LayoutKind.Sequential)] struct Point {public int X,Y;}
 [StructLayout(LayoutKind.Sequential)] struct Rect {public int L,T,R,B;}
 [StructLayout(LayoutKind.Sequential)] struct Placement {public int Length,Flags,Show;public Point Min,Max;public Rect Normal;}
 [DllImport("user32.dll")] static extern bool GetWindowPlacement(IntPtr h,ref Placement p);
 [DllImport("user32.dll")] static extern bool SetWindowPlacement(IntPtr h,ref Placement p);
 [STAThread] static int Main(string[] args) {
 Console.OutputEncoding=new System.Text.UTF8Encoding(false);
 IVirtualDesktop created=null, original=null; IApplicationView view=null;
 bool moved=false; IntPtr h=IntPtr.Zero; Placement placement=new Placement();placement.Length=Marshal.SizeOf(placement);
 string stage="初始化";Guid createdId=Guid.Empty;
 try {
 string build=(string)Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion","CurrentBuildNumber","");
if(build!="26100"&&build!="26200")throw new Exception("隐藏模式当前支持 Windows 11 24H2/25H2；此系统版本尚未适配");
var m=DesktopManager.VirtualDesktopManagerInternal;
 if(args.Length==0||args[0]=="probe"){Console.WriteLine("OK|"+m.GetCount()+"|"+m.GetCurrentDesktop().GetId());return 0;}
 if(args[0]=="cleanup-empty"){
 Guid cleanupId=new Guid(args[1]);var fallback=m.GetCurrentDesktop();
 if(cleanupId==fallback.GetId())throw new Exception("不能清理当前桌面");
 var desktop=m.FindDesktop(ref cleanupId);
 IObjectArray views;DesktopManager.ApplicationViewCollection.GetViews(out views);
 int count;views.GetCount(out count);
 for(int i=0;i<count;i++){object item;views.GetAt(i,typeof(IApplicationView).GUID,out item);Guid viewId;((IApplicationView)item).GetVirtualDesktopId(out viewId);if(viewId==cleanupId)throw new Exception("桌面仍有窗口，已保留");}
 m.RemoveDesktop(desktop,fallback);Console.WriteLine("OK|"+m.GetCount());return 0;
 }
 h=new IntPtr(long.Parse(args[1])); int pid; GetWindowThreadProcessId(h,out pid);
 if(!IsWindow(h)||pid!=int.Parse(args[2]))throw new Exception("窗口已关闭或已被替换");
 if(args.Length>=5){using(var process=System.Diagnostics.Process.GetProcessById(pid)){if(process.StartTime.ToUniversalTime().Ticks!=long.Parse(args[4]))throw new Exception("进程已重启，未操作窗口");}}
 view=h.GetApplicationView();
 string appId; view.GetAppUserModelId(out appId);
 if(DesktopManager.VirtualDesktopPinnedApps.IsViewPinned(view)||(!String.IsNullOrEmpty(appId)&&DesktopManager.VirtualDesktopPinnedApps.IsAppIdPinned(appId)))throw new Exception("窗口固定在所有桌面上，请先取消固定");
 if(!m.CanViewMoveDesktops(view))throw new Exception("此窗口不允许跨虚拟桌面移动");
 Guid id=DesktopManager.VirtualDesktopManager.GetWindowDesktopId(h);
 original=m.FindDesktop(ref id);if(!GetWindowPlacement(h,ref placement))throw new Exception("无法读取窗口状态");
  if(args[0]=="restore"){
 stage="找回窗口";
 var destination=m.GetCurrentDesktop();
 m.MoveViewToDesktop(view,destination);moved=true;
 if(DesktopManager.VirtualDesktopManager.GetWindowDesktopId(h)!=destination.GetId())throw new Exception("窗口未回到当前桌面");
 int show=int.Parse(args[3])==3?3:9;
 ShowWindowAsync(h,show);
 bool confirmed=false;
 for(int i=0;i<40;i++){Placement actual=new Placement();actual.Length=Marshal.SizeOf(actual);if(GetWindowPlacement(h,ref actual)&&!IsIconic(h)&&(show!=3||actual.Show==3)){confirmed=true;break;}Thread.Sleep(25);}
 if(!confirmed)throw new Exception("窗口已移回当前桌面，但应用未确认还原状态，请重试");
 Console.WriteLine("OK|"+destination.GetId()+"|restored");return 0;
 }
 stage="创建桌面";
 created=m.CreateDesktop();createdId=created.GetId();m.SetDesktopName(created,args[3]);
 if(created.GetName()!=args[3])throw new Exception("桌面命名未生效");
 stage="移动窗口";
 m.MoveViewToDesktop(view,created);moved=true;
 Guid targetId=created.GetId();
 for(int i=0;i<20&&DesktopManager.VirtualDesktopManager.GetWindowDesktopId(h)!=targetId;i++)Thread.Sleep(50);
 if(DesktopManager.VirtualDesktopManager.GetWindowDesktopId(h)!=targetId)throw new Exception("窗口未进入新桌面");
 stage="确认隐藏";
 ShowWindowAsync(h,7); // SW_SHOWMINNOACTIVE: do not activate the moved game.
 HideOutcome outcome=HideVerification.Observe(delegate {
     int actualPid;GetWindowThreadProcessId(h,out actualPid);bool exists=IsWindow(h)&&actualPid==pid;
     return new HideSample{Exists=exists,OnTarget=exists&&DesktopManager.VirtualDesktopManager.GetWindowDesktopId(h)==targetId,
         TargetIsCurrent=m.GetCurrentDesktop().GetId()==targetId,Minimized=exists&&IsIconic(h)};
 },Thread.Sleep);
 if(outcome==HideOutcome.Closed)throw new Exception("应用关闭或替换了原窗口；桌面与恢复记录已保留");
 if(outcome==HideOutcome.Returned)throw new Exception("应用返回了可见桌面或桌面已切换，未确认隐藏；桌面与恢复记录已保留。全屏游戏可尝试无边框窗口模式");
 Console.WriteLine("OK|"+targetId+"|"+(outcome==HideOutcome.Minimized?"minimized":"desktop-only"));
 if(args[0]=="selftest"){
 m.MoveViewToDesktop(view,original);moved=false;
 m.RemoveDesktop(created,original);created=null;
 SetWindowPlacement(h,ref placement);
 if(DesktopManager.VirtualDesktopManager.GetWindowDesktopId(h)!=id)throw new Exception("测试窗口还原失败");
 Console.WriteLine("CLEANUP|"+m.GetCount());
 }
 return 0;
 }catch(Exception ex){
 string recovery="";
 // Never expose a successfully moved window just because it refuses minimization.
 try {if(created!=null&&original!=null&&!moved){
     if(IsWindow(h)&&DesktopManager.VirtualDesktopManager.GetWindowDesktopId(h)==created.GetId())moved=true;
     else {DesktopManager.VirtualDesktopManagerInternal.RemoveDesktop(created,original);created=null;}
 }}
 catch(Exception rollback){recovery="；恢复失败："+rollback.Message;}
 string desktop=created==null?"":"|desktop="+createdId;
 Console.WriteLine("ERROR|"+stage+"："+ex.GetBaseException().Message+"（0x"+ex.HResult.ToString("X8")+"）"+recovery+desktop);return 1;
 }
 }
}
