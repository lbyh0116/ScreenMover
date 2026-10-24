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
    public static (RECT Bounds,Edge Edge) Flyout(RECT screen,RECT work,RECT anchor,double scale,int rows,int screens,double widestRatio=16.0/9,double applicationWidth=180) {
        scale=Math.Clamp(scale,.75,4);
        int gap=Math.Max(1,(int)Math.Round(12*scale));
        var size=FlyoutSize(rows,screens,widestRatio,applicationWidth);
        int width=Math.Min((int)Math.Round(size.Width*scale),Math.Max(1,work.Width-2*gap));
        int height=Math.Min((int)Math.Round(size.Height*scale),Math.Max(1,work.Height-2*gap));
        var edge=TaskbarEdge(screen,work,anchor);
        int x=edge==Edge.Left?work.Left+gap:work.Right-width-gap;
        int y=edge==Edge.Top?work.Top+gap:work.Bottom-height-gap;
        x=Math.Clamp(x,work.Left+gap,Math.Max(work.Left+gap,work.Right-width-gap));
        y=Math.Clamp(y,work.Top+gap,Math.Max(work.Top+gap,work.Bottom-height-gap));
        return (new(){Left=x,Top=y,Right=x+width,Bottom=y+height},edge);
    }
    public static double ApplicationColumn(IEnumerable<double> measuredNames){
        var widths=measuredNames.Where(x=>double.IsFinite(x)&&x>=0).Order().ToArray();
        double typical=widths.Length==0?70:widths[(int)Math.Ceiling(widths.Length*.75)-1];
        return Math.Clamp(Math.Ceiling(typical+82),168,256);
    }
    public static (double Width,double Height) FlyoutSize(int rows,int screens,double widestRatio=16.0/9,double applicationWidth=180){
        rows=Math.Clamp(rows,0,10000);screens=Math.Clamp(screens,1,100);
        double height=Math.Clamp(62+Math.Max(38+rows*44,screens*104),210,400);
        // Width follows the two actual columns, never the height of the window list.
        double width=Math.Clamp(applicationWidth+30+Math.Max(104,14+64*Ratio(widestRatio))+24,290,620);
        return (Math.Round(width),Math.Round(height));
    }
    public static double Ratio(double ratio)=>double.IsFinite(ratio)&&ratio>0?Math.Clamp(ratio,.2,10):16.0/9;
    public static double Ratio(RECT bounds)=>bounds.Width>0&&bounds.Height>0?Ratio((double)bounds.Width/bounds.Height):16.0/9;
    public static (double Width,double Height) ScreenPreview(double ratio,double height,double maximumWidth=double.PositiveInfinity){
        ratio=Ratio(ratio);height=Math.Max(1,Math.Min(height,maximumWidth/ratio));
        return (height*ratio,height);
    }
    public static (double Width,int Columns) GalleryGrid(double viewportWidth) {
        double available=Math.Max(1,viewportWidth-28);
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
