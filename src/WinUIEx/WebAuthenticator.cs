using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Windows.ApplicationModel.Activation;

namespace WinUIEx
{
    /// <summary>
    /// Handles OAuth redirection to the system browser and re-activation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Your app must be configured for OAuth. In you app package's <c>Package.appxmanifest</c> under Declarations, add a 
    /// Protocol declaration and add the scheme you registered for your application's oauth redirect url under "Name".
    /// </para>
    /// </remarks>
    public sealed class WebAuthenticator
    {
        /// <summary>
        /// Begin an authentication flow by navigating to the specified url and waiting for a callback/redirect to the callbackUrl scheme.
        /// </summary>
        /// <param name="authorizeUri">Url to navigate to, beginning the authentication flow.</param>
        /// <param name="callbackUri">Expected callback url that the navigation flow will eventually redirect to.</param>
        /// <returns>Returns a result parsed out from the callback url.</returns>
        public static Task<WebAuthenticatorResult> AuthenticateAsync(Uri authorizeUri, Uri callbackUri) => Instance.Authenticate(authorizeUri, callbackUri);

        private static readonly WebAuthenticator Instance = new WebAuthenticator();

        private Dictionary<string, TaskCompletionSource<Uri>> tasks = new Dictionary<string, TaskCompletionSource<Uri>>();

        private WebAuthenticator()
        {
            Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().Activated += CurrentAppInstance_Activated;
        }

        [System.Runtime.CompilerServices.ModuleInitializer]

        internal static void Init()
        {
            try
            {
                OnAppCreation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("WinUIEx: Failed to initialize the WebAuthenticator: " + ex.Message, "WinUIEx");
            }
        }

        private static bool IsUriProtocolDeclared(string scheme)
        {
            if (global::Windows.ApplicationModel.Package.Current is null)
                return false;
            var docPath = Path.Combine(global::Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "AppxManifest.xml");
            var doc = XDocument.Load(docPath, LoadOptions.None);
            var reader = doc.CreateReader();
            var namespaceManager = new XmlNamespaceManager(reader.NameTable);
            namespaceManager.AddNamespace("x", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");
            namespaceManager.AddNamespace("uap", "http://schemas.microsoft.com/appx/manifest/uap/windows10");

            // Check if the protocol was declared
            var decl = doc.Root?.XPathSelectElements($"//uap:Extension[@Category='windows.protocol']/uap:Protocol[@Name='{scheme}']", namespaceManager);

            return decl != null && decl.Any();
        }

        private static System.Collections.Specialized.NameValueCollection? GetState(Microsoft.Windows.AppLifecycle.AppActivationArguments activatedEventArgs)
        {
            if (activatedEventArgs.Kind == Microsoft.Windows.AppLifecycle.ExtendedActivationKind.Protocol &&
                activatedEventArgs.Data is IProtocolActivatedEventArgs protocolArgs)
            {
                return GetState(protocolArgs);
            }
            return null;
        }

        private static NameValueCollection? GetState(IProtocolActivatedEventArgs protocolArgs)
        {
            var vals = System.Web.HttpUtility.ParseQueryString(protocolArgs.Uri.Query);
            if (vals["state"] is string state)
            {
                var vals2 = System.Web.HttpUtility.ParseQueryString(state);
                // Some services doesn't like & encoded state parameters, and breaks them out separately.
                // In that case copy over the important values
                if (vals.AllKeys.Contains("appInstanceId") && !vals2.AllKeys.Contains("appInstanceId"))
                    vals2.Add("appInstanceId", vals["appInstanceId"]);
                if (vals.AllKeys.Contains("signinId") && !vals2.AllKeys.Contains("signinId"))
                    vals2.Add("signinId", vals["signinId"]);
                return vals2;
            }
            return null;
        }

        private static void OnAppCreation()
        {
            var activatedEventArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent()?.GetActivatedEventArgs();
            if (activatedEventArgs is null)
                return;
            var state = GetState(activatedEventArgs);
            if (state is not null && state["appInstanceId"] is string id && state["signinId"] is string signinId && !string.IsNullOrEmpty(signinId))
            {
                var instance = Microsoft.Windows.AppLifecycle.AppInstance.GetInstances().Where(i => i.Key == id).FirstOrDefault();

                if (instance is not null && !instance.IsCurrent)
                {
                    // Redirect to correct instance and close this one
                    instance.RedirectActivationToAsync(activatedEventArgs).AsTask().Wait();
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                }
            }
            else
            {
                var thisInstance = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent();
                if (string.IsNullOrEmpty(thisInstance.Key))
                {
                    Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey(Guid.NewGuid().ToString());
                }
            }
        }

        private void CurrentAppInstance_Activated(object? sender, Microsoft.Windows.AppLifecycle.AppActivationArguments e)
        {
            if (e.Kind == Microsoft.Windows.AppLifecycle.ExtendedActivationKind.Protocol)
            {
                if (e.Data is IProtocolActivatedEventArgs protocolArgs)
                {
                    var vals = GetState(protocolArgs);
                    if (vals is not null && vals["signinId"] is string signinId)
                    {
                        ResumeSignin(protocolArgs.Uri, signinId);
                    }
                }
            }
        }

        private void ResumeSignin(Uri callbackUri, string signinId)
        {
            if (signinId != null && tasks.ContainsKey(signinId))
            {
                var task = tasks[signinId];
                tasks.Remove(signinId);
                task.TrySetResult(callbackUri);
            }
        }

        private async Task<WebAuthenticatorResult> Authenticate(Uri authorizeUri, Uri callbackUri)
        {
            if (global::Windows.ApplicationModel.Package.Current is null)
            {
                throw new InvalidOperationException("The WebAuthenticator requires a packaged app with an AppxManifest");
            }
            if (!IsUriProtocolDeclared(callbackUri.Scheme))
            {
                throw new InvalidOperationException($"The URI Scheme {callbackUri.Scheme} is not declared in AppxManifest.xml");
            }
            var g = Guid.NewGuid();
            UriBuilder b = new UriBuilder(authorizeUri);

            var query = System.Web.HttpUtility.ParseQueryString(authorizeUri.Query);
            var state = $"appInstanceId={Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().Key}&signinId={g}";
            if(query["state"] is string oldstate && !string.IsNullOrEmpty(oldstate))
            {
                // Encode the state parameter
                state += "&state=" + System.Web.HttpUtility.UrlEncode(oldstate);
            }
            query["state"] = state;
            b.Query = query.ToString();
            authorizeUri = b.Uri;

            var tcs = new TaskCompletionSource<Uri>();
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "rundll32.exe";
            process.StartInfo.Arguments = "url.dll,FileProtocolHandler " + authorizeUri.ToString();
            process.StartInfo.UseShellExecute = true;
            process.Start();
            tasks.Add(g.ToString(), tcs);
            var uri = await tcs.Task.ConfigureAwait(false);
            return new WebAuthenticatorResult(uri);
        }
    }
}