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
        var themes=new[]{"System","Light","Dark"};
        var theme=Choice("外观",["跟随系统","浅色","深色"],Array.IndexOf(themes,settings.Theme));
        var trayEnabled=Toggle("在任务栏通知区域显示快捷操作图标",settings.TrayEnabled);
        var closeToTray=Toggle("关闭主窗口时保留托盘运行",settings.CloseToTray);
        var hotkeyBox=HotkeyCapture(settings.QuickHotkey);panel.Children.Add(hotkeyBox);
        var clearHotkey=new Button{Content="清除快捷键"};clearHotkey.Click+=(_,_)=>hotkeyBox.Text="";panel.Children.Add(clearHotkey);
        var startupState=StartupService.ReadState(out var startupError);bool startupWasEnabled=startupState==true;
        var startup=Toggle("登录 Windows 后启动到托盘",startupWasEnabled);
        startup.IsEnabled=startupState!=null;
        if(startupError!=null)panel.Children.Add(new TextBlock{Text=startupError,TextWrapping=TextWrapping.Wrap,MaxWidth=430});
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
            var d=new AppSettings();theme.SelectedIndex=0;startup.IsOn=false;hotkeyBox.Text="";
            trayEnabled.IsOn=d.TrayEnabled;closeToTray.IsOn=d.CloseToTray;fallback.IsOn=d.IconFallback;inactive.IsOn=d.ShowInactive;remember.IsOn=d.RememberSize;activate.IsOn=d.RefreshOnActivate;
            interval.Value=d.RefreshSeconds;screen.IsOn=d.ScreenPreviews;windows.IsOn=d.WindowPreviews;
            margin.Value=d.MoveMargin;shrink.IsOn=d.ShrinkToFit;duration.Value=d.NoticeSeconds;
        };
        panel.Children.Add(reset);
        ContentDialog? dialog=null;
        var dataPanel=new StackPanel{Spacing=8};
        var dataRow=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        var export=new Button{Content="导出设置"};var import=new Button{Content="导入设置"};var diagnose=new Button{Content="诊断包"};
        dataRow.Children.Add(export);dataRow.Children.Add(import);dataRow.Children.Add(diagnose);dataPanel.Children.Add(dataRow);
        dataPanel.Children.Add(new TextBlock{Text="导出已保存的偏好；导入后需保存并应用。启动项、窗口尺寸、排序和隐藏记录不随文件迁移。诊断包仅含系统版本、匿名屏幕几何与软件设置。",TextWrapping=TextWrapping.Wrap,MaxWidth=430,Opacity=.7});
        var dataStatus=new TextBlock{TextWrapping=TextWrapping.Wrap,MaxWidth=430};dataPanel.Children.Add(dataStatus);
        bool dataBusy=false;
        async Task DataAction(Func<Task<string>> action){
            if(dataBusy)return;dataBusy=true;export.IsEnabled=import.IsEnabled=diagnose.IsEnabled=false;if(dialog!=null)dialog.IsPrimaryButtonEnabled=false;
            try{dataStatus.Text=await action();}catch(Exception ex){dataStatus.Text="未完成："+ex.Message;}
            finally{dataBusy=false;export.IsEnabled=import.IsEnabled=diagnose.IsEnabled=true;if(dialog!=null)dialog.IsPrimaryButtonEnabled=true;}
        }
        export.Click+=async(_,_)=>await DataAction(async()=>await ExportSettings()?"已导出已保存设置":"已取消");
        diagnose.Click+=async(_,_)=>await DataAction(async()=>await ExportDiagnostics()?"诊断包已生成":"已取消");
        import.Click+=async(_,_)=>await DataAction(async()=>{
            var path=await OpenSettingsPath();if(path==null)return "已取消";
            var loaded=await SettingsProfile.Read(path);
            theme.SelectedIndex=Array.IndexOf(themes,loaded.Theme);trayEnabled.IsOn=loaded.TrayEnabled;closeToTray.IsOn=loaded.CloseToTray;
            hotkeyBox.Text=loaded.QuickHotkey;fallback.IsOn=loaded.IconFallback;inactive.IsOn=loaded.ShowInactive;remember.IsOn=loaded.RememberSize;
            activate.IsOn=loaded.RefreshOnActivate;interval.Value=loaded.RefreshSeconds;screen.IsOn=loaded.ScreenPreviews;windows.IsOn=loaded.WindowPreviews;
            margin.Value=loaded.MoveMargin;shrink.IsOn=loaded.ShrinkToFit;duration.Value=loaded.NoticeSeconds;
            return "已载入偏好，保存并应用后生效";
        });
        panel.Children.Add(new Expander{Header="设置备份与诊断",HorizontalAlignment=HorizontalAlignment.Stretch,Content=dataPanel});
        panel.Children.Add(new Expander{Header="操作说明与快捷键",HorizontalAlignment=HorizontalAlignment.Stretch,Content=new TextBlock{
            Text="主界面\n单击选择，Ctrl / Shift 多选，单击空白取消。按住卡片可拖动；拖到卡片之间排序，拖到屏幕移动。悬停屏幕会显示操作预告。双击激活窗口。\n\n快捷面板\n点击托盘图标打开。左边是窗口列表，右边是屏幕；拖到其他屏幕移动，拖到窗口所在屏幕隐藏到独立虚拟桌面。点击外部或按 Esc 收起。右上角按钮打开主界面；在主界面「已隐藏」中找回。\n\n快捷键\nCtrl+A 全选、Ctrl+F 搜索、F5 刷新。可在设置中录入全局组合键，在其他软件中也能打开快捷面板。\n\n截图与常驻\n截图是静态采样，只在内存中。关闭主窗口可继续驻留托盘；右键托盘图标退出。开机启动默认关闭，开启后请保留当前程序文件夹。",
            TextWrapping=TextWrapping.Wrap,MaxWidth=430}});
        var error=new TextBlock{TextWrapping=TextWrapping.Wrap};panel.Children.Add(error);
        dialog=new ContentDialog{XamlRoot=Root.XamlRoot,Title="设置",PrimaryButtonText="保存并应用",CloseButtonText="取消",DefaultButton=ContentDialogButton.Primary,
            Content=new ScrollViewer{Content=panel,MaxHeight=480,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled}};
        dialog.PrimaryButtonClick+=(_,args)=>{
            try{
                if(!double.IsFinite(interval.Value)||!double.IsFinite(margin.Value)||!double.IsFinite(duration.Value))throw new InvalidOperationException("请输入有效数字");
                if(interval.Value>0&&interval.Value<15)throw new InvalidOperationException("定时刷新请设为 0 或至少 15 秒");
                if(startup.IsOn&&!trayEnabled.IsOn)throw new InvalidOperationException("登录后驻留需要开启托盘图标");
                var shortcutName=HotkeySpec.Parse(hotkeyBox.Text)?.Name??"";
                var next=settings with{QuickHotkey=shortcutName,Theme=themes[theme.SelectedIndex],ShowInactive=inactive.IsOn,
                    TrayEnabled=trayEnabled.IsOn,CloseToTray=closeToTray.IsOn,IconFallback=fallback.IsOn,RememberSize=remember.IsOn,RefreshOnActivate=activate.IsOn,RefreshSeconds=(int)interval.Value,
                    ScreenPreviews=screen.IsOn,WindowPreviews=windows.IsOn,MoveMargin=(int)margin.Value,ShrinkToFit=shrink.IsOn,NoticeSeconds=(int)duration.Value};
                next.Normalize();using var shortcut=EnsureGlobalHotkey().Binding.Prepare(next.QuickHotkey);LocalData.Write(AppSettings.FilePath,next);
                try{if(startup.IsOn||startup.IsOn!=startupWasEnabled)StartupService.SetEnabled(startup.IsOn);}
                catch{LocalData.Write(AppSettings.FilePath,settings);throw;}
                shortcut.Commit();settings=next;
            }catch(Exception ex){error.Text="未保存："+ex.Message;args.Cancel=true;}
        };
        try {
            if(await dialog.ShowAsync()==ContentDialogResult.Primary){
                dialogOpen=false;
                ApplySettings();displaySignature="";await RefreshWithPreviews();Notice("设置已保存并应用");
            }
        }finally{dialogOpen=false;}
    }
    void ApplySettings(){
        Root.RequestedTheme=settings.Theme switch{"Light"=>ElementTheme.Light,"Dark"=>ElementTheme.Dark,_=>ElementTheme.Default};
        SetLayout();ConfigureTray();ConfigureHotkey();
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
