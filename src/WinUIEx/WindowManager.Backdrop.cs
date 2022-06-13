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
            else if (ActiveBackdropController != null)
            {
                UpdateController(ActiveBackdropController);
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
                        {
                             BackdropConfiguration.Theme = ConvertToSystemBackdropTheme(rootElement.ActualTheme);
                            UpdateController(ActiveBackdropController);
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
            if (kind == WinUIEx.Backdrop.Acrylic)
            {
                var acrylicController = new DesktopAcrylicController();
                UpdateController(acrylicController);
                acrylicController.AddSystemBackdropTarget(_window.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                acrylicController.SetSystemBackdropConfiguration(BackdropConfiguration);
                currentController = acrylicController;
            }
            else if (kind == WinUIEx.Backdrop.Mica)
            {
                var micaController = new MicaController();
                UpdateController(micaController);
                micaController.SetSystemBackdropConfiguration(BackdropConfiguration);
                micaController.AddSystemBackdropTarget(_window.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                currentController = micaController;
            }
            m_currentBackdrop = kind;
        }

        private void UpdateController(ISystemBackdropController? controller)
        {
            if (Backdrop is null)
                return;
            bool isDark = BackdropConfiguration?.Theme == SystemBackdropTheme.Dark;
            if (controller is MicaController mica)
            {
                if (!double.IsNaN(isDark ? Backdrop.DarkTintOpacity : Backdrop.LightTintOpacity))
                    mica.TintOpacity = (float)(isDark ? Backdrop.DarkTintOpacity : Backdrop.LightTintOpacity);
                if (!double.IsNaN(isDark ? Backdrop.DarkLuminosityOpacity : Backdrop.LightLuminosityOpacity))
                    mica.LuminosityOpacity = (float)(isDark ? Backdrop.DarkLuminosityOpacity : Backdrop.LightLuminosityOpacity);
                if (!IsEmpty(isDark ? Backdrop.DarkTintColor : Backdrop.LightTintColor))
                    mica.TintColor = isDark ? Backdrop.DarkTintColor : Backdrop.LightTintColor;
                if (!IsEmpty(isDark?Backdrop.DarkFallbackColor : Backdrop.LightFallbackColor))
                    mica.FallbackColor = isDark ? Backdrop.DarkFallbackColor : Backdrop.LightFallbackColor;
            }
            else if(controller is DesktopAcrylicController acrylic)
            {
                if (!double.IsNaN(isDark ? Backdrop.DarkTintOpacity : Backdrop.LightTintOpacity))
                    acrylic.TintOpacity = (float)(isDark ? Backdrop.DarkTintOpacity : Backdrop.LightTintOpacity);
                if (!double.IsNaN(isDark ? Backdrop.DarkLuminosityOpacity : Backdrop.LightLuminosityOpacity))
                    acrylic.LuminosityOpacity = (float)(isDark ? Backdrop.DarkLuminosityOpacity : Backdrop.LightLuminosityOpacity);
                if (!IsEmpty(isDark ? Backdrop.DarkTintColor : Backdrop.LightTintColor))
                    acrylic.TintColor = isDark ? Backdrop.DarkTintColor : Backdrop.LightTintColor;
                if (!IsEmpty(isDark ? Backdrop.DarkFallbackColor : Backdrop.LightFallbackColor))
                    acrylic.FallbackColor = isDark ? Backdrop.DarkFallbackColor : Backdrop.LightFallbackColor;
            }
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
