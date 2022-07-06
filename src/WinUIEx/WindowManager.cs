using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.Storage;
using WinUIEx.Messaging;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.UI;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace WinUIEx
{
    /// <summary>
    /// Manages Window sizes, persists location and size across application sessions, simplifies backdrop configurations etc.
    /// Use this class instead of <see cref="WindowEx"/> if you just want to extend an existing window with functionality,
    /// without having to change the baseclass.
    /// </summary>
    public partial class WindowManager : IDisposable
    {
        private readonly WindowMessageMonitor _monitor;
        private readonly Window _window;
        private OverlappedPresenter overlappedPresenter;
        private readonly static Dictionary<IntPtr, WeakReference<WindowManager>> managers = new Dictionary<IntPtr, WeakReference<WindowManager>>();

        private static bool TryGetWindowManager(Window window, [MaybeNullWhen(false)] out WindowManager manager)
        {
            if (window is null)
                throw new ArgumentNullException();
            var handle = window.GetWindowHandle();
            if (managers.TryGetValue(handle, out var weakHandle) && weakHandle.TryGetTarget(out manager))
            {
                if (!manager._isDisposed)
                    return true;
            }
            manager = null;
            return false;
        }

        static WindowManager()
        {
            try
            {
                PersitanceStorage = ApplicationData.Current?.LocalSettings?.CreateContainer("WinUIEx", ApplicationDataCreateDisposition.Always)?.Values;
            }
            catch { } // Throws for unpackaged apps
        }

        /// <summary>
        /// Gets (or creates) a window manager for the specific window.
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        public static WindowManager Get(Window window)
        {
            if (TryGetWindowManager(window, out var manager))
                return manager;
            else
                return new WindowManager(window);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowManager"/> class.
        /// </summary>
        /// <param name="window"></param>
        private WindowManager(Window window) : this(window, new WindowMessageMonitor(window))
        {
        }

        private WindowManager(Window window, WindowMessageMonitor monitor)
        {
            if(TryGetWindowManager(window, out var oldmonitor))
            {
                throw new InvalidOperationException("Only one window manager can be attached to a window");
            }
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _monitor = monitor;
            _monitor.WindowMessageReceived += OnWindowMessage;
            _window.Activated += Window_Activated;
            _window.Closed += Window_Closed;
            AppWindow.Changed += AppWindow_Changed;

            overlappedPresenter = AppWindow.Presenter as OverlappedPresenter ?? Microsoft.UI.Windowing.OverlappedPresenter.Create();
            managers[window.GetWindowHandle()] = new WeakReference<WindowManager>(this);
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            _window.Activated -= Window_Activated;
            _window.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
            {
                LoadPersistence();
                InitBackdrop();
            });
            if (BackdropConfiguration != null)
                BackdropConfiguration.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            SavePersistence();
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~WindowManager() => Dispose(false);

        private bool _isDisposed;

        /// <inheritdoc />
        public void Dispose() => Dispose(true);

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                var handle = _window.GetWindowHandle();
                if (managers.ContainsKey(handle))
                    managers.Remove(handle);
                AppWindow.Changed -= AppWindow_Changed;
                _window.Activated -= Window_Activated;
                _window.Closed -= Window_Closed;
                _monitor.WindowMessageReceived -= OnWindowMessage;
                _monitor.Dispose();
            }
            _isDisposed = true;
        }

        /// <summary>
        /// Gets a reference to the AppWindow for the app
        /// </summary>
        public Microsoft.UI.Windowing.AppWindow AppWindow => _window.GetAppWindow();

        /// <summary>
        /// Gets or sets the width of the window.
        /// </summary>
        /// <value>The window width in device independent pixels.</value>
        public double Width
        {
            get => AppWindow.Size.Width / (_window.GetDpiForWindow() / 96d);
            set => _window.SetWindowSize(value, Height);
        }

        /// <summary>
        /// Gets or sets the height of the window.
        /// </summary>
        /// <value>The window height in device independent pixels.</value>
        public double Height
        {
            get => AppWindow.Size.Height / (_window.GetDpiForWindow() / 96d);
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
            WindowMessageReceived?.Invoke(this, e);
            if (e.Handled)
                return;
            switch (e.MessageType)
            {
                case WindowsMessages.WM_GETMINMAXINFO:
                    {
                        if (_restoringPersistance)
                            break;
                        // Restrict min-size
                        MINMAXINFO* rect2 = (MINMAXINFO*)e.Message.LParam;
                        var currentDpi = _window.GetDpiForWindow();
                        rect2->ptMinTrackSize.x = (int)(Math.Max(MinWidth * (currentDpi / 96f), rect2->ptMinTrackSize.x));
                        rect2->ptMinTrackSize.y = (int)(Math.Max(MinHeight * (currentDpi / 96f), rect2->ptMinTrackSize.y));
                    }
                    break;
                case WindowsMessages.WM_DPICHANGED:
                    {
                        if (_restoringPersistance)
                            e.Handled = true; // Don't let WinUI resize the window due to a dpi change caused by restoring window position - we got this.
                        break;
                    }
            }
        }

        /// <summary>
        /// Event raised when a windows message is received.
        /// </summary>
        public event EventHandler<WindowMessageEventArgs>? WindowMessageReceived;

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
        /// By default the property uses ApplicationData storage, and therefore is currently only functional for
        /// packaged applications. If you're using an unpackaged application, you must also set the <see cref="PersitanceStorage"/>
        /// property and manage persisting this across application settings.
        /// </remarks>
        /// <seealso cref="PersitanceStorage"/>
        public string? PersistenceId { get; set; }

        private bool _restoringPersistance; // Flag used to avoid WinUI DPI adjustment

        /// <summary>
        /// Gets or sets the persistance storage for maintaining window settings across application settings.
        /// </summary>
        /// <remarks>
        /// For a packaged application, this will be initialized automatically for you, and saved with the application identity.
        /// However for an unpackaged app, you will need to set this and serialize the property to/from disk between
        /// application sessions.
        /// </remarks>
        /// <seealso cref="PersistenceId"/>
        public static IDictionary<string,object>? PersitanceStorage { get; set; }

        private void LoadPersistence()
        {
            if (!string.IsNullOrEmpty(PersistenceId))
            {
                try
                {
                    if(PersitanceStorage is null)
                        return;
                    byte[]? data = null;
                    if (PersitanceStorage.ContainsKey($"WindowPersistance_{PersistenceId}"))
                    {
                        var base64 = PersitanceStorage[$"WindowPersistance_{PersistenceId}"] as string;
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
                    _restoringPersistance = true;
                    Windows.Win32.PInvoke.SetWindowPlacement(new Windows.Win32.Foundation.HWND(_window.GetWindowHandle()), in retobj);
                    _restoringPersistance = false;
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
                if (PersitanceStorage != null)
                    PersitanceStorage[$"WindowPersistance_{PersistenceId}"] = Convert.ToBase64String(data.ToArray());
            }
        }
        #endregion

        private void AppWindow_Changed(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowChangedEventArgs args)
        {
            if (args.DidPositionChange)
                PositionChanged?.Invoke(this, sender.Position);
            if (args.DidPresenterChange)
            {
                if(AppWindow.Presenter is OverlappedPresenter op && op != overlappedPresenter)
                {
                    overlappedPresenter = op;
                    _IsTitleBarVisible = op.HasTitleBar;
                }
                PresenterChanged?.Invoke(this, sender.Presenter);
            }
            if(args.DidZOrderChange)
                ZOrderChanged?.Invoke(this, new ZOrderInfo() { IsZOrderAtTop = args.IsZOrderAtTop, IsZOrderAtBottom = args.IsZOrderAtBottom, ZOrderBelowWindowId = args.ZOrderBelowWindowId });
        }

        private bool _IsTitleBarVisible = true;

        /// <summary>
        /// Gets or sets a value indicating whether the default title bar is visible or not.
        /// </summary>
        public bool IsTitleBarVisible
        {
            get { return _IsTitleBarVisible; }
            set
            {
                _IsTitleBarVisible = value;
                overlappedPresenter.SetBorderAndTitleBar(true, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the minimize button is visible
        /// </summary>
        public bool IsMinimizable
        {
            get => overlappedPresenter.IsMinimizable;
            set => overlappedPresenter.IsMinimizable = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the maximimze button is visible
        /// </summary>
        public bool IsMaximizable
        {
            get => overlappedPresenter.IsMaximizable;
            set => overlappedPresenter.IsMaximizable = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the window can be resized.
        /// </summary>
        public bool IsResizable
        {
            get => overlappedPresenter.IsResizable;
            set => overlappedPresenter.IsResizable = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this window is always on top.
        /// </summary>
        public bool IsAlwaysOnTop
        {
            get => overlappedPresenter.IsAlwaysOnTop;
            set => overlappedPresenter.IsAlwaysOnTop = value;
        }

        /// <summary>
        /// Gets or sets the presenter kind for the current window
        /// </summary>
        /// <seealso cref="AppWindow.Presenter"/>
        /// <seealso cref="PresenterChanged"/>
        public Microsoft.UI.Windowing.AppWindowPresenterKind PresenterKind
        {
            get => AppWindow.Presenter.Kind;
            set
            {
                if (value is Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped)
                    AppWindow.SetPresenter(overlappedPresenter);
                else
                    AppWindow.SetPresenter(value);
            }
        }

        /// <summary>
        /// Raised if the window position changes.
        /// </summary>
        public event EventHandler<Windows.Graphics.PointInt32>? PositionChanged;

        /// <summary>
        /// Raised if the presenter for the window changes.
        /// </summary>
        public event EventHandler<AppWindowPresenter>? PresenterChanged;

        /// <summary>
        /// Raised if the Z order of the window changes.
        /// </summary>
        public event EventHandler<ZOrderInfo>? ZOrderChanged;
    }
}
