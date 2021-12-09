using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WinUIEx.TestTools.Input
{
    /// <summary>
    ///  Touch Injection enables Windows developers to programmatically simulate touch input.
    /// </summary>
    public class TouchInjection
    {
        // Docs reference: https://docs.microsoft.com/en-us/windows/win32/input_touchinjection/touch-injection-portal

        private readonly uint _maxCount;
        private readonly Windows.Win32.Foundation.HWND _hwnd;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxCount">
        /// <para>The maximum number of touch contacts. The <i>maxCount</i> parameter must be greater than 0 and less than or equal to MAX_TOUCH_COUNT (256) as  defined in winuser.h.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api/winuser/nf-winuser-initializetouchinjection#parameters">Read more on docs.microsoft.com</see>.</para>
        /// </param>
        /// <exception cref="NotSupportedException"></exception>
        public TouchInjection(IntPtr hwnd, uint maxCount = 10)
        {
            if (maxCount > 256)
                throw new ArgumentOutOfRangeException(nameof(maxCount), "A maximum of 256 touch points are supported");
            _maxCount = maxCount;
            _hwnd = new Windows.Win32.Foundation.HWND(hwnd);
            bool success = Windows.Win32.PInvoke.InitializeTouchInjection(maxCount, Windows.Win32.UI.Input.Pointer.TOUCH_FEEDBACK_MODE.TOUCH_FEEDBACK_INDIRECT);
            if (!success)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        internal void Inject(params TouchInfo[] contacts) => Inject((IEnumerable<TouchInfo>)contacts);

        /// <summary>Simulates touch input.</summary>
        /// <param name="contacts"></param>
        /// <remarks>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api/winuser/nf-winuser-injecttouchinput">Learn more about this API from docs.microsoft.com</see>.</para>
        /// </remarks>
        /// <exception cref="System.ComponentModel.Win32Exception">Array of <a href="https://docs.microsoft.com/windows/desktop/api/winuser/ns-winuser-pointer_touch_info">TouchInfo</a> structures that represents all contacts on the desktop.
        /// The screen coordinates of each contact must be within the bounds of the desktop.</exception>
        internal void Inject(IEnumerable<TouchInfo> contacts)
        {
            var c = contacts.Select(c => c.ToNative()).ToArray();
            bool success = Windows.Win32.PInvoke.InjectTouchInput(new ReadOnlySpan<Windows.Win32.UI.Input.Pointer.POINTER_TOUCH_INFO>(c));
            if (!success)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        /// <summary>
        /// Simulates touch tapping at the middle of the provided UIElement
        /// </summary>
        /// <param name="element">Element to tap.</param>
        /// <returns></returns>
        /// <exception cref="System.ComponentModel.Win32Exception"></exception>

        public void Tap(Microsoft.UI.Xaml.UIElement element) => Tap(new Windows.Foundation.Point(element.ActualSize.X / 2, element.ActualSize.Y / 2), element);

        /// <summary>
        /// Simulates touch tapping at the provided location.
        /// </summary>
        /// <param name="tapLocation">Either raw screen coordinates, or if <paramref name="relativeTo"/> is provided, device independent coordinates relative to this object.</param>
        /// <param name="relativeTo">Element coordinates are relative to, or <c>null</c>.</param>
        /// <returns></returns>
        /// <exception cref="System.ComponentModel.Win32Exception"></exception>
        public void Tap(Windows.Foundation.Point tapLocation, Microsoft.UI.Xaml.UIElement? relativeTo)
        {
            tapLocation = ToScreenLocation(tapLocation, relativeTo);
            Inject(TouchInfo.CreateDown(tapLocation, _hwnd));
            Inject(TouchInfo.CreateUp(tapLocation, _hwnd));
        }

        /// <summary>
        /// Simulates double tapping at the provided location.
        /// </summary>
        /// <param name="element">Element to double-tap.</param>
        /// <exception cref="System.ComponentModel.Win32Exception"></exception>
        public void DoubleTap(Microsoft.UI.Xaml.UIElement element) => DoubleTap(new Windows.Foundation.Point(element.ActualSize.X / 2, element.ActualSize.Y / 2), element);

        /// <summary>
        /// Simulates double tapping at the provided location.
        /// </summary>
        /// <param name="tapLocation">Either raw screen coordinates, or if <paramref name="relativeTo"/> is provided, device independent coordinates relative to this object.</param>
        /// <param name="relativeTo">Element coordinates are relative to, or <c>null</c>.</param>
        /// <exception cref="System.ComponentModel.Win32Exception"></exception>
        public async void DoubleTap(Windows.Foundation.Point tapLocation, Microsoft.UI.Xaml.UIElement? relativeTo)
        {
            tapLocation = ToScreenLocation(tapLocation, relativeTo);
            Tap(tapLocation, null);
            Tap(tapLocation, null);
        }
        
        /*
        public Task DragAsync(Windows.Foundation.Point fromLocation, Windows.Foundation.Point toLocation, TimeSpan duration, Microsoft.UI.Xaml.UIElement relativeTo)
        {
            return DragAsync(new Windows.Foundation.Point[] { fromLocation, toLocation }, duration, relativeTo);
        }

        public Task DragAsync(IEnumerable<Windows.Foundation.Point> locations, TimeSpan duration, Microsoft.UI.Xaml.UIElement relativeTo)
        {
            return DragAsync(new IEnumerable<Windows.Foundation.Point>[] { locations }, duration, relativeTo);
        }

        public Task TwoFingerDragAsync(Windows.Foundation.Point fromLocation1, Windows.Foundation.Point toLocation1, Windows.Foundation.Point fromLocation2, Windows.Foundation.Point toLocation2, TimeSpan duration, Microsoft.UI.Xaml.UIElement relativeTo)
        {
            return DragAsync(new IEnumerable<Windows.Foundation.Point>[] {
                new Windows.Foundation.Point[] { fromLocation1, toLocation1 },
                new Windows.Foundation.Point[] { fromLocation2, toLocation2 }
            }, duration, relativeTo);
        }

        public Task DragAsync(IEnumerable<IEnumerable<Windows.Foundation.Point>> locations, TimeSpan duration, Microsoft.UI.Xaml.UIElement relativeTo)
        {
            throw new NotImplementedException();
        }*/

        private Windows.Foundation.Point ToScreenLocation(Windows.Foundation.Point point, Microsoft.UI.Xaml.UIElement? relativeTo)
        {
            if (relativeTo != null)
            {
                point = relativeTo.TransformToVisual(null).TransformPoint(point);

                var p = new Windows.Win32.Foundation.POINT() { x = (int)(point.X * relativeTo.XamlRoot.RasterizationScale), y = (int)(point.Y * relativeTo.XamlRoot.RasterizationScale) };
                bool success = Windows.Win32.PInvoke.ClientToScreen(_hwnd, ref p);
                if (!success)
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }
                return new Windows.Foundation.Point(p.x, p.y);
            }
            return point;
        }
    }
}