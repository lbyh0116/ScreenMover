using ScreenMover.Models;
using ScreenMover.Services;
static partial class Regression {
    static void ReleaseChecks(){
        Check(!PlatformSupport.ForBuild(19045).CanHide&&!PlatformSupport.ForBuild(22631).CanHide,"Unsupported desktop interface enabled");
        Check(PlatformSupport.ForBuild(26100).CanHide&&PlatformSupport.ForBuild(26200).CanHide&&!PlatformSupport.ForBuild(27000).CanHide,"Desktop build allowlist changed");
        var display=new DisplayInfo("one",1,"one","One","",true,true,default,default,60,0);
        WindowInfo[] rows=[new(1,1,"Test","Window",1,default,"one")];
        Check(DropIntent.For(display,rows,true,false).Action==DropAction.Unavailable,"Win10 same-screen drop offered unsupported hide");
        Check(DropIntent.For(display with{Gdi="two"},rows,true,false).Allowed,"Win10 cross-monitor move was disabled");
        Check(WindowTitle.Middle("😀AB",0)==""&&WindowTitle.Middle("😀AB",1)=="…"&&WindowTitle.Middle("😀AB",2)=="😀…","Tiny Unicode title width failed");
        TransactionChecks();ProtocolChecks();CacheChecks();JournalChecks();
        Console.WriteLine("PASS release hardening: Win10/build capability guards, autosave rollback, bounded IPC/frame protocol, preview reference eviction and bounded log tail");
    }
    static void TransactionChecks(){
        var registrations=new Dictionary<int,string>();
        var occupied=new HashSet<string>();
        bool Register(int id,HotkeySpec spec)=>!occupied.Contains(spec.Name)&&!registrations.Values.Contains(spec.Name)&&registrations.TryAdd(id,spec.Name);
        using var quick=new HotkeyBinding(Register,id=>registrations.Remove(id));
        using var hide=new HotkeyBinding(Register,id=>registrations.Remove(id),0x5311);
        var before=new AppSettings{QuickHotkey="Ctrl+Alt+Space",HideHotkey="Ctrl+Alt+H"};
        using(var q=quick.Prepare(before.QuickHotkey))q.Commit();using(var h=hide.Prepare(before.HideHotkey))h.Commit();
        var next=before with{QuickHotkey="Ctrl+Alt+M",HideHotkey="Ctrl+Alt+N",Theme="Dark"};
        var disk=before;bool startup=false;
        bool failed=false;
        try{SettingsCommit.Write(before,next,quick,hide,true,false,true,v=>disk=v,_=>throw new UnauthorizedAccessException());}catch(UnauthorizedAccessException){failed=true;}
        Check(failed&&disk==before&&!startup&&quick.Current==before.QuickHotkey&&hide.Current==before.HideHotkey&&registrations.Count==2,"Autosave startup failure lost committed preferences/bindings");
        occupied.Add(next.HideHotkey);failed=false;
        try{SettingsCommit.Write(before,next,quick,hide,true,false,true,v=>disk=v,v=>startup=v);}catch(InvalidOperationException){failed=true;}
        Check(failed&&disk==before&&!startup&&registrations.Count==2,"Second shortcut conflict partially saved preferences");occupied.Clear();
        failed=false;try{SettingsCommit.Write(before,next,quick,hide,true,false,true,_=>throw new IOException(),v=>startup=v);}catch(IOException){failed=true;}
        Check(failed&&!startup&&quick.Current==before.QuickHotkey&&registrations.Count==2,"Disk failure changed startup/bindings");
        SettingsCommit.Write(before,next,quick,hide,true,false,true,v=>disk=v,v=>startup=v);
        Check(disk==next&&startup&&quick.Current==next.QuickHotkey&&hide.Current==next.HideHotkey,"Valid autosave failed");
        SettingsCommit.Write(next,next,quick,hide,false,true,true,v=>disk=v,v=>startup=v);
        Check(hide.Current==""&&disk.HideHotkey==next.HideHotkey&&quick.Current==next.QuickHotkey,"Win10 guard registered a hide shortcut or lost the stored preference");
    }
    static void ProtocolChecks(){
        static MemoryStream Frame(int width,int height,int count){var m=new MemoryStream();using(var w=new BinaryWriter(m,System.Text.Encoding.UTF8,true)){w.Write(width);w.Write(height);w.Write(new byte[count]);}m.Position=0;return m;}
        using(var valid=Frame(2,1,8))Check(SnapshotService.ReadFrame(valid,default).GetAwaiter().GetResult()?.Pixels.Length==8,"Valid capture frame rejected");
        using(var extra=Frame(2,1,9))Check(SnapshotService.ReadFrame(extra,default).GetAwaiter().GetResult()==null,"Capture protocol accepted trailing bytes");
        using(var oversized=Frame(int.MaxValue,360,0))Check(SnapshotService.ReadFrame(oversized,default).GetAwaiter().GetResult()==null,"Capture protocol accepted an unbounded bitmap");
        bool truncated=false;try{using var frame=Frame(360,360,2);SnapshotService.ReadFrame(frame,default).GetAwaiter().GetResult();}catch(EndOfStreamException){truncated=true;}Check(truncated,"Truncated frame accepted");
        using(var reader=new StreamReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes("show\n"))))Check(SingleInstance.ReadShortLine(reader,default).GetAwaiter().GetResult()=="show","IPC command changed");
        bool longMessage=false;try{using var reader=new StreamReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(new string('x',1024))));SingleInstance.ReadShortLine(reader,default).GetAwaiter().GetResult();}catch(InvalidDataException){longMessage=true;}Check(longMessage,"IPC accepted an unbounded line");
    }
    static void CacheChecks(){
        var cache=new Dictionary<string,object>();var windows=new List<WindowInfo>();
        for(int i=0;i<1000;i++){var w=new WindowInfo(i+1,i+1,"Test","Window",1,default,i%2==0?"one":"two");var frame=new byte[16384];w.SetPreview(frame,"");cache[w.OrderKey]=frame;windows.Add(w);}
        var weak=GetWeak(cache,windows[1].OrderKey);
        var retained=PreviewPolicy.Select(windows,"one");PreviewPolicy.Trim(cache,windows,retained);
        Check(cache.Count==64&&windows.Count(w=>w.Preview!=null)==64&&retained.All(w=>w.Gdi=="one"),"Preview cache/model references exceeded memory budget");
        GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
        Check(!weak.IsAlive,"Evicted preview remained strongly referenced");
        var reused=new WindowInfo(windows[0].Handle,windows[0].Pid,"Test","Window",1,default,"one"){ProcessStartedTicks=1};
        PreviewPolicy.Trim(cache,windows.Append(reused),new[]{reused});
        Check(cache.Count==0&&windows.All(w=>w.Preview==null)&&reused.Preview==null,"Reused PID/HWND retained an old process preview");
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static WeakReference GetWeak(IDictionary<string,object> cache,string key)=>new(cache[key]);
    static void JournalChecks(){
        string folder=Path.Combine(Path.GetTempPath(),"ScreenMover-log-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try{
            string path=Path.Combine(folder,"operations.log");File.WriteAllLines(path,Enumerable.Range(0,600).Select(i=>i.ToString()));
            var tail=OperationLog.ReadTail(path);Check(tail.Count==500&&tail[0]=="100"&&tail[^1]=="599","History tail lost its bound or order");
            File.WriteAllBytes(path,new byte[4*1024*1024]);OperationLog.Append(path,"new");
            Check(File.ReadAllText(path).Trim()=="new"&&Directory.GetFiles(folder,"operations-*.log").Length==1,"Log rotation discarded history or failed to bound the active file");
        }finally{foreach(var file in Directory.GetFiles(folder))File.Delete(file);Directory.Delete(folder);}
    }
}
