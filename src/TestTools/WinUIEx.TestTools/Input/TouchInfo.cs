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
        internal Windows.Win32.UI.Input.Pointer.POINTER_TOUCH_INFO ToNative()
        {
            var pi = PointerInfo.ToNative();
            return new Windows.Win32.UI.Input.Pointer.POINTER_TOUCH_INFO()
            {
                orientation = 0,
                pointerInfo = pi,
                pressure = 0,
                rcContact = new Windows.Win32.Foundation.RECT() { left = pi.ptPixelLocation.x, right = (int)pi.ptPixelLocation.x, top = pi.ptPixelLocation.y, bottom = (int)pi.ptPixelLocation.y },
                //rcContactRaw = new Windows.Win32.Foundation.RECT() { left = pi.ptPixelLocation.x, right = (int)pi.ptPixelLocation.x, top = pi.ptPixelLocation.y, bottom = (int)pi.ptPixelLocation.y },
                touchFlags = 0,
                touchMask = TOUCH_MASK_CONTACTAREA
            };
        }
        public PointerInfo PointerInfo { get; set; }
        private const uint TOUCH_MASK_NONE = 0x00000000;
        private const uint TOUCH_MASK_CONTACTAREA = 0x00000001;
        private const uint TOUCH_MASK_ORIENTATION = 0x00000002;
        private const uint TOUCH_MASK_PRESSURE = 0x00000004;
    }
}
