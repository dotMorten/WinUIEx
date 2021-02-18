using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Windows.Sdk;
using WinRT;
using WinUIExtensions.Windowing;

namespace WinUIEx
{
    public static class WindowExtensions
    {
        
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
        internal interface IWindowNative
        {
            IntPtr WindowHandle { get; }
        }

        private const uint MONITOR_DEFAULTTONULL = 0x00000000;
        private const uint MONITOR_DEFAULTTOPRIMARY = 0x00000001; // Returns a handle to the primary display monitor.
        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002; // Returns a handle to the display monitor that is nearest to the window.

        public static uint GetDpiForWindow(IntPtr hwnd)
        {
            var hwndDesktop = new HWND(PInvoke.MonitorFromWindow(new(hwnd), MONITOR_DEFAULTTONEAREST));
            return PInvoke.GetDpiForWindow(new HWND(hwndDesktop));
        }

        public static uint GetDpiForWindow(this Microsoft.UI.Xaml.Window window) => GetDpiForWindow(window.GetWindowHandle());

        public static bool SetForegroundWindow(IntPtr hwnd) => PInvoke.SetForegroundWindow(new HWND(hwnd));

        public static bool SetForegroundWindow(this Microsoft.UI.Xaml.Window window) => SetForegroundWindow(window.GetWindowHandle());

        public static IntPtr GetActiveWindow()
        {
            return (IntPtr)PInvoke.GetActiveWindow().Value;
        }

        public static IntPtr GetDesktopWindow()
        {
            return (IntPtr)PInvoke.GetDesktopWindow().Value;
        }

        public static bool SetAlwaysOnTop(this Microsoft.UI.Xaml.Window window, bool enable)
            => SetAlwaysOnTop(GetWindowHandle(window), enable);

        public static bool SetAlwaysOnTop(IntPtr hwnd, bool enable)
            => PInvoke.SetWindowPos(new HWND(hwnd), new HWND(enable ? -1 : -2), 0, 0, 0, 0, (uint) (SetWindowPos_Flags.SWP_NOSIZE | SetWindowPos_Flags.SWP_NOMOVE));

        /*public static IntPtr UpdateLayeredWindow(IntPtr hwnd)
        {
            var h = new HWND((nint)hwnd);
            PInvoke.UpdateLayeredWindow(h, )
        }*/
        public static void CenterOnScreen(IntPtr hwnd, double? width = null, double? height = null)
        {
            var hwndDesktop = new HWND(PInvoke.MonitorFromWindow(new(hwnd), MONITOR_DEFAULTTONEAREST));
            MONITORINFO info = new MONITORINFO();
            info.cbSize = 40;
            PInvoke.GetMonitorInfo(hwndDesktop, ref info);
            var dpi = PInvoke.GetDpiForWindow(new HWND(hwnd));
            PInvoke.GetWindowRect(new HWND(hwnd), out RECT windowRect);
            var scalingFactor = dpi / 96d;
            var w = width.HasValue ? (int)(width * scalingFactor) : windowRect.right - windowRect.left;
            var h = height.HasValue ? (int)(height * scalingFactor) : windowRect.bottom - windowRect.top;
            var cx = (info.rcMonitor.left + info.rcMonitor.right) / 2;
            var cy = (info.rcMonitor.bottom + info.rcMonitor.top) / 2;
            var left = cx - (w / 2);
            var top = cy - (h / 2);
            PInvoke.SetWindowPos(new HWND(hwnd), new HWND(), left, top, w, h, (uint)SetWindowPos_Flags.SWP_SHOWWINDOW);
        }
        public static void CenterOnScreen(this Microsoft.UI.Xaml.Window window, double? width = null, double? height = null)
            => CenterOnScreen(window.GetWindowHandle(), width, height);

        public static void SetWindowPositionAndSize(IntPtr hwnd, double x, double y, double width, double height)
        {
            var dpi = GetDpiForWindow(hwnd);
            var scalingFactor = dpi / 96d;
            PInvoke.SetWindowPos(new HWND(hwnd), new HWND(0), (int)(x * scalingFactor), (int)(y * scalingFactor), (int)(width * scalingFactor), (int)(height * scalingFactor), (uint)SetWindowPos_Flags.SWP_NOSENDCHANGING);
        }
        public static void SetWindowPositionAndSize(this Microsoft.UI.Xaml.Window window, double x, double y, double width, double height) 
            => SetWindowPositionAndSize(window.GetWindowHandle(), x, y, width, height);

        public static void SetWindowSize(IntPtr hwnd, double width, double height)
        {
            var dpi = GetDpiForWindow(hwnd);
            var scalingFactor = dpi / 96d;
            PInvoke.SetWindowPos(new HWND(hwnd), new HWND(0), 0, 0, (int)(width * scalingFactor), (int)(height * scalingFactor),
                (uint)(SetWindowPos_Flags.SWP_NOREPOSITION | SetWindowPos_Flags.SWP_NOSENDCHANGING));
        }

        public static void SetWindowSize(this Microsoft.UI.Xaml.Window window, double width, double height) 
            => SetWindowSize(GetWindowHandle(window), width, height);

        public static IntPtr GetWindowHandle(this Microsoft.UI.Xaml.Window window) => window.As<IWindowNative>().WindowHandle;
        
    }
}
