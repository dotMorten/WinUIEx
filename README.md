# WinUIEx
 WinUI Extensions
 
![image](https://github.com/dotMorten/WinUIEx/raw/main/src/WinUIEx/logo.png)

A set of extension methods and classes to fill some gaps in WinUI 3, mostly around windowing.

To get the extension methods, first add `using WinUIEx;` to the top of your code.

- Minimize/Maximize/Restore and Hide window.
```cs
    myWindow.MinimizeWindow();
    myWindow.MaximizeWindow();
    myWindow.RestoreWindow();
    myWindow.HideWindow();`
```

- Move and resize window
```cs
   myWindow.CenterOnScreen();
   myWindow.SetWindowPositionAndSize(100, 100, 1024, 768);
```

- Make Window always-on-top
```cs
    myWindow.SetAlwaysOnTop(true);
```

- Bring window to the top
```cs
    myWindow.BringToFront();
```

- Remove Window chrome buttons
Make your window class inherit from WindowEx instead, and set properties `IsCloseButtonVisible`, `IsMaximizeButtonVisible`, `IsMinimizeButtonVisible`

- Create a Tray Icon
```cs
    var icon = Icon.FromFile("Images/WindowIcon.ico");
    tray = new TrayIcon(icon);
    tray.TrayIconLeftMouseDown += (s, e) => this.BringToFront();
```
And more to come...

![image](https://user-images.githubusercontent.com/1378165/108465563-1e2d8700-7237-11eb-8eb4-736644606a64.png)
