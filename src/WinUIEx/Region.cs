using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            internal override HRGN Create(double scalefactor)
             => PInvoke.CreateRectRgn((int)(_rect.Left * scalefactor), (int)(_rect.Top * scalefactor),
                    (int)(_rect.Right * scalefactor), (int)(_rect.Bottom * scalefactor));
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
            internal override HRGN Create(double scalefactor)
             => PInvoke.CreateRoundRectRgn((int)(_rect.Left * scalefactor), (int)(_rect.Top * scalefactor),
                    (int)(_rect.Right * scalefactor), (int)(_rect.Bottom * scalefactor), (int)(_w * scalefactor), (int)(_h * scalefactor));
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

            internal override HRGN Create(double scaleFactor)
            {
                var rgn1 = _region1.Create(scaleFactor);
                var rgn2 = _region2.Create(scaleFactor);

                var dest = PInvoke.CreateRectRgn(0, 0, 0, 0);
                PInvoke.CombineRgn(dest, rgn1, rgn2, (RGN_COMBINE_MODE)_mode);
                PInvoke.DeleteObject(rgn1);
                PInvoke.DeleteObject(rgn2);
                return dest;
            }
        }

        /// <summary>
        /// Creates a rectangular region.
        /// </summary>
        /// <param name="rect">Rectangular region.</param>
        /// <returns></returns>
        public static Region Create(Rect rect) => new RectRegion(rect);

        /// <summary>
        /// Creates a rectangular region with rounded corners.
        /// </summary>
        /// <param name="rect">Rectangular region.</param>
        /// <param name="w">Specifies the width of the ellipse used to create the rounded corners.</param>
        /// <param name="h">Specifies the height of the ellipse used to create the rounded corners.</param>
        /// <returns></returns>
        public static Region Create(Rect rect, double w, double h) => new RoundRectRegion(rect, w, h);

        private Region() 
        {
        }

        internal abstract HRGN Create(double scalefactor);

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
