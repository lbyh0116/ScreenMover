using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ScreenMover.Services;
using Windows.Foundation;
namespace ScreenMover.Views;
public sealed class MiddleTitle : UserControl {
    readonly TextBlock label=new(){FontSize=13,VerticalAlignment=VerticalAlignment.Center};
    readonly TextBlock measure=new(){FontSize=13};
    public string FullText {get=>(string)GetValue(FullTextProperty);set=>SetValue(FullTextProperty,value);}
    public static readonly DependencyProperty FullTextProperty=DependencyProperty.Register(nameof(FullText),typeof(string),typeof(MiddleTitle),new PropertyMetadata("",(d,e)=>((MiddleTitle)d).Update()));
    public MiddleTitle(){Content=label;SizeChanged+=(_,_)=>Update();}
    void Update(){
        string text=FullText??"";ToolTipService.SetToolTip(this,text);
        double width=ActualWidth;
        if(width<=0){label.Text=WindowTitle.Middle(text,28);return;}
        measure.Text=text;measure.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));
        if(measure.DesiredSize.Width<=width){label.Text=text;return;}
        int lo=3,hi=Math.Min(100,System.Globalization.StringInfo.ParseCombiningCharacters(text).Length);
        string fitted="…";
        while(lo<=hi){
            int mid=(lo+hi)/2;string candidate=WindowTitle.Middle(text,mid);
            measure.Text=candidate;measure.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));
            if(measure.DesiredSize.Width<=width){fitted=candidate;lo=mid+1;}else hi=mid-1;
        }
        label.Text=fitted;
    }
}
