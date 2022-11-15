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
            string clientId = "VxIw33TIRCi1Tbk6pjh2i";
            string clientSecret = "eEkUe5e9gUpO6KOYdL5pKTi683LADpi5_izZdHCI8Mndy32B";
            string state = DateTime.Now.Ticks.ToString();
            Uri callbackUri = new Uri("mauiex://");
            Uri authorizeUri = new Uri($"https://www.oauth.com/playground/auth-dialog.html?response_type=code&client_id={clientId}&redirect_uri={Uri.EscapeDataString(callbackUri.OriginalString)}&scope=photo+offline_access");// &state={state}";

            _ = Navigation.PushModalAsync(new ContentPage()
            {
                Content = new Label() { Text = $"Waiting for sign in in your browser\n Username: 'nice-ferret@example.com'\n Password: Black-Capybara-83" }
            });
#if WINDOWS
            var result = await WinUIEx.WebAuthenticator.AuthenticateAsync(authorizeUri, callbackUri);
#else
            var result = await Microsoft.Maui.Authentication.WebAuthenticator.AuthenticateAsync(new WebAuthenticatorOptions()
            {
                Url = authorizeUri, CallbackUrl = callbackUri
            });
#endif
            if (Navigation.ModalStack.Count > 0)
                _ = Navigation.PopModalAsync();
            await DisplayAlert("Success!", "Signed in", "OK");
        }

        private void Picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedIndex = ((Picker)sender).SelectedIndex;
            var window = this.Window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (window is null) return;
            var manager = WinUIEx.WindowManager.Get(window);
            if (manager is null) return;
            switch (selectedIndex)
            {
                case 0: manager.Backdrop = new AcrylicSystemBackdrop(); break;
                case 1: manager.Backdrop = new MicaSystemBackdrop(); break;
                case 2: manager.Backdrop = null; break;
            }
        }
    }
}