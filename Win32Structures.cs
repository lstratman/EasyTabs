using System;
using System.Runtime.InteropServices;

namespace Stratman.Windows.Forms.TitleBarTabs
{
  [StructLayout(LayoutKind.Sequential)]
  internal struct MARGINS
  {
    public int cxLeftWidth;
    public int cxRightWidth;
    public int cyTopHeight;
    public int cyBottomHeight;
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct RECT
  {
    public int left;
    public int top;
    public int right;
    public int bottom;
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct NCCALCSIZE_PARAMS
  {
    public RECT rgrc0;
    public RECT rgrc1;
    public RECT rgrc2;
    public IntPtr lppos;
  }
}
