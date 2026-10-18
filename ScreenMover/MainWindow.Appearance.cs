using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;
namespace ScreenMover;

public sealed partial class MainWindow {
    readonly UISettings appearanceSettings=new();
    void SystemColorsChanged(UISettings sender,object args)=>DispatcherQueue.TryEnqueue(()=>{
        if(!closed){Views.Appearance.Apply(settings.AccentColor,appearanceSettings);UpdateCaptionTheme();}
    });
    void UpdateCaptionTheme(){
        var title=AppWindow.TitleBar;
        var foreground=cursorHost.ActualTheme==ElementTheme.Dark?Colors.White:Colors.Black;
        title.ButtonBackgroundColor=Colors.Transparent;
        title.ButtonInactiveBackgroundColor=Colors.Transparent;
        title.ButtonForegroundColor=foreground;title.ButtonInactiveForegroundColor=foreground;
        title.ButtonHoverForegroundColor=foreground;title.ButtonPressedForegroundColor=foreground;
        title.ButtonHoverBackgroundColor=cursorHost.ActualTheme==ElementTheme.Dark?Windows.UI.Color.FromArgb(40,255,255,255):Windows.UI.Color.FromArgb(25,0,0,0);
        title.ButtonPressedBackgroundColor=title.ButtonHoverBackgroundColor;
    }
}
