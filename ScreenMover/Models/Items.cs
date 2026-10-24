using ScreenMover.Interop;
namespace ScreenMover.Models;
public sealed record DisplayInfo(string Key,int Number,string Gdi,string Name,string Driver,bool Active,bool Primary,Native.RECT Bounds,Native.RECT Work,double Hertz,nint Monitor) {
public string Heading=>Active?L.F("屏幕 {0}", Number)+(Primary?L.T(" · 主显示器"):""):Name+L.T(" · 未启用");
public string Details=>Active?L.F("{0}\n{1}\n{2} × {3} · {4:0.##} Hz\n坐标 ({5}, {6})\n{7}", Gdi, Name, Bounds.Width, Bounds.Height, Hertz, Bounds.Left, Bounds.Top, Driver):L.T("存在，但系统未启用\n请在系统显示设置中启用");
}
public sealed record WindowInfo(nint Handle,int Pid,string ProcessName,string Title,int ShowCommand,Native.RECT Restore,string Gdi) : System.ComponentModel.INotifyPropertyChanged {
public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
public long ProcessStartedTicks {get;init;}
public string OrderKey=>$"{Pid}:{ProcessStartedTicks}:{Handle.ToInt64()}";
public string ShortTitle=>Services.WindowTitle.Middle(Title);
public string AppName=>Services.ApplicationNames.Display(ProcessName);
bool selected,iconFallback=true;
object? icon;
public object? AppIcon=>icon;
public double PreviewLimit=>preview==null?48:double.PositiveInfinity;
public object? DisplayPreview=>preview??(iconFallback?icon:null);
public bool Selected=>selected;
public void SetSelected(bool value){if(selected==value)return;selected=value;PropertyChanged?.Invoke(this,new(nameof(Selected)));}
public void SetIcon(object? value,bool fallback=true){icon=value;iconFallback=fallback;PropertyChanged?.Invoke(this,new(nameof(AppIcon)));PropertyChanged?.Invoke(this,new(nameof(DisplayPreview)));}
object? preview;
string previewCaption=L.T("尚未截图");
public object? Preview=>preview;
public string PreviewCaption=>previewCaption;
public void SetPreview(object? image,string caption){preview=image;previewCaption=caption;PropertyChanged?.Invoke(this,new(nameof(Preview)));PropertyChanged?.Invoke(this,new(nameof(PreviewLimit)));PropertyChanged?.Invoke(this,new(nameof(DisplayPreview)));PropertyChanged?.Invoke(this,new(nameof(PreviewCaption)));}
public string State=>ShowCommand is 2 or 7?L.T("最小化"):ShowCommand==3?L.T("最大化"):L.T("普通");
public string Summary=>$"{ProcessName} · {State}";
public string Details=>$"{ProcessName} · PID {Pid} · {State} · {Restore} · HWND 0x{Handle:X}";
}
public sealed record DisplaySnapshot(List<DisplayInfo> Displays,int AllPaths,int ActivePaths,int DesktopCount);
public sealed record OperationResult(string Title,bool Success,string Message);
