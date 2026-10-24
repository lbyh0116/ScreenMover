using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using ScreenMover.Services;
using Windows.System;
using Windows.UI.Core;
namespace ScreenMover.Views;

public sealed class HotkeyPicker : UserControl {
    readonly StackPanel body=new();
    UIElementCollection Children=>body.Children;
    readonly TextBox display=new(){IsReadOnly=true,PlaceholderText=L.T("按组合键，或从右侧选择"),Padding=new Thickness(12,5,42,5)};
    readonly Flyout flyout=new();
    readonly ToggleButton ctrl=new(){Content="Ctrl"},alt=new(){Content="Alt"},shift=new(){Content="Shift"},win=new(){Content="Win"};
    readonly ComboBox key=new(){Header=L.T("主键"),HorizontalAlignment=HorizontalAlignment.Stretch};
    readonly TextBlock error=new(){TextWrapping=TextWrapping.Wrap,FontSize=12};
    readonly DispatcherTimer compose=new(){Interval=TimeSpan.FromMilliseconds(350)};
    bool updating;
    string value="";
    public event EventHandler? ValueChanged;
    public string Value {get=>value;set {this.value=HotkeySpec.Parse(value)?.Name??"";display.Text=this.value;}}
    public HotkeyPicker(string title,string initial){
        Content=body;body.Spacing=6;Children.Add(new TextBlock{Text=title,TextWrapping=TextWrapping.Wrap});
        var field=new Grid();field.Children.Add(display);
        AutomationProperties.SetName(display,title);
        var arrow=new Button{Content=new FontIcon{Glyph="\uE70D",FontSize=12},Style=(Style)Application.Current.Resources["SubtleButtonStyle"],HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Stretch,Width=36,Padding=new Thickness(0),BorderThickness=new Thickness(0)};
        ToolTipService.SetToolTip(arrow,L.T("选择预设或自由组合"));AutomationProperties.SetName(arrow,L.T("选择快捷键"));field.Children.Add(arrow);Children.Add(field);Children.Add(error);
        var panel=new StackPanel{Spacing=10,Margin=new Thickness(0,0,24,0)};
        panel.Children.Add(new TextBlock{Text=L.T("常用组合"),FontWeight=Microsoft.UI.Text.FontWeights.SemiBold});
        foreach(string preset in new[]{"Ctrl+Alt+Space","Ctrl+Alt+H","Ctrl+Alt+M","Ctrl+Shift+Space","Win+Alt+M"}){
            var button=new Button{Content=preset,HorizontalAlignment=HorizontalAlignment.Stretch,HorizontalContentAlignment=HorizontalAlignment.Left};
            button.Click+=(_,_)=>{TrySet(preset);flyout.Hide();};panel.Children.Add(button);
        }
        panel.Children.Add(new TextBlock{Text=L.T("自由搭配"),FontWeight=Microsoft.UI.Text.FontWeights.SemiBold});
        var modifiers=new StackPanel{Orientation=Orientation.Horizontal,Spacing=6};
        foreach(var toggle in new[]{ctrl,alt,shift,win}){modifiers.Children.Add(toggle);toggle.Checked+=ComposeChanged;toggle.Unchecked+=ComposeChanged;}
        panel.Children.Add(modifiers);
        foreach(string name in Enumerable.Range(0,256).Select(v=>HotkeySpec.KeyName((uint)v)).Where(n=>n!=""&&n!="F12").Distinct())key.Items.Add(name);
        key.SelectionChanged+=(_,_)=>ScheduleCompose();panel.Children.Add(key);
        flyout.Content=new ScrollViewer{Content=panel,MaxHeight=430,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
        arrow.Click+=(_,_)=>{
            updating=true;
            try{
                var spec=HotkeySpec.Parse(Value);uint mods=spec?.Modifiers??3;
                ctrl.IsChecked=(mods&2)!=0;alt.IsChecked=(mods&1)!=0;shift.IsChecked=(mods&4)!=0;win.IsChecked=(mods&8)!=0;
                key.SelectedItem=spec==null?null:HotkeySpec.KeyName(spec.Key);
            }finally{updating=false;}
            var style=new Style(typeof(FlyoutPresenter));style.Setters.Add(new Setter(FrameworkElement.WidthProperty,Math.Max(280,field.ActualWidth)));style.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(12)));flyout.FlyoutPresenterStyle=style;
            flyout.ShowAt(field,new FlyoutShowOptions{Placement=FlyoutPlacementMode.BottomEdgeAlignedLeft});
        };
        compose.Tick+=(_,_)=>{compose.Stop();ApplyComposition();};
        flyout.Closed+=(_,_)=>FlushComposition();
        display.KeyDown+=(_,e)=>{
            bool Down(VirtualKey k)=>(InputKeyboardSource.GetKeyStateForCurrentThread(k)&CoreVirtualKeyStates.Down)!=0;
            bool modified=Down(VirtualKey.Control)||Down(VirtualKey.Menu)||Down(VirtualKey.LeftWindows)||Down(VirtualKey.RightWindows);
            if(e.Key is VirtualKey.Escape||e.Key==VirtualKey.Tab&&!modified)return;
            e.Handled=true;
            if(e.Key is VirtualKey.Control or VirtualKey.Menu or VirtualKey.Shift or VirtualKey.LeftWindows or VirtualKey.RightWindows or VirtualKey.Back or VirtualKey.Delete)return;
            var parts=new List<string>();if(Down(VirtualKey.Control))parts.Add("Ctrl");if(Down(VirtualKey.Menu))parts.Add("Alt");if(Down(VirtualKey.Shift))parts.Add("Shift");if(Down(VirtualKey.LeftWindows)||Down(VirtualKey.RightWindows))parts.Add("Win");
            string name=HotkeySpec.KeyName((uint)e.Key);if(name!=""){parts.Add(name);TrySet(string.Join('+',parts));}
        };
        Unloaded+=(_,_)=>{compose.Stop();flyout.Hide();};Value=initial;
    }
    void ComposeChanged(object sender,RoutedEventArgs e)=>ScheduleCompose();
    public void FlushComposition(){if(compose.IsEnabled){compose.Stop();ApplyComposition();}}
    void ScheduleCompose(){if(updating)return;compose.Stop();compose.Start();}
    void ApplyComposition(){
        if(key.SelectedItem is not string name)return;
        var parts=new List<string>();if(ctrl.IsChecked==true)parts.Add("Ctrl");if(alt.IsChecked==true)parts.Add("Alt");if(shift.IsChecked==true)parts.Add("Shift");if(win.IsChecked==true)parts.Add("Win");parts.Add(name);TrySet(string.Join('+',parts));
    }
    void TrySet(string text){
        try{string canonical=HotkeySpec.Parse(text)?.Name??"";error.Text="";if(canonical==Value)return;Value=canonical;ValueChanged?.Invoke(this,EventArgs.Empty);}
        catch(FormatException ex){error.Text=ex.Message;}
    }
}
