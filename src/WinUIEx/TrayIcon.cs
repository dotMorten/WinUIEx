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
    // See https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyicona
    const uint NIM_ADD = 0x00000000;
    const uint NIM_MODIFY = 0x00000001;
    const uint NIM_DELETE = 0x00000002;
    const uint NIM_SETVERSION = 0x00000004;

    private const uint TrayIconCallbackId = 0x8765;
    private readonly Window _window;
    private readonly nint _windowHandle;
    private readonly WindowMessageMonitor _monitor;
    private IconId currentIcon;
    private HICON _ownedIconHandle;
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
    /// <param name="iconPath">The file path to the icon image to display in the system tray. Supports .ico and .svg files.</param>
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
        ReleaseOwnedIcon();
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
            var grid = ((Microsoft.UI.Xaml.Controls.Grid)_window.Content);
            grid.ContextFlyout = flyout;
            _window.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(location.X, location.Y, 0, 0), Microsoft.UI.Windowing.DisplayArea.GetFromPoint(new Windows.Graphics.PointInt32(0, 0), Microsoft.UI.Windowing.DisplayAreaFallback.Primary));
            _window.Activate();
            _window.Show();
            WindowExtensions.SetForegroundWindow(_window);
            double w = (location.Width - location.X) /  grid.XamlRoot.RasterizationScale;
            double h = (location.Height - location.Y) / grid.XamlRoot.RasterizationScale;
            
            _currentFlyout.ShowAt(_root, new FlyoutShowOptions()
            {
                Position = new Windows.Foundation.Point(0, 0),
                ExclusionRect = new Windows.Foundation.Rect(0, 0, w, h)
            });
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
    /// <param name="iconPath">The file path to the icon image to display in the tray. Supports .ico and .svg files.</param>
    public unsafe void SetIcon(string iconPath)
    {
        CheckDisposed();
        if (iconPath is null)
            throw new ArgumentNullException(nameof(iconPath));

        string resolvedIconPath = IconPathHelper.ResolvePath(iconPath);
        int size = Math.Max((int)(WindowExtensions.GetDpiForWindow(_window) / 6d), 1);
        switch (GetIconFileType(resolvedIconPath))
        {
            case IconFileType.Ico:
                fixed (char* nameLocal = resolvedIconPath)
                {
                    var id = PInvoke.LoadImage(HINSTANCE.Null, nameLocal, GDI_IMAGE_TYPE.IMAGE_ICON, size, size, IMAGE_FLAGS.LR_LOADFROMFILE);
                    if (id.IsNull)
                        throw new ArgumentException($"Failed to load icon from {iconPath}", nameof(iconPath));
                    SetIconCore(new IconId((ulong)id.Value), new HICON(id.Value));
                }
                break;
            case IconFileType.Svg:
                var svgHandle = SvgIconHelper.CreateIconFromSvg(resolvedIconPath, (uint)size, (uint)size);
                SetIconCore(new IconId((ulong)svgHandle.Value), svgHandle);
                break;
            default:
                throw new ArgumentException($"Unsupported icon file format for {iconPath}", nameof(iconPath));
        }
    }

    /// <summary>
    /// Sets the icon displayed for the application's taskbar window.
    /// </summary>
    /// <param name="iconId">The identifier of the icon to display in the taskbar. Must be a valid <see cref="IconId"/> value.</param>
    public void SetIcon(IconId iconId)
    {
        CheckDisposed();
        SetIconCore(iconId, HICON.Null);
    }

    private void SetIconCore(IconId iconId, HICON ownedIconHandle)
    {
        ReleaseOwnedIcon();
        _ownedIconHandle = ownedIconHandle;
        currentIcon = iconId;
        if (IsVisible)
            UpdateIcon();
    }

    private void ReleaseOwnedIcon()
    {
        if (!_ownedIconHandle.IsNull)
        {
            PInvoke.DestroyIcon(_ownedIconHandle);
            _ownedIconHandle = HICON.Null;
        }
    }

    private static IconFileType GetIconFileType(string iconPath)
    {
        string extension = Path.GetExtension(iconPath);
        if (extension.Equals(".ico", StringComparison.OrdinalIgnoreCase))
            return IconFileType.Ico;
        if (extension.Equals(".svg", StringComparison.OrdinalIgnoreCase))
            return IconFileType.Svg;

        using FileStream stream = File.OpenRead(iconPath);
        Span<byte> header = stackalloc byte[4];
        int bytesRead = stream.Read(header);
        if (bytesRead >= 4 &&
            header[0] == 0x00 &&
            header[1] == 0x00 &&
            header[2] == 0x01 &&
            header[3] == 0x00)
        {
            return IconFileType.Ico;
        }

        stream.Position = 0;
        using StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 256, leaveOpen: false);
        char[] textBuffer = new char[256];
        int textLength = reader.Read(textBuffer, 0, textBuffer.Length);
        string text = new string(textBuffer, 0, textLength).TrimStart();
        if (text.StartsWith("<svg", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("<svg", StringComparison.OrdinalIgnoreCase))
        {
            return IconFileType.Svg;
        }

        return IconFileType.Unknown;
    }

    private enum IconFileType
    {
        Unknown,
        Ico,
        Svg
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

    private static uint CreateIconData(HICON hicon, string? tooltip, uint callbackId, out __ushort_128 tip)
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
            for (int i = 0; i < 128 && i < tooltip.Length; i++)
            {
                tip[i] = (ushort)tooltip[i];
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
        //System.Diagnostics.Debug.WriteLine($"Tray {type}: LParam=0x{lparam.ToString("x")} WParam=0x{message.WParam.ToString("x")}");

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