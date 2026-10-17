using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
namespace ScreenMover.Views;

public sealed class DragFeedback(FrameworkElement root,Canvas layer,Border ghost,Image image,TextBlock count) {
    readonly List<FrameworkElement> lifted=[];
    FrameworkElement? pressed;
    Point grip;
    int version;
    public async void Press(FrameworkElement? element,Point point,ImageSource? fallback){
        End();if(element==null)return;
        pressed=element;int request=++version;
        image.Source=fallback;
        var bounds=element.TransformToVisual(root).TransformBounds(new(0,0,element.ActualWidth,element.ActualHeight));
        double width=Math.Min(260,Math.Max(100,bounds.Width));
        ghost.Width=width;image.Height=Math.Clamp(width*bounds.Height/Math.Max(1,bounds.Width),40,240);
        grip=new(Math.Clamp((point.X-bounds.Left)/Math.Max(1,bounds.Width),0,1)*width,
            Math.Clamp((point.Y-bounds.Top)/Math.Max(1,bounds.Height),0,1)*image.Height);
        try{
            var bitmap=new RenderTargetBitmap();
            await bitmap.RenderAsync(element,(int)width,(int)Math.Max(1,image.Height));
            if(version==request)image.Source=bitmap;
        }catch{}
        if(version==request&&pressed!=null&&lifted.Count==0)Motion.Touch(element,true);
    }
    public void Start(IEnumerable<FrameworkElement> elements,int total){
        if(pressed!=null)Motion.Touch(pressed,false);
        foreach(var element in elements.Distinct()){lifted.Add(element);Motion.Lift(element);}
        count.Text=total>1?total.ToString():"";count.Visibility=total>1?Visibility.Visible:Visibility.Collapsed;
        if(count.Parent is Border badge)badge.Visibility=count.Visibility;
        ghost.Visibility=Visibility.Visible;Motion.Enter(ghost,0,3);
    }
    public void Move(Point point){
        var p=root.TransformToVisual(layer).TransformPoint(point);
        Canvas.SetLeft(ghost,p.X-grip.X);Canvas.SetTop(ghost,p.Y-grip.Y);
    }
    public void End(){
        version++;
        if(pressed!=null)Motion.Touch(pressed,false);
        foreach(var element in lifted)Motion.Touch(element,false);
        lifted.Clear();pressed=null;ghost.Visibility=Visibility.Collapsed;
    }
}
