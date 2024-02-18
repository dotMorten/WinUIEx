using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUIEx
{
    /// <summary>
    /// Starts the authentication operation. You can call the methods of this class multiple times in a single application or across multiple
    /// applications at the same time. The <see cref="https://github.com/microsoft/Windows-universal-samples/tree/master/Samples/WebAuthenticationBroker">Web authentication broker sample</see>
    /// in the Samples gallery is an example of how to use the WebAuthenticationBroker class for single sign on (SSO) connections.
    /// </summary>
    public static class WebAuthenticationBroker
    {
        /*
        /// <summary>
        /// Starts the authentication operation with one input.
        /// </summary>
        /// <param name="requestUri">The starting URI of the web service. This URI must be a secure address of https://.</param>
        static void AuthenticateAndContinue(Uri requestUri);

        /// <summary>
        /// Starts the authentication operation with two inputs.
        /// </summary>
        /// <param name="requestUri">The starting URI of the web service. This URI must be a secure address of https://.</param>
        /// <param name="callbackUri">The callback URI that indicates the completion of the web authentication. The broker matches this URI against every URI that it is about to navigate to. The broker never navigates to this URI, instead the broker returns the control back to the application when the user clicks a link or a web server redirection is made.</param>
        static void AuthenticateAndContinue(Uri requestUri, Uri callbackUri);

        /// <summary>
        /// Starts the authentication operation with four inputs.
        /// </summary>
        /// <param name="requestUri">The starting URI of the web service. This URI must be a secure address of https://.</param>
        /// <param name="callbackUri">The callback URI that indicates the completion of the web authentication. The broker matches this URI against every URI that it is about to navigate to. The broker never navigates to this URI, instead the broker returns the control back to the application when the user clicks a link or a web server redirection is made.</param>
        /// <param name="continuationData">Continuation data to be passed as part of the authentication operation.</param>
        /// <param name="options">The options for the authentication operation.</param>
        static void AuthenticateAndContinue(Uri requestUri, Uri callbackUri, ValueSet continuationData, WebAuthenticationOptions options)*/

        /// <summary>
        /// Starts the asynchronous authentication operation with two inputs. You can call this method multiple times in a single application or across multiple applications at the same time.
        /// </summary>
        /// <remarks>
        /// There is no explicit callbackUri parameter in this method. The application's default URI is used internally as the terminator. For more information, see GetCurrentApplicationCallbackUri.
        /// </remarks>
        /// <param name="options">The options for the authentication operation.</param>
        /// <param name="requestUri">The starting URI of the web service. This URI must be a secure address of https://.</param>
        /// <returns>The way to query the status and get the results of the authentication operation. If you are getting an invalid parameter error, the most common cause is that you are not using HTTPS for the requestUri parameter.</returns>
        public static Task<WebAuthenticationResult> AuthenticateAsync(WebAuthenticationOptions options, System.Uri requestUri)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Starts the asynchronous authentication operation with three inputs. You can call this method multiple times in a single application or across multiple applications at the same time.
        /// </summary>
        /// <remarks>
        /// When this method is used, no session state or persisted cookies are retained across multiple calls from the same or different UWP app. This method must be called on the UI thread.
        /// </remarks>
        /// <param name="options">The options for the authentication operation.</param>
        /// <param name="requestUri">The starting URI of the web service. This URI must be a secure address of https://.</param>
        /// <param name="callbackUri">The callback URI that indicates the completion of the web authentication. The broker matches this URI against every URI that it is about to navigate to. The broker never navigates to this URI, instead the broker returns the control back to the application when the user clicks a link or a web server redirection is made.</param>
        /// <returns>The way to query the status and get the results of the authentication operation. If you are getting an invalid parameter error, the most common cause is that you are not using HTTPS for the requestUri parameter.</returns>
        public static Task<WebAuthenticationResult> AuthenticateAsync(WebAuthenticationOptions options, System.Uri requestUri, Uri callbackUri)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Indicates the result of the authentication operation.
    /// </summary>
    public sealed class WebAuthenticationResult
    {
        //https://learn.microsoft.com/en-us/uwp/api/windows.security.authentication.web.webauthenticationresult?view=winrt-22621
    }

    /// <summary>
    /// Contains the status of the authentication operation.
    /// </summary>
    public enum WebAuthenticationStatus
    {
        /// <summary>
        /// The operation succeeded, and the response data is available.
        /// </summary>
        Success = 0,

        /// <summary>
        /// The operation was canceled by the user.
        /// </summary>
        UserCancel = 1,

        /// <summary>
        /// The operation failed because a specific HTTP error was returned, for example 404.
        /// </summary>
        ErrorHttp = 2
    }

    /// <summary>
    /// Contains the options available to the asynchronous operation.
    /// This enumeration supports a bitwise combination of its member values.
    /// </summary>
    [Flags]
    public enum WebAuthenticationOptions
    {
        /// <summary>
        /// No options are requested.
        /// </summary>
        None = 0,

        /// <summary>
        /// Tells the web authentication broker to not render any UI. This option will throw an exception if used with AuthenticateAndContinue; AuthenticateSilentlyAsync, which includes this option implicitly, should be used instead.
        /// </summary>
        SilentMode = 1,

        /// <summary>
        /// Tells the web authentication broker to render the webpage in an app container that supports privateNetworkClientServer, enterpriseAuthentication, and sharedUserCertificate capabilities. Note the application that uses this flag must have these capabilities as well.
        /// </summary>
        UseCorporateNetwork = 8,

        /// <summary>
        /// Tells the web authentication broker to return the body of the HTTP POST in the ResponseData property. For use with single sign-on (SSO) only.
        /// </summary>
        UseHttpPost = 4,

        /// <summary>
        /// Tells the web authentication broker to return the window title string of the webpage in the ResponseData property.
        /// </summary>
        UseTitle = 2
    }
}
