using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WinUIUnitTests
{
    [TestClass]
    [TestCategory("SvgIconHelper")]
    public class SvgIconHelperTests
    {
        [TestMethod]
        public void CreateIconFromSvg_ContainsExpectedVisibleColors()
        {
            nint hIcon = CreateIconFromSvg(@"TestAssets\InfoIcon.svg", 64, 64);
            try
            {
                uint[] pixels = RenderIconPixels(hIcon, 64, 64);

                bool hasWhitePixel = false;
                bool hasBluePixel = false;
                HashSet<uint> visibleColors = new HashSet<uint>();

                foreach (uint pixel in pixels)
                {
                    byte blue = (byte)(pixel & 0xFF);
                    byte green = (byte)((pixel >> 8) & 0xFF);
                    byte red = (byte)((pixel >> 16) & 0xFF);
                    byte alpha = (byte)((pixel >> 24) & 0xFF);
                    if (alpha == 0 && red == 0 && green == 0 && blue == 0)
                        continue;

                    visibleColors.Add(pixel);
                    if (red > 230 && green > 230 && blue > 230)
                        hasWhitePixel = true;
                    if (red < 40 && green > 80 && green < 140 && blue > 150)
                        hasBluePixel = true;
                }

                string diagnostic = $"Visible color count: {visibleColors.Count}. Sample colors: {string.Join(", ", visibleColors.Take(8).Select(c => $"0x{c:X8}"))}";
                Assert.IsTrue(hasWhitePixel, $"Expected at least one visible white pixel in the generated icon. {diagnostic}");
                Assert.IsTrue(hasBluePixel, $"Expected at least one visible blue pixel in the generated icon. {diagnostic}");
                Assert.IsTrue(visibleColors.Count > 1, $"Expected more than one visible color in the generated icon. {diagnostic}");
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }

        [TestMethod]
        public void CreateIconFromSvg_ContainsVisiblePixelsAtTraySize()
        {
            nint hIcon = CreateIconFromSvg(@"TestAssets\InfoIcon.svg", 16, 16);
            try
            {
                uint[] pixels = RenderIconPixels(hIcon, 16, 16);
                HashSet<uint> visibleColors = new HashSet<uint>();
                bool hasBluePixel = false;

                foreach (uint pixel in pixels)
                {
                    byte blue = (byte)(pixel & 0xFF);
                    byte green = (byte)((pixel >> 8) & 0xFF);
                    byte red = (byte)((pixel >> 16) & 0xFF);
                    byte alpha = (byte)((pixel >> 24) & 0xFF);
                    if (alpha == 0 && red == 0 && green == 0 && blue == 0)
                        continue;

                    visibleColors.Add(pixel);
                    if (red < 40 && green > 80 && green < 140 && blue > 150)
                        hasBluePixel = true;
                }

                string diagnostic = $"Visible color count: {visibleColors.Count}. Sample colors: {string.Join(", ", visibleColors.Take(8).Select(c => $"0x{c:X8}"))}";
                Assert.IsTrue(hasBluePixel, $"Expected at least one visible blue pixel in the generated tray-sized icon. {diagnostic}");
                Assert.IsTrue(visibleColors.Count > 1, $"Expected more than one visible rendered pixel color in the generated tray-sized icon. {diagnostic}");
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }

        private static nint CreateIconFromSvg(string path, uint width, uint height)
        {
            Type helperType = typeof(WinUIEx.TrayIcon).Assembly.GetType("WinUIEx.SvgIconHelper", throwOnError: true)!;
            MethodInfo method = helperType.GetMethod("CreateIconFromSvg", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
            object handle = method.Invoke(null, new object[] { path, width, height })!;
            Type handleType = handle.GetType();
            FieldInfo valueField = handleType.GetField("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (valueField is not null)
                return (nint)valueField.GetValue(handle)!;

            PropertyInfo valueProperty = handleType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (valueProperty is not null)
                return (nint)valueProperty.GetValue(handle)!;

            throw new InvalidOperationException($"Could not extract handle value from {handleType.FullName}.");
        }

        private static uint[] RenderIconPixels(nint hIcon, int width, int height)
        {
            nint hdc = CreateCompatibleDC(0);
            Assert.AreNotEqual(nint.Zero, hdc);
            try
            {
                BITMAPINFO info = new BITMAPINFO
                {
                    bmiHeader = new BITMAPINFOHEADER
                    {
                        biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                        biWidth = width,
                        biHeight = -height,
                        biPlanes = 1,
                        biBitCount = 32,
                        biCompression = 0
                    }
                };

                nint dibBits;
                nint hBitmap = CreateDIBSection(0, ref info, 0, out dibBits, 0, 0);
                Assert.AreNotEqual(nint.Zero, hBitmap);
                nint oldObject = SelectObject(hdc, hBitmap);
                Assert.AreNotEqual(nint.Zero, oldObject);
                try
                {
                    Assert.IsTrue(DrawIconEx(hdc, 0, 0, hIcon, width, height, 0, 0, 0x0003));
                    int[] rawPixels = new int[width * height];
                    Marshal.Copy(dibBits, rawPixels, 0, rawPixels.Length);
                    return rawPixels.Select(pixel => unchecked((uint)pixel)).ToArray();
                }
                finally
                {
                    SelectObject(hdc, oldObject);
                    DeleteObject(hBitmap);
                }
            }
            finally
            {
                DeleteDC(hdc);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(nint hIcon);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern nint CreateCompatibleDC(nint hdc);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern nint CreateDIBSection(nint hdc, ref BITMAPINFO pbmi, uint usage, out nint ppvBits, nint hSection, uint offset);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern nint SelectObject(nint hdc, nint h);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DrawIconEx(nint hdc, int xLeft, int yTop, nint hIcon, int cxWidth, int cyWidth, uint istepIfAniCur, nint hbrFlickerFreeDraw, uint diFlags);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(nint hObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteDC(nint hdc);

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            public uint bmiColors;
        }
    }
}
