using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using ScreenMover.Models;
using ScreenMover.Interop;
using ScreenMover.Views;
using ScreenMover.Services;
using Windows.Foundation;
using Windows.System;
using static ScreenMover.Interop.Native;
namespace ScreenMover;
public sealed partial class QuickWindow : Window {
    readonly MainWindow owner;
    List<WindowInfo> windows=[];
    readonly Dictionary<string,SelectionSurface> targets=[];
    readonly DragFeedback dragFeedback;
    readonly FlyoutChrome frame;
    FlyoutDismiss? outside;
    int noticeVersion;
    DisplayInfo? source,hover,anchor;
    WindowInfo? pressed;Point start;bool dragging,loading,acting,panelActive,pendingDismiss;
    int generation,visibilityVersion;
    readonly TrayClickGate trayClick=new();
    RECT panelAnchor;
    SurfaceLayout.Edge edge;
    public bool ConsumeTrayDismiss()=>trayClick.Consume(Environment.TickCount64);
    public bool IsPanelVisible {get;private set;}
    public QuickWindow(MainWindow parent){
        owner=parent;InitializeComponent();dragFeedback=new(QuickRoot,QuickDragLayer,QuickGhost,QuickGhostImage,QuickGhostCount);
        var presenter=OverlappedPresenter.Create();
        presenter.SetBorderAndTitleBar(false,false);presenter.IsResizable=false;presenter.IsMaximizable=false;presenter.IsMinimizable=false;presenter.IsAlwaysOnTop=true;
        AppWindow.SetPresenter(presenter);AppWindow.IsShownInSwitchers=false;
        frame=new(WinRT.Interop.WindowNative.GetWindowHandle(this));
        PanelSurface.ActualThemeChanged+=(_,_)=>{if(owner.Exiting)return;frame.Apply(PanelSurface.ActualTheme==ElementTheme.Dark);WindowMaterial.Apply(this,QuickRoot);};
        Activated+=(_,e)=>{panelActive=e.WindowActivationState!=WindowActivationState.Deactivated;if(!panelActive&&!acting&&IsPanelVisible){if(TrayIcon.GetCursorPos(out var point))RememberTrayClick(point);Cancel();Dismiss();}};
        AppWindow.Closing+=(_,e)=>{if(!owner.Exiting){e.Cancel=true;Dismiss();}};
        Closed+=(_,_)=>{frame.Dispose();outside?.Dispose();IsPanelVisible=false;generation++;visibilityVersion++;};
        QuickRoot.AddHandler(UIElement.PointerPressedEvent,new PointerEventHandler(Pressed),true);
        QuickRoot.AddHandler(UIElement.PointerMovedEvent,new PointerEventHandler(Moved),true);
        QuickRoot.AddHandler(UIElement.PointerReleasedEvent,new PointerEventHandler(Released),true);
        QuickRoot.AddHandler(UIElement.PointerCanceledEvent,new PointerEventHandler(Canceled),true);
        QuickRoot.AddHandler(UIElement.PointerCaptureLostEvent,new PointerEventHandler(CaptureLost),true);
        var escape=new KeyboardAccelerator{Key=VirtualKey.Escape};escape.Invoked+=(_,e)=>{Cancel();Dismiss();e.Handled=true;};QuickRoot.KeyboardAccelerators.Add(escape);
        var refresh=new KeyboardAccelerator{Key=VirtualKey.F5};refresh.Invoked+=async(_,e)=>{e.Handled=true;await Reload(true);};QuickRoot.KeyboardAccelerators.Add(refresh);
    }
    public async Task ShowPanel(bool fromTray=false){
        trayClick.Reset();
        var displays=new DisplayService().Read().Displays.Where(d=>d.Active).ToList();
        if(displays.Count==0)return;
        var hwnd=WinRT.Interop.WindowNative.GetWindowHandle(owner);
        bool hasTray=fromTray&&TrayIcon.TryGetBounds(hwnd,out panelAnchor);
        TrayIcon.GetCursorPos(out var cursor);
        if(!hasTray)panelAnchor=new(){Left=cursor.X,Top=cursor.Y,Right=cursor.X+1,Bottom=cursor.Y+1};
        var point=new POINT{X=panelAnchor.Left+panelAnchor.Width/2,Y=panelAnchor.Top+panelAnchor.Height/2};
        var display=displays.FirstOrDefault(d=>point.X>=d.Bounds.Left&&point.X<d.Bounds.Right&&point.Y>=d.Bounds.Top&&point.Y<d.Bounds.Bottom)
            ??displays.FirstOrDefault(d=>d.Primary)??displays[0];
        if(!hasTray)panelAnchor=new(){Left=display.Bounds.Right-1,Right=display.Bounds.Right,Top=display.Bounds.Bottom-1,Bottom=display.Bounds.Bottom};
        source=display;anchor=display;
        PanelSurface.RequestedTheme=owner.CurrentSettings.Theme switch{"Light"=>ElementTheme.Light,"Dark"=>ElementTheme.Dark,_=>ElementTheme.Default};
        QuickRoot.RequestedTheme=PanelSurface.RequestedTheme;WindowMaterial.Apply(this,QuickRoot);
        Motion.Reset(PanelSurface);
        pendingDismiss=false;
        PanelSurface.IsHitTestVisible=true;
        int version=++visibilityVersion;
        IsPanelVisible=true;
        // Populate and size while hidden: no intermediate 650px window or second resize on screen.
        while(loading){await Task.Delay(16);if(version!=visibilityVersion)return;}
        await Reload();
        if(!IsPanelVisible||version!=visibilityVersion)return;
        PlacePanel();QuickNotice.Visibility=Visibility.Collapsed;QuickList.SelectedItem=null;
        var offset=EnterOffset();Motion.PrepareSlide(PanelSurface,offset.X,offset.Y);
        AppWindow.Show();Activate();frame.Apply(PanelSurface.ActualTheme==ElementTheme.Dark);SetForegroundWindow(WinRT.Interop.WindowNative.GetWindowHandle(this));
        outside?.Dispose();outside=new FlyoutDismiss(WinRT.Interop.WindowNative.GetWindowHandle(this),point=>DispatcherQueue.TryEnqueue(()=>{
            RememberTrayClick(point);
            if(!IsPanelVisible)return;if(acting)pendingDismiss=true;else Dismiss();
        }));
        Motion.SlideIn(PanelSurface,offset.X,offset.Y);
    }
    void RememberTrayClick(POINT point){
        if(TrayIcon.TryGetBounds(WinRT.Interop.WindowNative.GetWindowHandle(owner),out var rect)&&point.X>=rect.Left&&point.X<rect.Right&&point.Y>=rect.Top&&point.Y<rect.Bottom)trayClick.DismissFromTray(Environment.TickCount64);
    }
    (float X,float Y) EnterOffset()=>edge switch{SurfaceLayout.Edge.Top=>(0,-48),SurfaceLayout.Edge.Left=>(-48,0),SurfaceLayout.Edge.Right=>(48,0),_=>(0,48)};
    public async void Dismiss(){
        if(acting||!IsPanelVisible)return;
        IsPanelVisible=false;generation++;int version=++visibilityVersion;outside?.Dispose();outside=null;
        PanelSurface.IsHitTestVisible=false;
        Cancel();var offset=EnterOffset();
        await Motion.Exit(PanelSurface,offset.X/2,offset.Y/2);
        if(version!=visibilityVersion||IsPanelVisible)return;
        AppWindow.Hide();QuickList.ItemsSource=null;windows=[];Screens.Children.Clear();targets.Clear();QuickStatus.Text="";Motion.Reset(PanelSurface);
    }
    void PlacePanel(){
        if(anchor==null)return;
        double scale=WindowChrome.ScaleAt(new(){X=anchor.Work.Left+10,Y=anchor.Work.Top+10});
        var textWidths=QuickList.Items.OfType<WindowInfo>().Select(w=>{
            var label=new TextBlock{Text=w.AppName,FontSize=14};label.Measure(new(double.PositiveInfinity,double.PositiveInfinity));return label.DesiredSize.Width;
        });
        double column=SurfaceLayout.ApplicationColumn(textWidths);
        double maxRatio=targets.Values.Select(b=>SurfaceLayout.Ratio(((DisplayInfo)b.Tag).Bounds)).DefaultIfEmpty(16.0/9).Max();
        var placement=SurfaceLayout.Flyout(anchor.Bounds,anchor.Work,panelAnchor,scale,QuickList.Items.Count,targets.Count,maxRatio,column);
        edge=placement.Edge;var rect=placement.Bounds;
        double requestedTargets=Math.Max(104,14+64*maxRatio)+24;
        WindowColumn.Width=new GridLength(Math.Min(column,Math.Max(80,(rect.Width/scale-30)*column/(column+requestedTargets))));
        double available=Math.Max(1,rect.Width/scale-30-WindowColumn.Width.Value-38);
        double previewHeight=Math.Min(64,available/maxRatio);
        foreach(var b in targets.Values)if(b.Child is StackPanel stack&&stack.Children.FirstOrDefault() is Image image){
            var size=SurfaceLayout.ScreenPreview(SurfaceLayout.Ratio(((DisplayInfo)b.Tag).Bounds),previewHeight);
            image.Width=size.Width;image.Height=size.Height;b.Width=Math.Max(Math.Min(104,available+14),size.Width+14);b.HorizontalAlignment=HorizontalAlignment.Left;
        }
        AppWindow.MoveAndResize(new(rect.Left,rect.Top,rect.Width,rect.Height));
    }
    async Task Reload(bool force=false){
        if(loading||acting||pressed!=null)return;loading=true;int request=++generation;
        try{
            var data=await owner.QuickData(force);
            if(!IsPanelVisible||request!=generation)return;
            windows=data.Windows;
            var selectedKey=source?.Key;
            targets.Clear();Screens.Children.Clear();SourceScreen.Items.Clear();
            foreach(var d in data.Displays.Where(d=>d.Active)){
                SourceScreen.Items.Add(new ComboBoxItem{Content=$"屏幕 {d.Number}",Tag=d});
                var image=new Image{Height=64,Stretch=Stretch.Uniform,Source=data.Previews.GetValueOrDefault(d.Key)};
                var stack=new StackPanel{Spacing=5};
                stack.Children.Add(image);stack.Children.Add(new TextBlock{Text=$"屏幕 {d.Number} · {d.Name}",TextTrimming=TextTrimming.CharacterEllipsis,FontSize=12});
                var border=new SelectionSurface{Child=stack,Padding=new(6),IsSelected=d.Key==selectedKey,Tag=d};
                ToolTipService.SetToolTip(border,d.Details);targets[d.Key]=border;Screens.Children.Add(border);
            }
            SourceScreen.SelectedItem=SourceScreen.Items.OfType<ComboBoxItem>().FirstOrDefault(x=>(x.Tag as DisplayInfo)?.Key==selectedKey)??SourceScreen.Items.FirstOrDefault();
            SetWindows();if(IsPanelVisible)PlacePanel();
        }catch(Exception ex){ShowNotice("刷新失败："+ex.Message);}
        finally{loading=false;}
    }
    void SourceChanged(object sender,SelectionChangedEventArgs e){
        if((SourceScreen.SelectedItem as ComboBoxItem)?.Tag is DisplayInfo d)source=d;
        SetWindows();
        foreach(var b in targets.Values)b.IsSelected=(b.Tag as DisplayInfo)?.Gdi==source?.Gdi;
    }
    void SetWindows(){
        var items=windows.Where(w=>w.Gdi==source?.Gdi).ToList();QuickList.ItemsSource=items;
        QuickEmpty.Visibility=items.Count==0?Visibility.Visible:Visibility.Collapsed;
        if(!loading&&IsPanelVisible&&!dragging)PlacePanel();
    }
    void SelectionChanged(object sender,SelectionChangedEventArgs e){foreach(var w in QuickList.Items.OfType<WindowInfo>())w.SetSelected(ReferenceEquals(QuickList.SelectedItem,w));}
    static WindowInfo? Find(DependencyObject? node){
        while(node!=null){if(node is ListViewItem item&&item.Content is WindowInfo row)return row;if(node is FrameworkElement f&&f.DataContext is WindowInfo w)return w;node=VisualTreeHelper.GetParent(node);}return null;
    }
    void Pressed(object sender,PointerRoutedEventArgs e){
        if(acting||!e.GetCurrentPoint(QuickRoot).Properties.IsLeftButtonPressed)return;
        pressed=Find(e.OriginalSource as DependencyObject);if(pressed==null){if(SelectionInput.IsBackground(e.OriginalSource as DependencyObject,QuickRoot))QuickList.SelectedItem=null;return;}
        start=e.GetCurrentPoint(QuickRoot).Position;QuickList.SelectedItem=pressed;
        e.Handled=true;
        if(!QuickRoot.CapturePointer(e.Pointer))pressed=null;else dragFeedback.Press(QuickList.ContainerFromItem(pressed) as FrameworkElement,start,pressed.AppIcon as ImageSource);
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
        if(!dragging){dragging=true;dragFeedback.Start(new[]{QuickList.ContainerFromItem(pressed)}.OfType<FrameworkElement>(),1);}
        hover=Hit(p);
        var intent=DropIntent.For(hover,new[]{pressed},true,PlatformSupport.Current.CanHide);
        QuickGhostText.Text=hover==null?WindowTitle.Middle(pressed.Title,24):intent.Label;
        dragFeedback.Move(p,hover!=null);
        foreach(var b in targets.Values){b.DropLabel=intent.Label;b.IsDropTarget=(b.Tag as DisplayInfo)?.Key==hover?.Key;}
        var area=ScreenScroll.TransformToVisual(QuickRoot).TransformBounds(new(0,0,ScreenScroll.ActualWidth,ScreenScroll.ActualHeight));
        if(area.Contains(p)){double dy=p.Y<area.Top+24?-12:p.Y>area.Bottom-24?12:0;if(dy!=0)ScreenScroll.ChangeView(null,Math.Clamp(ScreenScroll.VerticalOffset+dy,0,ScreenScroll.ScrollableHeight),null,true);}
        e.Handled=true;
    }
    async void Released(object sender,PointerRoutedEventArgs e){
        if(pressed==null)return;
        var item=pressed;var target=dragging?Hit(e.GetCurrentPoint(QuickRoot).Position):null;Cancel();
        if(target==null)return;
        acting=true;SourceScreen.IsEnabled=false;
        try{var result=await owner.QuickDrop(item,target);if(!result.Success)ShowNotice(result.Message);else{noticeVersion++;QuickNotice.Visibility=Visibility.Collapsed;}}
        catch(Exception ex){ShowNotice("操作失败："+ex.Message);}
        finally{acting=false;SourceScreen.IsEnabled=true;await Reload(true);if(!panelActive||pendingDismiss)Dismiss();}
        e.Handled=true;
    }
    void Canceled(object sender,PointerRoutedEventArgs e)=>Cancel();
    void CaptureLost(object sender,PointerRoutedEventArgs e){if(ReferenceEquals(e.OriginalSource,QuickRoot))Cancel();}
    void Cancel(){pressed=null;dragging=false;hover=null;dragFeedback.End();QuickRoot.ReleasePointerCaptures();foreach(var b in targets.Values)b.IsDropTarget=false;}
    void DismissClicked(object sender,RoutedEventArgs e)=>Dismiss();
    void MainClicked(object sender,RoutedEventArgs e){Dismiss();owner.ShowMain();}
    async void ShowNotice(string message){
        int request=++noticeVersion;QuickStatus.Text=message;QuickNotice.Visibility=Visibility.Visible;Motion.Enter(QuickNotice,0,4);
        await Task.Delay(2400);if(request!=noticeVersion||!IsPanelVisible)return;
        await Motion.Exit(QuickNotice,0,4);if(request==noticeVersion)QuickNotice.Visibility=Visibility.Collapsed;
    }
}
