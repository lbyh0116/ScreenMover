using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
using System.Security.Principal;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using ScreenMover.Models;
using ScreenMover.Services;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
namespace ScreenMover;
public sealed partial class MainWindow : Window {
Interop.WindowChrome? chrome;
Views.DragFeedback? dragFeedback;
readonly Views.PointerGrid cursorHost=new(); readonly DisplayService displays=new(); readonly WindowService windows=new();
readonly Dictionary<string,(Image image,TextBlock label)> monitorPreviews=[];
readonly Dictionary<(nint,int),(object? image,string caption)> windowPreviews=[];
CancellationTokenSource? previewCancellation;
bool previewing,pendingPreview;
long lastAutoScroll;
bool refreshRequested=true; bool active;
DateTime lastSnapshot=DateTime.MinValue;
int noticeVersion;
readonly List<string> history=[];
readonly string historyPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ScreenMover","operations.log");
readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromMilliseconds(250)};
WindowEvents? events; DisplaySnapshot? snapshot; List<WindowInfo> allWindows=[];
DisplayInfo? selected; bool refreshing,busy,closed; int ticks;
string displaySignature="",windowSignature="";
long lastClickTime; nint lastClickHandle; Point start; WindowInfo? pressed; List<WindowInfo> dragItems=[]; bool dragging;
UIElement? capture; DisplayInfo? hover; List<WindowInfo> initialSelection=[];
readonly bool admin=new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
public MainWindow() {
string? settingsWarning=null;
try{settings=LocalData.Read(AppSettings.FilePath,()=>new AppSettings(),out settingsWarning);settings.Normalize();}catch(Exception ex){settingsWarning=ex.Message;}
InitializeComponent();dragFeedback=new(Root,DragLayer,Ghost,GhostImage,GhostCount);Content=null;cursorHost.Children.Add(Root);Content=cursorHost;
var hwnd=WinRT.Interop.WindowNative.GetWindowHandle(this);
try {
    var primary=displays.Read().Displays.First(d=>d.Active&&d.Primary);
    double scale=Interop.WindowChrome.ScaleAt(new(){X=primary.Work.Left+10,Y=primary.Work.Top+10});
    var size=SurfaceLayout.InitialSize(settings,scale,primary.Work);
    AppWindow.MoveAndResize(new(primary.Work.Left+(primary.Work.Width-size.Width)/2,primary.Work.Top+(primary.Work.Height-size.Height)/2,size.Width,size.Height));
}catch{AppWindow.Resize(new(1060,820));}
chrome=new(hwnd);
initialized=true;ApplySettings();
events=new(WinRT.Interop.WindowNative.GetWindowHandle(this));
try {if(File.Exists(historyPath))history.AddRange(File.ReadAllLines(historyPath));}catch{}
Root.Loaded+=async(_,_)=>{SetLayout();Views.Motion.Enter(Root,0,8);await RefreshWithPreviews();if(settingsWarning!=null)Notice(settingsWarning);if(hidden.LoadWarning!=null)Notice(hidden.LoadWarning);};
Root.SizeChanged+=(_,_)=>SetLayout();
Activated+=(_,e)=>{bool now=e.WindowActivationState!=WindowActivationState.Deactivated;if(now&&!active&&settings.RefreshOnActivate)refreshRequested=true;active=now;};
timer.Tick+=async(_,_)=>{ticks++; if(!busy&&pressed==null&&!refreshing&&!previewing&&(refreshRequested||(settings.RefreshSeconds>0&&DateTime.Now-lastSnapshot>=TimeSpan.FromSeconds(settings.RefreshSeconds)))){refreshRequested=false;await RefreshWithPreviews();}else if(ticks%8==0||Interlocked.Exchange(ref events.Dirty,0)!=0)await Refresh();};
timer.Start();
Closed+=(_,_)=>{try{if(settings.RememberSize){
var p=WindowService.Placement(WinRT.Interop.WindowNative.GetWindowHandle(this));
double scale=Math.Max(.75,Services.TrayIcon.GetDpiForWindow(WinRT.Interop.WindowNative.GetWindowHandle(this))/96.0);
settings.Width=(int)Math.Round(p.rcNormalPosition.Width/scale);settings.Height=(int)Math.Round(p.rcNormalPosition.Height/scale);settings.SizeInDips=true;
settings.Normalize();LocalData.Write(AppSettings.FilePath,settings);
}}catch{}
closed=true;globalHotkey?.Dispose();chrome?.Dispose();timer.Stop();events.Dispose();previewCancellation?.Cancel();};
Root.AddHandler(UIElement.PointerPressedEvent,new PointerEventHandler(Pressed),true);
Root.AddHandler(UIElement.PointerMovedEvent,new PointerEventHandler(Moved),true);
Root.AddHandler(UIElement.PointerReleasedEvent,new PointerEventHandler(Released),true);
Root.AddHandler(UIElement.PointerCanceledEvent,new PointerEventHandler(Canceled),true);
Root.AddHandler(UIElement.PointerCaptureLostEvent,new PointerEventHandler(Canceled),true);
var f5=new KeyboardAccelerator{Key=VirtualKey.F5}; f5.Invoked+=async(_,e)=>{if(dialogOpen)return;e.Handled=true;await RefreshWithPreviews();};Root.KeyboardAccelerators.Add(f5);
var all=new KeyboardAccelerator{Key=VirtualKey.A,Modifiers=VirtualKeyModifiers.Control};all.Invoked+=(_,e)=>{if(!dialogOpen&&!ReferenceEquals(FocusManager.GetFocusedElement(Root.XamlRoot),SearchBox)){WindowsList.SelectAll();e.Handled=true;}};Root.KeyboardAccelerators.Add(all);
var esc=new KeyboardAccelerator{Key=VirtualKey.Escape};esc.Invoked+=(_,e)=>{if(dialogOpen)return;if(pressed!=null){RestoreSelection();CancelDrag();e.Handled=true;}else if(SearchBox.Text.Length>0){SearchBox.Text="";e.Handled=true;}};Root.KeyboardAccelerators.Add(esc);
var find=new KeyboardAccelerator{Key=VirtualKey.F,Modifiers=VirtualKeyModifiers.Control};find.Invoked+=(_,e)=>{if(!dialogOpen){SearchBox.Focus(FocusState.Keyboard);SearchBox.SelectAll();e.Handled=true;}};Root.KeyboardAccelerators.Add(find);
AdminButton.Visibility=admin?Visibility.Collapsed:Visibility.Visible;SetupTrayLifecycle();
}
async Task Refresh() {
if(refreshing||busy||pressed!=null||closed)return;
refreshing=true;
try {
var knownIcons=iconCache.Keys.ToHashSet();
var data=await Task.Run(()=>{
var ds=displays.Read();var ws=windows.Read();
var icons=new Dictionary<string,SnapshotFrame>();
foreach(var w in ws.Where(w=>!knownIcons.Contains(w.OrderKey))){var frame=IconService.Read(w);if(frame!=null)icons[w.OrderKey]=frame;}
return (ds,ws,icons);
});if(closed||pressed!=null)return;
foreach(var pair in data.Item3)iconCache[pair.Key]=Bitmap(pair.Value);
foreach(var old in iconCache.Keys.Where(k=>!data.Item2.Any(w=>w.OrderKey==k)).ToList())iconCache.Remove(old);
foreach(var w in data.Item2)w.SetIcon(iconCache.GetValueOrDefault(w.OrderKey),settings.IconFallback);
snapshot=data.Item1;allWindows=data.Item2;
string signature=string.Join("|",snapshot.Displays.Select(d=>$"{d.Key},{d.Gdi},{d.Details},{d.Primary}"));
if(signature!=displaySignature) {
displaySignature=signature;string? key=selected?.Key;
selected=snapshot.Displays.FirstOrDefault(d=>d.Key==key&&d.Active)??snapshot.Displays.FirstOrDefault(d=>d.Active);
pendingPreview=true;monitorPreviews.Clear();MonitorTiles.Items.Clear();
foreach(var d in snapshot.Displays.Where(d=>d.Active||settings.ShowInactive).OrderByDescending(d=>d.Active)) {
var stack=new StackPanel{Spacing=4,Width=240,Height=180};
stack.Children.Add(new TextBlock{Text=d.Heading,FontSize=d.Active?22:16,FontWeight=Microsoft.UI.Text.FontWeights.SemiBold});
if(d.Active) {
var preview=new Image{Width=240,Height=90,Stretch=Stretch.Uniform};
var label=new TextBlock{Visibility=Visibility.Collapsed};
stack.Children.Add(new Border{Child=preview,Background=new SolidColorBrush(Colors.Black),CornerRadius=new CornerRadius(5)});
stack.Children.Add(new TextBlock{Text=$"{d.Name}\n{d.Bounds.Width} × {d.Bounds.Height} · {d.Hertz:0.##} Hz",FontSize=11,TextWrapping=TextWrapping.Wrap,Opacity=.8});
stack.Children.Add(label);monitorPreviews[d.Key]=(preview,label);
ToolTipService.SetToolTip(stack,d.Details);
}else {stack.Children.Add(new Border{Height=90,Child=new TextBlock{Text="未启用",HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Opacity=.5}});stack.Children.Add(new TextBlock{Text=d.Name+"\n系统编号：未分配",TextWrapping=TextWrapping.Wrap,FontSize=11});}
var border=new Border{Child=stack,Padding=new Thickness(10),CornerRadius=new CornerRadius(8),BorderThickness=new Thickness(0),Tag=d,Opacity=d.Active?1:.65};
if(!d.Active) {
var dash=new Microsoft.UI.Xaml.Shapes.Rectangle{Stroke=new SolidColorBrush(Colors.Gray),StrokeDashArray=new DoubleCollection{4,3},StrokeThickness=1,RadiusX=8,RadiusY=8,IsHitTestVisible=false};
var grid=new Grid();border.BorderThickness=new Thickness(0);grid.Children.Add(border);grid.Children.Add(dash);grid.Tag=d;MonitorTiles.Items.Add(grid);
}else MonitorTiles.Items.Add(border);
}
MonitorTiles.SelectedItem=MonitorTiles.Items.OfType<FrameworkElement>().FirstOrDefault(b=>(b.Tag as DisplayInfo)?.Key==selected?.Key);
MonitorHeading.Text=$"系统正在使用的显示器 · {snapshot.DesktopCount}";
RebuildButtons();UpdateMonitorDensity();
}
UpdateWindows();
} catch(Exception ex) {Notice("刷新失败："+ex.Message);}
finally {refreshing=false;if(pendingPreview&&!closed){pendingPreview=false;_ = UpdatePreviews();}}
}
void UpdateWindows() {
var items=order.Sort(WindowQuery.Filter(allWindows,selected?.Gdi,SearchBox.Text,AllScreens.IsChecked==true));
foreach(var w in items){
if(!settings.WindowPreviews)w.SetPreview(null,"窗口预览已关闭");
else if(windowPreviews.TryGetValue((w.Handle,w.Pid),out var cached))w.SetPreview(cached.image,w.ShowCommand is 2 or 7?"最小化 · "+cached.caption:cached.caption);
else w.SetPreview(null,w.ShowCommand is 2 or 7?"最小化 · 暂无截图":"等待自动截图");
}
foreach(var key in windowPreviews.Keys.Where(k=>!allWindows.Any(w=>w.Handle==k.Item1&&w.Pid==k.Item2)).ToList())windowPreviews.Remove(key);
string signature=string.Join("|",items.Select(w=>$"{w.Handle},{w.Title},{w.Details}"));
if(signature!=windowSignature) {
windowSignature=signature;var chosen=WindowsList.SelectedItems.Cast<WindowInfo>().Select(w=>w.Handle).ToHashSet();
WindowsList.ItemsSource=items;
DispatcherQueue.TryEnqueue(UpdateGalleryWidth);
foreach(var w in items.Where(w=>chosen.Contains(w.Handle)))WindowsList.SelectedItems.Add(w);
}
EmptyState.Text=SearchBox.Text.Length>0?"没有匹配的窗口，试试更短的关键词或「所有屏幕」":"此屏幕上暂无可管理的窗口";
EmptyState.Visibility=items.Count==0?Visibility.Visible:Visibility.Collapsed;
WindowHeading.Text=(AllScreens.IsChecked==true?"所有屏幕":selected==null?"窗口":$"屏幕 {selected.Number} 上的窗口")+$" · {items.Count}";
UpdateStatus();
}
void RebuildButtons() {
MoveButtons.Children.Clear();
foreach(var d in snapshot?.Displays.Where(d=>d.Active)??[]) {
var b=new Button{Content=(d.Key==selected?.Key&&AllScreens.IsChecked!=true?"当前屏幕：":"→ ")+$"屏幕 {d.Number}"+(d.Primary?" · 主屏":""),Tag=d};
b.Click+=async(_,_)=>await MoveSelected(d);MoveButtons.Children.Add(b);
}
UpdateStatus();
}
void UpdateStatus() {
if(!initialized)return;
int count=WindowsList.SelectedItems.Count;
UndoButton.IsEnabled=moves.Count>0&&!busy;UndoButton.Content=moves.Count>0?$"撤销移动 · {moves.Count}":"撤销移动";HiddenButton.Content=$"已隐藏 · {hidden.Read().Count}";SettingsButton.IsEnabled=HiddenButton.IsEnabled=!busy;
SelectionStatus.Text=count>0?$"已选 {count} 个窗口":"";
HideButton.IsEnabled=MinButton.IsEnabled=RestoreButton.IsEnabled=MaxButton.IsEnabled=count>0&&!busy;
foreach(var b in MoveButtons.Children.OfType<Button>())b.IsEnabled=count>0&&!busy&&(AllScreens.IsChecked==true||(b.Tag as DisplayInfo)?.Key!=selected?.Key);
}
async void MonitorSelectionChanged(object sender,SelectionChangedEventArgs e) {
if((MonitorTiles.SelectedItem as FrameworkElement)?.Tag is DisplayInfo d){
if(!d.Active){MonitorTiles.SelectedItem=MonitorTiles.Items.OfType<FrameworkElement>().FirstOrDefault(x=>(x.Tag as DisplayInfo)?.Key==selected?.Key);Notice("此显示器未启用，请在系统显示设置中启用。");return;}
selected=d;windowSignature="";UpdateWindows();Views.Motion.Enter(WindowsList,0,8);RebuildButtons();UpdateMonitorDensity();
}
await Task.CompletedTask;
}
void WindowSelectionChanged(object sender,SelectionChangedEventArgs e){MarkSelection();UpdateStatus();}
async void RefreshClicked(object sender,RoutedEventArgs e){await RefreshWithPreviews();}
async void MinClicked(object sender,RoutedEventArgs e)=>await Change(2);
async void RestoreClicked(object sender,RoutedEventArgs e)=>await Change(9);
async void MaxClicked(object sender,RoutedEventArgs e)=>await Change(3);
async Task Change(int cmd)=>await Operate(WindowsList.SelectedItems.Cast<WindowInfo>().ToList(),w=>windows.ChangeState(w,cmd),"修改状态");
async Task MoveSelected(DisplayInfo d)=>await MoveBatch(WindowsList.SelectedItems.Cast<WindowInfo>().ToList(),d);
async Task Operate(List<WindowInfo> list,Func<WindowInfo,OperationResult> action,string verb) {
if(busy||list.Count==0)return;
busy=true;UpdateStatus();
try {
var result=await Task.Run(()=>list.Select(action).ToList());
Notice($"{verb} {list.Count} 个窗口：成功 {result.Count(r=>r.Success)}，失败 {result.Count(r=>!r.Success)}"+
string.Concat(result.Select(r=>$"\n{r.Title}：{r.Message}")));
} catch(Exception ex){Notice("操作失败："+ex.Message);}
finally{busy=false;await Refresh();UpdateStatus();}
}
async void WindowDoubleTapped(object sender,DoubleTappedRoutedEventArgs e) {
var item=FindWindow(e.OriginalSource as DependencyObject);if(item!=null)await Operate([item],windows.Activate,"激活");
}
static WindowInfo? FindWindow(DependencyObject? source) {
while(source!=null){if(source is FrameworkElement f&&f.DataContext is WindowInfo w)return w;source=VisualTreeHelper.GetParent(source);}return null;
}
void Pressed(object sender,PointerRoutedEventArgs e) {
if(busy||!e.GetCurrentPoint(Root).Properties.IsLeftButtonPressed)return;
var item=FindWindow(e.OriginalSource as DependencyObject);if(item==null){if(Views.SelectionInput.IsBackground(e.OriginalSource as DependencyObject,Root)){WindowsList.SelectedItems.Clear();lastClickHandle=0;}return;}
WindowsList.Focus(FocusState.Pointer);pressed=item;start=e.GetCurrentPoint(Root).Position;initialSelection=WindowsList.SelectedItems.Cast<WindowInfo>().ToList();
capture=Root;
if(!capture.CapturePointer(e.Pointer)){pressed=null;capture=null;}
else dragFeedback?.Press(WindowsList.ContainerFromItem(item) as FrameworkElement,start,item.DisplayPreview as ImageSource);
}
void Moved(object sender,PointerRoutedEventArgs e) {
if(pressed==null)return;var p=e.GetCurrentPoint(Root).Position;
if(!dragging&&Math.Abs(p.X-start.X)+Math.Abs(p.Y-start.Y)>6) {
dragging=true;
dragItems=initialSelection.Any(w=>w.Handle==pressed.Handle)?initialSelection:[pressed];
WindowsList.SelectedItems.Clear();foreach(var w in dragItems)WindowsList.SelectedItems.Add(w);
dragFeedback?.Start(dragItems.Select(w=>WindowsList.ContainerFromItem(w)).OfType<FrameworkElement>(),dragItems.Count);
}
if(!dragging)return;
AutoScrollMonitors(p);AutoScrollWindows(p);hover=Hit(p);var intent=DropIntent.For(hover,dragItems,false);cursorHost.SetDragCursor(true,intent.Allowed);
GhostText.Text=hover!=null?intent.Label:WindowTitle.Middle(pressed.Title,24);
if(hover==null)ShowInsertion(p);else InsertionMark.Visibility=Visibility.Collapsed;
dragFeedback?.Move(p);
foreach(var el in MonitorTiles.Items.OfType<FrameworkElement>())if(MonitorTiles.ContainerFromItem(el) is FrameworkElement container&&Descendant<Views.SelectionSurface>(container) is {} surface){surface.DropLabel=intent.Label;surface.IsDropTarget=(el.Tag as DisplayInfo)?.Key==hover?.Key;}
e.Handled=true;
}
DisplayInfo? Hit(Point p) {
foreach(var el in MonitorTiles.Items.OfType<FrameworkElement>()) {
var owner=MonitorTiles;
var viewport=owner.TransformToVisual(Root).TransformBounds(new Rect(0,0,owner.ActualWidth,owner.ActualHeight));
if(!viewport.Contains(p))continue;
var bounds=el.TransformToVisual(Root).TransformBounds(new Rect(0,0,el.ActualWidth,el.ActualHeight));
if(bounds.Contains(p))return el.Tag as DisplayInfo;
}
return null; // Root DIP space for hit testing, physical pixels for native window geometry.
}
async void Released(object sender,PointerRoutedEventArgs e) {
if(pressed==null)return;
var target=dragging?Hit(e.GetCurrentPoint(Root).Position):null;var list=dragItems.ToList();bool wasDrag=dragging;var item=pressed;
var insertion=wasDrag&&target==null?FindInsertion(e.GetCurrentPoint(Root).Position):default;
CancelDrag();
if(wasDrag) {
e.Handled=true;
if(target!=null&&DropIntent.For(target,list,false).Action==DropAction.Move)await MoveBatch(list,target);
else if(insertion.Window!=null)Reorder(list,insertion.Window,insertion.After);
else {RestoreSelection();if(target is {Active:false})Notice("此显示器未启用，窗口无法移过去。请在系统显示设置中启用。");}
} else {
bool ctrl=(InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)&CoreVirtualKeyStates.Down)!=0;
bool shift=(InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)&CoreVirtualKeyStates.Down)!=0;
if(shift&&initialSelection.Count>0) {
int a=WindowsList.Items.IndexOf(initialSelection.Last()),b=WindowsList.Items.IndexOf(item);
if(a>=0&&b>=0) for(int i=Math.Min(a,b);i<=Math.Max(a,b);i++)if(!WindowsList.SelectedItems.Contains(WindowsList.Items[i]))WindowsList.SelectedItems.Add(WindowsList.Items[i]);
}else if(ctrl){if(initialSelection.Contains(item))WindowsList.SelectedItems.Remove(item);else if(!WindowsList.SelectedItems.Contains(item))WindowsList.SelectedItems.Add(item);}
else {WindowsList.SelectedItems.Clear();WindowsList.SelectedItems.Add(item); if(lastClickHandle==item.Handle&&Environment.TickCount64-lastClickTime<500){lastClickHandle=0;await Operate([item],windows.Activate,"激活");}else{lastClickHandle=item.Handle;lastClickTime=Environment.TickCount64;}}
}
}
void RestoreSelection(){WindowsList.SelectedItems.Clear();foreach(var w in initialSelection)if(WindowsList.Items.Contains(w))WindowsList.SelectedItems.Add(w);}
void Canceled(object sender,PointerRoutedEventArgs e){if(pressed!=null){RestoreSelection();CancelDrag();}}
void CancelDrag() {InsertionMark.Visibility=Visibility.Collapsed;cursorHost.SetDragCursor(false,false);pressed=null;dragging=false;hover=null;dragFeedback?.End();var c=capture;capture=null;c?.ReleasePointerCaptures();foreach(var el in MonitorTiles.Items.OfType<FrameworkElement>())if(MonitorTiles.ContainerFromItem(el) is FrameworkElement container&&Descendant<Views.SelectionSurface>(container) is {} surface)surface.IsDropTarget=false;}

