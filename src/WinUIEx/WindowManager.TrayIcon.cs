using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.Storage;
using WinUIEx.Messaging;
using Windows.Win32.UI.WindowsAndMessaging;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace WinUIEx
{
    public partial class WindowManager
    {
        private const uint DefaultTrayIconId = 123;
        private const uint TrayIconCallbackId = 0x8765;

        private bool _isVisibleInTray = false;

        /// <summary>
        /// Gets or sets a value indicating whether the window is shown in the system tray.
        /// </summary>
        /// <remarks>
        /// <para>The system tray icon will use the same icon as Window's Taskbar icon, and tooltip will match the AppWindow.Title value. Double-clicking the icon restores the window if minimized and brings it to the front.</para>
        /// <para>See <see cref="AppWindow.IsShownInSwitchers" /> to hide the window from the Alt+Tab switcher and task bar.
        /// If you want to minimize the window to the tray, set this to <c>true</c> and when  <see cref="WindowManager.WindowStateChanged"/> is fired and changes to minimized,
        /// hide it from the switcher.</para>
        /// <note type="important">
        /// It is important that you assign a task bar icon first before setting this to <c>true</c>, or this method will throw.
        /// See <see cref="AppWindow.SetTaskbarIcon(string)"/> or <see cref="WindowExtensions.SetTaskBarIcon(Window, Icon?)"/>.
        /// </note>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if the TaskBarIcon has not been set</exception>
        /// <seealso cref="TrayIconInvoked"/>
        /// <seealso cref="AppWindow.SetTaskbarIcon(Microsoft.UI.IconId)"/>
        /// <seealso cref="AppWindow.SetTaskbarIcon(string)"/>
        /// <seealso cref="WindowExtensions.SetTaskBarIcon(Window, Icon?)"/>
        public bool IsVisibleInTray
        {
            get => _isVisibleInTray;
            set
            {
                if (_isVisibleInTray != value)
                {
                    _isVisibleInTray = value;
                    if (value)
                    {
                        if (currentIcon == 0)
                            throw new InvalidOperationException("No icon currently assigned to the taskbar. Call AppWindow.SetTaskbarIcon or WindowExtensions.SetTaskBarIcon prior to turning on the tray icon");
                        AddToTray(DefaultTrayIconId);
                    }
                    else
                        RemoveFromTray(DefaultTrayIconId);
                }
            }
        }

        private void AddToTray(uint iconId)
        {
            if (currentIcon == 0) // No icon to add
                return;
            // See https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyicona
            const uint NIM_ADD = 0x00000000;
            // const uint NIM_MODIFY = 0x00000001;
            const uint NIF_MESSAGE = 0x00000001;
            const uint NIF_ICON = 0x00000002;
            const uint NIF_TIP = 0x00000004;
            var hicon = new HICON(currentIcon);
            Windows.Win32.__ushort_128 tip = new Windows.Win32.__ushort_128();
            for (int i = 0; i < 128 && i < AppWindow.Title.Length; i++)
            {
                tip[i] = (ushort)AppWindow.Title[i];
            }

            if (Environment.Is64BitProcess)
            {
                var notifyIconData = new Windows.Win32.NOTIFYICONDATAW64
                {
                    hWnd = new Windows.Win32.Foundation.HWND(_window.GetWindowHandle()),
                    cbSize = (uint)Marshal.SizeOf<Windows.Win32.NOTIFYICONDATAW64>(),
                    uID = iconId,
                    uFlags = NIF_ICON | NIF_MESSAGE | NIF_TIP, // Icon and callback message is set and valid
                    hIcon = hicon,
                    uCallbackMessage = TrayIconCallbackId,
                    szTip = tip
                };
                Windows.Win32.PInvoke.Shell_NotifyIcon(NIM_ADD, notifyIconData);
            }
            else
            {
                var notifyIconData = new Windows.Win32.NOTIFYICONDATAW32
                {
                    hWnd = new Windows.Win32.Foundation.HWND(_window.GetWindowHandle()),
                    cbSize = (uint)Marshal.SizeOf<Windows.Win32.NOTIFYICONDATAW32>(),
                    uID = iconId,
                    uFlags = NIF_ICON | NIF_MESSAGE | NIF_TIP, // Icon and callback message is set and valid
                    hIcon = hicon,
                    uCallbackMessage = TrayIconCallbackId,
                    szTip = tip
                };
                Windows.Win32.PInvoke.Shell_NotifyIcon(NIM_ADD, notifyIconData);
            }
        }

        private void RemoveFromTray(uint iconId)
        {
            const uint NIM_DELETE = 0x00000002;
            if (Environment.Is64BitProcess)
            {
                var notifyIconData = new Windows.Win32.NOTIFYICONDATAW64
                {
                    hWnd = new Windows.Win32.Foundation.HWND(_window.GetWindowHandle()),
                    cbSize = (uint)Marshal.SizeOf<Windows.Win32.NOTIFYICONDATAW64>(),
                    uID = iconId,
                };
                Windows.Win32.PInvoke.Shell_NotifyIcon(NIM_DELETE, notifyIconData);
            }
            else
            {
                var notifyIconData = new Windows.Win32.NOTIFYICONDATAW32
                {
                    hWnd = new Windows.Win32.Foundation.HWND(_window.GetWindowHandle()),
                    cbSize = (uint)Marshal.SizeOf<Windows.Win32.NOTIFYICONDATAW32>(),
                    uID = iconId,
                };
                Windows.Win32.PInvoke.Shell_NotifyIcon(NIM_DELETE, notifyIconData);
            }
        }

        private void ProcessTrayIconEvents(Message message)
        {
            var iconid = (uint)message.WParam;
            if (iconid != DefaultTrayIconId)
                return;
            switch ((WindowsMessages)(message.LParam & 0xffff))
            {
                case WindowsMessages.WM_LBUTTONDBLCLK:
                    HandleTrayIconClick(TrayIconInvokeType.LeftDoubleClick);
                    break;
                case WindowsMessages.WM_RBUTTONDBLCLK:
                    HandleTrayIconClick(TrayIconInvokeType.RightDoubleClick);
                    break;
                case WindowsMessages.WM_RBUTTONUP:
                    HandleTrayIconClick(TrayIconInvokeType.RightMouseUp);
                    break;
                case WindowsMessages.WM_RBUTTONDOWN:
                    HandleTrayIconClick(TrayIconInvokeType.RightMouseDown);
                    break;
                case WindowsMessages.WM_LBUTTONUP:
                    HandleTrayIconClick(TrayIconInvokeType.LeftMouseUp);
                    break;
                case WindowsMessages.WM_LBUTTONDOWN:
                    HandleTrayIconClick(TrayIconInvokeType.LeftMouseDown);
                    break;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NOTIFYICONIDENTIFIER
        {
            public uint cbSize;
            public nint hWnd;
            public uint uID;
            public Guid guidItem;
        }
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int Shell_NotifyIconGetRect([In] ref NOTIFYICONIDENTIFIER identifier, [Out] out Windows.Graphics.RectInt32 iconLocation);

        private void HandleTrayIconClick(TrayIconInvokeType type)
        {
            bool handled = false;
            if (TrayIconInvoked is EventHandler<TrayIconInvokedEventArgs> handler)
            {
                var args = new TrayIconInvokedEventArgs(type);
                handler.Invoke(this, args);
                if (args.Flyout is FlyoutBase flyout)
                {
                    var icon = new NOTIFYICONIDENTIFIER()
                    {
                        uID = DefaultTrayIconId,
                        hWnd = _window.GetWindowHandle(),
                        cbSize = (uint)Marshal.SizeOf<NOTIFYICONIDENTIFIER>(),
                    };
                    var hresult = Shell_NotifyIconGetRect(ref icon, out var location);
                    if (hresult == 0)
                    {
                        var w = new TrayIconWindow(flyout);
                        w.ShowAt(location.X, location.Y);
                    }
                }
                handled = args.Handled;
            }
            if (!handled && type == TrayIconInvokeType.LeftDoubleClick)
            {
                // Default action
                // If icon was double-clicked, restore the window and bring to front
                if (_windowState == WindowState.Minimized)
                {
                    WindowExtensions.Restore(_window);
                }
                WindowExtensions.SetForegroundWindow(_window);
            }
        }

        private class TrayIconWindow : Window
        {
            private WindowManager manager;
            private readonly FlyoutBase flyout;

            public TrayIconWindow(FlyoutBase flyout)
            {
                manager = WindowManager.Get(this);
                manager.MinHeight = 0;
                manager.MinWidth = 0;
                WindowExtensions.SetWindowStyle(this, WindowStyle.Popup);
                AppWindow.IsShownInSwitchers = false;

                this.Closed += TrayIconWindow_Closed;
                this.Content = new Microsoft.UI.Xaml.Controls.Grid();
                ((FrameworkElement)this.Content).Loaded += TrayIconWindow_Loaded;
                this.flyout = flyout;
                flyout.Closing += Flyout_Closing;
                manager.WindowMessageReceived += Manager_WindowMessageReceived;
            }

            internal void ShowAt(int x, int y)
            {
                Activate();
                AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, 0, 0), Microsoft.UI.Windowing.DisplayArea.GetFromPoint(new Windows.Graphics.PointInt32(0, 0), Microsoft.UI.Windowing.DisplayAreaFallback.Primary));
                WindowExtensions.SetForegroundWindow(this);
            }

            private void Flyout_Closing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
            {
                Close();
            }

            private void TrayIconWindow_Loaded(object sender, RoutedEventArgs e)
            {
                flyout.ShouldConstrainToRootBounds = false;
                flyout.ShowAt((FrameworkElement)this.Content, new FlyoutShowOptions()
                {
                    ShowMode = FlyoutShowMode.Auto,
                    Placement = FlyoutPlacementMode.Auto,
                    Position = new Point(0, 0)
                });
            }

            private void TrayIconWindow_Closed(object sender, WindowEventArgs args)
            {
                manager.WindowMessageReceived -= Manager_WindowMessageReceived;
                ((FrameworkElement)this.Content).Loaded -= TrayIconWindow_Loaded;
                flyout.Closing -= Flyout_Closing;
                Closed -= TrayIconWindow_Closed;
                Content = null;
            }

            private void Manager_WindowMessageReceived(object? sender, WindowMessageEventArgs e)
            {
                if (e.MessageType == WindowsMessages.WM_ACTIVATE)
                {
                    if (e.Message.WParam == 0) // Window lost focus
                    {
                        this.DispatcherQueue.TryEnqueue(() => Hide());
                    }
                }
            }
            private void Hide()
            {
                flyout.Hide();
            }
        }

        /// <summary>
        /// Raised when the user invokes the trayicon by clicking or accessing via keyboard
        /// </summary>
        /// <seealso cref="IsVisibleInTray"/>
        public event EventHandler<TrayIconInvokedEventArgs>? TrayIconInvoked;
    }

    /// <summary>
    /// The event arguments for the <see cref="WindowManager.TrayIconInvoked"/> event.
    /// </summary>
    public class TrayIconInvokedEventArgs : EventArgs
    {
        internal TrayIconInvokedEventArgs(TrayIconInvokeType type)
        {
            Type = type;
        }

        /// <summary>
        /// Gets the way the tray icon was invoked.
        /// </summary>
        public TrayIconInvokeType Type { get; }

        /// <summary>
        /// Gets or sets a flyout to display by the trayicon
        /// </summary>
        public FlyoutBase? Flyout { get; set; }

        /// <summary>
        /// Set to true to avoid any default behavior
        /// </summary>
        /// <remarks>
        /// When the type is <see cref="TrayIconInvokeType.LeftDoubleClick"/>
        /// the window is restored and brought to the front. By marking this event
        /// handled, this default behavior will be disabled.
        /// </remarks>
        public bool Handled { get; set; }
    }

    /// <summary>
    /// Describes the way the tray icon was interacted with
    /// </summary>
    /// <seealso cref="WindowManager.TrayIconInvoked"/>
    /// <seealso cref="WindowManager.IsVisibleInTray"/>
    public enum TrayIconInvokeType
    {
        /// <summary>
        /// User moused down on the tray icon using the primary button.
        /// </summary>
        LeftMouseDown,

        /// <summary>
        /// User moused down on the tray icon using the secondary button.
        /// </summary>
        RightMouseDown,

        /// <summary>
        /// User released the primary mouse button on the tray icon.
        /// </summary>
        LeftMouseUp,

        /// <summary>
        /// User released the secondary mouse button on the tray icon.
        /// </summary>
        RightMouseUp,

        /// <summary>
        /// User double-clicked the primary mouse button on the tray icon.
        /// </summary>
        LeftDoubleClick,

        /// <summary>
        /// User double-clicked the secondary mouse button on the tray icon.
        /// </summary>
        RightDoubleClick,
    }
}
