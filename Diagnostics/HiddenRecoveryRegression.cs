using ScreenMover.DesktopLogic;
using ScreenMover.Services;
static partial class Regression {
    static void HiddenRecoveryChecks(){
        int elapsed=0;
        HideSample Sample(bool minimized=true)=>new(){Exists=true,OnTarget=true,Minimized=minimized};
        Check(HideVerification.Observe(()=>Sample(),ms=>elapsed+=ms)==HideOutcome.Minimized&&elapsed==2500,"Hide observation skipped the delayed restore window");
        elapsed=0;
        Check(HideVerification.Observe(()=>Sample(elapsed<2000),ms=>elapsed+=ms)==HideOutcome.DesktopOnly,"Refused minimization must preserve an already hidden window");
        elapsed=0;
        Check(HideVerification.Observe(()=>new(){Exists=true,OnTarget=elapsed<2000,Minimized=true},ms=>elapsed+=ms)==HideOutcome.Returned,"Delayed return to another desktop was reported as hidden");
        Check(HideVerification.Observe(()=>new(){Exists=true,OnTarget=true,TargetIsCurrent=true},_=>{})==HideOutcome.Returned,"Visible target desktop was reported as hidden");
        Check(HideVerification.Observe(()=>new(),_=>{})==HideOutcome.Closed,"Destroyed window was reported as hidden");
        var desktop=Guid.NewGuid();
        var saved=new SavedWindow(1,2,3,"Example","Example","display",default,default,default);
        var entry=new HiddenWindow(Guid.NewGuid(),saved,"Example",desktop.ToString(),DateTime.Now,"已隐藏");
        Check(HiddenWindowState.Classify(entry,desktop,true,false)==HiddenObservation.Hidden,"Simply visiting the hidden desktop discarded recovery");
        Check(HiddenWindowState.Classify(entry,Guid.NewGuid(),true,true)==HiddenObservation.Returned,"A manually moved minimized window retained its hidden record");
        Check(HiddenWindowState.Classify(entry,Guid.Empty,true,false)==HiddenObservation.Unknown,"An unknown desktop discarded recovery");
        var pending=entry with{Id=Guid.NewGuid(),DesktopId=null,Status="正在隐藏 / 若操作中断可尝试找回"};
        Check(HiddenWindowState.Classify(pending,desktop,true,false)==HiddenObservation.Unknown,"An unconfirmed operation lost its recovery intent");
        Check(HiddenWindowState.Classify(pending with{Status="ERROR|窗口最小化失败"},desktop,true,false)==HiddenObservation.Returned,"Legacy rolled-back failure retained a visible restored window");
        string folder=Path.Combine(Path.GetTempPath(),"ScreenMover-reconcile-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try{
            string path=Path.Combine(folder,"hidden.json");
            var closed=entry with{Id=Guid.NewGuid()};var unknown=entry with{Id=Guid.NewGuid()};
            LocalData.Write(path,new[]{entry,closed,unknown,pending});
            var records=new HiddenWindows(path);
            var removed=records.Reconcile(x=>x.Id==entry.Id?HiddenObservation.Returned:x.Id==closed.Id?HiddenObservation.Closed:HiddenObservation.Unknown);
            Check(removed.Count==1&&removed[0].Id==entry.Id,"Manual recovery reconciliation removed wrong entries");
            Check(records.ActiveCount==2&&records.Read().Count==3,"Closed records still inflated hidden count or uncertain entries were dropped");
            var loaded=new HiddenWindows(path);Check(loaded.ActiveCount==2&&loaded.Read().Single(x=>x.Id==closed.Id).Closed,"Reconciled states did not persist");
            var before=File.ReadAllText(path);records.Reconcile(_=>HiddenObservation.Unknown);
            Check(File.ReadAllText(path)==before,"Unknown state overwrote recovery information");
        }finally{foreach(var file in Directory.GetFiles(folder))File.Delete(file);Directory.Delete(folder);}
        Check(TrayTip.Format("桌面 2")=="ScreenMover\n当前桌面：桌面 2","Native tooltip did not contain exactly two lines");
        string longTip=TrayTip.Format(string.Concat(Enumerable.Repeat("😀",200)));
        Check(longTip.Length<=127&&longTip.EndsWith('…')&&!char.IsHighSurrogate(longTip[^2]),"Tooltip overflowed szTip or split a Unicode character");
        Check(TrayTip.Format("a\nb\rc").Count(c=>c=='\n')==1,"Desktop name injected extra tooltip lines");
        Console.WriteLine("PASS hide/recovery: delayed game restore, no-minimize success, manual moves, desktop visits, uncertain/closed states, persistence and native tooltip bounds");
    }
}
