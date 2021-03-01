using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace WinUIEx
{
    public class SplashScreen : Window
    {
        private Window? _window;

        public SplashScreen(Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            this.Activated += SplashScreen_Activated;
            this.DispatcherQueue.TryEnqueue(Microsoft.System.DispatcherQueuePriority.Normal, () =>
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
            _window?.Activate();
            this.Close();
            _window?.SetForegroundWindow();
            _window = null;
        }

        private void SplashScreen_Activated(object sender, WindowActivatedEventArgs args)
        {
            this.Activated -= SplashScreen_Activated;
            var hwnd = this.GetWindowHandle();
            HwndExtensions.ToggleWindowStyle(hwnd, false, WindowStyle.TiledWindow);
            HwndExtensions.ToggleWindowStyle(hwnd, true, WindowStyle.ExLayered);
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
    }
}
