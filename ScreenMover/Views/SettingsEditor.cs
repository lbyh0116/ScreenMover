using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ScreenMover.Services;
namespace ScreenMover.Views;

public sealed class SettingsEditor : UserControl {
    readonly StackPanel body=new();
    UIElementCollection Children=>body.Children;
    readonly ComboBox theme,accentMode;
    readonly ColorPicker accent;
    readonly ToggleSwitch tray,closeToTray,startup,fallback,inactive,remember,activate,screen,windows,shrink;
    readonly NumberBox interval,margin,duration;
    readonly HotkeyPicker quickKey,hideKey;
    public event Action<string>? StatusChanged;
    bool ready;
    public bool Working {get;private set;}
    public bool StartupWanted=>startup.IsOn;
    public event EventHandler? Changed;
    public Func<Task<string>>? Export,Import,Diagnose;
    public SettingsEditor(AppSettings settings,bool? startupState,string? startupError){
        Content=body;body.Spacing=18;MaxWidth=820;HorizontalAlignment=HorizontalAlignment.Stretch;
        Section("个性化");
        theme=Choice("外观",["跟随系统","浅色","深色"]);
        accentMode=Choice("主题色",["跟随系统","自定义颜色"]);
        accent=new(){IsAlphaEnabled=false,IsHexInputVisible=true,IsAlphaSliderVisible=false,IsAlphaTextInputVisible=false,HorizontalAlignment=HorizontalAlignment.Left};
        accent.ColorChanged+=(_,_)=>Signal();Children.Add(accent);
        accentMode.SelectionChanged+=(_,_)=>accent.Visibility=accentMode.SelectedIndex==1?Visibility.Visible:Visibility.Collapsed;
        Section("托盘与启动");tray=Toggle("在任务栏通知区域显示快捷操作图标");closeToTray=Toggle("关闭主窗口时保留托盘运行");
        quickKey=new("全局快捷键：打开 / 收起快捷面板","");Children.Add(quickKey);quickKey.ValueChanged+=(_,_)=>Signal();
        var clearQuick=new Button{Content="清除快捷键"};clearQuick.Click+=(_,_)=>{quickKey.Value="";Signal();};Children.Add(clearQuick);
        hideKey=new("全局快捷键：隐藏当前前台窗口",""){IsEnabled=PlatformSupport.Current.CanHide};Children.Add(hideKey);hideKey.ValueChanged+=(_,_)=>Signal();
        if(!PlatformSupport.Current.CanHide)Children.Add(new TextBlock{Text=PlatformSupport.Current.HideStatus,TextWrapping=TextWrapping.Wrap,Opacity=.7});
        var clearHide=new Button{Content="清除隐藏快捷键"};clearHide.Click+=(_,_)=>{hideKey.Value="";Signal();};Children.Add(clearHide);
        startup=Toggle("登录 Windows 后启动到托盘");startup.IsEnabled=startupState!=null;
        if(startupError!=null)Children.Add(new TextBlock{Text=startupError,TextWrapping=TextWrapping.Wrap});
        tray.Toggled+=(_,_)=>{if(ready&&!tray.IsOn)startup.IsOn=false;};
        startup.Toggled+=(_,_)=>{if(ready&&startup.IsOn)tray.IsOn=true;};
        Section("窗口与截图");fallback=Toggle("没有截图时用软件图标代替");inactive=Toggle("显示未启用的屏幕");remember=Toggle("记住软件窗口尺寸");activate=Toggle("重新激活 ScreenMover 时自动刷新");
        interval=Number("定时刷新间隔（秒；0 关闭，最短 15 秒）",0,3600);
        screen=Toggle("显示屏幕截图");windows=Toggle("显示窗口截图");
        Section("移动与提示");margin=Number("移动后的屏幕边缘留白（物理像素）",0,300);shrink=Toggle("窗口过大时按比例缩小至目标工作区");duration=Number("操作提示停留时间（秒）",2,30);
        var reset=new Button{Content="恢复默认设置"};reset.Click+=(_,_)=>SetValues(new AppSettings(),false);Children.Add(reset);
        Section("数据与帮助");var data=new StackPanel{Spacing=8};var row=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        var dataStatus=new TextBlock{TextWrapping=TextWrapping.Wrap};
        foreach(var (label,run) in new (string,Func<Task<string>>)[]{("导出设置",()=>Export?.Invoke()??Task.FromResult("不可用")),("导入设置",()=>Import?.Invoke()??Task.FromResult("不可用")),("诊断包",()=>Diagnose?.Invoke()??Task.FromResult("不可用"))}){
            var button=new Button{Content=label};button.Click+=async(_,_)=>{
                if(Working)return;Working=true;IsEnabled=false;
                try{dataStatus.Text=await run();}catch(Exception ex){dataStatus.Text="未完成："+ex.Message;}
                finally{Working=false;IsEnabled=true;Signal();}
            };row.Children.Add(button);
        }
        data.Children.Add(row);data.Children.Add(dataStatus);
        data.Children.Add(new TextBlock{Text="设置变化后自动保存。无效数字或快捷键冲突会保留上次有效设置。导入不迁移启动项、设备尺寸、排序或隐藏记录。诊断包不包含窗口标题、截图或原始日志。",TextWrapping=TextWrapping.Wrap,Opacity=.7});
        Children.Add(new Expander{Header="设置备份与诊断",Content=data,HorizontalAlignment=HorizontalAlignment.Stretch});
        Children.Add(new Expander{Header="操作说明与快捷键",HorizontalAlignment=HorizontalAlignment.Stretch,Content=new TextBlock{TextWrapping=TextWrapping.Wrap,Text="主界面：单击选择，Ctrl / Shift 多选，点击空白取消。拖动卡片排序；拖到屏幕移动；双击激活窗口。\n\n快捷面板：点击托盘图标打开或收起。拖到其他屏幕移动，拖到当前屏幕隐藏（仅已适配系统）；按 Esc 或点击外部收起。已隐藏的窗口可在主界面找回。\n\n快捷键：支持按键录入，也可点输入栏右侧箭头选择预设或组合；通过清除按钮停用。隐藏前台窗口默认为关闭。\n\n截图：静态采样，仅保存在内存中；启动、重新激活、定时或 F5 时刷新。最多保留 64 个窗口缩略图，超额时优先当前屏幕，其余使用图标。\n\n离开设置页前会保存有效变更。右键托盘图标可彻底退出。"}});
        var about=new StackPanel{Spacing=8};
        about.Children.Add(new TextBlock{Text=$"ScreenMover {BuildInfo.DisplayVersion}\n{PlatformSupport.Current.Description}\n{PlatformSupport.Current.HideStatus}",TextWrapping=TextWrapping.Wrap});
        about.Children.Add(new TextBlock{Text="本地处理窗口与截图，不上传画面，不启用遥测。应用图标采用桌面收纳方案。",TextWrapping=TextWrapping.Wrap,Opacity=.7});
        about.Children.Add(new HyperlinkButton{Content="版本与下载",NavigateUri=new Uri("https://github.com/lbyh0116/ScreenMover/releases")});
        about.Children.Add(new HyperlinkButton{Content="反馈问题",NavigateUri=new Uri("https://github.com/lbyh0116/ScreenMover/issues/new/choose")});
        var folder=new Button{Content="打开数据与完整操作记录"};folder.Click+=(_,_)=>{try{Directory.CreateDirectory(LocalData.Folder);System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(LocalData.Folder){UseShellExecute=true});}catch(Exception ex){SetStatus("无法打开："+ex.Message);}};about.Children.Add(folder);
        Children.Add(new Expander{Header="关于与隐私",Content=about,HorizontalAlignment=HorizontalAlignment.Stretch});
        SetValues(settings,startupState==true);ready=true;SetStatus("设置变化后自动保存");
    }
    void Section(string text)=>Children.Add(new TextBlock{Text=text,FontSize=20,FontWeight=Microsoft.UI.Text.FontWeights.SemiBold,Margin=new Thickness(0,12,0,0)});
    ComboBox Choice(string title,string[] labels){var c=new ComboBox{Header=title,HorizontalAlignment=HorizontalAlignment.Stretch};foreach(var label in labels)c.Items.Add(label);c.SelectionChanged+=(_,_)=>Signal();Children.Add(c);return c;}
    ToggleSwitch Toggle(string title){var c=new ToggleSwitch{Header=title,OnContent="开启",OffContent="关闭"};c.Toggled+=(_,_)=>Signal();Children.Add(c);return c;}
    NumberBox Number(string title,int min,int max){var c=new NumberBox{Header=title,Minimum=min,Maximum=max,SpinButtonPlacementMode=NumberBoxSpinButtonPlacementMode.Compact};c.ValueChanged+=(_,_)=>Signal();Children.Add(c);return c;}
    void Signal(){if(ready)Changed?.Invoke(this,EventArgs.Empty);}
    public void SetStatus(string text)=>StatusChanged?.Invoke(text);
    public void SetValues(AppSettings value,bool? startupValue=null){
        bool wasReady=ready;ready=false;
        try{
            theme.SelectedIndex=value.Theme switch{"Light"=>1,"Dark"=>2,_=>0};accentMode.SelectedIndex=value.AccentColor==""?0:1;accent.Color=Appearance.Parse(value.AccentColor==""?"#0078D4":value.AccentColor);
            tray.IsOn=value.TrayEnabled;closeToTray.IsOn=value.CloseToTray;quickKey.Value=value.QuickHotkey;hideKey.Value=value.HideHotkey;
            if(startupValue!=null)startup.IsOn=startupValue.Value;
            fallback.IsOn=value.IconFallback;inactive.IsOn=value.ShowInactive;remember.IsOn=value.RememberSize;activate.IsOn=value.RefreshOnActivate;interval.Value=value.RefreshSeconds;screen.IsOn=value.ScreenPreviews;windows.IsOn=value.WindowPreviews;margin.Value=value.MoveMargin;shrink.IsOn=value.ShrinkToFit;duration.Value=value.NoticeSeconds;
        }finally{ready=wasReady;}
        Signal();
    }
    public AppSettings Read(AppSettings baseline){
        quickKey.FlushComposition();hideKey.FlushComposition();
        if(!double.IsFinite(interval.Value)||!double.IsFinite(margin.Value)||!double.IsFinite(duration.Value))throw new InvalidOperationException("请输入有效数字");
        if(interval.Value>0&&interval.Value<15)throw new InvalidOperationException("定时刷新请设为 0 或至少 15 秒");
        if(startup.IsOn&&!tray.IsOn)throw new InvalidOperationException("登录后驻留需要开启托盘图标");
        if(quickKey.Value!=""&&quickKey.Value==hideKey.Value)throw new InvalidOperationException("两个操作必须使用不同的快捷键");
        var next=baseline with{Theme=theme.SelectedIndex switch{1=>"Light",2=>"Dark",_=>"System"},AccentColor=accentMode.SelectedIndex==0?"":$"#{accent.Color.R:X2}{accent.Color.G:X2}{accent.Color.B:X2}",QuickHotkey=quickKey.Value,HideHotkey=hideKey.Value,TrayEnabled=tray.IsOn,CloseToTray=closeToTray.IsOn,IconFallback=fallback.IsOn,ShowInactive=inactive.IsOn,RememberSize=remember.IsOn,RefreshOnActivate=activate.IsOn,RefreshSeconds=(int)interval.Value,ScreenPreviews=screen.IsOn,WindowPreviews=windows.IsOn,MoveMargin=(int)margin.Value,ShrinkToFit=shrink.IsOn,NoticeSeconds=(int)duration.Value};next.Normalize();return next;
    }
}
