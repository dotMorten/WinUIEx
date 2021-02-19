using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Windows.Sdk;
using WinRT;

namespace WinUIEx
{
    /// <summary>
    /// WinUI Window Extension Methods
    /// </summary>
    public static class WindowExtensions
    {       
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
        internal interface IWindowNative
        {
            IntPtr WindowHandle { get; }
        }

        /// <summary>Returns the dots per inch (dpi) value for the associated window.</summary>
        /// <param name = "window">The window you want to get information about.</param>
        /// <returns>The DPI for the window which depends on the <a href = "/windows/desktop/api/windef/ne-windef-dpi_awareness">DPI_AWARENESS</a> of the window. See the Remarks for more information. An invalid <i>hwnd</i> value will result in a return value of 0.</returns>
        /// <remarks>
        /// <para><see href = "https://docs.microsoft.com/windows/win32/api//winuser/nf-winuser-getdpiforwindow">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        public static uint GetDpiForWindow(this Microsoft.UI.Xaml.Window window) => HwndExtensions.GetDpiForWindow(window.GetWindowHandle());

        /// <summary>Brings the thread that created the specified window into the foreground and activates the window.</summary>
        /// <param name="window">
        /// <para>The window that should be activated and brought to the foreground.</para>
        /// </param>
        /// <returns>
        /// <para><c>true</c> if the window was brought to the foreground.</para>
        /// <para><c>false</c> if the window was not brought to the foreground, the return value is zero.</para>
        /// </returns>
        public static bool SetForegroundWindow(this Microsoft.UI.Xaml.Window window) => HwndExtensions.SetForegroundWindow(window.GetWindowHandle());

        /// <summary>
        /// Configures whether the window should always be displayed on top of other windows or not
        /// </summary>
        /// <param name="window">Window</param>
        /// <param name="enable">Whether to display on top</param>
        /// <returns><c>true</c> it the function succeeds</returns>
        public static void SetAlwaysOnTop(this Microsoft.UI.Xaml.Window window, bool enable)
            => HwndExtensions.SetAlwaysOnTop(GetWindowHandle(window), enable);

        /// <summary>
        /// Centers the window on the current monitor
        /// </summary>
        /// <param name="window">Window</param>
        /// <param name="width">Width of the window in device independent pixels, or <c>null</c> if keeping the current size</param>
        /// <param name="height">Height of the window in device independent pixels, or <c>null</c> if keeping the current size</param>
        public static void CenterOnScreen(this Microsoft.UI.Xaml.Window window, double? width = null, double? height = null)
            => HwndExtensions.CenterOnScreen(window.GetWindowHandle(), width, height);

        /// <summary>
        /// Positions and resizes the window
        /// </summary>
        /// <param name="window">Window</param>
        /// <param name="x">Left side of the window in device independent pixels</param>
        /// <param name="y">Top  side of the window in device independent pixels</param>
        /// <param name="width">Width of the window in device independent pixels, or <c>null</c> if keeping the current size</param>
        /// <param name="height">Height of the window in device independent pixels, or <c>null</c> if keeping the current size</param>
        public static void SetWindowPositionAndSize(this Microsoft.UI.Xaml.Window window, double x, double y, double width, double height) 
            => HwndExtensions.SetWindowPositionAndSize(window.GetWindowHandle(), x, y, width, height);

        /// <summary>
        /// Sets the width and height of the window in device-independent pixels.
        /// </summary>
        /// <param name="window">Window to set the size for.</param>
        /// <param name="width">Width of the window in device-independent units.</param>
        /// <param name="height">Height of the window in device-independent units.</param>
        public static void SetWindowSize(this Microsoft.UI.Xaml.Window window, double width, double height) 
            => HwndExtensions.SetWindowSize(GetWindowHandle(window), width, height);

        /// <summary>
        /// Gets the native HWND pointer handle for the window
        /// </summary>
        /// <param name="window">The window to return the handle for</param>
        /// <returns>HWND handle</returns>
        public static IntPtr GetWindowHandle(this Microsoft.UI.Xaml.Window window) 
            => window is null ? throw new ArgumentNullException(nameof(window)) : window.As<IWindowNative>().WindowHandle;

        /// <summary>
        /// Activates the window and displays it in its current size and position.
        /// </summary>
        /// <param name="window">Window</param>
        public static void ShowWindow(this Microsoft.UI.Xaml.Window window) => HwndExtensions.ShowWindow(GetWindowHandle(window));

        /// <summary>
        /// Hides the window and activates another window.
        /// </summary>
        /// <param name="window">Window</param>
        public static void HideWindow(this Microsoft.UI.Xaml.Window window) => HwndExtensions.HideWindow(GetWindowHandle(window));

        /// <summary>
        /// Maximizes the specified window.
        /// </summary>
        /// <param name="window">Window</param>
        public static void MaximizeWindow(this Microsoft.UI.Xaml.Window window) => HwndExtensions.MaximizeWindow(GetWindowHandle(window));

        /// <summary>
        /// Minimizes the specified window and activates the next top-level window in the Z order.
        /// </summary>
        /// <param name="window">Window</param>
        public static void MinimizeWindow(this Microsoft.UI.Xaml.Window window) => HwndExtensions.MinimizeWindow(GetWindowHandle(window));

        /// <summary>
        /// Activates and displays the window. If the window is minimized or maximized, the system restores
        /// it to its original size and position. An application should specify this flag when restoring a minimized window.
        /// </summary>
        /// <param name="window">Window</param>
        public static void RestoreWindow(this Microsoft.UI.Xaml.Window window) => HwndExtensions.RestoreWindow(GetWindowHandle(window));
    }
}
