using ScreenMover.Models;
namespace ScreenMover.Services;

public enum DropAction { None, Move, Hide, Current, Unavailable }
public sealed record DropIntent(DropAction Action,string Label) {
    public bool Allowed=>Action is DropAction.Move or DropAction.Hide;
    public static DropIntent For(DisplayInfo? target,IReadOnlyCollection<WindowInfo> windows,bool quick,bool hideAvailable=true){
        if(target==null||windows.Count==0)return new(DropAction.None,"");
        if(!target.Active)return new(DropAction.Unavailable,L.T("屏幕未启用"));
        if(windows.All(w=>w.Gdi==target.Gdi))return quick
            ?hideAvailable?new(DropAction.Hide,L.T("隐藏到新桌面")):new(DropAction.Unavailable,L.T("此系统暂不支持隐藏"))
            :new(DropAction.Current,L.T("已在此屏幕"));
        return new(DropAction.Move,L.F("移到屏幕 {0}", target.Number));
    }
}
