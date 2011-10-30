using System.Runtime.InteropServices;

namespace Stratman.Windows.Forms.TitleBarTabs
{
    // ReSharper disable InconsistentNaming
    /// <summary>
    ///   Returned by the GetThemeMargins function to define the margins of windows that have visual styles applied.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MARGINS
    {
        /// <summary>
        ///   Width of the left border that retains its size.
        /// </summary>
        public int cxLeftWidth;

        /// <summary>
        ///   Width of the right border that retains its size.
        /// </summary>
        public int cxRightWidth;

        /// <summary>
        ///   Height of the top border that retains its size.
        /// </summary>
        public int cyTopHeight;

        /// <summary>
        ///   Height of the bottom border that retains its size.
        /// </summary>
        public int cyBottomHeight;
    }

    /// <summary>
    ///   The RECT structure defines the coordinates of the upper-left and lower-right corners of a rectangle.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        /// <summary>
        ///   The x-coordinate of the upper-left corner of the rectangle.
        /// </summary>
        public int left;

        /// <summary>
        ///   The y-coordinate of the upper-left corner of the rectangle.
        /// </summary>
        public int top;

        /// <summary>
        ///   The x-coordinate of the lower-right corner of the rectangle.
        /// </summary>
        public int right;

        /// <summary>
        ///   The y-coordinate of the lower-right corner of the rectangle.
        /// </summary>
        public int bottom;
    }
    // ReSharper restore InconsistentNaming
}