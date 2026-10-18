namespace ScreenMover.Services;

public static class SettingsCommit {
    public static void Write(AppSettings previous,AppSettings next,HotkeyBinding quick,HotkeyBinding hide,
        bool hideAvailable,bool wasStartup,bool wantStartup,Action<AppSettings> write,Action<bool> setStartup){
        using var q=quick.Prepare(next.QuickHotkey);
        using var h=hide.Prepare(hideAvailable?next.HideHotkey:"");
        write(next);
        try{if(wasStartup!=wantStartup)setStartup(wantStartup);}
        catch(Exception failure){
            try{write(previous);}catch(Exception rollback){throw new AggregateException("启动项更新失败，设置文件回退也未完成",failure,rollback);}
            throw;
        }
        q.Commit();h.Commit();
    }
}
