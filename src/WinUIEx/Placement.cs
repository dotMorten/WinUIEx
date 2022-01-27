using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUIEx
{
    /// <summary>
    /// The edge positions MoveTo can move to
    /// </summary>
    /// <seealso cref="HwndExtensions.MoveTo" />
    /// <seealso cref="WindowExtensions.MoveTo" />
    public enum Placement
    {
        /// <summary>
        /// The top far left corner of the screen.
        /// </summary>
        TopLeft,
        /// <summary>
        /// The top of the screen and half way across the width.
        /// </summary>
        TopCenter,
        /// <summary>
        /// The top far right corner of the screen.
        /// </summary>
        TopRight,
        /// <summary>
        /// The far left side of the screen, half way down the height
        /// </summary>
        MiddleLeft,
        /// <summary>
        /// The middle of the screen.
        /// </summary>
        MiddleCenter,
        /// <summary>
        /// The far right side of screen, half way down the height.
        /// </summary>
        MiddleRight,
        /// <summary>
        /// The bottom far left corner of the screen.
        /// </summary>
        BottomLeft,
        /// <summary>
        /// The bottom of the screen, and half way across the width.
        /// </summary>
        BottomCenter,
        /// <summary>
        /// The bottom far right corner of the screen.
        /// </summary>
        BottomRight
    }
}
