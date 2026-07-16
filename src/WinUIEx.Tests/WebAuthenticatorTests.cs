using Microsoft.Windows.AppLifecycle;
using System;
using System.Threading;

namespace WinUIUnitTests
{
    [TestClass]
    [TestCategory(nameof(WinUIEx.WebAuthenticator))]
    public class WebAuthenticatorTests
    {
        [TestMethod]
        [Timeout(15000)]
        public async Task ProtocolActivationRedirectsToAuthenticatingInstance()
        {
            Assert.IsFalse(string.IsNullOrEmpty(AppInstance.GetCurrent().Key));

            var callbackUri = new Uri($"{App.OAuthCallbackScheme}://complete");
            var authorizeUri = new UriBuilder($"{App.OAuthAuthorizeScheme}://authorize");
            var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
            query["callback"] = callbackUri.OriginalString;
            var readyEventName = $@"Local\WinUIEx.WebAuthenticatorTests.{Guid.NewGuid():N}";
            query["readyEvent"] = readyEventName;
            authorizeUri.Query = query.ToString();

            using var readyEvent = new EventWaitHandle(false, EventResetMode.ManualReset, readyEventName);
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var authenticationTask = WebAuthenticator.AuthenticateAsync(authorizeUri.Uri, callbackUri, cancellationTokenSource.Token);
            readyEvent.Set();
            var result = await authenticationTask;

            Assert.AreEqual("integration-test-code", result.Properties["code"]);
        }

        [TestMethod]
        [Timeout(15000)]
        public void ConcurrentOAuthCallbacksReturnToCorrectInstances()
        {
            var firstEventName = $@"Local\WinUIEx.WebAuthenticatorTests.{Guid.NewGuid():N}";
            var secondEventName = $@"Local\WinUIEx.WebAuthenticatorTests.{Guid.NewGuid():N}";
            using var firstCompletion = new EventWaitHandle(false, EventResetMode.ManualReset, firstEventName);
            using var secondCompletion = new EventWaitHandle(false, EventResetMode.ManualReset, secondEventName);

            LaunchOAuthWorker(firstEventName, "first-instance-code");
            LaunchOAuthWorker(secondEventName, "second-instance-code");

            Assert.IsTrue(WaitHandle.WaitAll(new WaitHandle[] { firstCompletion, secondCompletion }, TimeSpan.FromSeconds(10)));
        }

        private static void LaunchOAuthWorker(string completionEventName, string code)
        {
            var workerUri = new UriBuilder($"{App.OAuthWorkerScheme}://start");
            var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
            query["completionEvent"] = completionEventName;
            query["code"] = code;
            workerUri.Query = query.ToString();

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "rundll32.exe",
                Arguments = $"url.dll,FileProtocolHandler \"{workerUri.Uri.OriginalString}\"",
                UseShellExecute = true,
            });
        }
    }
}
