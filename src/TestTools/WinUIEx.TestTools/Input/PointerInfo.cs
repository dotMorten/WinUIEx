using System;
using Windows.Win32.UI.Input.Pointer;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Foundation;

namespace WinUIEx.TestTools.Input
{
    /// <summary>Contains basic pointer information common to all pointer types. Applications can retrieve this information using the GetPointerInfo, GetPointerFrameInfo, GetPointerInfoHistory and GetPointerFrameInfoHistory functions.</summary>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-pointer_info">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    internal struct PointerInfo
    {
        internal POINTER_INFO ToNative()
        {
            return new POINTER_INFO()
            {
                ButtonChangeType = (POINTER_BUTTON_CHANGE_TYPE)ButtonChangeType,
                dwKeyStates = 0,
                dwTime = 0,
                frameId = FrameId,
                historyCount = 0,
                //hwndTarget = new HWND(Hwnd),
                InputData = 0,
                PerformanceCount = 0,
                pointerFlags = (POINTER_FLAGS)PointerFlags,
                pointerId = PointerId,
                pointerType = (POINTER_INPUT_TYPE)PointerType,
                ptHimetricLocation = new POINT() { x = 0, y = 0 },
                ptHimetricLocationRaw = new POINT() { y = 0, x = 0 },
                ptPixelLocation = new POINT() { y = (int)PixelLocation.Y, x = (int)PixelLocation.X },
                //ptPixelLocationRaw = new POINT() { y = (int)PixelLocation.Y, x = (int)PixelLocation.X },
                //sourceDevice = new HANDLE()
            };
        }

        /// <summary>
        /// <para>Type: <b>UINT32</b> An identifier that uniquely identifies a pointer during its lifetime. A pointer comes into existence when it is first detected and ends its existence when it goes out of detection range. Note that if a physical entity (finger or pen) goes out of detection range and then returns to be detected again, it is treated as a new pointer and may be assigned a new pointer identifier.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-pointer_info#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        public uint PointerId { get; set; }

        /// <summary>
        /// <para>Type: <b>UINT32</b> An identifier common to multiple pointers for which the source device reported an update in a single input frame. For example, a parallel-mode multi-touch digitizer may report the positions of multiple touch contacts in a single update to the system. Note that frame identifier is assigned as input is reported to the system for all pointers across all devices. Therefore, this field may not contain strictly sequential values in a single series of messages that a window receives. However, this field will contain the same numerical value for all input updates that were reported in the same input frame by a single device.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-pointer_info#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        public uint FrameId { get; set; }

        /// <summary>
        /// <para><b>HWND</b> Window to which this message was targeted. If the pointer is captured, either implicitly by virtue of having made contact over this window or explicitly
        /// using the pointer capture API, this is the capture window. If the pointer is uncaptured, this is the window over which the pointer was when this message was generated.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-pointer_info#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        public IntPtr Hwnd { get; set; }

        public Windows.Foundation.Point PixelLocation { get; set; }

        /// <summary>
        /// <para>Type: <b><a href="https://docs.microsoft.com/previous-versions/windows/desktop/inputmsg/pointer-flags-contants">POINTER_FLAGS</a></b> May be any reasonable combination of flags from the <a href="https://docs.microsoft.com/previous-versions/windows/desktop/inputmsg/pointer-flags-contants">Pointer Flags</a> constants.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-pointer_info#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        public PointerFlag PointerFlags { get; set; }

        /// <summary>
        /// <para>A value from the <a href="https://docs.microsoft.com/windows/win32/api/winuser/ne-winuser-tagpointer_input_type">POINTER_INPUT_TYPE</a> enumeration that specifies the pointer type.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-pointer_info#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        public PointerInputType PointerType { get; set; }

        /// <summary>
        /// <para>A value from the <a href="https://docs.microsoft.com/windows/desktop/api/winuser/ne-winuser-pointer_button_change_type">POINTER_BUTTON_CHANGE_TYPE</a> enumeration that specifies the change in button state between this input and the previous input.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-pointer_info#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        public PointerButtonChangeType ButtonChangeType { get; set; }
    }

    /// <summary>
    /// Identifies the pointer input types
    /// </summary>
    internal enum PointerInputType
    {
        /// <summary>
        /// Generic pointer type. This type never appears in pointer messages or pointer data. Some data query functions allow the caller to restrict the query to specific pointer type. The PT_POINTER type can be used in these functions to specify that the query is to include pointers of all types
        /// </summary>
        Pointer = POINTER_INPUT_TYPE.PT_POINTER,
        /// <summary>
        /// Touch pointer type.
        /// </summary>
        Touch = POINTER_INPUT_TYPE.PT_TOUCH,
        /// <summary>
        /// Pen pointer type.
        /// </summary>
        Pen = POINTER_INPUT_TYPE.PT_PEN,
        /// <summary>
        /// Mouse pointer type.
        /// </summary>
        Mouse = POINTER_INPUT_TYPE.PT_MOUSE,
        /// <summary>
        /// Touchpad pointer type (Windows 8.1 and later).
        /// </summary>
        TouchPad = POINTER_INPUT_TYPE.PT_TOUCHPAD
    }

