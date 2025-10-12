using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinUIEx.Messaging;

namespace WinUIEx;

/// <summary>
/// Represents an application icon displayed in the Windows system tray, providing functionality to show, update, and
/// interact with a tray icon and its associated tooltip and events.
/// </summary>
/// <remarks><para>The TrayIcon class enables applications to display an icon in the system tray (notification area),
/// respond to user interactions such as clicks and double-clicks, and show contextual flyouts. It manages the icon's
/// visibility, tooltip, and icon image, and provides events for common user actions.</para>
/// <note>Make sure you keep a reference to the <see cref="TrayIcon"/> as long as you are relying on it in the tray,
/// or the Garbage Collector will collect it and remove it from the tray.</note>
/// </remarks>
public class TrayIcon : IDisposable
{
    private const uint TrayIconCallbackId = 0x8765;
    private readonly Window _window;
    private readonly nint _windowHandle;
    private readonly WindowMessageMonitor _monitor;
    private IconId currentIcon;
    private string _tooltip;
    private FrameworkElement _root;
    private FlyoutBase? _currentFlyout;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrayIcon" /> class with the specified icon and tooltip text.
    /// </summary>
    /// <param name="trayiconId">A unique identifier for the tray icon.</param>
    /// <param name="iconId">The identifier of the icon to display in the system tray. Determines the visual appearance of the tray icon.</param>
    /// <param name="tooltip">The tooltip text to display when the user hovers over the tray icon. Maximum length: 128 characters.</param>
    public TrayIcon(uint trayiconId, IconId iconId, string tooltip) : this(trayiconId, tooltip)
    {
        currentIcon = iconId;
    }

    /// <summary>
    /// Initializes a new instance of the TrayIcon class with the specified icon and tooltip text.
    /// </summary>
    /// <param name="trayiconId">A unique identifier for the tray icon.</param>
    /// <param name="iconPath">The file path to the icon image to display in the system tray. Must refer to a valid .ico image file.</param>
    /// <param name="tooltip">The tooltip text to display when the user hovers over the tray icon. Maximum length: 128 characters.</param>
    public TrayIcon(uint trayiconId, string iconPath, string tooltip) : this(trayiconId, tooltip)
    {
        SetIcon(iconPath);
    }

    private TrayIcon(uint trayiconId, string tooltip)
    {
        TrayIconId = trayiconId;
        _tooltip = tooltip;
        _window = new Window();
        _window.Content = _root = new Microsoft.UI.Xaml.Controls.Grid();
        _windowHandle = _window.GetWindowHandle();
        _window.AppWindow.IsShownInSwitchers = false;
        WindowExtensions.SetWindowStyle(_window, WindowStyle.Popup);
        WindowExtensions.SetIsAlwaysOnTop(_window, true);
        _monitor = new WindowMessageMonitor(_windowHandle);
        _monitor.WindowMessageReceived += WindowMessageReceived;
    }

