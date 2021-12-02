using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using System.Runtime.InteropServices.WindowsRuntime;

namespace WinUIEx.Testing
{
    /// <summary>
    /// Method for finding pixels connected to each other. Great for finding UI Elements on the screen
    /// based on pixel color filter.
    /// A good explanation of the Connected Component Analysis can be seen here: https://www.youtube.com/watch?v=ticZclUYy88
    /// Uses a 4-connectivity 2-pass Hoshen-Kopelman algorithm
    /// </summary>
    public static class ImageAnalysis
    {
        /// <summary>
        /// Finds a set of pixels that are connected to each other, Looks at any pixels that are not black and/or transparent
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static Task<IList<Blob>> FindConnectedPixelsAsync(this RenderTargetBitmap image)
        {
            return FindConnectedPixelsAsync(image, (c) => (c.R > 0 || c.G > 0 || c.B > 0) && c.A > 0);
        }

        /// <summary>
        /// Finds a set of pixels selected by a filter that are connected to each other
        /// </summary>
        /// <param name="element"></param>
        /// <param name="includePixelFunction">Pixel filter function</param>
        /// <returns></returns>
        public static async Task<IList<Blob>> FindConnectedPixelsAsync(this FrameworkElement element, Func<Color, bool> includePixelFunction)
        {
            var bitmap = await UIExtensions.AsBitmapAsync(element);
            return await FindConnectedPixelsAsync(bitmap, includePixelFunction);
        }

        /// <summary>
        /// Finds a set of pixels selected by a filter that are connected to each other
        /// </summary>
        /// <param name="element"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static async Task<IList<Blob>> FindConnectedPixelsAsync(this FrameworkElement element, Color color)
        {
            var bitmap = await UIExtensions.AsBitmapAsync(element);
            return await FindConnectedPixelsAsync(bitmap, color);
        }

        /// <summary>
        /// Finds a set of pixels of a certain color that are connected to each other
        /// </summary>
        /// <param name="image"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Task<IList<Blob>> FindConnectedPixelsAsync(this RenderTargetBitmap image, Color color)
        {
            return FindConnectedPixelsAsync(image, c => c.A == color.A && c.R == color.R && c.G == color.G && c.B == color.B);
        }

