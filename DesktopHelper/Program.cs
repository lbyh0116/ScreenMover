using System;
using System.Runtime.InteropServices;
using System.Threading;
using VirtualDesktop;
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
 created=m.CreateDesktop(); m.SetDesktopName(created,args[3]);
 if(created.GetName()!=args[3])throw new Exception("桌面命名未生效");
 m.MoveViewToDesktop(view,created);moved=true;
 if(DesktopManager.VirtualDesktopManager.GetWindowDesktopId(h)!=created.GetId())throw new Exception("窗口未进入新桌面");
 ShowWindowAsync(h,2);
 for(int i=0;i<20&&!IsIconic(h);i++)Thread.Sleep(50);
 if(!IsIconic(h))throw new Exception("窗口最小化失败");
 Console.WriteLine("OK|"+created.GetId()+"|"+args[3]);
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
 try {if(created!=null&&original!=null){if(moved)DesktopManager.VirtualDesktopManagerInternal.MoveViewToDesktop(view,original);DesktopManager.VirtualDesktopManagerInternal.RemoveDesktop(created,original);if(h!=IntPtr.Zero)SetWindowPlacement(h,ref placement);}}
 catch(Exception rollback){recovery="；恢复失败："+rollback.Message;}
 Console.WriteLine("ERROR|"+ex.GetBaseException().Message+recovery);return 1;
 }
 }
}
