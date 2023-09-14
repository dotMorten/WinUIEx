using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using WinRT;
using WinUIEx.Messaging;

namespace WinUIEx
{
    /// <summary>
    /// A custom backdrop that make the window completely transparent.
    /// </summary>
    public class TransparentBackdrop : ColorBackdrop
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransparentBackdrop"/> class.
        /// </summary>
        public TransparentBackdrop() : base(Windows.UI.Color.FromArgb(0, 255, 255, 255))
        { 
        }
    }

    /// <summary>
    /// A custom backdrop that sets the background to the specified color - supports opacity to make the window semi-transparent.
    /// </summary>
    public class ColorBackdrop : Microsoft.UI.Xaml.Media.SystemBackdrop
    {
        private WindowMessageMonitor? monitor;
        private Windows.UI.Composition.CompositionColorBrush? brush;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ColorBackdrop"/> class.
        /// </summary>
        public ColorBackdrop() : this(Microsoft.UI.Colors.White)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorBackdrop"/> class.
        /// </summary>
        /// <param name="color">Color for the background</param>
        public ColorBackdrop(Windows.UI.Color color)
        {
            Color = color;
        }

        private Windows.UI.Color _color;

        /// <summary>
        /// Gets or sets the color used for the backdrop.
        /// </summary>
        public Windows.UI.Color  Color
        {
            get { return _color; }
            set
            {
                _color = value;
                if (brush != null)
                {
                    brush.Color = value;
                }
            }
        }

        /// <inheritdoc />
        protected override void OnDefaultSystemBackdropConfigurationChanged(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
        {
            if (target != null)
                base.OnDefaultSystemBackdropConfigurationChanged(target, xamlRoot);
        }

        /// <inheritdoc />
        protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
        {
            var inspectable = connectedTarget.As<IInspectable>();
            var xamlSource = DesktopWindowXamlSource.FromAbi(inspectable.ThisPtr);
            var hWnd = xamlSource.SiteBridge.SiteView.EnvironmentView.AppWindowId.Value;

            monitor = new WindowMessageMonitor((IntPtr)hWnd);
            monitor.WindowMessageReceived += Monitor_WindowMessageReceived;

            ConfigureDwm(hWnd);

            brush = WindowManager.Compositor.CreateColorBrush(Color);
            connectedTarget.SystemBackdrop = brush;

            var hdc = PInvoke.GetDC(new Windows.Win32.Foundation.HWND((nint)hWnd));
            ClearBackground((nint)hWnd, hdc.Value);

            base.OnTargetConnected(connectedTarget, xamlRoot);
        }

        /// <inheritdoc />
        protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
        {
            monitor?.Dispose();
            monitor = null;
            var backdrop = disconnectedTarget.SystemBackdrop;
            disconnectedTarget.SystemBackdrop = null;
            backdrop?.Dispose();
            brush?.Dispose();
            brush = null;
            base.OnTargetDisconnected(disconnectedTarget);
        }

        private static void ConfigureDwm(ulong hWnd)
        {
            Windows.Win32.Foundation.HWND handle = new Windows.Win32.Foundation.HWND((nint)hWnd);
            PInvoke.DwmExtendFrameIntoClientArea(handle, new Windows.Win32.UI.Controls.MARGINS());
            PInvoke.DwmEnableBlurBehindWindow(handle, new Windows.Win32.Graphics.Dwm.DWM_BLURBEHIND()
            {
                dwFlags = 3,
                fEnable = true,
                hRgnBlur = PInvoke.CreateRectRgn(-2, -2, -1, -1),
            });
        }

        private bool ClearBackground(nint hwnd, nint hdc)
        {
            if (PInvoke.GetClientRect(new Windows.Win32.Foundation.HWND(hwnd), out var rect))
            {
                var brush = PInvoke.CreateSolidBrush(0);
                FillRect(hdc, ref rect, brush);
                return true;
            }
            return false;
        }

        private unsafe void Monitor_WindowMessageReceived(object? sender, WindowMessageEventArgs e)
        {
            if (e.MessageType == WindowsMessages.WM_ERASEBKGND)
            {
                if (ClearBackground(e.Message.Hwnd, (nint)e.Message.WParam))
                {
                    e.Result = 1;
                    e.Handled = true;
                }
            }
            else if ((int)e.MessageType == 798 /*WM_DWMCOMPOSITIONCHANGED*/)
            {
                ConfigureDwm((ulong)e.Message.Hwnd);
                e.Handled = true;
                e.Result = 0;
            }
        }

        [DllImport("User32", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern unsafe int FillRect(IntPtr hDC, ref Windows.Win32.Foundation.RECT lprc, Windows.Win32.Graphics.Gdi.HBRUSH hbr);
    }
}
