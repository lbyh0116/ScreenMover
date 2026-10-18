using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Windows.UI.ViewManagement;
namespace ScreenMover.Views;

public static class Motion {
    public static void Touch(FrameworkElement element,bool pressed)=>Feedback(element,pressed?.975f:1,pressed?.78f:1);
    public static void Lift(FrameworkElement element)=>Feedback(element,1,.18f);
    static void Feedback(FrameworkElement element,float scale,float opacity){
        var visual=ElementCompositionPreview.GetElementVisual(element);
        visual.CenterPoint=new((float)element.ActualWidth/2,(float)element.ActualHeight/2,0);
        visual.StopAnimation("Scale");visual.StopAnimation("Opacity");
        if(!Enabled){visual.Scale=new(scale,scale,1);visual.Opacity=opacity;return;}
        var ease=visual.Compositor.CreateCubicBezierEasingFunction(new(.2f,0),new(0,1));
        var zoom=visual.Compositor.CreateVector3KeyFrameAnimation();zoom.InsertKeyFrame(1,new(scale,scale,1),ease);zoom.Duration=TimeSpan.FromMilliseconds(scale<1?90:160);
        var fade=visual.Compositor.CreateScalarKeyFrameAnimation();fade.InsertKeyFrame(1,opacity,ease);fade.Duration=zoom.Duration;
        visual.StartAnimation("Scale",zoom);visual.StartAnimation("Opacity",fade);
    }
    public static bool Enabled {get{try{return new UISettings().AnimationsEnabled;}catch{return false;}}}
    public static void Reset(UIElement element) {
        ElementCompositionPreview.SetIsTranslationEnabled(element,true);
        var visual=ElementCompositionPreview.GetElementVisual(element);
        visual.StopAnimation("Opacity");visual.StopAnimation("Translation");
        visual.Opacity=1;visual.Properties.InsertVector3("Translation",Vector3.Zero);
    }
    public static void Enter(UIElement element,float x=0,float y=12) {
        Reset(element);if(!Enabled)return;
        var visual=ElementCompositionPreview.GetElementVisual(element);var compositor=visual.Compositor;
        var ease=compositor.CreateCubicBezierEasingFunction(new(.16f,1),new(.3f,1));
        var opacity=compositor.CreateScalarKeyFrameAnimation();opacity.InsertKeyFrame(0,0);opacity.InsertKeyFrame(1,1);opacity.Duration=TimeSpan.FromMilliseconds(180);
        var move=compositor.CreateVector3KeyFrameAnimation();move.InsertKeyFrame(0,new(x,y,0));move.InsertKeyFrame(1,Vector3.Zero,ease);move.Duration=TimeSpan.FromMilliseconds(240);
        visual.StartAnimation("Opacity",opacity);visual.StartAnimation("Translation",move);
    }
    public static void PrepareSlide(UIElement element,float x,float y){
        Reset(element);if(!Enabled)return;
        var visual=ElementCompositionPreview.GetElementVisual(element);
        visual.Opacity=0;visual.Properties.InsertVector3("Translation",new(x,y,0));
    }
    public static void SlideIn(UIElement element,float x,float y){
        if(!Enabled){Reset(element);return;}
        var visual=ElementCompositionPreview.GetElementVisual(element);var compositor=visual.Compositor;
        var ease=compositor.CreateCubicBezierEasingFunction(new(.16f,1),new(.3f,1));
        var move=compositor.CreateVector3KeyFrameAnimation();move.InsertKeyFrame(0,new(x,y,0));move.InsertKeyFrame(1,Vector3.Zero,ease);move.Duration=TimeSpan.FromMilliseconds(320);
        var opacity=compositor.CreateScalarKeyFrameAnimation();opacity.InsertKeyFrame(0,0);opacity.InsertKeyFrame(.25f,1);opacity.InsertKeyFrame(1,1);opacity.Duration=move.Duration;
        visual.StartAnimation("Translation",move);visual.StartAnimation("Opacity",opacity);
    }
    public static async Task Exit(UIElement element,float x=0,float y=6) {
        if(!Enabled)return;
        var visual=ElementCompositionPreview.GetElementVisual(element);var compositor=visual.Compositor;
        var opacity=compositor.CreateScalarKeyFrameAnimation();opacity.InsertKeyFrame(1,0);opacity.Duration=TimeSpan.FromMilliseconds(100);
        var move=compositor.CreateVector3KeyFrameAnimation();move.InsertKeyFrame(1,new(x,y,0));move.Duration=opacity.Duration;
        visual.StartAnimation("Opacity",opacity);visual.StartAnimation("Translation",move);
        await Task.Delay(110);
    }
}
