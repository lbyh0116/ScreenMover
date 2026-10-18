using ScreenMover.Models;
namespace ScreenMover.Services;

public static class PreviewPolicy {
    public const int MaximumWindows=64;
    public static List<WindowInfo> Select(IEnumerable<WindowInfo> windows,string? preferredScreen)=>windows
        .OrderByDescending(w=>w.Gdi==preferredScreen).ThenBy(w=>w.ShowCommand is 2 or 7)
        .Take(MaximumWindows).ToList();
    public static void Trim<T>(IDictionary<string,T> cache,IEnumerable<WindowInfo> models,IReadOnlyList<WindowInfo> retained){
        var keys=retained.Select(w=>w.OrderKey).ToHashSet();
        foreach(var key in cache.Keys.Where(k=>!keys.Contains(k)).ToList())cache.Remove(key);
        foreach(var model in models.Where(w=>!keys.Contains(w.OrderKey)))model.SetPreview(null,"内存受控：优先当前屏幕预览");
    }
}
