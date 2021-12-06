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
    public partial class WindowExTests
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
        public void TestCleanup()
        {
            WindowEx.Width = sizeWidth;
            WindowEx.Height = sizeHeight;
            WindowEx.PresenterKind = Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped;
            WindowEx.Restore();
        }
        public WindowEx WindowEx => (WindowEx)WindowContext;

        [WinUITestMethod]
        public async Task SetWidth()
        {
            var grid = new Grid();
            WindowContext.Content = grid;
            await grid.LoadAsync();
            var width = WindowContext.Bounds.Width;
            var windowPadding = WindowEx.Width - width;
            WindowEx.Width = 500;            
            await grid.LayoutUpdatedAsync();
            Assert.AreEqual(500, grid.ActualWidth + windowPadding);
        }

        [WinUITestMethod]
        public async Task SetHeight()
        {
            var grid = new Grid();
            WindowContext.Content = grid;
            await grid.LoadAsync();
            var height = WindowContext.Bounds.Height;
            var windowPadding = WindowEx.Height - height;
            WindowEx.Height = 500;
            await grid.LayoutUpdatedAsync();
            Assert.AreEqual(500, grid.ActualHeight + windowPadding);
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
            await grid.LayoutUpdatedAsync();
            Assert.IsTrue(grid.ActualHeight > size);
            WindowEx.Restore();
            Assert.AreEqual(Microsoft.UI.Windowing.OverlappedPresenterState.Restored, presenter.State);
            await grid.LayoutUpdatedAsync();
            Assert.AreEqual(size, grid.ActualHeight);
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
            await grid.LayoutUpdatedAsync();
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
            await grid.LayoutUpdatedAsync();
            Assert.IsTrue(grid.ActualWidth > width);
            Assert.IsTrue(grid.ActualHeight > height);
        }
    }
}
