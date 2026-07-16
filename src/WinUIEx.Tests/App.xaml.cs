using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUIUnitTests
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            var currentInstance = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent();
            var activationArgs = currentInstance.GetActivatedEventArgs();
            if (TryCompleteOAuthAuthorization(activationArgs))
                return;
            if (TryInitializeOAuthWorker(activationArgs))
            {
                this.InitializeComponent();
                return;
            }
            if (WebAuthenticator.CheckOAuthRedirectionActivation(activationArgs))
                return;
            this.InitializeComponent();
        }

        internal const string OAuthAuthorizeScheme = "winuiex-test-authorize";
        internal const string OAuthCallbackScheme = "winuiex-test-callback";
        internal const string OAuthWorkerScheme = "winuiex-test-worker";
        private static OAuthWorkerOptions oauthWorkerOptions;

        private sealed class OAuthWorkerOptions
        {
            public OAuthWorkerOptions(string completionEventName, string code)
            {
                CompletionEventName = completionEventName;
                Code = code;
            }

            public string CompletionEventName { get; }

            public string Code { get; }
        }

        private static bool TryInitializeOAuthWorker(AppActivationArguments activationArgs)
        {
            if (activationArgs.Kind != ExtendedActivationKind.Protocol ||
                activationArgs.Data is not IProtocolActivatedEventArgs protocolArgs ||
                protocolArgs.Uri.Scheme != OAuthWorkerScheme)
            {
                return false;
            }

            var query = System.Web.HttpUtility.ParseQueryString(protocolArgs.Uri.Query);
            var completionEventName = query["completionEvent"];
            var code = query["code"];
            if (string.IsNullOrEmpty(completionEventName) || string.IsNullOrEmpty(code))
                return false;

            WebAuthenticator.CheckOAuthRedirectionActivation(null);
            oauthWorkerOptions = new OAuthWorkerOptions(completionEventName, code);
            return true;
        }

        private static bool TryCompleteOAuthAuthorization(AppActivationArguments activationArgs)
        {
            if (activationArgs.Kind != ExtendedActivationKind.Protocol ||
                activationArgs.Data is not IProtocolActivatedEventArgs protocolArgs ||
                protocolArgs.Uri.Scheme != OAuthAuthorizeScheme)
            {
                return false;
            }

            var authorizeQuery = System.Web.HttpUtility.ParseQueryString(protocolArgs.Uri.Query);
            var callback = authorizeQuery["callback"];
            var state = authorizeQuery["state"];
            var readyEventName = authorizeQuery["readyEvent"];
            if (string.IsNullOrEmpty(callback) || string.IsNullOrEmpty(state) || string.IsNullOrEmpty(readyEventName))
                return false;

            var callbackUri = new UriBuilder(callback);
            var callbackQuery = System.Web.HttpUtility.ParseQueryString(callbackUri.Query);
            callbackQuery["state"] = state;
            callbackQuery["code"] = authorizeQuery["code"] ?? "integration-test-code";
            callbackUri.Query = callbackQuery.ToString();

            using var readyEvent = System.Threading.EventWaitHandle.OpenExisting(readyEventName);
            if (!readyEvent.WaitOne(TimeSpan.FromSeconds(10)))
                return false;

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "rundll32.exe",
                Arguments = $"url.dll,FileProtocolHandler \"{callbackUri.Uri.OriginalString}\"",
                UseShellExecute = true,
            });
            System.Diagnostics.Process.GetCurrentProcess().Kill();
            return true;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            if (oauthWorkerOptions is not null)
            {
                RunOAuthWorker(oauthWorkerOptions);
                return;
            }

            m_window = new WindowEx();
            m_window.Activate();
            Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.CreateDefaultUI();
            Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.Run(Environment.CommandLine);
        }

        private static async void RunOAuthWorker(OAuthWorkerOptions options)
        {
            try
            {
                using var completionEvent = System.Threading.EventWaitHandle.OpenExisting(options.CompletionEventName);
                var readyEventName = $@"Local\WinUIEx.WebAuthenticatorTests.{Guid.NewGuid():N}";
                using var readyEvent = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.ManualReset, readyEventName);

                var callbackUri = new Uri($"{OAuthCallbackScheme}://complete");
                var authorizeUri = new UriBuilder($"{OAuthAuthorizeScheme}://authorize");
                var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
                query["callback"] = callbackUri.OriginalString;
                query["readyEvent"] = readyEventName;
                query["code"] = options.Code;
                authorizeUri.Query = query.ToString();

                using var cancellationTokenSource = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                var authenticationTask = WebAuthenticator.AuthenticateAsync(authorizeUri.Uri, callbackUri, cancellationTokenSource.Token);
                readyEvent.Set();
                var result = await authenticationTask;
                if (result.Properties["code"] == options.Code)
                    completionEvent.Set();
            }
            finally
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
        }

        private static Window m_window;

        public static Window Window => m_window;
    }
}
