using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUIEx.TestTools.Input
{
    /// <summary>Defines basic touch information common to all pointer types.</summary>
    internal class TouchInfo
    {
        private const uint TOUCH_MASK_NONE = 0x00000000;
        private const uint TOUCH_MASK_CONTACTAREA = 0x00000001;
        private const uint TOUCH_MASK_ORIENTATION = 0x00000002;
        private const uint TOUCH_MASK_PRESSURE = 0x00000004;

        internal static TouchInfo CreateDown(Windows.Foundation.Point tapLocation, Windows.Win32.Foundation.HWND hwnd)
        {
            return Create(tapLocation, hwnd, PointerFlag.Down | PointerFlag.InRange | PointerFlag.InContact);
        }
        internal static TouchInfo CreateUp(Windows.Foundation.Point tapLocation, Windows.Win32.Foundation.HWND hwnd)
        {
            return Create(tapLocation, hwnd, PointerFlag.Up);
        }
        internal static TouchInfo Create(Windows.Foundation.Point tapLocation, Windows.Win32.Foundation.HWND hwnd, PointerFlag flags)
        {
            var pDown = new PointerInfo()
            {
                PointerId = 1,
                PointerType = PointerInputType.Touch,
                PointerFlags = flags,
                PixelLocation = tapLocation,
                Hwnd = hwnd
            };
            return new TouchInfo() { PointerInfo = pDown };
        }

        internal Windows.Win32.UI.Input.Pointer.POINTER_TOUCH_INFO ToNative()
        {
            var pi = PointerInfo.ToNative();
            var mask = TOUCH_MASK_CONTACTAREA;
            if (Pressure > 0)
                mask += TOUCH_MASK_PRESSURE;
            if (Orientation.HasValue)
                mask += TOUCH_MASK_ORIENTATION;
            return new Windows.Win32.UI.Input.Pointer.POINTER_TOUCH_INFO()
            {
                orientation = Orientation.HasValue ? Orientation.Value : 0,
                pointerInfo = pi,
                pressure = Pressure,
                rcContact = new Windows.Win32.Foundation.RECT() { left = pi.ptPixelLocation.x, right = (int)pi.ptPixelLocation.x, top = pi.ptPixelLocation.y, bottom = (int)pi.ptPixelLocation.y },
                //rcContactRaw = new Windows.Win32.Foundation.RECT() { left = pi.ptPixelLocation.x, right = (int)pi.ptPixelLocation.x, top = pi.ptPixelLocation.y, bottom = (int)pi.ptPixelLocation.y },
                touchFlags = 0,
                touchMask = mask
            };
        }

        public uint Pressure { get; set; }

        public uint? Orientation { get; set; }

        public PointerInfo PointerInfo { get; set; }
    }
}
