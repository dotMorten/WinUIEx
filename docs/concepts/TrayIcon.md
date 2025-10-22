## Windows Tray Icon

You can add your application to the Windows Tray by simply setting [`IsVisibleInTray`](https://dotmorten.github.io/WinUIEx/api/WinUIEx.WindowManager.IsVisibleInTray.html) on the [`WindowManager`](https://dotmorten.github.io/WinUIEx/api/WinUIEx.WindowManager.html).

```cs
var wm = WindowManager.Get(MyWindow);
wm.IsVisibleInTray = true;                        
```

If the user clicks/selects this icon, the window is brought to the front, and if minimized, restored.
The icon used is obtained from the Window's task bar icon. You can set the icon using the `Window.AppWindow.SetTaskbarIcon` method.

### Context menu and custom click actions
You can add an additional context-menu action to tray icon by subscribing to the [`TrayIconContextMenu`](https://dotmorten.github.io/WinUIEx/api/WinUIEx.WindowManager.TrayIconContextMenu.html) event.
This event gives you the ability to display any kind of flyout by setting the 
[`Flyout`](https://dotmorten.github.io/WinUIEx/api/WinUIEx.TrayIconEventArgs.Flyout.html#TrayIconEventArgs) property on the event argument.

For example:
```cs
wm.TrayIconContextMenu += (w, e) =>
{
    var flyout = new MenuFlyout();
    flyout.Items.Add(new MenuFlyoutItem() { Text = "Open" });
    flyout.Items.Add(new MenuFlyoutItem() { Text = "Quit App" });
    ((MenuFlyoutItem)flyout.Items[0]).Click += (s, e) => MyWindow.Activate();
    ((MenuFlyoutItem)flyout.Items[1]).Click += (s, e) => MyWindow.Close();
    e.Flyout = flyout;
};
```

### Implementing Minimize-To-Tray or Launch-To-Tray

You can create a minimize-to-tray feature by reacting to the minimize event and hide the window from the task switchers.
Similarly, you can launch the app minimized to have the app just start up in the tray.

```cs
protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
{
    _window = new MainWindow();
    var wm = WindowManager.Get(_window);
    wm.IsVisibleInTray = true; // Show app in tray
    // Minimize to tray:
    wm.WindowStateChanged += (s, state) =>
        wm.AppWindow.IsShownInSwitchers = state != WindowState.Minimized;

    if (MyAppSettings.LaunchToTray) // Delay activating the window by starting minimized
        wm.WindowState = WindowState.Minimized;
    else
        _window.Activate();
}
```

## Using the TrayIcon class

For more fine-grained control over the tray icon, you can use the [`TrayIcon`](https://dotmorten.github.io/WinUIEx/api/WinUIEx.TrayIcon.html) class directly, which provides no default behaviors and more events to work with.
This class allows you to create a tray icon without associating it with a window, and gives you more control over its behavior,
updating its icon and tooltip, or even have multiple icons for a single process.

Note: Make sure once you close your application, that all TrayIcon instances are disposed, otherwise the icon will remain in the tray and the process will not exit.
This behavior however also allows you to create a window-less application, and (if needed) creating a Window on demand based on TrayIcon interactions. For example:

```cs
public partial class App : Application
{
    private TrayIcon icon;
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    private Window GetMainWindow()
    {
        if (_window is not null)
            return _window;
        _window = new MainWindow();
        _window.AppWindow.Closing += (s, e) =>
        {
            // Prevent closing so it can be re-activated later. We'll just hide it for now
            // As an alternative don't cache the Window, but close and recreate a new Window every time.
            e.Cancel = true; 
            s.Hide(); 
        };
        var wm = WindowManager.Get(_window);
        wm.WindowStateChanged += (s, state) => wm.AppWindow.IsShownInSwitchers = state != WindowState.Minimized;
        return _window;
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        // In OnLaunched we don't create a window but just the tray icon. We'll create a window later if we need to.
        // Note: This icon will keep the application process alive as well.
        icon = new TrayIcon(1, "Images/StatusOK.ico", "Test");
        icon.IsVisible = true;
        icon.Selected += (s, e) => GetMainWindow().Activate();
        icon.ContextMenu += (w, e) =>
        {
            var flyout = new MenuFlyout();
            flyout.Items.Add(new MenuFlyoutItem() { Text = "Open" });
            ((MenuFlyoutItem)flyout.Items[0]).Click += (s, e) => GetMainWindow().Activate();
            flyout.Items.Add(new MenuFlyoutItem() { Text = "Quit App" });
            ((MenuFlyoutItem)flyout.Items[1]).Click += (s, e) =>
            {
                // Make sure we close both the main window, and the icon for the process to exit
                _window?.Close();
                icon.Dispose();
            };
            e.Flyout = flyout;
        };
    }
}
```
