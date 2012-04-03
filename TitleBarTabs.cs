using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Stratman.Windows.Forms.TitleBarTabs
{
    /// <summary>
    /// Base class that contains the functionality to render tabs within a WinForms application's title bar area. This 
    /// is done through a borderless overlay window (<see cref="Overlay"/>) rendered on top of the non-client area at 
    /// the top of this window.  All an implementing class will need to do is set the <see cref="TabRenderer" /> 
    /// property and begin adding tabs to <see cref="Tabs" />.
    /// </summary>
    public abstract partial class TitleBarTabs : Form
    {
        /// <summary>
        /// Event delegate for <see cref="TitleBarTabs.TabDeselecting" /> and 
        /// <see cref="TitleBarTabs.TabSelecting" /> that allows subscribers to cancel the event and keep it from 
        /// proceeding.
        /// </summary>
        /// <param name="sender">Object for which this event was raised.</param>
        /// <param name="e">Data associated with the event.</param>
        public delegate void TitleBarTabCancelEventHandler(object sender, TitleBarTabCancelEventArgs e);

        /// <summary>
        /// Event delegate for <see cref="TitleBarTabs.TabSelected" /> and 
        /// <see cref="TitleBarTabs.TabDeselected" />.
        /// </summary>
        /// <param name="sender">Object for which this event was raised.</param>
        /// <param name="e">Data associated with the event.</param>
        public delegate void TitleBarTabEventHandler(object sender, TitleBarTabEventArgs e);

        /// <summary>
        /// Borderless window that is rendered over top of the non-client area of this window.
        /// </summary>
        internal TitleBarTabsOverlay Overlay;

        /// <summary>
        /// Height of the non-client area at the top of the window.
        /// </summary>
        protected int _nonClientAreaHeight;

        /// <summary>
        /// Maintains the previous window state so that we can respond properly to maximize/restore events in
        /// <see cref="OnSizeChanged" />.
        /// </summary>
        protected FormWindowState? _previousWindowState;

        /// <summary>
        /// Class responsible for actually rendering the tabs in <see cref="Overlay"/>.
        /// </summary>
        protected BaseTabRenderer _tabRenderer;

        /// <summary>
        /// List of tabs to display for this window.
        /// </summary>
        protected ListWithEvents<TitleBarTab> _tabs = new ListWithEvents<TitleBarTab>();

        protected bool _drawTitlebarBackground = false;

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected TitleBarTabs()
        {
            _previousWindowState = null;
            _drawTitlebarBackground = !IsCompositionEnabled;
            ExitOnLastTabClose = true;
            InitializeComponent();
            SetWindowThemeAttributes(WTNCA.NODRAWCAPTION | WTNCA.NODRAWICON);

            _tabs.CollectionModified += _tabs_CollectionModified;
            SystemColorsChanged += TitleBarTabs_SystemColorsChanged;

            // Set the window style so that we take care of painting the non-client area, a redraw is triggered when
            // the size of the window changes, and the window itself has a transparent background color (otherwise the
            // non-client area will simply be black when the window is maximized)
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
                true);
        }

        /// <summary>
        /// Repaints the window to update the background color for the titlebar via 
        /// <see cref="DrawTitleBarBackground(System.Drawing.Rectangle)"/>.
        /// </summary>
        /// <param name="e">Arguments associated with the event.</param>
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Invalidate();
        }

        /// <summary>
        /// Repaints the window to update the background color for the titlebar via 
        /// <see cref="DrawTitleBarBackground(System.Drawing.Rectangle)"/>.
        /// </summary>
        /// <param name="e">Arguments associated with the event.</param>
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            Invalidate();
        }

        /// <summary>
        /// Called when the OS changes display resolution or themes, which triggers a repaint.
        /// </summary>
        /// <param name="sender">Object from which this event originated.</param>
        /// <param name="e">Arguments associated with the event.</param>
        void TitleBarTabs_SystemColorsChanged(object sender, EventArgs e)
        {
            _drawTitlebarBackground = !IsCompositionEnabled;
            Invalidate();
        }

        /// <summary>
        /// Flag indicating whether composition is enabled on the desktop.
        /// </summary>
        internal bool IsCompositionEnabled
        {
            get
            {
                // This tests that the OS will support what we want to do. Will be false on Windows XP and earlier, as 
                // well as on Vista and 7 with Aero Glass disabled.
                bool hasComposition;
                Win32Interop.DwmIsCompositionEnabled(out hasComposition);

                return hasComposition;
            }
        }

        /// <summary>
        /// List of tabs to display for this window.
        /// </summary>
        public ListWithEvents<TitleBarTab> Tabs
        {
            get
            {
                return _tabs;
            }
        }

        /// <summary>
        /// The renderer to use when drawing the tabs.
        /// </summary>
        public BaseTabRenderer TabRenderer
        {
            get
            {
                return _tabRenderer;
            }

            set
            {
                _tabRenderer = value;
                SetFrameSize();
            }
        }

        /// <summary>
        /// The tab that is currently selected by the user.
        /// </summary>
        public TitleBarTab SelectedTab
        {
            get
            {
                return Tabs.FirstOrDefault((TitleBarTab t) => t.Active);
            }

            set
            {
                SelectedTabIndex = Tabs.IndexOf(value);
            }
        }

        /// <summary>
        /// Gets or sets the index of the tab that is currently selected by the user.
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
                    OnTabDeselected(
                        new TitleBarTabEventArgs
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
                    OnTabDeselected(
                        new TitleBarTabEventArgs
                                        {
                                            Tab = Tabs[value],
                                            TabIndex = value,
                                            Action = TabControlAction.Selected
                                        });
                }

                if (Overlay != null)
                    Overlay.Render();
            }
        }

        /// <summary>
        /// Flag indicating whether the application itself should exit when the last tab is closed.
        /// </summary>
        public bool ExitOnLastTabClose
        {
            get;
            set;
        }

        /// <summary>
        /// Calls <see cref="Win32Interop.SetWindowThemeAttribute"/> to set various attributes on the window.
        /// </summary>
        /// <param name="attributes">Attributes to set on the window.</param>
        private void SetWindowThemeAttributes(WTNCA attributes)
        {
            WTA_OPTIONS options = new WTA_OPTIONS
                                      {
                                          dwFlags = attributes,
                                          dwMask = WTNCA.VALIDBITS
                                      };

            // The SetWindowThemeAttribute API call takes care of everything
            Win32Interop.SetWindowThemeAttribute(
                Handle, WINDOWTHEMEATTRIBUTETYPE.WTA_NONCLIENT, ref options, (uint) Marshal.SizeOf(typeof (WTA_OPTIONS)));
        }

        /// <summary>
        /// Event handler that is invoked when the <see cref="Form.Load"/> event is fired.  Instantiates 
        /// <see cref="Overlay"/> and clears out the window's caption.
        /// </summary>
        /// <param name="e">Arguments associated with the event.</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Overlay = TitleBarTabsOverlay.GetInstance(this);
        }

        /// <summary>
        /// When the window's state (maximized, minimized, or restored) changes, this sets the size of the non-client
        /// area at the top of the window properly so that the tabs can be displayed.
        /// </summary>
        protected void SetFrameSize()
        {
            if (TabRenderer == null)
                return;

            int topPadding = TabRenderer.TabHeight - SystemInformation.VerticalResizeBorderThickness;

            if (WindowState == FormWindowState.Maximized)
                topPadding -= SystemInformation.CaptionHeight - SystemInformation.VerticalResizeBorderThickness -
                              SystemInformation.BorderSize.Width;

        	Padding = new Padding(
        		Padding.Left, topPadding > 0
        		              	? topPadding
        		              	: 0, Padding.Right, Padding.Bottom);

            // Set the margins and extend the frame into the client area
            MARGINS margins = new MARGINS
                                  {
                                      cxLeftWidth = 0,
                                      cxRightWidth = 0,
                                      cyBottomHeight = 0,
                                      cyTopHeight = topPadding > 0
                                                        ? topPadding
                                                        : 0
                                  };

            Win32Interop.DwmExtendFrameIntoClientArea(Handle, ref margins);

            _nonClientAreaHeight = SystemInformation.CaptionHeight + (topPadding > 0
                                                                          ? topPadding
                                                                          : 0);
        }

        /// <summary>
        /// Event that is raised immediately prior to a tab being deselected (<see cref="TabDeselected" />).
        /// </summary>
        public event TitleBarTabCancelEventHandler TabDeselecting;

        /// <summary>
        /// Event that is raised after a tab has been deselected.
        /// </summary>
        public event TitleBarTabEventHandler TabDeselected;

        /// <summary>
        /// Event that is raised immediately prior to a tab being selected (<see cref="TabSelected" />).
        /// </summary>
        public event TitleBarTabCancelEventHandler TabSelecting;

        /// <summary>
        /// Event that is raised after a tab has been selected.
        /// </summary>
        public event TitleBarTabEventHandler TabSelected;

        /// <summary>
        /// Callback that should be implemented by the inheriting class that will create a new 
        /// <see cref="TitleBarTab" /> object when the add button is clicked.
        /// </summary>
        /// <returns>A newly created tab.</returns>
        public abstract TitleBarTab CreateTab();

        /// <summary>
        /// Callback for the <see cref="TabDeselecting" /> event.
        /// </summary>
        /// <param name="e">Arguments associated with the event.</param>
        protected void OnTabDeselecting(TitleBarTabCancelEventArgs e)
        {
            if (TabDeselecting != null)
                TabDeselecting(this, e);
        }

        /// <summary>
        /// Callback for the <see cref="TabDeselected" /> event.
        /// </summary>
        /// <param name="e">Arguments associated with the event.</param>
        protected void OnTabDeselected(TitleBarTabEventArgs e)
        {
            if (TabDeselected != null)
                TabDeselected(this, e);
        }

        /// <summary>
        /// Callback for the <see cref="TabSelecting" /> event.
        /// </summary>
        /// <param name="e">Arguments associated with the event.</param>
        protected void OnTabSelecting(TitleBarTabCancelEventArgs e)
        {
            if (TabSelecting != null)
                TabSelecting(this, e);
        }

        /// <summary>
        /// Callback for the <see cref="TabSelected" /> event.
        /// </summary>
        /// <param name="e">Arguments associated with the event.</param>
        protected void OnTabSelected(TitleBarTabEventArgs e)
        {
            if (TabSelected != null)
                TabSelected(this, e);
        }

        /// <summary>
        /// Callback for the <see cref="Form.ClientSizeChanged" /> event that resizes the 
        /// <see cref="TitleBarTab.Content" /> form of the currently selected tab when the size of the client area 
        /// for this window changes.
        /// </summary>
        /// <param name="e">Arguments associated with the event.</param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            
            ResizeTabContents();
        }

        /// <summary>
        /// Resizes the <see cref="TitleBarTab.Content" /> form of the <see cref="tab" /> to match the size of the 
        /// client area for this window.
        /// </summary>
        /// <param name="tab">Tab whose <see cref="TitleBarTab.Content" /> form we should resize; if not specified, 
        /// we default to <see cref="SelectedTab" />.</param>
        public void ResizeTabContents(TitleBarTab tab = null)
        {
            if (tab == null)
                tab = SelectedTab;

            if (tab != null)
            {
                tab.Content.Location = new Point(0, Padding.Top - 1);
                tab.Content.Size = new Size(
                    ClientRectangle.Width,
                                            ClientRectangle.Height - Padding.Top + 1);
            }
        }

        /// <summary>
        /// Override of the handler for the paint background event that is left blank so that code is never executed.
        /// </summary>
        /// <param name="e">Arguments associated with the event.</param>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

		internal void ForwardMessage(ref Message m)
		{
			m.HWnd = Handle;
			WndProc(ref m);
		}

        /// <summary>
        /// Callback that is invoked whenever anything is added or removed from <see cref="Tabs" /> so that we can
        /// trigger a redraw of the tabs.
        /// </summary>
        /// <param name="sender">Object for which this event was raised.</param>
        /// <param name="e">Arguments associated with the event.</param>
        private void _tabs_CollectionModified(object sender, ListModificationEventArgs e)
        {
            if (e.Modification == ListModification.ItemAdded || e.Modification == ListModification.RangeAdded)
            {
                for (int i = 0; i < e.Count; i++)
                {
                    Tabs[i + e.StartIndex].Content.TextChanged += Content_TextChanged;
                    Tabs[i + e.StartIndex].Closing += TitleBarTabs_Closing;
                }
            }

            if (Overlay != null)
                Overlay.Render();
        }

        /// <summary>
        /// Event handler that is called when a tab's <see cref="TitleBarTab.Closing"/> event is fired, which removes
        /// the tab from <see cref="Tabs"/> and re-renders <see cref="Overlay"/>.
        /// </summary>
        /// <param name="sender">Object from which this event originated (the <see cref="TitleBarTab"/> in this
        /// case).</param>
        /// <param name="e">Arguments associated with the event.</param>
        private void Content_TextChanged(object sender, EventArgs e)
        {
            if (Overlay != null)
                Overlay.Render();
        }

        /// <summary>
        /// Event handler that is called when a tab's <see cref="TitleBarTab.Closing"/> event is fired, which removes
        /// the tab from <see cref="Tabs"/> and re-renders <see cref="Overlay"/>.
        /// </summary>
        /// <param name="sender">Object from which this event originated (the <see cref="TitleBarTab"/> in this 
        /// case).</param>
        /// <param name="e">Arguments associated with the event.</param>
        private void TitleBarTabs_Closing(object sender, CancelEventArgs e)
        {
            CloseTab((TitleBarTab) sender);

            if (Overlay != null)
                Overlay.Render();
        }

        /// <summary>
        /// Overrides the <see cref="Control.SizeChanged" /> handler so that we can detect when the user has 
        /// maximized or restored the window and adjust the size of the non-client area accordingly.
        /// </summary>
        /// <param name="e">Arguments associated with the event.</param>
        protected override void OnSizeChanged(EventArgs e)
        {
            // If no tab renderer has been set yet or the window state hasn't changed, don't do anything
            if (_previousWindowState != null && WindowState != _previousWindowState.Value)
                SetFrameSize();

            _previousWindowState = WindowState;

			base.OnSizeChanged(e);
        }

        /// <summary>
        /// Overrides the message processor for the window so that we can respond to windows events to render and
        /// manipulate the tabs properly.
        /// </summary>
        /// <param name="m">Message received by the pump.</param>
        protected override void WndProc(ref Message m)
        {
            bool callDwp = true;

            switch (m.Msg)
            {
                // When the window is activated, set the size of the non-client area appropriately
                case Win32Messages.WM_ACTIVATE:
                    SetFrameSize();
                    ResizeTabContents();
                    m.Result = IntPtr.Zero;

                    break;

                case Win32Messages.WM_NCHITTEST:
                    // Call the base message handler to see where the user clicked in the window
                    base.WndProc(ref m);

                    // If they were over the minimize/maximize/close buttons or the system menu, let the message pass
                    if (!(m.Result.ToInt32() == Win32Constants.HTCLOSE ||
                          m.Result.ToInt32() == Win32Constants.HTMINBUTTON ||
                          m.Result.ToInt32() == Win32Constants.HTMAXBUTTON ||
                          m.Result.ToInt32() == Win32Constants.HTMENU ||
                          m.Result.ToInt32() == Win32Constants.HTSYSMENU))
                    {
                        int hitResult = HitTest(m);
                        m.Result = new IntPtr(hitResult);
                    }

                    callDwp = false;

                    break;
            }

            if (callDwp)
                base.WndProc(ref m);
        }

        /// <summary>
        /// Calls <see cref="CreateTab"/>, adds the resulting tab to the <see cref="Tabs"/> collection, and activates
        /// it.
        /// </summary>
        public virtual void AddNewTab()
        {
            TitleBarTab newTab = CreateTab();

            Tabs.Add(newTab);
            ResizeTabContents(newTab);

            SelectedTabIndex = _tabs.Count - 1;
        }

        /// <summary>
        /// Removes <see cref="closingTab"/> from <see cref="Tabs"/> and selects the next applicable tab in the list.
        /// </summary>
        /// <param name="closingTab">Tab that is being closed.</param>
        protected virtual void CloseTab(TitleBarTab closingTab)
        {
            int removeIndex = Tabs.IndexOf(closingTab);
            int selectedTabIndex = SelectedTabIndex;

            Tabs.Remove(closingTab);

            if (selectedTabIndex > removeIndex)
                SelectedTabIndex = selectedTabIndex - 1;

            else if (selectedTabIndex == removeIndex)
                SelectedTabIndex = Math.Min(selectedTabIndex, Tabs.Count - 1);

            else
                SelectedTabIndex = selectedTabIndex;

            if (Tabs.Count == 0 && ExitOnLastTabClose)
                Close();
        }

        /// <summary>
        /// Called when a <see cref="Win32Messages.WM_NCHITTEST" /> message is received to see where in the non-
        /// client area the user clicked.
        /// </summary>
        /// <param name="m">Message received by <see cref="WndProc" />.</param>
        /// <returns>One of the <see cref="Win32Constants" />.HT* constants, depending on where the user
        /// clicked.</returns>
        private int HitTest(Message m)
        {
            // Get the point that the user clicked
            int lParam = (int) m.LParam;
            Point point = new Point(lParam & 0xffff, lParam >> 16);
            RECT rect;

            Win32Interop.GetWindowRect(m.HWnd, out rect);
            Rectangle area = new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);

            int row = 1;
            int column = 1;
            bool onResizeBorder = false;

            // Determine if we are on the top or bottom border
            if (point.Y >= area.Top &&
                point.Y < area.Top + SystemInformation.VerticalResizeBorderThickness + _nonClientAreaHeight - 2)
            {
                onResizeBorder = point.Y < (area.Top + SystemInformation.VerticalResizeBorderThickness);
                row = 0;
            }

            else if (point.Y < area.Bottom && point.Y > area.Bottom - SystemInformation.VerticalResizeBorderThickness)
                row = 2;

            // Determine if we are on the left border or the right border
            if (point.X >= area.Left && point.X < area.Left + SystemInformation.HorizontalResizeBorderThickness)
                column = 0;

            else if (point.X < area.Right && point.X >= area.Right - SystemInformation.HorizontalResizeBorderThickness)
                column = 2;

            int[,] hitTests = new[,]
                                  {
                                      {
                                          onResizeBorder
                                              ? Win32Constants.HTTOPLEFT
                                              : Win32Constants.HTLEFT,
                                          onResizeBorder
                                              ? Win32Constants.HTTOP
                                              : Win32Constants.HTCAPTION,
                                          onResizeBorder
                                              ? Win32Constants.HTTOPRIGHT
                                              : Win32Constants.HTRIGHT
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