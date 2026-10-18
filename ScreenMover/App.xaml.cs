using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace ScreenMover;
public partial class App : Application {
private Window? window;
private Services.SingleInstance? single;
public App() { UnhandledException+=(_,e)=>Log(e.Exception); InitializeComponent(); }
static void Log(Exception e){try {var folder=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ScreenMover");System.IO.Directory.CreateDirectory(folder);System.IO.File.AppendAllText(System.IO.Path.Combine(folder,"errors.log"),DateTimeOffset.Now+Environment.NewLine+e+Environment.NewLine);}catch {}}
protected override async void OnLaunched(LaunchActivatedEventArgs args) {
var command=Environment.GetCommandLineArgs();
if(command.Contains("--fixture")) {
window=new Window {Title="ScreenMover Test Fixture",Content=new TextBlock {Text="ScreenMover 测试窗口\n仅供移动、最小化、最大化验证；无用户文档。",Margin=new Thickness(32),FontSize=22}};
window.AppWindow.Resize(new Windows.Graphics.SizeInt32(640,420));
} else {
    var queue=Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();bool pending=false;
    single=Services.SingleInstance.Claim(Services.SingleInstance.Name);
    if(single==null&&command.Contains("--elevated-restart")){
        for(int i=0;i<30&&single==null;i++){await Task.Delay(100);single=Services.SingleInstance.Claim(Services.SingleInstance.Name);}
    }
    if(single==null){
        if(!await Services.SingleInstance.Activate(Services.SingleInstance.Name))Log(new IOException("已有实例暂未响应，请从托盘打开或退出后重试。"));
        Exit();return;
    }
    single.Listen(()=>queue.TryEnqueue(()=>{if(window is MainWindow main)main.ShowMain();else pending=true;}));
    try{window=new MainWindow();}catch(Exception e){single.Dispose();Log(e);throw;}
    window.Closed+=(_,_)=>{single?.Dispose();single=null;};
    if(!pending&&command.Contains("--start-in-tray")&&((MainWindow)window).CanStartInTray){window.AppWindow.Hide();return;}
}
window.Activate();
}
}
