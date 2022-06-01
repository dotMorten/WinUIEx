using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.Storage;
using WinUIEx.Messaging;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinUIEx
{
    /// <summary>
    /// Manages Window size, and ensures window correctly resizes during DPI changes to keep consistent
    /// DPI-independent sizing.
    /// </summary>
    internal class WindowManager : IDisposable
    {
        private readonly WindowMessageMonitor _monitor;
        private readonly Window _window;
        private readonly AppWindow _appWindow;
        
        public WindowManager(Window window) : this(window, new WindowMessageMonitor(window))
        {
        }

        public WindowManager(Window window, WindowMessageMonitor monitor)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _appWindow = window.GetAppWindow();
            _monitor = monitor;
            _monitor.WindowMessageReceived += OnWindowMessage;
            _window.Activated += Window_Activated;
            _window.Closed += Window_Closed;
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            _window.Activated -= Window_Activated;
            _window.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
            {
                LoadPersistence();
            });
        }

        private void Window_Closed(object sender, WindowEventArgs args) => SavePersistence();

        /// <summary>
        /// Finalizer
        /// </summary>
        ~WindowManager() => Dispose(false);

        /// <inheritdoc />
        public void Dispose() => Dispose(true);

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _monitor.WindowMessageReceived -= OnWindowMessage;
                _monitor.Dispose();
            }
        }

        /// <summary>
        /// Gets or sets the width of the window.
        /// </summary>
        /// <value>The window width in device independent pixels.</value>
        public double Width
        {
            get => _appWindow.Size.Width / (_window.GetDpiForWindow() / 96d);
            set => _window.SetWindowSize(value, Height);
        }

        /// <summary>
        /// Gets or sets the height of the window.
        /// </summary>
        /// <value>The window height in device independent pixels.</value>
        public double Height
        {
            get => _appWindow.Size.Height / (_window.GetDpiForWindow() / 96d);
            set => _window.SetWindowSize(Width, value);
        }

        private double _minWidth = 136;

        /// <summary>
        /// Gets or sets the minimum width of this window
        /// </summary>
        /// <value>The minimum window width in device independent pixels.</value>
        /// <remarks>A window is currently set to a minimum of 139 pixels.</remarks>
        public double MinWidth
        {
            get => _minWidth;
            set
            {
                _minWidth = value;
                if (Width < value)
                    Width = value;
            }
        }

        private double _minHeight = 39;

        /// <summary>
        /// Gets or sets the minimum height of this window
        /// </summary>
        /// <value>The minimum window height in device independent pixels.</value>
        /// <remarks>A window is currently set to a minimum of 39 pixels.</remarks>
        public double MinHeight
        {
            get => _minHeight;
            set
            {
                _minHeight = value;
                if (Height < value)
                    Height = value;
            }
        }

        private unsafe void OnWindowMessage(object? sender, Messaging.WindowMessageEventArgs e)
        {
            switch (e.MessageType)
            {
                case WindowsMessages.WM_GETMINMAXINFO:
                    {
                        // Restrict min-size
                        MINMAXINFO* rect2 = (MINMAXINFO*)e.Message.LParam;
                        var currentDpi = _window.GetDpiForWindow();
                        rect2->ptMinTrackSize.x = (int)(Math.Max(MinWidth * (currentDpi / 96f), rect2->ptMinTrackSize.x));
                        rect2->ptMinTrackSize.y = (int)(Math.Max(MinHeight * (currentDpi / 96f), rect2->ptMinTrackSize.y));
                    }
                    break;
                case WindowsMessages.WM_DPICHANGED:
                    {
                        // Resize to account for DPI change
                        var suggestedRect = (Windows.Win32.Foundation.RECT*)e.Message.LParam;
                        bool result = Windows.Win32.PInvoke.SetWindowPos(new Windows.Win32.Foundation.HWND(_window.GetWindowHandle()), new Windows.Win32.Foundation.HWND(), suggestedRect->left, suggestedRect->top,
                            suggestedRect->right - suggestedRect->left, suggestedRect->bottom - suggestedRect->top, Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOZORDER | Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
                        break;
                    }
            }
        }

        private struct MINMAXINFO
        {
#pragma warning disable CS0649
            public Windows.Win32.Foundation.POINT ptReserved;
            public Windows.Win32.Foundation.POINT ptMaxSize;
            public Windows.Win32.Foundation.POINT ptMaxPosition;
            public Windows.Win32.Foundation.POINT ptMinTrackSize;
            public Windows.Win32.Foundation.POINT ptMaxTrackSize;
#pragma warning restore CS0649
        }

        #region Persistence

        /// <summary>
        /// Gets or sets a unique ID used for saving and restoring window size and position
        /// across sessions.
        /// </summary>
        /// <remarks>
        /// The ID must be set before the window activates. The window size and position
        /// will only be restored if the monitor layout hasn't changed between application settings.
        /// The property uses ApplicationData storage, and therefore is currently only functional for
        /// packaged applications.
        /// </remarks>
        public string? PersistenceId { get; set; }

        private void LoadPersistence()
        {
            if (!string.IsNullOrEmpty(PersistenceId))
            {
                try
                {
                    if (ApplicationData.Current?.LocalSettings?.Containers is null ||
                        !ApplicationData.Current.LocalSettings.Containers.ContainsKey("WinUIEx"))
                        return;
                    byte[]? data = null;
                    var winuiExSettings = ApplicationData.Current.LocalSettings.CreateContainer("WinUIEx", ApplicationDataCreateDisposition.Existing);
                    if (winuiExSettings is not null && winuiExSettings.Values.ContainsKey($"WindowPersistance_{PersistenceId}"))
                    {
                        var base64 = winuiExSettings.Values[$"WindowPersistance_{PersistenceId}"] as string;
                        if(base64 != null)
                            data = Convert.FromBase64String(base64);
                    }
                    if (data is null)
                        return;
                    // Check if monitor layout changed since we stored position
                    var monitors = MonitorInfo.GetDisplayMonitors();
                    System.IO.BinaryReader br = new System.IO.BinaryReader(new System.IO.MemoryStream(data));
                    int monitorCount = br.ReadInt32();
                    if (monitorCount < monitors.Count)
                        return; // Don't restore - list of monitors changed
                    for (int i = 0; i < monitorCount; i++)
                    {
                        var pMonitor = monitors[i];
                        if (pMonitor.Name != br.ReadString() ||
                            pMonitor.RectMonitor.Left != br.ReadDouble() ||
                            pMonitor.RectMonitor.Top != br.ReadDouble() ||
                            pMonitor.RectMonitor.Right != br.ReadDouble() ||
                            pMonitor.RectMonitor.Bottom != br.ReadDouble())
                            return; // Don't restore - Monitor layout changed
                    }
                    int structSize = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
                    byte[] placementData = br.ReadBytes(structSize);
                    IntPtr buffer = Marshal.AllocHGlobal(structSize);
                    Marshal.Copy(placementData, 0, buffer, structSize);
                    var retobj = (WINDOWPLACEMENT)Marshal.PtrToStructure(buffer, typeof(WINDOWPLACEMENT))!;
                    Marshal.FreeHGlobal(buffer);
                    // Ignore anything by maximized or normal
                    if (retobj.showCmd == SHOW_WINDOW_CMD.SW_INVALIDATE && retobj.flags == WINDOWPLACEMENT_FLAGS.WPF_RESTORETOMAXIMIZED)
                        retobj.showCmd = SHOW_WINDOW_CMD.SW_MAXIMIZE;
                    else if (retobj.showCmd != SHOW_WINDOW_CMD.SW_MAXIMIZE)
                        retobj.showCmd = SHOW_WINDOW_CMD.SW_NORMAL;
                    Windows.Win32.PInvoke.SetWindowPlacement(new Windows.Win32.Foundation.HWND(_window.GetWindowHandle()), in retobj);
                }
                catch { }
            }
        }

        private void SavePersistence()
        {
            if (!string.IsNullOrEmpty(PersistenceId))
            {
                // Store monitor info - we won't restore on original screen if original monitor layout has changed
                using var data = new System.IO.MemoryStream();
                using var sw = new System.IO.BinaryWriter(data);
                var monitors = MonitorInfo.GetDisplayMonitors();
                sw.Write(monitors.Count);
                foreach (var monitor in monitors)
                {
                    sw.Write(monitor.Name);
                    sw.Write(monitor.RectMonitor.Left);
                    sw.Write(monitor.RectMonitor.Top);
                    sw.Write(monitor.RectMonitor.Right);
                    sw.Write(monitor.RectMonitor.Bottom);
                }
                var placement = new WINDOWPLACEMENT();
                Windows.Win32.PInvoke.GetWindowPlacement(new Windows.Win32.Foundation.HWND(_window.GetWindowHandle()), ref placement);

                int structSize = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
                IntPtr buffer = Marshal.AllocHGlobal(structSize);
                Marshal.StructureToPtr(placement, buffer, false);
                byte[] placementData = new byte[structSize];
                Marshal.Copy(buffer, placementData, 0, structSize);
                Marshal.FreeHGlobal(buffer);
                sw.Write(placementData);
                sw.Flush();
                var winuiExSettings = ApplicationData.Current?.LocalSettings?.CreateContainer("WinUIEx", ApplicationDataCreateDisposition.Always);
                if (winuiExSettings != null)
                    winuiExSettings.Values[$"WindowPersistance_{PersistenceId}"] = Convert.ToBase64String(data.ToArray());
            }
        }
        #endregion
    }
}
