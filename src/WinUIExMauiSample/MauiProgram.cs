using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;
using WinUIEx;

namespace WinUIExMauiSample
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
#if WINDOWS
                 .ConfigureLifecycleEvents(events =>
                 {
                     events.AddWindows(wndLifeCycleBuilder =>
                     {
                         wndLifeCycleBuilder.OnWindowCreated(window =>
                         {
                             var manager = WinUIEx.WindowManager.Get(window);
                             manager.PersistenceId = "MainWindowPersistanceId";
                             manager.MinWidth = 640;
                             manager.MinHeight = 480;
                             window.SystemBackdrop = new DesktopAcrylicBackdrop();
                         });
                     });
                 })
                 .Services.AddSingleton<IWebAuthenticator>(WinUIExWebAuthenticator.Default) // Register the WinUIEx-based WebAuthenticator to work around the limitation in .NET MAUI

#endif
                 ;

            return builder.Build();
        }

    }

#if WINDOWS
    public sealed class WinUIExWebAuthenticator : Microsoft.Maui.Authentication.IWebAuthenticator
    {
        private WinUIExWebAuthenticator() { }

        public static Microsoft.Maui.Authentication.IWebAuthenticator Default { get; } = new WinUIExWebAuthenticator();

        async Task<Microsoft.Maui.Authentication.WebAuthenticatorResult> Microsoft.Maui.Authentication.IWebAuthenticator.AuthenticateAsync(WebAuthenticatorOptions o)
        {
            ArgumentNullException.ThrowIfNull(o, nameof(o));
            ArgumentNullException.ThrowIfNull(o.Url, nameof(WebAuthenticatorOptions.Url));
            ArgumentNullException.ThrowIfNull(o.CallbackUrl, nameof(WebAuthenticatorOptions.CallbackUrl));

            var result = await WinUIEx.WebAuthenticator.AuthenticateAsync(o.Url, o.CallbackUrl);
            return new Microsoft.Maui.Authentication.WebAuthenticatorResult(result.Properties);
        }
    }
#endif
}