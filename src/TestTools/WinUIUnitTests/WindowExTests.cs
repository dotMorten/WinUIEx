namespace WinUIUnitTests;

[TestClass]
public partial class WindowExTests
{
    public WindowEx WindowEx => (WindowEx)WindowContext;

    [WorkItem(16)]
    [WinUITestMethod]
    public async Task DefaultContentAlignment()
    {
        var layoutRoot = new StackPanel() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        Button button = new Button() { Content = "Click Me" };
        layoutRoot.Children.Add(button);
        WindowEx.WindowContent = layoutRoot;
        await button.LayoutUpdatedAsync();
        var root = WindowContext.Content;
        var transform = button.TransformToVisual(root);
        var center = transform.TransformPoint(new Windows.Foundation.Point(button.ActualWidth / 2, button.ActualHeight / 2));
        var rootSize = root.ActualSize;
        rootSize = WindowContext.Content.ActualSize;
        Assert.IsTrue(rootSize.X > button.ActualSize.X, "X");
        Assert.IsTrue(rootSize.Y > button.ActualSize.Y, "Y");
        Assert.AreEqual(rootSize.X / 2, center.X, 2, "Center X");
        Assert.AreEqual(rootSize.Y / 2, center.Y, 2, "Center Y");
    }
}