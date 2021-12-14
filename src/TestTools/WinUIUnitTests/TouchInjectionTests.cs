using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using WinUIEx.TestTools.Input;

namespace WinUIUnitTests;

[TestClass]
public partial class TouchInjectionTests
{
    public TestContext TestContext { get; set; }

    private const ManipulationModes NoInertia = ManipulationModes.Rotate | ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.Scale;

    private static TouchInjection ti;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        ti = new TouchInjection(TestHost.Window);
    }

    [TestInitialize]
    public void TestInitialize()
    {
        var op = (Microsoft.UI.Windowing.OverlappedPresenter)TestHost.Window.GetAppWindow().Presenter;
        op.IsAlwaysOnTop = true;
    }
    [TestCleanup]
    public void TestCleanup()
    {
        var op = (Microsoft.UI.Windowing.OverlappedPresenter)TestHost.Window.GetAppWindow().Presenter;
        op.IsAlwaysOnTop = false;
    }

    [WinUITestMethod]
    public async Task ButtonClickedWithTap()
    {
        Button button = new Button() { Background = new SolidColorBrush(Microsoft.UI.Colors.Red), Content = "Click me", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        WindowContext.Content = button;
        await button.LoadAsync();
        
        int clickCount = 0;
        button.Click += (s, e) => { clickCount++; };        
        await ti.TapAsync(TimeSpan.FromMilliseconds(100), button);
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
        ti.DoubleTap(grid);
        await Task.Delay(100);
        Assert.AreEqual(1, clickCount);
    }

    [WinUITestMethod]
    public async Task Drag()
    {
        Grid grid = new Grid() { Background = new SolidColorBrush(Microsoft.UI.Colors.Red), ManipulationMode = NoInertia };
        WindowContext.Content = grid;
        await grid.LoadAsync();
        var points = new List<ManipulationDeltaRoutedEventArgs>();
        var completed = ManipulationCompletionTracker(grid, TestContext, points);
        await ti.DragAsync(new Windows.Foundation.Point(10,10), new Windows.Foundation.Point(60,110), TimeSpan.FromMilliseconds(1000), grid);
        var endResult = await completed;
        Assert.AreEqual(60, endResult.Position.X, .5);
        Assert.AreEqual(110, endResult.Position.Y, .5);
        Assert.AreEqual(50, endResult.Cumulative.Translation.X, 3);
        Assert.AreEqual(100, endResult.Cumulative.Translation.Y);
    }

    [WinUITestMethod]
    public async Task RotateAsync()
    {
        Grid grid = new Grid() { Background = new SolidColorBrush(Microsoft.UI.Colors.Red), ManipulationMode = NoInertia };
        WindowContext.Content = grid;
        await grid.LoadAsync();
        var points = new List<ManipulationDeltaRoutedEventArgs>();
        var completed = ManipulationCompletionTracker(grid, TestContext, points);
        await ti.RotateAsync(new Windows.Foundation.Point(grid.ActualWidth / 2, grid.ActualHeight / 2), 100, 90, TimeSpan.FromSeconds(1), grid);
        var endResult = await completed;
        //var last = points.Last();
        Assert.IsTrue(points.Count > 1);
        Assert.AreEqual(90, endResult.Cumulative.Rotation, .1);
        Assert.AreEqual(0, endResult.Cumulative.Translation.X, .1);
        Assert.AreEqual(0, endResult.Cumulative.Translation.Y, .1);
        Assert.AreEqual(1, endResult.Cumulative.Scale, .001);
    }

    [WinUITestMethod]
    public async Task PinchAsync()
    {
        Grid grid = new Grid() { Background = new SolidColorBrush(Microsoft.UI.Colors.Red), ManipulationMode = NoInertia };
        WindowContext.Content = grid;
        await grid.LoadAsync();
        var points = new List<ManipulationDeltaRoutedEventArgs>();
        var completed = ManipulationCompletionTracker(grid, TestContext, points);
        await ti.PinchAsync(new Windows.Foundation.Point(grid.ActualWidth / 2, grid.ActualHeight / 2), 100, 50, TimeSpan.FromSeconds(.5), grid);
        var endResult = await completed;
        Assert.AreEqual(0.5, endResult.Cumulative.Scale, .001);
    }

    private static async Task<ManipulationCompletedRoutedEventArgs> ManipulationCompletionTracker(UIElement element, TestContext testContext = null, List<ManipulationDeltaRoutedEventArgs> points = null)
    {
        var completed = new TaskCompletionSource<ManipulationCompletedRoutedEventArgs>();
        ManipulationStartedEventHandler startHandler = (s, e) => testContext?.WriteLine($"ManipulationStarted @ {e.Position.X},{e.Position.Y}");
        ManipulationDeltaEventHandler deltaHandler = (s, e) =>
        {
            points?.Add(e);
            testContext?.WriteLine($"ManipulationDelta @ {e.Position.X},{e.Position.Y}. Translation: {e.Cumulative.Translation.X}, {e.Cumulative.Translation.Y}. Scale: {e.Cumulative.Scale}. Rotation: {e.Cumulative.Rotation}");
        };
        ManipulationCompletedEventHandler completedHandler = (s, e) => {
            testContext?.WriteLine($"ManipulationCompleted @ {e.Position.X},{e.Position.Y}. Translation: {e.Cumulative.Translation.X}, {e.Cumulative.Translation.Y}. Scale: {e.Cumulative.Scale}. Rotation: {e.Cumulative.Rotation}");
            completed.TrySetResult(e);
        };
        element.ManipulationStarted += startHandler;
        element.ManipulationDelta += deltaHandler;
        element.ManipulationCompleted += completedHandler;
        _ = Task.Delay(2000).ContinueWith(t => completed.TrySetException(new TimeoutException("ManipulationCompleted didn't fire")));
        try
        {
            var result = await completed.Task;
            return result;
        }
        finally
        {
            element.ManipulationStarted -= startHandler;
            element.ManipulationDelta -= deltaHandler;
            element.ManipulationCompleted -= completedHandler;
        }
    }
}
