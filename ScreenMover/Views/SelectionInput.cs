using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
namespace ScreenMover.Views;

public static class SelectionInput {
    public static bool IsBackground(DependencyObject? node,DependencyObject root){
        while(node!=null){
            if(node is ButtonBase or TextBox or ComboBox or ScrollBar or ListViewItem or GridViewItem)return false;
            if(node is FrameworkElement f&&f.Tag is Models.DisplayInfo)return false;
            if(ReferenceEquals(node,root))return true;
            node=VisualTreeHelper.GetParent(node);
        }
        return false;
    }
}
