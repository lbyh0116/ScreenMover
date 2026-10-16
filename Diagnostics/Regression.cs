using ScreenMover.Models;
using ScreenMover.Services;
using static ScreenMover.Interop.Native;
static class Regression {
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void Pure(){
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
        string folder=Path.Combine(Path.GetTempPath(),"ScreenMover-tests-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path=Path.Combine(folder,"settings.json");
        try{
            var config=new AppSettings{Layout="Gallery",Theme="Dark",RefreshSeconds=0,MoveMargin=125,WindowPreviews=false};
            LocalData.Write(path,config);
            var copy=LocalData.Read(path,()=>new AppSettings(),out var warning);
            Check(warning==null&&copy.Layout=="Gallery"&&copy.RefreshSeconds==0&&copy.MoveMargin==125&&!copy.WindowPreviews,"Settings roundtrip");
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
        Console.WriteLine("PASS search, settings persistence, corrupt-file backup, validation, reorder persistence, group order, middle-ellipsis titles");
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
