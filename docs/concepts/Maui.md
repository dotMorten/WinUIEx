## Using WinUIEx with .NET MAUI

### Include WinUIEx package in Windows target

Add WinUIEx to the Windows build target by adding a package reference in the `.csproj` file:
```xml
  <ItemGroup>
    <PackageReference Include="WinUIEx" Version="1.5.0" Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'" />
  </ItemGroup>
```

### Perform operations when Windows are created

Use the `ConfigureLifecycleEvents` to be notified of new windows getting created, which you can then extend with WinUIEx.

```cs
using Microsoft.Maui.LifecycleEvents;

#if WINDOWS
using WinUIEx;
#endif

namespace MyApp.Maui
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
                });

#if WINDOWS
            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddWindows(wndLifeCycleBuilder =>
                {
                    wndLifeCycleBuilder.OnWindowCreated(window =>
                    {
                        window.CenterOnScreen(1024,768); //Set size and center on screen using WinUIEx extension method

                        var manager = WinUIEx.WindowManager.Get(window);
                        manager.PersistenceId = "MainWindowPersistanceId"; // Remember window position and size across runs
                        manager.MinWidth = 640;
                        manager.MinHeight = 480;
                    });
                });
            });
#endif

            return builder.Build();
        }
    }
}
```

### Interact with the Page's containing window:

```cs
namespace MyApp.Maui;
#if WINDOWS
using WinUIEx;
#endif

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private void OnMaximizeClicked(object sender, EventArgs e)
    {
#if WINDOWS
        var window = this.Window.Handler.PlatformView as Microsoft.UI.Xaml.Window;
        window.Maximize(); // Use WinUIEx Extension method to maximize window
#endif
    }
    

    private void OnFullScreenClicked(object sender, EventArgs e)
    {
#if WINDOWS
        // Get the window manager
        var manager = WinUIEx.WindowManager.Get(this.Window.Handler.PlatformView as Microsoft.UI.Xaml.Window);
        if (manager.PresenterKind == Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped)
            manager.PresenterKind = Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen;
        else
            manager.PresenterKind = Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped;
#endif
    }
}
```
