using ScreenMover.Models;
using ScreenMover.Services;
using static ScreenMover.Interop.Native;
static partial class Regression {
    static void Surfaces(){
        RECT Box(int x,int y,int w,int h)=>new(){Left=x,Top=y,Right=x+w,Bottom=y+h};
        var screen=Box(-1920,-1080,1920,1080);
        var scenarios=new[]{
            (Box(-1920,-1080,1920,1032),Box(-100,-48,32,32),SurfaceLayout.Edge.Bottom),
            (Box(-1920,-1032,1920,1032),Box(-100,-1080,32,32),SurfaceLayout.Edge.Top),
            (Box(-1872,-1080,1872,1080),Box(-1920,-100,32,32),SurfaceLayout.Edge.Left),
            (Box(-1920,-1080,1872,1080),Box(-48,-100,32,32),SurfaceLayout.Edge.Right),
            (screen,Box(-100,-2,32,2),SurfaceLayout.Edge.Bottom),
            (screen,Box(-1920,-100,2,32),SurfaceLayout.Edge.Left)
        };
        foreach(var (work,anchor,edge) in scenarios)foreach(double scale in new[]{1,1.25,1.5,2})foreach(int count in new[]{0,1,30}){
            var p=SurfaceLayout.Flyout(screen,work,anchor,scale,count,3);
            Check(p.Edge==edge,"Flyout taskbar edge");
            Check(p.Bounds.Left>=work.Left&&p.Bounds.Top>=work.Top&&p.Bounds.Right<=work.Right&&p.Bounds.Bottom<=work.Bottom,"Flyout escaped negative-coordinate work area");
            Check(p.Bounds.Width>0&&p.Bounds.Height>0,"Flyout empty size");
        }
        var small=SurfaceLayout.Flyout(Box(0,0,800,600),Box(0,0,800,552),Box(760,552,32,32),2,50,8).Bounds;
        Check(small.Right<=800&&small.Bottom<=552,"High-DPI small screen clamp");
        foreach(double width in new[]{120,300,620,1000,1600}){
            var grid=SurfaceLayout.GalleryGrid(width);
            Check(grid.Width>0&&grid.Columns>=1&&grid.Width*grid.Columns<=Math.Max(1,width-20),"Gallery grid exceeded available width");
        }
        foreach(int rows in new[]{0,1,3,6,30,1000})foreach(int screens in new[]{1,2,4,10}){
            var size=SurfaceLayout.FlyoutSize(rows,screens);
            Check(size.Width<=480&&size.Height<=360&&Math.Abs(size.Width/size.Height-4.0/3)<.04,"Flyout lost compact 4:3 proportions");
        }
        Check(SurfaceLayout.FlyoutSize(3,1).Width<400,"Short application names still receive an oversized flyout");
        Check(SurfaceLayout.FlyoutSize(6,1).Height>SurfaceLayout.FlyoutSize(1,1).Height,"Window count ignored in flyout sizing");
        Check(SurfaceLayout.FlyoutSize(1,2).Height>SurfaceLayout.FlyoutSize(1,1).Height,"Display count ignored in flyout sizing");
        Check(ApplicationNames.Display("WINWORD.exe")=="Word"&&ApplicationNames.Display("Weixin")=="微信"&&ApplicationNames.Display("Hermes")=="Hermes","Friendly application names");
        var namedWindow=new WindowInfo(1,1,"WINWORD","Private document title",1,default,"one");
        Check(namedWindow.AppName=="Word"&&!namedWindow.AppName.Contains("Private"),"Quick list label used the document title");
        long style=0x16CF0000,extended=0x00020388;
        Check((ScreenMover.Interop.FlyoutChrome.ClientStyle(style)&(0x00C00000L|0x40000))==0,"Flyout native frame flags survived");
        Check((ScreenMover.Interop.FlyoutChrome.ClientStyle(style)&0x10000000)!=0,"Flyout lost visibility style");
        Check((ScreenMover.Interop.FlyoutChrome.ClientExtendedStyle(extended)&0x20300)==0&&(ScreenMover.Interop.FlyoutChrome.ClientExtendedStyle(extended)&0x88)==0x88,"Flyout changed topmost/tool-window state");
        Check(SurfaceLayout.GalleryGrid(300).Columns==1&&SurfaceLayout.GalleryGrid(1000).Columns==3,"Gallery responsive columns");
        foreach(double ratio in new[]{9.0/16,16.0/9,21.0/9,32.0/9}){
            var preview=SurfaceLayout.ScreenPreview(ratio,120);
            Check(preview.Height==120&&Math.Abs(preview.Width/preview.Height-ratio)<.001,"Screen preview forced a fixed aspect ratio");
            var compact=SurfaceLayout.ScreenPreview(ratio,64,130);
            Check(compact.Width<=130.001&&Math.Abs(compact.Width/compact.Height-ratio)<.001,"Quick preview distorted a wide monitor");
        }
        Check(SurfaceLayout.ScreenPreview(9.0/16,120).Width<SurfaceLayout.ScreenPreview(16.0/9,120).Width,"Portrait monitor card not narrower");
        Check(SurfaceLayout.FlyoutSize(1,1,32.0/9).Width>SurfaceLayout.FlyoutSize(1,1).Width,"Ultrawide target receives no extra popup width");
        var gate=new TrayClickGate();
        Check(!gate.Consume(1),"First tray click suppressed");gate.DismissFromTray(10);
        Check(gate.Consume(50)&&!gate.Consume(60),"Tray down/up dismissal reopened the panel or swallowed the next click");
        gate.DismissFromTray(100);Check(!gate.Consume(2000),"Cancelled tray click blocked a later open");
        gate.DismissFromTray(2100);gate.Reset();Check(!gate.Consume(2150),"Keyboard activation retained a stale mouse dismissal");
        var config=new AppSettings{Width=1000,Height=800,SizeInDips=true};
        Check(SurfaceLayout.InitialSize(config,1.5,Box(0,0,2560,1400))==(1500,1200),"DIP size restore");
        config.SizeInDips=false;
        Check(SurfaceLayout.InitialSize(config,1.25,Box(0,0,2560,1400))==(1000,800),"Legacy physical size migration");
        Check(SurfaceLayout.InitialSize(config,2,Box(0,0,1280,720))==(1280,720),"Minimum size exceeded work area");
        Console.WriteLine("PASS surface layout: 72 taskbar/negative-coordinate/DPI flyouts, auto-hide, small-screen clamping, gallery column sizing and size migration");
    }
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void Pure(){
        Surfaces();
        DropTargets();
        Instances();
        Preferences();
        ReleaseChecks();
        RECT Box(int x,int y,int w,int h)=>new(){Left=x,Top=y,Right=x+w,Bottom=y+h};
        // Mixed resolutions and relative monitor positions, all in physical pixels.
        var works=new[]{Box(-1920,0,1920,1040),Box(0,-2160,3840,2100),Box(2560,0,1080,1880),Box(-1080,-1920,1080,1880),Box(48,40,1872,1040)};
        foreach(var work in works)foreach(var size in new[]{(640,420),(5000,3000),(900,2400)})foreach(int margin in new[]{0,60,300}){
            var r=WindowService.Fit(Box(0,0,size.Item1,size.Item2),work,margin);
            Check(r.Left>=work.Left&&r.Top>=work.Top&&r.Right<=work.Right&&r.Bottom<=work.Bottom,"Fit outside work area");
            Check(r.Width<=size.Item1&&r.Height<=size.Item2,"Fit enlarged window");
            Check(Math.Abs((double)r.Width/r.Height-(double)size.Item1/size.Item2)<.02,"Fit changed aspect ratio");
        }
        var display=new DisplayInfo("test",2,"test","Portrait","",true,false,Box(-1080,-1920,1080,1920),Box(-1032,-1880,1032,1880),60,0);
        var screen=Box(-972,-1820,640,420);
        var workspace=WindowService.ToWorkspace(screen,display);
        Check(workspace.Left==-1020&&workspace.Top==-1860,"Taskbar workspace conversion");
        Check(WindowService.ToScreen(workspace,display).Equals(screen),"Workspace roundtrip");
        Check(WindowService.ClampToWork(Box(9000,9000,640,420),Box(0,0,1920,1040)).Equals(Box(1280,620,640,420)),"Disconnected monitor fallback");
        var unscaled=WindowService.Fit(Box(0,0,3000,2000),Box(0,0,1920,1080),0,false);
        Check(unscaled.Width==3000&&unscaled.Height==2000,"Preserve size setting");
        Console.WriteLine("PASS geometry: 45 negative/above/portrait/mixed-resolution work-area cases, taskbar offsets, disconnected-screen clamping");

        var windows=new[]{new WindowInfo(1,101,"Editor","项目 A",1,default,"one"),new WindowInfo(2,102,"Browser","项目 B",2,default,"two")};
        Check(WindowQuery.Filter(windows,"one","EDITOR 项目",false).Count==1,"Case-insensitive AND search");
        Check(WindowQuery.Filter(windows,"one","Browser",false).Count==0,"Current-screen search");
        Check(WindowQuery.Filter(windows,"one","102",true).Count==1,"All-screen PID search");
        Check(WindowQuery.Filter(windows,"one","missing",true).Count==0,"Empty search result");
        Check(WindowQuery.Filter(windows,"clean-virtual-display","",false).Count==0,"Clean virtual display leaked main-screen windows");
        Check(WindowQuery.Foreground(windows,2,1,102)?.Handle==2,"Foreground hotkey did not prefer the active window");
        Check(WindowQuery.Foreground(windows,99,1,101)?.Handle==1,"Owned modal window did not resolve to its software window");
        Check(WindowQuery.Foreground(windows,2,1,999)==null,"Foreground target ignored captured process identity");
        var fill=windows[0] with{};fill.SetPreview(new object(),"capture");
        Check(double.IsPositiveInfinity(fill.PreviewLimit),"Gallery fill constrained a wide thumbnail");
        var revision=new WindowListRevision();
        Check(revision.Update("main-screen-windows"),"Initial list binding skipped");
        revision.Invalidate();
        Check(revision.Update(""),"Selecting an empty display retained the previous list");
        Check(!revision.Update(""),"Unchanged empty list rebound");
        revision.Invalidate();
        Check(revision.Update(""),"Invalidated empty list was treated as unchanged");
        Check(revision.Update("main-screen-windows"),"Returning from an empty display did not rebind");
        Check(AccentColors.Normalize(" #12abEF ")=="#12ABEF"&&AccentColors.Normalize("#abc")==""&&AccentColors.Normalize("#12FFZZ")=="","Accent validation");
        foreach(var rgb in new[]{((byte)255,(byte)255,(byte)255),((byte)255,(byte)255,(byte)0),((byte)255,(byte)0,(byte)0),((byte)0,(byte)0,(byte)0)}){
            var drop=AccentColors.Overlay(rgb.Item1,rgb.Item2,rgb.Item3);
            double Linear(byte v){double x=v/255.0;return x<=.04045?x/12.92:Math.Pow((x+.055)/1.055,2.4);}
            double luminance=.2126*Linear(drop.R)+.7152*Linear(drop.G)+.0722*Linear(drop.B);
            Check(1.05/(luminance+.05)>=4.5,"Pale custom accent makes white drop text unreadable");
        }
        string folder=Path.Combine(Path.GetTempPath(),"ScreenMover-tests-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path=Path.Combine(folder,"settings.json");
        try{
            var config=new AppSettings{Theme="Dark",AccentColor="#12ABEF",RefreshSeconds=0,MoveMargin=125,WindowPreviews=false};
            LocalData.Write(path,config);
            var copy=LocalData.Read(path,()=>new AppSettings(),out var warning);
            Check(warning==null&&copy.AccentColor=="#12ABEF"&&copy.RefreshSeconds==0&&copy.MoveMargin==125&&!copy.WindowPreviews,"Settings roundtrip");
            foreach(string legacy in new[]{"Auto","List","Gallery"}){
                File.WriteAllText(path,"{\"Layout\":\""+legacy+"\",\"Theme\":\"Dark\"}");
                var migrated=LocalData.Read(path,()=>new AppSettings(),out warning);
                Check(warning==null&&migrated.Theme=="Dark","Legacy layout config failed to load");
            }
            File.WriteAllText(path,"{broken");
            var recovered=LocalData.Read(path,()=>new AppSettings(),out warning);
            Check(warning!=null&&recovered.RefreshSeconds==60&&Directory.GetFiles(folder,"*.backup").Length==1,"Corrupt settings recovery");
            config.Theme="wrong";config.RefreshSeconds=1;config.NoticeSeconds=999;config.Normalize();
            Check(config.Theme=="System"&&config.RefreshSeconds==15&&config.NoticeSeconds==30,"Settings bounds");
        }finally{foreach(var file in Directory.GetFiles(folder))File.Delete(file);Directory.Delete(folder);}
                string orderPath=Path.Combine(Path.GetTempPath(),"ScreenMover-order-"+Guid.NewGuid().ToString("N")+".json");
        try{
            var order=new WindowOrder(orderPath);
            var third=new WindowInfo(3,103,"Notes","长标题-末尾",1,default,"one");
            var rows=windows.Append(third).ToList();
            Check(order.Reorder(rows,[third],windows[0],false),"Reorder rejected");
            var refreshed=new WindowOrder(orderPath).Sort(rows.Select(w=>w with{Title=w.Title+" changed"}));
            Check(refreshed.Select(w=>w.Handle).SequenceEqual(new nint[]{3,1,2}),"Refresh/restart lost order");
            Check(order.Reorder(rows,[third,windows[0]],windows[1],true),"Group reorder rejected");
            Check(order.Sort(rows).Select(w=>w.Handle).SequenceEqual(new nint[]{2,3,1}),"Group order changed");
            Check(!order.Reorder(rows,[third],third,true),"Self drop changed order");
            Check(WindowTitle.Middle("12345678901234567890",9)=="12345…890","Middle ellipsis must retain both ends");
            Check(WindowTitle.Middle("短标题",9)=="短标题","Short title changed");
            var unicode=WindowTitle.Middle("😀😀😀😀😀😀😀😀😀😀😀😀",7);
            Check(!unicode.Contains('\uFFFD')&&unicode.StartsWith("😀")&&unicode.EndsWith("😀"),"Unicode title split");
        }finally{if(File.Exists(orderPath))File.Delete(orderPath);}
        Console.WriteLine("PASS empty-display filtering and list invalidation, accent contrast/persistence, search, settings persistence, corrupt-file backup, validation, reorder persistence, group order, middle-ellipsis titles");
    }
    static void Preferences(){
        Check(HotkeySpec.Parse("alt+control+space")?.Name=="Ctrl+Alt+Space","Hotkey canonicalization");
        Check(HotkeySpec.Parse("")==null,"Disabled hotkey");
        foreach(string bad in new[]{"A","Shift+A","Ctrl+F12","Win+L","Ctrl+Ctrl+A","Ctrl++A","Ctrl+Alt+Delete"}){
            bool rejected=false;try{HotkeySpec.Parse(bad);}catch(FormatException){rejected=true;}Check(rejected,"Unsafe or malformed hotkey accepted: "+bad);
        }
        var registered=new Dictionary<int,string>();var occupied=new HashSet<string>();
        using(var binding=new HotkeyBinding((id,spec)=>!occupied.Contains(spec.Name)&&registered.TryAdd(id,spec.Name),id=>registered.Remove(id))){
            using(var first=binding.Prepare("Ctrl+Alt+Space"))first.Commit();
            string previous=binding.Current;
            using(var cancelled=binding.Prepare("Ctrl+Alt+K"))Check(registered.Count==2,"Previous hotkey was removed before commit");
            Check(binding.Current==previous&&registered.Count==1,"Cancelled edit changed active hotkey");
            occupied.Add("Ctrl+Alt+J");bool rejected=false;
            try{using var conflict=binding.Prepare("Ctrl+Alt+J");conflict.Commit();}catch(InvalidOperationException){rejected=true;}
            Check(rejected&&binding.Current==previous&&registered.Count==1,"Conflict lost old hotkey");
            using(var saved=binding.Prepare("Ctrl+Alt+K"))saved.Commit();
            Check(binding.Current=="Ctrl+Alt+K"&&registered.Count==1,"Hotkey commit leaked registration");
            using(var disabled=binding.Prepare(""))disabled.Commit();Check(registered.Count==0,"Hotkey disable did not unregister");
            using var pending=binding.Prepare("Ctrl+Alt+M");binding.Dispose();binding.Dispose();Check(registered.Count==0,"Dispose leaked pending registration");
        }
        bool Register(int id,HotkeySpec spec)=>!occupied.Contains(spec.Name)&&!registered.Values.Contains(spec.Name)&&registered.TryAdd(id,spec.Name);
        using(var quick=new HotkeyBinding(Register,id=>registered.Remove(id)))
        using(var hide=new HotkeyBinding(Register,id=>registered.Remove(id),0x5311)){
            using(var q=quick.Prepare("Ctrl+Alt+Space"))q.Commit();
            using(var h=hide.Prepare("Ctrl+Alt+H"))h.Commit();
            Check(quick.CurrentId!=hide.CurrentId&&registered.Count==2,"Two shortcuts reused a native registration ID");
            bool rejected=false;
            try{using var q=quick.Prepare("Ctrl+Alt+M");using var h=hide.Prepare("Ctrl+Alt+J");q.Commit();h.Commit();}catch(InvalidOperationException){rejected=true;}
            Check(rejected&&quick.Current=="Ctrl+Alt+Space"&&hide.Current=="Ctrl+Alt+H"&&registered.Count==2,"Failure of the second shortcut did not roll back both edits");
        }
        Check(registered.Count==0,"Multiple shortcut disposal leaked registrations");
        var original=new AppSettings{Theme="Dark",AccentColor="#12ABEF",QuickHotkey="Ctrl+Alt+K",HideHotkey="Ctrl+Alt+H",MoveMargin=125,Width=2500,Height=1500};
        string profile=SettingsProfile.Export(original);
        var restored=SettingsProfile.Import(profile);
        Check(restored.HideHotkey==original.HideHotkey&&restored.AccentColor==original.AccentColor&&restored.Theme=="Dark"&&restored.QuickHotkey==original.QuickHotkey&&restored.MoveMargin==125,"Preferences roundtrip");
        Check(!profile.Contains("\"Width\"")&&!profile.Contains("\"Height\"")&&!profile.Contains("SizeInDips"),"Device geometry leaked into portable preferences");
        Check(SettingsProfile.Import("{\"Theme\":\"Light\",\"Layout\":\"List\"}").Theme=="Light","Legacy preferences import");
        foreach(string bad in new[]{"{broken","[]","{}","{\"Format\":\"Other\",\"Version\":1}","{\"Format\":\"ScreenMover.Settings\",\"Version\":99,\"Settings\":{\"Theme\":\"Dark\"}}","{\"QuickHotkey\":\"F12\"}","{\"HideHotkey\":\"F12\"}","{\"QuickHotkey\":\"Ctrl+Alt+H\",\"HideHotkey\":\"Ctrl+Alt+H\"}",new string('x',256*1024+1)}){
            bool rejected=false;try{SettingsProfile.Import(bad);}catch(Exception ex)when(ex is System.Text.Json.JsonException or InvalidDataException or FormatException){rejected=true;}Check(rejected,"Invalid settings profile accepted");
        }
        var display=new DisplayInfo("private-key",1,@"C:\Users\Private\device","PRIVATE MONITOR","PRIVATE SERIAL",true,true,new(){Right=1920,Bottom=1080},new(){Right=1920,Bottom=1040},60,0);
        string report=DiagnosticReport.Build(original,new[]{display});
        Check(!report.Contains("PRIVATE",StringComparison.OrdinalIgnoreCase)&&!report.Contains("C:\\",StringComparison.OrdinalIgnoreCase)&&!report.Contains("private-key"),"Diagnostic report exposed private display metadata");
        string folder=Path.Combine(Path.GetTempPath(),"ScreenMover-profile-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try{
            string settings=Path.Combine(folder,"settings.json"),zipPath=Path.Combine(folder,"diagnostics.zip");
            SettingsProfile.WriteText(settings,profile);Check(SettingsProfile.Read(settings).GetAwaiter().GetResult().MoveMargin==125,"Profile file roundtrip");
            DiagnosticReport.Write(zipPath,report);
            using var zip=System.IO.Compression.ZipFile.OpenRead(zipPath);
            Check(zip.Entries.Count==2&&zip.GetEntry("diagnostics.json")!=null&&zip.GetEntry("README.txt")!=null,"Diagnostic package included unexpected files");
            using var reader=new StreamReader(zip.GetEntry("diagnostics.json")!.Open());Check(reader.ReadToEnd()==report,"Diagnostic package corrupted report");
        }finally{foreach(var file in Directory.GetFiles(folder))File.Delete(file);Directory.Delete(folder);}
        Console.WriteLine("PASS preferences: hotkey parsing/conflict/prepare/commit/cancel/dispose; profile validation and roundtrip; diagnostic metadata allowlist and ZIP contents");
    }
    static void DropTargets(){
        var one=new DisplayInfo("one",1,"one","One","",true,true,default,default,60,0);
        var two=one with{Key="two",Gdi="two",Number=2};
        WindowInfo[] windows=[new(1,1,"Test","Window",1,default,"one")];
        Check(DropIntent.For(one,windows,false).Action==DropAction.Current&&!DropIntent.For(one,windows,false).Allowed,"Main same-screen drop should be a no-op");
        Check(DropIntent.For(one,windows,true).Action==DropAction.Hide&&DropIntent.For(one,windows,true).Allowed,"Quick same-screen hide intent");
        Check(DropIntent.For(two,windows,false).Action==DropAction.Move&&DropIntent.For(two,windows,true).Action==DropAction.Move,"Cross-screen drop intent");
        Check(DropIntent.For(two with{Active=false},windows,true).Action==DropAction.Unavailable,"Inactive screen accepted");
        Check(DropIntent.For(null,windows,false).Action==DropAction.None,"Empty drop target accepted");
        Check(DropIntent.For(one,windows.Append(windows[0] with{Gdi="two"}).ToArray(),false).Action==DropAction.Move,"Mixed-screen batch intent");
        var small=SurfaceLayout.Flyout(new(){Right=1920,Bottom=1080},new(){Right=1920,Bottom=1040},new(){Left=1800,Top=1040,Right=1832,Bottom=1072},1,3,1).Bounds;
        Check(small.Width<400&&small.Height<280,"Small flyout left unnecessary empty space");
        Check(StartupService.Command(@"C:\Program Files\ScreenMover\ScreenMover.exe")=="\"C:\\Program Files\\ScreenMover\\ScreenMover.exe\" --start-in-tray","Startup path was not quoted");
        Console.WriteLine("PASS drop intents: move/current/hide/unavailable/mixed-screen batch; compact flyout and startup command quoting");
    }
    static void Instances(){
        string name="ScreenMover-test-"+Guid.NewGuid().ToString("N");
        int signals=0;
        using(var owner=SingleInstance.Claim(name)){
            Check(owner!=null,"First instance did not acquire ownership");
            owner!.Listen(()=>Interlocked.Increment(ref signals));
            var duplicate=Task.Run(()=>SingleInstance.Claim(name)).GetAwaiter().GetResult();
            Check(duplicate==null,"Second instance acquired a held mutex");
            Check(Task.Run(()=>SingleInstance.Activate(name)).GetAwaiter().GetResult(),"Second instance failed to signal owner");
            Check(signals==1,"Activation was not delivered exactly once");
            owner.Dispose();owner.Dispose();
        }
        using var reopened=SingleInstance.Claim(name);
        Check(reopened!=null,"Ownership was not released on exit");
        Console.WriteLine("PASS single instance: ownership exclusion, current-user IPC activation, release and relaunch (no UI)");
    }
    public static void Fixture(WindowInfo window,string helper){
        var displays=new DisplayService().Read().Displays.Where(d=>d.Active).DistinctBy(d=>d.Gdi).ToList();
        var history=new MoveHistory();var service=new WindowService();
        foreach(var target in displays)foreach(int state in new[]{9,2,3}){
            Check(service.ChangeState(window,state).Success,"Prepare fixture state");
            var before=WindowService.Placement(window.Handle);
            var results=history.Move([window],target,90,true);
            Check(results.Count==1&&results[0].Success,"Move fixture before undo");
            Check(history.Count==1,"Undo batch missing");
            var restored=history.Undo();
            Check(restored.Single().Success&&history.Count==0,"Undo failed");
            var after=WindowService.Placement(window.Handle);
            Console.WriteLine($"Undo state={state}, before={before.showCmd} {before.rcNormalPosition}, after={after.showCmd} {after.rcNormalPosition}");
Check(after.showCmd==before.showCmd&&after.rcNormalPosition.Equals(before.rcNormalPosition),"Undo did not restore exact raw placement");
        }
        Console.WriteLine($"PASS move/undo: {displays.Count} active display(s), normal/minimized/maximized exact placement");
        var bad=MoveHistory.Capture(window) with{ProcessStarted=1};
        Check(!MoveHistory.Restore(bad).Success,"Restarted process accepted by undo");

        VirtualDesktopService.HelperPath=helper;
        var probe=VirtualDesktopService.Run("probe");
        Check(probe.Success,"Virtual desktop probe failed");
        var path=Path.Combine(Path.GetTempPath(),"ScreenMover-hidden-test-"+Guid.NewGuid().ToString("N")+".json");
        HiddenWindow? entry=null;
        try{
            var hidden=new HiddenWindows(path);
            var result=hidden.Hide(window);Check(result.Success,"Hide integration: "+result.Message);
            // A new service instance models an application restart.
            hidden=new HiddenWindows(path);entry=hidden.Read().Single();
            Check(entry.DesktopId!=null&&MoveHistory.Alive(entry.Saved)&&IsIconic(window.Handle),"Durable hide record missing");
            var recovered=hidden.Recover(entry);Check(recovered.Success,"Recover integration: "+recovered.Message);
            Check(new HiddenWindows(path).Read().Count==0&&!IsIconic(window.Handle),"Recover left hidden record or minimized window");
            Check(WindowService.Placement(window.Handle).showCmd==3,"Recover lost maximized state");
            Console.WriteLine("PASS hidden window journal survives reload, exact identity guard, recovery to current desktop");
        }finally{
            if(entry!=null){
                if(IsIconic(window.Handle))new HiddenWindows(path).Recover(entry);
                var cleanup=VirtualDesktopService.Run("cleanup-empty",entry.DesktopId!);
                Check(cleanup.Success,"Test desktop cleanup failed: "+cleanup.Output);
            }
            if(File.Exists(path))File.Delete(path);
        }
        var final=VirtualDesktopService.Run("probe");
        Check(final.Output==probe.Output,"Test changed original desktop count/current desktop");
        Console.WriteLine("PASS virtual desktop integration cleanup; original desktop count and current desktop unchanged");
    }
}
