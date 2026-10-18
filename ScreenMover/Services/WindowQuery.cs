using ScreenMover.Models;
namespace ScreenMover.Services;
public sealed class WindowListRevision {
    string? current;
    public void Invalidate()=>current=null;
    public bool Update(string next) {
        if(current==next)return false;
        current=next;return true;
    }
}
public static class WindowQuery {
    public static WindowInfo? Foreground(IEnumerable<WindowInfo> windows,nint handle,nint owner,int pid){
        var matching=windows.Where(w=>w.Pid==pid).ToList();
        return matching.FirstOrDefault(w=>w.Handle==handle)??matching.FirstOrDefault(w=>owner!=0&&w.Handle==owner);
    }
    public static List<WindowInfo> Filter(IEnumerable<WindowInfo> windows,string? gdi,string query,bool allScreens) {
        var words=query.Trim().Split(' ',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
        return windows.Where(w=>(allScreens||w.Gdi==gdi)&&words.All(word=>
            w.Title.Contains(word,StringComparison.OrdinalIgnoreCase)||
            w.ProcessName.Contains(word,StringComparison.OrdinalIgnoreCase)||
            w.Pid.ToString().Equals(word,StringComparison.Ordinal))).ToList();
    }
}
