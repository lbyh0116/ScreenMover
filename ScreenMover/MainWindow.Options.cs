using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ScreenMover.Models;
using ScreenMover.Services;
namespace ScreenMover;

public sealed partial class MainWindow {
    AppSettings settings=new();
    readonly MoveHistory moves=new();
    readonly HiddenWindows hidden=new();
    bool initialized,dialogOpen;

    void SearchChanged(object sender,TextChangedEventArgs e){if(initialized)UpdateWindows();}
    void ScopeChanged(object sender,RoutedEventArgs e){if(initialized){UpdateWindows();RebuildButtons();}}
    async void UndoClicked(object sender,RoutedEventArgs e) {
        if(busy||moves.Count==0)return;
        busy=true;UpdateStatus();
        try{ShowResults("撤销移动",await Task.Run(moves.Undo));}
        catch(Exception ex){Notice("撤销失败："+ex.Message);}
        finally{busy=false;await Refresh();UpdateStatus();}
    }
    void ShowResults(string verb,IReadOnlyList<OperationResult> results) =>
        Notice($"{verb} {results.Count} 个窗口：成功 {results.Count(r=>r.Success)}，失败 {results.Count(r=>!r.Success)}"+
        string.Concat(results.Select(r=>$"\n{r.Title}：{r.Message}")));
    async Task MoveBatch(List<WindowInfo> items,DisplayInfo target) {
        if(busy||items.Count==0)return;
        busy=true;UpdateStatus();
        try{ShowResults("移动",await Task.Run(()=>moves.Move(items,target,settings.MoveMargin,settings.ShrinkToFit)));}
        catch(Exception ex){Notice("移动失败："+ex.Message);}
        finally{busy=false;await Refresh();UpdateStatus();}
    }
    async void SettingsClicked(object sender,RoutedEventArgs e) {
        if(dialogOpen||busy)return;dialogOpen=true;
        var panel=new StackPanel{Spacing=14,MinWidth=360};
        ComboBox Choice(string title,string[] labels,int index){
            var control=new ComboBox{Header=title,HorizontalAlignment=HorizontalAlignment.Stretch};
            foreach(var label in labels)control.Items.Add(label);control.SelectedIndex=index;panel.Children.Add(control);return control;
        }
        ToggleSwitch Toggle(string title,bool value){
            var control=new ToggleSwitch{Header=title,IsOn=value,OnContent="开启",OffContent="关闭"};panel.Children.Add(control);return control;
        }
        NumberBox Number(string title,double value,double min,double max){
            var control=new NumberBox{Header=title,Value=value,Minimum=min,Maximum=max,SpinButtonPlacementMode=NumberBoxSpinButtonPlacementMode.Compact};
            panel.Children.Add(control);return control;
        }
        var themes=new[]{"System","Light","Dark"};var layouts=new[]{"Auto","List","Gallery"};
        var theme=Choice("外观",["跟随系统","浅色","深色"],Array.IndexOf(themes,settings.Theme));
        var layout=Choice("窗口排列",["根据窗口比例自动切换","始终使用列表","始终使用相册"],Array.IndexOf(layouts,settings.Layout));
        var trayEnabled=Toggle("在任务栏通知区域显示快捷操作图标",settings.TrayEnabled);
        var closeToTray=Toggle("关闭主窗口时保留托盘运行",settings.CloseToTray);
        var fallback=Toggle("没有截图时用软件图标代替",settings.IconFallback);
        var inactive=Toggle("显示未启用的屏幕",settings.ShowInactive);
        var remember=Toggle("记住软件窗口尺寸",settings.RememberSize);
        var activate=Toggle("重新激活 ScreenMover 时自动刷新",settings.RefreshOnActivate);
        var interval=Number("定时刷新间隔（秒；0 表示关闭，最短 15 秒）",settings.RefreshSeconds,0,3600);
        var screen=Toggle("显示屏幕截图",settings.ScreenPreviews);
        var windows=Toggle("显示窗口截图",settings.WindowPreviews);
        panel.Children.Add(new TextBlock{Text="关闭截图会清除当前预览缓存。截图只保留在内存中；F5 仍可刷新列表和已启用的截图。",TextWrapping=TextWrapping.Wrap,Opacity=.7});
        var margin=Number("移动后的屏幕边缘留白（物理像素）",settings.MoveMargin,0,300);
        var shrink=Toggle("窗口过大时按比例缩小至目标工作区",settings.ShrinkToFit);
        var duration=Number("操作提示停留时间（秒）",settings.NoticeSeconds,2,30);
        var reset=new Button{Content="恢复默认设置"};
        reset.Click+=(_,_)=>{
            var d=new AppSettings();theme.SelectedIndex=0;layout.SelectedIndex=0;
            trayEnabled.IsOn=d.TrayEnabled;closeToTray.IsOn=d.CloseToTray;fallback.IsOn=d.IconFallback;inactive.IsOn=d.ShowInactive;remember.IsOn=d.RememberSize;activate.IsOn=d.RefreshOnActivate;
            interval.Value=d.RefreshSeconds;screen.IsOn=d.ScreenPreviews;windows.IsOn=d.WindowPreviews;
            margin.Value=d.MoveMargin;shrink.IsOn=d.ShrinkToFit;duration.Value=d.NoticeSeconds;
        };
        panel.Children.Add(reset);
        var error=new TextBlock{TextWrapping=TextWrapping.Wrap};panel.Children.Add(error);
        var dialog=new ContentDialog{XamlRoot=Root.XamlRoot,Title="设置",PrimaryButtonText="保存并应用",CloseButtonText="取消",DefaultButton=ContentDialogButton.Primary,
            Content=new ScrollViewer{Content=panel,MaxHeight=480,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled}};
        dialog.PrimaryButtonClick+=(_,args)=>{
            try{
                if(!double.IsFinite(interval.Value)||!double.IsFinite(margin.Value)||!double.IsFinite(duration.Value))throw new InvalidOperationException("请输入有效数字");
                if(interval.Value>0&&interval.Value<15)throw new InvalidOperationException("定时刷新请设为 0 或至少 15 秒");
                var next=settings with{Theme=themes[theme.SelectedIndex],Layout=layouts[layout.SelectedIndex],ShowInactive=inactive.IsOn,
                    TrayEnabled=trayEnabled.IsOn,CloseToTray=closeToTray.IsOn,IconFallback=fallback.IsOn,RememberSize=remember.IsOn,RefreshOnActivate=activate.IsOn,RefreshSeconds=(int)interval.Value,
                    ScreenPreviews=screen.IsOn,WindowPreviews=windows.IsOn,MoveMargin=(int)margin.Value,ShrinkToFit=shrink.IsOn,NoticeSeconds=(int)duration.Value};
                next.Normalize();LocalData.Write(AppSettings.FilePath,next);settings=next;
            }catch(Exception ex){error.Text="未保存："+ex.Message;args.Cancel=true;}
        };
        try {
            if(await dialog.ShowAsync()==ContentDialogResult.Primary){
                ApplySettings();displaySignature="";await RefreshWithPreviews();Notice("设置已保存并应用");
            }
        }finally{dialogOpen=false;}
    }
    void ApplySettings(){
        Root.RequestedTheme=settings.Theme switch{"Light"=>ElementTheme.Light,"Dark"=>ElementTheme.Dark,_=>ElementTheme.Default};
        gallery=null;SetLayout();ConfigureTray();
foreach(var w in allWindows.Concat(WindowsList.Items.OfType<WindowInfo>()))w.SetIcon(iconCache.GetValueOrDefault(w.OrderKey),settings.IconFallback);
        previewCancellation?.Cancel();
        if(!settings.WindowPreviews){
            windowPreviews.Clear();
            foreach(var w in allWindows.Concat(WindowsList.Items.OfType<WindowInfo>()))w.SetPreview(null,"窗口预览已关闭");
        }
        if(!settings.ScreenPreviews)foreach(var view in monitorPreviews.Values){view.image.Source=null;view.label.Text="屏幕预览已关闭";}
        windowSignature="";UpdateWindows();
    }
    async void HiddenClicked(object sender,RoutedEventArgs e) {
        if(dialogOpen||busy)return;dialogOpen=true;
        var panel=new StackPanel{Spacing=12,MinWidth=360};
        panel.Children.Add(new TextBlock{Text="选择记录后可找回当前虚拟桌面。原桌面会保留，避免影响其中的其他窗口。记录会在软件重启后保留。",TextWrapping=TextWrapping.Wrap});
        var list=new ListView{SelectionMode=ListViewSelectionMode.Single,MaxHeight=300};
        var detail=new TextBlock{TextWrapping=TextWrapping.Wrap};
        var row=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        var recover=new Button{Content="找回选中窗口",IsEnabled=false};
        var forget=new Button{Content="移除失效记录",IsEnabled=false};
        row.Children.Add(recover);row.Children.Add(forget);panel.Children.Add(list);panel.Children.Add(row);panel.Children.Add(detail);
        void Populate(){
            list.Items.Clear();
            foreach(var entry in hidden.Read().OrderByDescending(x=>x.HiddenAt)){
                var text=new TextBlock{Text=entry.ToString()+(MoveHistory.Alive(entry.Saved)?"":"\n原窗口已关闭 / 进程已重启"),TextWrapping=TextWrapping.Wrap,MaxWidth=430};
                list.Items.Add(new ListViewItem{Content=text,Tag=entry});
            }
            if(list.Items.Count==0)detail.Text="暂无隐藏记录";
            recover.IsEnabled=forget.IsEnabled=false;
            HiddenButton.Content=$"已隐藏 · {hidden.Read().Count}";
        }
        list.SelectionChanged+=(_,_)=>{
            var entry=(list.SelectedItem as ListViewItem)?.Tag as HiddenWindow;
            bool alive=entry!=null&&MoveHistory.Alive(entry.Saved);
            recover.IsEnabled=alive&&!busy;forget.IsEnabled=entry!=null&&!alive&&!busy;
        };
        var dialog=new ContentDialog{XamlRoot=Root.XamlRoot,Title="已隐藏窗口",CloseButtonText="关闭",Content=panel};
        recover.Click+=async(_,_)=>{
            if((list.SelectedItem as ListViewItem)?.Tag is not HiddenWindow entry)return;
            busy=true;UpdateStatus();recover.IsEnabled=forget.IsEnabled=false;dialog.IsEnabled=false;detail.Text="正在找回…";
            try{
                var result=await Task.Run(()=>hidden.Recover(entry));Populate();detail.Text=result.Message;
                Notice($"找回窗口：{entry.Saved.Title} · {result.Message}");
            }catch(Exception ex){detail.Text=ex.Message;}
            finally{busy=false;dialog.IsEnabled=true;UpdateStatus();await Refresh();}
        };
        forget.Click+=(_,_)=>{
            if((list.SelectedItem as ListViewItem)?.Tag is not HiddenWindow entry||MoveHistory.Alive(entry.Saved))return;
            try{hidden.Forget(entry.Id);Populate();Notice("已移除失效记录："+entry.Saved.Title);}
            catch(Exception ex){detail.Text=ex.Message;}
        };
        Populate();
        try{await dialog.ShowAsync();}finally{dialogOpen=false;}
    }
}
