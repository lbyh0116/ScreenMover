namespace ScreenMover.Services;

public static class ApplicationNames {
    public static string Display(string? process){
        string name=(process??"").Trim();
        if(name.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))name=name[..^4];
        return name.ToUpperInvariant() switch{
            "WINWORD"=>"Word","EXCEL"=>"Excel","POWERPNT"=>"PowerPoint","ONENOTE"=>"OneNote","OUTLOOK"=>"Outlook",
            "CHROME"=>"Chrome","MSEDGE"=>"Edge","FIREFOX"=>"Firefox","EXPLORER"=>L.T("文件资源管理器"),
            "WECHAT" or "WEIXIN"=>L.T("微信"),"QQ"=>"QQ","CHATGPT"=>"ChatGPT","HERMES"=>"Hermes","CODE"=>"Visual Studio Code",
            "MS-TEAMS"=>"Teams","WINDOWSTERMINAL"=>L.T("Windows 终端"),"NOTEPAD"=>L.T("记事本"),
            "DELTAFORCECLIENT-WIN64-SHIPPING" or "DELTAFORCECLIENT"=>L.T("三角洲行动"),
            "" or "已退出 / 不可访问"=>L.T("应用"),_=>name
        };
    }
}
