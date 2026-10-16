using static ScreenMover.Interop.Native;
namespace ScreenMover.Services;
public sealed class WindowEvents : IDisposable {
readonly WinEventProc callback; readonly SubclassProc subclass; readonly List<nint> hooks=[]; readonly nint hwnd;
public int Dirty=1;
public WindowEvents(nint h) {
hwnd=h;
callback=(_,evt,window,obj,child,thread,time)=>{if(evt==3||obj==0) Interlocked.Exchange(ref Dirty,1);};
foreach(uint evt in new uint[]{3,0x8000,0x8001,0x8002,0x8003,0x800B,0x800C}) {
var hook=SetWinEventHook(evt,evt,0,callback,0,0,2); if(hook==0) throw new InvalidOperationException("无法注册窗口事件"); hooks.Add(hook);
}
subclass=(window,msg,w,l,id,data)=>{if(msg is 0x7E or 0x219 or 0x2E0) Interlocked.Exchange(ref Dirty,1);return DefSubclassProc(window,msg,w,l);};
if(!SetWindowSubclass(h,subclass,1,0)) throw new InvalidOperationException("无法监听显示器变化");
}
public void Dispose(){foreach(var h in hooks)UnhookWinEvent(h);RemoveWindowSubclass(hwnd,subclass,1);}
}
