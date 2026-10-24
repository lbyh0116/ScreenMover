using System.Diagnostics;
using ScreenMover.Models;
namespace ScreenMover.Services;

public static class VirtualDesktopService {
    public static string HelperPath {get;set;}=Path.Combine(AppContext.BaseDirectory,"Helpers","ScreenMover.DesktopHelper.exe");
    public static (bool Success,string Output) Run(params string[] args) {
        var info=new ProcessStartInfo(HelperPath){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=System.Text.Encoding.UTF8};
        foreach(var arg in args)info.ArgumentList.Add(arg);
        using var process=Process.Start(info)!;
        var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();
        if(!process.WaitForExit(15000)){process.Kill();return (false,L.T("操作超时，记录已保留，请在任务视图确认状态或重试找回"));}
        string result=output.GetAwaiter().GetResult().Trim();
        return (process.ExitCode==0&&result.StartsWith("OK|",StringComparison.Ordinal),string.IsNullOrEmpty(result)?error.GetAwaiter().GetResult():result);
    }
}
public sealed record HiddenWindow(Guid Id,SavedWindow Saved,string DesktopName,string? DesktopId,DateTime HiddenAt,string Status) {
    public bool Closed {get;init;}
    [System.Text.Json.Serialization.JsonIgnore] public string DisplayStatus=>Closed?L.T("原窗口已关闭 / 进程已重启"):L.External(Status.Replace("ERROR|","").Split("|desktop=")[0]);
    public override string ToString()=>L.F("{0}\n桌面：{1} · {2:MM-dd HH:mm}\n{3}", Saved.Title, DesktopName, HiddenAt, DisplayStatus);
}
public sealed class HiddenWindows {
    readonly object gate=new();
    readonly string path;
    List<HiddenWindow> records;
    public string? LoadWarning {get;}
    public HiddenWindows(string? file=null) {
        path=file??Path.Combine(LocalData.Folder,"hidden-windows.json");
        records=LocalData.Read(path,()=>new List<HiddenWindow>(),out var warning);LoadWarning=warning;
    }
    public List<HiddenWindow> Read(){lock(gate)return records.ToList();}
    public int ActiveCount {get {lock(gate)return records.Count(x=>!x.Closed);}}
    public List<HiddenWindow> Reconcile(){using var state=new HiddenWindowState();return Reconcile(state.Read);}
    public List<HiddenWindow> Reconcile(Func<HiddenWindow,HiddenObservation> inspect){
        var observations=Read().Where(x=>!x.Status.StartsWith("正在隐藏",StringComparison.Ordinal))
            .Select(entry=>(entry,state:inspect(entry))).ToList();
        var returned=new List<HiddenWindow>();
        lock(gate){
            var next=records.ToList();
            foreach(var (entry,state) in observations){
                int index=next.FindIndex(x=>x==entry);if(index<0)continue;
                if(state==HiddenObservation.Returned){next.RemoveAt(index);returned.Add(entry);}
                else if(state==HiddenObservation.Closed)next[index]=entry with{Closed=true};
                else if(state==HiddenObservation.Hidden&&entry.Closed)next[index]=entry with{Closed=false};
            }
            if(!next.SequenceEqual(records)){LocalData.Write(path,next);records=next;}
        }
        return returned;
    }
    void Put(HiddenWindow entry) {
        lock(gate){
            var next=records.Where(x=>x.Id!=entry.Id).Append(entry).ToList();
            LocalData.Write(path,next);records=next;
        }
    }
    public void Forget(Guid id) {
        lock(gate){var next=records.Where(x=>x.Id!=id).ToList();LocalData.Write(path,next);records=next;}
    }
    public OperationResult Hide(WindowInfo window) {
        if(!PlatformSupport.Current.CanHide)return new(window.Title,false,PlatformSupport.Current.HideStatus);
        HiddenWindow? entry=null;
        try {
            if(Read().Count>0)Reconcile();
            if(Read().Any(x=>x.Saved.Handle==window.Handle.ToInt64()&&MoveHistory.Alive(x.Saved)))
                return new(window.Title,false,L.T("此窗口已有隐藏记录，请先从「已隐藏」找回"));
            var saved=MoveHistory.Capture(window);
            using var target=Process.GetProcessById(window.Pid);
            string name=window.ProcessName;
            try {name=target.MainModule?.FileVersionInfo.FileDescription??name;}catch{}
            if(string.IsNullOrWhiteSpace(name))name=window.ProcessName;
            entry=new(Guid.NewGuid(),saved,name,null,DateTime.Now,"正在隐藏 / 若操作中断可尝试找回");
            Put(entry); // Durable recovery intent precedes the external mutation.
            var result=VirtualDesktopService.Run("hide",saved.Handle.ToString(),saved.Pid.ToString(),name,saved.ProcessStarted.ToString());
            if(result.Success) {
                string desktop=result.Output.Split('|')[1];
                bool minimized=!result.Output.EndsWith("|desktop-only",StringComparison.Ordinal);
                Put(entry with{DesktopId=desktop,Status=minimized?"已隐藏 · 已最小化":"已隐藏在独立桌面 · 应用未接受最小化"});
                return new(window.Title,true,minimized?L.F("已最小化至「{0}」，可从「已隐藏」一键找回", name):L.F("已移至独立桌面「{0}」；应用未接受最小化，保留隐藏位置，可一键找回", name));
            }
            string? failedDesktop=result.Output.Split('|').FirstOrDefault(x=>x.StartsWith("desktop="))?[8..];
            Put(entry with{DesktopId=Guid.TryParse(failedDesktop,out var desktopId)?desktopId.ToString():null,Status=result.Output});
            return new(window.Title,false,L.External(result.Output)+L.T("；恢复记录已保留"));
        }catch(Exception ex){
            return new(window.Title,false,ex.Message+(entry==null?"":L.T("；请查看已隐藏记录或任务视图")));
        }
    }
    public OperationResult Recover(HiddenWindow entry) {
        try {
            if(!MoveHistory.Alive(entry.Saved))return new(entry.Saved.Title,false,L.T("原窗口已关闭或进程已重启；可移除此失效记录"));
            var s=entry.Saved;
            var result=VirtualDesktopService.Run("restore",s.Handle.ToString(),s.Pid.ToString(),s.Placement.showCmd.ToString(),s.ProcessStarted.ToString());
            if(!result.Success){Put(entry with{Status=result.Output});return new(s.Title,false,L.External(result.Output));}
            Forget(entry.Id);
            return new(s.Title,true,L.T("已找回当前虚拟桌面并还原；原虚拟桌面保留"));
        }catch(Exception ex){return new(entry.Saved.Title,false,ex.Message+L.T("；记录保留，可重试"));}
    }
}
