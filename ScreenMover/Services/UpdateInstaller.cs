using System.Diagnostics;
using System.IO.Compression;
using ScreenMover.UpdateCore;
namespace ScreenMover.Services;

public sealed record PreparedUpdate(string Session,UpdatePlan Plan);
public static class UpdateInstaller {
    public static string UpdatesFolder=>Path.Combine(LocalData.Folder,"updates");
    public static string CreateSession(){
        UpdateEngine.Root(UpdatesFolder);
        string path=Path.Combine(UpdatesFolder,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(path);return path;
    }
    public static async Task<PreparedUpdate> Prepare(string zip,string session,string target,string version,CancellationToken token)=>await Task.Run(()=>{
        target=UpdateEngine.Root(target);session=UpdateEngine.Root(session);
        if(!session.StartsWith(UpdateEngine.Root(UpdatesFolder)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException(L.T("无效的更新暂存目录"));
        string stage=Path.Combine(session,"payload");Directory.CreateDirectory(stage);
        using var archive=ZipFile.OpenRead(zip);
        if(archive.Entries.Count>4000)throw new InvalidDataException(L.T("更新包文件过多"));
        var entries=archive.Entries.Where(e=>!e.FullName.EndsWith('/')&&!e.FullName.EndsWith('\\')).ToList();
        var main=entries.Where(e=>e.FullName.Replace('\\','/').Split('/').Last()=="ScreenMover.exe").ToList();
        if(main.Count!=1)throw new InvalidDataException(L.T("更新包缺少唯一主程序"));
        string prefix=main[0].FullName[..^"ScreenMover.exe".Length];long expanded=0;
        var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var plan=new UpdatePlan(target,stage,version);
        foreach(var entry in entries){
            token.ThrowIfCancellationRequested();
            if(!entry.FullName.StartsWith(prefix,StringComparison.Ordinal)||entry.FullName.Length==prefix.Length)throw new InvalidDataException(L.T("更新包目录结构无效"));
            if(((entry.ExternalAttributes>>16)&0xF000)==0xA000)throw new InvalidDataException(L.T("更新包不得包含符号链接"));
            string name=UpdateEngine.Relative(entry.FullName[prefix.Length..]);
            if(!names.Add(name))throw new InvalidDataException(L.T("更新包包含重复路径"));
            expanded+=entry.Length;if(entry.Length<0||expanded>1536L*1024*1024)throw new InvalidDataException(L.T("更新包展开大小超限"));
            string path=UpdateEngine.Inside(stage,name);Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using(var input=entry.Open())using(var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
                byte[] buffer=new byte[65536];long count=0;int read;
                while((read=input.Read(buffer))>0){token.ThrowIfCancellationRequested();count+=read;if(count>entry.Length)throw new InvalidDataException(L.T("更新文件长度无效"));output.Write(buffer,0,read);}
                if(count!=entry.Length)throw new InvalidDataException(L.T("更新文件不完整"));
            }
            plan.Files.Add(new(name,UpdateEngine.Hash(path)));
        }
        foreach(string required in new[]{"ScreenMover.exe","ScreenMover.dll","ScreenMover.runtimeconfig.json","ScreenMover.pri","MainWindow.xbf","QuickWindow.xbf","ScreenMover.CaptureWorker.exe","Helpers\\ScreenMover.UpdateHelper.exe"})
            if(!names.Contains(required))throw new InvalidDataException(L.T("更新包缺少组件：")+required);
        foreach(string executable in new[]{"ScreenMover.exe","ScreenMover.CaptureWorker.exe","Helpers\\ScreenMover.UpdateHelper.exe"})ValidateMachine(Path.Combine(stage,executable));
        string actual=FileVersionInfo.GetVersionInfo(Path.Combine(stage,"ScreenMover.exe")).ProductVersion?.Split('+')[0]??"";
        if(ReleaseVersion.Parse(actual).CompareTo(ReleaseVersion.Parse(version))!=0)throw new InvalidDataException(L.T("主程序版本与发布标签不符"));
        UpdateEngine.Validate(plan);token.ThrowIfCancellationRequested();return new PreparedUpdate(session,plan);
    },token);
    static void ValidateMachine(string path){
        using var stream=File.OpenRead(path);using var reader=new BinaryReader(stream);
        if(stream.Length<64||reader.ReadUInt16()!=0x5A4D)throw new InvalidDataException(L.T("无效的更新程序文件"));
        stream.Position=0x3C;int offset=reader.ReadInt32();if(offset<64||offset>stream.Length-6)throw new InvalidDataException(L.T("无效的 PE 文件"));
        stream.Position=offset;
        ushort expected=GitHubUpdates.Architecture switch{"x64"=>0x8664,"arm64"=>0xAA64,"x86"=>0x14C,_=>throw new PlatformNotSupportedException()};
        if(reader.ReadUInt32()!=0x4550||reader.ReadUInt16()!=expected)throw new InvalidDataException(L.T("更新组件架构不匹配"));
    }
    public static void CheckWritable(){
        string path=Path.Combine(AppContext.BaseDirectory,".screenmover-update-"+Guid.NewGuid().ToString("N"));
        try{using var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,1,FileOptions.DeleteOnClose);}
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){throw new IOException(L.T("当前程序目录不可写，请将便携版移到可写目录后更新，或使用发布页手动安装"),ex);}
    }
    public static void Start(PreparedUpdate prepared){
        if(!GitHubUpdates.Portable)throw new InvalidOperationException(L.T("商店版本请通过 Microsoft Store 更新"));
        CheckWritable();
        string current=UpdateEngine.Root(AppContext.BaseDirectory);
        if(!current.Equals(prepared.Plan.Target,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException(L.T("安装目标已变化"));
        string source=Path.Combine(current,"Helpers","ScreenMover.UpdateHelper.exe");
        string helper=Path.Combine(prepared.Session,"ScreenMover.UpdateHelper.exe");File.Copy(source,helper,false);
        using var process=Process.GetCurrentProcess();prepared.Plan.ParentId=process.Id;prepared.Plan.ParentStarted=process.StartTime.ToUniversalTime().Ticks;
        string planPath=Path.Combine(prepared.Session,"plan.xml");prepared.Plan.Save(planPath);
        var info=new ProcessStartInfo(helper){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=prepared.Session};info.ArgumentList.Add(planPath);
        info.Environment["SCREENMOVER_UPDATE_TITLE"]=L.T("ScreenMover 更新");
        info.Environment["SCREENMOVER_UPDATE_FAILED_TITLE"]=L.T("ScreenMover 更新失败");
        info.Environment["SCREENMOVER_UPDATE_RESTART_MESSAGE"]=L.T("更新完成，但未能重新启动。请手动打开 ScreenMover。\n");
        info.Environment["SCREENMOVER_UPDATE_FAILED_MESSAGE"]=L.T("更新未完成。请查看更新结果与备份：\n");
        using var child=Process.Start(info)??throw new IOException(L.T("无法启动独立更新程序"));
    }
}
