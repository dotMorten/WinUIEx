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
        private readonly Microsoft.UI.Xaml.Window _window;
        private readonly Microsoft.UI.Windowing.AppWindow _appWindow;
        /// <summary>
        /// Number of touch events per second.
        /// </summary>
        public int TouchesPerSecond { get; set; } = 60;

        /// <summary>
        /// Creates a new instance of the <see cref="TouchInjection"/> class
        /// </summary>
        /// <param name="window">Window to inject touch events for.</param>
        /// <param name="maxCount">
        /// <para>The maximum number of touch contacts. The <i>maxCount</i> parameter must be greater than 0 and less than or equal to MAX_TOUCH_COUNT (256) as  defined in winuser.h.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api/winuser/nf-winuser-initializetouchinjection#parameters">Read more on docs.microsoft.com</see>.</para>
        /// </param>
        /// <exception cref="NotSupportedException"></exception>
        public TouchInjection(Microsoft.UI.Xaml.Window window, uint maxCount = 10)
        {
            if (maxCount > 256)
                throw new ArgumentOutOfRangeException(nameof(maxCount), "A maximum of 256 touch points are supported");
            _maxCount = maxCount;
            _window = window;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            _hwnd = new Windows.Win32.Foundation.HWND(hwnd);

            _appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd));
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
        /// Simulates touch tapping at the middle of the provided UIElement
        /// </summary>
        /// <param name="duration">The speed of the tap.</param>
        /// <param name="element">Element to tap.</param>
        /// <returns></returns>
        /// <exception cref="System.ComponentModel.Win32Exception"></exception>

        public Task TapAsync(TimeSpan duration, Microsoft.UI.Xaml.UIElement element) => TapAsync(new Windows.Foundation.Point(element.ActualSize.X / 2, element.ActualSize.Y / 2), duration, element);

        /// <summary>
        /// Simulates touch tapping at the provided location.
        /// </summary>
        /// <param name="tapLocation">Either raw screen coordinates, or if <paramref name="relativeTo"/> is provided, device independent coordinates relative to this object.</param>
        /// <param name="duration">The speed of the tap.</param>
        /// <param name="relativeTo">Element coordinates are relative to, or <c>null</c>.</param>
        /// <returns></returns>
        /// <exception cref="System.ComponentModel.Win32Exception"></exception>
        public async Task TapAsync(Windows.Foundation.Point tapLocation, TimeSpan duration, Microsoft.UI.Xaml.UIElement? relativeTo)
        {
            tapLocation = ToScreenLocation(tapLocation, relativeTo);
            Inject(TouchInfo.CreateDown(tapLocation, _hwnd));
            if (duration.Ticks > 0)
                await Task.Delay(duration);
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
        public void DoubleTap(Windows.Foundation.Point tapLocation, Microsoft.UI.Xaml.UIElement? relativeTo)
        {
            tapLocation = ToScreenLocation(tapLocation, relativeTo);
            Tap(tapLocation, null);
            Tap(tapLocation, null);
        }

        /// <summary>
        /// Simulates double tapping at the provided location.
        /// </summary>
        /// <param name="tapDuration">Duration of a single tap</param>
        /// <param name="timeBetweenTaps">Pause between the two taps</param>
        /// <param name="element">Element to double-tap.</param>
        /// <exception cref="System.ComponentModel.Win32Exception"></exception>
        public Task DoubleTapAsync(TimeSpan tapDuration, TimeSpan timeBetweenTaps, Microsoft.UI.Xaml.UIElement element) => DoubleTapAsync(new Windows.Foundation.Point(element.ActualSize.X / 2, element.ActualSize.Y / 2), tapDuration, timeBetweenTaps, element);

        /// <summary>
        /// Simulates double tapping at the provided location.
        /// </summary>
        /// <param name="tapLocation">Either raw screen coordinates, or if <paramref name="relativeTo"/> is provided, device independent coordinates relative to this object.</param>
        /// <param name="tapDuration">Duration of a single tap</param>
        /// <param name="timeBetweenTaps">Pause between the two taps</param>
        /// <param name="relativeTo">Element coordinates are relative to, or <c>null</c>.</param>
        /// <exception cref="System.ComponentModel.Win32Exception"></exception>
        public async Task DoubleTapAsync(Windows.Foundation.Point tapLocation, TimeSpan tapDuration, TimeSpan timeBetweenTaps, Microsoft.UI.Xaml.UIElement? relativeTo)
        {
            tapLocation = ToScreenLocation(tapLocation, relativeTo);
            await TapAsync(tapLocation, tapDuration, null);
            if (timeBetweenTaps.Ticks > 0)
                await Task.Delay(timeBetweenTaps);
            await TapAsync(tapLocation, tapDuration, null);
        }

        /// <summary>
        /// Performs a drag between two locations over the specified duration
        /// </summary>
        /// <param name="fromLocation">Start location</param>
        /// <param name="toLocation">End location</param>
        /// <param name="duration">Time for the drag operation</param>
        /// <param name="relativeTo">Element coordinates are relative to, or <c>null</c>.</param>
        /// <returns></returns>
        public Task DragAsync(Windows.Foundation.Point fromLocation, Windows.Foundation.Point toLocation, TimeSpan duration, Microsoft.UI.Xaml.UIElement? relativeTo)
        {
            var midLocation = new Windows.Foundation.Point((fromLocation.X + toLocation.X) / 2, (fromLocation.Y + toLocation.Y) / 2);
            return DragAsync(Interpolate(fromLocation, toLocation, (int)Math.Max(3, duration.TotalSeconds * TouchesPerSecond)), duration, relativeTo);
        }

        private IEnumerable<Windows.Foundation.Point> Interpolate(Windows.Foundation.Point fromLocation, Windows.Foundation.Point toLocation, int count)
        {
            var dx = (toLocation.X - fromLocation.X) / (count - 1);
            var dy = (toLocation.Y - fromLocation.Y) / (count - 1);
            yield return fromLocation;
            for (int i = 1; i < count - 1; i++)
            {
                yield return new Windows.Foundation.Point(fromLocation.X + dx * i, fromLocation.Y + dy * i);
            }
            yield return toLocation;
        }

        /// <summary>
        /// Performs a series of move events over the provided locations evenly spaced over the specified duration.
        /// </summary>
        /// <param name="locations">Locations of the move events</param>
        /// <param name="duration">Time for the drag operation</param>
        /// <param name="relativeTo">Element coordinates are relative to, or <c>null</c>.</param>
        /// <returns></returns>
        public Task DragAsync(IEnumerable<Windows.Foundation.Point> locations, TimeSpan duration, Microsoft.UI.Xaml.UIElement? relativeTo)
            => DragAsync(new IEnumerable<Windows.Foundation.Point>[] { locations }, duration, relativeTo);

        /// <summary>
        /// Performs a drag between two locations over the specified duration
        /// </summary>
        /// <param name="fromLocation1">Start location for the first finger</param>
        /// <param name="toLocation1">End location for the first finger</param>
        /// <param name="fromLocation2">Start location for the second finger</param>
        /// <param name="toLocation2">End location for the second finger</param>
        /// <param name="duration">Time for the drag operation</param>
        /// <param name="relativeTo">Element coordinates are relative to, or <c>null</c>.</param>
        /// <returns></returns>
        public Task TwoFingerDragAsync(Windows.Foundation.Point fromLocation1, Windows.Foundation.Point toLocation1, Windows.Foundation.Point fromLocation2, Windows.Foundation.Point toLocation2, TimeSpan duration, Microsoft.UI.Xaml.UIElement? relativeTo)
        {
            var count = (int)Math.Max(3, duration.TotalSeconds * TouchesPerSecond);
            return DragAsync(new IEnumerable<Windows.Foundation.Point>[] {
                Interpolate(fromLocation1, toLocation1, count),
                Interpolate(fromLocation2, toLocation2, count)
            }, duration, relativeTo);
        }

        /// <summary>
        /// Performs a two-finger pinch or zoom gesture
        /// </summary>
        /// <param name="center">Center of the gesture</param>
        /// <param name="startWidth">The starting width between the two touch locations.</param>
        /// <param name="endWidth">The ending width between the two touch locations.</param>
        /// <param name="duration">Time for the drag operation</param>
        /// <param name="relativeTo">Element coordinates are relative to, or <c>null</c>.</param>
        /// <returns></returns>
        public Task PinchAsync(Windows.Foundation.Point center, double startWidth, double endWidth, TimeSpan duration, Microsoft.UI.Xaml.UIElement? relativeTo)
        {
            var f1start = new Windows.Foundation.Point(center.X - startWidth / 2, center.Y);
            var f1end = new Windows.Foundation.Point(center.X - endWidth / 2, center.Y);
            var f2start = new Windows.Foundation.Point(center.X + startWidth / 2, center.Y);
            var f2end = new Windows.Foundation.Point(center.X + endWidth / 2, center.Y);
            return TwoFingerDragAsync(f1start, f1end, f2start, f2end, duration, relativeTo);
        }

        /// <summary>
        /// Performs a series of move events over the provided locations evenly spaced over the specified duration.
        /// </summary>
        /// <param name="locations">A collection of move events for each touch point.</param>
        /// <param name="duration">Time for the drag operation</param>
        /// <param name="relativeTo">Element coordinates are relative to, or <c>null</c>.</param>
        /// <returns></returns>
        public async Task DragAsync(IEnumerable<IEnumerable<Windows.Foundation.Point>> locations, TimeSpan duration, Microsoft.UI.Xaml.UIElement? relativeTo)
        {
            if (locations is null)
                throw new ArgumentNullException(nameof(locations));
            if (!locations.Any())
                return;
            if (locations.Select(l => l.Count()).Distinct().Count() > 2)
                throw new ArgumentException("Each set of locations must have the same number of points");
            if(locations.Count() > _maxCount)
                throw new ArgumentException("More touch operations provided than the maximum touch count");

            var count = locations.First().Count();
            if (count == 0)
                return;
            if (count < 2)
                throw new ArgumentException("Each set of locations must have at least two points");
            TouchInfo[] touches = new TouchInfo[locations.Count()];

            for (int i = 0; i < count; i++)
            {
                uint j = 0;
                foreach (var location in locations)
                {
                    var point = location.ElementAt(i);
                    if (i == 0)
                        touches[j] = TouchInfo.CreateDown(ToScreenLocation(point, relativeTo), _hwnd, j + 1);
                    else
                        touches[j] = TouchInfo.CreateMove(ToScreenLocation(point, relativeTo), _hwnd, j + 1);
                    j++;
                }
                Inject(touches);
                if (duration.Ticks > 0)
                    await Task.Delay((int)(duration.TotalMilliseconds / (count - 1)));
            }
            foreach (var touch in touches)
            {
                var p = touch.PointerInfo;
                p.PointerFlags = PointerFlag.Up;
                touch.PointerInfo = p;
            }
            Inject(touches);
        }

        /// <summary>
        /// Performs a two-finger rotate gesture around the provided center
        /// </summary>
        /// <param name="center">The middle of the rotation gesture</param>
        /// <param name="radius">The distance from the center to the two touch points</param>
        /// <param name="angle">Amount of rotation to perform</param>
        /// <param name="duration">Time for the drag operation</param>
        /// <param name="relativeTo">Element coordinates are relative to, or <c>null</c>.</param>
        /// <returns></returns>
        public Task RotateAsync(Windows.Foundation.Point center, double radius, double angle, TimeSpan duration, Microsoft.UI.Xaml.UIElement? relativeTo)
        {
            if(relativeTo != null)
            {
                center = ToScreenLocation(center, relativeTo);
                radius *= relativeTo.XamlRoot.RasterizationScale;
            }
            var frames = (int)(duration.TotalSeconds * TouchesPerSecond);
            if (frames < 3)
                frames = 3;
            var finger1 = new List<Windows.Foundation.Point>(frames + 1);
            var finger2 = new List<Windows.Foundation.Point>(frames + 1);
            angle = angle / 180 * Math.PI / frames;
            for (int i = 0; i <= frames; i++)
            {
                var dx = Math.Cos(angle  * i) * radius;
                var dy = Math.Sin(angle  * i) * radius;
                finger1.Add(new Windows.Foundation.Point(center.X + dx, center.Y + dy));
                finger2.Add(new Windows.Foundation.Point(center.X - dx, center.Y - dy));
            }

            return DragAsync(new IEnumerable<Windows.Foundation.Point>[] { finger1, finger2 }, duration, null);
        }

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