    /// <inheritdoc />
    ~TrayIcon()
    {
        Dispose(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes this instance and cleans up any resources
    /// </summary>
    /// <param name="disposing"></param>
    protected void Dispose(bool disposing)
    {
        _disposed = true;
        if (disposing)
        {
            _window.Close();
            _monitor.Dispose();
        }
        RemoveFromTray(TrayIconId);
    }

    private void CheckDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TrayIcon));
    }

    /// <summary>
    /// Gets the unique identifier for the tray icon.
    /// </summary>
    public uint TrayIconId { get; }

    private bool _isVisible;

    /// <summary>
    /// Gets or sets a value indicating whether this instance is visible in the tray.
    /// </summary>
    public bool IsVisible
    {
        get { return _isVisible; }
        set
        {
            CheckDisposed();
            _isVisible = value;
            if (_isVisible)
                AddToTray(TrayIconId);
            else
                RemoveFromTray(TrayIconId);
        }
    }

    private unsafe void WindowMessageReceived(object? sender, WindowMessageEventArgs e)
    {
        switch (e.MessageType)
        {
            case WindowsMessages.WM_GETMINMAXINFO:
                {
                    MINMAXINFO* rect2 = (MINMAXINFO*)e.Message.LParam;
                    // Restrict size to 0x0 to prevent the window from being physically shown
                    rect2->ptMinTrackSize.X = 0;
                    rect2->ptMinTrackSize.Y = 0;
                }
                break;
            case (WindowsMessages)TrayIconCallbackId: // Callback from tray icon defined in AddToTray()
                {
                    ProcessTrayIconEvents(e.Message);
                    break;
                }
            case WindowsMessages.WM_ACTIVATE:
                {
                    if (e.Message.WParam == 0) // Window lost focus
                    {
                        _window.DispatcherQueue.TryEnqueue(() => _currentFlyout?.Hide());
                    }
                    break;
                }
        }
    }

    private void ShowFlyout(FlyoutBase flyout)
    {
        CheckDisposed();
        CloseFlyout();
        var icon = new NOTIFYICONIDENTIFIER()
        {
            uID = TrayIconId,
            hWnd = _windowHandle,
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONIDENTIFIER>(),
        };
        var hresult = PInvoke.Shell_NotifyIconGetRect(ref icon, out var location);
        if (hresult == 0)
        {
            if (_currentFlyout != null && _currentFlyout != flyout)
                _currentFlyout.Hide();
            flyout.ShouldConstrainToRootBounds = false;
            _currentFlyout = flyout;
            ((Microsoft.UI.Xaml.Controls.Grid)_window.Content).ContextFlyout = flyout;
            _window.Activate();
            _window.Show();
            _window.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(location.X, location.Y, 0, 0), Microsoft.UI.Windowing.DisplayArea.GetFromPoint(new Windows.Graphics.PointInt32(0, 0), Microsoft.UI.Windowing.DisplayAreaFallback.Primary));
            WindowExtensions.SetForegroundWindow(_window);
            _currentFlyout.ShowAt(_root, new FlyoutShowOptions() { Position = new Windows.Foundation.Point(0, 0) });
            _currentFlyout.Closing += CurrentFlyout_Closing;
        }
    }

    private void CurrentFlyout_Closing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
    {
        sender.Closing -= CurrentFlyout_Closing;
        _window.Hide();
    }
    
    /// <summary>
    /// Force-closes the flyout if it is visible.
    /// </summary>
    public void CloseFlyout()
    {
        CheckDisposed();
        if (_currentFlyout is not null)
        {
            _currentFlyout.Closing -= CurrentFlyout_Closing;
            if (_currentFlyout.IsOpen)
                _currentFlyout.Hide();
            _currentFlyout = null;
        }
        _window.Hide();
    }

    /// <summary>
    /// Gets or sets the tooltip displayed when hovering on the callout. Maximum length: 128 characters
    /// </summary>
    public string Tooltip
    {
        get => _tooltip;
        set
        {
            CheckDisposed();
            if (_tooltip != value)
            {
                _tooltip = value;
                UpdateTooltip();
            }
        }
    }

    const uint NIM_MODIFY = 0x00000001;
    private void UpdateTooltip()
    {
        if (!IsVisible)
            return;

        if (Environment.Is64BitProcess)
        {
            var notifyIconData = CreateIconData64(TrayIconId, HICON.Null, Tooltip, 0);
            PInvoke.Shell_NotifyIcon(NIM_MODIFY, notifyIconData);
        }
        else
        {
            var notifyIconData = CreateIconData32(TrayIconId, HICON.Null, Tooltip, 0);
            PInvoke.Shell_NotifyIcon(NIM_MODIFY, notifyIconData);
        }
    }

    private void UpdateIcon()
    {
        if (!IsVisible || currentIcon.Value == 0)
            return;
        const uint NIM_MODIFY = 0x00000001;
        var hicon = new HICON((nint)currentIcon.Value);

        if (Environment.Is64BitProcess)
        {
            var notifyIconData = CreateIconData64(TrayIconId, hicon, null, 0);
            PInvoke.Shell_NotifyIcon(NIM_MODIFY, notifyIconData);
        }
        else
        {
            var notifyIconData = CreateIconData32(TrayIconId, hicon, null, 0);
            PInvoke.Shell_NotifyIcon(NIM_MODIFY, notifyIconData);
        }
    }

    /// <summary>
    /// Sets the icon displayed for the application's taskbar button.
    /// </summary>
    /// <param name="iconPath">The file path to the icon image to display on the taskbar button. The path must refer to a valid .ico image file.</param>
    public unsafe void SetIcon(string iconPath)
    {
        fixed (char* nameLocal = iconPath)
        {
            var size = (int)(WindowExtensions.GetDpiForWindow(_window) / 6d);
            var id = PInvoke.LoadImage(HINSTANCE.Null, nameLocal, GDI_IMAGE_TYPE.IMAGE_ICON, size, size, IMAGE_FLAGS.LR_LOADFROMFILE);
            if (id.IsNull)
                throw new ArgumentException($"Failed to load icon from {iconPath}");
            SetIcon(new IconId((ulong)id.Value));
        }
    }

    /// <summary>
    /// Sets the icon displayed for the application's taskbar window.
    /// </summary>
    /// <param name="iconId">The identifier of the icon to display in the taskbar. Must be a valid <see cref="IconId"/> value.</param>
    public void SetIcon(IconId iconId)
    {
        currentIcon = iconId;
        if (IsVisible)
            UpdateIcon();
    }

    private void AddToTray(uint iconId)
    {
        if (currentIcon.Value == 0) // Fallback to default icon
        {
            var lresult = PInvoke.SendMessage(new Windows.Win32.Foundation.HWND(_windowHandle), (uint)WindowsMessages.WM_GETICON, 1, (nint)0);
            if (lresult > 0)
                currentIcon = new IconId((ulong)lresult.Value);
            else
            {
                lresult = PInvoke.SendMessage(new Windows.Win32.Foundation.HWND(_windowHandle), (uint)WindowsMessages.WM_GETICON, 0, (nint)0);
                if (lresult > 0)
                    currentIcon = new IconId((ulong)lresult.Value);
            }
        }

        // See https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyicona
        const uint NIM_ADD = 0x00000000;
        const uint NIM_SETVERSION = 0x00000004;
        HICON hicon;
        if (currentIcon.Value > 0)
        {
            hicon = new HICON((nint)currentIcon.Value);
        }
        else // Fall back to default application icon
        {
            var icon = PInvoke.LoadIcon(Windows.Win32.Foundation.HINSTANCE.Null, lpIconName: PInvoke.IDI_APPLICATION);
            hicon = new HICON(icon);
        }

        if (Environment.Is64BitProcess)
        {
            var notifyIconData = CreateIconData64(iconId, hicon, Tooltip, TrayIconCallbackId);
            PInvoke.Shell_NotifyIcon(NIM_ADD, notifyIconData);
            PInvoke.Shell_NotifyIcon(NIM_SETVERSION, notifyIconData);
        }
        else
        {
            var notifyIconData = CreateIconData32(iconId, hicon, Tooltip, TrayIconCallbackId);
            PInvoke.Shell_NotifyIcon(NIM_ADD, notifyIconData);
            PInvoke.Shell_NotifyIcon(NIM_SETVERSION, notifyIconData);
        }
    }

    private uint CreateIconData(HICON hicon, string? tooltip, uint callbackId, out __ushort_128 tip)
    {
        const uint NIF_MESSAGE = 0x00000001;
        const uint NIF_ICON = 0x00000002;
        const uint NIF_TIP = 0x00000004;
        const uint NIF_SHOWTIP = 0x80;
        uint flags = 0;
        if (!hicon.IsNull)
            flags = flags | NIF_ICON;
        tip = new __ushort_128();
        if (!string.IsNullOrEmpty(tooltip))
        {
            if (!string.IsNullOrEmpty(_tooltip))
            {
                for (int i = 0; i < 128 && i < _tooltip.Length; i++)
                {
                    tip[i] = (ushort)_tooltip[i];
                }
            }
            flags = flags | NIF_TIP | NIF_SHOWTIP;
        }
        if (callbackId > 0)
            flags = flags | NIF_MESSAGE;
        return flags;
    }

    private NOTIFYICONDATAW64 CreateIconData64(uint iconId, HICON hicon, string? tooltip = null, uint callbackId = 0)
    {
        System.Diagnostics.Debug.Assert(Environment.Is64BitProcess);
        uint flags = CreateIconData(hicon, tooltip, callbackId, out var tip);

        return new NOTIFYICONDATAW64
        {
            hWnd = new Windows.Win32.Foundation.HWND(_windowHandle),
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW64>(),
            uID = iconId,
            uFlags = flags,
            hIcon = hicon,
            uCallbackMessage = TrayIconCallbackId,
            szTip = tip, 
            VersionOrTimeout = 4,
        };
    }

    private NOTIFYICONDATAW32 CreateIconData32(uint iconId, HICON hicon, string? tooltip = null, uint callbackId = 0)
    {
        System.Diagnostics.Debug.Assert(!Environment.Is64BitProcess);
        uint flags = CreateIconData(hicon, tooltip, callbackId, out var tip);

        return new NOTIFYICONDATAW32
        {
            hWnd = new Windows.Win32.Foundation.HWND(_windowHandle),
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW32>(),
            uID = iconId,
            uFlags = flags,
            hIcon = hicon,
            uCallbackMessage = TrayIconCallbackId,
            szTip = tip,
            VersionOrTimeout = 4 
        };
    }

    private void RemoveFromTray(uint iconId)
    {
        const uint NIM_DELETE = 0x00000002;
        if (Environment.Is64BitProcess)
        {
            var notifyIconData = new NOTIFYICONDATAW64
            {
                hWnd = new Windows.Win32.Foundation.HWND(_windowHandle),
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW64>(),
                uID = iconId,
            };
            PInvoke.Shell_NotifyIcon(NIM_DELETE, notifyIconData);
        }
        else
        {
            var notifyIconData = new NOTIFYICONDATAW32
            {
                hWnd = new Windows.Win32.Foundation.HWND(_windowHandle),
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW32>(),
                uID = iconId,
            };
            PInvoke.Shell_NotifyIcon(NIM_DELETE, notifyIconData);
        }
    }

    private void ProcessTrayIconEvents(Message message)
    {
        var iconid = message.HighOrder;
        if (iconid != TrayIconId)
            return;
        var type = (WindowsMessages)(message.LParam & 0xffff);
        var lparam = message.LParam & 0xffff0000;
        System.Diagnostics.Debug.WriteLine($"Tray {type}: LParam={lparam.ToString("0x")} WParam=0x{message.WParam.ToString("x2")}");

        TrayIconEventArgs? args = null;
        switch (type)
        {
            case WindowsMessages.WM_LBUTTONDBLCLK:
                LeftDoubleClick?.Invoke(this, args = new TrayIconEventArgs());
                break;
            case WindowsMessages.WM_RBUTTONDBLCLK:
                RightDoubleClick?.Invoke(this, args = new TrayIconEventArgs());
                break;
            case WindowsMessages.NIN_SELECT:
                Selected?.Invoke(this, args = new TrayIconEventArgs());
                break;
            case WindowsMessages.WM_CONTEXTMENU:
                ContextMenu?.Invoke(this, args = new TrayIconEventArgs());
                break;
        }
        if (args?.Flyout != null)
            ShowFlyout(args.Flyout);
    }

    /// <summary>
    /// Occurs when the user clicks the left mouse button on the tray icon or selects it via the keyboard.
    /// </summary>
    public event Windows.Foundation.TypedEventHandler<TrayIcon, TrayIconEventArgs>? Selected;

    /// <summary>
    /// Occurs when the user right-clicks the tray icon or selects the context menu from the keyboard.
    /// </summary>
    public event Windows.Foundation.TypedEventHandler<TrayIcon, TrayIconEventArgs>? ContextMenu;

    /// <summary>
    /// Occurs when the user double-clicks the left mouse button on the tray icon.
    /// </summary>
    public event Windows.Foundation.TypedEventHandler<TrayIcon, TrayIconEventArgs>? LeftDoubleClick;

    /// <summary>
    /// Occurs when the user double-clicks the tray icon with the right mouse button.
    /// </summary>
    public event Windows.Foundation.TypedEventHandler<TrayIcon, TrayIconEventArgs>? RightDoubleClick;
}

/// <summary>
/// Provides data for events related to the tray icon, including information about the associated flyout to display.
/// </summary>
public class TrayIconEventArgs : EventArgs
{
    internal TrayIconEventArgs()
    {
    }

    /// <summary>
    /// Gets or sets the flyout to be shown when this event triggers.
    /// </summary>
    public FlyoutBase? Flyout { get; set; }

    /// <summary>
    /// Prevents any default action associated with this event.
    /// </summary>
    public bool Handled { get; set; }
}