    /// <summary>
    /// Values that can appear in the <see cref="PointerInfo.PointerFlags"/> field of the <see cref="PointerInfo"/> structure.
    /// </summary>
    [Flags]
    internal enum PointerFlag : uint
    {
        /// <summary>
        /// Default.
        /// </summary>
        None = POINTER_FLAGS.POINTER_FLAG_NONE,
        /// <summary>
        /// Indicates the arrival of a new pointer.
        /// </summary>
        New = POINTER_FLAGS.POINTER_FLAG_NEW,
        /// <summary>
        /// Indicates that this pointer continues to exist. When this flag is not set, it indicates the pointer has left detection range.
        /// This flag is typically not set only when a hovering pointer leaves detection range (<see cref="PointerFlag.Update"/> is set) or when a pointer in contact with a window surface leaves detection range (<see cref="PointerFlag.Up"/> is set).
        /// </summary>
        InRange = POINTER_FLAGS.POINTER_FLAG_INRANGE,
        /// <summary>
        /// Indicates that this pointer is in contact with the digitizer surface. When this flag is not set, it indicates a hovering pointer.
        /// </summary>
        InContact = POINTER_FLAGS.POINTER_FLAG_INCONTACT,
        /// <summary>
        /// <para>Indicates a primary action, analogous to a left mouse button down.</para>
        /// <para>A touch pointer has this flag set when it is in contact with the digitizer surface.</para>
        /// <para>A pen pointer has this flag set when it is in contact with the digitizer surface with no buttons pressed.</para>
        /// <para>A mouse pointer has this flag set when the left mouse button is down.</para>
        /// </summary>
        FirstButton = POINTER_FLAGS.POINTER_FLAG_FIRSTBUTTON,
        SecondButton = POINTER_FLAGS.POINTER_FLAG_SECONDBUTTON,
        ThirdButton = POINTER_FLAGS.POINTER_FLAG_THIRDBUTTON,
        FourthButton = POINTER_FLAGS.POINTER_FLAG_FOURTHBUTTON,
        FifthButton = POINTER_FLAGS.POINTER_FLAG_FIFTHBUTTON,
        /// <summary>
        /// <para>Indicates that this pointer has been designated as the primary pointer. A primary pointer is a single pointer that can perform actions beyond those available to non-primary pointers.
        /// For example, when a primary pointer makes contact with a window s surface, it may provide the window an opportunity to activate by sending it a WM_POINTERACTIVATE message.</para>
        /// <para>The primary pointer is identified from all current user interactions on the system (mouse, touch, pen, and so on). As such, the primary pointer might not be associated with your app.
        /// The first contact in a multi-touch interaction is set as the primary pointer. Once a primary pointer is identified, all contacts must be lifted before a new contact can be identified as a 
        /// primary pointer. For apps that don't process pointer input, only the primary pointer's events are promoted to mouse events.</para>
        /// </summary>
        Primary = POINTER_FLAGS.POINTER_FLAG_PRIMARY,
        /// <summary>
        /// Confidence is a suggestion from the source device about whether the pointer represents an intended or accidental interaction, which is especially relevant for <see cref="PointerInputType.Touch"/> 
        /// pointers where an accidental interaction (such as with the palm of the hand) can trigger input. The presence of this flag indicates that the source device has high confidence that this input is 
        /// part of an intended interaction.
        /// </summary>
        Confidence = POINTER_FLAGS.POINTER_FLAG_CONFIDENCE,
        /// <summary>
        /// Indicates that the pointer is departing in an abnormal manner, such as when the system receives invalid input for the pointer or when a device with active pointers departs
        /// abruptly. If the application receiving the input is in a position to do so, it should treat the interaction as not completed and reverse any effects of the concerned pointer.
        /// </summary>
        Canceled = POINTER_FLAGS.POINTER_FLAG_CANCELED,
        /// <summary>
        /// Indicates that this pointer transitioned to a down state; that is, it made contact with the digitizer surface.
        /// </summary>
        Down = POINTER_FLAGS.POINTER_FLAG_DOWN,
        /// <summary>
        /// Indicates that this is a simple update that does not include pointer state changes.
        /// </summary>
        Update = POINTER_FLAGS.POINTER_FLAG_UPDATE,
        /// <summary>
        /// Indicates that this pointer transitioned to an up state; that is, contact with the digitizer surface ended.
        /// </summary>
        Up = POINTER_FLAGS.POINTER_FLAG_UP,
        /// <summary>
        /// Indicates input associated with a pointer wheel. For mouse pointers, this is equivalent to the action of the mouse scroll wheel
        /// </summary>
        Wheel = POINTER_FLAGS.POINTER_FLAG_WHEEL,
        /// <summary>
        /// Indicates input associated with a pointer h-wheel. For mouse pointers, this is equivalent to the action of the mouse horizontal scroll wheel
        /// </summary>
        HWheel = POINTER_FLAGS.POINTER_FLAG_HWHEEL,
        /// <summary>
        /// Indicates that this pointer was captured by (associated with) another element and the original element has lost capture 
        /// </summary>
        CaptureChanged = POINTER_FLAGS.POINTER_FLAG_CAPTURECHANGED,
        /// <summary>
        /// Indicates that this pointer has an associated transform.
        /// </summary>
        HasTranform = POINTER_FLAGS.POINTER_FLAG_HASTRANSFORM
    }

