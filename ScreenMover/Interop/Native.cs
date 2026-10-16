using System.Runtime.InteropServices;
using System.Text;
namespace ScreenMover.Interop;
public static class Native {
[StructLayout(LayoutKind.Sequential)] public struct LUID { public uint LowPart; public int HighPart; }
[StructLayout(LayoutKind.Sequential)] public struct RATIONAL { public uint Numerator, Denominator; }
[StructLayout(LayoutKind.Sequential)] public struct PathSourceInfo { public LUID adapterId; public uint id, modeInfoIdx, statusFlags; }
[StructLayout(LayoutKind.Sequential)] public struct PathTargetInfo { public LUID adapterId; public uint id,modeInfoIdx,outputTechnology,rotation,scaling; public RATIONAL refreshRate; public uint scanLineOrdering; public int targetAvailable; public uint statusFlags; }
[StructLayout(LayoutKind.Sequential)] public struct PathInfo { public PathSourceInfo sourceInfo; public PathTargetInfo targetInfo; public uint flags; }
[StructLayout(LayoutKind.Sequential, Size=64)] public struct ModeInfo { public uint infoType,id; public LUID adapterId; }
[StructLayout(LayoutKind.Sequential)] public struct DeviceInfoHeader { public uint type,size; public LUID adapterId; public uint id; }
[StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] public struct TargetDeviceName {
public DeviceInfoHeader header; public uint flags,outputTechnology; public ushort edidManufactureId,edidProductCodeId; public uint connectorInstance;
[MarshalAs(UnmanagedType.ByValTStr,SizeConst=64)] public string monitorFriendlyDeviceName;
[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] public string monitorDevicePath; }
[StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] public struct SourceDeviceName {
public DeviceInfoHeader header; [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string viewGdiDeviceName; }
[StructLayout(LayoutKind.Sequential)] public struct RECT {
public int Left,Top,Right,Bottom; public readonly int Width=>Right-Left; public readonly int Height=>Bottom-Top;
public override readonly string ToString()=>$"({Left},{Top}) {Width}×{Height}";
}
[StructLayout(LayoutKind.Sequential)] public struct POINT { public int X,Y; }
[StructLayout(LayoutKind.Sequential)] public struct WINDOWPLACEMENT { public int length,flags,showCmd; public POINT ptMinPosition,ptMaxPosition; public RECT rcNormalPosition; }
[StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] public struct MONITORINFOEX {
public int cbSize; public RECT rcMonitor,rcWork; public uint dwFlags;
[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string szDevice; }
[StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] public struct DISPLAY_DEVICE {
public int cb; [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string DeviceName;
[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] public string DeviceString; public int StateFlags;
[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] public string DeviceID;
[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] public string DeviceKey; }
public delegate bool MonitorEnumProc(nint h,nint dc,ref RECT r,nint data);
public delegate bool EnumWindowsProc(nint h,nint data);
public delegate void WinEventProc(nint hook,uint evt,nint h,int obj,int child,uint thread,uint time);
public delegate nint SubclassProc(nint h,uint msg,nint w,nint l,nuint id,nuint data);
[DllImport("user32.dll")] public static extern int GetDisplayConfigBufferSizes(uint flags,out uint paths,out uint modes);
[DllImport("user32.dll")] public static extern int QueryDisplayConfig(uint flags,ref uint paths,[Out] PathInfo[] p,ref uint modes,[Out] ModeInfo[] m,nint topology);
[DllImport("user32.dll")] public static extern int DisplayConfigGetDeviceInfo(ref TargetDeviceName n);
[DllImport("user32.dll")] public static extern int DisplayConfigGetDeviceInfo(ref SourceDeviceName n);
[DllImport("user32.dll")] public static extern bool EnumDisplayMonitors(nint dc,nint clip,MonitorEnumProc cb,nint data);
[DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern bool GetMonitorInfoW(nint h,ref MONITORINFOEX m);
[DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="EnumDisplayDevicesW")] public static extern bool EnumDisplayDevices(nint device,uint index,ref DISPLAY_DEVICE d,uint flags);
[DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb,nint data);
[DllImport("user32.dll")] public static extern bool IsWindow(nint h);
[DllImport("user32.dll")] public static extern bool IsWindowVisible(nint h);
[DllImport("user32.dll")] public static extern bool IsIconic(nint h);
[DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetWindowTextW(nint h,StringBuilder s,int n);
[DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetClassNameW(nint h,StringBuilder s,int n);
[DllImport("user32.dll")] public static extern int GetWindowTextLengthW(nint h);
[DllImport("user32.dll")] public static extern nint GetWindow(nint h,uint cmd);
[DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] public static extern nint GetWindowLongPtr(nint h,int index);
[DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(nint h,out int pid);
[DllImport("user32.dll",SetLastError=true)] public static extern bool GetWindowPlacement(nint h,ref WINDOWPLACEMENT p);
[DllImport("user32.dll",SetLastError=true)] public static extern bool SetWindowPlacement(nint h,ref WINDOWPLACEMENT p);
[DllImport("user32.dll",SetLastError=true)] public static extern bool SetWindowPos(nint h,nint after,int x,int y,int w,int height,uint flags);
[DllImport("user32.dll")] public static extern bool ShowWindow(nint h,int cmd);
[DllImport("user32.dll",SetLastError=true)] public static extern bool ShowWindowAsync(nint h,int cmd);
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(nint h);
[DllImport("user32.dll")] public static extern bool GetWindowRect(nint h,out RECT r);
[DllImport("user32.dll")] public static extern nint MonitorFromPoint(POINT p,uint flags);
[DllImport("user32.dll")] public static extern nint MonitorFromWindow(nint h,uint flags);
[DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(nint h,uint attr,out int val,int size);
[DllImport("user32.dll")] public static extern nint SetWinEventHook(uint min,uint max,nint module,WinEventProc cb,uint pid,uint thread,uint flags);
[DllImport("user32.dll")] public static extern bool UnhookWinEvent(nint hook);
[DllImport("comctl32.dll")] public static extern bool SetWindowSubclass(nint h,SubclassProc cb,nuint id,nuint data);
[DllImport("comctl32.dll")] public static extern bool RemoveWindowSubclass(nint h,SubclassProc cb,nuint id);
[DllImport("comctl32.dll")] public static extern nint DefSubclassProc(nint h,uint msg,nint w,nint l);
}
