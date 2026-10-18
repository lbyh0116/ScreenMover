using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ScreenMover.Models;
using ScreenMover.Services;
using Windows.Foundation;
namespace ScreenMover;
public sealed partial class MainWindow {
    readonly WindowOrder order=new();
    readonly Dictionary<string,object> iconCache=[];
    long lastListScroll;
    void MarkSelection(){
        var keys=WindowsList.SelectedItems.Cast<WindowInfo>().Select(w=>w.OrderKey).ToHashSet();
        foreach(var w in WindowsList.Items.OfType<WindowInfo>().Concat(allWindows))w.SetSelected(keys.Contains(w.OrderKey));
    }
    (WindowInfo? Window,bool After,Rect Bounds) FindInsertion(Point point){
        var viewport=WindowsList.TransformToVisual(Root).TransformBounds(new(0,0,WindowsList.ActualWidth,WindowsList.ActualHeight));
        if(!viewport.Contains(point))return default;
        (WindowInfo? Window,bool After,Rect Bounds) closest=default;double distance=double.MaxValue;
        foreach(var item in WindowsList.Items.OfType<WindowInfo>()){
            if(WindowsList.ContainerFromItem(item) is not FrameworkElement container)continue;
            var r=container.TransformToVisual(Root).TransformBounds(new(0,0,container.ActualWidth,container.ActualHeight));
            if(r.Bottom<viewport.Top||r.Top>viewport.Bottom)continue;
            double dx=point.X-(r.Left+r.Width/2),dy=point.Y-(r.Top+r.Height/2);
            double d=dx*dx+dy*dy;
            if(d<distance){distance=d;closest=(item,point.Y>r.Bottom||point.Y>=r.Top&&dx>0,r);}
        }
        return closest;
    }
    void ShowInsertion(Point point){
        InsertionMark.Visibility=Visibility.Collapsed;
        var slot=FindInsertion(point);
        if(slot.Window==null||dragItems.Any(w=>w.OrderKey==slot.Window.OrderKey))return;
        var r=slot.Bounds;
        var p=Root.TransformToVisual(DragLayer).TransformPoint(new(slot.After?r.Right:r.Left,r.Top));
        Canvas.SetLeft(InsertionMark,p.X);Canvas.SetTop(InsertionMark,p.Y);
        InsertionMark.Width=3;InsertionMark.Height=r.Height;InsertionMark.Visibility=Visibility.Visible;
        GhostText.Text=$"排序：放到「{WindowTitle.Middle(slot.Window.Title,20)}」{(slot.After?"之后":"之前")}";
        cursorHost.SetDragCursor(true,true);
    }
    void AutoScrollWindows(Point point){
        if(Environment.TickCount64-lastListScroll<90)return;
        var r=WindowsList.TransformToVisual(Root).TransformBounds(new(0,0,WindowsList.ActualWidth,WindowsList.ActualHeight));
        if(!r.Contains(point))return;
        var scroll=Descendant<ScrollViewer>(WindowsList);if(scroll==null)return;
        double delta=point.Y<r.Top+28?-24:point.Y>r.Bottom-28?24:0;
        if(delta!=0){scroll.ChangeView(null,Math.Clamp(scroll.VerticalOffset+delta,0,scroll.ScrollableHeight),null,true);lastListScroll=Environment.TickCount64;}
    }
    void Reorder(List<WindowInfo> items,WindowInfo target,bool after){
        try{
            if(order.Reorder(allWindows,items,target,after)){
                windowRevision.Invalidate();UpdateWindows();
                var keys=items.Select(w=>w.OrderKey).ToHashSet();
                foreach(var w in WindowsList.Items.OfType<WindowInfo>().Where(w=>keys.Contains(w.OrderKey)))if(!WindowsList.SelectedItems.Contains(w))WindowsList.SelectedItems.Add(w);
                MarkSelection();Notice("窗口顺序已保存，刷新和重新打开 ScreenMover 后保持顺序");
            }
        }catch(Exception ex){Notice("排序未保存："+ex.Message);}
    }
}
