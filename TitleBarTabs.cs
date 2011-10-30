using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace Stratman.Windows.Forms.TitleBarTabs
{
  public abstract partial class TitleBarTabs : Form
  {
    protected int BorderTop {get; private set;}
    protected int BorderLeft { get; private set; }
    protected int BorderRight {get; private set;}
    protected int BorderBottom {get; private set;}
      protected ListWithEvents<TitleBarTab> _tabs = new ListWithEvents<TitleBarTab>();
      protected TitleBarTab _clickedTab = null;
      protected FormWindowState? _previousWindowState = null;

      public delegate void TitleBarTabEventHandler(object sender, TitleBarTabEventArgs e);
      public delegate void TitleBarTabCancelEventHandler(object sender, TitleBarTabCancelEventArgs e);

      public event TitleBarTabCancelEventHandler TabDeselecting;
      public event TitleBarTabEventHandler TabDeselected;
      public event TitleBarTabCancelEventHandler TabSelecting;
      public event TitleBarTabEventHandler TabSelected;

      public abstract TitleBarTab CreateTab();

      public ListWithEvents<TitleBarTab> Tabs
      {
          get
          {
              return _tabs;
          }
      }

      public BaseTabRenderer TabRenderer
      {
          get;
          set;
      }

      public TitleBarTab SelectedTab
      {
          get
          {
              return Tabs.FirstOrDefault((TitleBarTab t) => t.Active);
          }
      }

      protected void OnTabDeselecting(TitleBarTabCancelEventArgs e)
      {
          if (TabDeselecting != null)
            TabDeselecting(this, e);
      }

      protected void OnTabDeselected(TitleBarTabEventArgs e)
      {
          if (TabDeselected != null)
            TabDeselected(this, e);
      }

      protected void OnTabSelecting(TitleBarTabCancelEventArgs e)
      {
          if (TabSelecting != null)
            TabSelecting(this, e);
      }

      protected void OnTabSelected(TitleBarTabEventArgs e)
      {
          if (TabSelected != null)
            TabSelected(this, e);
      }

      public int SelectedTabIndex
      {
          get
          {
              return Tabs.FindIndex((TitleBarTab t) => t.Active);
          }

          set
          {
              TitleBarTab selectedTab = SelectedTab;
              int selectedTabIndex = SelectedTabIndex;

              if (selectedTab != null && selectedTabIndex != value)
              {
                  TitleBarTabCancelEventArgs e = new TitleBarTabCancelEventArgs
                  {
                      Action = TabControlAction.Deselecting,
                      Tab = selectedTab,
                      TabIndex = selectedTabIndex
                  };

                  OnTabDeselecting(e);

                  if (e.Cancel)
                      return;

                  selectedTab.Active = false;

                  OnTabDeselected(new TitleBarTabEventArgs
                      {
                          Tab = selectedTab,
                          TabIndex = selectedTabIndex,
                          Action = TabControlAction.Deselected
                      });
              }

              if (value != -1)
              {
                  TitleBarTabCancelEventArgs e = new TitleBarTabCancelEventArgs
                  {
                      Action = TabControlAction.Selecting,
                      Tab = Tabs[value],
                      TabIndex = value
                  };

                  OnTabSelecting(e);

                  if (e.Cancel)
                      return;

                  Tabs[value].Active = true;

                  OnTabDeselected(new TitleBarTabEventArgs
                  {
                      Tab = Tabs[value],
                      TabIndex = value,
                      Action = TabControlAction.Selected
                  });
              }

              Refresh();
          }
      }

      protected override void OnClientSizeChanged(EventArgs e)
      {
          ResizeTabContents();
      }

      protected void ResizeTabContents(TitleBarTab tab = null)
      {
          if (tab == null)
            tab = SelectedTab;

          if (tab != null)
          {
              tab.Content.Location = new Point(BorderLeft - 2, BorderTop - 2);
              tab.Content.Size = new Size(ClientRectangle.Width - BorderLeft - BorderRight + 4, ClientRectangle.Height - BorderTop - BorderBottom + 4);
          }
      }

      protected override CreateParams CreateParams
      {
          get
          {
              const int WS_EX_TRANSPARENT = 0x20;
              CreateParams cp = base.CreateParams;
              cp.ExStyle |= WS_EX_TRANSPARENT;
              return cp;
          }
      }

      protected override void OnPaintBackground(PaintEventArgs e)
      {
          
      }

    public TitleBarTabs()
    {
      InitializeComponent();
      SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
      _tabs.CollectionModified += new EventHandler<ListModificationEventArgs>(_tabs_CollectionModified);
    }

    void _tabs_CollectionModified(object sender, ListModificationEventArgs e)
    {
        Refresh();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using (BufferedGraphics graphics = BufferedGraphicsManager.Current.Allocate(e.Graphics, e.ClipRectangle))
        {
            base.OnPaint(new PaintEventArgs(graphics.Graphics, e.ClipRectangle));

            if (TabRenderer != null)
                TabRenderer.Render(Tabs, graphics.Graphics, PointToClient(Cursor.Position));

            graphics.Render();
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);

        if (TabRenderer != null && (_previousWindowState == null || WindowState != _previousWindowState.Value))
        {
            BorderTop = TabRenderer.TabHeight + 10 + (WindowState != FormWindowState.Maximized ? 13 : 0);

            MARGINS margins = new MARGINS();

            margins.cxLeftWidth = Math.Abs(BorderLeft);
            margins.cxRightWidth = Math.Abs(BorderRight);
            margins.cyBottomHeight = Math.Abs(BorderBottom);
            margins.cyTopHeight = Math.Abs(BorderTop);

            Win32Interop.DwmExtendFrameIntoClientArea(Handle, ref margins);
            
            _previousWindowState = WindowState;

            Refresh();
        }
    }

    protected override void WndProc(ref Message m)
    {
      IntPtr result = IntPtr.Zero;

      bool callDWP = !Win32Interop.DwmDefWindowProc(m.HWnd, m.Msg, m.WParam, m.LParam, out result);

      switch (m.Msg)
      {
          case Win32Messages.WM_CREATE:
              {
                  RECT rc;
                  int style = Win32Interop.GetWindowLong(m.HWnd, Win32Constants.GWL_STYLE);
                  int styleEx = Win32Interop.GetWindowLong(m.HWnd, Win32Constants.GWL_EXSTYLE);
                  Win32Interop.AdjustWindowRectEx(out rc, style, false, styleEx);

                  BorderTop = Math.Abs(rc.top);
                  BorderLeft = Math.Abs(rc.left);
                  BorderRight = Math.Abs(rc.right);
                  BorderBottom = Math.Abs(rc.bottom);
              }
              break;

          case Win32Messages.WM_ACTIVATE:
              {
                  MARGINS margins = new MARGINS();

                  margins.cxLeftWidth = Math.Abs(BorderLeft);
                  margins.cxRightWidth = Math.Abs(BorderRight);
                  margins.cyBottomHeight = Math.Abs(BorderBottom);
                  margins.cyTopHeight = Math.Abs(BorderTop);

                  int hr = Win32Interop.DwmExtendFrameIntoClientArea(m.HWnd, ref margins);

                  ResizeTabContents();
                  result = IntPtr.Zero;
              }
              break;

          case Win32Messages.WM_NCCALCSIZE:
              {
                  if (m.WParam != IntPtr.Zero)
                  {
                      result = IntPtr.Zero;
                      callDWP = false;
                  }
              }
              break;

          case Win32Messages.WM_NCLBUTTONDOWN:
          case Win32Messages.WM_LBUTTONDOWN:
              _clickedTab = TabRenderer.OverTab(Tabs, new Point(Cursor.Position.X - Location.X, Cursor.Position.Y - Location.Y));

              if (_clickedTab != null || TabRenderer.IsOverAddButton(PointToClient(Cursor.Position)))
                  Win32Interop.SetCapture(m.HWnd);

              break;

          case Win32Messages.WM_LBUTTONUP:
          case Win32Messages.WM_NCLBUTTONUP:
              if (_clickedTab != null)
              {
                  Rectangle absoluteCloseButtonArea = new Rectangle();

                  if (_clickedTab.ShowCloseButton)
                      absoluteCloseButtonArea = new Rectangle(_clickedTab.Area.X + _clickedTab.CloseButtonArea.X, _clickedTab.Area.Y + _clickedTab.CloseButtonArea.Y, _clickedTab.CloseButtonArea.Width, _clickedTab.CloseButtonArea.Height);

                  if (absoluteCloseButtonArea.Contains(PointToClient(Cursor.Position)))
                  {
                      int removeIndex = Tabs.IndexOf(_clickedTab);
                      int selectedTabIndex = SelectedTabIndex;

                      Tabs.Remove(_clickedTab);

                      if (selectedTabIndex > removeIndex)
                          SelectedTabIndex = selectedTabIndex - 1;

                      else if (selectedTabIndex == removeIndex)
                          SelectedTabIndex = Math.Min(selectedTabIndex, Tabs.Count - 1);

                      else
                          SelectedTabIndex = selectedTabIndex;
                  }

                  else
                  {
                      ResizeTabContents(_clickedTab);
                      SelectedTabIndex = Tabs.IndexOf(_clickedTab);
                  }

                  Win32Interop.ReleaseCapture();
              }

              else if (TabRenderer.IsOverAddButton(PointToClient(Cursor.Position)))
              {
                  TitleBarTab newTab = CreateTab();

                  Tabs.Add(newTab);
                  ResizeTabContents(newTab);

                  SelectedTabIndex = _tabs.Count - 1;

                  Win32Interop.ReleaseCapture();
              }

              break;

          case Win32Messages.WM_NCHITTEST:
              {
                  base.WndProc(ref m);

                  if (m.Result.ToInt32() == Win32Constants.HTCLOSE || m.Result.ToInt32() == Win32Constants.HTMINBUTTON || m.Result.ToInt32() == Win32Constants.HTMAXBUTTON || m.Result.ToInt32() == Win32Constants.HTMENU || m.Result.ToInt32() == Win32Constants.HTSYSMENU)
                  {
                      result = m.Result;
                      callDWP = false;
                  }

                  else
                  {
                      int ht = NCHitText(m);

                      callDWP = (ht == Win32Constants.HTNOWHERE);
                      result = new IntPtr(ht);
                  }
              }
              break;
      }

      m.Result = result;
      if (callDWP)
      {
        base.WndProc(ref m);
      }
    }

    private int NCHitText(Message m)
    {
      int lParam = (int)m.LParam;
      Point pt = new Point(lParam & 0xffff, lParam >> 16);

      Rectangle rc = new Rectangle(this.Location, ClientRectangle.Size);      
      
      int row = 1;
      int col = 1;
      bool onResizeBorder = false;
      
      // Determine if we are on the top or bottom border
      if (pt.Y >= rc.Top && pt.Y < rc.Top + BorderTop)
      {
        onResizeBorder = pt.Y < (rc.Top + BorderBottom);
        row = 0;        
      }
      else if (pt.Y < rc.Bottom && pt.Y > rc.Bottom - BorderBottom)
      {
        row = 2;
      }

      // Determine if we are on the left border or the right border
      if (pt.X >= rc.Left && pt.X < rc.Left + BorderLeft)
      {
        col = 0;
      }
      else if (pt.X < rc.Right && pt.X >= rc.Right - BorderRight)
      {
        col = 2;
      }

      int[,] hitTests = new int[,]
      {
        {Win32Constants.HTTOPLEFT, onResizeBorder ? Win32Constants.HTTOP : Win32Constants.HTCAPTION, Win32Constants.HTTOPRIGHT},
        {Win32Constants.HTLEFT, Win32Constants.HTNOWHERE, Win32Constants.HTRIGHT},
        {Win32Constants.HTBOTTOMLEFT, Win32Constants.HTBOTTOM, Win32Constants.HTBOTTOMRIGHT}
      };
      
      return hitTests[row, col];
    }
  }
}
