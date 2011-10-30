using System;
using System.Runtime.InteropServices;

namespace Stratman.Windows.Forms.TitleBarTabs
{
    /// <summary>
    ///   Win32 interop functions necessary to interact with low-level DWM and windowing functionality.
    /// </summary>
    internal static class Win32Interop
    {
        /// <summary>
        ///   Calculates the required size of the window rectangle, based on the desired size of the client rectangle. 
        ///   The window rectangle can then be passed to the CreateWindowEx function to create a window whose client 
        ///   area is the desired size.
        /// </summary>
        /// <param name = "lpRect">A pointer to a RECT structure that contains the coordinates of the top-left and 
        ///   bottom-right corners of the desired client area. When the function returns, the structure contains the 
        ///   coordinates of the top-left and bottom-right corners of the window to accommodate the desired client 
        ///   area.</param>
        /// <param name = "dwStyle">The window style of the window whose required size is to be calculated. Note that 
        ///   you cannot specify the WS_OVERLAPPED style.</param>
        /// <param name = "bMenu">Indicates whether the window has a menu.</param>
        /// <param name = "dwExStyle">The extended window style of the window whose required size is to be 
        ///   calculated.</param>
        /// <returns>If the function succeeds, the return value is nonzero.  If the function fails, the return value is 
        ///   zero.</returns>
        [DllImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AdjustWindowRectEx(out RECT lpRect, int dwStyle, bool bMenu, int dwExStyle);

        /// <summary>
        ///   Retrieves information about the specified window. The function also retrieves the 32-bit (DWORD) value at 
        ///   the specified offset into the extra window memory.
        /// </summary>
        /// <param name = "hWnd">A handle to the window and, indirectly, the class to which the window belongs.</param>
        /// <param name = "nIndex">The zero-based offset to the value to be retrieved. Valid values are in the range 
        ///   zero through the number of bytes of extra window memory, minus four; for example, if you specified 12
        ///   or more bytes of extra memory, a value of 8 would be an index to the third 32-bit integer.</param>
        /// <returns>If the function succeeds, the return value is the requested value.  If the function fails, the 
        ///   return value is zero.</returns>
        [DllImport("user32", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        /// <summary>
        ///   Sets the mouse capture to the specified window belonging to the current thread.SetCapture captures mouse 
        ///   input either when the mouse is over the capturing window, or when the mouse button was pressed while the 
        ///   mouse was over the capturing window and the button is still down. Only one window at a time can capture 
        ///   the mouse.  If the mouse cursor is over a window created by another thread, the system will direct mouse 
        ///   input to the specified window only if a mouse button is down.
        /// </summary>
        /// <param name = "hWnd">A handle to the window in the current thread that is to capture the mouse.</param>
        /// <returns>The return value is a handle to the window that had previously captured the mouse. If there is no 
        ///   such window, the return value is NULL.</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr SetCapture(IntPtr hWnd);

        /// <summary>
        ///   Releases the mouse capture from a window in the current thread and restores normal mouse input processing. 
        ///   A window that has captured the mouse receives all mouse input, regardless of the position of the cursor, 
        ///   except when a mouse button is clicked while the cursor hot spot is in the window of another thread.
        /// </summary>
        /// <returns>If the function succeeds, the return value is nonzero.  If the function fails, the return value is 
        ///   zero.</returns>
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        /// <summary>
        ///   Default window procedure for Desktop Window Manager (DWM) hit testing within the non-client area.
        /// </summary>
        /// <param name = "hWnd">A handle to the window procedure that received the message.</param>
        /// <param name = "msg">The message.</param>
        /// <param name = "wParam">Specifies additional message information. The content of this parameter depends on 
        ///   the value of the <see cref = "msg" /> parameter.</param>
        /// <param name = "lParam">Specifies additional message information. The content of this parameter depends on 
        ///   the value of the <see cref = "msg" /> parameter.</param>
        /// <param name = "plResult">A pointer to an LRESULT value that, when this method returns successfully, 
        ///   receives the result of the hit test.</param>
        /// <returns>TRUE if DwmDefWindowProc handled the message; otherwise, FALSE.</returns>
        [DllImport("dwmapi", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DwmDefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam,
                                                   out IntPtr plResult);

        /// <summary>
        ///   Extends the window frame into the client area.
        /// </summary>
        /// <param name = "hWnd">The handle to the window in which the frame will be extended into the client area.</param>
        /// <param name = "pMarInset">A pointer to a MARGINS structure that describes the margins to use when extending 
        ///   the frame into the client area.</param>
        /// <returns>If this function succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.</returns>
        [DllImport("dwmapi", SetLastError = true)]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);
    }
}