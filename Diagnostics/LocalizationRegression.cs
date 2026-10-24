using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ScreenMover.Services;

static partial class Regression {
    static void Languages(){
        Check(new AppSettings().Language=="System","Default language must follow Windows");
        foreach(var (input,expected) in new[]{("zh-TW","zh-CN"),("en-GB","en"),("de-AT","de"),("fr-CA","fr"),("es-MX","es"),("ja-JP","en")})
            Check(Localization.Resolve("System",input)==expected,"System language fallback: "+input);
        var invalid=new AppSettings{Language="unknown"};invalid.Normalize();Check(invalid.Language=="System","Invalid saved language");
        string Slots(string text)=>string.Join(",",Regex.Matches(text,@"\{(\d+)(?:[,:][^}]*)?\}").Select(m=>m.Groups[1].Value).Order());
        foreach(var (source,translations) in Localization.Catalog){
            Check(translations.Length==4,"Missing translations: "+source);
            foreach(var translated in translations){
                Check(!string.IsNullOrWhiteSpace(translated),"Empty translation: "+source);
                Check(Slots(source)==Slots(translated),"Changed format arguments: "+source);
                Check(CompositeFormat.Parse(source).MinimumArgumentCount==CompositeFormat.Parse(translated).MinimumArgumentCount,"Invalid composite format: "+source);
                Check(source=="简体中文"||!Regex.IsMatch(translated,@"[\u4e00-\u9fff]"),"Untranslated UI: "+source);
            }
        }
        var culture=CultureInfo.CurrentUICulture;var threadCulture=CultureInfo.DefaultThreadCurrentUICulture;
        try{
            foreach(string code in new[]{"zh-CN","en","de","fr","es"}){
                Localization.Initialize(code);
                var settings=SettingsProfile.Import(SettingsProfile.Export(new AppSettings{Language=code}));
                Check(settings.Language==code,"Language lost during settings round trip");
                var release=new GitHubRelease("v0.2.2","Release notes",new Uri("https://github.com/lbyh0116/ScreenMover/releases"),null,0,null);
                Check(UpdatePresentation.Notes(release,"0.2.2")==L.T("已是最新版"),"Latest release notes");
                Check(UpdatePresentation.Notes(release,"0.2.3")==L.T("已是最新版"),"Older release notes");
                Check(UpdatePresentation.Notes(release,"0.2.1")=="Release notes","Release content changed");
                Check(UpdatePresentation.Notes(release with{Notes=""},"0.2.1")==L.T("此版本暂无更新说明，请查看发布页。"),"Empty release notes fallback");
                string title="Private {0} 文档";
                Check(L.F("排序：放到「{0}」{1}",title,L.T("之前")).Contains(title),"Window title altered by translation");
                Check(TrayTip.Format(new string('x',300)).Length<=127&&TrayTip.Format("My desktop").Split('\n').Length==2,"Localized tray tooltip bounds");
                Check(L.External("已隐藏")==L.T("已隐藏"),"Stored hidden status translation");
                Check(ApplicationNames.Display("已退出 / 不可访问")==L.T("应用"),"Process sentinel localized before matching");
            }
        }finally{Localization.Initialize("zh-CN");CultureInfo.CurrentUICulture=culture;CultureInfo.DefaultThreadCurrentUICulture=threadCulture;}
        Console.WriteLine("PASS localization: five languages, regional fallback, complete catalogs, format arguments, settings round trip, title preservation, tray limits and update notes");
    }
}
