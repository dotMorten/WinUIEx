using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.Storage;
using WinUIEx.Messaging;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.UI.Composition.SystemBackdrops;
using WinRT;

namespace WinUIEx
{
    /// <summary>
    /// Manages Window size, and ensures window correctly resizes during DPI changes to keep consistent
    /// DPI-independent sizing.
    /// </summary>
    internal partial class WindowManager : IDisposable
    {
        private object m_dispatcherQueueController = null;
        private ISystemBackdropController? currentController;
        private SystemBackdropConfiguration BackdropConfiguration;
        private Backdrop m_backdrop;
        private Backdrop m_currentBackdrop = Backdrop.Default;

        /// <summary>
        /// Gets or sets the system backdrop of the window.
        /// Note: Windows 10 doesn't support these, so will fall back to default backdrop.
        /// </summary>
        public Backdrop Backdrop
        {
            get => m_backdrop;
            set
            {
                if (m_backdrop != value)
                {
                    m_backdrop = value;
                    if (_window.Visible)
                        InitBackdrop();
                }
            }
        }

        private void InitBackdrop()
        {
            if (m_currentBackdrop == m_backdrop) return;
            if (m_backdrop == Backdrop.Default ||
                Backdrop == Backdrop.Acrylic && !DesktopAcrylicController.IsSupported() ||
                Backdrop == Backdrop.Mica && !MicaController.IsSupported())
            {
                CleanUpBackdrop();
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
                        if (BackdropConfiguration != null)
                            BackdropConfiguration.Theme = ConvertToSystemBackdropTheme(rootElement.ActualTheme);
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
            if (Backdrop == Backdrop.Acrylic)
            {
                var m_acrylicController = new DesktopAcrylicController();
                m_acrylicController.AddSystemBackdropTarget(_window.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_acrylicController.SetSystemBackdropConfiguration(BackdropConfiguration);
                currentController = m_acrylicController;
            }
            else if (Backdrop == Backdrop.Mica)
            {
                var m_micaController = new MicaController();
                m_micaController.SetSystemBackdropConfiguration(BackdropConfiguration);
                m_micaController.AddSystemBackdropTarget(_window.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                currentController = m_micaController;
            }
            m_currentBackdrop = m_backdrop;
        }

        private void CleanUpBackdrop()
        {
            currentController?.Dispose();
            currentController = null;
            m_currentBackdrop = Backdrop.Default;
        }

        private void EnsureDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() == null && m_dispatcherQueueController is null)
            {
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                CreateDispatcherQueueController(options, ref m_dispatcherQueueController!);
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
        private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

    }
}
