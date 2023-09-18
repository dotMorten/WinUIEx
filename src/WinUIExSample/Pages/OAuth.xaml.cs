using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUIExSample.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OAuth : Page
    {
        public OAuth()
        {
            this.InitializeComponent();
        }
        private CancellationTokenSource? oauthCancellationSource;
        private async void DoOAuth_Click(object sender, RoutedEventArgs e)
        {
            SigninButton.IsEnabled = false;
            Result.Text = "Waiting for login in browser...";
            string clientId = "imIwo061j9SUOQYm7O8Oe4HK";
            string clientSecret = "aeApQwwjBl1n_J6nknnxWNONuB0RaEjVHL5yhYdgz5XJOnDi";
            string state = DateTime.Now.ToString();
            string callbackUri = "winuiex://";
            string authorizeUri = $"https://www.oauth.com/playground/auth-dialog.html?response_type=code&client_id={clientId}&redirect_uri={Uri.EscapeDataString(callbackUri)}&scope=photo+offline_access&state={Uri.EscapeDataString(state)}";

            loginDetails.Text = "Login: pleasant-koala@example.com\npassword: Modern-Seahorse-66";
            oauthCancellationSource = new CancellationTokenSource();
            oauthCancellationSource.Token.Register(() => { OAuthWindow.Visibility = Visibility.Collapsed; });
            OAuthWindow.Visibility = Visibility.Visible;
            try
            {
                var result = await WebAuthenticator.AuthenticateAsync(new Uri(authorizeUri), new Uri(callbackUri), oauthCancellationSource.Token);
                OAuthWindow.Visibility = Visibility.Collapsed;
                Result.Text = $"Logged in. Code returned: {result.Properties["code"]}\tState carried: {result.Properties["state"]}";
            }
            catch (TaskCanceledException) {
                Result.Text = "Sign in cancelled";
            }
            SigninButton.IsEnabled = true;
        }

        private void OAuthCancel_Click(object sender, RoutedEventArgs e)
        {
            oauthCancellationSource?.Cancel();
        }

        private void Result_Holding(object sender, HoldingRoutedEventArgs e)
        {

        }
    }
}
