using System;
using System.Runtime.InteropServices;

namespace Stratman.Windows.Forms.TitleBarTabs
{
	// ReSharper disable InconsistentNaming
	/// <summary>
	/// Returned by the GetThemeMargins function to define the margins of windows that have visual styles applied.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	internal struct MARGINS
	{
		/// <summary>
		/// Width of the left border that retains its size.
		/// </summary>
		public int cxLeftWidth;

		/// <summary>
		/// Width of the right border that retains its size.
		/// </summary>
		public int cxRightWidth;

		/// <summary>
		/// Height of the top border that retains its size.
		/// </summary>
		public int cyTopHeight;

		/// <summary>
		/// Height of the bottom border that retains its size.
		/// </summary>
		public int cyBottomHeight;
	}

	/// <summary>
	/// The RECT structure defines the coordinates of the upper-left and lower-right corners of a rectangle.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	internal struct RECT
	{
		/// <summary>
		/// The x-coordinate of the upper-left corner of the rectangle.
		/// </summary>
		public int left;

		/// <summary>
		/// The y-coordinate of the upper-left corner of the rectangle.
		/// </summary>
		public int top;

		/// <summary>
		/// The x-coordinate of the lower-right corner of the rectangle.
		/// </summary>
		public int right;

		/// <summary>
		/// The y-coordinate of the lower-right corner of the rectangle.
		/// </summary>
		public int bottom;
	}

	/// <summary>
	/// Contains information that an application can use while processing the WM_NCCALCSIZE message to calculate the size, position, and valid contents of the 
	/// client area of a window.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	internal struct NCCALCSIZE_PARAMS
	{
		/// <summary>
		/// When the window procedure receives the WM_NCCALCSIZE message, this contains the new coordinates of a window that has been moved or resized, that 
		/// is, it is the proposed new window coordinates. When the window procedure returns, the first rectangle contains the coordinates of the new client 
		/// rectangle resulting from the move or resize.
		/// </summary>
		public RECT rcNewWindow;

		/// <summary>
		/// When the window procedure receives the WM_NCCALCSIZE message, this contains the coordinates of the window before it was moved or resized.  When the 
		/// window procedure returns, this contains the valid destination rectangle.
		/// </summary>
		public RECT rcOldWindow;

		/// <summary>
		/// When the window procedure receives the WM_NCCALCSIZE message, this contains the coordinates of the window's client area before the window was moved 
		/// or resized.  When the window procedure returns, this contains the valid source rectangle.
		/// </summary>
		public RECT rcClient;

		/// <summary>
		/// A pointer to a WINDOWPOS structure that contains the size and position values specified in the operation that moved or resized the window.
		/// </summary>
		// ReSharper disable FieldCanBeMadeReadOnly.Local
		private IntPtr lppos;
		// ReSharper restore FieldCanBeMadeReadOnly.Local
	}

	/// <summary>
	/// The POINT structure defines the x- and y-coordinates of a point.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct POINT
	{
		/// <summary>
		/// Specifies the x-coordinate of a point.
		/// </summary>
		public Int32 x;

		/// <summary>
		/// Specifies the y-coordinate of a point.
		/// </summary>
		public Int32 y;

		/// <summary>
		/// Constructor that initializes the structure.
		/// </summary>
		/// <param name="x">Specifies the x-coordinate of a point.</param>
		/// <param name="y">Specifies the y-coordinate of a point.</param>
		public POINT(Int32 x, Int32 y)
		{
			this.x = x;
			this.y = y;
		}
	}

	/// <summary>
	/// The SIZE structure specifies the width and height of a rectangle.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct SIZE
	{
		/// <summary>
		/// Specifies the rectangle's width. The units depend on which function uses this.
		/// </summary>
		public Int32 cx;

		/// <summary>
		/// Specifies the rectangle's height. The units depend on which function uses this.
		/// </summary>
		public Int32 cy;

		/// <summary>
		/// Constructor that initializes the structure.
		/// </summary>
		/// <param name="cx">Specifies the rectangle's width. The units depend on which function uses this.</param>
		/// <param name="cy">Specifies the rectangle's height. The units depend on which function uses this.</param>
		public SIZE(Int32 cx, Int32 cy)
		{
			this.cx = cx;
			this.cy = cy;
		}
	}

