using System;
using System.Runtime.InteropServices;

namespace Stratman.Windows.Forms.TitleBarTabs
{
    /// <summary>
    /// Win32 interop functions necessary to interact with low-level DWM and windowing functionality.
    /// </summary>
    internal static class Win32Interop
    {
        // ReSharper disable InconsistentNaming
        /// <summary>
        /// Calculates the required size of the window rectangle, based on the desired size of the client rectangle. 
        /// The window rectangle can then be passed to the CreateWindowEx function to create a window whose client 
        /// area is the desired size.
        /// </summary>
        /// <param name="lpRect">A pointer to a RECT structure that contains the coordinates of the top-left and 
        /// bottom-right corners of the desired client area. When the function returns, the structure contains the 
        /// coordinates of the top-left and bottom-right corners of the window to accommodate the desired client 
        /// area.</param>
        /// <param name="dwStyle">The window style of the window whose required size is to be calculated. Note that 
        /// you cannot specify the WS_OVERLAPPED style.</param>
        /// <param name="bMenu">Indicates whether the window has a menu.</param>
        /// <param name="dwExStyle">The extended window style of the window whose required size is to be 
        /// calculated.</param>
        /// <returns>If the function succeeds, the return value is nonzero.  If the function fails, the return value is 
        /// zero.</returns>
        [DllImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AdjustWindowRectEx(out RECT lpRect, int dwStyle, bool bMenu, int dwExStyle);

        /// <summary>
        /// Retrieves information about the specified window. The function also retrieves the 32-bit (DWORD) value at 
        /// the specified offset into the extra window memory.
        /// </summary>
        /// <param name="hWnd">A handle to the window and, indirectly, the class to which the window belongs.</param>
        /// <param name="nIndex">The zero-based offset to the value to be retrieved. Valid values are in the range 
        /// zero through the number of bytes of extra window memory, minus four; for example, if you specified 12
        /// or more bytes of extra memory, a value of 8 would be an index to the third 32-bit integer.</param>
        /// <returns>If the function succeeds, the return value is the requested value.  If the function fails, the 
        /// return value is zero.</returns>
        [DllImport("user32", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        /// <summary>
        /// Sets the mouse capture to the specified window belonging to the current thread.SetCapture captures mouse 
        /// input either when the mouse is over the capturing window, or when the mouse button was pressed while the 
        /// mouse was over the capturing window and the button is still down. Only one window at a time can capture 
        /// the mouse.  If the mouse cursor is over a window created by another thread, the system will direct mouse 
        /// input to the specified window only if a mouse button is down.
        /// </summary>
        /// <param name="hWnd">A handle to the window in the current thread that is to capture the mouse.</param>
        /// <returns>The return value is a handle to the window that had previously captured the mouse. If there is no 
        /// such window, the return value is NULL.</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr SetCapture(IntPtr hWnd);

        /// <summary>
        /// Releases the mouse capture from a window in the current thread and restores normal mouse input processing. 
        /// A window that has captured the mouse receives all mouse input, regardless of the position of the cursor, 
        /// except when a mouse button is clicked while the cursor hot spot is in the window of another thread.
        /// </summary>
        /// <returns>If the function succeeds, the return value is nonzero.  If the function fails, the return value is 
        /// zero.</returns>
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        /// <summary>
        /// Default window procedure for Desktop Window Manager (DWM) hit testing within the non-client area.
        /// </summary>
        /// <param name="hWnd">A handle to the window procedure that received the message.</param>
        /// <param name="msg">The message.</param>
        /// <param name="wParam">Specifies additional message information. The content of this parameter depends on 
        /// the value of the <see cref="msg" /> parameter.</param>
        /// <param name="lParam">Specifies additional message information. The content of this parameter depends on 
        /// the value of the <see cref="msg" /> parameter.</param>
        /// <param name="plResult">A pointer to an LRESULT value that, when this method returns successfully, 
        /// receives the result of the hit test.</param>
        /// <returns>TRUE if DwmDefWindowProc handled the message; otherwise, FALSE.</returns>
        [DllImport("dwmapi", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DwmDefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam,
                                                   out IntPtr plResult);

        /// <summary>
        /// Extends the window frame into the client area.
        /// </summary>
        /// <param name="hWnd">The handle to the window in which the frame will be extended into the client area.</param>
        /// <param name="pMarInset">A pointer to a MARGINS structure that describes the margins to use when extending 
        /// the frame into the client area.</param>
        /// <returns>If this function succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.</returns>
        [DllImport("dwmapi", SetLastError = true)]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        /// <summary>
        /// Retrieves the dimensions of the bounding rectangle of the specified window. The dimensions are given in 
        /// screen coordinates that are relative to the upper-left corner of the screen.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="lpRect">A pointer to a RECT structure that receives the screen coordinates of the upper-left 
        /// and lower-right corners of the window.</param>
        /// <returns>If the function succeeds, the return value is nonzero.  If the function fails, the return value is 
        /// zero. To get extended error information, call GetLastError.</returns>
        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// Retrieves a handle to the desktop window. The desktop window covers the entire screen. The desktop window 
        /// is the area on top of which other windows are painted.
        /// </summary>
        /// <returns>The return value is a handle to the desktop window.</returns>
        [DllImport("user32")]
        public static extern IntPtr GetDesktopWindow();

        /// <summary>
        /// Obtains a value that indicates whether Desktop Window Manager (DWM) composition is enabled. Applications 
        /// can listen for composition state changes by handling the WM_DWMCOMPOSITIONCHANGED notification.
        /// </summary>
        /// <param name="enabled">A pointer to a value that, when this function returns successfully, receives TRUE if 
        /// DWM composition is enabled; otherwise, FALSE.</param>
        /// <returns>If this function succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.</returns>
        [DllImport("dwmapi")]
        public static extern int DwmIsCompositionEnabled(out bool enabled);

        /// <summary>
        /// Updates the position, size, shape, content, and translucency of a layered window.
        /// </summary>
        /// <param name="hwnd">A handle to a layered window. A layered window is created by specifying WS_EX_LAYERED 
        /// when creating the window with the CreateWindowEx function.</param>
        /// <param name="hdcDst">A handle to a DC for the screen. This handle is obtained by specifying NULL when 
        /// calling the function. It is used for palette color matching when the window contents are updated. If hdcDst 
        /// isNULL, the default palette will be used.  If hdcSrc is NULL, hdcDst must be NULL.</param>
        /// <param name="pptDst">A pointer to a structure that specifies the new screen position of the layered window. 
        /// If the current position is not changing, pptDst can be NULL.</param>
        /// <param name="psize">A pointer to a structure that specifies the new size of the layered window. If the size 
        /// of the window is not changing, psize can be NULL. If hdcSrc is NULL, psize must be NULL.</param>
        /// <param name="hdcSrc">A handle to a DC for the surface that defines the layered window. This handle can be 
        /// obtained by calling the CreateCompatibleDC function. If the shape and visual context of the window are not 
        /// changing, hdcSrc can be NULL.</param>
        /// <param name="pprSrc">A pointer to a structure that specifies the location of the layer in the device 
        /// context. If hdcSrc is NULL, pptSrc should be NULL.</param>
        /// <param name="crKey">A structure that specifies the color key to be used when composing the layered window. 
        /// To generate a COLORREF, use the RGB macro.</param>
        /// <param name="pblend">A pointer to a structure that specifies the transparency value to be used when 
        /// composing the layered window.</param>
        /// <param name="dwFlags">Flags controlling how the window is blended with the underlying content.</param>
        /// <returns>If the function succeeds, the return value is nonzero.  If the function fails, the return value is 
        /// zero. To get extended error information, call GetLastError.</returns>
        [DllImport("user32", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize,
                                                      IntPtr hdcSrc, ref POINT pprSrc, Int32 crKey,
                                                      ref BLENDFUNCTION pblend, Int32 dwFlags);

        /// <summary>
        /// Selects an object into the specified device context (DC). The new object replaces the previous object of 
        /// the same type.
        /// </summary>
        /// <param name="hDC">A handle to the DC.</param>
        /// <param name="hObject">A handle to the object to be selected.</param>
        /// <returns>If the selected object is not a region and the function succeeds, the return value is a handle to 
        /// the object being replaced.</returns>
        [DllImport("gdi32", ExactSpelling = true)]
        public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        /// <summary>
        /// This function deletes a logical pen, brush, font, bitmap, region, or palette, freeing all system resources 
        /// associated with the object. After the object is deleted, the specified handle is no longer valid.
        /// </summary>
        /// <param name="hObject">Handle to a logical pen, brush, font, bitmap, region, or palette.</param>
        /// <returns>Nonzero indicates success.  Zero indicates that the specified handle is not valid or that the 
        /// handle is currently selected into a device context.  To get extended error information, call 
        /// GetLastError.</returns>
        [DllImport("gdi32", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);

        /// <summary>
        /// Retrieves a handle to a device context (DC) for the client area of a specified window or for the entire 
        /// screen. You can use the returned handle in subsequent GDI functions to draw in the DC. The device context 
        /// is an opaque data structure, whose values are used internally by GDI.
        /// </summary>
        /// <param name="hWnd">A handle to the window whose DC is to be retrieved. If this value is NULL, GetDC 
        /// retrieves the DC for the entire screen.</param>
        /// <returns>If the function succeeds, the return value is a handle to the DC for the specified window's client 
        /// area.  If the function fails, the return value is NULL.</returns>
        [DllImport("user32", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetDC(IntPtr hWnd);

        /// <summary>
        /// This function releases a device context (DC), freeing it for use by other applications. The effect of 
        /// ReleaseDC depends on the type of device context.
        /// </summary>
        /// <param name="hWnd">Handle to the window whose device context is to be released.</param>
        /// <param name="hDC">Handle to the device context to be released.</param>
        /// <returns>The return value specifies whether the device context is released.  1 indicates that the device 
        /// context is released.  Zero indicates that the device context is not released.</returns>
        [DllImport("user32", ExactSpelling = true)]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        /// <summary>
        /// Creates a memory device context (DC) compatible with the specified device.
        /// </summary>
        /// <param name="hDC">Handle to an existing device context.  If this handle is NULL, the function creates a 
        /// memory device context compatible with the application's current screen.</param>
        /// <returns>The handle to a memory device context indicates success.  NULL indicates failure.  To get extended 
        /// error information, call GetLastError.</returns>
        [DllImport("gdi32", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        /// <summary>
        /// This function deletes the specified device context (DC).
        /// </summary>
        /// <param name="hdc">Handle to the device context.</param>
        /// <returns>Nonzero indicates success.  Zero indicates failure.  To get extended error information, call 
        /// GetLastError.</returns>
        [DllImport("gdi32", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteDC(IntPtr hdc);
        // ReSharper restore InconsistentNaming
    }
}