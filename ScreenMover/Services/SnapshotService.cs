using System.Diagnostics;
using System.Runtime.InteropServices;
using ScreenMover.Interop;
using static ScreenMover.Interop.CaptureNative;
namespace ScreenMover.Services;

public sealed record SnapshotFrame(int Width,int Height,byte[] Pixels);
public static class SnapshotService {
const int PreviewWidth=360;
[DllImport("user32.dll")] static extern bool DrawIconEx(nint dc,int x,int y,nint icon,int w,int h,uint step,nint brush,uint flags);
public static SnapshotFrame? Icon(nint icon){
if(icon==0)return null;
var dc=GetDC(0);
try{
using var surface=new Surface(dc,48,48);
var pixels=new byte[48*48*4];Marshal.Copy(pixels,0,surface.Bits,pixels.Length);
if(!DrawIconEx(surface.Dc,0,0,icon,48,48,0,0,3))return null;
Marshal.Copy(surface.Bits,pixels,0,pixels.Length);
if(!pixels.Where((_,i)=>i%4==3).Any(a=>a!=0))for(int i=3;i<pixels.Length;i+=4)pixels[i]=255;
return new(48,48,pixels);
}finally{ReleaseDC(0,dc);}
}
sealed class Surface : IDisposable {
public nint Dc,Bitmap,Bits,old;
public Surface(nint reference,int width,int height) {
Dc=CreateCompatibleDC(reference);
var info=new BitmapInfo{Size=40,Width=width,Height=-height,Planes=1,BitCount=32};
Bitmap=CreateDIBSection(reference,ref info,0,out Bits,0,0);
if(Dc==0||Bitmap==0){Dispose();throw new InvalidOperationException("无法创建截图");}
old=SelectObject(Dc,Bitmap);
}
public void Dispose(){if(Dc!=0&&old!=0)SelectObject(Dc,old);if(Bitmap!=0)DeleteObject(Bitmap);if(Dc!=0)DeleteDC(Dc);Dc=Bitmap=0;}
}
static SnapshotFrame Reduce(nint source,int x,int y,int width,int height) {
int w=PreviewWidth,h=Math.Clamp((int)((double)height/width*w),1,360);
if(height>width){h=360;w=Math.Max(1,(int)((double)width/height*h));}
using var dest=new Surface(source,w,h);SetStretchBltMode(dest.Dc,4);
if(!StretchBlt(dest.Dc,0,0,w,h,source,x,y,width,height,0x00CC0020))throw new InvalidOperationException("截图不可用");
var bytes=new byte[w*h*4];Marshal.Copy(dest.Bits,bytes,0,bytes.Length);
for(int i=3;i<bytes.Length;i+=4)bytes[i]=255;
return new(w,h,bytes);
}
public static SnapshotFrame Screen(Native.RECT bounds) {
if(bounds.Width<=0||bounds.Height<=0)throw new InvalidOperationException("显示器未启用");
var dc=GetDC(0);if(dc==0)throw new InvalidOperationException("无法读取桌面");
try {return Reduce(dc,bounds.Left,bounds.Top,bounds.Width,bounds.Height);}finally{ReleaseDC(0,dc);}
}
// Run PrintWindow in an isolated process: a hung target cannot retain a worker thread or block the UI.
public static async Task<SnapshotFrame?> WindowAsync(nint hwnd,int pid,CancellationToken cancellation) {
using var process=new Process{StartInfo=new ProcessStartInfo(Environment.ProcessPath!){
UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true}};
process.StartInfo.ArgumentList.Add("--capture-window");
process.StartInfo.ArgumentList.Add(hwnd.ToInt64().ToString());
process.StartInfo.ArgumentList.Add(pid.ToString());
try {
process.Start();
using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancellation);timeout.CancelAfter(2500);
using var data=new MemoryStream();
var copy=process.StandardOutput.BaseStream.CopyToAsync(data,timeout.Token);
await process.WaitForExitAsync(timeout.Token);await copy;
if(process.ExitCode!=0||data.Length<8)return null;
data.Position=0;using var reader=new BinaryReader(data);
int w=reader.ReadInt32(),h=reader.ReadInt32();
if(w<1||w>360||h<1||h>360||data.Length!=8+w*h*4)return null;
return new(w,h,reader.ReadBytes(w*h*4));
} catch(OperationCanceledException) {return null;}
catch {return null;}
finally {try{if(!process.HasExited){process.Kill();await process.WaitForExitAsync();}}catch{}}
}
public static int CaptureWorker(string[] args) {
try {
var hwnd=(nint)long.Parse(args[0]);int pid=int.Parse(args[1]);
Native.GetWindowThreadProcessId(hwnd,out int current);
if(current!=pid||!Native.IsWindow(hwnd)||Native.IsIconic(hwnd))return 2;
if(GetWindowDisplayAffinity(hwnd,out uint affinity)&&affinity!=0)return 3;
if(!Native.GetWindowRect(hwnd,out var r)||r.Width<=0||r.Height<=0||r.Width>8192||r.Height>8192||(long)r.Width*r.Height>24000000)return 4;
var dc=GetDC(0);
try {
using var full=new Surface(dc,r.Width,r.Height);
if(!PrintWindow(hwnd,full.Dc,2))return 5;
var frame=Reduce(full.Dc,0,0,r.Width,r.Height);
if(!frame.Pixels.Where((_,i)=>i%4!=3).Any(b=>b>8))return 6;
using var output=new BinaryWriter(Console.OpenStandardOutput());
output.Write(frame.Width);output.Write(frame.Height);output.Write(frame.Pixels);output.Flush();return 0;
} finally{ReleaseDC(0,dc);}
} catch{return 7;}
}
}
