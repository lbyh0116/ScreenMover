using System.Text.Json;
namespace ScreenMover.Services;

public static class LocalData {
    public static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ScreenMover");
    public static readonly JsonSerializerOptions Json = new() { WriteIndented=true, IncludeFields=true };
    public static T Read<T>(string path, Func<T> defaults, out string? warning) {
        warning=null;
        if(!File.Exists(path))return defaults();
        try {return JsonSerializer.Deserialize<T>(File.ReadAllText(path),Json) ?? throw new InvalidDataException("文件内容为空");}
        catch(Exception ex) {
            warning=$"{Path.GetFileName(path)} 无法读取：{ex.Message}";
            // Keep the original recoverable rather than silently overwrite it.
            string backup=path+"."+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+".backup";
            File.Copy(path,backup); warning+="；已备份至 "+backup;
            return defaults();
        }
    }
    public static void Write<T>(string path,T value) {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try {File.WriteAllText(temp,JsonSerializer.Serialize(value,Json));File.Move(temp,path,true);}
        finally {if(File.Exists(temp))File.Delete(temp);}
    }
}

public sealed record AppSettings {
    public bool IconFallback {get;set;}=true;
    public bool TrayEnabled {get;set;}=true;
    public bool CloseToTray {get;set;}=true;
    public string HideHotkey {get;set;}="";
    public string QuickHotkey {get;set;}="";
    public string Theme {get;set;}="System";
    public string AccentColor {get;set;}="";
    public bool ShowInactive {get;set;}=true;
    public bool RefreshOnActivate {get;set;}=true;
    public int RefreshSeconds {get;set;}=60; // 0 disables the periodic trigger.
    public bool ScreenPreviews {get;set;}=true;
    public bool WindowPreviews {get;set;}=true;
    public int MoveMargin {get;set;}=60;
    public bool ShrinkToFit {get;set;}=true;
    public int NoticeSeconds {get;set;}=5;
    public bool RememberSize {get;set;}=true;
    public bool SizeInDips {get;set;}
    public int Width {get;set;}=1320;
    public int Height {get;set;}=1080;
    public void Normalize() {
        try{QuickHotkey=HotkeySpec.Parse(QuickHotkey)?.Name??"";}catch(FormatException){QuickHotkey="";}
        try{HideHotkey=HotkeySpec.Parse(HideHotkey)?.Name??"";}catch(FormatException){HideHotkey="";}
        if(HideHotkey!=""&&HideHotkey==QuickHotkey)HideHotkey="";
        if(Theme is not ("System" or "Light" or "Dark"))Theme="System";
        AccentColor=AccentColors.Normalize(AccentColor);
        RefreshSeconds=RefreshSeconds==0?0:Math.Clamp(RefreshSeconds,15,3600);
        MoveMargin=Math.Clamp(MoveMargin,0,300);NoticeSeconds=Math.Clamp(NoticeSeconds,2,30);
        Width=Math.Clamp(Width,720,4000);Height=Math.Clamp(Height,640,2400);
    }
    public static string FilePath=>Path.Combine(LocalData.Folder,"settings.json");
}
