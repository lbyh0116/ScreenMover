using System.Runtime.InteropServices;
using ScreenMover.Services;
using static ScreenMover.Interop.Native;
namespace ScreenMover.Interop;

public sealed class GlobalHotkey : IDisposable {
    [DllImport("user32.dll",SetLastError=true)] static extern bool RegisterHotKey(nint window,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(nint window,int id);
    readonly nint window;
    readonly SubclassProc callback;
    bool disposed;
    readonly nuint subclassId;
    public HotkeyBinding Binding {get;}
    public GlobalHotkey(nint hwnd,Action invoke,nuint listenerId=44,int firstId=0x5301){
        window=hwnd;subclassId=listenerId;Binding=new((id,spec)=>RegisterHotKey(window,id,spec.Modifiers|0x4000,spec.Key),id=>UnregisterHotKey(window,id),firstId);
        callback=(h,msg,w,l,id,data)=>{if(!disposed&&msg==0x312&&Binding.CurrentId!=0&&w.ToInt32()==Binding.CurrentId){invoke();return 0;}return DefSubclassProc(h,msg,w,l);};
        if(!SetWindowSubclass(hwnd,callback,subclassId,0))throw new InvalidOperationException("无法监听全局快捷键");
    }
    public void Dispose(){if(disposed)return;disposed=true;Binding.Dispose();RemoveWindowSubclass(window,callback,subclassId);}
}
