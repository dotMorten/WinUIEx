#if EXPERIMENTAL
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Graphics.Gdi;

namespace WinUIEx
{
    public class WindowMessageMonitor : IDisposable
    {
        IntPtr handle;
        private bool disposedValue;
        private string? WindowId = "WinUIEx_" + Guid.NewGuid();

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowMessageMonitor"/> class.
        /// </summary>
        /// <param name="window">Window</param>
        public WindowMessageMonitor(Microsoft.UI.Xaml.Window window) : this(window.GetWindowHandle())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowMessageMonitor"/> class.
        /// </summary>
        /// <param name="handle">Window handle</param>
        public WindowMessageMonitor(IntPtr handle)
        {
            CreateMessageWindow(handle);
        }

        /// <summary>
        /// Creates the helper message window that is used
        /// to receive messages from the taskbar icon.
        /// </summary>
        private unsafe void CreateMessageWindow(IntPtr handle)
        {

            //register window message handler
            // Create a simple window class which is reference through
            WNDCLASSW wc = new WNDCLASSW();
            wc.style = 0;
            wc.lpfnWndProc = OnWindowMessageReceived;
            wc.cbClsExtra = 0;
            wc.cbWndExtra = 0;
            wc.hInstance = new HINSTANCE(IntPtr.Zero);
            wc.hIcon = new HICON(IntPtr.Zero);
            wc.hCursor = new HCURSOR(IntPtr.Zero);
            wc.hbrBackground = new HBRUSH(IntPtr.Zero);
            fixed (char* id = string.Empty)
                wc.lpszMenuName = id;
            fixed (char* id = WindowId)
                wc.lpszClassName = id;

            // Register the window class
            PInvoke.RegisterClass(wc);

            // Get the message used to indicate the taskbar has been restarted
            // This is used to re-add icons when the taskbar restarts
            //TODO taskbarRestartMessageId = PInvoke.RegisterWindowMessage("TaskbarCreated");

            //var hwnd = new HWND();
            var hwnd = new HWND(handle);
            //var hwnd = PInvoke.GetActiveWindow();
            // Create the message window
            MessageWindowHandle = PInvoke.CreateWindowEx(0, WindowId, "", 0, 0, 0, 1, 1, hwnd, null, null, null);

            if (MessageWindowHandle == IntPtr.Zero)
            {
                throw new Win32Exception("Message window handle was not a valid pointer");
            }
        }

        /// <summary>
        /// Callback method that receives messages from the taskbar area.
        /// </summary>
        private LRESULT OnWindowMessageReceived(HWND hWnd, uint messageId, WPARAM wParam, LPARAM lParam)
        {
            /*
            if (messageId == taskbarRestartMessageId)
            {
                //recreate the icon if the taskbar was restarted (e.g. due to Win Explorer shutdown)
                var listener = TaskbarCreated;
                listener?.Invoke();
            }*/

            //forward message
            ProcessWindowMessage(messageId, wParam, lParam);
            return PInvoke.DefWindowProc(hWnd, messageId, wParam, lParam);
        }

        /// <summary>
        /// Processes incoming system messages.
        /// </summary>
        /// <param name="msg">Callback ID.</param>
        /// <param name="wParam">If the version is Vista
        /// or higher, this parameter can be used to resolve mouse coordinates.
        /// Currently not in use.</param>
        /// <param name="lParam">Provides information about the event.</param>
        private void ProcessWindowMessage(uint msg, WPARAM wParam, LPARAM lParam)
        {
            //if (msg != CallbackMessageId) return;
            WindowMessageRecieved?.Invoke(this, new WindowMessageEventArgs() { Message = msg, WParam = wParam.Value, LParam = lParam.Value });
            var message = (WindowsMessages)lParam.Value;
            Debug.WriteLine($"Got message '{(WindowsMessages)message}','{wParam.Value}' from message id {msg}");
            switch (message)
            {
                case WindowsMessages.WM_CONTEXTMENU:
                    // TODO: Handle WM_CONTEXTMENU, see https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyiconw
                    Debug.WriteLine("Unhandled WM_CONTEXTMENU");
                    break;
/*
                case WindowsMessages.WM_MOUSEMOVE:
                    MouseEventReceived?.Invoke(MouseEvent.MouseMove);
                    break;

                case WindowsMessages.WM_LBUTTONDOWN:
                    MouseEventReceived?.Invoke(MouseEvent.IconLeftMouseDown);
                    break;

                case WindowsMessages.WM_LBUTTONUP:
                    if (!isDoubleClick)
                    {
                        MouseEventReceived?.Invoke(MouseEvent.IconLeftMouseUp);
                    }
                    isDoubleClick = false;
                    break;

                case WindowsMessages.WM_LBUTTONDBLCLK:
                    isDoubleClick = true;
                    MouseEventReceived?.Invoke(MouseEvent.IconDoubleClick);
                    break;

                case WindowsMessages.WM_RBUTTONDOWN:
                    MouseEventReceived?.Invoke(MouseEvent.IconRightMouseDown);
                    break;

                case WindowsMessages.WM_RBUTTONUP:
                    MouseEventReceived?.Invoke(MouseEvent.IconRightMouseUp);
                    break;

                case WindowsMessages.WM_RBUTTONDBLCLK:
                    //double click with right mouse button - do not trigger event
                    break;

                case WindowsMessages.WM_MBUTTONDOWN:
                    MouseEventReceived?.Invoke(MouseEvent.IconMiddleMouseDown);
                    break;

                case WindowsMessages.WM_MBUTTONUP:
                    MouseEventReceived?.Invoke(MouseEvent.IconMiddleMouseUp);
                    break;

                case WindowsMessages.WM_MBUTTONDBLCLK:
                    //double click with middle mouse button - do not trigger event
                    break;

                case WindowsMessages.NIN_BALLOONSHOW:
                    BalloonToolTipChanged?.Invoke(true);
                    break;

                case WindowsMessages.NIN_BALLOONHIDE:
                case WindowsMessages.NIN_BALLOONTIMEOUT:
                    BalloonToolTipChanged?.Invoke(false);
                    break;

                case WindowsMessages.NIN_BALLOONUSERCLICK:
                    MouseEventReceived?.Invoke(MouseEvent.BalloonToolTipClicked);
                    break;

                case WindowsMessages.NIN_POPUPOPEN:
                    ChangeToolTipStateRequest?.Invoke(true);
                    break;

                case WindowsMessages.NIN_POPUPCLOSE:
                    ChangeToolTipStateRequest?.Invoke(false);
                    break;

                case WindowsMessages.NIN_SELECT:
                    // TODO: Handle NIN_SELECT see https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyiconw
                    Debug.WriteLine("Unhandled NIN_SELECT");
                    break;

                case WindowsMessages.NIN_KEYSELECT:
                    // TODO: Handle NIN_KEYSELECT see https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyiconw
                    Debug.WriteLine("Unhandled NIN_KEYSELECT");
                    break;

                default:
                    Debug.WriteLine("Unhandled NotifyIcon message ID: " + lParam);
                    break;*/
            }
        }

        /// <summary>
        /// Handle for the message window.
        /// </summary> 
        internal HWND MessageWindowHandle { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~WindowEventMonitor()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public event EventHandler<WindowMessageEventArgs>? WindowMessageRecieved;
    }

    public class WindowMessageEventArgs : EventArgs
    {
        public uint Message { get; internal set; }
        public nuint WParam { get; internal set; }
        public nint LParam { get; internal set; }

        public string MessageType
        {
            get
            {
                return ((WindowsMessages)Message).ToString();
            }
        }
    }
}
#endif