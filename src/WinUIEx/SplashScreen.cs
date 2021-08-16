#if EXPERIMENTAL
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace WinUIEx
{
    /// <summary>
    /// A splash screen window that shows with no chrome, and once <see cref="SplashScreen.OnLoading"/> has completed,
    /// opens a new window
    /// </summary>
    public class SplashScreen : Window
    {
        private Window? _window;
        private Type? _windowType;        

        /// <summary>
        /// Creates and activates a new splashscreen, and opens the specified window once complete.
        /// </summary>
        /// <param name="window">Window to open once splash screen is complete</param>
        public SplashScreen(Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            this.Activated += SplashScreen_Activated;
            this.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                this.Activate();
            });
        }

        /// <summary>
        /// Creates and activates a new splashscreen, and creates and opens the specified window type once complete.
        /// </summary>
        /// <param name="window">Type of window to create. Must have an empty constructor</param>
        public SplashScreen(Type window)
        {
            if (!window.IsSubclassOf(typeof(Window)) && window != typeof(Window))
                throw new ArgumentException("Type must be a Window");
            _windowType = window ?? throw new ArgumentNullException(nameof(window));
            this.Activated += SplashScreen_Activated;
            this.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                this.Activate();
            });
        }

        private async void Content_Loaded(object sender, RoutedEventArgs e)
        {
            this.SetAlwaysOnTop(true);
            double h = Height;
            double w = Width;
            if (Content is FrameworkElement f)
            {
                if (double.IsNaN(Width) && f.DesiredSize.Width > 0)
                    w = f.DesiredSize.Width;
                if (double.IsNaN(Height) && f.DesiredSize.Height > 0)
                    h = f.DesiredSize.Height;
            }
            if (double.IsNaN(w))
                w = 640;
            if (double.IsNaN(h))
                h = 480;

            this.CenterOnScreen(w, h);
            await OnLoading();
            if (_windowType != null)
                _window = Activator.CreateInstance(_windowType) as Window;
            _window?.Activate();
            this.Close();
            _window?.SetForegroundWindow();
            Completed?.Invoke(this, _window);
            _window = null;
        }

        private void SplashScreen_Activated(object sender, WindowActivatedEventArgs args)
        {
            this.Activated -= SplashScreen_Activated;
            this.HideWindow(); // Hides until content is loaded
            var hwnd = this.GetWindowHandle();
            HwndExtensions.ToggleWindowStyle(hwnd, false, WindowStyle.TiledWindow);
            var content = this.Content as FrameworkElement;
            if (content is null || content.IsLoaded)
                Content_Loaded(this, new RoutedEventArgs());
            else
                content.Loaded += Content_Loaded;
        }

        /// <summary>
        /// Override to display loading progress or delay loading of main window
        /// </summary>
        /// <returns></returns>
        protected virtual Task OnLoading()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets or sets the width of the splash screen. Set to NaN to size for content
        /// </summary>
        public double Width { get; set; } = 640;
        /// <summary>
        /// Gets or sets the height of the splash screen. Set to NaN to size for content
        /// </summary>
        public double Height { get; set; } = 480;

        /// <summary>
        /// Gets or sets a value indicating whether the splash screen should be top-most
        /// </summary>
        public bool IsAlwaysOnTop { get; set; } = true;

        /// <summary>
        /// Raised once the splash screen has completed <see cref="OnLoading"/>.
        /// </summary>
        public event EventHandler<Window?>? Completed;
    }
}
#endif