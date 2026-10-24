using System.Text.Json;
namespace ScreenMover.Services;

public static class SettingsProfile {
    const int MaximumLength=256*1024;
    sealed record Document(string Format,int Version,JsonElement Settings);
    public static string Export(AppSettings settings){
        var copy=settings with{};copy.Normalize();
        var portable=JsonSerializer.SerializeToNode(copy,LocalData.Json)!.AsObject();
        portable.Remove(nameof(AppSettings.Width));portable.Remove(nameof(AppSettings.Height));portable.Remove(nameof(AppSettings.SizeInDips));
        return JsonSerializer.Serialize(new Document("ScreenMover.Settings",1,JsonSerializer.SerializeToElement(portable)),LocalData.Json);
    }
    public static AppSettings Import(string json){
        if(json.Length>MaximumLength)throw new InvalidDataException(L.T("设置文件超过 256 KB"));
        using var document=JsonDocument.Parse(json,new(){MaxDepth=16});
        var root=document.RootElement;
        if(root.ValueKind!=JsonValueKind.Object)throw new InvalidDataException(L.T("设置文件必须是 JSON 对象"));
        JsonElement settings;
        if(root.TryGetProperty("Format",out var format)){
            if(format.ValueKind!=JsonValueKind.String||format.GetString()!="ScreenMover.Settings"||!root.TryGetProperty("Version",out var version)||version.ValueKind!=JsonValueKind.Number||!version.TryGetInt32(out int v)||v!=1)
                throw new InvalidDataException(L.T("设置格式或版本不受支持"));
            if(!root.TryGetProperty("Settings",out settings)||settings.ValueKind!=JsonValueKind.Object)throw new InvalidDataException(L.T("缺少有效设置内容"));
        }else settings=root;
        var known=typeof(AppSettings).GetProperties().Where(p=>p.CanWrite).Select(p=>p.Name).ToHashSet();
        if(!settings.EnumerateObject().Any(p=>known.Contains(p.Name)))throw new InvalidDataException(L.T("文件不包含 ScreenMover 设置"));
        var value=settings.Deserialize<AppSettings>(LocalData.Json)??throw new InvalidDataException(L.T("设置为空"));
        var quick=HotkeySpec.Parse(value.QuickHotkey);var hide=HotkeySpec.Parse(value.HideHotkey);
        if(quick!=null&&quick.Name==hide?.Name)throw new FormatException(L.T("两个操作不能使用相同的快捷键"));
        value.Normalize();return value;
    }
    public static async Task<AppSettings> Read(string path){
        using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        if(file.Length>MaximumLength)throw new InvalidDataException(L.T("设置文件超过 256 KB"));
        using var reader=new StreamReader(file);
        return Import(await reader.ReadToEndAsync());
    }
    public static void WriteText(string path,string text){
        string temp=Path.GetFullPath(path)+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllText(temp,text);File.Move(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}
    }
}
