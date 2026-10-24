using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ScreenMover.Services;
namespace ScreenMover;

public sealed partial class MainWindow {
    Action? settingsReturn,stopSettings,settingsFlush;
    void SettingsBackClicked(object sender,RoutedEventArgs e)=>settingsReturn?.Invoke();
    async void SettingsClicked(object sender,RoutedEventArgs e){
        if(dialogOpen||busy)return;dialogOpen=true;
        var debounce=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(450)};
        Views.SettingsEditor? editor=null;Action<string>? statusChanged=null;
        try{
            var startupState=StartupService.ReadState(out var startupError);bool wasStartup=startupState==true;
            editor=new(settings,startupState,startupError);
            statusChanged=text=>{SettingsState.Text=text;ToolTipService.SetToolTip(SettingsState,text);};editor.StatusChanged+=statusChanged;
            SettingsState.Text="";
            bool Save(){
                debounce.Stop();if(closed)return true;
                try{
                    var next=editor.Read(settings);var previous=settings;
                    if(next!=settings||wasStartup!=editor.StartupWanted){
                        SettingsCommit.Write(settings,next,EnsureGlobalHotkey().Binding,EnsureHideHotkey().Binding,
                            PlatformSupport.Current.CanHide,wasStartup,editor.StartupWanted,
                            value=>LocalData.Write(AppSettings.FilePath,value),StartupService.SetEnabled);
                        settings=next;wasStartup=editor.StartupWanted;ApplySettings(previous);
                        if(previous.ShowInactive!=next.ShowInactive){displaySignature="";refreshRequested=true;}
                        if(next.ScreenPreviews&&!previous.ScreenPreviews||next.WindowPreviews&&!previous.WindowPreviews)refreshRequested=true;
                    }
                    editor.SetStatus("");return true;
                }catch(Exception ex){editor.SetStatus(ex is AggregateException?"保存未能完整回退："+ex.Message:"未保存："+ex.Message+"；已保留上次有效设置");return false;}
            }
            editor.Export=async()=>{if(!Save())return "请先修正无效设置";return await ExportSettings()?"已导出设置":"已取消";};
            editor.Diagnose=async()=>await ExportDiagnostics()?"诊断包已生成":"已取消";
            editor.Import=async()=>{var path=await OpenSettingsPath();if(path==null)return "已取消";editor.SetValues(await SettingsProfile.Read(path));return Save()?"已导入并自动保存":"导入值未保存，请检查提示";};
            editor.InstallUpdate=prepared=>{if(busy||!Save())return false;UpdateInstaller.Start(prepared);ExitApp();return true;};
            var completion=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            EventHandler changed=(_,_)=>{debounce.Stop();debounce.Start();};editor.Changed+=changed;
            debounce.Tick+=(_,_)=>{if(busy||editor.Working)return;Save();};
            settingsFlush=()=>Save();settingsReturn=()=>{if(!editor.Working)completion.TrySetResult(Save());};
            stopSettings=()=>{debounce.Stop();editor.Changed-=changed;completion.TrySetResult(false);};
            SettingsScroll.Content=editor;SettingsScroll.ChangeView(null,0,null,true);
            Root.Visibility=Visibility.Collapsed;SettingsPage.Visibility=Visibility.Visible;Views.Motion.Enter(SettingsPage,0,10);SettingsBack.Focus(FocusState.Programmatic);
            bool saved=await completion.Task;
            if(!saved&&!closed)Notice("部分设置无效，已保留上次有效值");
        }finally{
            debounce.Stop();stopSettings?.Invoke();settingsReturn=settingsFlush=stopSettings=null;
            if(editor!=null){editor.StatusChanged-=statusChanged;editor.Export=editor.Import=editor.Diagnose=null;editor.InstallUpdate=null;}
            if(!closed){SettingsState.Text="";SettingsScroll.Content=null;SettingsPage.Visibility=Visibility.Collapsed;Root.Visibility=Visibility.Visible;dialogOpen=false;SettingsButton.Focus(FocusState.Programmatic);}
        }
    }
    void ApplySettings(AppSettings? previous=null){
        cursorHost.RequestedTheme=settings.Theme switch{"Light"=>ElementTheme.Light,"Dark"=>ElementTheme.Dark,_=>ElementTheme.Default};
        Views.Appearance.Apply(settings.AccentColor,appearanceSettings,cursorHost.ActualTheme);UpdateCaptionTheme();
        SetLayout();ConfigureTray();ConfigureHotkey();ConfigureHideHotkey();
        if(previous==null||previous.IconFallback!=settings.IconFallback)
            foreach(var w in allWindows.Concat(WindowsList.Items.OfType<Models.WindowInfo>()))w.SetIcon(iconCache.GetValueOrDefault(w.OrderKey),settings.IconFallback);
        if(previous==null||previous.WindowPreviews!=settings.WindowPreviews||previous.ScreenPreviews!=settings.ScreenPreviews)previewCancellation?.Cancel();
        if(!settings.WindowPreviews){windowPreviews.Clear();foreach(var w in allWindows.Concat(WindowsList.Items.OfType<Models.WindowInfo>()))w.SetPreview(null,"窗口预览已关闭");}
        if(!settings.ScreenPreviews)foreach(var view in monitorPreviews.Values){view.image.Source=null;view.label.Text="屏幕预览已关闭";}
        windowRevision.Invalidate();UpdateWindows();
    }
}
