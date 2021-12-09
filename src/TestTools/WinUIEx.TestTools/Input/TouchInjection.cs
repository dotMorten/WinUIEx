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

        public async Task TapAsync(Windows.Foundation.Point tapLocation, Microsoft.UI.Xaml.UIElement? relativeTo)
        {
            Windows.Win32.PInvoke.GetWindowRect(_hwnd, out Windows.Win32.Foundation.RECT lpRect);
            if (relativeTo != null)
            {
                tapLocation = relativeTo.TransformToVisual(null).TransformPoint(tapLocation);
                // TODO: Account for titlebar offset
            }
            tapLocation = new Windows.Foundation.Point(tapLocation.X + lpRect.left, tapLocation.Y + lpRect.top);
            var pi = new PointerInfo() { PointerId = 0, PointerType = PointerInputType.Touch, PointerFlags = PointerFlag.Down | PointerFlag.InRange | PointerFlag.InContact, PixelLocation = tapLocation, Hwnd = _hwnd };
            var touchInfo = new TouchInfo() { PointerInfo = pi };
            Inject(touchInfo);
            pi.PointerFlags = PointerFlag.Up | PointerFlag.InRange;
            var p1 = new TouchInfo() { PointerInfo = pi };
            Inject(p1);
        }

        public async Task DoubleTapAsync(Windows.Foundation.Point tapLocation, Microsoft.UI.Xaml.UIElement? relativeTo)
        {
            await TapAsync(tapLocation, relativeTo);
            await Task.Delay(100);
            await TapAsync(tapLocation, relativeTo);
        }
        /*
        public Task DragAsync(Windows.Foundation.Point fromLocation, Windows.Foundation.Point toLocation, TimeSpan duration, Microsoft.UI.Xaml.UIElement relativeTo)
        {
            throw new NotImplementedException();
        }

        public Task DragAsync(IEnumerable<Windows.Foundation.Point> locations, TimeSpan duration, Microsoft.UI.Xaml.UIElement relativeTo)
        {
            throw new NotImplementedException();
        }

        public Task DragAsync(IEnumerable<IEnumerable<Windows.Foundation.Point>> locations, TimeSpan duration, Microsoft.UI.Xaml.UIElement relativeTo)
        {
            throw new NotImplementedException();
        }

        public Task TwoFingerDragAsync(Windows.Foundation.Point fromLocation1, Windows.Foundation.Point toLocation1, Windows.Foundation.Point fromLocation2, Windows.Foundation.Point toLocation2, TimeSpan duration, Microsoft.UI.Xaml.UIElement relativeTo)
        {
            throw new NotImplementedException();
        }*/
    }
}