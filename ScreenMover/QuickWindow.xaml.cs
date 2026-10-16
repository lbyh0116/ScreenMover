using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using ScreenMover.Models;
using ScreenMover.Services;
using Windows.Foundation;
using Windows.System;
using static ScreenMover.Interop.Native;
namespace ScreenMover;
public sealed partial class QuickWindow : Window {
    readonly MainWindow owner;
    List<WindowInfo> windows=[];
    readonly Dictionary<string,Border> targets=[];
    DisplayInfo? source,hover,anchor;
    WindowInfo? pressed;Point start;bool dragging,loading,acting;
    int generation;
    public bool IsPanelVisible {get;private set;}
    public QuickWindow(MainWindow parent){
        owner=parent;InitializeComponent();
        var presenter=OverlappedPresenter.Create();
        presenter.SetBorderAndTitleBar(false,false);presenter.IsResizable=false;presenter.IsMaximizable=false;presenter.IsMinimizable=false;presenter.IsAlwaysOnTop=true;
        AppWindow.SetPresenter(presenter);AppWindow.IsShownInSwitchers=false;
        Activated+=(_,e)=>{if(e.WindowActivationState==WindowActivationState.Deactivated&&!acting){Cancel();Dismiss();}};
        AppWindow.Closing+=(_,e)=>{if(!owner.Exiting){e.Cancel=true;Dismiss();}};
        QuickRoot.AddHandler(UIElement.PointerPressedEvent,new PointerEventHandler(Pressed),true);
        QuickRoot.AddHandler(UIElement.PointerMovedEvent,new PointerEventHandler(Moved),true);
        QuickRoot.AddHandler(UIElement.PointerReleasedEvent,new PointerEventHandler(Released),true);
        QuickRoot.AddHandler(UIElement.PointerCanceledEvent,new PointerEventHandler(Canceled),true);
        QuickRoot.AddHandler(UIElement.PointerCaptureLostEvent,new PointerEventHandler(Canceled),true);
        var escape=new KeyboardAccelerator{Key=VirtualKey.Escape};escape.Invoked+=(_,e)=>{Cancel();Dismiss();e.Handled=true;};QuickRoot.KeyboardAccelerators.Add(escape);
        var refresh=new KeyboardAccelerator{Key=VirtualKey.F5};refresh.Invoked+=async(_,e)=>{e.Handled=true;await Reload();};QuickRoot.KeyboardAccelerators.Add(refresh);
    }
    public async Task ShowPanel(){
        TrayIcon.GetCursorPos(out var cursor);
        var display=new DisplayService().Read().Displays.FirstOrDefault(d=>d.Active&&cursor.X>=d.Bounds.Left&&cursor.X<d.Bounds.Right&&cursor.Y>=d.Bounds.Top&&cursor.Y<d.Bounds.Bottom)
            ??new DisplayService().Read().Displays.First(d=>d.Active);
        source=display;anchor=display;
        // Move first so GetDpiForWindow reflects the destination monitor.
        AppWindow.Move(new(display.Work.Left+20,display.Work.Top+20));
        double scale=Math.Max(1,TrayIcon.GetDpiForWindow(WinRT.Interop.WindowNative.GetWindowHandle(this))/96.0);
        int width=Math.Min((int)(720*scale),display.Work.Width-24),height=Math.Min((int)(650*scale),display.Work.Height-24);
        AppWindow.MoveAndResize(new(display.Work.Right-width-12,display.Work.Bottom-height-12,width,height));
        QuickRoot.RequestedTheme=owner.CurrentSettings.Theme switch{"Light"=>ElementTheme.Light,"Dark"=>ElementTheme.Dark,_=>ElementTheme.Default};
        IsPanelVisible=true;AppWindow.Show();Activate();await Reload();
    }
    public void Dismiss(){if(acting)return;IsPanelVisible=false;generation++;AppWindow.Hide();}
    async Task Reload(){
        if(loading||acting||pressed!=null)return;loading=true;int request=++generation;
        try{
            var data=await owner.QuickData();
            if(!IsPanelVisible||request!=generation)return;
            windows=data.Windows;
            var selectedKey=source?.Key;
            targets.Clear();Screens.Children.Clear();SourceScreen.Items.Clear();
            foreach(var d in data.Displays.Where(d=>d.Active)){
                SourceScreen.Items.Add(new ComboBoxItem{Content=$"屏幕 {d.Number} · {d.Name}",Tag=d});
                var image=new Image{Height=92,Stretch=Stretch.Uniform,Source=data.Previews.GetValueOrDefault(d.Key)};
                var stack=new StackPanel{Spacing=4};
                stack.Children.Add(image);stack.Children.Add(new TextBlock{Text=$"屏幕 {d.Number} · {d.Name}",TextTrimming=TextTrimming.CharacterEllipsis,FontSize=12});
                stack.Children.Add(new TextBlock{Text=d.Key==selectedKey?"所在屏幕 · 拖入隐藏":"拖入移动",FontSize=11,Opacity=.7});
                var border=new Border{Child=stack,Padding=new(8),CornerRadius=new(8),BorderThickness=new(2),BorderBrush=new SolidColorBrush(Colors.Gray),Tag=d};
                ToolTipService.SetToolTip(border,d.Details);targets[d.Key]=border;Screens.Children.Add(border);
            }
            SourceScreen.SelectedItem=SourceScreen.Items.OfType<ComboBoxItem>().FirstOrDefault(x=>(x.Tag as DisplayInfo)?.Key==selectedKey)??SourceScreen.Items.FirstOrDefault();
            SetWindows();
        }catch(Exception ex){QuickStatus.Text="刷新失败："+ex.Message;}
        finally{loading=false;}
    }
    void SourceChanged(object sender,SelectionChangedEventArgs e){
        if((SourceScreen.SelectedItem as ComboBoxItem)?.Tag is DisplayInfo d)source=d;
        SetWindows();
        foreach(var b in targets.Values)if(b.Child is StackPanel stack&&stack.Children.Last() is TextBlock hint)hint.Text=(b.Tag as DisplayInfo)?.Gdi==source?.Gdi?"所在屏幕 · 拖入隐藏":"拖入移动";
    }
    void SetWindows(){
        var items=windows.Where(w=>w.Gdi==source?.Gdi).ToList();QuickList.ItemsSource=items;
        QuickEmpty.Visibility=items.Count==0?Visibility.Visible:Visibility.Collapsed;
        if(anchor!=null&&IsPanelVisible){
            double scale=Math.Max(1,TrayIcon.GetDpiForWindow(WinRT.Interop.WindowNative.GetWindowHandle(this))/96.0);
            int width=Math.Min((int)(720*scale),anchor.Work.Width-24);
            int height=Math.Min((int)(Math.Clamp(220+Math.Max(items.Count*48,targets.Count*152),360,650)*scale),anchor.Work.Height-24);
            AppWindow.MoveAndResize(new(anchor.Work.Right-width-12,anchor.Work.Bottom-height-12,width,height));
        }
    }
    void SelectionChanged(object sender,SelectionChangedEventArgs e){foreach(var w in QuickList.Items.OfType<WindowInfo>())w.SetSelected(ReferenceEquals(QuickList.SelectedItem,w));}
    static WindowInfo? Find(DependencyObject? node){
        while(node!=null){if(node is FrameworkElement f&&f.DataContext is WindowInfo w)return w;node=VisualTreeHelper.GetParent(node);}return null;
    }
    void Pressed(object sender,PointerRoutedEventArgs e){
        if(acting||!e.GetCurrentPoint(QuickRoot).Properties.IsLeftButtonPressed)return;
        pressed=Find(e.OriginalSource as DependencyObject);if(pressed==null)return;
        start=e.GetCurrentPoint(QuickRoot).Position;QuickList.SelectedItem=pressed;
        if(!QuickRoot.CapturePointer(e.Pointer))pressed=null;
    }
    DisplayInfo? Hit(Point point){
        var clip=ScreenScroll.TransformToVisual(QuickRoot).TransformBounds(new(0,0,ScreenScroll.ActualWidth,ScreenScroll.ActualHeight));
        if(!clip.Contains(point))return null;
        foreach(var b in targets.Values){
            var rect=b.TransformToVisual(QuickRoot).TransformBounds(new(0,0,b.ActualWidth,b.ActualHeight));
            if(rect.Contains(point))return b.Tag as DisplayInfo;
        }
        return null;
    }
    void Moved(object sender,PointerRoutedEventArgs e){
        if(pressed==null)return;var p=e.GetCurrentPoint(QuickRoot).Position;
        if(!dragging&&Math.Abs(p.X-start.X)+Math.Abs(p.Y-start.Y)<6)return;
        dragging=true;hover=Hit(p);
        QuickGhost.Visibility=Visibility.Visible;
        QuickGhostText.Text=hover==null?"拖到右侧屏幕":hover.Gdi==pressed.Gdi?"隐藏到新桌面":$"移到屏幕 {hover.Number}";
        QuickStatus.Text=hover?.Gdi==pressed.Gdi?"松开将创建虚拟桌面并隐藏此窗口，可从「已隐藏 / 找回」恢复":"拖到其他屏幕移动；拖到所在屏幕隐藏";
        var pos=QuickRoot.TransformToVisual(QuickDragLayer).TransformPoint(p);
        Canvas.SetLeft(QuickGhost,Math.Clamp(pos.X-80,0,Math.Max(0,QuickRoot.ActualWidth-220)));Canvas.SetTop(QuickGhost,Math.Max(0,pos.Y-45));
        foreach(var b in targets.Values)b.BorderBrush=new SolidColorBrush((b.Tag as DisplayInfo)?.Key==hover?.Key?(Windows.UI.Color)Application.Current.Resources["SystemAccentColor"]:Colors.Gray);
        var area=ScreenScroll.TransformToVisual(QuickRoot).TransformBounds(new(0,0,ScreenScroll.ActualWidth,ScreenScroll.ActualHeight));
        if(area.Contains(p)){double dy=p.Y<area.Top+24?-12:p.Y>area.Bottom-24?12:0;if(dy!=0)ScreenScroll.ChangeView(null,Math.Clamp(ScreenScroll.VerticalOffset+dy,0,ScreenScroll.ScrollableHeight),null,true);}
        e.Handled=true;
    }
    async void Released(object sender,PointerRoutedEventArgs e){
        if(pressed==null)return;
        var item=pressed;var target=dragging?Hit(e.GetCurrentPoint(QuickRoot).Position):null;Cancel();
        if(target==null)return;
        acting=true;SourceScreen.IsEnabled=false;QuickStatus.Text="正在操作…";
        try{QuickStatus.Text=await owner.QuickDrop(item,target);}
        catch(Exception ex){QuickStatus.Text="操作失败："+ex.Message;}
        finally{acting=false;SourceScreen.IsEnabled=true;await Reload();}
        e.Handled=true;
    }
    void Canceled(object sender,PointerRoutedEventArgs e)=>Cancel();
    void Cancel(){pressed=null;dragging=false;hover=null;QuickGhost.Visibility=Visibility.Collapsed;QuickRoot.ReleasePointerCaptures();foreach(var b in targets.Values)b.BorderBrush=new SolidColorBrush(Colors.Gray);}
    void DismissClicked(object sender,RoutedEventArgs e)=>Dismiss();
    void MainClicked(object sender,RoutedEventArgs e){Dismiss();owner.ShowMain();}
    void HiddenClicked(object sender,RoutedEventArgs e){Dismiss();owner.ShowHidden();}
    void QuitClicked(object sender,RoutedEventArgs e)=>owner.ExitApp();
}
