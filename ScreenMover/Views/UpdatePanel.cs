using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ScreenMover.Services;
namespace ScreenMover.Views;

public sealed class UpdatePanel : UserControl {
    readonly GitHubUpdates updates=new();
    readonly Button check=new(){Content=L.T("检查更新")},install=new(){Content=L.T("下载并安装"),IsEnabled=false},cancel=new(){Content=L.T("取消"),Visibility=Visibility.Collapsed};
    readonly CheckBox previews=new(){Content=L.T("包含预览版")};
    readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap},notes=new(){TextWrapping=TextWrapping.Wrap,IsTextSelectionEnabled=true};
    readonly ProgressBar progress=new(){Minimum=0,Maximum=100,Visibility=Visibility.Collapsed};
    readonly HyperlinkButton page=new(){Content=L.T("打开 GitHub 发布页"),NavigateUri=new Uri("https://github.com/lbyh0116/ScreenMover/releases")};
    CancellationTokenSource? cancellation;GitHubRelease? release;PreparedUpdate? prepared;
    public Func<PreparedUpdate,bool>? Install;
    public bool Working=>cancellation!=null;
    public UpdatePanel(){
        previews.IsChecked=BuildInfo.DisplayVersion.Contains('-');
        var body=new StackPanel{Spacing=10};Content=body;
        body.Children.Add(new TextBlock{Text=L.T("当前版本：")+BuildInfo.DisplayVersion,TextWrapping=TextWrapping.Wrap});
        body.Children.Add(previews);
        var row=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};row.Children.Add(check);row.Children.Add(install);row.Children.Add(cancel);body.Children.Add(row);
        body.Children.Add(progress);body.Children.Add(status);body.Children.Add(page);
        body.Children.Add(new Expander{Header=L.T("更新说明"),Content=notes,HorizontalAlignment=HorizontalAlignment.Stretch});
        body.Children.Add(new TextBlock{Text=L.T("检查更新会连接 GitHub，不上传窗口信息。安装时会退出并重启；替换失败会尝试还原旧文件，更新记录与备份保存在数据目录的 updates 文件夹。"),TextWrapping=TextWrapping.Wrap,Opacity=.7});
        if(!GitHubUpdates.Portable){check.IsEnabled=false;previews.IsEnabled=false;status.Text=L.T("此安装由 Microsoft Store 管理，请通过商店更新。");}
        check.Click+=async(_,_)=>await Check();install.Click+=async(_,_)=>await InstallClicked();cancel.Click+=(_,_)=>cancellation?.Cancel();
        previews.Click+=(_,_)=>{release=null;prepared=null;install.IsEnabled=false;notes.Text="";status.Text=L.T("更新渠道已变化，请重新检查。");};
        Unloaded+=(_,_)=>{cancellation?.Cancel();Install=null;};
    }
    void Begin(){cancellation=new();check.IsEnabled=false;install.IsEnabled=false;previews.IsEnabled=false;cancel.Visibility=Visibility.Visible;progress.Visibility=Visibility.Visible;progress.IsIndeterminate=true;}
    void End(){cancellation?.Dispose();cancellation=null;check.IsEnabled=true;previews.IsEnabled=true;cancel.Visibility=Visibility.Collapsed;progress.Visibility=Visibility.Collapsed;install.IsEnabled=prepared!=null||release is {Download:not null,Sha256:not null}&&ReleaseVersion.Parse(release.Tag).CompareTo(ReleaseVersion.Parse(BuildInfo.DisplayVersion))>0;}
    async Task Check(){
        if(Working)return;release=null;prepared=null;notes.Text="";install.Content=L.T("下载并安装");Begin();status.Text=L.T("正在读取 GitHub Releases…");
        try{
            release=await updates.Check(previews.IsChecked==true,cancellation!.Token);
            if(release==null){status.Text=L.T("此渠道暂未发布版本。");return;}
            page.NavigateUri=release.Page;notes.Text=UpdatePresentation.Notes(release,BuildInfo.DisplayVersion);
            bool newer=ReleaseVersion.Parse(release.Tag).CompareTo(ReleaseVersion.Parse(BuildInfo.DisplayVersion))>0;
            status.Text=newer?L.F("发现新版本 {0}", release.Tag)+(release.Size>0?$" ({release.Size/1048576d:F1} MiB)":""):L.T("当前已是此渠道的最新版本。");
            if(newer&&(release.Download==null||release.Sha256==null))status.Text+=L.T(" 此版本缺少对应架构的包或 SHA-256，请打开发布页手动更新。");
        }catch(OperationCanceledException){status.Text=L.T("检查已取消或请求超时，可重试。");}catch(Exception ex){status.Text=L.T("检查失败：")+ex.Message;}
        finally{End();}
    }
    async Task InstallClicked(){
        if(Working||release==null)return;
        if(prepared!=null){TryInstall();return;}
        Begin();string? session=null;
        try{
            UpdateInstaller.CheckWritable();session=UpdateInstaller.CreateSession();string zip=Path.Combine(session,"release.zip");
            progress.IsIndeterminate=false;progress.Value=0;status.Text=L.T("正在下载并校验更新…");
            progress.IsIndeterminate=release.Size==0;
            await updates.Download(release,zip,new Progress<double>(v=>{if(Working){progress.IsIndeterminate=false;progress.Value=v*100;}}),cancellation!.Token);
            progress.IsIndeterminate=true;status.Text=L.T("校验通过，正在准备安装文件…");
            prepared=await UpdateInstaller.Prepare(zip,session,AppContext.BaseDirectory,release.Tag,cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            File.Delete(zip);install.Content=L.T("立即安装并重启");status.Text=L.T("更新已准备好。");
            TryInstall();
        }catch(OperationCanceledException){status.Text=L.T("下载已取消或超时，可重试。");}catch(Exception ex){status.Text=L.T("更新未完成：")+ex.Message;}
        finally{
            if(prepared==null&&session!=null)CleanupIncomplete(session);
            End();
        }
    }
    void TryInstall(){
        try{if(Install?.Invoke(prepared!)!=true)status.Text=L.T("更新已准备好，请修正未保存的设置或等待当前操作结束，再点击安装。");}
        catch(Exception ex){status.Text=L.T("无法开始安装：")+ex.Message;}
    }
    static void CleanupIncomplete(string session){
        try{
            string root=ScreenMover.UpdateCore.UpdateEngine.Root(UpdateInstaller.UpdatesFolder),path=ScreenMover.UpdateCore.UpdateEngine.Root(session);
            if(path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&!File.Exists(Path.Combine(path,"plan.xml")))Directory.Delete(path,true);
        }catch{}
    }
}
