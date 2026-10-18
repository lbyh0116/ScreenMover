using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
namespace ScreenMover.Views;

public sealed class SelectionSurface : ContentControl {
    public static readonly DependencyProperty IsSelectedProperty=DependencyProperty.Register(nameof(IsSelected),typeof(bool),typeof(SelectionSurface),new PropertyMetadata(false,Changed));
    public static readonly DependencyProperty IsDropTargetProperty=DependencyProperty.Register(nameof(IsDropTarget),typeof(bool),typeof(SelectionSurface),new PropertyMetadata(false,Changed));
    public bool IsSelected {get=>(bool)GetValue(IsSelectedProperty);set=>SetValue(IsSelectedProperty,value);}
    public bool IsDropTarget {get=>(bool)GetValue(IsDropTargetProperty);set=>SetValue(IsDropTargetProperty,value);}
    public static readonly DependencyProperty DropLabelProperty=DependencyProperty.Register(nameof(DropLabel),typeof(string),typeof(SelectionSurface),new PropertyMetadata(""));
    public string DropLabel {get=>(string)GetValue(DropLabelProperty);set=>SetValue(DropLabelProperty,value);}
    public UIElement? Child {get=>Content as UIElement;set=>Content=value;}
    bool over,dropShown;
    Storyboard? marching;
    public SelectionSurface(){
        Loaded+=(_,_)=>Update();
        Unloaded+=(_,_)=>{marching?.Stop();marching=null;dropShown=false;};
        PointerEntered+=(_,_)=>{over=true;Update();};
        PointerExited+=(_,_)=>{over=false;Update();};
        PointerPressed+=(_,_)=>{if(IsDisplay)Motion.Touch(this,true);};
        PointerReleased+=(_,_)=>{if(IsDisplay)Motion.Touch(this,false);};
        PointerCanceled+=(_,_)=>{if(IsDisplay)Motion.Touch(this,false);};
        PointerCaptureLost+=(_,_)=>{if(IsDisplay)Motion.Touch(this,false);};
        Update();
    }
    static void Changed(DependencyObject d,DependencyPropertyChangedEventArgs e)=>((SelectionSurface)d).Update();
    bool IsDisplay=>Tag is Models.DisplayInfo||Content is FrameworkElement element&&element.Tag is Models.DisplayInfo;
    protected override void OnApplyTemplate(){marching?.Stop();marching=null;dropShown=false;base.OnApplyTemplate();Update();}
    void Update(){
        string key=IsDropTarget?"SurfaceDrop":IsSelected?"SurfaceSelected":over?"SurfaceHover":"SurfaceNormal";
        if(Application.Current.Resources.TryGetValue(key,out var value)&&value is Style style&&!ReferenceEquals(Style,style))Style=style;
        if(GetTemplateChild("DropOverlay") is UIElement overlay){
            overlay.Visibility=IsDropTarget?Visibility.Visible:Visibility.Collapsed;
            if(IsDropTarget&&!dropShown){
                Motion.Enter(overlay,0,3);
                if(Motion.Enabled&&GetTemplateChild("DropOutline") is Microsoft.UI.Xaml.Shapes.Rectangle outline){
                    var animation=new DoubleAnimation{From=0,To=-7,Duration=TimeSpan.FromMilliseconds(850),RepeatBehavior=RepeatBehavior.Forever,EnableDependentAnimation=true};
                    Storyboard.SetTarget(animation,outline);Storyboard.SetTargetProperty(animation,"StrokeDashOffset");
                    marching=new();marching.Children.Add(animation);marching.Begin();
                }
            }else if(!IsDropTarget){marching?.Stop();marching=null;}
            dropShown=IsDropTarget;
        }
    }
}
