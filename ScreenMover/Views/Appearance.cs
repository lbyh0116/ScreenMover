using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using ScreenMover.Services;
using Windows.UI;
using Windows.UI.ViewManagement;
namespace ScreenMover.Views;

public static class Appearance {
    static readonly SolidColorBrush accentText=new(Microsoft.UI.Colors.White);
    static readonly SolidColorBrush secondary=new();
    static readonly SolidColorBrush tertiary=new();
    public static void InitializeResources(){
        var resources=Application.Current.Resources;
        var accent=resources["AppAccentBrush"];
        resources["AccentFillColorDefaultBrush"]=accent;
        resources["AccentFillColorSecondaryBrush"]=secondary;
        resources["AccentFillColorTertiaryBrush"]=tertiary;
        resources["SystemControlHighlightAccentBrush"]=accent;
        resources["TextOnAccentFillColorPrimaryBrush"]=accentText;
        resources["TextOnAccentFillColorSecondaryBrush"]=accentText;
    }
    public static Color Parse(string hex)=>Color.FromArgb(255,Convert.ToByte(hex.Substring(1,2),16),Convert.ToByte(hex.Substring(3,2),16),Convert.ToByte(hex.Substring(5,2),16));
    public static void Apply(string custom,UISettings system) {
        var color=AccentColors.Normalize(custom)==""?system.GetColorValue(UIColorType.Accent):Parse(custom);
        ((SolidColorBrush)Application.Current.Resources["AppAccentBrush"]).Color=color;
        secondary.Color=Color.FromArgb(230,color.R,color.G,color.B);
        tertiary.Color=Color.FromArgb(204,color.R,color.G,color.B);
        accentText.Color=AccentColors.UseBlackText(color.R,color.G,color.B)?Microsoft.UI.Colors.Black:Microsoft.UI.Colors.White;
        var drop=AccentColors.Overlay(color.R,color.G,color.B);
        ((SolidColorBrush)Application.Current.Resources["AppDropBrush"]).Color=Color.FromArgb(255,drop.R,drop.G,drop.B);
    }
}
