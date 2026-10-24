using Microsoft.UI.Xaml.Markup;
namespace ScreenMover.Views;
[MarkupExtensionReturnType(ReturnType=typeof(string))]
public sealed class LocExtension : MarkupExtension {
    public string Key {get;set;}="";
    protected override object ProvideValue()=>L.T(Key);
}
