using System;
using System.Runtime.InteropServices;
using Microsoft.Windows.Sdk;

namespace WinUIEx
{
    /// <summary>
    /// Creates and manages a tray icon in the task bar
    /// </summary>
    public class TrayIcon : IDisposable
    {
        private WindowMessageSink messageSink;
        private NOTIFYICONDATAW iconData;
        private object lockObject = new object();

        /// <summary>
        /// Initializes a new instance of the tray icon
        /// </summary>
        public TrayIcon(Icon icon)
        {
            messageSink = new WindowMessageSink();
            iconData = new NOTIFYICONDATAW()
            {
                cbSize = (uint)Marshal.SizeOf(iconData),
                hWnd = messageSink.MessageWindowHandle,
                uCallbackMessage = WindowMessageSink.CallbackMessageId,
                hIcon = new HICON(IntPtr.Zero),
                dwState = 0x01, //Hidden
                dwStateMask = 0x01, // hidden
                Anonymous = new NOTIFYICONDATAW._Anonymous_e__Union() { uVersion = 0x00 },
            };
            
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
        public unsafe void SetIcon(Icon icon)
        {
            iconData.uFlags = (uint)IconDataMembers.Icon;
            iconData.hIcon = new HICON(icon.DangerousGetHandle());
            var status = PInvoke.Shell_NotifyIcon((uint)NotifyCommand.Modify, iconData);
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

                iconData.uFlags = (uint)members;
                //write initial configuration
                var status = PInvoke.Shell_NotifyIcon((uint)NotifyCommand.Add, iconData);
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
                iconData.dwInfoFlags = (uint)IconDataMembers.Message;
                PInvoke.Shell_NotifyIcon((uint)NotifyCommand.Delete, iconData);
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
            if (IsDisposed || !disposing) return;

            lock (lockObject)
            {
                IsDisposed = true;

                // de-register application event listener
                /*
                if (Application.Current != null)
                {
                    Application.Current.Exit -= OnExit;
                }*/

                // stop timers
                // singleClickTimer.Dispose();
                // balloonCloseTimer.Dispose();

                // dispose message sink
                messageSink.Dispose();

                // remove icon
                RemoveTaskbarIcon();
            }
        }
    }
}
