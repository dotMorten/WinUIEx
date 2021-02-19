using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Windows.Sdk;
using WinRT;
using WinUIExtensions.Windowing;

namespace WinUIEx
{
    /// <summary>
    /// A set of HWND Helper Methods
    /// </summary>
    public static class HwndExtensions
    {
        private const uint MONITOR_DEFAULTTONULL = 0x00000000;
        private const uint MONITOR_DEFAULTTOPRIMARY = 0x00000001; // Returns a handle to the primary display monitor.
        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002; // Returns a handle to the display monitor that is nearest to the window.

        /// <summary>Returns the dots per inch (dpi) value for the associated window.</summary>
        /// <param name = "hwnd">The window you want to get information about.</param>
        /// <returns>The DPI for the window which depends on the <a href = "/windows/desktop/api/windef/ne-windef-dpi_awareness">DPI_AWARENESS</a> of the window. See the Remarks for more information. An invalid <i>hwnd</i> value will result in a return value of 0.</returns>
        /// <remarks>
        /// <para><see href = "https://docs.microsoft.com/windows/win32/api//winuser/nf-winuser-getdpiforwindow">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        public static uint GetDpiForWindow(IntPtr hwnd) => PInvoke.GetDpiForWindow(new HWND(hwnd));
        
        /// <summary>
        /// Gets the DPI for the monitor that the Window is on
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns></returns>
        public static uint GetDpiForWindowsMonitor(IntPtr hwnd)
        {
            var hwndDesktop = new HWND(PInvoke.MonitorFromWindow(new(hwnd), MONITOR_DEFAULTTONEAREST));
            return PInvoke.GetDpiForWindow(new HWND(hwndDesktop));
        }

        /// <summary>Brings the thread that created the specified window into the foreground and activates the window.</summary>
        /// <param name="hWnd">
        /// <para>A handle to the window that should be activated and brought to the foreground.</para>
        /// </param>
        /// <returns>
        /// <para><c>true</c> if the window was brought to the foreground.</para>
        /// <para><c>false</c> if the window was not brought to the foreground, the return value is zero.</para>
        /// </returns>
        /// <remarks>
        /// <para><see href = "https://docs.microsoft.com/windows/win32/api//winuser/nf-winuser-setforegroundwindow">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        public static bool SetForegroundWindow(IntPtr hWnd) => PInvoke.SetForegroundWindow(new HWND(hWnd));

        /// <summary>Retrieves the window handle to the active window attached to the calling thread's message queue.</summary>
        /// <returns>
        /// <para>The return value is the handle to the active window attached to the calling thread's message queue. Otherwise, the return value is <b>IntPtr.Zero</b>.</para>
        /// </returns>
        /// <remarks>
        /// <para><see href = "https://docs.microsoft.com/windows/win32/api//winuser/nf-winuser-getactivewindow">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        public static IntPtr GetActiveWindow()
        {
            return (IntPtr)PInvoke.GetActiveWindow().Value;
        }

        /// <summary>Retrieves a handle to the desktop window. The desktop window covers the entire screen. The desktop window is the area on top of which other windows are painted.</summary>
        /// <returns>
        /// <para>The return value is a handle to the desktop window.</para>
        /// </returns>
        /// <remarks>
        /// <para><see href = "https://docs.microsoft.com/windows/win32/api//winuser/nf-winuser-getdesktopwindow">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        public static IntPtr GetDesktopWindow()
        {
            return (IntPtr)PInvoke.GetDesktopWindow().Value;
        }

        /// <summary>
        /// Configures whether the window should always be displayed on top of other windows or not
        /// </summary>
        /// <param name="hwnd">Window handle</param>
        /// <param name="enable">Whether to display on top</param>
        /// <returns><c>true</c> it the function succeeds</returns>
        public static void SetAlwaysOnTop(IntPtr hwnd, bool enable)
            => SetWindowPosOrThrow(new HWND(hwnd), new HWND(enable ? -1 : -2), 0, 0, 0, 0, (uint) (SetWindowPos_Flags.SWP_NOSIZE | SetWindowPos_Flags.SWP_NOMOVE));

