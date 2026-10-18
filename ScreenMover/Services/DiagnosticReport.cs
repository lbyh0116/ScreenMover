using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using ScreenMover.Models;
namespace ScreenMover.Services;

public static class DiagnosticReport {
    public static string Build(AppSettings settings,IEnumerable<DisplayInfo> displays){
        var safe=settings with{};safe.Normalize();
        return JsonSerializer.Serialize(new{
            Format="ScreenMover.Diagnostics",Version=1,CreatedAt=DateTimeOffset.Now,
            AppVersion=BuildInfo.FullVersion,
            BuildId=typeof(DiagnosticReport).Module.ModuleVersionId,
            OS=RuntimeInformation.OSDescription,Architecture=RuntimeInformation.ProcessArchitecture.ToString(),Runtime=RuntimeInformation.FrameworkDescription,
            Settings=safe,
            Displays=displays.Select((d,index)=>new{Index=index+1,d.Active,d.Primary,d.Bounds,d.Work,RefreshRate=d.Hertz}).ToArray()
        },LocalData.Json);
    }
    public static void Write(string path,string report){
        string temp=Path.GetFullPath(path)+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{
            using(var file=File.Create(temp))using(var zip=new ZipArchive(file,ZipArchiveMode.Create)){
                using(var writer=new StreamWriter(zip.CreateEntry("diagnostics.json").Open()))writer.Write(report);
                using(var writer=new StreamWriter(zip.CreateEntry("README.txt").Open()))writer.Write("ScreenMover 诊断包\n包含：版本、系统信息、匿名屏幕几何和软件设置。\n不包含：截图、窗口标题、用户路径、显示器名称、隐藏窗口记录或原始日志。\n");
            }
            File.Move(temp,path,true);
        }finally{if(File.Exists(temp))File.Delete(temp);}
    }
}
