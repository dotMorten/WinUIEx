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
    internal partial class WindowManager : IDisposable
    {
        private object? m_dispatcherQueueController = null;
        private ISystemBackdropController? currentController;
        private SystemBackdropConfiguration? BackdropConfiguration;
        private BackdropSettings? m_backdrop;
        private Backdrop m_currentBackdrop = WinUIEx.Backdrop.Default;

        /// <summary>
        /// Gets or sets the system backdrop of the window.
        /// Note: Windows 10 doesn't support these, so will fall back to default backdrop.
        /// </summary>
        public BackdropSettings? Backdrop
        {
            get => m_backdrop;
            set
            {
                if (m_backdrop != value)
                {
                    if(m_backdrop != null)
                        ((INotifyPropertyChanged)m_backdrop).PropertyChanged -= Backdrop_PropertyChanged;
                    m_backdrop = value;
                    if (m_backdrop != null) 
                        ((INotifyPropertyChanged)m_backdrop).PropertyChanged += Backdrop_PropertyChanged;
                    if (_window.Visible)
                        InitBackdrop();
                }
            }
        }
        private static bool IsEmpty(Windows.UI.Color c) => c.A == 0 && c.R == 0 && c.G == 0 && c.B == 0;

        private void Backdrop_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (Backdrop is null)
                return;
            if(e.PropertyName == nameof(BackdropSettings.Kind))
            {
                InitBackdrop();
            }
            if (ActiveBackdropController is MicaController micaController)
            {
                if (e.PropertyName == nameof(BackdropSettings.TintOpacity) && !double.IsNaN(Backdrop.TintOpacity))
                    micaController.TintOpacity = (float)Backdrop.TintOpacity;
                if (e.PropertyName == nameof(BackdropSettings.LuminosityOpacity) && !double.IsNaN(Backdrop.LuminosityOpacity))
                    micaController.LuminosityOpacity = (float)Backdrop.LuminosityOpacity;
                if (e.PropertyName == nameof(BackdropSettings.TintColor) && !IsEmpty(Backdrop.TintColor))
                    micaController.TintColor = Backdrop.TintColor;
                if (e.PropertyName == nameof(BackdropSettings.FallbackColor) && !IsEmpty(Backdrop.FallbackColor))
                    micaController.FallbackColor = Backdrop.FallbackColor;
            }
            else if (ActiveBackdropController is DesktopAcrylicController acrylicController)
            {
                if (e.PropertyName == nameof(BackdropSettings.TintOpacity) && !double.IsNaN(Backdrop.TintOpacity))
                    acrylicController.TintOpacity = (float)Backdrop.TintOpacity;
                if (e.PropertyName == nameof(BackdropSettings.LuminosityOpacity) && !double.IsNaN(Backdrop.LuminosityOpacity))
                    acrylicController.LuminosityOpacity = (float)Backdrop.LuminosityOpacity;
                if (e.PropertyName == nameof(BackdropSettings.TintColor) && !IsEmpty(Backdrop.TintColor))
                    acrylicController.TintColor = Backdrop.TintColor;
                if (e.PropertyName == nameof(BackdropSettings.FallbackColor) && !IsEmpty(Backdrop.FallbackColor))
                    acrylicController.FallbackColor = Backdrop.FallbackColor;
            }
        }

        public ISystemBackdropController? ActiveBackdropController => currentController;

        private void InitBackdrop()
        {
            var kind = (m_backdrop?.Kind ?? WinUIEx.Backdrop.Default);
            if (m_currentBackdrop == kind) return;
            if (kind == WinUIEx.Backdrop.Default ||
                kind == WinUIEx.Backdrop.Acrylic && !DesktopAcrylicController.IsSupported() ||
                kind == WinUIEx.Backdrop.Mica && !MicaController.IsSupported())
            {
                CleanUpBackdrop();
                return;
            }
            if (Backdrop is null)
                return;

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
                        (currentController as MicaController)?.SetSystemBackdropConfiguration(BackdropConfiguration);
                        (currentController as DesktopAcrylicController)?.SetSystemBackdropConfiguration(BackdropConfiguration);
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
            if (kind == WinUIEx.Backdrop.Acrylic)
            {
                var m_acrylicController = new DesktopAcrylicController();
                if (!double.IsNaN(Backdrop.TintOpacity))
                    m_acrylicController.TintOpacity = (float)Backdrop.TintOpacity;
                if (!double.IsNaN(Backdrop.LuminosityOpacity))
                    m_acrylicController.LuminosityOpacity = (float)Backdrop.LuminosityOpacity;
                if (!IsEmpty(Backdrop.TintColor))
                    m_acrylicController.TintColor = Backdrop.TintColor;
                if (!IsEmpty(Backdrop.FallbackColor))
                    m_acrylicController.FallbackColor = Backdrop.FallbackColor;
                m_acrylicController.AddSystemBackdropTarget(_window.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_acrylicController.SetSystemBackdropConfiguration(BackdropConfiguration);
                currentController = m_acrylicController;
            }
            else if (kind == WinUIEx.Backdrop.Mica)
            {
                var m_micaController = new MicaController();
                if (!double.IsNaN(Backdrop.TintOpacity))
                    m_micaController.TintOpacity = (float)Backdrop.TintOpacity;
                if (!double.IsNaN(Backdrop.LuminosityOpacity))
                    m_micaController.LuminosityOpacity = (float)Backdrop.LuminosityOpacity;
                if (!IsEmpty(Backdrop.TintColor))
                    m_micaController.TintColor = Backdrop.TintColor;
                if (!IsEmpty(Backdrop.FallbackColor))
                    m_micaController.FallbackColor = Backdrop.FallbackColor;
                m_micaController.SetSystemBackdropConfiguration(BackdropConfiguration);
                m_micaController.AddSystemBackdropTarget(_window.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                currentController = m_micaController;
            }
            m_currentBackdrop = kind;
        }

        private void CleanUpBackdrop()
        {
            (currentController as MicaController)?.RemoveAllSystemBackdropTargets();
            (currentController as DesktopAcrylicController)?.RemoveAllSystemBackdropTargets();
            currentController?.Dispose();
            currentController = null;
            m_currentBackdrop = WinUIEx.Backdrop.Default;
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
