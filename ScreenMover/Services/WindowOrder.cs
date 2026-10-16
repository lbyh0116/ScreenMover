using System.Globalization;
using ScreenMover.Models;
namespace ScreenMover.Services;
public sealed class WindowOrder {
    readonly string path;
    List<string> keys;
    public string? Warning {get;}
    public WindowOrder(string? file=null){
        path=file??Path.Combine(LocalData.Folder,"window-order.json");
        keys=LocalData.Read(path,()=>new List<string>(),out var warning).Distinct().ToList();Warning=warning;
    }
    public List<WindowInfo> Sort(IEnumerable<WindowInfo> windows){
        var ranks=keys.Select((k,i)=>(k,i)).ToDictionary(x=>x.k,x=>x.i);
        return windows.OrderBy(w=>ranks.GetValueOrDefault(w.OrderKey,int.MaxValue)).ToList();
    }
    public bool Reorder(IEnumerable<WindowInfo> all,IEnumerable<WindowInfo> dragged,WindowInfo target,bool after){
        var ordered=Sort(all);var moving=dragged.Select(w=>w.OrderKey).ToHashSet();
        if(moving.Contains(target.OrderKey))return false;
        var group=ordered.Where(w=>moving.Contains(w.OrderKey)).ToList();
        var rest=ordered.Where(w=>!moving.Contains(w.OrderKey)).ToList();
        int index=rest.FindIndex(w=>w.OrderKey==target.OrderKey);
        if(index<0||group.Count==0)return false;
        rest.InsertRange(index+(after?1:0),group);
        var present=rest.Select(w=>w.OrderKey).ToList();
        var next=present.Concat(keys.Where(k=>!present.Contains(k))).Take(4096).ToList();
        LocalData.Write(path,next);keys=next;return true;
    }
}
public static class WindowTitle {
    public static string Middle(string text,int max=30){
        var parts=StringInfo.ParseCombiningCharacters(text);
        if(parts.Length<=max)return text;
        int head=(max-1)*2/3,tail=max-1-head;
        return text[..parts[head]]+"…"+text[parts[^tail]..];
    }
}