static WriteableBitmap Bitmap(SnapshotFrame frame) {
var image=new WriteableBitmap(frame.Width,frame.Height);
using var stream=image.PixelBuffer.AsStream();stream.Write(frame.Pixels);image.Invalidate();return image;
}
async Task UpdatePreviews() {
if(closed||snapshot==null)return;
if(!settings.ScreenPreviews&&!settings.WindowPreviews){lastSnapshot=DateTime.Now;RefreshTime.Text="预览已关闭";return;}
if(previewing){previewCancellation?.Cancel();pendingPreview=true;return;}
previewing=true;previewCancellation=new();var token=previewCancellation.Token;
lastSnapshot=DateTime.Now;RefreshTime.Text="正在截图…";
var screenList=snapshot.Displays.Where(d=>d.Active&&settings.ScreenPreviews).ToList();
var windowList=settings.WindowPreviews?allWindows.ToList():[];
try {
foreach(var d in screenList) {
token.ThrowIfCancellationRequested();
try {
var frame=await Task.Run(()=>SnapshotService.Screen(d.Bounds),token);
if(!closed&&monitorPreviews.TryGetValue(d.Key,out var view)){view.image.Source=Bitmap(frame);view.label.Text="截图 "+DateTime.Now.ToString("HH:mm:ss");}
}catch(OperationCanceledException){throw;}
catch {if(monitorPreviews.TryGetValue(d.Key,out var view))view.label.Text="截图不可用";}
}
foreach(var w in windowList) {
token.ThrowIfCancellationRequested();
if(w.ShowCommand is 2 or 7)continue;
var frame=await SnapshotService.WindowAsync(w.Handle,w.Pid,token);token.ThrowIfCancellationRequested();
if(closed)return;
object? image=frame==null?null:Bitmap(frame);
string caption=frame==null?"应用未提供画面 / 截图超时":"截图 "+DateTime.Now.ToString("HH:mm:ss");
if(frame==null&&windowPreviews.TryGetValue((w.Handle,w.Pid),out var old)&&old.image!=null){image=old.image;caption="历史 · "+old.caption.Replace("历史 · ","");}
windowPreviews[(w.Handle,w.Pid)]=(image,caption);
foreach(var current in allWindows.Where(x=>x.Handle==w.Handle&&x.Pid==w.Pid))current.SetPreview(image,caption);
foreach(var current in WindowsList.Items.OfType<WindowInfo>().Where(x=>x.Handle==w.Handle&&x.Pid==w.Pid))current.SetPreview(image,caption);
}
}catch(OperationCanceledException){}
catch(Exception e){if(!closed)Notice("截图失败："+e.Message);}
finally {
previewing=false;previewCancellation.Dispose();previewCancellation=null;
if(!closed){RefreshTime.Text="截图 "+lastSnapshot.ToString("HH:mm:ss");if(pendingPreview&&!refreshing){pendingPreview=false;_ = UpdatePreviews();}}
}
}
static T? Descendant<T>(DependencyObject node) where T:DependencyObject {
for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++){var child=VisualTreeHelper.GetChild(node,i);if(child is T match)return match;var nested=Descendant<T>(child);if(nested!=null)return nested;}return null;
}
void AutoScrollMonitors(Point p) {
if(Environment.TickCount64-lastAutoScroll<90)return;
var bounds=MonitorTiles.TransformToVisual(Root).TransformBounds(new Rect(0,0,MonitorTiles.ActualWidth,MonitorTiles.ActualHeight));
if(!bounds.Contains(p))return;
var scroll=Descendant<ScrollViewer>(MonitorTiles);if(scroll==null)return;
double delta=p.X<bounds.Left+36?-24:p.X>bounds.Right-36?24:0;
if(delta!=0){scroll.ChangeView(Math.Clamp(scroll.HorizontalOffset+delta,0,scroll.ScrollableWidth),null,null,true);lastAutoScroll=Environment.TickCount64;}
}void SetLayout(){
if(Root.ActualWidth<=0||Root.ActualHeight<=0)return;

UpdateGalleryWidth();UpdateMonitorDensity();
}
void WindowListSizeChanged(object sender,SizeChangedEventArgs e)=>UpdateGalleryWidth();
void UpdateGalleryWidth(){
    if(WindowsList.ActualWidth<=0)return;
    if(Descendant<ItemsWrapGrid>(WindowsList) is not {} panel)return;
    double width=SurfaceLayout.GalleryGrid(WindowsList.ActualWidth).Width;
    if(Math.Abs(panel.ItemWidth-width)>.5)panel.ItemWidth=width;
}
void UpdateMonitorDensity(){
    bool compact=Root.ActualHeight<720;
    MonitorHeading.Visibility=compact?Visibility.Collapsed:Visibility.Visible;
    HeadingGrid.RowSpacing=compact?0:6;
    foreach(var element in MonitorTiles.Items.OfType<FrameworkElement>()){
        var border=element as Border??(element as Grid)?.Children.OfType<Border>().FirstOrDefault();
        if(border?.Child is not StackPanel stack)continue;
        stack.Width=compact?216:240;stack.Height=compact?116:180;
        if(stack.Children.FirstOrDefault() is TextBlock heading)heading.FontSize=compact?16:22;
        if(border.Tag is DisplayInfo d&&d.Active&&monitorPreviews.TryGetValue(d.Key,out var preview)){
            preview.image.Width=compact?216:240;preview.image.Height=compact?44:90;preview.label.Visibility=Visibility.Collapsed;
        }else if(stack.Children.ElementAtOrDefault(1) is Border empty)empty.Height=compact?44:90;
    }
}
async Task RefreshWithPreviews(){
if(busy||pressed!=null){refreshRequested=true;return;}
refreshRequested=false;pendingPreview=false;await Refresh();
if(!previewing)await UpdatePreviews();
}
async void HideClicked(object sender,RoutedEventArgs e)=>await Operate(WindowsList.SelectedItems.Cast<WindowInfo>().ToList(),hidden.Hide,"隐藏到新桌面");
async void Notice(string message){
string entry=DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+"  "+message.Replace("\r","").Replace("\n","  |  ");
history.Add(entry);
try {Directory.CreateDirectory(Path.GetDirectoryName(historyPath)!);File.AppendAllText(historyPath,entry+Environment.NewLine);}catch{}
int version=++noticeVersion;
HistoryButton.Visibility=Visibility.Collapsed;NoticePanel.Visibility=Visibility.Visible;ResultStatus.Opacity=1;ResultStatus.Text=message;Views.Motion.Enter(NoticePanel,0,4);
await Task.Delay(settings.NoticeSeconds*1000);
if(version!=noticeVersion||closed)return;
await Views.Motion.Exit(NoticePanel,0,-4);
if(version==noticeVersion){ResultStatus.Text="";NoticePanel.Visibility=Visibility.Collapsed;HistoryButton.Visibility=Visibility.Visible;}
}
async void HistoryClicked(object sender,RoutedEventArgs e){
if(dialogOpen)return;dialogOpen=true;
var text=new TextBlock{Text=history.Count==0?"暂无操作记录":string.Join("\n\n",history.AsEnumerable().Reverse()),TextWrapping=TextWrapping.Wrap,IsTextSelectionEnabled=true};
var dialog=new ContentDialog{XamlRoot=Root.XamlRoot,Title="操作记录",CloseButtonText="关闭",Content=new ScrollViewer{Content=text,MaxHeight=520}};
try{await dialog.ShowAsync();}finally{dialogOpen=false;}
}void AdminClicked(object sender,RoutedEventArgs e) {
try {var p=Process.Start(new ProcessStartInfo(Environment.ProcessPath!,"--elevated-restart"){UseShellExecute=true,Verb="runas"});if(p!=null)ExitApp();}
catch(Exception ex){Notice("未重启："+ex.Message);}
}
}
