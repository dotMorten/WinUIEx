## Extended Window type

Make your window class inherit from `WinUIEx.WindowEx` instead of `Window` and you get new features like:
 - Set Title in XAML
 - Set custom TitleBar content in XAML
 - Width and Height get/set properties
 - Minimum Width and Height get/set properties
 - Set TaskBarIcon
 - Several Convenience properties for controlling if the window can be maximized, minimized, resized, should show in task bar and switchers, change presenter,  make Window always on top, etc.
 - Easy shortcuts for showing Message Dialogs

## Usage

To use the WindowEx class, change the baseclass of your window (usually MainWindow).
In `MainWindow.xaml.cs` change 
```cs
  public sealed partial class MainWindow : Window
```
to
```cs
  public sealed partial class MainWindow : WinUIEx.WindowEx
```
And in `MainWindow.xaml` change
```xml
<Window   ...>
```
to
```xml
<winex:WindowEx xmlns:winex="using:WinUIEx"   ...>
```

## Object Model

![image](https://user-images.githubusercontent.com/1378165/145076790-1c09c2cb-e2b8-4485-ac89-2b27b0ae1aae.png)

