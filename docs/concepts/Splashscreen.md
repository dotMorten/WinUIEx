## Splash Screen

WinUIEx provides two kinds of splash screens:
 - `FastSplashScreen`: A simple splash screen that shows just an image and can be launched before any UI loads.
 - `SplashScreen`: A more advanced splash screen that can show any XAML including a progress bar and status text.


### FastSplashScreen

`FastSplashScreen` will show an image while the app is loading. To use it, create a new `FastSplashScreen` in App.xaml.cs:

```cs
private FastSplashScreen vss { get; set; }

public App()
{
  fss = FastSplashScreen.ShowDefaultSplashScreen(); // Shows the splash screen you already defined in your app manifest. For unpackaged apps use .ShowSplashScreenImage(imagepath):
  // fss = FastSplashScreen.ShowSplashScreenImage(full_path_to_image_); // Shows a custom splash screen image. Must be a full-path (no relative paths)
  this.InitializeComponent();
}
```

Once your window is activated, you can remove the splash screen by either calling `.Dispose()` or `Hide()`.

```cs
protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
{
  m_window = new MainWindow();
  m_window.Activate();
  Window_Activated += Window_Activated;
  
}

private void Window_Activated(object sender, WindowActivatedEventArgs args)
{
  ((Window)sender).Activated -= Window_Activated;
  fss?.Hide();
  fss = null;
}
```

For even faster splash screen, disable the XAML generated main method by defining the `DISABLE_XAML_GENERATED_MAIN` preprocessor directive
and instead defining your own start method. You'll then be able to display the splash screen as the very first thing before the application is created. For example:

```cs
#if DISABLE_XAML_GENERATED_MAIN
  public static class Program
  {
    [System.STAThreadAttribute]
    static void Main(string[] args)
    {
      // If you're using the WebAuthenticator, make sure you call this method first before the splashscreen shows
      if (WebAuthenticator.CheckOAuthRedirectionActivation(true))
        return;
      var fss = FastSplashScreen.ShowDefaultSplashScreen();
      WinRT.ComWrappersSupport.InitializeComWrappers();
      Microsoft.UI.Xaml.Application.Start((p) => {
        var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
        System.Threading.SynchronizationContext.SetSynchronizationContext(context);
        new App(fss); // Pass the splash screen to your app so it can close it on activation
      });
    }
  }
#endif
```

### SplashScreen

To create a new splash screen, first create a new WinUI Window.
Next change the baseclass from `Window` to `SplashScreen`:

Before:
```xml
<Window ...>
</Window>
```
After:
```xml
<winuiex:SplashScreen
    xmlns:winuiex="using:WinUIEx" ...>

</winuiex:SplashScreen>
```
And codebehind:

Before:
```cs
public sealed partial class SplashScreen : Window
{
  public SplashScreen()
  {
    this.InitializeComponent();
  }
}
```
After:
```cs
public sealed partial class SplashScreen : WinUIEx.SplashScreen
{
  public SplashScreen(Type window) : base(window)
  {
    this.InitializeComponent();
  }
}
```

Next in App.xaml.cs, change OnLaunched from:
```cs
protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
{
  m_window = new MainWindow();
  m_window.Activate();
}
```
To:
```cs
protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
{
  var splash = new SplashScreen(typeof(MainWindow));
  splash.Completed += (s, e) => m_window = e;
}
```

Lastly, override OnLoading, to create some long-running setup work - once this method completes, the splash screen will close and the window in the type parameter will launch.

Example:
```cs
protected override async Task OnLoading()
{
  //TODO: Do some actual work
  for (int i = 0; i < 100; i+=5)
  {
    statusText.Text = $"Loading {i}%...";
    progressBar.Value = i;
    await Task.Delay(50);
  }
}
```