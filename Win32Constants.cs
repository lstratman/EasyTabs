namespace Stratman.Windows.Forms.TitleBarTabs
{
    /// <summary>
    /// Collection of Win32 constants we use for interop purposes.
    /// </summary>
    internal static class Win32Constants
    {
        // ReSharper disable InconsistentNaming
        /// <summary>
        /// Hit test succeeded outside the control or on a transparent area.
        /// </summary>
        public const int HTNOWHERE = 0;

        /// <summary>
        /// Hit test succeeded in the middle background segment.
        /// </summary>
        public const int HTCLIENT = 1;

        /// <summary>
        /// Hit test succeeded in the top, top left, or top right background segments.
        /// </summary>
        public const int HTCAPTION = 2;

        /// <summary>
        /// In a Control menu or in a Close button in a child window.
        /// </summary>
        public const int HTSYSMENU = 3;

        /// <summary>
        /// In a size box.
        /// </summary>
        public const int HTGROWBOX = 4;

        /// <summary>
        /// In a size box.
        /// </summary>
        public const int HTSIZE = HTGROWBOX;

        /// <summary>
        /// In a menu area.
        /// </summary>
        public const int HTMENU = 5;

        /// <summary>
        /// In the horizontal scroll bar.
        /// </summary>
        public const int HTHSCROLL = 6;

        /// <summary>
        /// In the vertical scroll bar.
        /// </summary>
        public const int HTVSCROLL = 7;

        /// <summary>
        ///  In a Minimize button.
        /// </summary>
        public const int HTMINBUTTON = 8;

        /// <summary>
        /// In a Maximize button.
        /// </summary>
        public const int HTMAXBUTTON = 9;

        /// <summary>
        ///  In the left border of the window.
        /// </summary>
        public const int HTLEFT = 10;

        /// <summary>
        /// In the right border of the window.
        /// </summary>
        public const int HTRIGHT = 11;

        /// <summary>
        /// In the upper horizontal border of the window.
        /// </summary>
        public const int HTTOP = 12;

        /// <summary>
        /// In the upper-left corner of the window border.
        /// </summary>
        public const int HTTOPLEFT = 13;

        /// <summary>
        /// In the upper-right corner of the window border.
        /// </summary>
        public const int HTTOPRIGHT = 14;

        /// <summary>
        /// In the lower horizontal border of the window.
        /// </summary>
        public const int HTBOTTOM = 15;

        /// <summary>
        /// In the lower-left corner of the window border.
        /// </summary>
        public const int HTBOTTOMLEFT = 16;

        /// <summary>
        /// In the lower-right corner of the window border.
        /// </summary>
        public const int HTBOTTOMRIGHT = 17;

        /// <summary>
        /// In the border of a window that does not have a sizing border.
        /// </summary>
        public const int HTBORDER = 18;

        /// <summary>
        ///  In a Minimize button.
        /// </summary>
        public const int HTREDUCE = HTMINBUTTON;

        /// <summary>
        /// In a Maximize button.
        /// </summary>
        public const int HTZOOM = HTMAXBUTTON;

        /// <summary>
        ///  In the left border of the window.
        /// </summary>
        public const int HTSIZEFIRST = HTLEFT;

        /// <summary>
        /// In the lower-right corner of the window border.
        /// </summary>
        public const int HTSIZELAST = HTBOTTOMRIGHT;

        /// <summary>
        /// In a Close button.
        /// </summary>
        public const int HTCLOSE = 20;

        /// <summary>
        /// Source bitmap is placed over the destination bitmap based on the alpha values of the source pixels.
        /// </summary>
        public const byte AC_SRC_OVER = 0x00;

        /// <summary>
        /// The source surface can be assumed to be in a pre-multiplied alpha 32bpp "BGRA" format; that is, the surface 
        /// type is BMF_32BPP and the palette type is BI_RGB. The alpha component is an integer in the range of 
        /// [0,255], where 0 is completely transparent and 255 is completely opaque.
        /// </summary>
        public const byte AC_SRC_ALPHA = 0x01;

        /// <summary>
        /// Use pblend as the blend function. If the display mode is 256 colors or less, the effect of this value is 
        /// the same as the effect of ULW_OPAQUE.
        /// </summary>
        public const int ULW_ALPHA = 0x00000002;

        /// <summary>
        /// Retrieves the window styles.
        /// </summary>
        public static int GWL_STYLE = -16;

        /// <summary>
        /// Retrieves the extended window styles.
        /// </summary>
        public static int GWL_EXSTYLE = -20;

        /// <summary>
        /// All descendants of a window get bottom-to-top painting order using double-buffering. Bottom-to-top painting
        /// order allows a descendent window to have translucency (alpha) and transparency (color-key) effects.
        /// </summary>
        public static int WS_EX_COMPOSITED = 0x02000000;

        /// <summary>
        /// Specifies that a window created with this style is to be transparent. That is, any windows that are beneath 
        /// the window are not obscured by the window. A window created with this style receives WM_PAINT messages only 
        /// after all sibling windows beneath it have been updated.
        /// </summary>
        public static int WS_EX_TRANSPARENT = 0x20;

        /// <summary>
        /// The window is a layered window. Note that this cannot be used for child windows. Also, this cannot be used 
        /// if the window has a class style of either CS_OWNDC or CS_CLASSDC.
        /// </summary>
        public static int WS_EX_LAYERED = 0x00080000;

    	public static int WS_EX_NOACTIVATE = 0x08000000;

    	public static int WS_EX_TOOLWINDOW = 0x00000080;
		public static int WS_CHILD = 0x40000000;
    	// ReSharper restore InconsistentNaming
    }
}