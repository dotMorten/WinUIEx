using Microsoft.UI.Xaml.Media;
using WinUIEx.TestTools.Input;

namespace WinUIUnitTests;

[TestClass]
public partial class TouchInjectionTests
{
    [WinUITestMethod]
    public async Task ButtonClickedWithTap()
    {
        Button button = new Button() { Background = new SolidColorBrush(Microsoft.UI.Colors.Red), Content = "Click me", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        WindowContext.Content = button;
        await button.LoadAsync();
        
        int clickCount = 0;
        button.Click += (s, e) => { clickCount++; };        
        var ti = new TouchInjection(WindowContext.GetWindowHandle());
        ti.Tap(button);
        await Task.Delay(100);
        Assert.AreEqual(1, clickCount);
    }

    [WinUITestMethod]
    public async Task Tap()
    {
        Grid grid = new Grid() { Background = new SolidColorBrush(Microsoft.UI.Colors.Red) };
        WindowContext.Content = grid;
        await grid.LoadAsync();
        int clickCount = 0;
        grid.Tapped += (s, e) => { clickCount++; };
        var ti = new TouchInjection(WindowContext.GetWindowHandle());
        ti.Tap(grid);
        await Task.Delay(100);
        Assert.AreEqual(1, clickCount);
    }

    [WinUITestMethod]
    public async Task DoubleTap()
    {
        Grid grid = new Grid() { Background = new SolidColorBrush(Microsoft.UI.Colors.Red) };
        WindowContext.Content = grid;
        await grid.LoadAsync();
        int clickCount = 0;
        grid.DoubleTapped += (s, e) => { clickCount++; };
        var ti = new TouchInjection(WindowContext.GetWindowHandle());
        ti.DoubleTap(grid);
        await Task.Delay(100);
        Assert.AreEqual(1, clickCount);
    }
}
