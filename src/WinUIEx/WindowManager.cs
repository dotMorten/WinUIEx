using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUIEx.Messaging;

namespace WinUIEx
{
    /// <summary>
    /// Manages Window size, and ensures window correctly resizes during DPI changes to keep consistent
    /// DPI-independent sizing.
    /// </summary>
    internal class WindowManager : IDisposable
    {
        private WindowMessageMonitor _monitor;
        private Window _window;
        private AppWindow _appWindow;
        
        public WindowManager(Window window) : this(window, new WindowMessageMonitor(window))
        {
        }

        public WindowManager(Window window, WindowMessageMonitor monitor)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _appWindow = window.GetAppWindow();
            _monitor = monitor;
            _monitor.WindowMessageReceived += OnWindowMessage;
        }

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
    }
}
