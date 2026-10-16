using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace ScreenMover;
public partial class App : Application {
private Window? window;
public App() { UnhandledException+=(_,e)=>Log(e.Exception); InitializeComponent(); }
static void Log(Exception e){try {var folder=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ScreenMover");System.IO.Directory.CreateDirectory(folder);System.IO.File.AppendAllText(System.IO.Path.Combine(folder,"errors.log"),DateTimeOffset.Now+Environment.NewLine+e+Environment.NewLine);}catch {}}
protected override void OnLaunched(LaunchActivatedEventArgs args) {
var command=Environment.GetCommandLineArgs();
if(command.Length>=4&&command[1]=="--capture-window"){Environment.Exit(Services.SnapshotService.CaptureWorker(command.Skip(2).ToArray()));return;}
if(command.Contains("--fixture")) {
window=new Window {Title="ScreenMover Test Fixture",Content=new TextBlock {Text="ScreenMover 测试窗口\n仅供移动、最小化、最大化验证；无用户文档。",Margin=new Thickness(32),FontSize=22}};
window.AppWindow.Resize(new Windows.Graphics.SizeInt32(640,420));
} else {try {window=new MainWindow();}catch(Exception e){Log(e);throw;}}
window.Activate();
}
}
