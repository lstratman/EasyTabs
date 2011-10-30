using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stratman.Windows.Forms.TitleBarTabs
{
  internal static class Win32Messages
  {
    public const int WM_CREATE = 0x0001;

    public const int WM_ACTIVATE = 0x0006;

    public const int WM_PAINT = 0x000F;

    public const int WM_NCCALCSIZE = 0x0083;
    public const int WM_NCHITTEST = 0x0084;
    public const int WM_NCPAINT = 0x0085;
      public const int WM_SYSCOMMAND = 0x0112;
      public const int WM_LBUTTONUP = 0x0202;
      public const int WM_LBUTTONDOWN = 0x0201;
      public const int WM_NCLBUTTONUP = 0x00A2;
      public const int WM_NCLBUTTONDOWN = 0x00A1;
  }
}
