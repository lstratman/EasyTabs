using System;
using System.Runtime.InteropServices;

namespace Stratman.Windows.Forms.TitleBarTabs
{
  internal static class Win32Interop
  {
    #region "User32 Functions"
    [DllImport("user32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AdjustWindowRectEx(
      out RECT lpRect,
      int dwStyle,
      bool bMenu,
      int dwExStyle);

    [DllImport("user32", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(
      IntPtr hWnd, 
      IntPtr hWndInsertAfter, 
      int x, int y, 
      int cx, int cy, 
      uint uFlags);

    [DllImport("user32.dll")]
    public static extern IntPtr SetCapture(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();
    #endregion

    #region "GDI32 Functions"
    [DllImport("gdi32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteDC(IntPtr hdc);
    #endregion

    #region "UxTheme Functions"
    [DllImport("uxtheme", CharSet=CharSet.Unicode, SetLastError=true)]
    public static extern IntPtr OpenThemeData(IntPtr hWnd, string pszClassList);

    [DllImport("uxtheme", SetLastError = true)]
    public static extern IntPtr CloseThemeData(IntPtr hTheme);

    [DllImport("uxtheme", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int DrawThemeText(
      IntPtr hTheme, 
      IntPtr hdc, 
      int iPartId, 
      int iStateId, 
      string pszText, 
      int iCharCount, 
      uint dwTextFlags, 
      uint dwTextFlags2, 
      ref RECT pRect);

    #endregion

    #region "DWM Functions"
    [DllImport("dwmapi", SetLastError = true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    public static extern bool DwmDefWindowProc(
      IntPtr hWnd, 
      int msg, 
      IntPtr wParam, 
      IntPtr lParam, 
      out IntPtr plResult);

    [DllImport("dwmapi", SetLastError = true)]
    public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);
    #endregion    
  }  
}
