using System;
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

    internal struct NCCALCSIZE_PARAMS
    {
        public RECT rcNewWindow;
        public RECT rcOldWindow;
        public RECT rcClient;
        IntPtr lppos;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public Int32 x;
        public Int32 y;

        public POINT(Int32 x, Int32 y) { this.x = x; this.y = y; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SIZE
    {
        public Int32 cx;
        public Int32 cy;

        public SIZE(Int32 cx, Int32 cy) { this.cx = cx; this.cy = cy; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BLENDFUNCTION
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }
    // ReSharper restore InconsistentNaming
}