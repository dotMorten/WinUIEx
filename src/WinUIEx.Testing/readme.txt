* WinUI Unit Test Helpers *

Please read known issues at the end.


Getting started:
Add the following line to App.xaml.cs at the end of the "OnLaunched" method:
-------------------------------------
     WinUIEx.Testing.UnitTestClient.Run(m_window);
-------------------------------------

Next add the following package reference to your project file:
-------------------------------------
<ItemGroup>
    <PackageReference Include="MSTest.TestAdapter">
        <Version>2.2.8</Version>
    </PackageReference>
</ItemGroup>
-------------------------------------


You can now add a UI unit test class to your project. Example:
-------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using WinUIEx.Testing;

namespace MyTestApp
{
    [TestClass]
    public partial class MyTests
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

            var bitmap = await grid.AsBitmap();
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

-------------------------------------

KNOWN ISSUES
-------------------
Launching tests with the debugger does not work.
If you need to debug, add the following line of code to launch a debugger after the normal run has started:
   System.Diagnostics.Debugger.Launch();

When this code is hit, a dialog will pop up asking you to pick a VS instance for debugging. Pick the active instance where you have your tests.