## TrayIcon APIs

- Create a Tray Icon
```cs
    var icon = Icon.FromFile("Images/WindowIcon.ico");
    tray = new TrayIcon(icon);
    tray.TrayIconLeftMouseDown += (s, e) => this.BringToFront();
```

![image](https://user-images.githubusercontent.com/1378165/145077131-2a19643d-808f-4aa8-9f18-3966150b0500.png)
