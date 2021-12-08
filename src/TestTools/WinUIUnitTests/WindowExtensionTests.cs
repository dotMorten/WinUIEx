using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using WinUIEx;
using WinUIEx.TestTools;
using WinUIEx.TestTools.MSTest;

namespace WinUIUnitTests
{
    [TestClass]
    public partial class WindowExtensionTests
    {
        private double sizeWidth;
        private double sizeHeight;

        [TestInitialize]
        public void TestInitialize()
        {
            sizeWidth = ((WindowEx)TestHost.Window).Width;
            sizeHeight = ((WindowEx)TestHost.Window).Height;
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            WindowEx.PresenterKind = Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped;
            WindowEx.Restore();
            WindowEx.SetWindowSize(sizeWidth, sizeHeight);
            await UIExtensions.WaitFrame();
        }
        public WindowEx WindowEx => (WindowEx)WindowContext;

        [WinUITestMethod]
        public async Task SetWidth()
        {
            var grid = new Grid();
            WindowContext.Content = grid;
            await grid.SizeChangedAsync();
            var width = grid.ActualWidth;
            var windowPadding = WindowEx.Width - width;
            WindowEx.Width = 500;            
            await grid.SizeChangedAsync();
            Assert.AreEqual(500, grid.ActualWidth + windowPadding, "Width after window resize");
        }

        [WinUITestMethod]
        public async Task SetHeight()
        {
            var grid = new Grid();
            WindowContext.Content = grid;
            await grid.LoadAsync();
            var height = grid.ActualHeight;
            var windowPadding = WindowEx.Height - height;
            WindowEx.Height = 500;
            await grid.SizeChangedAsync();
            Assert.AreEqual(500, grid.ActualHeight + windowPadding, "Height after window resize");
        }

        [WinUITestMethod]
        public void Minimize()
        {
            Assert.IsTrue(WindowEx.Visible);
            WindowEx.Minimize();
            var presenter = WindowEx.AppWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
            Assert.AreEqual(Microsoft.UI.Windowing.OverlappedPresenterState.Minimized, presenter.State);
            Assert.IsFalse(WindowEx.Visible);
            WindowEx.Restore();
            Assert.AreEqual(Microsoft.UI.Windowing.OverlappedPresenterState.Restored, presenter.State);
            Assert.IsTrue(WindowEx.Visible);
        }

        [WinUITestMethod]
        public async Task Maximize()
        {
            var grid = new Grid();
            WindowContext.Content = grid;
            await grid.LoadAsync();
            var size = grid.ActualHeight;
            var presenter = WindowEx.AppWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
            WindowEx.Maximize();
            Assert.AreEqual(Microsoft.UI.Windowing.OverlappedPresenterState.Maximized, presenter.State);
            await grid.SizeChangedAsync();
            Assert.IsTrue(grid.ActualHeight > size, "grid.ActualHeight > size");
            size = grid.ActualHeight;
            WindowEx.Restore();
            Assert.AreEqual(Microsoft.UI.Windowing.OverlappedPresenterState.Restored, presenter.State);
            await grid.SizeChangedAsync();
            Assert.IsTrue(grid.ActualHeight < size, "grid.ActualHeight < size");
        }

        [WinUITestMethod]
        public async Task CompactPresenter()
        {
            var grid = new Grid();
            WindowContext.Content = grid;
            await grid.LoadAsync();
            var width = WindowContext.Bounds.Width;
            var height = WindowContext.Bounds.Height;
            WindowEx.PresenterKind = Microsoft.UI.Windowing.AppWindowPresenterKind.CompactOverlay;
            await grid.SizeChangedAsync();
            Assert.IsTrue(grid.ActualWidth < width);
            Assert.IsTrue(grid.ActualHeight < height);
        }

        [WinUITestMethod]
        public async Task FullscreenPresenter()
        {
            var grid = new Grid();
            WindowContext.Content = grid;
            await grid.LoadAsync();
            var width = WindowContext.Bounds.Width;
            var height = WindowContext.Bounds.Height;
            WindowEx.PresenterKind = Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen;
            await grid.SizeChangedAsync();
            Assert.IsTrue(grid.ActualWidth > width, "width");
            Assert.IsTrue(grid.ActualHeight > height, "height");
        }
    }
}
