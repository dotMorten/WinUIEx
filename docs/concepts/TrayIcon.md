## Windows Tray Icon

You can add your application to the Windows Tray by simply setting [`IsVisibleInTray`](https://dotmorten.github.io/WinUIEx/api/WinUIEx.WindowManager.IsVisibleInTray.html) on the [`WindowManager`](https://dotmorten.github.io/WinUIEx/api/WinUIEx.WindowManager.html).

```cs
var wm = WindowManager.Get(MyWindow);
wm.IsVisibleInTray = true;                        
```

By default, if the user double-clicks this icon, the window is brought to the front, and if minimized, restored.
The icon used is obtained from the Window's task bar icon. You can set the icon using the `Window.AppWindow.SetTaskbarIcon` method.

### Context menu and custom click actions
You can add additional actions to user-inactions by subscribing to the [`TrayIconInvoked`](https://dotmorten.github.io/WinUIEx/api/WinUIEx.WindowManager.TrayIconInvoked.html) event.
This event gives you the ability override the default double-click action by marking the event handled,
or adding any kind of flyout by setting the [`Flyout`](https://dotmorten.github.io/WinUIEx/api/WinUIEx.TrayIconInvokedEventArgs.Flyout.html#WinUIEx_TrayIconInvokedEventArgs_Flyout) property on the event argument.

For example:
```cs
wm.TrayIconInvoked += (w, e) =>
{
    if (e.Type == TrayIconInvokeType.RightMouseUp)
    {
        var flyout = new MenuFlyout();
        flyout.Items.Add(new MenuFlyoutItem() { Text = "Open" });
        flyout.Items.Add(new MenuFlyoutItem() { Text = "Quit App" });
        ((MenuFlyoutItem)flyout.Items[0]).Click += (s, e) => MyWindow.Activate();
        ((MenuFlyoutItem)flyout.Items[1]).Click += (s, e) => MyWindow.Close();
        e.Flyout = flyout;
    }
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