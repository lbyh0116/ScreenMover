using ScreenMover.Models;
namespace ScreenMover.Services;
public static class WindowQuery {
    public static List<WindowInfo> Filter(IEnumerable<WindowInfo> windows,string? gdi,string query,bool allScreens) {
        var words=query.Trim().Split(' ',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
        return windows.Where(w=>(allScreens||w.Gdi==gdi)&&words.All(word=>
            w.Title.Contains(word,StringComparison.OrdinalIgnoreCase)||
            w.ProcessName.Contains(word,StringComparison.OrdinalIgnoreCase)||
            w.Pid.ToString().Equals(word,StringComparison.Ordinal))).ToList();
    }
}
