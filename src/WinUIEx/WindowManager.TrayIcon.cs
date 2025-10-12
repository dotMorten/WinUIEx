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

        private TrayIcon? _trayIcon;

        /// <summary>
        /// Gets or sets a value indicating whether the window is shown in the system tray.
        /// </summary>
        /// <remarks>
        /// <para>The system tray icon will use the same icon as Window's Taskbar icon, and tooltip will match the AppWindow.Title value. Double-clicking the icon restores the window if minimized and brings it to the front.</para>
        /// <para>See <see cref="AppWindow.IsShownInSwitchers" /> to hide the window from the Alt+Tab switcher and task bar.
        /// If you want to minimize the window to the tray, set this to <c>true</c> and when  <see cref="WindowManager.WindowStateChanged"/> is fired and changes to minimized,
        /// hide it from the switcher.</para>
        /// <note type="tip">
        /// The taskbar icon will be used for the tray icon. You can update the taskbar icon by calling <see cref="AppWindow.SetTaskbarIcon(string)">AppWindow.SetTaskbarIcon(string)</see>.
        /// </note>
        /// <para>
        /// For more advanced scenarios where more control is needed over the tray, multiple icons or managing tooltip and icons separately
        /// from the window, see the <see cref="TrayIcon"/> class.
        /// </para>
        /// </remarks>
        /// <seealso cref="TrayIcon"/>
        /// <seealso cref="AppWindow.SetTaskbarIcon(Microsoft.UI.IconId)"/>
        /// <seealso cref="AppWindow.SetTaskbarIcon(string)"/>
        /// <seealso cref="WindowExtensions.SetTaskBarIcon(Window, Icon?)"/>
        /// <seealso cref="TrayIconId"/>
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
                        AddToTray(DefaultTrayIconId);
                    }
                    else
                        RemoveFromTray(DefaultTrayIconId);
                }
            }
        }

        private uint _trayIconId = uint.MaxValue - 1;

        /// <summary>
        /// Gets or sets a unique identifier for the tray icon.
        /// </summary>
        public uint TrayIconId
        {
            get { return _trayIconId; }
            set
            {
                if (_trayIconId != value)
                {
                    if (_isVisibleInTray)
                        RemoveFromTray(_trayIconId);
                    _trayIconId = value;
                    if (_isVisibleInTray)
                        AddToTray(_trayIconId);
                }
            }
        }

        private void AddToTray(uint iconId)
        {
            if (_trayIcon is null)
            {
                var icon = GetCurrentIcon();
                _trayIcon = new TrayIcon(iconId, icon, AppWindow.Title);
                _trayIcon.LeftDoubleClick += TrayIcon_LeftDoubleClick;
                _trayIcon.LeftClick += TrayIcon_LeftClick;
                _trayIcon.RightClick += TrayIcon_RightClick;
            }
            _trayIcon.IsVisible = true;
        }

        private void RemoveFromTray(uint iconId)
        {
            if (_trayIcon is not null)
            {
                _trayIcon.LeftDoubleClick -= TrayIcon_LeftDoubleClick;
                _trayIcon.LeftClick -= TrayIcon_LeftClick;
                _trayIcon.RightClick -= TrayIcon_RightClick;
                _trayIcon.IsVisible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }

        private void TrayIcon_LeftDoubleClick(TrayIcon sender, TrayIconEventArgs args)
        {
            if (_windowState == WindowState.Minimized)
            {
                WindowExtensions.Restore(_window);
            }
            WindowExtensions.SetForegroundWindow(_window);
        }

        private void TrayIcon_RightClick(TrayIcon sender, TrayIconEventArgs args) => RightClick?.Invoke(this, args);

        private void TrayIcon_LeftClick(TrayIcon sender, TrayIconEventArgs args) => LeftClick?.Invoke(this, args);

        /// <summary>
        /// Occurs when the user clicks the left mouse button on the tray icon.
        /// </summary>
        public event TypedEventHandler<WindowManager, TrayIconEventArgs>? LeftClick;

        /// <summary>
        /// Occurs when the user right-clicks the tray icon.
        /// </summary>
        public event TypedEventHandler<WindowManager, TrayIconEventArgs>? RightClick;

        internal Microsoft.UI.IconId GetCurrentIcon()
        {
            var lresult = Windows.Win32.PInvoke.SendMessage(new Windows.Win32.Foundation.HWND(_window.GetWindowHandle()), (uint)WindowsMessages.WM_GETICON, 1, (nint)0);
            if (lresult > 0)
                return new Microsoft.UI.IconId((ulong)(nint)lresult);
            else
            {
                lresult = Windows.Win32.PInvoke.SendMessage(new Windows.Win32.Foundation.HWND(_window.GetWindowHandle()), (uint)WindowsMessages.WM_GETICON, 0, (nint)0);
                if (lresult > 0)
                    return new Microsoft.UI.IconId((ulong)(nint)lresult);
            }
            var icon = Windows.Win32.PInvoke.LoadIcon(Windows.Win32.Foundation.HINSTANCE.Null, lpIconName: Windows.Win32.PInvoke.IDI_APPLICATION);
            return new Microsoft.UI.IconId((ulong)icon.Value);
        }
    }
}
