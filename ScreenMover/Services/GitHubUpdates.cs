using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
namespace ScreenMover.Services;

public sealed record ReleaseVersion(int Major,int Minor,int Patch,string[] Pre) : IComparable<ReleaseVersion> {
    public static ReleaseVersion Parse(string text){
        if(text.Length>128)throw new FormatException("版本号过长");
        var m=Regex.Match(text,@"^v?(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z.-]+)?$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
        if(!m.Success)throw new FormatException("版本号格式无效");
        var pre=m.Groups[4].Success?m.Groups[4].Value.Split('.'):[];
        if(pre.Any(s=>s.All(char.IsAsciiDigit)&&s.Length>1&&s[0]=='0'))throw new FormatException("预览版本号格式无效");
        return new(int.Parse(m.Groups[1].Value),int.Parse(m.Groups[2].Value),int.Parse(m.Groups[3].Value),pre);
    }
    public int CompareTo(ReleaseVersion? other){
        if(other==null)return 1;
        foreach(var n in new[]{Major.CompareTo(other.Major),Minor.CompareTo(other.Minor),Patch.CompareTo(other.Patch)})if(n!=0)return n;
        if(Pre.Length==0||other.Pre.Length==0)return (Pre.Length==0?1:0).CompareTo(other.Pre.Length==0?1:0);
        for(int i=0;i<Math.Min(Pre.Length,other.Pre.Length);i++){
            string a=Pre[i],b=other.Pre[i];bool an=a.All(char.IsAsciiDigit),bn=b.All(char.IsAsciiDigit);
            int c=an&&bn?(a.Length==b.Length?string.CompareOrdinal(a,b):a.Length.CompareTo(b.Length)):an!=bn?(an?-1:1):string.CompareOrdinal(a,b);
            if(c!=0)return c;
        }
        return Pre.Length.CompareTo(other.Pre.Length);
    }
}
public sealed record GitHubRelease(string Tag,string Notes,Uri Page,Uri? Download,long Size,string? Sha256);
public sealed class GitHubUpdates {
    public const string Repository="lbyh0116/ScreenMover";
    public const long MaximumDownload=512L*1024*1024;
    static readonly HttpClient Shared=new(new HttpClientHandler{AllowAutoRedirect=false}){Timeout=Timeout.InfiniteTimeSpan};
    readonly HttpClient http;
    public GitHubUpdates(HttpClient? client=null)=>http=client??Shared;
    public static string Architecture=>RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
    public static bool Portable=>string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPX_PACKAGE_FAMILY_NAME"))&&!HasPackageIdentity();
    static bool HasPackageIdentity(){try{_ = Windows.ApplicationModel.Package.Current;return true;}catch(InvalidOperationException){return false;}catch(COMException){return false;}}
    public async Task<GitHubRelease?> Check(bool previews,CancellationToken token){
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(TimeSpan.FromSeconds(25));
        try{
            using var response=await Send(new Uri($"https://api.github.com/repos/{Repository}/releases?per_page=100"),false,timeout.Token);
            var data=await ReadBounded(response,2*1024*1024,timeout.Token);
            return Select(data,previews,Architecture);
        }catch(HttpRequestException){return await PublicRelease(previews,timeout.Token);}
    }
    async Task<GitHubRelease?> PublicRelease(bool previews,CancellationToken token){
        string? tag;
        if(previews)tag=await FeedTag(token);
        else{
            using var latest=await Send(new Uri($"https://github.com/{Repository}/releases/latest"),true,token);
            tag=TagFromPage(latest.RequestMessage?.RequestUri);
            if(tag!=null&&ReleaseVersion.Parse(tag).Pre.Length!=0)tag=null;
        }
        if(tag==null)return null;
        return await PublicAsset(tag,token);
    }
    static string? TagFromPage(Uri? uri){
        string prefix=$"/{Repository}/releases/tag/";
        if(uri==null||uri.Scheme!="https"||!uri.IsDefaultPort||uri.UserInfo!=""||uri.Host!="github.com"||!uri.AbsolutePath.StartsWith(prefix,StringComparison.Ordinal))return null;
        string tag=Uri.UnescapeDataString(uri.AbsolutePath[prefix.Length..]);
        try{ReleaseVersion.Parse(tag);return tag;}catch(Exception e)when(e is FormatException or OverflowException){return null;}
    }
    async Task<string?> FeedTag(CancellationToken token){
        using var response=await Send(new Uri($"https://github.com/{Repository}/releases.atom"),true,token);
        byte[] xml=await ReadBounded(response,2*1024*1024,token);
        using var memory=new MemoryStream(xml);using var reader=XmlReader.Create(memory,new(){DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=2*1024*1024});
        var doc=XDocument.Load(reader);XNamespace atom="http://www.w3.org/2005/Atom";
        var choices=new List<(ReleaseVersion Version,string Tag)>();
        foreach(var entry in doc.Descendants(atom+"entry")){
            var link=entry.Elements(atom+"link").Select(e=>(string?)e.Attribute("href")).FirstOrDefault(s=>s?.Contains("/releases/tag/",StringComparison.Ordinal)==true);
            if(!Uri.TryCreate(link,UriKind.Absolute,out var uri))continue;
            string? tag=TagFromPage(uri);if(tag!=null)choices.Add((ReleaseVersion.Parse(tag),tag));
        }
        return choices.OrderByDescending(c=>c.Version).Select(c=>c.Tag).FirstOrDefault();
    }
    async Task<GitHubRelease> PublicAsset(string tag,CancellationToken token){
        var version=ReleaseVersion.Parse(tag);
        string root=$"https://github.com/{Repository}/releases/download/{Uri.EscapeDataString(tag)}/",file=$"ScreenMover-v{tag.TrimStart('v')}-win-{Architecture}.zip";
        string? sha=null;var sums=new List<string>{"SHA256SUMS.txt"};
        if(version.Pre.Length==2&&version.Pre[0]=="preview"&&version.Pre[1].All(char.IsAsciiDigit))sums.Add($"SHA256SUMS-preview{version.Pre[1]}.txt");
        foreach(string name in sums)try{
            using var checksum=await Send(new Uri(root+name),true,token);
            string text=System.Text.Encoding.UTF8.GetString(await ReadBounded(checksum,64*1024,token));
            foreach(string line in text.Split('\n')){
                var match=Regex.Match(line.Trim('\uFEFF','\r',' '),@"^([0-9a-fA-F]{64})\s+\*?(.+)$");
                if(match.Success&&match.Groups[2].Value==file){sha=match.Groups[1].Value.ToLowerInvariant();break;}
            }
            if(sha!=null)break;
        }catch(HttpRequestException){}
        return new(tag,"GitHub API 暂不可用，已通过公开发布页读取版本。完整更新说明请打开发布页。",new Uri($"https://github.com/{Repository}/releases/tag/{Uri.EscapeDataString(tag)}"),new Uri(root+file),0,sha);
    }
    public static GitHubRelease? Select(byte[] json,bool previews,string architecture){
        using var doc=JsonDocument.Parse(json,new(){MaxDepth=32});
        var choices=new List<(ReleaseVersion Version,GitHubRelease Release)>();
        foreach(var item in doc.RootElement.EnumerateArray()){
            if(item.GetProperty("draft").GetBoolean()||(!previews&&item.GetProperty("prerelease").GetBoolean()))continue;
            string tag=item.GetProperty("tag_name").GetString()??"";ReleaseVersion version;
            try{version=ReleaseVersion.Parse(tag);}catch(Exception e)when(e is FormatException or OverflowException){continue;}
            if(!previews&&version.Pre.Length!=0)continue;
            var page=new Uri($"https://github.com/{Repository}/releases/tag/{Uri.EscapeDataString(tag)}");
            string expected=$"ScreenMover-v{tag.TrimStart('v')}-win-{architecture}.zip";
            Uri? download=null;long size=0;string? sha=null;
            foreach(var asset in item.GetProperty("assets").EnumerateArray()){
                if(asset.GetProperty("name").GetString()!=expected)continue;
                var url=asset.GetProperty("browser_download_url").GetString();
                if(!Uri.TryCreate(url,UriKind.Absolute,out var uri)||!AssetUrl(uri))continue;
                size=asset.GetProperty("size").GetInt64();
                if(size<=0||size>MaximumDownload)continue;
                if(asset.TryGetProperty("digest",out var digest)&&digest.ValueKind==JsonValueKind.String){
                    string value=digest.GetString()??"";
                    if(Regex.IsMatch(value,@"^sha256:[0-9a-fA-F]{64}$"))sha=value[7..].ToLowerInvariant();
                }
                download=uri;break;
            }
            string notes=item.TryGetProperty("body",out var body)&&body.ValueKind==JsonValueKind.String?body.GetString()??"":"";
            choices.Add((version,new(tag,notes.Length>12000?notes[..12000]+"…":notes,page,download,size,sha)));
        }
        return choices.OrderByDescending(c=>c.Version).Select(c=>c.Release).FirstOrDefault();
    }
    public static bool AssetUrl(Uri uri)=>uri.Scheme=="https"&&uri.IsDefaultPort&&uri.UserInfo==""&&uri.Host=="github.com"&&uri.AbsolutePath.StartsWith($"/{Repository}/releases/download/",StringComparison.Ordinal);
    static bool DownloadHost(Uri uri)=>uri.Scheme=="https"&&uri.IsDefaultPort&&uri.UserInfo==""&&uri.Host is "github.com" or "release-assets.githubusercontent.com" or "objects.githubusercontent.com";
    async Task<HttpResponseMessage> Send(Uri uri,bool download,CancellationToken token){
        for(int i=0;i<6;i++){
            if(download&&!DownloadHost(uri))throw new InvalidDataException("下载被重定向到不受信任的地址");
            using var request=new HttpRequestMessage(HttpMethod.Get,uri);
            request.Headers.UserAgent.ParseAdd("ScreenMover/"+BuildInfo.DisplayVersion);
            request.Headers.Accept.ParseAdd(download?"application/octet-stream":"application/vnd.github+json");
            var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
            response.RequestMessage??=request;
            if((int)response.StatusCode is 301 or 302 or 303 or 307 or 308){
                var location=response.Headers.Location;response.Dispose();
                if(!download||location==null)throw new HttpRequestException("GitHub API 地址发生变化，请手动查看发布页");
                uri=location.IsAbsoluteUri?location:new Uri(uri,location);continue;
            }
            if(!response.IsSuccessStatusCode){var code=response.StatusCode;response.Dispose();throw new HttpRequestException(code is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests?"GitHub 请求受限，请稍后重试":$"GitHub 请求失败（{(int)code}）");}
            return response;
        }
        throw new HttpRequestException("下载重定向次数过多");
    }
    static async Task<byte[]> ReadBounded(HttpResponseMessage response,int limit,CancellationToken token){
        if(response.Content.Headers.ContentLength>limit)throw new InvalidDataException("GitHub 响应过大");
        using var stream=await response.Content.ReadAsStreamAsync(token);using var output=new MemoryStream();var buffer=new byte[32768];int read;
        while((read=await stream.ReadAsync(buffer,token))>0){if(output.Length+read>limit)throw new InvalidDataException("GitHub 响应过大");await output.WriteAsync(buffer.AsMemory(0,read),token);}
        return output.ToArray();
    }
    public async Task Download(GitHubRelease release,string path,IProgress<double>? progress,CancellationToken token){
        if(release.Download==null||!AssetUrl(release.Download)||release.Sha256==null||release.Size<0||release.Size>MaximumDownload)throw new InvalidDataException("此版本缺少对应架构的下载包或可信 SHA-256，请使用发布页手动更新");
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(TimeSpan.FromMinutes(15));
        bool created=false;
        try{
            using var response=await Send(release.Download,true,timeout.Token);
            long expected=release.Size;
            if(response.Content.Headers.ContentLength is long length){
                if(length<=0||length>MaximumDownload||expected>0&&length!=expected)throw new InvalidDataException("下载文件大小与发布信息不符");
                expected=length;
            }
            using var input=await response.Content.ReadAsStreamAsync(timeout.Token);
            await using var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,65536,true);
            created=true;
            using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);var buffer=new byte[65536];long total=0;int read;
            while((read=await input.ReadAsync(buffer,timeout.Token))>0){total+=read;if(total>MaximumDownload||expected>0&&total>expected)throw new InvalidDataException("下载文件超出声明大小");hash.AppendData(buffer,0,read);await output.WriteAsync(buffer.AsMemory(0,read),timeout.Token);if(expected>0)progress?.Report((double)total/expected);}
            if(total==0||expected>0&&total!=expected||!Convert.ToHexString(hash.GetHashAndReset()).Equals(release.Sha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("SHA-256 校验失败，下载包不会安装");
        }catch{if(created)try{File.Delete(path);}catch{}throw;}
    }
}
