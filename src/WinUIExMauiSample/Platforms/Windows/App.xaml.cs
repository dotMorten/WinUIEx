﻿using Microsoft.UI.Xaml;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUIExMauiSample.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
#if !DISABLE_XAML_GENERATED_MAIN
        if (WinUIEx.WebAuthenticator.CheckOAuthRedirectionActivation())
            return;
#endif
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        fss?.Hide(TimeSpan.FromSeconds(1));
        fss = null;
    }

    internal SimpleSplashScreen? fss { get; set; }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}


#if DISABLE_XAML_GENERATED_MAIN
/// <summary>
/// Program class
/// </summary>
public static class Program
{
    [global::System.STAThreadAttribute]
    static void Main(string[] args)
    {
        if (WinUIEx.WebAuthenticator.CheckOAuthRedirectionActivation(true))
            return;
        // Launch splash screen
        var fss = WinUIEx.SimpleSplashScreen.ShowDefaultSplashScreen();
        global::WinRT.ComWrappersSupport.InitializeComWrappers();
        global::Microsoft.UI.Xaml.Application.Start((p) => {
            var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            new App() { fss = fss };
        });
    }
}
#endif
