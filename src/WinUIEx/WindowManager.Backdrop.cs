using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.Storage;
using WinUIEx.Messaging;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.UI.Composition.SystemBackdrops;
using WinRT;
using Windows.UI;
using System.ComponentModel;

namespace WinUIEx
{
    /// <summary>
    /// Manages Window size, and ensures window correctly resizes during DPI changes to keep consistent
    /// DPI-independent sizing.
    /// </summary>
    public partial class WindowManager : IDisposable
    {
        private IntPtr m_dispatcherQueueController = IntPtr.Zero;
        private ISystemBackdropController? currentController;
        private SystemBackdropConfiguration? BackdropConfiguration;
        private SystemBackdrop? m_backdrop;

        /// <summary>
        /// Gets or sets the system backdrop for the window.
        /// Note: Windows 10 doesn't support these, so will fall back to default backdrop.
        /// </summary>
        /// <seealso cref="MicaSystemBackdrop"/>
        /// <seealso cref="AcrylicSystemBackdrop"/>
        public SystemBackdrop? Backdrop
        {
            get => m_backdrop;
            set
            {
                if (m_backdrop != value)
                {
                    if (m_backdrop != null)
                        m_backdrop.IsDirty -= Backdrop_IsDirty;
                    m_backdrop = value;
                    CleanUpBackdrop();
                    if (m_backdrop != null)
                        m_backdrop.IsDirty += Backdrop_IsDirty;
                    if (m_backdrop is not null && _window.Visible)
                        InitBackdrop();
                }
            }
        }

        private static bool IsEmpty(Windows.UI.Color c) => c.A == 0 && c.R == 0 && c.G == 0 && c.B == 0;

        private void Backdrop_IsDirty(object? sender, EventArgs e)
        {
            if (currentController != null && BackdropConfiguration != null && m_backdrop != null)
                m_backdrop.UpdateController(currentController, BackdropConfiguration.Theme);
        }

        /// <summary>
        /// Gets the currently active window backdrop.
        /// </summary>
        public ISystemBackdropController? ActiveBackdropController => currentController;

        private Microsoft.UI.Xaml.Media.SolidColorBrush? _fallbackBackdrop;

        private void InitBackdrop()
        {
            if(m_backdrop is null)
            {
                CleanUpBackdrop();
                return;
            }
            if (!m_backdrop.IsSupported)
            {
                var rootElement = _window.Content as FrameworkElement;
                if (rootElement is not null)
                {
                    // Initial state.
                    _fallbackBackdrop = new Microsoft.UI.Xaml.Media.SolidColorBrush(rootElement.ActualTheme == ElementTheme.Dark ? m_backdrop.DarkFallbackColor : m_backdrop.LightFallbackColor);
                    if (rootElement is Microsoft.UI.Xaml.Controls.Control control && control.Background is null)
                    {
                        control.Background = _fallbackBackdrop;
                    }
                    else if (rootElement is Microsoft.UI.Xaml.Controls.Panel panel && panel.Background is null)
                    {
                        panel.Background = _fallbackBackdrop;
                    }
                    else
                    {
                        _fallbackBackdrop = null;
                        return;
                    }
                    // This should probably be weak in the rare event the root content changes
                    // Unfortunately there's no good event to detect changes though.
                    rootElement.ActualThemeChanged += (s, e) =>
                    {
                        bool isDark = s.ActualTheme == ElementTheme.Dark;
                        if (_fallbackBackdrop != null && m_backdrop != null)
                        {
                            _fallbackBackdrop.Color = isDark ? m_backdrop.DarkFallbackColor : m_backdrop.LightFallbackColor;
                        }
                    };
                }
                return;
            }

            if (BackdropConfiguration is null)
            {
                EnsureDispatcherQueueController();
                BackdropConfiguration = new SystemBackdropConfiguration();

                var rootElement = _window.Content as FrameworkElement;
                if (rootElement is not null)
                {
                    // This should probably be weak in the rare event the root content changes
                    // Unfortunately there's no good event to detect changes though.
                    rootElement.ActualThemeChanged += (s, e) =>
                    {
                        if (BackdropConfiguration != null && currentController != null)
                        {
                            BackdropConfiguration.Theme = ConvertToSystemBackdropTheme(s.ActualTheme);
                            m_backdrop.UpdateController(currentController, BackdropConfiguration.Theme);
                        }
                    };

                    // Initial state.
                    BackdropConfiguration.Theme = ConvertToSystemBackdropTheme(rootElement.ActualTheme);
                }
            }
            if (currentController != null)
            {
                currentController.Dispose();
                currentController = null;
            }
            var controller = m_backdrop.CreateController();
            m_backdrop.UpdateController(controller, BackdropConfiguration.Theme);
            m_backdrop.ApplyController(controller, _window.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>(), BackdropConfiguration);
            currentController = controller;
        }

        private void CleanUpBackdrop()
        {
            (currentController as MicaController)?.RemoveAllSystemBackdropTargets();
            (currentController as DesktopAcrylicController)?.RemoveAllSystemBackdropTargets();
            currentController?.Dispose();
            currentController = null;
            if (_fallbackBackdrop is not null)
            {
                var rootElement = _window.Content as FrameworkElement;
                if (_window.Content is Microsoft.UI.Xaml.Controls.Control control && control.Background == _fallbackBackdrop)
                {
                    control.Background = null;
                }
                else if (_window.Content is Microsoft.UI.Xaml.Controls.Panel panel && panel.Background == _fallbackBackdrop)
                {
                    panel.Background = null;
                }
                _fallbackBackdrop = null;
            }
        }

        private void EnsureDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() == null && m_dispatcherQueueController == IntPtr.Zero)
            {
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                IntPtr m_dispatcherQueueControllerPtr = IntPtr.Zero;
                CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
            }
        }

        private static SystemBackdropTheme ConvertToSystemBackdropTheme(ElementTheme theme)
        {
            switch (theme)
            {
                case ElementTheme.Dark:
                    return SystemBackdropTheme.Dark;
                case ElementTheme.Light:
                    return SystemBackdropTheme.Light;
                default:
                    return SystemBackdropTheme.Default;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out] ref IntPtr dispatcherQueueController);
    }
}
