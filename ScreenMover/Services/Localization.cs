using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace ScreenMover.Services;

public static class Localization {
    static readonly string systemLanguage=CultureInfo.CurrentUICulture.Name;
    static readonly Dictionary<string,string[]> catalog=Load();
    static readonly Regex externalText=new(string.Join("|",catalog.Keys.Where(k=>!k.Contains('{')).OrderByDescending(k=>k.Length).Select(Regex.Escape)),RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
    public static string Code {get;private set;}="zh-CN";
    public static IReadOnlyDictionary<string,string[]> Catalog=>catalog;
    static Dictionary<string,string[]> Load(){
        using var stream=typeof(Localization).Assembly.GetManifestResourceStream("ScreenMover.Localization.Strings.json")
            ??throw new InvalidDataException("Missing localization catalog");
        return JsonSerializer.Deserialize<Dictionary<string,string[]>>(stream)??throw new InvalidDataException("Empty localization catalog");
    }
    public static string Resolve(string preference,string? system=null){
        var code=preference=="System"?(system??systemLanguage):preference;
        return code.Split('-','_')[0].ToLowerInvariant() switch{"zh"=>"zh-CN","de"=>"de","fr"=>"fr","es"=>"es",_=>"en"};
    }
    public static void Initialize(string preference){
        Code=Resolve(preference);
        CultureInfo.CurrentUICulture=CultureInfo.GetCultureInfo(Code);
        CultureInfo.DefaultThreadCurrentUICulture=CultureInfo.GetCultureInfo(Code);
    }
    public static string T(string source)=>Translate(source,Code);
    public static string Translate(string source,string language){
        int index=language switch{"en"=>0,"de"=>1,"fr"=>2,"es"=>3,_=>-1};
        return index>=0&&catalog.TryGetValue(source,out var translations)&&index<translations.Length?translations[index]:source;
    }
    public static string F(string source,params object?[] args)=>string.Format(CultureInfo.CurrentCulture,T(source),args);
    // Only for our helper diagnostics and persisted status text, never window titles or user names.
    public static string External(string message){
        if(Code=="zh-CN"||string.IsNullOrEmpty(message))return message;
        try{return externalText.Replace(message,m=>T(m.Value));}catch(RegexMatchTimeoutException){return message;}
    }
}
