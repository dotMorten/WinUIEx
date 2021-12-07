#if EXPERIMENTAL
using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using WinRT;

namespace WinUIEx.TestTools
{
    internal static class CaptureHelper
    {
        static readonly Guid GraphicsCaptureItemGuid = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760");

        [ComImport]
        [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        interface IInitializeWithWindow
        {
            void Initialize(
                IntPtr hwnd);
        }

        [ComImport]
        [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        interface IGraphicsCaptureItemInterop
        {
            IntPtr CreateForWindow(
                [In] IntPtr window,
                [In] ref Guid iid);

            IntPtr CreateForMonitor(
                [In] IntPtr monitor,
                [In] ref Guid iid);
        }

        public static void SetWindow(this GraphicsCapturePicker picker, IntPtr hwnd)
        {
            var interop = (IInitializeWithWindow)(object)picker;
            interop.Initialize(hwnd);
        }

        public static GraphicsCaptureItem CreateItemForWindow(IntPtr hwnd)
        {
            var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
            var itemPointer = interop.CreateForWindow(hwnd, GraphicsCaptureItemGuid);
            var item = MarshalInterface<GraphicsCaptureItem>.FromAbi(itemPointer);
            Marshal.Release(itemPointer);
            return item;
        }
       
        public static GraphicsCaptureItem CreateItemForMonitor(IntPtr hmon)
        {
            var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
            var itemPointer = interop.CreateForMonitor(hmon, GraphicsCaptureItemGuid);
            var item = MarshalInterface<GraphicsCaptureItem>.FromAbi(itemPointer);
            Marshal.Release(itemPointer);
            return item;
        }
    }
}
#endif