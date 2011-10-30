using System;

namespace Stratman.Windows.Forms.TitleBarTabs
{
  internal static class Win32Constants
  {
    public static int GWL_STYLE = -16;
    public static int GWL_EXSTYLE = -20;
    
    public static IntPtr HWND_TOP = new IntPtr(0);
    public static IntPtr HWND_BOTTOM = new IntPtr(1);
    public static IntPtr HWND_TOPMOST = new IntPtr(-1);
    public static IntPtr HWND_NOTOPMOST = new IntPtr(-2);

    public const int SWP_NOSIZE = 0x0001;
    public const int SWP_NOMOVE = 0x0002;
    public const int SWP_NOZORDER = 0x0004;
    public const int SWP_FRAMECHANGED = 0x0020;

    public const int HTNOWHERE = 0;
    public const int HTCLIENT = 1;
    public const int HTCAPTION = 2;
    public const int HTSYSMENU = 3;
    public const int HTGROWBOX = 4;
    public const int HTSIZE = HTGROWBOX;
    public const int HTMENU = 5;
    public const int HTHSCROLL = 6;
    public const int HTVSCROLL = 7;
    public const int HTMINBUTTON = 8;
    public const int HTMAXBUTTON = 9;
    public const int HTLEFT = 10;
    public const int HTRIGHT = 11;
    public const int HTTOP = 12;
    public const int HTTOPLEFT = 13;
    public const int HTTOPRIGHT = 14;
    public const int HTBOTTOM = 15;
    public const int HTBOTTOMLEFT = 16;
    public const int HTBOTTOMRIGHT = 17;
    public const int HTBORDER = 18;
    public const int HTREDUCE = HTMINBUTTON;
    public const int HTZOOM = HTMAXBUTTON;
    public const int HTSIZEFIRST = HTLEFT;
    public const int HTSIZELAST = HTBOTTOMRIGHT;
    public const int HTCLOSE = 20;
  }
}
