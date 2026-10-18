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
            if(binding.Current==settings.HideHotkey)return;
            using var update=binding.Prepare(settings.HideHotkey);update.Commit();
        }catch(Exception ex){Notice("隐藏快捷键未启用："+ex.Message);}
    }
    async Task HideForeground((nint Handle,nint Owner,int Pid) target){
        if(closed||busy)return;
        busy=true;UpdateStatus();
        try{
            var result=await Task.Run(()=>{
                var current=Services.WindowQuery.Foreground(windows.Read(),target.Handle,target.Owner,target.Pid);
                return current==null?new Models.OperationResult("前台窗口",false,"窗口已关闭或不属于可管理的软件窗口"):hidden.Hide(current);
            });
            ShowResults("快捷隐藏",new[]{result});
        }catch(Exception ex){Notice("快捷隐藏失败："+ex.Message);}
        finally{busy=false;await Refresh();UpdateStatus();}
    }
    Interop.GlobalHotkey EnsureGlobalHotkey()=>globalHotkey??=new(WinRT.Interop.WindowNative.GetWindowHandle(this),()=>DispatcherQueue.TryEnqueue(async()=>{if(!closed)await OpenQuick();}));
    void ConfigureHotkey(){
        try{
            var binding=EnsureGlobalHotkey().Binding;
            if(binding.Current==settings.QuickHotkey)return;
            using var update=binding.Prepare(settings.QuickHotkey);update.Commit();
        }catch(Exception ex){Notice("全局快捷键未启用："+ex.Message);}
    }
    static TextBox HotkeyCapture(string value,string title="全局快捷键：打开 / 收起快捷面板"){
        var box=new TextBox{Header=title,IsReadOnly=true,Text=value,PlaceholderText="点击后按组合键；Delete 清除"};
        box.KeyDown+=(_,e)=>{
            e.Handled=true;
            if(e.Key is VirtualKey.Delete or VirtualKey.Back){box.Text="";return;}
            if(e.Key is VirtualKey.Control or VirtualKey.Menu or VirtualKey.Shift or VirtualKey.LeftWindows or VirtualKey.RightWindows or VirtualKey.Escape)return;
            bool Down(VirtualKey key)=>(InputKeyboardSource.GetKeyStateForCurrentThread(key)&CoreVirtualKeyStates.Down)!=0;
            var parts=new List<string>();if(Down(VirtualKey.Control))parts.Add("Ctrl");if(Down(VirtualKey.Menu))parts.Add("Alt");if(Down(VirtualKey.Shift))parts.Add("Shift");if(Down(VirtualKey.LeftWindows)||Down(VirtualKey.RightWindows))parts.Add("Win");
            var key=Services.HotkeySpec.KeyName((uint)e.Key);if(key!=""){parts.Add(key);box.Text=string.Join('+',parts);}
        };
        return box;
    }
}
