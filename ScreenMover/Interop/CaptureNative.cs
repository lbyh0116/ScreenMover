using System.Runtime.InteropServices;
namespace ScreenMover.Interop;
internal static class CaptureNative {
[StructLayout(LayoutKind.Sequential)] internal struct BitmapInfo {
public uint Size; public int Width,Height; public ushort Planes,BitCount;
public uint Compression,SizeImage; public int XPels,YPels; public uint ClrUsed,ClrImportant;
}
[DllImport("user32.dll")] internal static extern nint GetDC(nint h);
[DllImport("user32.dll")] internal static extern int ReleaseDC(nint h,nint dc);
[DllImport("user32.dll")] internal static extern bool PrintWindow(nint h,nint dc,uint flags);
[DllImport("user32.dll")] internal static extern bool GetWindowDisplayAffinity(nint h,out uint affinity);
[DllImport("gdi32.dll")] internal static extern nint CreateCompatibleDC(nint dc);
[DllImport("gdi32.dll")] internal static extern nint CreateDIBSection(nint dc,ref BitmapInfo info,uint usage,out nint bits,nint section,uint offset);
[DllImport("gdi32.dll")] internal static extern nint SelectObject(nint dc,nint obj);
[DllImport("gdi32.dll")] internal static extern bool DeleteObject(nint obj);
[DllImport("gdi32.dll")] internal static extern bool DeleteDC(nint dc);
[DllImport("gdi32.dll")] internal static extern bool StretchBlt(nint dest,int x,int y,int w,int h,nint src,int sx,int sy,int sw,int sh,uint rop);
[DllImport("gdi32.dll")] internal static extern int SetStretchBltMode(nint dc,int mode);
}
