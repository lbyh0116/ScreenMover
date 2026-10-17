using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using Windows.UI.Core;
namespace ScreenMover;

public sealed partial class MainWindow {
    Interop.GlobalHotkey? globalHotkey;
    Interop.GlobalHotkey EnsureGlobalHotkey()=>globalHotkey??=new(WinRT.Interop.WindowNative.GetWindowHandle(this),()=>DispatcherQueue.TryEnqueue(async()=>{if(!closed)await OpenQuick();}));
    void ConfigureHotkey(){
        try{
            var binding=EnsureGlobalHotkey().Binding;
            if(binding.Current==settings.QuickHotkey)return;
            using var update=binding.Prepare(settings.QuickHotkey);update.Commit();
        }catch(Exception ex){Notice("全局快捷键未启用："+ex.Message);}
    }
    static TextBox HotkeyCapture(string value){
        var box=new TextBox{Header="全局快捷键：打开 / 收起快捷面板",IsReadOnly=true,Text=value,PlaceholderText="点击后按组合键；Delete 清除"};
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
