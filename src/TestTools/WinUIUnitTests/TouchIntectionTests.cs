using Microsoft.UI.Xaml.Media;
using WinUIEx.TestTools.Input;

namespace WinUIUnitTests;

[TestClass]
public partial class TouchInjectionTests
{
    [WinUITestMethod]
    public async Task ButtonClickedWithTap()
    {
        Button b = new Button() { Background = new SolidColorBrush(Microsoft.UI.Colors.Red), Content = "Click me", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        WindowContext.Content = b;
        await b.LoadAsync();
        
        int clickCount = 0;
        b.Click += (s, e) => { clickCount++; };        
        var ti = new TouchInjection(WindowContext.GetWindowHandle());
        ti.Tap(b);
        await Task.Delay(100);
        Assert.AreEqual(1, clickCount);
    }

    [WinUITestMethod]
    public async Task Tap()
    {
        Grid b = new Grid() { Background = new SolidColorBrush(Microsoft.UI.Colors.Red) };
        WindowContext.Content = b;
        await b.LoadAsync();
        int clickCount = 0;
        b.Tapped += (s, e) => { clickCount++; };
        var ti = new TouchInjection(WindowContext.GetWindowHandle());
        ti.Tap(b);
        await Task.Delay(100);
        Assert.AreEqual(1, clickCount);
    }

    [WinUITestMethod]
    public async Task DoubleTap()
    {
        Grid b = new Grid() { Background = new SolidColorBrush(Microsoft.UI.Colors.Red) };
        WindowContext.Content = b;
        await b.LoadAsync();
        int clickCount = 0;
        b.DoubleTapped += (s, e) => { clickCount++; };
        var ti = new TouchInjection(WindowContext.GetWindowHandle());
        ti.DoubleTap(b);
        await Task.Delay(100);
        Assert.AreEqual(1, clickCount);
    }
}
