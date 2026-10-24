using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
namespace ScreenMover.Views;

public static class WindowMaterial {
    public static string Apply(Window window,Grid root){
        root.Background=new SolidColorBrush(root.ActualTheme==ElementTheme.Dark?Color.FromArgb(255,32,32,32):Color.FromArgb(255,243,243,243));
        try{
            if(MicaController.IsSupported())window.SystemBackdrop=new MicaBackdrop();
            else if(DesktopAcrylicController.IsSupported())window.SystemBackdrop=new DesktopAcrylicBackdrop();
            else {window.SystemBackdrop=null;return L.T("纯色");}
            root.Background=new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            return window.SystemBackdrop is MicaBackdrop?L.T("云母"):L.T("亚克力");
        }catch(Exception ex)when(ex is System.Runtime.InteropServices.COMException or NotSupportedException){window.SystemBackdrop=null;return L.T("纯色");}
    }
}
