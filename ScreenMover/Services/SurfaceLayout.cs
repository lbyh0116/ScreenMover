using static ScreenMover.Interop.Native;
namespace ScreenMover.Services;

// All native window rectangles are physical pixels; layout measurements are DIPs.
public static class SurfaceLayout {
    public enum Edge { Bottom, Top, Left, Right }
    public static Edge TaskbarEdge(RECT bounds, RECT work, RECT anchor) {
        var gaps = new[] { (Edge.Left, work.Left-bounds.Left), (Edge.Top, work.Top-bounds.Top),
            (Edge.Right, bounds.Right-work.Right), (Edge.Bottom, bounds.Bottom-work.Bottom) };
        var reserved = gaps.OrderByDescending(x=>x.Item2).First();
        if(reserved.Item2>2)return reserved.Item1;
        // Auto-hidden taskbars have no reserved work area. Use the tray icon edge.
        int x=anchor.Left+anchor.Width/2,y=anchor.Top+anchor.Height/2;
        return new[] {(Edge.Bottom,Math.Abs(bounds.Bottom-y)),(Edge.Top,Math.Abs(y-bounds.Top)),
            (Edge.Left,Math.Abs(x-bounds.Left)),(Edge.Right,Math.Abs(bounds.Right-x))}.MinBy(v=>v.Item2).Item1;
    }
    public static (RECT Bounds,Edge Edge) Flyout(RECT screen,RECT work,RECT anchor,double scale,int rows,int screens) {
        scale=Math.Clamp(scale,.75,4);
        int gap=Math.Max(1,(int)Math.Round(12*scale));
        var size=FlyoutSize(rows,screens);
        int width=Math.Min((int)Math.Round(size.Width*scale),Math.Max(1,work.Width-2*gap));
        int height=Math.Min((int)Math.Round(size.Height*scale),Math.Max(1,work.Height-2*gap));
        var edge=TaskbarEdge(screen,work,anchor);
        int x=edge==Edge.Left?work.Left+gap:work.Right-width-gap;
        int y=edge==Edge.Top?work.Top+gap:work.Bottom-height-gap;
        x=Math.Clamp(x,work.Left+gap,Math.Max(work.Left+gap,work.Right-width-gap));
        y=Math.Clamp(y,work.Top+gap,Math.Max(work.Top+gap,work.Bottom-height-gap));
        return (new(){Left=x,Top=y,Right=x+width,Bottom=y+height},edge);
    }
    public static (double Width,double Height) FlyoutSize(int rows,int screens){
        rows=Math.Clamp(rows,0,10000);screens=Math.Clamp(screens,1,100);
        // Short app names need only a compact left column. Balance 4:3 for small
        // collections, then grow vertically before resorting to scrolling.
        double height=Math.Clamp(64+Math.Max(36+rows*44,screens*108),258,360);
        double width=Math.Clamp(height*4/3,350,480);
        return (Math.Round(width),Math.Round(height));
    }
    public static (double Width,int Columns) GalleryGrid(double viewportWidth) {
        double available=Math.Max(1,viewportWidth-20);
        int columns=Math.Max(1,(int)(available/248));
        return (Math.Floor(available/columns),columns);
    }
    public static (int Width,int Height) InitialSize(AppSettings settings,double scale,RECT work) {
        double factor=settings.SizeInDips?scale:1;
        int width=settings.RememberSize?(int)Math.Round(settings.Width*factor):(int)(1060*scale);
        int height=settings.RememberSize?(int)Math.Round(settings.Height*factor):(int)(820*scale);
        return (Math.Min(Math.Max(width,(int)(720*scale)),work.Width),Math.Min(Math.Max(height,(int)(640*scale)),work.Height));
    }
}
