using System.Net;
using System.Security.Cryptography;
using System.Text;
using ScreenMover.Services;
using ScreenMover.UpdateCore;

static class UpdateRegression {
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Reject(Action action){try{action();}catch(Exception ex)when(ex is InvalidDataException or FormatException or IOException){return;}throw new Exception("Unsafe update input was accepted");}
    public static void Run(){
        string[] ordered=["0.2.0-preview.2","0.2.0-preview.10","0.2.0-rc.1","0.2.0","0.2.1","1.0.0"];
        for(int i=1;i<ordered.Length;i++)Check(ReleaseVersion.Parse(ordered[i]).CompareTo(ReleaseVersion.Parse(ordered[i-1]))>0,"Version ordering failed");
        Check(ReleaseVersion.Parse("v1.0.0+abc").CompareTo(ReleaseVersion.Parse("1.0.0+def"))==0,"Build metadata changed version precedence");
        Reject(()=>ReleaseVersion.Parse("1.2.3-preview.01"));Reject(()=>ReleaseVersion.Parse("bad"));
        string digest=new('a',64);
        string Json(string tag,bool pre,string arch,string hash)=>System.Text.Json.JsonSerializer.Serialize(new[]{new{draft=false,prerelease=pre,tag_name=tag,body="Notes",assets=new[]{new{name=$"ScreenMover-{tag}-win-{arch}.zip",browser_download_url=$"https://github.com/lbyh0116/ScreenMover/releases/download/{tag}/package.zip",size=4,digest=hash}}}});
        var release=GitHubUpdates.Select(Encoding.UTF8.GetBytes(Json("v1.0.0-preview.2",true,"x64","sha256:"+digest)),true,"x64");
        Check(release?.Sha256==digest&&release.Download!=null,"Release asset selection failed");
        Check(GitHubUpdates.Select(Encoding.UTF8.GetBytes(Json("v1.0.0-preview.2",true,"x64","sha256:"+digest)),false,"x64")==null,"Stable channel included preview");
        Check(GitHubUpdates.Select(Encoding.UTF8.GetBytes(Json("v1.0.0",false,"arm64","sha256:"+digest)),true,"x64")?.Download==null,"Wrong architecture selected");
        Check(GitHubUpdates.Select(Encoding.UTF8.GetBytes(Json("v1.0.0",false,"x64","bad")),true,"x64")?.Sha256==null,"Invalid digest accepted");
        Check(!GitHubUpdates.AssetUrl(new Uri("https://github.com.evil.test/lbyh0116/ScreenMover/releases/download/v1/a.zip"))&&!GitHubUpdates.AssetUrl(new Uri("http://github.com/lbyh0116/ScreenMover/releases/download/v1/a.zip")),"Unsafe download URL accepted");
        NetworkChecks().GetAwaiter().GetResult();TransactionChecks();
        Console.WriteLine("PASS updates: semantic versions/channels/architecture, HTTPS redirects, bounded downloads/SHA-256/cancellation, safe paths, tamper rejection, file rollback and update-plan roundtrip (temporary fixtures only)");
    }
    sealed class Handler(Func<HttpRequestMessage,HttpResponseMessage> send):HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>Task.FromResult(send(request));
    }
    static async Task NetworkChecks(){
        byte[] bytes=Encoding.UTF8.GetBytes("test");string hash=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var release=new GitHubRelease("v1.0.0","",new Uri("https://github.com/lbyh0116/ScreenMover/releases"),new Uri("https://github.com/lbyh0116/ScreenMover/releases/download/v1.0.0/package.zip"),4,hash);
        string folder=Path.Combine(Path.GetTempPath(),"ScreenMover-update-net-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try{
            using var client=new HttpClient(new Handler(_=>new(HttpStatusCode.OK){Content=new ByteArrayContent(bytes)}));var api=new GitHubUpdates(client);
            string good=Path.Combine(folder,"good.zip");await api.Download(release,good,null,CancellationToken.None);Check(File.ReadAllBytes(good).SequenceEqual(bytes),"Downloaded bytes changed");
            bool existedRejected=false;try{await api.Download(release,good,null,CancellationToken.None);}catch(IOException){existedRejected=true;}
            Check(existedRejected&&File.ReadAllBytes(good).SequenceEqual(bytes),"Existing download destination was overwritten/deleted");
            string unknown=Path.Combine(folder,"unknown.zip");await api.Download(release with{Size=0},unknown,null,CancellationToken.None);Check(File.ReadAllBytes(unknown).SequenceEqual(bytes),"Feed download without API size failed");
            using var fallback=new HttpClient(new Handler(request=>{
                string url=request.RequestUri!.AbsoluteUri;
                if(request.RequestUri.Host=="api.github.com")return new(HttpStatusCode.Forbidden);
                string content=url.EndsWith(".atom",StringComparison.Ordinal)?"<feed xmlns='http://www.w3.org/2005/Atom'><entry><link href='https://github.com/lbyh0116/ScreenMover/releases/tag/v1.0.0-preview.2'/></entry></feed>":hash+"  ScreenMover-v1.0.0-preview.2-win-"+GitHubUpdates.Architecture+".zip\n";
                return new(HttpStatusCode.OK){Content=new StringContent(content)};
            }));
            var feed=await new GitHubUpdates(fallback).Check(true,CancellationToken.None);Check(feed?.Tag=="v1.0.0-preview.2"&&feed.Sha256==hash&&feed.Size==0,"API rate-limit feed fallback failed");
            Check(await new GitHubUpdates(fallback).Check(false,CancellationToken.None)==null,"Stable channel guessed GitHub prerelease status from feed");
            using var stableFallback=new HttpClient(new Handler(request=>{
                if(request.RequestUri!.Host=="api.github.com")return new(HttpStatusCode.Forbidden);
                if(request.RequestUri.AbsolutePath.EndsWith("/latest",StringComparison.Ordinal)){
                    var result=new HttpResponseMessage(HttpStatusCode.Redirect);result.Headers.Location=new Uri("https://github.com/lbyh0116/ScreenMover/releases/tag/v1.0.0");return result;
                }
                return new(HttpStatusCode.OK){Content=new StringContent(hash+"  ScreenMover-v1.0.0-win-"+GitHubUpdates.Architecture+".zip\n")};
            }));
            var stable=await new GitHubUpdates(stableFallback).Check(false,CancellationToken.None);
            Check(stable?.Tag=="v1.0.0"&&stable.Sha256==hash,"Official latest stable release fallback failed");
            string bad=Path.Combine(folder,"bad.zip");bool rejected=false;
            try{await api.Download(release with{Sha256=new string('0',64)},bad,null,CancellationToken.None);}catch(InvalidDataException){rejected=true;}
            Check(rejected&&!File.Exists(bad),"Corrupt download retained");
            using var redirected=new HttpClient(new Handler(_=>{var result=new HttpResponseMessage(HttpStatusCode.Redirect);result.Headers.Location=new Uri("https://evil.test/package.zip");return result;}));
            rejected=false;try{await new GitHubUpdates(redirected).Download(release,bad,null,CancellationToken.None);}catch(InvalidDataException){rejected=true;}
            Check(rejected&&!File.Exists(bad),"Untrusted redirect followed");
            rejected=false;try{await api.Download(release with{Size=3},bad,null,CancellationToken.None);}catch(InvalidDataException){rejected=true;}
            Check(rejected&&!File.Exists(bad),"Oversized download accepted");
            using var canceled=new CancellationTokenSource();canceled.Cancel();rejected=false;
            try{await api.Download(release,bad,null,canceled.Token);}catch(OperationCanceledException){rejected=true;}
            Check(rejected&&!File.Exists(bad),"Canceled download retained");
        }finally{Directory.Delete(folder,true);}
    }
    static void TransactionChecks(){
        string root=Path.Combine(Path.GetTempPath(),"ScreenMover-update-txn-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try{
            string target=Path.Combine(root,"install"),stage=Path.Combine(root,"payload");Directory.CreateDirectory(target);Directory.CreateDirectory(stage);
            File.WriteAllText(Path.Combine(target,"ScreenMover.exe"),"old executable");File.WriteAllText(Path.Combine(stage,"ScreenMover.exe"),"new executable");
            File.WriteAllText(Path.Combine(target,"keep.txt"),"user file");File.WriteAllText(Path.Combine(stage,"new.txt"),"new file");
            var plan=new UpdatePlan(target,stage,"v1.0.0"){ParentId=1,ParentStarted=1};
            foreach(string name in new[]{"ScreenMover.exe","new.txt"})plan.Files.Add(new(name,UpdateEngine.Hash(Path.Combine(stage,name))));
            string path=Path.Combine(root,"plan.xml");plan.Save(path);var restored=UpdatePlan.Load(path);
            Check(restored.Files.Count==2&&restored.ParentStarted==1&&restored.Target==target,"Update plan changed in transit");
            Reject(()=>UpdateEngine.Relative("../escape"));Reject(()=>UpdateEngine.Relative("sub\\..\\escape"));Reject(()=>UpdateEngine.Relative("file:stream"));Reject(()=>UpdateEngine.Relative("CON.txt"));Reject(()=>UpdateEngine.Relative("file. "));
            var dup=new UpdatePlan(target,stage,"v1");dup.Files.AddRange(plan.Files);dup.Files.Add(plan.Files[0]);Reject(()=>UpdateEngine.Validate(dup));
            File.WriteAllText(Path.Combine(stage,"new.txt"),"tampered");Reject(()=>UpdateEngine.Apply(plan,Path.Combine(root,"bad-backup"),_=>{}));
            Check(File.ReadAllText(Path.Combine(target,"ScreenMover.exe"))=="old executable","Tampered package changed installation");
            File.WriteAllText(Path.Combine(stage,"new.txt"),"new file");
            bool failed=false;try{UpdateEngine.Apply(plan,Path.Combine(root,"rollback"),n=>{if(n==2)throw new IOException("Simulated disk failure");});}catch(IOException){failed=true;}
            Check(failed&&File.ReadAllText(Path.Combine(target,"ScreenMover.exe"))=="old executable"&&!File.Exists(Path.Combine(target,"new.txt")),"Rollback did not restore old/new files");
            UpdateEngine.Apply(plan,Path.Combine(root,"success"),_=>{});
            Check(File.ReadAllText(Path.Combine(target,"ScreenMover.exe"))=="new executable"&&File.ReadAllText(Path.Combine(target,"keep.txt"))=="user file"&&File.Exists(Path.Combine(target,"new.txt")),"Successful update lost unrelated data");
        }finally{Directory.Delete(root,true);}
    }
    public static void HelperSmoke(string helper){
        string root=Path.Combine(Path.GetTempPath(),"ScreenMover-update-worker-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try{
            string target=Path.Combine(root,"install"),session=Path.Combine(root,"session"),stage=Path.Combine(session,"payload");Directory.CreateDirectory(target);Directory.CreateDirectory(stage);
            string source=Path.Combine(root,"fixture.cs"),fixture=Path.Combine(target,"ScreenMover.exe");
            File.WriteAllText(source,"using System;using System.IO;using System.Threading;class Fixture{static void Main(string[] args){if(args.Length>0){Thread.Sleep(2500);return;}File.WriteAllText(\"restart.marker\",File.ReadAllText(\"state.txt\"));}}");
            var compile=new System.Diagnostics.ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"Microsoft.NET","Framework64","v4.0.30319","csc.exe")){UseShellExecute=false,CreateNoWindow=true};
            foreach(string argument in new[]{"/nologo","/target:winexe","/platform:x64","/out:"+fixture,source})compile.ArgumentList.Add(argument);
            using(var compiler=System.Diagnostics.Process.Start(compile)!){Check(compiler.WaitForExit(15000)&&compiler.ExitCode==0,"Cannot build no-UI update fixture");}
            File.Copy(fixture,Path.Combine(stage,"ScreenMover.exe"));File.WriteAllText(Path.Combine(target,"state.txt"),"old");File.WriteAllText(Path.Combine(stage,"state.txt"),"new");
            using var parent=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fixture,"wait"){UseShellExecute=false,CreateNoWindow=true})!;
            var plan=new UpdatePlan(target,stage,"v1.0.0"){ParentId=parent.Id,ParentStarted=parent.StartTime.ToUniversalTime().Ticks};
            foreach(string name in new[]{"ScreenMover.exe","state.txt"})plan.Files.Add(new(name,UpdateEngine.Hash(Path.Combine(stage,name))));
            string planPath=Path.Combine(session,"plan.xml");plan.Save(planPath);
            string worker=Path.Combine(session,"ScreenMover.UpdateHelper.exe");File.Copy(helper,worker);
            var start=new System.Diagnostics.ProcessStartInfo(worker){UseShellExecute=false,CreateNoWindow=true};start.ArgumentList.Add(planPath);
            using var updater=System.Diagnostics.Process.Start(start)!;
            Thread.Sleep(300);Check(File.ReadAllText(Path.Combine(target,"state.txt"))=="old","Updater replaced files before parent exit");
            if(!updater.WaitForExit(20000)){updater.Kill();throw new Exception("Updater timed out: "+(File.Exists(Path.Combine(session,"result.txt"))?File.ReadAllText(Path.Combine(session,"result.txt")):"no result"));}
            Check(updater.ExitCode==0,"Real updater failed");
            string marker=Path.Combine(target,"restart.marker");for(int i=0;i<100&&!File.Exists(marker);i++)Thread.Sleep(100);
            Check(File.Exists(marker)&&File.ReadAllText(marker)=="new"&&File.ReadAllText(Path.Combine(session,"backup","state.txt"))=="old","Real updater did not install/restart/retain backup");
            Console.WriteLine("PASS real update helper: waited for fixture parent, installed files, retained backup and restarted a temporary no-UI fixture; user installation untouched");
        }finally{try{Directory.Delete(root,true);}catch(IOException){}}
    }
    public static async Task PackageSmoke(string zip){
        string root=Path.Combine(Path.GetTempPath(),"ScreenMover-package-fixture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        string session=UpdateInstaller.CreateSession();
        try{
            File.WriteAllText(Path.Combine(root,"ScreenMover.exe"),"fixture only");
            var prepared=await UpdateInstaller.Prepare(zip,session,root,BuildInfo.DisplayVersion,CancellationToken.None);
            Check(prepared.Plan.Files.Count>10&&File.ReadAllText(Path.Combine(root,"ScreenMover.exe"))=="fixture only","Package staging changed target");
            Console.WriteLine($"PASS release ZIP staging: {prepared.Plan.Files.Count} files, version/PE architecture/required resources/checksums; no installation performed");
        }finally{Directory.Delete(root,true);UpdateEngine.Root(session);Directory.Delete(session,true);}
    }
}
