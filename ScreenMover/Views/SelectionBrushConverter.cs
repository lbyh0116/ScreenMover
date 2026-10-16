using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
namespace ScreenMover.Views;
public sealed class SelectionBrushConverter : IValueConverter {
    public object Convert(object value,Type targetType,object parameter,string language) =>
        new SolidColorBrush(value is true ? (Windows.UI.Color)Application.Current.Resources["SystemAccentColor"] : Colors.Transparent);
    public object ConvertBack(object value,Type targetType,object parameter,string language)=>throw new NotSupportedException();
}
