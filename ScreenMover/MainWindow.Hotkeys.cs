using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using Windows.UI.Core;
namespace ScreenMover;

public sealed partial class MainWindow {
    Interop.GlobalHotkey? globalHotkey;
    Interop.GlobalHotkey? hideHotkey;
    Interop.GlobalHotkey EnsureHideHotkey()=>hideHotkey??=new(WinRT.Interop.WindowNative.GetWindowHandle(this),()=>{
        var target=Interop.ForegroundWindow.Capture();
        if(target.Handle!=0&&target.Pid!=Environment.ProcessId)DispatcherQueue.TryEnqueue(async()=>await HideForeground(target));
    },46,0x5311);
    void ConfigureHideHotkey(){
        try{
            var binding=EnsureHideHotkey().Binding;
            string wanted=Services.PlatformSupport.Current.CanHide?settings.HideHotkey:"";
            if(binding.Current==wanted)return;
            using var update=binding.Prepare(wanted);update.Commit();
        }catch(Exception ex){Notice(L.T("隐藏快捷键未启用：")+ex.Message);}
    }
    async Task HideForeground((nint Handle,nint Owner,int Pid) target){
        if(closed||busy)return;
        busy=true;UpdateStatus();
        try{
            var result=await Task.Run(()=>{
                var current=Services.WindowQuery.Foreground(windows.Read(),target.Handle,target.Owner,target.Pid);
                return current==null?new Models.OperationResult(L.T("前台窗口"),false,L.T("窗口已关闭或不属于可管理的软件窗口")):hidden.Hide(current);
            });
            ShowResults(L.T("快捷隐藏"),new[]{result});
        }catch(Exception ex){Notice(L.T("快捷隐藏失败：")+ex.Message);}
        finally{busy=false;await Refresh();UpdateStatus();}
    }
    Interop.GlobalHotkey EnsureGlobalHotkey()=>globalHotkey??=new(WinRT.Interop.WindowNative.GetWindowHandle(this),()=>DispatcherQueue.TryEnqueue(async()=>{if(!closed)await OpenQuick();}));
    void ConfigureHotkey(){
        try{
            var binding=EnsureGlobalHotkey().Binding;
            if(binding.Current==settings.QuickHotkey)return;
            using var update=binding.Prepare(settings.QuickHotkey);update.Commit();
        }catch(Exception ex){Notice(L.T("全局快捷键未启用：")+ex.Message);}
    }
}
