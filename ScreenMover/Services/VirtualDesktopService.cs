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
        if(!process.WaitForExit(15000)){process.Kill();return (false,"操作超时，记录已保留，请在任务视图确认状态或重试找回");}
        string result=output.GetAwaiter().GetResult().Trim();
        return (process.ExitCode==0&&result.StartsWith("OK|",StringComparison.Ordinal),string.IsNullOrEmpty(result)?error.GetAwaiter().GetResult():result);
    }
}
public sealed record HiddenWindow(Guid Id,SavedWindow Saved,string DesktopName,string? DesktopId,DateTime HiddenAt,string Status) {
    public override string ToString()=>$"{Saved.Title}\n桌面：{DesktopName} · {HiddenAt:MM-dd HH:mm}\n{Status}";
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
        HiddenWindow? entry=null;
        try {
            if(Read().Any(x=>x.Saved.Handle==window.Handle.ToInt64()&&MoveHistory.Alive(x.Saved)))
                return new(window.Title,false,"此窗口已有隐藏记录，请先从「已隐藏」找回");
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
                Put(entry with{DesktopId=desktop,Status="已隐藏"});
                return new(window.Title,true,$"已最小化至「{name}」，可从「已隐藏」一键找回");
            }
            Put(entry with{Status=result.Output});
            return new(window.Title,false,result.Output+"；恢复记录已保留");
        }catch(Exception ex){
            return new(window.Title,false,ex.Message+(entry==null?"":"；请查看已隐藏记录或任务视图"));
        }
    }
    public OperationResult Recover(HiddenWindow entry) {
        try {
            if(!MoveHistory.Alive(entry.Saved))return new(entry.Saved.Title,false,"原窗口已关闭或进程已重启；可移除此失效记录");
            var s=entry.Saved;
            var result=VirtualDesktopService.Run("restore",s.Handle.ToString(),s.Pid.ToString(),s.Placement.showCmd.ToString(),s.ProcessStarted.ToString());
            if(!result.Success){Put(entry with{Status=result.Output});return new(s.Title,false,result.Output);}
            Forget(entry.Id);
            return new(s.Title,true,"已找回当前虚拟桌面并还原；原虚拟桌面保留");
        }catch(Exception ex){return new(entry.Saved.Title,false,ex.Message+"；记录保留，可重试");}
    }
}
