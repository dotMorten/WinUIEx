using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUIEx.TestTools
{
    /// <summary>
    /// A set of helper extensions for UI Testing
    /// </summary>
    public static class UIExtensions
    {
#if EXPERIMENTAL
        /// <summary>
        /// Creates a screenshot of the specified window
        /// </summary>
        /// <param name="window">window</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<object> CaptureWindow(this global::Microsoft.UI.Xaml.Window window)
        {
            var tcs = new TaskCompletionSource<byte[]>();
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            Windows.Graphics.Capture.GraphicsCaptureItem item = CaptureHelper.CreateItemForWindow(hwnd);
            if (item == null)
                throw new Exception("Failed to start window capture");


            using var device = Direct3D11Helper.CreateDevice();
            using var d3dDevice = Direct3D11Helper.CreateSharpDXDevice(device);
            using var framePool = Windows.Graphics.Capture.Direct3D11CaptureFramePool.Create(
                   device,
                   Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                   2,
                   item.Size);

            using var session = framePool.CreateCaptureSession(item);

            framePool.FrameArrived += (sender, e) =>
            {
                try
                {
                    using (var frame = sender.TryGetNextFrame())
                    {
                        if (frame is null) return;
                        session.Dispose();

                        System.Diagnostics.Debugger.Launch();
                        //SharpDX.Direct3D11.Texture2DDescription desc = frame.Description;
                        //desc.CpuAccessFlags = CpuAccessFlags.Read;
                        //desc.Usage = ResourceUsage.Staging;
                        //desc.OptionFlags = ResourceOptionFlags.None;
                        //desc.BindFlags = BindFlags.None;
                        using (var bitmap = Direct3D11Helper.CreateSharpDXTexture2D(frame.Surface))
                        {
                            using var surface = bitmap.QueryInterface<SharpDX.DXGI.Surface>();
                            SharpDX.DataStream dataStream = null;
                            var map = surface.Map(SharpDX.DXGI.MapFlags.Read, out dataStream);
                            int lines = (int)(dataStream.Length / map.Pitch);
                            byte[] data = new byte[surface.Description.Width * surface.Description.Height * 4];

                            int dataCounter = 0;
                            // width of the surface - 4 bytes per pixel.
                            int actualWidth = surface.Description.Width * 4;
                            for (int y = 0; y < lines; y++)
                            {
                                for (int x = 0; x < map.Pitch; x++)
                                {
                                    if (x < actualWidth)
                                    {
                                        data[dataCounter++] = dataStream.Read<byte>();
                                    }
                                    else
                                    {
                                        dataStream.Read<byte>();
                                    }
                                }
                            }
                            dataStream.Dispose();
                            surface.Unmap();

                            tcs.SetResult(data);
                        }
                    }
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }

            };
            session.StartCapture();
            return await tcs.Task;
        }
#endif

        /// <summary>
        /// Completes when the provided element has loaded.
        /// </summary>
        /// <param name="element">Element.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns></returns>
        public static async Task LoadAsync(this global::Microsoft.UI.Xaml.FrameworkElement element, System.Threading.CancellationToken cancellationToken = default)
        {
            if (element.IsLoaded)
                return;
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            cancellationToken.Register(() => tcs.TrySetCanceled());
            Microsoft.UI.Xaml.RoutedEventHandler handler = (s, e) =>
            {
                tcs.TrySetResult(null);
            };
            element.Loaded += handler;
            try
            {
                await tcs.Task;
            }
            finally
            {
                element.Loaded -= handler;
            };
        }

        /// <summary>
        /// Completes when the provided element has loaded.
        /// </summary>
        /// <param name="element">Element.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns></returns>
        public static async Task LayoutUpdatedAsync(this global::Microsoft.UI.Xaml.FrameworkElement element, System.Threading.CancellationToken cancellationToken = default)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            cancellationToken.Register(() => tcs.TrySetCanceled());
            EventHandler<object> handler = (s, e) =>
            {
                tcs.TrySetResult(null);
            };
            element.LayoutUpdated += handler;
            try
            {
                await tcs.Task;
            }
            finally
            {
                element.LayoutUpdated -= handler;
            };
        }

        /// <summary>
        /// Completes when the provided element has changed size.
        /// </summary>
        /// <param name="element">Element.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns></returns>
        public static async Task SizeChangedAsync(this global::Microsoft.UI.Xaml.FrameworkElement element, System.Threading.CancellationToken cancellationToken = default)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            cancellationToken.Register(() => tcs.TrySetCanceled());
            Microsoft.UI.Xaml.SizeChangedEventHandler handler = (s, e) =>
            {
                tcs.TrySetResult(null);
            };
            element.SizeChanged += handler;
            try
            {
                await tcs.Task;
            }
            finally
            {
                element.SizeChanged -= handler;
            };
        }

        /// <summary>
        /// Returns a bitmap of the provided element.
        /// </summary>
        /// <param name="element">Element.</param>
        /// <returns></returns>
        public static async Task<RenderTargetBitmap> AsBitmapAsync(this global::Microsoft.UI.Xaml.FrameworkElement element)
        {
            var bitmap = new RenderTargetBitmap();
            await bitmap.RenderAsync(element);
            return bitmap;
        }
    }
}