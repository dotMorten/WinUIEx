using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Content;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;

namespace WinUIEx
{
    /// <summary>
    /// Represents a region of the screen in device-independent pixels.
    /// </summary>
    public abstract class Region
    {
        private record Operation(Region region, CombineMode mode);

        private class RectRegion : Region
        {
            private readonly Rect _rect;
            public RectRegion(Rect rect) { _rect = rect; }
            internal override HRGN Create(ContentCoordinateConverter converter, PointInt32 screenLoc, double scaleFactor)
            {
                var p1 = Convert(_rect.Left, _rect.Top, converter, screenLoc, scaleFactor);
                var p2 = Convert(_rect.Right, _rect.Bottom, converter, screenLoc, scaleFactor);
                return PInvoke.CreateRectRgn(p1.X, p1.Y, p2.X, p2.Y);
            }
        }
        private class RectInt32Region : Region
        {
            private readonly RectInt32 _rect;
            public RectInt32Region(RectInt32 rect) { _rect = rect; }
            internal override HRGN Create(ContentCoordinateConverter converter, PointInt32 screenLoc, double scaleFactor)
             => PInvoke.CreateRectRgn(_rect.X, _rect.Y, _rect.X + _rect.Width, _rect.Y + _rect.Height);
        }

        private class RoundRectRegion : Region
        {
            private readonly Rect _rect;
            private readonly double _w;
            private readonly double _h;
            public RoundRectRegion(Rect rect, double w, double h)
            {
                _rect = rect;
                _w = w;
                _h = h;
            }
            internal override HRGN Create(ContentCoordinateConverter converter, PointInt32 screenLoc, double scaleFactor)
            {
                var p1 = Convert(_rect.Left, _rect.Top, converter, screenLoc, scaleFactor);
                var p2 = Convert(_rect.Right, _rect.Bottom, converter, screenLoc, scaleFactor);
                return PInvoke.CreateRoundRectRgn(p1.X, p1.Y, p2.X, p2.Y, (int)(_w * scaleFactor), (int)(_h * scaleFactor));
            }
        }

        private class EllipticRegion : Region
        {
            private readonly double _x1;
            private readonly double _y1;
            private readonly double _x2;
            private readonly double _y2;
            public EllipticRegion(double x1, double y1, double x2, double y2)
            {
                _x1 = x1;
                _y1 = y1;
                _x2 = x2;
                _y2 = y2;
            }
            internal override HRGN Create(ContentCoordinateConverter converter, PointInt32 screenLoc, double scaleFactor)
            { 
                var p1 = Convert(_x1, _y1, converter, screenLoc, scaleFactor);
                var p2 = Convert(_x2, _y2, converter, screenLoc, scaleFactor);
                return PInvoke.CreateEllipticRgn(p1.X, p1.Y, p2.X, p2.Y);
            }
        }

        private class PolygonRegion : Region
        {
            private readonly IEnumerable<Point> _points;
            private readonly CREATE_POLYGON_RGN_MODE _mode;
            public PolygonRegion(IEnumerable<Point> points, CREATE_POLYGON_RGN_MODE mode)
            {
                _points = points;
                _mode = mode;
            }
            internal unsafe override HRGN Create(ContentCoordinateConverter converter, PointInt32 screenLoc, double scaleFactor)
            {
                var pint32 = converter.ConvertLocalToScreen(_points.Select(p => new Point(p.X * scaleFactor, p.Y * scaleFactor)).ToArray());
                var array = pint32.Select(p => new System.Drawing.Point(p.X - screenLoc.X, p.Y - screenLoc.Y)).ToArray();
                fixed (System.Drawing.Point* pArray = array)
                    return PInvoke.CreatePolygonRgn(pArray, array.Length, _mode);
            }
             
        }

        private class CurrentRegion : Region
        {
            private nint _WindowHandle;
            public CurrentRegion(nint windowHandle)
            {
                _WindowHandle = windowHandle;
            }
            internal override HRGN Create(ContentCoordinateConverter converter, PointInt32 screenLoc, double scaleFactor)
            {
                var dest = PInvoke.CreateRectRgn(0, 0, 0, 0);
                //PInvoke.GetWindowRgnBox(new Windows.Win32.Foundation.HWND(_WindowHandle), dest);
                var type = PInvoke.GetWindowRgnBox(new Windows.Win32.Foundation.HWND(_WindowHandle), out var rect);
                return PInvoke.CreateRectRgn(rect.left, rect.top, rect.right, rect.bottom);
                // PInvoke.InvertRgn(dest, null);
                // return dest;
            }
        }

        private class CombinedRegion : Region
        {
            private readonly Region _region1;
            private readonly Region _region2;
            private readonly CombineMode _mode;
            public CombinedRegion(Region region1, Region region2, CombineMode mode)
            {
                _region1 = region1;
                _region2 = region2;
                _mode = mode;
            }

            internal override HRGN Create(ContentCoordinateConverter converter, PointInt32 screenLoc, double scaleFactor)
            {
                var rgn1 = _region1.Create(converter, screenLoc, scaleFactor);
                var rgn2 = _region2.Create(converter, screenLoc, scaleFactor);

                var dest = PInvoke.CreateRectRgn(0, 0, 0, 0);
                PInvoke.CombineRgn(dest, rgn1, rgn2, (RGN_COMBINE_MODE)_mode);
                PInvoke.DeleteObject(rgn1);
                PInvoke.DeleteObject(rgn2);
                return dest;
            }
        }

