## WinUIEX1003: Frame.Navigate target must inherit from Page.

`Frame.Navigate(Type)` only supports types that inherit from `Microsoft.UI.Xaml.Controls.Page`. Passing any other type will fail at runtime, so the analyzer reports this as a build error.

|Item|Value|
|-|-|
|Category|Usage|
|Enabled|True|
|Severity|Error|
|CodeFix|False|
---

### Example

This will trigger the analyzer:
```cs
frame.Navigate(typeof(MyViewModel));
```

Use a page type instead:
```cs
frame.Navigate(typeof(MyPage));
```

### References

 - [Frame.Navigate(Type) Method](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.frame.navigate)
