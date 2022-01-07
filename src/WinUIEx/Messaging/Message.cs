using System;
using System.Runtime.InteropServices;

namespace WinUIEx.Messaging
{
    /// <summary>
    /// Represents a Windows Message.
    /// </summary>
    /// <remarks>
    /// Refer to Windows MSG structure documentation for more info: <a href="https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-msg" />.
    /// </remarks>
    public struct Message
    {
        internal Message(IntPtr hwnd, uint messageId, nuint wParam, IntPtr lParam)
        {
            Hwnd = hwnd;
            MessageId = messageId;
            WParam = wParam;
            LParam = lParam;
        }

        /// <summary>
        /// Gets the window handle of the message.
        /// </summary>
        /// <remarks>Window handle is a value that uniquely identifies a window on the system. This property returns a handle of the window whose window procedure receives this message. It is useful when your code need to interact with some native Windows API functions that expect window handles as parameters.</remarks>
        public IntPtr Hwnd { get; private set; }

        /// <summary>
        /// Gets the ID number for the message.
        /// </summary>
        public uint MessageId { get; private set; }

        /// <summary>
        /// Gets or sets the WParam field of the message.
        /// </summary>
        public nuint WParam { get; private set; }

        /// <summary>
        /// Specifies the LParam field of the message.
        /// </summary>
        public nint LParam { get; private set; }

        internal int LowOrder => unchecked((short)LParam);

        internal int HighOrder => unchecked((short)((long)LParam >> 16));

        /// <inheritdoc />
        public override string ToString()
        {
            switch ((WindowsMessages)MessageId)
            {
                case WindowsMessages.WM_SIZING:
                    string side = "";
                    switch (WParam)
                    {
                        case 1: side = "Left"; break;
                        case 2: side = "Right"; break;
                        case 3: side = "Top"; break;
                        case 4: side = "Top-Left"; break;
                        case 5: side = "Top-Right"; break;
                        case 6: side = "Bottom"; break;
                        case 7: side = "Bottom-Left"; break;
                        case 8: side = "Bottom-Right"; break;
                        default: side = WParam.ToString(); break;
                    }
                    var rect = Marshal.PtrToStructure<Windows.Win32.Foundation.RECT>((IntPtr)LParam);

                    return $"WM_SIZING: Side: {side} Rect: {rect.left},{rect.top},{rect.right},{rect.bottom}";
                default:
                    break;
            }
            return $"{(WindowsMessages)MessageId}: LParam={LParam} WParam={WParam}";
        }
    }
}
