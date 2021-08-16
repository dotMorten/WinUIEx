using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinUIEx
{
    /// <summary>
    /// Creates and manages a tray icon in the task bar
    /// </summary>
    public class TrayIcon : IDisposable
    {
        private WindowMessageSink messageSink;
        private NOTIFYICONDATAW32 iconData32;
        private NOTIFYICONDATAW64 iconData64;
        private object lockObject = new object();
        private Icon _icon;

        /// <summary>
        /// Initializes a new instance of the tray icon
        /// </summary>
        public TrayIcon(Icon icon)
        {
            messageSink = new WindowMessageSink();
            if (Environment.Is64BitProcess)
            {
                iconData64 = new NOTIFYICONDATAW64()
                {
                    cbSize = (uint)Marshal.SizeOf(iconData64),
                    hWnd = messageSink.MessageWindowHandle,
                    uCallbackMessage = WindowMessageSink.CallbackMessageId,
                    hIcon = new HICON(IntPtr.Zero),
                    dwState = 0x01, //Hidden
                    dwStateMask = 0x01, // hidden
                    Anonymous = new NOTIFYICONDATAW64._Anonymous_e__Union() { uVersion = 0x00 },
                };
            }
            else
            {
                iconData32 = new NOTIFYICONDATAW32()
                {
                    cbSize = (uint)Marshal.SizeOf(iconData32),
                    hWnd = messageSink.MessageWindowHandle,
                    uCallbackMessage = WindowMessageSink.CallbackMessageId,
                    hIcon = new HICON(IntPtr.Zero),
                    dwState = 0x01, //Hidden
                    dwStateMask = 0x01, // hidden
                    Anonymous = new NOTIFYICONDATAW32._Anonymous_e__Union() { uVersion = 0x00 },
                };
            }

            CreateTaskbarIcon(); 
            messageSink.MouseEventReceived += OnMouseEvent;
            messageSink.TaskbarCreated += OnTaskbarCreated;
            messageSink.ChangeToolTipStateRequest += OnToolTipChange;
            messageSink.BalloonToolTipChanged += OnBalloonToolTipChanged;

            SetIcon(icon);
        }

        /// <summary>
        /// Updates the icon in the tray
        /// </summary>
        /// <param name="icon"></param>
        [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(_icon))]
        public unsafe void SetIcon(Icon icon)
        {
            _icon = icon ?? throw new ArgumentNullException(nameof(_icon)) ; // pin to avoid GC
            if (Environment.Is64BitProcess)
            {
                iconData64.uFlags = (uint)IconDataMembers.Icon;
                iconData64.hIcon = icon.Handle;
                var status = PInvoke.Shell_NotifyIcon((uint)NotifyCommand.Modify, iconData64);
            }
            else
            {
                iconData32.uFlags = (uint)IconDataMembers.Icon;
                iconData32.hIcon = icon.Handle;
                var status = PInvoke.Shell_NotifyIcon((uint)NotifyCommand.Modify, iconData32);
            }
        }

        private void OnBalloonToolTipChanged(bool obj)
        {
        }

        private void OnToolTipChange(bool obj)
        {
        }

        private void OnMouseEvent(MouseEvent e)
        {
            if (IsDisposed) return;
            switch(e)
            {
                case MouseEvent.IconLeftMouseDown:
                    TrayIconLeftMouseDown?.Invoke(this, EventArgs.Empty);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Fired when the user left-clicks the tray icon
        /// </summary>
        public event EventHandler? TrayIconLeftMouseDown;

        private void OnTaskbarCreated()
        {
            IsTaskbarIconCreated = false;
            CreateTaskbarIcon();
        }

        private void CreateTaskbarIcon()
        {
            lock (lockObject)
            {
                if (IsTaskbarIconCreated)
                {
                    return;
                }

                const IconDataMembers members = IconDataMembers.Message
                                                | IconDataMembers.Icon
                                                | IconDataMembers.Tip;

                //write initial configuration
                bool status;
                if (Environment.Is64BitProcess)
                {
                    iconData64.uFlags = (uint)members;
                    status = PInvoke.Shell_NotifyIcon((uint)NotifyCommand.Add, iconData64);
                }
                else
                {
                    iconData32.uFlags = (uint)members;
                    status = PInvoke.Shell_NotifyIcon((uint)NotifyCommand.Add, iconData32);
                }
                if (!status)
                {
                    // couldn't create the icon - we can assume this is because explorer is not running (yet!)
                    // -> try a bit later again rather than throwing an exception. Typically, if the windows
                    // shell is being loaded later, this method is being re-invoked from OnTaskbarCreated
                    // (we could also retry after a delay, but that's currently YAGNI)
                    return;
                }

                //set to most recent version
                // SetVersion();
                // messageSink.Version = (NotifyIconVersion)iconData.VersionOrTimeout;

                IsTaskbarIconCreated = true;
            }
        }

        private bool IsTaskbarIconCreated;

        private void RemoveTaskbarIcon()
        {
            lock (lockObject)
            {
                // make sure we didn't schedule a creation

                if (!IsTaskbarIconCreated)
                {
                    return;
                }
                if (Environment.Is64BitProcess)
                {
                    iconData64.dwInfoFlags = (uint)IconDataMembers.Message;
                    PInvoke.Shell_NotifyIcon((uint)NotifyCommand.Delete, iconData64);
                }
                else
                {
                    iconData32.dwInfoFlags = (uint)IconDataMembers.Message;
                    PInvoke.Shell_NotifyIcon((uint)NotifyCommand.Delete, iconData32);
                }
                IsTaskbarIconCreated = false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the this instance has been disposed
        /// </summary>
        public bool IsDisposed { get; private set; }

        private void EnsureNotDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);
        }

        /// <inheritdoc />
        ~TrayIcon()
        {
            Dispose(false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);

            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SuppressFinalize to
            // take this object off the finalization queue 
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            // don't do anything if the component is already disposed
            if (IsDisposed) return;

            if (disposing)
            {

                lock (lockObject)
                {
                    IsDisposed = true;
                    // dispose message sink
                    messageSink.Dispose();

                    _icon.Dispose();

                    // stop timers
                    // singleClickTimer.Dispose();
                    // balloonCloseTimer.Dispose();

                }
            }
            // remove icon
            RemoveTaskbarIcon();
        }
    }
}
