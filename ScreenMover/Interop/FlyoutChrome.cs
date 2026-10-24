using System.Runtime.InteropServices;
using static ScreenMover.Interop.Native;
namespace ScreenMover.Interop;

public sealed class FlyoutChrome : IDisposable {
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW",SetLastError=true)] static extern nint SetWindowLongPtr(nint hwnd,int index,nint value);
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(nint hwnd,int attribute,ref int value,int size);
    readonly nint window;
    readonly SubclassProc callback;
    bool applying,disposed,dark;
    public int BorderResult {get;private set;}
    public static long ClientStyle(long style)=>style&~(0x00C00000L|0x00040000L);
    public static long ClientExtendedStyle(long style)=>style&~(0x100L|0x200L|0x20000L);
    public FlyoutChrome(nint hwnd){
        window=hwnd;
        callback=(h,msg,w,l,id,data)=>{
            // Cover the native frame with the client surface; changing its color alone
            // does not remove the non-client margins created by the presenter.
            if(msg==0x83&&w!=0)return 0;
            var result=DefSubclassProc(h,msg,w,l);
            if(msg is 0x6 or 0x86 or 0x7D or 0x31A or 0x31E or 0x320)Apply(dark);
            return result;
        };
        if(!SetWindowSubclass(window,callback,45,0))throw new InvalidOperationException(L.T("无法设置快捷面板边框"));
        Apply(false);
    }
    public void Apply(bool isDark){
        if(disposed||applying)return;applying=true;dark=isDark;
        try{
            long style=GetWindowLongPtr(window,-16).ToInt64(),extended=GetWindowLongPtr(window,-20).ToInt64();
            bool changed=style!=ClientStyle(style)||extended!=ClientExtendedStyle(extended);
            if(style!=ClientStyle(style))SetWindowLongPtr(window,-16,(nint)ClientStyle(style));
            if(extended!=ClientExtendedStyle(extended))SetWindowLongPtr(window,-20,(nint)ClientExtendedStyle(extended));
            int none=unchecked((int)0xFFFFFFFE),round=2,theme=isDark?1:0;
            BorderResult=DwmSetWindowAttribute(window,34,ref none,sizeof(int));
            DwmSetWindowAttribute(window,33,ref round,sizeof(int));DwmSetWindowAttribute(window,20,ref theme,sizeof(int));
            if(changed)SetWindowPos(window,0,0,0,0,0,0x1|0x2|0x4|0x10|0x20);
        }finally{applying=false;}
    }
    public void Dispose(){if(disposed)return;disposed=true;RemoveWindowSubclass(window,callback,45);}
}
