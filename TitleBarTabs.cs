using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Stratman.Windows.Forms.TitleBarTabs
{
    /// <summary>
    ///   Base class that contains the functionality to render tabs within a WinForms application's title bar area. 
    ///   This takes care of making the necessary DWM calls to set the size of the non-client area and overriding
    ///   <see cref = "WndProc" /> to respond to various messages.  All an implementing class will need to do is set the
    ///   <see cref = "TabRenderer" /> property and begin adding tabs to <see cref = "Tabs" />.
    /// </summary>
    public abstract partial class TitleBarTabs : Form
    {
        /// <summary>
        ///   Event delegate for <see cref = "TitleBarTabs.TabDeselecting" /> and 
        ///   <see cref = "TitleBarTabs.TabSelecting" /> that allows subscribers to cancel the event and keep it from 
        /// proceeding.
        /// </summary>
        /// <param name = "sender">Object for which this event was raised.</param>
        /// <param name = "e">Data associated with the event.</param>
        public delegate void TitleBarTabCancelEventHandler(object sender, TitleBarTabCancelEventArgs e);

        /// <summary>
        ///   Event delegate for <see cref = "TitleBarTabs.TabSelected" /> and 
        ///   <see cref = "TitleBarTabs.TabDeselected" />.
        /// </summary>
        /// <param name = "sender">Object for which this event was raised.</param>
        /// <param name = "e">Data associated with the event.</param>
        public delegate void TitleBarTabEventHandler(object sender, TitleBarTabEventArgs e);

        /// <summary>
        ///   State information representing a tab that was clicked during a 
        ///   <see cref = "Win32Messages.WM_LBUTTONDOWN" /> message so that we can respond properly during the 
        ///   <see cref = "Win32Messages.WM_LBUTTONUP" /> message.
        /// </summary>
        protected TitleBarTab _clickedTab;

        /// <summary>
        ///   Maintains the previous window state so that we can respond properly to maximize/restore events in
        ///   <see cref = "OnSizeChanged" />.
        /// </summary>
        protected FormWindowState? _previousWindowState;

        /// <summary>
        ///   List of tabs to display for this window.
        /// </summary>
        protected ListWithEvents<TitleBarTab> _tabs = new ListWithEvents<TitleBarTab>();

        /// <summary>
        ///   Default constructor.
        /// </summary>
        protected TitleBarTabs()
        {
            _previousWindowState = null;
            InitializeComponent();

            _tabs.CollectionModified += _tabs_CollectionModified;

            // Set the window style so that we take care of painting the non-client area, a redraw is triggered when
            // the size of the window changes, and the window itself has a transparent background color (otherwise the
            // non-client area will simply be black when the window is maximized)
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw | ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        }

        /// <summary>
        ///   Width of the area between the outside of the window border and the top of the client area.
        /// </summary>
        public int BorderTop
        {
            get;
            protected set;
        }

        /// <summary>
        ///   Width of the area between the outside of the window border and the left side of the client area.
        /// </summary>
        public int BorderLeft
        {
            get;
            protected set;
        }

        /// <summary>
        ///   Width of the area between the outside of the window border and the right side of the client area.
        /// </summary>
        public int BorderRight
        {
            get;
            protected set;
        }

        /// <summary>
        ///   Width of the area between the outside of the window border and the bottom of the client area.
        /// </summary>
        public int BorderBottom
        {
            get;
            protected set;
        }

        /// <summary>
        ///   List of tabs to display for this window.
        /// </summary>
        public ListWithEvents<TitleBarTab> Tabs
        {
            get
            {
                return _tabs;
            }
        }

        /// <summary>
        ///   The renderer to use when drawing the tabs.
        /// </summary>
        public BaseTabRenderer TabRenderer
        {
            get;
            set;
        }

        /// <summary>
        ///   The tab that is currently selected by the user.
        /// </summary>
        public TitleBarTab SelectedTab
        {
            get
            {
                return Tabs.FirstOrDefault((TitleBarTab t) => t.Active);
            }
        }

        /// <summary>
        ///   Gets or sets the index of the tab that is currently selected by the user.
        /// </summary>
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
                    // Raise the TabDeselecting event
                    TitleBarTabCancelEventArgs e = new TitleBarTabCancelEventArgs
                                                       {
                                                           Action = TabControlAction.Deselecting,
                                                           Tab = selectedTab,
                                                           TabIndex = selectedTabIndex
                                                       };

                    OnTabDeselecting(e);

                    // If the subscribers to the event cancelled it, return before we do anything else
                    if (e.Cancel)
                        return;

                    selectedTab.Active = false;

                    // Raise the TabDeselected event
                    OnTabDeselected(new TitleBarTabEventArgs
                                        {
                                            Tab = selectedTab,
                                            TabIndex = selectedTabIndex,
                                            Action = TabControlAction.Deselected
                                        });
                }

                if (value != -1)
                {
                    // Raise the TabSelecting event
                    TitleBarTabCancelEventArgs e = new TitleBarTabCancelEventArgs
                                                       {
                                                           Action = TabControlAction.Selecting,
                                                           Tab = Tabs[value],
                                                           TabIndex = value
                                                       };

                    OnTabSelecting(e);

                    // If the subscribers to the event cancelled it, return before we do anything else
                    if (e.Cancel)
                        return;

                    Tabs[value].Active = true;

                    // Raise the TabSelected event
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

        /// <summary>
        ///   Overridden method that allows us to specify a transparent background for the window, meaning that the
        ///   title bar won't show up as black when we maximize the window.
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;

                // 0x20 is the transparent background flag
                cp.ExStyle |= 0x20;
                return cp;
            }
        }

        /// <summary>
        ///   Event that is raised immediately prior to a tab being deselected (<see cref = "TabDeselected" />).
        /// </summary>
        public event TitleBarTabCancelEventHandler TabDeselecting;

        /// <summary>
        ///   Event that is raised after a tab has been deselected.
        /// </summary>
        public event TitleBarTabEventHandler TabDeselected;

        /// <summary>
        ///   Event that is raised immediately prior to a tab being selected (<see cref = "TabSelected" />).
        /// </summary>
        public event TitleBarTabCancelEventHandler TabSelecting;

        /// <summary>
        ///   Event that is raised after a tab has been selected.
        /// </summary>
        public event TitleBarTabEventHandler TabSelected;

        /// <summary>
        ///   Callback that should be implemented by the inheriting class that will create a new 
        ///   <see cref = "TitleBarTab" /> object when the add button is clicked.
        /// </summary>
        /// <returns>A newly created tab.</returns>
        public abstract TitleBarTab CreateTab();

        /// <summary>
        ///   Callback for the <see cref = "TabDeselecting" /> event.
        /// </summary>
        /// <param name = "e">Arguments associated with the event.</param>
        protected void OnTabDeselecting(TitleBarTabCancelEventArgs e)
        {
            if (TabDeselecting != null)
                TabDeselecting(this, e);
        }

        /// <summary>
        ///   Callback for the <see cref = "TabDeselected" /> event.
        /// </summary>
        /// <param name = "e">Arguments associated with the event.</param>
        protected void OnTabDeselected(TitleBarTabEventArgs e)
        {
            if (TabDeselected != null)
                TabDeselected(this, e);
        }

        /// <summary>
        ///   Callback for the <see cref = "TabSelecting" /> event.
        /// </summary>
        /// <param name = "e">Arguments associated with the event.</param>
        protected void OnTabSelecting(TitleBarTabCancelEventArgs e)
        {
            if (TabSelecting != null)
                TabSelecting(this, e);
        }

        /// <summary>
        ///   Callback for the <see cref = "TabSelected" /> event.
        /// </summary>
        /// <param name = "e">Arguments associated with the event.</param>
        protected void OnTabSelected(TitleBarTabEventArgs e)
        {
            if (TabSelected != null)
                TabSelected(this, e);
        }

        /// <summary>
        ///   Callback for the <see cref = "Form.ClientSizeChanged" /> event that resizes the 
        ///   <see cref = "TitleBarTab.Content" /> form of the currently selected tab when the size of the client area 
        ///   for this window changes.
        /// </summary>
        /// <param name = "e">Arguments associated with the event.</param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            ResizeTabContents();
        }

        /// <summary>
        ///   Resizes the <see cref = "TitleBarTab.Content" /> form of the <see cref = "tab" /> to match the size of the 
        ///   client area for this window.
        /// </summary>
        /// <param name = "tab">Tab whose <see cref = "TitleBarTab.Content" /> form we should resize; if not specified, 
        ///   we default to <see cref = "SelectedTab" />.</param>
        protected void ResizeTabContents(TitleBarTab tab = null)
        {
            if (tab == null)
                tab = SelectedTab;

            if (tab != null)
            {
                tab.Content.Location = new Point(BorderLeft - 2, BorderTop - 2);
                tab.Content.Size = new Size(ClientRectangle.Width - BorderLeft - BorderRight + 4,
                                            ClientRectangle.Height - BorderTop - BorderBottom + 4);
            }
        }

        /// <summary>
        ///   Override of the handler for the paint background event that is left blank so that code is never executed.
        /// </summary>
        /// <param name = "e">Arguments associated with the event.</param>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        /// <summary>
        ///   Callback that is invoked whenever anything is added or removed from <see cref = "Tabs" /> so that we can
        ///   trigger a redraw of the tabs.
        /// </summary>
        /// <param name = "sender">Object for which this event was raised.</param>
        /// <param name = "e">Arguments associated with the event.</param>
        private void _tabs_CollectionModified(object sender, ListModificationEventArgs e)
        {
            Refresh();
        }

        /// <summary>
        ///   Overrides the <see cref = "Control.Paint" /> handler so that we can render the tabs in the client area.
        /// </summary>
        /// <param name = "e">Arguments associated with the event.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            // Use double buffering to prevent flicker during resize; this does not eliminate tearing during resize,
            // but everything that I've read indicates that's impossible for WinForms apps
            using (BufferedGraphics graphics = BufferedGraphicsManager.Current.Allocate(e.Graphics, e.ClipRectangle))
            {
                base.OnPaint(new PaintEventArgs(graphics.Graphics, e.ClipRectangle));

                if (TabRenderer != null)
                    TabRenderer.Render(Tabs, graphics.Graphics, PointToClient(Cursor.Position));

                graphics.Render();
            }
        }

        /// <summary>
        ///   Overrides the <see cref = "Control.SizeChanged" /> handler so that we can detect when the user has 
        ///   maximized or restored the window and adjust the size of the non-client area accordingly.
        /// </summary>
        /// <param name = "e">Arguments associated with the event.</param>
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            // If no tab renderer has been set yet or the window state hasn't changed, don't do anything
            if (TabRenderer == null || (_previousWindowState != null && WindowState == _previousWindowState.Value))
                return;

            // Set the height of the non-client area using the window state and the tab height for the current renderer
            BorderTop = TabRenderer.TabHeight + 10 + (WindowState != FormWindowState.Maximized
                                                          ? 13
                                                          : 0);

            // Set the margins and extend the frame into the client area
            MARGINS margins = new MARGINS
                                  {
                                      cxLeftWidth = Math.Abs(BorderLeft),
                                      cxRightWidth = Math.Abs(BorderRight),
                                      cyBottomHeight = Math.Abs(BorderBottom),
                                      cyTopHeight = Math.Abs(BorderTop)
                                  };

            Win32Interop.DwmExtendFrameIntoClientArea(Handle, ref margins);

            _previousWindowState = WindowState;
        }

        /// <summary>
        ///   Overrides the message processor for the window so that we can respond to windows events to render and
        ///   manipulate the tabs properly.
        /// </summary>
        /// <param name = "m">Message received by the pump.</param>
        protected override void WndProc(ref Message m)
        {
            IntPtr result;
            bool callDwp = !Win32Interop.DwmDefWindowProc(m.HWnd, m.Msg, m.WParam, m.LParam, out result);

            switch (m.Msg)
            {
                case Win32Messages.WM_CREATE:
                    RECT area;
                    int style = Win32Interop.GetWindowLong(m.HWnd, Win32Constants.GWL_STYLE);
                    int styleEx = Win32Interop.GetWindowLong(m.HWnd, Win32Constants.GWL_EXSTYLE);

                    Win32Interop.AdjustWindowRectEx(out area, style, false, styleEx);

                    // Initialize the various border properties to the size of the non-client area for the window
                    BorderTop = Math.Abs(area.top);
                    BorderLeft = Math.Abs(area.left);
                    BorderRight = Math.Abs(area.right);
                    BorderBottom = Math.Abs(area.bottom);

                    break;

                // When the window is activated, set the size of the non-client area appropriately
                case Win32Messages.WM_ACTIVATE:
                    MARGINS margins = new MARGINS
                                          {
                                              cxLeftWidth = Math.Abs(BorderLeft),
                                              cxRightWidth = Math.Abs(BorderRight),
                                              cyBottomHeight = Math.Abs(BorderBottom),
                                              cyTopHeight = Math.Abs(BorderTop)
                                          };

                    Win32Interop.DwmExtendFrameIntoClientArea(m.HWnd, ref margins);

                    ResizeTabContents();
                    result = IntPtr.Zero;

                    break;

                case Win32Messages.WM_NCCALCSIZE:
                    if (m.WParam != IntPtr.Zero)
                    {
                        result = IntPtr.Zero;
                        callDwp = false;
                    }
                    break;

                case Win32Messages.WM_NCLBUTTONDOWN:
                case Win32Messages.WM_LBUTTONDOWN:
                    // When the user clicks a mouse button, save the tab that the user was over so we can respond
                    // properly when the mouse button is released
                    _clickedTab = TabRenderer.OverTab(Tabs,
                                                      new Point(Cursor.Position.X - Location.X,
                                                                Cursor.Position.Y - Location.Y));

                    // If we were over a tab, set the capture state for the window so that we'll actually receive
                    // a WM_LBUTTONUP message
                    if (_clickedTab != null || TabRenderer.IsOverAddButton(PointToClient(Cursor.Position)))
                        Win32Interop.SetCapture(m.HWnd);

                    break;

                case Win32Messages.WM_LBUTTONUP:
                case Win32Messages.WM_NCLBUTTONUP:
                    if (_clickedTab != null)
                    {
                        Rectangle absoluteCloseButtonArea = new Rectangle();

                        if (_clickedTab.ShowCloseButton)
                        {
                            absoluteCloseButtonArea = new Rectangle(_clickedTab.Area.X + _clickedTab.CloseButtonArea.X,
                                                                    _clickedTab.Area.Y + _clickedTab.CloseButtonArea.Y,
                                                                    _clickedTab.CloseButtonArea.Width,
                                                                    _clickedTab.CloseButtonArea.Height);
                        }

                        // If the user clicked the close button, remove the tab from the list
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

                            // Otherwise, select the tab that was clicked
                        else
                        {
                            ResizeTabContents(_clickedTab);
                            SelectedTabIndex = Tabs.IndexOf(_clickedTab);
                        }

                        // Release the mouse capture
                        Win32Interop.ReleaseCapture();
                    }

                    // Otherwise, if the user clicked the add button, call CreateTab to add a new tab to the list
                    // and select it
                    else if (TabRenderer.IsOverAddButton(PointToClient(Cursor.Position)))
                    {
                        TitleBarTab newTab = CreateTab();

                        Tabs.Add(newTab);
                        ResizeTabContents(newTab);

                        SelectedTabIndex = _tabs.Count - 1;

                        // Release the mouse capture
                        Win32Interop.ReleaseCapture();
                    }

                    break;

                case Win32Messages.WM_NCHITTEST:
                    // Call the base message handler to see where the user clicked in the window
                    base.WndProc(ref m);

                    // If they were over the minimize/maximize/close buttons or the system menu, let the message pass
                    if (m.Result.ToInt32() == Win32Constants.HTCLOSE ||
                        m.Result.ToInt32() == Win32Constants.HTMINBUTTON ||
                        m.Result.ToInt32() == Win32Constants.HTMAXBUTTON ||
                        m.Result.ToInt32() == Win32Constants.HTMENU ||
                        m.Result.ToInt32() == Win32Constants.HTSYSMENU)
                    {
                        result = m.Result;
                        callDwp = false;
                    }

                    // Otherwise, see where the user clicked; if it was HTNOWHERE, let the base message handler take 
                    // care of it
                    else
                    {
                        int hitResult = HitTest(m);

                        callDwp = (hitResult == Win32Constants.HTNOWHERE);
                        result = new IntPtr(hitResult);
                    }

                    break;
            }

            m.Result = result;

            if (callDwp)
                base.WndProc(ref m);
        }

        /// <summary>
        ///   Called when a <see cref = "Win32Messages.WM_NCHITTEST" /> message is received to see where in the non-
        ///   client area the user clicked.
        /// </summary>
        /// <param name = "m">Message received by <see cref = "WndProc" />.</param>
        /// <returns>One of the <see cref = "Win32Constants" />.HT* constants, depending on where the user
        ///   clicked.</returns>
        private int HitTest(Message m)
        {
            // Get the point that the user clicked
            int lParam = (int) m.LParam;
            Point point = new Point(lParam & 0xffff, lParam >> 16);

            Rectangle area = new Rectangle(Location, ClientRectangle.Size);

            int row = 1;
            int column = 1;
            bool onResizeBorder = false;

            // Determine if we are on the top or bottom border
            if (point.Y >= area.Top && point.Y < area.Top + BorderTop)
            {
                onResizeBorder = point.Y < (area.Top + BorderBottom);
                row = 0;
            }
            else if (point.Y < area.Bottom && point.Y > area.Bottom - BorderBottom)
                row = 2;

            // Determine if we are on the left border or the right border
            if (point.X >= area.Left && point.X < area.Left + BorderLeft)
                column = 0;

            else if (point.X < area.Right && point.X >= area.Right - BorderRight)
                column = 2;

            int[,] hitTests = new[,]
                                  {
                                      {
                                          Win32Constants.HTTOPLEFT,
                                          onResizeBorder
                                              ? Win32Constants.HTTOP
                                              : Win32Constants.HTCAPTION,
                                          Win32Constants.HTTOPRIGHT
                                      },
                                      {
                                          Win32Constants.HTLEFT, Win32Constants.HTNOWHERE, Win32Constants.HTRIGHT
                                      },
                                      {
                                          Win32Constants.HTBOTTOMLEFT, Win32Constants.HTBOTTOM,
                                          Win32Constants.HTBOTTOMRIGHT
                                      }
                                  };

            return hitTests[row, column];
        }
    }
}