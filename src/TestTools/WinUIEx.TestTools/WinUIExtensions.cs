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
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns></returns>
        public static async Task<RenderTargetBitmap> AsBitmapAsync(this global::Microsoft.UI.Xaml.FrameworkElement element)
        {
            var bitmap = new RenderTargetBitmap();
            await bitmap.RenderAsync(element);
            return bitmap;
        }
    }
}