	/// <summary>
	/// The BLENDFUNCTION structure controls blending by specifying the blending functions for source and destination bitmaps.
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct BLENDFUNCTION
	{
		/// <summary>
		/// The source blend operation. Currently, the only source and destination blend operation that has been defined is AC_SRC_OVER.
		/// </summary>
		public byte BlendOp;

		/// <summary>
		/// Must be zero.
		/// </summary>
		public byte BlendFlags;

		/// <summary>
		/// Specifies an alpha transparency value to be used on the entire source bitmap. The SourceConstantAlpha value is combined with any per-pixel alpha 
		/// values in the source bitmap. If you set SourceConstantAlpha to 0, it is assumed that your image is transparent. Set the SourceConstantAlpha value 
		/// to 255 (opaque) when you only want to use per-pixel alpha values.
		/// </summary>
		public byte SourceConstantAlpha;

		/// <summary>
		/// This member controls the way the source and destination bitmaps are interpreted.
		/// </summary>
		public byte AlphaFormat;
	}

	/// <summary>
	/// Specifies flags that modify window visual style attributes.
	/// </summary>
	[Flags]
	public enum WTNCA : uint
	{
		/// <summary>
		/// Prevents the window caption from being drawn.
		/// </summary>
		NODRAWCAPTION = 1,

		/// <summary>
		/// Prevents the system icon from being drawn.
		/// </summary>
		NODRAWICON = 2,

		/// <summary>
		/// Prevents the system icon menu from appearing.
		/// </summary>
		NOSYSMENU = 4,

		/// <summary>
		/// Prevents mirroring of the question mark, even in right-to-left (RTL) layout.
		/// </summary>
		NOMIRRORHELP = 8,

		/// <summary>
		/// A mask that contains all the valid bits.
		/// </summary>
		VALIDBITS = NODRAWCAPTION | NODRAWICON | NOSYSMENU | NOMIRRORHELP
	}

	/// <summary>
	/// Specifies the type of visual style attribute to set on a window.
	/// </summary>
	public enum WINDOWTHEMEATTRIBUTETYPE : uint
	{
		/// <summary>
		/// Non-client area window attributes will be set.
		/// </summary>
		WTA_NONCLIENT = 1,
	}

	/// <summary>
	/// Defines options that are used to set window visual style attributes.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct WTA_OPTIONS
	{
		/// <summary>
		/// A combination of flags that modify window visual style attributes. Can be a combination of the WTNCA constants.
		/// </summary>
		public WTNCA dwFlags;

		/// <summary>
		/// A bitmask that describes how the values specified in dwFlags should be applied. If the bit corresponding to a value in dwFlags is 0, that flag will 
		/// be removed. If the bit is 1, the flag will be added.
		/// </summary>
		public WTNCA dwMask;
	}

	/// <summary>
	/// Contains information about a low-level mouse input event.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct MSLLHOOKSTRUCT
	{
		/// <summary>
		/// The x- and y-coordinates of the cursor, in screen coordinates.
		/// </summary>
		public POINT pt;

		/// <summary>
		/// If the message is WM_MOUSEWHEEL, the high-order word of this member is the wheel delta. The low-order word is reserved. A positive value indicates 
		/// that the wheel was rotated forward, away from the user; a negative value indicates that the wheel was rotated backward, toward the user. One wheel 
		/// click is defined as WHEEL_DELTA, which is 120. If the message is WM_XBUTTONDOWN, WM_XBUTTONUP, WM_XBUTTONDBLCLK, WM_NCXBUTTONDOWN, WM_NCXBUTTONUP, 
		/// or WM_NCXBUTTONDBLCLK, the high-order word specifies which X button was pressed or released, and the low-order word is reserved.
		/// </summary>
		public uint mouseData;

		/// <summary>
		/// The event-injected flag. An application can use the following value to test the mouse flags.
		/// </summary>
		public uint flags;

		/// <summary>
		/// The time stamp for this message.
		/// </summary>
		public uint time;

		/// <summary>
		/// Additional information associated with the message.
		/// </summary>
		public IntPtr dwExtraInfo;
	}
	// ReSharper restore InconsistentNaming
}