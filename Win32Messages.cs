namespace Stratman.Windows.Forms.TitleBarTabs
{
    /// <summary>
    /// A series of Win32 messaging constants that we use for interop.
    /// </summary>
    internal static class Win32Messages
    {
        // ReSharper disable InconsistentNaming
        /// <summary>
        /// Sent when an application requests that a window be created by calling the CreateWindowEx or CreateWindow 
        /// function. (The message is sent before the function returns.) The window procedure of the new window 
        /// receives this message after the window is created, but before the window becomes visible.
        /// </summary>
        public const int WM_CREATE = 0x0001;

        /// <summary>
        /// Sent to both the window being activated and the window being deactivated. If the windows use the same 
        /// input queue, the message is sent synchronously, first to the window procedure of the top-level window 
        /// being deactivated, then to the window procedure of the top-level window being activated. If the windows 
        /// use different input queues, the message is sent asynchronously, so the window is activated immediately.
        /// </summary>
        public const int WM_ACTIVATE = 0x0006;

        /// <summary>
        /// The WM_PAINT message is sent when the system or another application makes a request to paint a portion of 
        /// an application's window.
        /// </summary>
        public const int WM_PAINT = 0x000F;

        /// <summary>
        /// Sent when the size and position of a window's client area must be calculated. By processing this message, 
        /// an application can control the content of the window's client area when the size or position of the 
        /// window changes.
        /// </summary>
        public const int WM_NCCALCSIZE = 0x0083;

        /// <summary>
        /// Sent to a window in order to determine what part of the window corresponds to a particular screen 
        /// coordinate. This can happen, for example, when the cursor moves, when a mouse button is pressed or 
        /// released, or in response to a call to a function such as WindowFromPoint. If the mouse is not captured, 
        /// the message is sent to the window beneath the cursor. Otherwise, the message is sent to the window that 
        /// has captured the mouse.
        /// </summary>
        public const int WM_NCHITTEST = 0x0084;

        /// <summary>
        /// The WM_NCPAINT message is sent to a window when its frame must be painted.
        /// </summary>
        public const int WM_NCPAINT = 0x0085;

        /// <summary>
        /// A window receives this message when the user chooses a command from the Window menu (formerly known as 
        /// the system or control menu) or when the user chooses the maximize button, minimize button, restore 
        /// button, or close button.
        /// </summary>
        public const int WM_SYSCOMMAND = 0x0112;

        /// <summary>
        /// Posted when the user releases the left mouse button while the cursor is in the client area of a window. 
        /// If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the 
        /// message is posted to the window that has captured the mouse.
        /// </summary>
        public const int WM_LBUTTONUP = 0x0202;

        /// <summary>
        /// Posted when the user presses the left mouse button while the cursor is in the client area of a window. 
        /// If the mouse is not captured, the message is posted to the window beneath the cursor. Otherwise, the 
        /// message is posted to the window that has captured the mouse.
        /// </summary>
        public const int WM_LBUTTONDOWN = 0x0201;

        /// <summary>
        /// Posted when the user releases the left mouse button while the cursor is within the non-client area of a 
        /// window. This message is posted to the window that contains the cursor. If a window has captured the 
        /// mouse, this message is not posted.
        /// </summary>
        public const int WM_NCLBUTTONUP = 0x00A2;

        /// <summary>
        /// Posted when the user presses the left mouse button while the cursor is within the non-client area of a 
        /// window. This message is posted to the window that contains the cursor. If a window has captured the 
        /// mouse, this message is not posted.
        /// </summary>
        public const int WM_NCLBUTTONDOWN = 0x00A1;
        // ReSharper restore InconsistentNaming
    }
}