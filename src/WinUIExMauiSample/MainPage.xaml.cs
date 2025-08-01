using Microsoft.UI.Xaml.Media;
using WinUIEx;

namespace WinUIExMauiSample
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void OnFullScreenClicked(object sender, EventArgs e)
        {
#if WINDOWS
            var manager = WinUIEx.WindowManager.Get(this.Window.Handler.PlatformView as Microsoft.UI.Xaml.Window);
            if (manager.PresenterKind == Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped)
                manager.PresenterKind = Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen;
            else
                manager.PresenterKind = Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped;
#endif
        }

        private async void OnOAuthClicked(object sender, EventArgs e)
        {
            using var server = new WinUIExSample.MockOAuthServer();

            string clientId = "imIwo061j9SUOQYm7O8Oe4HK";
            string callbackUri = "mauiex://";
            string authorizeUri = $"{server.Url}?response_type=code&client_id={clientId}&redirect_uri={Uri.EscapeDataString(callbackUri)}&scope=photo+offline_access";

            _ = Navigation.PushModalAsync(new ContentPage()
            {
                Content = new Label() { Text = $"Waiting for sign in in your browser..." }
            });
            string code = "";
#if WINDOWS
#if UNPACKAGED
            // Packaged app uses appxmanifest for protocol activation. Unpackaged apps must manually register
            Microsoft.Windows.AppLifecycle.ActivationRegistrationManager.RegisterForProtocolActivation("mauiex", "Assets\\Square150x150Logo.scale-100", "WinUI EX Maui", null);
#endif
            try
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var result = await WinUIEx.WebAuthenticator.AuthenticateAsync(new Uri(authorizeUri), new Uri(callbackUri));
#pragma warning restore CS0618 // Type or member is obsolete
            }
            finally
            {
#if UNPACKAGED
                Microsoft.Windows.AppLifecycle.ActivationRegistrationManager.UnregisterForProtocolActivation("mauiex", null);
#endif
            }

#else
            var result = await Microsoft.Maui.Authentication.WebAuthenticator.AuthenticateAsync(new WebAuthenticatorOptions()
            {
                Url = authorizeUri, CallbackUrl = callbackUri
            });
            code = result.Properties["code"];
#endif
            if (Navigation.ModalStack.Count > 0)
                _ = Navigation.PopModalAsync();
#if WINDOWS
            // On Windows, we can bring the main window to the front after authentication in the browser
            var window = this.Window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            window?.SetForegroundWindow();
#endif
            await DisplayAlert("Success!", $"Signed in. Access code: {code}", "OK");
        }

        private void Picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedIndex = ((Picker)sender).SelectedIndex;
            var window = this.Window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (window is null) return;
            switch (selectedIndex)
            {
                case 0: window.SystemBackdrop = new DesktopAcrylicBackdrop(); break;
                case 1: window.SystemBackdrop = new MicaBackdrop(); break;
                case 2: window.SystemBackdrop = new TransparentTintBackdrop(); break;
                case 3: window.SystemBackdrop = null; break;
            }
        }
    }
}