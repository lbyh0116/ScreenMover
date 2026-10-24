using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using ScreenMover.Models;
using ScreenMover.Services;
namespace ScreenMover;
public sealed partial class MainWindow {
    TrayIcon? tray;
    QuickWindow? quick;
    public bool Exiting {get;private set;}
    public AppSettings CurrentSettings=>settings;
    public bool CanStartInTray=>tray?.Installed==true;
    void SetupTrayLifecycle(){
        ConfigureTray();
        AppWindow.Closing+=(_,e)=>{
            settingsFlush?.Invoke();
            if(!Exiting&&settings.CloseToTray&&tray?.Installed==true){
                e.Cancel=true;AppWindow.Hide();
            }
        };
        Closed+=(_,_)=>{Exiting=true;tray?.Dispose();quick?.Close();};
    }
    void ConfigureTray(){
        if(!initialized)return;
        if(!settings.TrayEnabled){tray?.Dispose();tray=null;return;}
        if(tray!=null)return;
        try{
            tray=new(WinRT.Interop.WindowNative.GetWindowHandle(this),mouse=>DispatcherQueue.TryEnqueue(async()=>await OpenQuick(true,mouse)),()=>DispatcherQueue.TryEnqueue(ShowMain),()=>DispatcherQueue.TryEnqueue(ExitApp));
            if(!tray.Installed)Notice(L.T("托盘图标创建失败；关闭主窗口时将正常退出"));
        }catch(Exception ex){Notice(L.T("托盘不可用：")+ex.Message);}
    }
    public void ShowMain(){AppWindow.Show();if(AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p&&p.State==Microsoft.UI.Windowing.OverlappedPresenterState.Minimized)p.Restore();Activate();}
    public void ShowHidden(){ShowMain();HiddenClicked(this,new RoutedEventArgs());}
    public void ShowSettings(){ShowMain();SettingsClicked(this,new RoutedEventArgs());}
    public void ExitApp(){Exiting=true;Close();}
    async Task OpenQuick(bool fromTray=false,bool mouseTrayClick=false){
        if(dialogOpen){ShowMain();return;}if(busy)return;
        try{quick??=new QuickWindow(this);if(mouseTrayClick&&quick.ConsumeTrayDismiss()){quick.Dismiss();return;}if(quick.IsPanelVisible)quick.Dismiss();else await quick.ShowPanel(fromTray);}
        catch(Exception ex){Notice(L.T("快捷面板无法打开：")+ex.Message);ShowMain();}
    }
    async void QuickClicked(object sender,RoutedEventArgs e)=>await OpenQuick();
    public async Task<(List<DisplayInfo> Displays,List<WindowInfo> Windows,Dictionary<string,ImageSource> Previews)> QuickData(bool force=false){
        // The main window keeps its data current while in the tray. Reuse static previews
        // so opening the flyout does not wait for another round of native icon/capture calls.
        if(!force&&snapshot!=null&&lastSnapshot!=DateTime.MinValue){
            var cachedImages=monitorPreviews.Where(p=>p.Value.image.Source!=null)
                .ToDictionary(p=>p.Key,p=>p.Value.image.Source!);
            return (snapshot.Displays.ToList(),order.Sort(allWindows),cachedImages);
        }
        var cached=iconCache.Keys.ToHashSet();bool capture=settings.ScreenPreviews;
        var data=await Task.Run(()=>{
            var ds=displays.Read().Displays;var ws=windows.Read();
            var icons=new Dictionary<string,SnapshotFrame>();var previews=new Dictionary<string,SnapshotFrame>();
            foreach(var w in ws.Where(w=>!cached.Contains(w.OrderKey))){var f=IconService.Read(w);if(f!=null)icons[w.OrderKey]=f;}
            if(capture)foreach(var d in ds.Where(d=>d.Active)){try{previews[d.Key]=SnapshotService.Screen(d.Bounds);}catch{}}
            return (ds,ws,icons,previews);
        });
        foreach(var entry in data.icons)iconCache[entry.Key]=Bitmap(entry.Value);
        foreach(var w in data.ws)w.SetIcon(iconCache.GetValueOrDefault(w.OrderKey),settings.IconFallback);
        var images=data.previews.ToDictionary(p=>p.Key,p=>(ImageSource)Bitmap(p.Value));
        return (data.ds,order.Sort(data.ws),images);
    }
    public async Task<OperationResult> QuickDrop(WindowInfo window,DisplayInfo target){
        if(busy)return new(window.Title,false,L.T("有操作正在进行，请稍后重试"));
        busy=true;UpdateStatus();
        try{
            var result=await Task.Run(()=>{
                var current=windows.Read().FirstOrDefault(w=>w.Handle==window.Handle&&w.Pid==window.Pid&&w.ProcessStartedTicks==window.ProcessStartedTicks);
                if(current==null)return new OperationResult(window.Title,false,L.T("窗口已关闭或已不在当前虚拟桌面"));
                var destination=displays.Read().Displays.FirstOrDefault(d=>d.Active&&d.Key==target.Key);
                if(destination==null)return new OperationResult(window.Title,false,L.T("目标屏幕已断开"));
                return destination.Gdi==current.Gdi?hidden.Hide(current):moves.Move([current],destination,settings.MoveMargin,settings.ShrinkToFit).Single();
            });
            Notice(L.F("快捷操作：{0} · {1}", result.Title, result.Message));return result;
        }finally{busy=false;await Refresh();UpdateStatus();}
    }
}