        /// <summary>
        /// Creates a rectangular region in device independent units.
        /// </summary>
        /// <param name="rect">Rectangular region.</param>
        /// <returns></returns>
        public static Region CreateRectangle(Rect rect) => new RectRegion(rect);

        /// <summary>
        /// Creates a rectangular region in screen units.
        /// </summary>
        /// <param name="rect">Rectangular region.</param>
        /// <returns></returns>
        public static Region CreateRectangle(RectInt32 rect) => new RectInt32Region(rect);

        /// <summary>
        /// Creates a rectangular region with rounded corners.
        /// </summary>
        /// <param name="rect">Rectangular region.</param>
        /// <param name="w">Specifies the width of the ellipse used to create the rounded corners.</param>
        /// <param name="h">Specifies the height of the ellipse used to create the rounded corners.</param>
        /// <returns></returns>
        public static Region CreateRoundedRectangle(Rect rect, double w, double h) => new RoundRectRegion(rect, w, h);

        /// <summary>
        /// Creates an elliptical region.
        /// </summary>
        /// <param name="x1">Specifies the x-coordinate in device independent units, of the upper-left corner of the bounding rectangle of the ellipse.</param>
        /// <param name="y1">Specifies the yx-coordinate in device independent units, of the upper-left corner of the bounding rectangle of the ellipse.</param>
        /// <param name="x2">Specifies the x-coordinate in device independent units, of the lower-right corner of the bounding rectangle of the ellipse.</param>
        /// <param name="y2">Specifies the y-coordinate in device independent units, of the lower-right corner of the bounding rectangle of the ellipse.</param>
        /// <returns></returns>
        public static Region CreateElliptic(double x1, double y1, double x2, double y2) => new EllipticRegion(x1, y1, x2, y2);

        /// <summary>
        /// Creates a polygonal region using alternate mode (fills area between odd-numbered and even-numbered polygon sides on each scan line).
        /// </summary>
        /// <param name="points">The vertices of the polygon in device independent units. The polygon is presumed closed. Each vertex can be specified only once.</param>
        /// <returns></returns>
        public static Region CreatePolygon(IEnumerable<Point> points) => new PolygonRegion(points, CREATE_POLYGON_RGN_MODE.ALTERNATE);

        /*
        /// <summary>
        /// Creates a region that represents the current visible region of a window.
        /// </summary>
        /// <param name="window">The window to obtain the region from</param>
        /// <returns></returns>
        public static Region GetWindowRegion(Microsoft.UI.Xaml.Window window) => new CurrentRegion(window.GetWindowHandle());*/

        private Region() 
        {
        }

        internal abstract HRGN Create(ContentCoordinateConverter converter, PointInt32 screenLoc, double scaleFactor);


        private protected static PointInt32 Convert(double x, double y, ContentCoordinateConverter converter, PointInt32 screenLoc, double scaleFactor) => Convert(new Point(x, y), converter, screenLoc, scaleFactor);

        private protected static PointInt32 Convert(Point local, ContentCoordinateConverter converter, PointInt32 screenLoc, double scaleFactor)
        {
            local = new Point(local.X * scaleFactor, local.Y * scaleFactor);
            var p = converter.ConvertLocalToScreen(local);
            return new PointInt32(p.X - screenLoc.X, p.Y - screenLoc.Y);
        }

        /// <summary>
        /// The Combine function combines two regions. The two regions are combined according to the specified mode.
        /// </summary>
        /// <param name="other">Region to combine with</param>
        /// <param name="mode">A mode indicating how the two regions will be combined.</param>
        /// <returns>Combined region</returns>
        public Region Combine(Region other, CombineMode mode) => new CombinedRegion(this, other, mode);

        /// <summary>
        /// Creates the intersection of the two regions.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Region And(Region a, Region b) => a.Combine(b, CombineMode.And);

        /// <summary>
        /// Creates the union of two regions except for any overlapping areas.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Region Xor(Region a, Region b) => a.Combine(b, CombineMode.Xor);

        /// <summary>
        /// Creates the union of two combined regions.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Region operator +(Region a, Region b) => a.Combine(b, CombineMode.Or);

        /// <summary>
        /// Combines the parts of <paramref name="region1"/> that are not part of <paramref name="region2"/>.
        /// </summary>
        /// <param name="region1"></param>
        /// <param name="region2"></param>
        /// <returns></returns>
        public static Region operator -(Region region1, Region region2) => region1.Combine(region2, CombineMode.Diff);
    }

    /// <summary>
    /// A mode indicating how two regions will be combined. 
    /// </summary>
    /// <seealso cref="Region.Combine(Region, CombineMode)"/>
    public enum CombineMode
    {
        /// <summary>
        /// Creates the intersection of the two combined regions.
        /// </summary>
        And = 1,
        /// <summary>
        /// Creates the union of two combined regions.
        /// </summary>
        Or = 2,
        /// <summary>
        /// Creates the union of two combined regions except for any overlapping areas.
        /// </summary>
        Xor = 3,
        /// <summary>
        /// Combines the parts of source1 that are not part of source2.
        /// </summary>
        Diff = 4,
        // Copy = 5,
    }
}
