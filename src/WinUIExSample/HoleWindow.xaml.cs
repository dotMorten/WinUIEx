using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using WinUIEx;

namespace WinUIExSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HoleWindow : Window
    {
        private readonly WindowManager manager;
        public HoleWindow()
        {
            this.InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            manager = WindowManager.Get(this);
            manager.Width = 800;
            manager.Height = 640;
            manager.IsResizable = false; //Disable resizing for simplicity - You need to update regions on resize otherwise
            manager.IsAlwaysOnTop = true;
            // ((OverlappedPresenter)AppWindow.Presenter).SetBorderAndTitleBar(false, false);
            this.Closed += HoleWindow_Closed;
            MainWindow.Closed += MainWindow_Closed;
        }

        private void RectangleArea_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // Get the bounds of the rectangle relative to the window
            var bounds = RectangleArea.TransformToVisual(this.Content).TransformBounds(new Rect(0, 1, RectangleArea.ActualWidth, RectangleArea.ActualHeight));
            // Subtract the rectangle from the window region
            var regionWithHole = GetWindowRegion() - Region.CreateRectangle(bounds);
            this.SetRegion(regionWithHole);
        }

        private void RoundedRectangleArea_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // Get the bounds of the rectangle relative to the window
            var bounds = RoundedRectangleArea.TransformToVisual(this.Content).TransformBounds(new Rect(1, 1, RoundedRectangleArea.ActualWidth, RoundedRectangleArea.ActualHeight));
            // Subtract the rectangle from the window region
            var regionWithHole = GetWindowRegion() - Region.CreateRoundedRectangle(bounds, 40, 40);
            this.SetRegion(regionWithHole);
        }


        private void EllipseArea_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // Get the bounds of the ellipse relative to the window   
            var bounds = EllipseArea.TransformToVisual(this.Content).TransformBounds(new Rect(0, 1, EllipseArea.ActualWidth, EllipseArea.ActualHeight + 1));
            // Subtract the ellipse from the window region
            var regionWithHole = GetWindowRegion() - Region.CreateElliptic(bounds.X, bounds.Y, bounds.Width+bounds.X, bounds.Height + bounds.Y);
            this.SetRegion(regionWithHole);
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e) => this.SetRegion(null);

        private Region GetWindowRegion()
        {
            return Region.CreateRoundedRectangle(new Rect(0, 1, manager.Width - 16, manager.Height - 8), 8, 8);
        }

        #region Cleanup: Ensure this window is closed when main window closes
        private void MainWindow_Closed(object sender, WindowEventArgs args) => this.Close();

        public WindowEx MainWindow => ((App)Application.Current).MainWindow!;

        private void HoleWindow_Closed(object sender, WindowEventArgs args) => MainWindow.Closed -= MainWindow_Closed;
        #endregion

        private void StarArea_Pressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var path = (Microsoft.UI.Xaml.Shapes.Path)sender;
            var transformer = path.TransformToVisual(this.Content);
            var geom = (Microsoft.UI.Xaml.Media.PathGeometry)path.Data;
            var figure = geom.Figures[0];
            List<Point> vertices = [transformer.TransformPoint(figure.StartPoint)];
            foreach(var segment in figure.Segments.OfType<Microsoft.UI.Xaml.Media.LineSegment>())
            {
                vertices.Add(transformer.TransformPoint(segment.Point));
            }
            // Subtract the polygon from the window region
            var regionWithHole = GetWindowRegion() - Region.CreatePolygon(vertices);
            this.SetRegion(regionWithHole);
        }
    }
}