using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using WinUIEx.TestTools;
using WinUIEx.TestTools.MSTest;

namespace WinUIUnitTests
{
    [TestClass]
    public partial class TestToolsTests
    {
        [WinUITestMethod]
        public async Task TestGridArrange()
        {
            var grid = new Grid() { Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red) };
            WindowContext.Content = grid;
            await grid.LoadAsync();
            Assert.AreEqual(WindowContext.Bounds.Width, grid.ActualWidth, "Full Width");
            Assert.AreEqual(WindowContext.Bounds.Height, grid.ActualHeight, "Full height");
            grid.Width = 300;
            grid.Height = 100;
            await grid.LayoutUpdatedAsync();
            Assert.AreEqual(300, grid.ActualWidth, "ActualWidth");
            Assert.AreEqual(100, grid.ActualHeight, "ActualHeight");

            var bitmap = await grid.AsBitmapAsync();
            Assert.AreEqual(300, bitmap.PixelWidth);
            Assert.AreEqual(100, bitmap.PixelHeight);

            var blobs = await grid.FindConnectedPixelsAsync(Microsoft.UI.Colors.Red);
            Assert.AreEqual(1, blobs.Count);
            Assert.AreEqual(300, blobs[0].Width);
            Assert.AreEqual(100, blobs[0].Height);

            grid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
            blobs = await grid.FindConnectedPixelsAsync(Microsoft.UI.Colors.Green);
            Assert.AreEqual(1, blobs.Count);
            Assert.AreEqual(300, blobs[0].Width);
            Assert.AreEqual(100, blobs[0].Height);
        }
    }
}
