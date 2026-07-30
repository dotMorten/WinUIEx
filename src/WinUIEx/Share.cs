using System;

using Windows.ApplicationModel.DataTransfer;

namespace WinUIEx
{
    // https://learn.microsoft.com/en-us/windows/apps/develop/windows-integration/integrate-sharesheet-send#implement-share-for-desktop-apps-winui-3-wpf-winforms

    [System.Runtime.InteropServices.ComImport]
    [System.Runtime.InteropServices.Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
    [System.Runtime.InteropServices.InterfaceType(
        System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    interface IDataTransferManagerInterop
    {
        IntPtr GetForWindow([System.Runtime.InteropServices.In] IntPtr appWindow,
            [System.Runtime.InteropServices.In] ref Guid riid);
        void ShowShareUIForWindow(IntPtr appWindow);
    }

    public partial class WindowEx
    {
        private DataTransferManager? _dtm;

        // Call this from your window or form constructor (or load handler):
        private void InitializeShare()
        {
            // Retrieve the window handle (HWND) for the current window:
            //   WinUI 3:  IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            //   WPF:      IntPtr hWnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            //   WinForms: IntPtr hWnd = this.Handle;
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            IDataTransferManagerInterop interop =
                DataTransferManager.As<IDataTransferManagerInterop>();

            // IID of DataTransferManager, passed as the riid to GetForWindow:
            Guid dtmIid = new(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);
            _dtm = WinRT.MarshalInterface<DataTransferManager>.FromAbi(interop.GetForWindow(hWnd, dtmIid));
        }

        /// <summary>
        /// Displays the Windows Share UI for this window and supplies the specified
        /// <see cref="DataPackage"/> when the user completes the share operation.
        /// </summary>
        /// <param name="data">
        /// The data package to share. This is provided to the Share UI when the
        /// <c>DataRequested</c> event is raised.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="InitializeShare"/> has not been called before invoking this method.
        /// </exception>
        public void Share(DataPackage data)
        {
            if (_dtm is null) 
            {
                throw new InvalidOperationException("Share has not been initialized. Call InitializeShare() first.");
            }

            void handler(DataTransferManager sender, DataRequestedEventArgs args)
            {
                args.Request.Data = data;
                if (_dtm is not null) {
                    _dtm.DataRequested -= handler;
                }
            }

            _dtm.DataRequested += handler;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var interop = DataTransferManager.As<IDataTransferManagerInterop>();
            interop.ShowShareUIForWindow(hWnd);
        }
    }
}