        private static void SetWindowPosOrThrow(HWND hWnd, HWND hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags)
        {
            bool result = PInvoke.SetWindowPos(hWnd, hWndInsertAfter, X, Y, cx, cy, uFlags);
            if (!result)
                Marshal.ThrowExceptionForHR(Marshal.GetLastWin32Error());
        }
        /*public static IntPtr UpdateLayeredWindow(IntPtr hwnd)
        {
            var h = new HWND((nint)hwnd);
            PInvoke.UpdateLayeredWindow(h, )
        }*/

        /// <summary>
        /// Centers the window on the current monitor
        /// </summary>
        /// <param name="hwnd">Window handle</param>
        /// <param name="width">Width of the window in device independent pixels, or <c>null</c> if keeping the current size</param>
        /// <param name="height">Height of the window in device independent pixels, or <c>null</c> if keeping the current size</param>
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
            SetWindowPosOrThrow(new HWND(hwnd), new HWND(), left, top, w, h, (uint)SetWindowPos_Flags.SWP_SHOWWINDOW);
        }

        /// <summary>
        /// Positions and resizes the window
        /// </summary>
        /// <param name="hwnd">Window handle</param>
        /// <param name="x">Left side of the window in device independent pixels</param>
        /// <param name="y">Top  side of the window in device independent pixels</param>
        /// <param name="width">Width of the window in device independent pixels, or <c>null</c> if keeping the current size</param>
        /// <param name="height">Height of the window in device independent pixels, or <c>null</c> if keeping the current size</param>
        public static void SetWindowPositionAndSize(IntPtr hwnd, double x, double y, double width, double height)
        {
            var dpi = GetDpiForWindow(hwnd);
            var scalingFactor = dpi / 96d;
            SetWindowPosOrThrow(new HWND(hwnd), new HWND(0), (int)(x * scalingFactor), (int)(y * scalingFactor), (int)(width * scalingFactor), (int)(height * scalingFactor), (uint)SetWindowPos_Flags.SWP_NOSENDCHANGING);
        }

        /// <summary>
        /// Sets the width and height of the window in device-independent pixels
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public static void SetWindowSize(IntPtr hwnd, double width, double height)
        {
            var dpi = GetDpiForWindow(hwnd);
            var scalingFactor = dpi / 96d;
            SetWindowPosOrThrow(new HWND(hwnd), new HWND(0), 0, 0, (int)(width * scalingFactor), (int)(height * scalingFactor),
                (uint)(SetWindowPos_Flags.SWP_NOREPOSITION | SetWindowPos_Flags.SWP_NOSENDCHANGING));
        }

        /// <summary>
        /// Activates the window and displays it in its current size and position.
        /// </summary>
        /// <param name="hwnd">Window handle</param>
        public static void ShowWindow(IntPtr hwnd) => ShowWindowOrThrow(new HWND(hwnd), 5);

        /// <summary>
        /// Hides the window and activates another window.
        /// </summary>
        /// <param name="hwnd">Window handle</param>
        public static void HideWindow(IntPtr hwnd) => ShowWindowOrThrow(new HWND(hwnd), 0);

        /// <summary>
        /// Maximizes the specified window.
        /// </summary>
        /// <param name="hwnd">Window handle</param>
        public static void MaximizeWindow(IntPtr hwnd) => ShowWindowOrThrow(new HWND(hwnd), 3);

        /// <summary>
        /// Minimizes the specified window and activates the next top-level window in the Z order.
        /// </summary>
        /// <param name="hwnd">Window handle</param>
        public static void MinimizeWindow(IntPtr hwnd) => ShowWindowOrThrow(new HWND(hwnd), 6);

        /// <summary>
        /// Activates and displays the window. If the window is minimized or maximized, the system restores
        /// it to its original size and position. An application should specify this flag when restoring a minimized window.
        /// </summary>
        /// <param name="hwnd">Window handle</param>
        public static void RestoreWindow(IntPtr hwnd) => ShowWindowOrThrow(new HWND(hwnd), 9);

        private static void ShowWindowOrThrow(IntPtr hWnd, int nCmdShow)
        {
            bool result = PInvoke.ShowWindow(new HWND(hWnd), nCmdShow);
            if (!result)
                Marshal.ThrowExceptionForHR(Marshal.GetLastWin32Error());
        }
    }
}
