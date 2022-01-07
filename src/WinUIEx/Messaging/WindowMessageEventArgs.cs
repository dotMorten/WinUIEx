using System;

namespace WinUIEx.Messaging
{
    /// <summary>
    /// Event arguments for the <see cref="WindowMessageMonitor.WindowMessageReceived"/> event.
    /// </summary>
    public sealed class WindowMessageEventArgs : EventArgs
    {
        internal WindowMessageEventArgs(IntPtr hwnd, uint messageId, nuint wParam, nint lParam)
        {
            Message = new Message(hwnd, messageId, wParam, lParam);
        }

        /// <summary>
        /// The result after processing the message. Set this to a non-zero value to prevent further processing.
        /// </summary>
        public nint Result { get; set; }

        /// <summary>
        /// The Windows WM Message
        /// </summary>
        public Message Message { get; }

        internal WindowsMessages MessageType => (WindowsMessages)Message.MessageId;
    }
}
