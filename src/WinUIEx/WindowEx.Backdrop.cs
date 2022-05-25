using System.Runtime.InteropServices;
using WinRT;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;

namespace WinUIEx
{
    /// <summary>
    /// The backdrop type to apply to a <see cref="WindowEx"/> window.
    /// </summary>
    /// <seealso cref="WindowEx.Backdrop"/>
    public enum Backdrop
    {
        /// <summary>
        /// No backdrop applied
        /// </summary>
        Default,

        /// <summary>
        /// Acrylic semi-transparent backdrop
        /// </summary>
        Acrylic,

        /// <summary>
        /// Mica backdrop
        /// </summary>
        Mica
    }

    public partial class WindowEx
    {
        object m_dispatcherQueueController = null;
        ISystemBackdropController? currentController;

        SystemBackdropConfiguration Configuration;
       
        private Backdrop m_Backdrop;

        /// <summary>
        /// Gets or sets the system backdrop of the window.
        /// Note: Windows 10 doesn't support these, so will fall back to default backdrop.
        /// </summary>
        public Backdrop Backdrop
        {
            get => m_Backdrop;
            set
            {
                if (m_Backdrop != value)
                {
                    m_Backdrop = value;
                    if (this.Visible)
                        InitBackdrop();
                }
            }
        }

        private void InitBackdrop()
        {
            if (m_Backdrop == Backdrop.Default ||
                Backdrop == Backdrop.Acrylic && !DesktopAcrylicController.IsSupported() ||
                Backdrop == Backdrop.Mica && !MicaController.IsSupported())
            {
                CleanUpBackdrop();
                return;
            }

            if (Configuration is null)
            {
                EnsureDispatcherQueueController();
                Configuration = new SystemBackdropConfiguration();
                this.Activated += (s, args) =>
                {
                    if (Configuration != null)
                        Configuration.IsInputActive = args.WindowActivationState != Microsoft.UI.Xaml.WindowActivationState.Deactivated;
                };

                var rootElement = windowArea as Microsoft.UI.Xaml.FrameworkElement;
                if (rootElement is not null)
                {
                    rootElement.ActualThemeChanged += (s, e) =>
                    {
                        if (Configuration != null)
                            Configuration.Theme = ConvertToSystemBackdropTheme(rootElement.ActualTheme);
                    };

                    // Initial state.
                    Configuration.Theme = ConvertToSystemBackdropTheme(rootElement.ActualTheme);
                }
                this.Closed += (s,e) => CleanUpBackdrop();
            }
            if(currentController != null)
            {
                currentController.Dispose();
                currentController = null;
            }
            if(Backdrop == Backdrop.Acrylic)
            {
                var m_acrylicController = new DesktopAcrylicController();
                m_acrylicController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_acrylicController.SetSystemBackdropConfiguration(Configuration);
                currentController = m_acrylicController;
            }
            else if(Backdrop == Backdrop.Mica)
            {
                var m_micaController = new MicaController();
                m_micaController.SetSystemBackdropConfiguration(Configuration);
                m_micaController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                currentController = m_micaController;
            }
        }
        private void CleanUpBackdrop()
        {
            currentController?.Dispose();
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