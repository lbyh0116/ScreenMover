using System.Diagnostics;
using ScreenMover.Models;
using static ScreenMover.Interop.Native;
namespace ScreenMover.Services;

public sealed record SavedWindow(long Handle,int Pid,long ProcessStarted,string Title,string ProcessName,
    string DisplayKey,RECT MonitorBounds,RECT ScreenRect,WINDOWPLACEMENT Placement) {
    [System.Text.Json.Serialization.JsonIgnore] public WindowInfo Window=>new((nint)Handle,Pid,ProcessName,Title,Placement.showCmd,Placement.rcNormalPosition,"");
}
public sealed class MoveHistory {
    List<SavedWindow> undo=[];
    public int Count=>undo.Count;
    public IReadOnlyList<OperationResult> Move(IReadOnlyList<WindowInfo> items,DisplayInfo target,int margin=60,bool shrink=true) {
        var batch=new List<SavedWindow>();var results=new List<OperationResult>();
        foreach(var w in items) {
            try {
                var saved=Capture(w);
                var result=new WindowService().Move(w,target,margin,shrink);
                results.Add(result);
                // A partially failed move can still have changed geometry or state.
                if(result.Success)batch.Add(saved);
                else if(Alive(saved)){
                    try {
                        var after=WindowService.Placement(w.Handle);
                        if(!after.rcNormalPosition.Equals(saved.Placement.rcNormalPosition)||after.showCmd!=saved.Placement.showCmd)batch.Add(saved);
                    }catch{ /* A window closing after the attempt must not add a second result. */ }
                }
            }catch(Exception ex){results.Add(new(w.Title,false,ex.Message));}
        }
        if(batch.Count>0)undo=batch;
        return results;
    }
    public IReadOnlyList<OperationResult> Undo() {
        var failed=new List<SavedWindow>();var results=new List<OperationResult>();
        foreach(var saved in undo) {
            var result=Restore(saved);results.Add(result);
            if(!result.Success)failed.Add(saved);
        }
        undo=failed;return results;
    }
    public static bool Alive(SavedWindow saved) {
        try {
            GetWindowThreadProcessId((nint)saved.Handle,out int pid);
            using var p=Process.GetProcessById(saved.Pid);
            return IsWindow((nint)saved.Handle)&&pid==saved.Pid&&p.StartTime.ToUniversalTime().Ticks==saved.ProcessStarted;
        }catch{return false;}
    }
    public static SavedWindow Capture(WindowInfo w) {
        GetWindowThreadProcessId(w.Handle,out int pid);
        if(!IsWindow(w.Handle)||pid!=w.Pid)throw new InvalidOperationException("窗口已关闭或已被替换");
        using var process=Process.GetProcessById(w.Pid);
        if(w.ProcessStartedTicks>0&&process.StartTime.ToUniversalTime().Ticks!=w.ProcessStartedTicks)throw new InvalidOperationException("原进程已重启，未操作替代窗口");
        var placement=WindowService.StablePlacement(w.Handle);
        string gdi=WindowService.MonitorNameForWindow(w.Handle);
        var display=new DisplayService().Read().Displays.First(d=>d.Active&&d.Gdi==gdi);
        return new(w.Handle.ToInt64(),w.Pid,process.StartTime.ToUniversalTime().Ticks,w.Title,w.ProcessName,
            display.Key,display.Bounds,WindowService.ToScreen(placement.rcNormalPosition,display),placement);
    }
    public static OperationResult Restore(SavedWindow saved) {
        try {
            if(!Alive(saved))return new(saved.Title,false,"原窗口已关闭或进程已重启，未操作其他窗口");
            var displays=new DisplayService().Read().Displays.Where(d=>d.Active).ToList();
            var original=displays.FirstOrDefault(d=>d.Key==saved.DisplayKey);
            var target=original??displays.FirstOrDefault(d=>d.Primary)??displays.First();
            var rect=saved.ScreenRect;
            if(original!=null){
                int dx=target.Bounds.Left-saved.MonitorBounds.Left,dy=target.Bounds.Top-saved.MonitorBounds.Top;
                rect=new(){Left=rect.Left+dx,Right=rect.Right+dx,Top=rect.Top+dy,Bottom=rect.Bottom+dy};
            }
            if(original==null||original.Bounds.Width!=saved.MonitorBounds.Width||original.Bounds.Height!=saved.MonitorBounds.Height)rect=WindowService.ClampToWork(rect,target.Work);
            var placement=saved.Placement;placement.flags|=4;
            placement.rcNormalPosition=WindowService.ToWorkspace(rect,target);
            if(!SetWindowPlacement((nint)saved.Handle,ref placement))throw new System.ComponentModel.Win32Exception();
            bool confirmed=false;
            for(int i=0;i<40;i++){
                var actual=WindowService.Placement((nint)saved.Handle);
                if(actual.showCmd==saved.Placement.showCmd&&Math.Abs(actual.rcNormalPosition.Left-placement.rcNormalPosition.Left)<=4&&Math.Abs(actual.rcNormalPosition.Top-placement.rcNormalPosition.Top)<=4&&Math.Abs(actual.rcNormalPosition.Width-placement.rcNormalPosition.Width)<=4&&Math.Abs(actual.rcNormalPosition.Height-placement.rcNormalPosition.Height)<=4){confirmed=true;break;}
                Thread.Sleep(25);
            }
            return new(saved.Title,confirmed,confirmed?(original==null?"原屏幕已断开，已恢复到当前可用屏幕":"已恢复原位置、大小和状态"):"应用未确认恢复位置或状态，可重试撤销");
        }catch(Exception ex){return new(saved.Title,false,ex.Message);}
    }
}
