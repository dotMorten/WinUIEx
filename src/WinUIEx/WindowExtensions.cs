using Microsoft.UI;
using Microsoft.UI.Windowing;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT;

namespace WinUIEx
{
    /// <summary>
    /// WinUI Window Extension Methods
    /// </summary>
    public static class WindowExtensions
    {
        /// <summary>
        /// Gets the AppWindow from the handle
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        public static AppWindow GetAppWindow(this Microsoft.UI.Xaml.Window window)
        {
            if (window is WindowEx wex)
                return wex.AppWindow; //Use cached version
            return GetAppWindowFromWindowHandle(window.GetWindowHandle());
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
        /// <para><c>false</c> if the window was not brought to the foreground.</para>
        /// </returns>
        public static bool SetForegroundWindow(this Microsoft.UI.Xaml.Window window) => HwndExtensions.SetForegroundWindow(window.GetWindowHandle());

        /// <summary>
        /// Configures whether the window should always be displayed on top of other windows or not
        /// </summary>
        /// <param name="window">Window</param>
        /// <param name="enable">Whether to display on top</param>
        public static void SetAlwaysOnTop(this Microsoft.UI.Xaml.Window window, bool enable) => window.UpdateWindowConfiguration((c) => c.IsAlwaysOnTop = enable);

        /// <summary>
        /// Sets the parent window for the window
        /// </summary>
        /// <param name="window">Window to assign an owner to.</param>
        /// <param name="parent">Parent</param>
        public static void SetOwnerWindow(this Microsoft.UI.Xaml.Window window, Microsoft.UI.Xaml.Window parent) => window.UpdateWindowConfiguration((c) => c.OwnerWindowId = parent.GetAppWindow().Id);

        /// <summary>
        /// Enables or disables the window frame.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="enable"></param>
        public static void SetHasFrame(this Microsoft.UI.Xaml.Window window, bool enable) => window.UpdateWindowConfiguration((c) => c.HasFrame = enable);

        /// <summary>
        /// Enables or disables the title bar.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="enable"></param>
        public static void SetHasTitleBar(this Microsoft.UI.Xaml.Window window, bool enable) => window.UpdateWindowConfiguration((c) => c.HasTitleBar = enable);

        /// <summary>
        /// Enables or disables the maximize button.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="enable"></param>
        public static void SetIsMaximizable(this Microsoft.UI.Xaml.Window window, bool enable) => window.UpdateWindowConfiguration((c) => c.IsMaximizable = enable);

        /// <summary>
        /// Enables or disables the minimize button
        /// </summary>
        /// <param name="window"></param>
        /// <param name="enable"></param>
        public static void SetIsMinimizable(this Microsoft.UI.Xaml.Window window, bool enable) => window.UpdateWindowConfiguration((c) => c.IsMinimizable = enable);

        /// <summary>
        /// Sets a value defining whether this is a modal window or not.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="enable"></param>
        public static void SetIsModal(this Microsoft.UI.Xaml.Window window, bool enable) => window.UpdateWindowConfiguration((c) => c.IsModal = enable);

        /// <summary>
        /// Enables or disables the ability to resize the window.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="enable"></param>
        public static void SetIsResizable(this Microsoft.UI.Xaml.Window window, bool enable) => window.UpdateWindowConfiguration((c) => c.IsResizable = enable);

        /// <summary>
        /// Enables or disables showing the window in the switchers.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="enable"></param>
        public static void SetIsShownInSwitchers(this Microsoft.UI.Xaml.Window window, bool enable) => window.UpdateWindowConfiguration((c) => c.IsShownInSwitchers = enable);

        /// <summary>
        /// Sets the icon for the window, using the specified icon path.
        /// </summary>
        /// <param name="window">Window</param>
        /// <param name="iconPath">The path of the icon.</param>
        public static void SetIcon(this Microsoft.UI.Xaml.Window window, string iconPath) => HwndExtensions.SetIcon(window.GetWindowHandle(), iconPath);

        /// <summary>
        /// Sets the icon for the window, using the specified icon ID.
        /// </summary>
        /// <param name="window">Window</param>
        /// <param name="iconId">The ID of the icon.</param>
        public static void SetIcon(this Microsoft.UI.Xaml.Window window, IconId iconId) => HwndExtensions.SetIcon(window.GetWindowHandle(), iconId);

        private static void UpdateWindowConfiguration(this Microsoft.UI.Xaml.Window window, Action<AppWindowConfiguration> action)
        {
            var appwindow = window.GetAppWindow();
            var config = appwindow.Configuration;
            action(config);
            appwindow.ApplyConfiguration(config);
        }

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
        public static void MoveAndResize(this Microsoft.UI.Xaml.Window window, double x, double y, double width, double height)
            => window.GetAppWindow().MoveAndResize(new Windows.Graphics.RectInt32((int)x, (int)y, (int)width, (int)height)); // TODO: Adjust for dpi
            //=> HwndExtensions.SetWindowPositionAndSize(window.GetWindowHandle(), x, y, width, height);

        /// <summary>
        /// Sets the width and height of the window in device-independent pixels.
        /// </summary>
        /// <param name="window">Window to set the size for.</param>
        /// <param name="width">Width of the window in device-independent units.</param>
        /// <param name="height">Height of the window in device-independent units.</param>
        public static void SetWindowSize(this Microsoft.UI.Xaml.Window window, double width, double height)
            => window.GetAppWindow().Resize(new Windows.Graphics.SizeInt32((int)width, (int)height)); // TODO: Adjust for dpi
                                                                                                      //=> HwndExtensions.SetWindowSize(GetWindowHandle(window), width, height);

        /// <summary>
        /// Sets the window presenter kind used.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="kind"></param>
        /// <returns></returns>
        public static bool TrySetWindowPresenter(this Microsoft.UI.Xaml.Window window, AppWindowPresenterKind kind) => window.GetAppWindow().TrySetPresenter(kind);

        /// <summary>
        /// Sets the window style for the window.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="style"></param>
        public static void SetWindowStyle(this Microsoft.UI.Xaml.Window window, WindowStyle style)
        {
            switch(style)
            {
                case WindowStyle.Default:
                    window.GetAppWindow().ApplyConfiguration(AppWindowConfiguration.CreateDefault()); break;
                case WindowStyle.ContextMenu:
                    window.GetAppWindow().ApplyConfiguration(AppWindowConfiguration.CreateForContextMenu()); break;
                case WindowStyle.Dialog:
                    window.GetAppWindow().ApplyConfiguration(AppWindowConfiguration.CreateDefault()); break;
                case WindowStyle.ToolWindow:
                    window.GetAppWindow().ApplyConfiguration(AppWindowConfiguration.CreateForToolWindow()); break;
            }    
        }

        /// <summary>
        /// The window style used with <see cref="SetWindowStyle(Microsoft.UI.Xaml.Window, WindowStyle)"/>
        /// </summary>
        public enum WindowStyle
        {
            /// <summary>
            /// Default
            /// </summary>
            Default, 

            /// <summary>
            /// Context menu
            /// </summary>
            ContextMenu,

            /// <summary>
            /// Dialog
            /// </summary>
            Dialog,

            /// <summary>
            /// Tool window
            /// </summary>
            ToolWindow
        }

        /// <summary>
        /// Gets the native HWND pointer handle for the window
        /// </summary>
        /// <param name="window">The window to return the handle for</param>
        /// <returns>HWND handle</returns>
        public static IntPtr GetWindowHandle(this Microsoft.UI.Xaml.Window window)
            => window is null ? throw new ArgumentNullException(nameof(window)) : WinRT.Interop.WindowNative.GetWindowHandle(window);

        [DllImport("Microsoft.Internal.FrameworkUdk.dll", EntryPoint = "Windowing_GetWindowHandleFromWindowId", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetWindowHandleFromWindowId(WindowId windowId, out IntPtr result);

        /// <summary>
        /// Gets the window HWND handle from a Window ID.
        /// </summary>
        /// <param name="windowId">Window ID to get handle from</param>
        /// <returns>Window HWND handle</returns>
        public static IntPtr GetWindowHandle(this WindowId windowId)
        {
            IntPtr hwnd;
            GetWindowHandleFromWindowId(windowId, out hwnd);
            return hwnd;
        }

        [DllImport("Microsoft.Internal.FrameworkUdk.dll", EntryPoint = "Windowing_GetWindowIdFromWindowHandle", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetWindowIdFromWindowHandle(IntPtr hwnd, out WindowId result);

        /// <summary>
        /// Gets the AppWindow from an HWND
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns>AppWindow</returns>
        public static AppWindow GetAppWindowFromWindowHandle(IntPtr hwnd)
        {
            WindowId windowId;
            GetWindowIdFromWindowHandle(hwnd, out windowId);
            return AppWindow.GetFromWindowId(windowId);
        }

        /// <summary>
        /// Activates the window and displays it in its current size and position.
        /// </summary>
        /// <param name="window">Window</param>
        /// <returns><c>true</c> if the window was previously visible, or <c>false</c> if the window was previously hidden.</returns>
        public static bool ShowWindow(this Microsoft.UI.Xaml.Window window) => HwndExtensions.ShowWindow(GetWindowHandle(window));

        /// <summary>
        /// Hides the window and activates another window.
        /// </summary>
        /// <param name="window">Window</param>
        /// <returns><c>true</c> if the window was previously visible, or <c>false</c> if the window was previously hidden.</returns>
        public static bool HideWindow(this Microsoft.UI.Xaml.Window window) => HwndExtensions.HideWindow(GetWindowHandle(window));

        /// <summary>
        /// Maximizes the specified window.
        /// </summary>
        /// <param name="window">Window</param>
        /// <returns><c>true</c> if the window was previously visible, or <c>false</c> if the window was previously hidden.</returns>
        public static bool MaximizeWindow(this Microsoft.UI.Xaml.Window window) => HwndExtensions.MaximizeWindow(GetWindowHandle(window));

        /// <summary>
        /// Minimizes the specified window and activates the next top-level window in the Z order.
        /// </summary>
        /// <param name="window">Window</param>
        /// <returns><c>true</c> if the window was previously visible, or <c>false</c> if the window was previously hidden.</returns>
        public static bool MinimizeWindow(this Microsoft.UI.Xaml.Window window) => HwndExtensions.MinimizeWindow(GetWindowHandle(window));

        /// <summary>
        /// Activates and displays the window. If the window is minimized or maximized, the system restores
        /// it to its original size and position. An application should specify this flag when restoring a minimized window.
        /// </summary>
        /// <param name="window">Window</param>
        /// <returns><c>true</c> if the window was previously visible, or <c>false</c> if the window was previously hidden.</returns>
        public static bool RestoreWindow(this Microsoft.UI.Xaml.Window window) => HwndExtensions.RestoreWindow(GetWindowHandle(window));

        /// <summary>
        /// Sets the task bar icon to the provided icon
        /// </summary>
        /// <param name="window">Window</param>
        /// <param name="icon">Icon</param>
        public static void SetTaskBarIcon(this Microsoft.UI.Xaml.Window window, Icon? icon) => HwndExtensions.SetTaskBarIcon(window.GetWindowHandle(), icon);
    }
}
