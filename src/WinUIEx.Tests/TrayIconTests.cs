using Microsoft.UI;
using System;
using System.IO;
using System.Threading;

namespace WinUIUnitTests
{
    [TestClass]
    [TestCategory(nameof(WinUIEx.TrayIcon))]
    public class TrayIconTests
    {
        private const string TestSvg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""128"" height=""128"" viewBox=""0 0 128 128"">
                <rect width=""128"" height=""128"" rx=""16"" fill=""#0F6CBD"" />
                <circle cx=""64"" cy=""64"" r=""32"" fill=""#FFFFFF"" />
              </svg>";

        private static int s_trayIconId;

        [TestMethod]
        public async Task SetIcon_LoadsIcoFile()
        {
            string iconPath = CreateTempIco(".ico");
            try
            {
                await RunSetIconTest(iconPath);
            }
            finally
            {
                File.Delete(iconPath);
            }
        }

        [TestMethod]
        public async Task SetIcon_LoadsSvgFile()
        {
            string iconPath = CreateTempSvg(".svg");
            try
            {
                await RunSetIconTest(iconPath);
            }
            finally
            {
                File.Delete(iconPath);
            }
        }

        [TestMethod]
        public async Task SetIcon_LoadsRelativeIcoFile()
        {
            await RunSetIconTest(@"TestAssets\OKIcon.ico");
        }

        [TestMethod]
        public async Task SetIcon_LoadsRelativeSvgFile()
        {
            await RunSetIconTest(@"TestAssets\InfoIcon.svg");
        }

        [TestMethod]
        public async Task SetIcon_DetectsIcoFileFromHeaderWhenExtensionIsUnknown()
        {
            string iconPath = CreateTempIco(".bin");
            try
            {
                await RunSetIconTest(iconPath);
            }
            finally
            {
                File.Delete(iconPath);
            }
        }

        [TestMethod]
        public async Task SetIcon_DetectsSvgFileFromContentWhenExtensionIsUnknown()
        {
            string iconPath = CreateTempSvg(".bin");
            try
            {
                await RunSetIconTest(iconPath);
            }
            finally
            {
                File.Delete(iconPath);
            }
        }

        private static async Task RunSetIconTest(string iconPath)
        {
            await UITestHelper.RunUITest(_ =>
            {
                using var trayIcon = new WinUIEx.TrayIcon((uint)Interlocked.Increment(ref s_trayIconId), new IconId(0), "test");
                trayIcon.SetIcon(iconPath);
                trayIcon.IsVisible = true;
                trayIcon.IsVisible = false;
                return Task.CompletedTask;
            });
        }

        private static string CreateTempSvg(string extension)
        {
            string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");
            File.WriteAllText(path, TestSvg);
            return path;
        }

        private static string CreateTempIco(string extension)
        {
            string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");
            File.WriteAllBytes(path, CreateIcoBytes());
            return path;
        }

        private static byte[] CreateIcoBytes()
        {
            const int width = 16;
            const int height = 16;
            const int bytesPerPixel = 4;
            int xorMaskSize = width * height * bytesPerPixel;
            int andMaskStride = 4;
            int andMaskSize = andMaskStride * height;
            int imageSize = 40 + xorMaskSize + andMaskSize;

            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);

            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)1);

            writer.Write((byte)width);
            writer.Write((byte)height);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write(imageSize);
            writer.Write(6 + 16);

            writer.Write(40);
            writer.Write(width);
            writer.Write(height * 2);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write(0);
            writer.Write(xorMaskSize + andMaskSize);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    writer.Write((byte)0xBD);
                    writer.Write((byte)0x6C);
                    writer.Write((byte)0x0F);
                    writer.Write((byte)0xFF);
                }
            }

            for (int i = 0; i < andMaskSize; i++)
                writer.Write((byte)0x00);

            return stream.ToArray();
        }
    }
}
