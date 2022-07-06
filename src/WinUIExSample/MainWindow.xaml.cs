using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Graphics;
using WinUIEx;
using WinUIEx.Messaging;

namespace WinUIExSample
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx
    {
        private readonly Queue<string> windowEvents = new Queue<string>(101);
        private readonly WindowMessageMonitor monitor;
        private AcrylicSystemBackdrop acrylicBackdrop = new AcrylicSystemBackdrop();
        private MicaSystemBackdrop micaBackdrop;

        public MainWindow()
        {
            this.InitializeComponent();
            this.SetTitleBarBackgroundColors(Microsoft.UI.Colors.CornflowerBlue);
            PersistenceId = "MainWindow";
            monitor = new WindowMessageMonitor(this);
            var monitors = MonitorInfo.GetDisplayMonitors();
            foreach (var monitor in monitors.Reverse())
                Log("  - " + monitor.ToString());
            Log($"{monitors.Count} monitors detected");
            micaBackdrop = this.Backdrop as MicaSystemBackdrop;
            acrylicBackdrop.DarkFallbackColor = micaBackdrop.DarkFallbackColor;
            acrylicBackdrop.DarkLuminosityOpacity = micaBackdrop.DarkLuminosityOpacity;
            acrylicBackdrop.DarkTintColor = micaBackdrop.DarkTintColor;
            acrylicBackdrop.DarkTintOpacity = micaBackdrop.DarkTintOpacity;
            acrylicBackdrop.LightFallbackColor = micaBackdrop.LightFallbackColor;
            acrylicBackdrop.LightLuminosityOpacity = micaBackdrop.LightLuminosityOpacity;
            acrylicBackdrop.LightTintColor = micaBackdrop.LightTintColor;
            acrylicBackdrop.LightTintOpacity = micaBackdrop.LightTintOpacity;
        }

        protected override void OnPositionChanged(PointInt32 position) => Log($"Position Changed: {position.X},{position.Y}");

        protected override void OnPresenterChanged(AppWindowPresenter newPresenter) => Log($"Presenter Changed: {newPresenter.Kind}");
        protected override bool OnSizeChanged(Size size)
        {
            Log($"Size Changed: {size.Width} x {size.Height}");
            return base.OnSizeChanged(size);
        }

        //protected override void OnThemeChanged(ElementTheme theme) => Log($"Theme Changed: {theme}");

        private void Log(string message)
        {
            if (!DispatcherQueue.HasThreadAccess)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    Log(message);
                });
                return;
            }
            windowEvents.Enqueue(message);
            if (windowEvents.Count > 100)
                windowEvents.Dequeue();
            if (WindowEventLog != null)
                WindowEventLog.Text = string.Join('\n', windowEvents.Reverse());
        }

        private void Center_Click(object sender, RoutedEventArgs e) => this.CenterOnScreen();

        private void MaximizeWindow_Click(object sender, RoutedEventArgs e) => this.Maximize();

        private void RestoreWindow_Click(object sender, RoutedEventArgs e) => this.Restore();

        private async void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Minimize();
            await Task.Delay(2000);
            this.Restore();
        }

        private async void HideWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            await Task.Delay(2000);
            this.Restore();
        }

        private async void BringToFront_Click(object sender, RoutedEventArgs e)
        {
            await Task.Delay(2000);
            this.BringToFront();
        }

        private void Presenter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var index = ((ComboBox)sender).SelectedIndex;
            if (index == 0)
                PresenterKind = Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped;
            else if (index == 1)
                PresenterKind = Microsoft.UI.Windowing.AppWindowPresenterKind.CompactOverlay;
            else if (index == 2)
                PresenterKind = Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen;
        }

        private async void ShowDialog_Click(object sender, RoutedEventArgs e)
        {
            var commands = new List<Windows.UI.Popups.IUICommand>();
            commands.Add(new Windows.UI.Popups.UICommand("OK"));
            commands.Add(new Windows.UI.Popups.UICommand("Maybe"));
            commands.Add(new Windows.UI.Popups.UICommand("Cancel"));
            var result = await ShowMessageDialogAsync("This is a simple message dialog", commands, cancelCommandIndex: 2, title: "Dialog title");
            Log("You clicked: " + result.Label);
        }

        private void WMMessages_Toggled(object sender, RoutedEventArgs e)
        {
            if (((ToggleSwitch)sender).IsOn)
                monitor.WindowMessageReceived += WindowMessageReceived;
            else
                monitor.WindowMessageReceived -= WindowMessageReceived;
        }

        private void WindowMessageReceived(object sender, WindowMessageEventArgs e)
        {
            Log(e.Message.ToString());
        }

        private void Backdrop_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (micaBackdrop is null) return; // Still loading
            switch (((ComboBox)sender).SelectedIndex)
            {
                case 1: this.Backdrop = acrylicBackdrop; break;
                case 2: this.Backdrop = micaBackdrop; break;
                default: this.Backdrop = null; break;
            }
        }

        private void Theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ElementTheme theme;
            switch (((ComboBox)sender).SelectedIndex)
            {
                case 1: theme = ElementTheme.Dark; break;
                case 2: theme = ElementTheme.Light; break;
                default: theme = ElementTheme.Default; break;
            }
            LayoutRoot.RequestedTheme = theme;
        }

        private async void DoOAuth_Click(object sender, RoutedEventArgs e)
        {
            string clientId = "VxIw33TIRCi1Tbk6pjh2i";
            string clientSecret = "eEkUe5e9gUpO6KOYdL5pKTi683LADpi5_izZdHCI8Mndy32B";
            string state = DateTime.Now.Ticks.ToString();
            string callbackUri = "winuiex://";
            string authorizeUri = $"https://www.oauth.com/playground/auth-dialog.html?response_type=code&client_id={clientId}&redirect_uri={Uri.EscapeDataString(callbackUri)}&scope=photo+offline_access";// &state={state}";

            // login: nice-ferret@example.com
            // password: Black-Capybara-83
            _ = base.ShowMessageDialogAsync("Login: nice-ferret@example.com\npassword: Black-Capybara-83", "Enter credentials in browser");
            var result = await WebAuthenticator.AuthenticateAsync(new Uri(authorizeUri), new Uri(callbackUri));
            Log("Logged in. Code returned: " + result.Properties["code"]);
        }
    }
}