        /// <summary>
        /// Finds a set of pixels of a certain color that are connected to each other
        /// </summary>
        /// <param name="image"></param>
        /// <param name="includePixelFunction"></param>
        /// <returns></returns>
        public static async Task<IList<Blob>> FindConnectedPixelsAsync(this RenderTargetBitmap image, Func<Color, bool> includePixelFunction)
        {
            int width = image.PixelWidth;
            int height = image.PixelHeight;
            bool[] pixels = new bool[width * height];

            double scaleFactor = UnitTestClient.Window?.Content.XamlRoot?.RasterizationScale ?? 1;

            int bitsPerPixel = 32;
            int stride = image.PixelWidth * bitsPerPixel / 8;
            var buffer = await image.GetPixelsAsync().AsTask().ConfigureAwait(false);
            using var stream = buffer.AsStream();
            for (int i = 0; i < pixels.Length; i++)
            {
                byte b = (byte)stream.ReadByte();
                byte g = (byte)stream.ReadByte();
                byte r = (byte)stream.ReadByte();
                byte a = (byte)stream.ReadByte();

                pixels[i] = includePixelFunction(Color.FromArgb(a, r, g, b));
            }

            Func<Color, bool> ismatch = includePixelFunction;
            int[] labels = new int[pixels.Length];
            int currentLabel = 0;
            // First pass - Label pixels
            UnionFind<int> sets = new UnionFind<int>();
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    var idx = j + i * width;
                    bool v = pixels[idx];
                    if (v)
                    {
                        var l1 = i == 0 ? 0 : labels[j + (i - 1) * width];
                        var l2 = j == 0 ? 0 : labels[j + i * width - 1];
                        if (l1 == 0 && l2 == 0)
                        {
                            //Assign new label
                            currentLabel++;
                            labels[idx] = currentLabel;
                            sets.MakeSet(currentLabel);
                        }
                        else if (l1 > 0 && l2 == 0)
                            labels[idx] = l1; //Copy label from neighbor
                        else if (l1 == 0 && l2 > 0)
                            labels[idx] = l2; //Copy label from neighbor
                        else
                        {
                            labels[idx] = l1 < l2 ? l1 : l2; // Both neighbors have values. Grab the smallest label
                            if (l1 != l2)
                                sets.Union(sets.Find(l1), sets.Find(l2)); //store L1 is equivalent to L2
                        }
                    }
                }
            }
            // Second pass: Update equivalent labels
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    var idx = j + i * width;
                    var lbl = labels[idx];
                    if (lbl > 0)
                    {
                        var l = sets.Find(lbl);
                        labels[idx] = l.Value;
                    }
                }
            }
            // Generate blobs
            var blobs = new List<Blob>();
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    var idx = j + i * width;
                    var lbl = labels[idx];
                    if (lbl > 0)
                    {
                        var blob = blobs.Where(b => b.Id == lbl).FirstOrDefault();
                        if (blob != null)
                        {
                            blob.MinColumn = Math.Min(blob.MinColumn, j);
                            blob.MaxColumn = Math.Max(blob.MaxColumn, j);
                            blob.MinRow = Math.Min(blob.MinRow, i);
                            blob.MaxRow = Math.Max(blob.MaxRow, i);
                        }
                        else
                        {
                            blob = new Blob() { Id = lbl, MinColumn = j, MaxColumn = j, MinRow = i, MaxRow = i };
                            blobs.Add(blob);
                        }
                        blob.Pixels.Add(new System.Drawing.Point(j, i));
                    }
                }
            }
            foreach (var b in blobs)
            {
                b.MinColumn /= scaleFactor;
                b.MaxColumn /= scaleFactor;
                b.MinRow /= scaleFactor;
                b.MaxRow /= scaleFactor;
            }
            return blobs;
        }

        private class UnionFind<T>
        {
            // A generic Union Find Data Structure 
            // Based on https://github.com/thomas-villagers/unionfind.cs
            private Dictionary<T, SetElement> dict;

            public class SetElement
            {
                public SetElement Parent { get; internal set; }
                public T Value { get; }
                public int Size { get; internal set; }
                public SetElement(T value)
                {
                    Value = value;
                    Parent = this;
                    Size = 1;
                }
                public override string ToString() => string.Format("{0}, size:{1}", Value, Size);
            }

            public UnionFind()
            {
                dict = new Dictionary<T, SetElement>();
            }

            public SetElement MakeSet(T value)
            {
                SetElement element = new SetElement(value);
                dict[value] = element;
                return element;
            }

            public SetElement Find(T value)
            {
                SetElement element = dict[value];
                SetElement root = element;
                while (root.Parent != root)
                {
                    root = root.Parent;
                }
                SetElement z = element;
                while (z.Parent != z)
                {
                    SetElement temp = z;
                    z = z.Parent;
                    temp.Parent = root;
                }
                return root;
            }

            public SetElement Union(SetElement root1, SetElement root2)
            {
                if (root2.Size > root1.Size)
                {
                    root2.Size += root1.Size;
                    root1.Parent = root2;
                    return root2;
                }
                else
                {
                    root1.Size += root2.Size;
                    root2.Parent = root1;
                    return root1;
                }
            }
        }

        /// <summary>
        /// Blob bounding box
        /// </summary>
        public class Blob
        {
            public int Id;
            public double MinColumn;
            public double MaxColumn;
            public double MinRow;
            public double MaxRow;
            public double Width => MaxColumn - MinColumn + 1;
            public double Height => MaxRow - MinRow + 1;
            public double CenterRow => MinRow + Height / 2;
            public double CenterColumn => MinColumn + Width / 2;
            public List<System.Drawing.Point> Pixels { get; } = new List<System.Drawing.Point>();
        }

        /// <summary>
        /// Merges two blobs to a new blob covering them both
        /// </summary>
        /// <param name="blobs"></param>
        /// <returns></returns>
        public static Blob Union(this IEnumerable<Blob> blobs)
        {
            var b = blobs.FirstOrDefault();
            if (b != null)
                foreach (var bl in blobs.Skip(1))
                {
                    b.MinColumn = Math.Min(bl.MinColumn, b.MinColumn);
                    b.MinRow = Math.Min(bl.MinRow, b.MinRow);
                    b.MaxColumn = Math.Max(bl.MaxColumn, b.MaxColumn);
                    b.MaxRow = Math.Max(bl.MaxRow, b.MaxRow);
                }
            return b;
        }
    }
}
