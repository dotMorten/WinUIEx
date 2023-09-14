## Custom Backdrops

The [TransparentBackdrop](https://dotmorten.github.io/WinUIEx/api/WinUIEx.TransparentBackdrop.html) and [ColorBackdrop](https://dotmorten.github.io/WinUIEx/api/WinUIEx.ColorBackdrop.html) allows you to set the background to be fully transparent, or a solid color with alpha.
This can be useful for changing the visual shape of the window, and create transparent areas of the window.
These are similar to the Mica and Acrylic backdrops, but without the blur effects.

Transparent backdrop:
```xml
<Window ...
   xmlns:winuiex="using:WinUIEx">
    <Window.SystemBackdrop>
        <winex:TransparentBackdrop />
    </Window.SystemBackdrop>
</Window>
```
Semi-transparent blue backdrop:
```xml
<Window ...
   xmlns:winuiex="using:WinUIEx">
    <Window.SystemBackdrop>
        <winex:ColorBackdrop Color="#554444ff" />
    </Window.SystemBackdrop>
</Window>
```