    /// <summary>Identifies a change in the state of a button associated with a pointer.</summary>
    /// <remarks>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ne-winuser-pointer_button_change_type">Learn more about this API from docs.microsoft.com</see>.</para>
    /// </remarks>
    internal enum PointerButtonChangeType
    {
        /// <summary>No change in button state.</summary>
        None = POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_NONE,
        /// <summary>The first button (see <a href="https://docs.microsoft.com/previous-versions/windows/desktop/inputmsg/pointer-flags-contants">POINTER_FLAG_FIRSTBUTTON</a>) transitioned to a pressed state.</summary>
        FirstButtonDown = POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_FIRSTBUTTON_DOWN,
        /// <summary>The first button (see <a href="https://docs.microsoft.com/previous-versions/windows/desktop/inputmsg/pointer-flags-contants">POINTER_FLAG_FIRSTBUTTON</a>) transitioned to a released state.</summary>
        FirstButtonUp = POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_FIRSTBUTTON_UP,
        /// <summary>The second button (see <a href="https://docs.microsoft.com/previous-versions/windows/desktop/inputmsg/pointer-flags-contants">POINTER_FLAG_SECONDBUTTON</a>) transitioned to a pressed state.</summary>
        SecondButtonDown = POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_SECONDBUTTON_DOWN,
        /// <summary>The second button (see <a href="https://docs.microsoft.com/previous-versions/windows/desktop/inputmsg/pointer-flags-contants">POINTER_FLAG_SECONDBUTTON</a>) transitioned to a released state.</summary>
        SecondButtonUp = POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_SECONDBUTTON_UP,
        /// <summary>The third button (see <a href="https://docs.microsoft.com/previous-versions/windows/desktop/inputmsg/pointer-flags-contants">POINTER_FLAG_THIRDBUTTON</a>) transitioned to a pressed state.</summary>
        ThirdButtonDown = POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_THIRDBUTTON_DOWN,
        /// <summary>The third button (see <a href="https://docs.microsoft.com/previous-versions/windows/desktop/inputmsg/pointer-flags-contants">POINTER_FLAG_THIRDBUTTON</a>) transitioned to a released state.</summary>
        ThirdButtonUp = POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_THIRDBUTTON_UP,
        /// <summary>The fourth button (see <a href="https://docs.microsoft.com/previous-versions/windows/desktop/inputmsg/pointer-flags-contants">POINTER_FLAG_FOURTHBUTTON</a>) transitioned to a pressed state.</summary>
        FourthButtonDown = POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_FOURTHBUTTON_DOWN,
        /// <summary>The fourth button (see <a href="https://docs.microsoft.com/previous-versions/windows/desktop/inputmsg/pointer-flags-contants">POINTER_FLAG_FOURTHBUTTON</a>) transitioned to a released state.</summary>
        FourthButtonUp = POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_FOURTHBUTTON_UP,
        /// <summary>The fifth button (see <a href="https://docs.microsoft.com/previous-versions/windows/desktop/inputmsg/pointer-flags-contants">POINTER_FLAG_FIFTHBUTTON</a>) transitioned to a pressed state.</summary>
        FifthButtonDown = POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_FIFTHBUTTON_DOWN,
        /// <summary>The fifth button (see <a href="https://docs.microsoft.com/previous-versions/windows/desktop/inputmsg/pointer-flags-contants">POINTER_FLAG_FIFTHBUTTON</a>) transitioned to a released state.</summary>
        FifthButtonUp = POINTER_BUTTON_CHANGE_TYPE.POINTER_CHANGE_FIFTHBUTTON_UP
    }
}