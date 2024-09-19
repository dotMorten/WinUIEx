using System;
using System.Runtime.InteropServices;

namespace WinUIEx.Messaging
{
    /// <summary>
    /// The message monitor allows you to monitor all WM_MESSAGE events for a given window.
    /// </summary>
    public sealed class WindowMessageMonitor : IDisposable
    {
        private GCHandle? _monitorGCHandle;
        private IntPtr _hwnd = IntPtr.Zero;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Initialize a new instance of the <see cref="WindowMessageMonitor"/> class.
        /// </summary>
        /// <param name="window">The window to listen to messages for</param>
        public WindowMessageMonitor(Microsoft.UI.Xaml.Window window) : this(window.GetWindowHandle())
        {
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="WindowMessageMonitor"/> class.
        /// </summary>
        /// <param name="hwnd">The window handle to listen to messages for</param>
        public WindowMessageMonitor(IntPtr hwnd)
        {
            _hwnd = hwnd;
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~WindowMessageMonitor()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes this instance
        /// </summary>
        public void Dispose() => Dispose(true);

        private void Dispose(bool disposing)
        { 
            if (_NativeMessage != null)
                RemoveWindowSubclass();
        }

        private event EventHandler<WindowMessageEventArgs>? _NativeMessage;

        /// <summary>
        /// Event raised when a windows message is received.
        /// </summary>
        public event EventHandler<WindowMessageEventArgs> WindowMessageReceived
        {
            add
            {
                if (_NativeMessage is null)
                {
                    SetWindowSubclass();
                }
                _NativeMessage += value;
            }
            remove
            {
                _NativeMessage -= value;
                if (_NativeMessage is null)
                {
                    RemoveWindowSubclass();
                }
            }
        }

        [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })]
        private static Windows.Win32.Foundation.LRESULT NewWindowProc(Windows.Win32.Foundation.HWND hWnd, uint uMsg, Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
        {
            var handle = GCHandle.FromIntPtr((IntPtr)(nint)dwRefData);
            if (handle.IsAllocated && handle.Target is WindowMessageMonitor monitor)
            {
                var handler = monitor._NativeMessage;
                if (handler != null)
                {
                    var args = new WindowMessageEventArgs(hWnd, uMsg, wParam.Value, lParam);
                    handler.Invoke(monitor, args);
                    if (args.Handled)
                        return new Windows.Win32.Foundation.LRESULT((int)args.Result);
                }
            }
            return Windows.Win32.PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }
        
        private unsafe void SetWindowSubclass()
        {
            lock (_lockObject)
                if (!_monitorGCHandle.HasValue)
                {
                    _monitorGCHandle = GCHandle.Alloc(this);
                    bool ok = Windows.Win32.PInvoke.SetWindowSubclass(new Windows.Win32.Foundation.HWND(_hwnd), &NewWindowProc, 101, (nuint)GCHandle.ToIntPtr(_monitorGCHandle.Value).ToPointer());
                }
        }

        private unsafe void RemoveWindowSubclass()
        {
            lock (_lockObject)
                if (_monitorGCHandle.HasValue)
                {
                    Windows.Win32.PInvoke.RemoveWindowSubclass(new Windows.Win32.Foundation.HWND(_hwnd), &NewWindowProc, 101);
                    _monitorGCHandle?.Free();
                    _monitorGCHandle = null;
                }
        }
    }
}
