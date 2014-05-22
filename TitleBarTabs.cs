using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Taskbar;
using Win32Interop.Enums;
using Win32Interop.Methods;
using Win32Interop.Structs;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace EasyTabs
{
	/// <summary>
	/// Base class that contains the functionality to render tabs within a WinForms application's title bar area. This  is done through a borderless overlay
	/// window (<see cref="_overlay" />) rendered on top of the non-client area at the top of this window.  All an implementing class will need to do is set
	/// the <see cref="TabRenderer" /> property and begin adding tabs to <see cref="Tabs" />.
	/// </summary>
	public abstract partial class TitleBarTabs : Form
	{
		/// <summary>
		/// Event delegate for <see cref="TitleBarTabs.TabDeselecting" /> and <see cref="TitleBarTabs.TabSelecting" /> that allows subscribers to cancel the
		/// event and keep it from proceeding.
		/// </summary>
		/// <param name="sender">Object for which this event was raised.</param>
		/// <param name="e">Data associated with the event.</param>
		public delegate void TitleBarTabCancelEventHandler(object sender, TitleBarTabCancelEventArgs e);

		/// <summary>Event delegate for <see cref="TitleBarTabs.TabSelected" /> and <see cref="TitleBarTabs.TabDeselected" />.</summary>
		/// <param name="sender">Object for which this event was raised.</param>
		/// <param name="e">Data associated with the event.</param>
		public delegate void TitleBarTabEventHandler(object sender, TitleBarTabEventArgs e);

		/// <summary>Flag indicating whether or not each tab has an Aero Peek entry allowing the user to switch between tabs from the taskbar.</summary>
		protected bool _aeroPeekEnabled = true;

		/// <summary>Height of the non-client area at the top of the window.</summary>
		protected int _nonClientAreaHeight;

		/// <summary>Borderless window that is rendered over top of the non-client area of this window.</summary>
		protected internal TitleBarTabsOverlay _overlay;

		/// <summary>The preview images for each tab used to display each tab when Aero Peek is activated.</summary>
		protected Dictionary<Form, Bitmap> _previews = new Dictionary<Form, Bitmap>();

		/// <summary>
		/// When switching between tabs, this keeps track of the tab that was previously active so that, when it is switched away from, we can generate a fresh
		/// Aero Peek preview image for it.
		/// </summary>
		protected TitleBarTab _previousActiveTab = null;

		/// <summary>Maintains the previous window state so that we can respond properly to maximize/restore events in <see cref="OnSizeChanged" />.</summary>
		protected FormWindowState? _previousWindowState;

		/// <summary>Class responsible for actually rendering the tabs in <see cref="_overlay" />.</summary>
		protected BaseTabRenderer _tabRenderer;

		/// <summary>List of tabs to display for this window.</summary>
		protected ListWithEvents<TitleBarTab> _tabs = new ListWithEvents<TitleBarTab>();

		/// <summary>Default constructor.</summary>
		protected TitleBarTabs()
		{
			_previousWindowState = null;
			ExitOnLastTabClose = true;
			InitializeComponent();
			SetWindowThemeAttributes(WTNCA.NODRAWCAPTION | WTNCA.NODRAWICON);

			_tabs.CollectionModified += _tabs_CollectionModified;

			// Set the window style so that we take care of painting the non-client area, a redraw is triggered when the size of the window changes, and the 
			// window itself has a transparent background color (otherwise the non-client area will simply be black when the window is maximized)
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
		}

		/// <summary>Flag indicating whether composition is enabled on the desktop.</summary>
		internal bool IsCompositionEnabled
		{
			get
			{
				// This tests that the OS will support what we want to do. Will be false on Windows XP and earlier, as well as on Vista and 7 with Aero Glass 
				// disabled.
				bool hasComposition;
				Dwmapi.DwmIsCompositionEnabled(out hasComposition);

				return hasComposition;
			}
		}

		/// <summary>Flag indicating whether or not each tab has an Aero Peek entry allowing the user to switch between tabs from the taskbar.</summary>
		public bool AeroPeekEnabled
		{
			get
			{
				return _aeroPeekEnabled;
			}

			set
			{
				_aeroPeekEnabled = value;

				// Clear out any previously generate thumbnails if we are no longer enabled
				if (!_aeroPeekEnabled)
				{
					foreach (TitleBarTab tab in Tabs)
					{
						TaskbarManager.Instance.TabbedThumbnail.RemoveThumbnailPreview(tab.Content);
					}

					_previews.Clear();
				}

				else
				{
					foreach (TitleBarTab tab in Tabs)
					{
						CreateThumbnailPreview(tab);
					}

					if (SelectedTab != null)
					{
						TaskbarManager.Instance.TabbedThumbnail.SetActiveTab(SelectedTab.Content);
					}
				}
			}
		}

		/// <summary>List of tabs to display for this window.</summary>
		public ListWithEvents<TitleBarTab> Tabs
		{
			get
			{
				return _tabs;
			}
		}

		/// <summary>The renderer to use when drawing the tabs.</summary>
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

		/// <summary>The tab that is currently selected by the user.</summary>
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

		/// <summary>Gets or sets the index of the tab that is currently selected by the user.</summary>
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
					{
						return;
					}

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
					{
						return;
					}

					Tabs[value].Active = true;

					// Raise the TabSelected event
					OnTabSelected(
						new TitleBarTabEventArgs
						{
							Tab = Tabs[value],
							TabIndex = value,
							Action = TabControlAction.Selected
						});
				}

				if (_overlay != null)
				{
					_overlay.Render();
				}
			}
		}

		/// <summary>Flag indicating whether the application itself should exit when the last tab is closed.</summary>
		public bool ExitOnLastTabClose
		{
			get;
			set;
		}

		/// <summary>Flag indicating whether we are in the process of closing the window.</summary>
		public bool IsClosing
		{
			get;
			set;
		}

		/// <summary>Application context under which this particular window runs.</summary>
		public TitleBarTabsApplicationContext ApplicationContext
		{
			get;
			internal set;
		}

		/// <summary>Height of the "glassed" area of the window's non-client area.</summary>
		public int NonClientAreaHeight
		{
			get
			{
				return _nonClientAreaHeight;
			}
		}

		/// <summary>Area of the screen in which tabs can be dropped for this window.</summary>
		public Rectangle TabDropArea
		{
			get
			{
				return _overlay.TabDropArea;
			}
		}

		/// <summary>Calls <see cref="Uxtheme.SetWindowThemeAttribute" /> to set various attributes on the window.</summary>
		/// <param name="attributes">Attributes to set on the window.</param>
		private void SetWindowThemeAttributes(WTNCA attributes)
		{
			WTA_OPTIONS options = new WTA_OPTIONS
			                      {
				                      dwFlags = attributes,
				                      dwMask = WTNCA.VALIDBITS
			                      };

			// The SetWindowThemeAttribute API call takes care of everything
			Uxtheme.SetWindowThemeAttribute(Handle, WINDOWTHEMEATTRIBUTETYPE.WTA_NONCLIENT, ref options, (uint) Marshal.SizeOf(typeof (WTA_OPTIONS)));
		}

		/// <summary>
		/// Event handler that is invoked when the <see cref="Form.Load" /> event is fired.  Instantiates <see cref="_overlay" /> and clears out the window's
		/// caption.
		/// </summary>
		/// <param name="e">Arguments associated with the event.</param>
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			_overlay = TitleBarTabsOverlay.GetInstance(this);

			if (TabRenderer != null)
			{
				_overlay.MouseMove += TabRenderer.Overlay_MouseMove;
				_overlay.MouseUp += TabRenderer.Overlay_MouseUp;
				_overlay.MouseDown += TabRenderer.Overlay_MouseDown;
			}
		}

		/// <summary>
		/// When the window's state (maximized, minimized, or restored) changes, this sets the size of the non-client area at the top of the window properly so
		/// that the tabs can be displayed.
		/// </summary>
		protected void SetFrameSize()
		{
			if (TabRenderer == null || WindowState == FormWindowState.Minimized)
			{
				return;
			}

			int topPadding;

			if (WindowState == FormWindowState.Maximized)
			{
				topPadding = TabRenderer.TabHeight - SystemInformation.CaptionHeight;
			}

			else
			{
				topPadding = (TabRenderer.TabHeight + SystemInformation.CaptionButtonSize.Height) - SystemInformation.CaptionHeight;
			}

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

			Dwmapi.DwmExtendFrameIntoClientArea(Handle, ref margins);

			_nonClientAreaHeight = SystemInformation.CaptionHeight + (topPadding > 0
				? topPadding
				: 0);

			if (AeroPeekEnabled)
			{
				foreach (
					TabbedThumbnail preview in
						Tabs.Select(tab => TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(tab.Content)).Where(preview => preview != null))
				{
					preview.PeekOffset = new Vector(Padding.Left, Padding.Top - 1);
				}
			}
		}

		/// <summary>Event that is raised immediately prior to a tab being deselected (<see cref="TabDeselected" />).</summary>
		public event TitleBarTabCancelEventHandler TabDeselecting;

		/// <summary>Event that is raised after a tab has been deselected.</summary>
		public event TitleBarTabEventHandler TabDeselected;

		/// <summary>Event that is raised immediately prior to a tab being selected (<see cref="TabSelected" />).</summary>
		public event TitleBarTabCancelEventHandler TabSelecting;

		/// <summary>Event that is raised after a tab has been selected.</summary>
		public event TitleBarTabEventHandler TabSelected;

		/// <summary>Event that is raised after a tab has been clicked.</summary>
		public event TitleBarTabEventHandler TabClicked;

		/// <summary>
		/// Callback that should be implemented by the inheriting class that will create a new <see cref="TitleBarTab" /> object when the add button is
		/// clicked.
		/// </summary>
		/// <returns>A newly created tab.</returns>
		public abstract TitleBarTab CreateTab();

		/// <summary>Callback for the <see cref="TabClicked" /> event.</summary>
		/// <param name="e">Arguments associated with the event.</param>
		protected internal void OnTabClicked(TitleBarTabEventArgs e)
		{
			if (TabClicked != null)
			{
				TabClicked(this, e);
			}
		}

		/// <summary>
		/// Callback for the <see cref="TabDeselecting" /> event.  Called when a <see cref="TitleBarTab" /> is in the process of losing focus.  Grabs an image of
		/// the tab's content to be used when Aero Peek is activated.
		/// </summary>
		/// <param name="e">Arguments associated with the event.</param>
		protected void OnTabDeselecting(TitleBarTabCancelEventArgs e)
		{
			if (_previousActiveTab != null && AeroPeekEnabled)
			{
				UpdateTabThumbnail(_previousActiveTab);
			}

			if (TabDeselecting != null)
			{
				TabDeselecting(this, e);
			}
		}

		/// <summary>Generate a new thumbnail image for <paramref name="tab" />.</summary>
		/// <param name="tab">Tab that we need to generate a thumbnail for.</param>
		protected void UpdateTabThumbnail(TitleBarTab tab)
		{
			TabbedThumbnail preview = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(tab.Content);

			if (preview == null)
			{
				return;
			}

			Bitmap bitmap = TabbedThumbnailScreenCapture.GrabWindowBitmap(tab.Content.Handle, tab.Content.Size);

			preview.SetImage(bitmap);

			// If we already had a preview image for the tab, dispose of it
			if (_previews.ContainsKey(tab.Content) && _previews[tab.Content] != null)
			{
				_previews[tab.Content].Dispose();
			}

			_previews[tab.Content] = bitmap;
		}

		/// <summary>Callback for the <see cref="TabDeselected" /> event.</summary>
		/// <param name="e">Arguments associated with the event.</param>
		protected void OnTabDeselected(TitleBarTabEventArgs e)
		{
			if (TabDeselected != null)
			{
				TabDeselected(this, e);
			}
		}

		/// <summary>Callback for the <see cref="TabSelecting" /> event.</summary>
		/// <param name="e">Arguments associated with the event.</param>
		protected void OnTabSelecting(TitleBarTabCancelEventArgs e)
		{
			if (TabSelecting != null)
			{
				TabSelecting(this, e);
			}
		}

		/// <summary>
		/// Callback for the <see cref="TabSelected" /> event.  Called when a <see cref="TitleBarTab" /> gains focus.  Sets the active window in Aero Peek via a
		/// call to <see cref="TabbedThumbnailManager.SetActiveTab(Control)" />.
		/// </summary>
		/// <param name="e">Arguments associated with the event.</param>
		protected void OnTabSelected(TitleBarTabEventArgs e)
		{
			if (SelectedTabIndex != -1 && _previews.ContainsKey(SelectedTab.Content) && AeroPeekEnabled)
			{
				TaskbarManager.Instance.TabbedThumbnail.SetActiveTab(SelectedTab.Content);
			}

			_previousActiveTab = SelectedTab;

			if (TabSelected != null)
			{
				TabSelected(this, e);
			}
		}

		/// <summary>
		/// Handler method that's called when Aero Peek needs to display a thumbnail for a <see cref="TitleBarTab" />; finds the preview bitmap generated in
		/// <see cref="TabDeselecting" /> and returns that.
		/// </summary>
		/// <param name="sender">Object from which this event originated.</param>
		/// <param name="e">Arguments associated with this event.</param>
		private void preview_TabbedThumbnailBitmapRequested(object sender, TabbedThumbnailBitmapRequestedEventArgs e)
		{
			foreach (
				TitleBarTab rdcWindow in Tabs.Where(rdcWindow => rdcWindow.Content.Handle == e.WindowHandle && _previews.ContainsKey(rdcWindow.Content)))
			{
				TabbedThumbnail preview = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(rdcWindow.Content);
				preview.SetImage(_previews[rdcWindow.Content]);

				break;
			}
		}

		/// <summary>
		/// Callback for the <see cref="Control.ClientSizeChanged" /> event that resizes the <see cref="TitleBarTab.Content" /> form of the currently selected
		/// tab when the size of the client area for this window changes.
		/// </summary>
		/// <param name="e">Arguments associated with the event.</param>
		protected override void OnClientSizeChanged(EventArgs e)
		{
			base.OnClientSizeChanged(e);

			ResizeTabContents();
		}

		/// <summary>Resizes the <see cref="TitleBarTab.Content" /> form of the <paramref name="tab" /> to match the size of the client area for this window.</summary>
		/// <param name="tab">Tab whose <see cref="TitleBarTab.Content" /> form we should resize; if not specified, we default to
		/// <see cref="SelectedTab" />.</param>
		public void ResizeTabContents(TitleBarTab tab = null)
		{
			if (tab == null)
			{
				tab = SelectedTab;
			}

			if (tab != null)
			{
				tab.Content.Location = new Point(0, Padding.Top - 1);
				tab.Content.Size = new Size(ClientRectangle.Width, ClientRectangle.Height - Padding.Top + 1);
			}
		}

		/// <summary>Override of the handler for the paint background event that is left blank so that code is never executed.</summary>
		/// <param name="e">Arguments associated with the event.</param>
		protected override void OnPaintBackground(PaintEventArgs e)
		{
		}

		/// <summary>Forwards a message received by <see cref="TitleBarTabsOverlay" /> to the underlying window.</summary>
		/// <param name="m">Message received by the overlay.</param>
		internal void ForwardMessage(ref Message m)
		{
			m.HWnd = Handle;
			WndProc(ref m);
		}

		/// <summary>
		/// Handler method that's called when the user clicks on an Aero Peek preview thumbnail.  Finds the tab associated with the thumbnail and
		/// focuses on it.
		/// </summary>
		/// <param name="sender">Object from which this event originated.</param>
		/// <param name="e">Arguments associated with this event.</param>
		private void preview_TabbedThumbnailActivated(object sender, TabbedThumbnailEventArgs e)
		{
			foreach (TitleBarTab tab in Tabs.Where(tab => tab.Content.Handle == e.WindowHandle))
			{
				SelectedTabIndex = Tabs.IndexOf(tab);
				TaskbarManager.Instance.TabbedThumbnail.SetActiveTab(tab.Content);

				break;
			}

			// Restore the window if it was minimized
			if (WindowState == FormWindowState.Minimized)
			{
				User32.ShowWindow(Handle, 3);
			}

			else
			{
				Focus();
			}
		}

		/// <summary>
		/// Handler method that's called when the user clicks the close button in an Aero Peek preview thumbnail.  Finds the window associated with the thumbnail
		/// and calls <see cref="Form.Close" /> on it.
		/// </summary>
		/// <param name="sender">Object from which this event originated.</param>
		/// <param name="e">Arguments associated with this event.</param>
		private void preview_TabbedThumbnailClosed(object sender, TabbedThumbnailEventArgs e)
		{
			foreach (TitleBarTab tab in Tabs.Where(tab => tab.Content.Handle == e.WindowHandle))
			{
				CloseTab(tab);

				break;
			}
		}

		/// <summary>Callback that is invoked whenever anything is added or removed from <see cref="Tabs" /> so that we can trigger a redraw of the tabs.</summary>
		/// <param name="sender">Object for which this event was raised.</param>
		/// <param name="e">Arguments associated with the event.</param>
		private void _tabs_CollectionModified(object sender, ListModificationEventArgs e)
		{
			SetFrameSize();

			if (e.Modification == ListModification.ItemAdded || e.Modification == ListModification.RangeAdded)
			{
				for (int i = 0; i < e.Count; i++)
				{
					TitleBarTab currentTab = Tabs[i + e.StartIndex];

					currentTab.Content.TextChanged += Content_TextChanged;
					currentTab.Closing += TitleBarTabs_Closing;

					if (AeroPeekEnabled)
					{
						TaskbarManager.Instance.TabbedThumbnail.SetActiveTab(CreateThumbnailPreview(currentTab));
					}
				}
			}

			if (_overlay != null)
			{
				_overlay.Render(true);
			}
		}

		/// <summary>
		/// Creates a new thumbnail for <paramref name="tab" /> when the application is initially enabled for AeroPeek or when it is turned on sometime during
		/// execution.
		/// </summary>
		/// <param name="tab">Tab that we are to create the thumbnail for.</param>
		/// <returns>Thumbnail created for <paramref name="tab" />.</returns>
		protected virtual TabbedThumbnail CreateThumbnailPreview(TitleBarTab tab)
		{
			TabbedThumbnail preview = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(tab.Content);

			if (preview != null)
			{
				TaskbarManager.Instance.TabbedThumbnail.RemoveThumbnailPreview(tab.Content);
			}

			preview = new TabbedThumbnail(Handle, tab.Content)
			          {
				          Title = tab.Content.Text,
				          Tooltip = tab.Content.Text
			          };

			preview.SetWindowIcon((Icon)tab.Content.Icon.Clone());

			preview.TabbedThumbnailActivated += preview_TabbedThumbnailActivated;
			preview.TabbedThumbnailClosed += preview_TabbedThumbnailClosed;
			preview.TabbedThumbnailBitmapRequested += preview_TabbedThumbnailBitmapRequested;
			preview.PeekOffset = new Vector(Padding.Left, Padding.Top - 1);

			TaskbarManager.Instance.TabbedThumbnail.AddThumbnailPreview(preview);

			return preview;
		}

		/// <summary>
		/// When a child tab updates its <see cref="Form.Icon"/> property, it should call this method to update the icon in the AeroPeek preview.
		/// </summary>
		/// <param name="tab">Tab whose icon was updated.</param>
		/// <param name="icon">The new icon to use.  If this is left as null, we use <see cref="Form.Icon"/> on <paramref name="tab"/>.</param>
		public virtual void UpdateThumbnailPreviewIcon(TitleBarTab tab, Icon icon = null)
		{
			if (!AeroPeekEnabled)
			{
				return;
			}

			TabbedThumbnail preview = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(tab.Content);

			if (preview == null)
			{
				return;
			}

			if (icon == null)
			{
				icon = tab.Content.Icon;
			}

			preview.SetWindowIcon((Icon)icon.Clone());
		}

		/// <summary>
		/// Event handler that is called when a tab's <see cref="Form.Text" /> property is changed, which re-renders the tab text and updates the title of the
		/// Aero Peek preview.
		/// </summary>
		/// <param name="sender">Object from which this event originated (the <see cref="TitleBarTab.Content" /> object in this case).</param>
		/// <param name="e">Arguments associated with the event.</param>
		private void Content_TextChanged(object sender, EventArgs e)
		{
			if (AeroPeekEnabled)
			{
				TabbedThumbnail preview = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview((Form) sender);

				if (preview != null)
				{
					preview.Title = (sender as Form).Text;
				}
			}

			if (_overlay != null)
			{
				_overlay.Render(true);
			}
		}

		/// <summary>
		/// Event handler that is called when a tab's <see cref="TitleBarTab.Closing" /> event is fired, which removes the tab from <see cref="Tabs" /> and
		/// re-renders <see cref="_overlay" />.
		/// </summary>
		/// <param name="sender">Object from which this event originated (the <see cref="TitleBarTab" /> in this case).</param>
		/// <param name="e">Arguments associated with the event.</param>
		private void TitleBarTabs_Closing(object sender, CancelEventArgs e)
		{
			TitleBarTab tab = (TitleBarTab) sender;
			CloseTab(tab);

			if (!tab.Content.IsDisposed && AeroPeekEnabled)
			{
				TaskbarManager.Instance.TabbedThumbnail.RemoveThumbnailPreview(tab.Content);
			}

			if (_overlay != null)
			{
				_overlay.Render(true);
			}
		}

		/// <summary>
		/// Calls <see cref="TitleBarTabsOverlay.Render(bool)"/> on <see cref="_overlay"/> to force a redrawing of the tabs.
		/// </summary>
		public void RedrawTabs()
		{
			if (_overlay != null)
			{
				_overlay.Render(true);
			}
		}

		/// <summary>
		/// Overrides the <see cref="Control.SizeChanged" /> handler so that we can detect when the user has maximized or restored the window and adjust the size
		/// of the non-client area accordingly.
		/// </summary>
		/// <param name="e">Arguments associated with the event.</param>
		protected override void OnSizeChanged(EventArgs e)
		{
			// If no tab renderer has been set yet or the window state hasn't changed, don't do anything
			if (_previousWindowState != null && WindowState != _previousWindowState.Value)
			{
				SetFrameSize();
			}

			_previousWindowState = WindowState;

			base.OnSizeChanged(e);
		}

		/// <summary>Overrides the message processor for the window so that we can respond to windows events to render and manipulate the tabs properly.</summary>
		/// <param name="m">Message received by the pump.</param>
		protected override void WndProc(ref Message m)
		{
			bool callDwp = true;

			switch ((WM) m.Msg)
			{
					// When the window is activated, set the size of the non-client area appropriately
				case WM.WM_ACTIVATE:
					if ((m.WParam.ToInt64() & 0x0000FFFF) != 0)
					{
						SetFrameSize();
						ResizeTabContents();
						m.Result = IntPtr.Zero;
					}

					break;

				case WM.WM_NCHITTEST:
					// Call the base message handler to see where the user clicked in the window
					base.WndProc(ref m);

					HT hitResult = (HT) m.Result.ToInt32();

					// If they were over the minimize/maximize/close buttons or the system menu, let the message pass
					if (!(hitResult == HT.HTCLOSE || hitResult == HT.HTMINBUTTON || hitResult == HT.HTMAXBUTTON || hitResult == HT.HTMENU ||
					      hitResult == HT.HTSYSMENU))
					{
						m.Result = new IntPtr((int) HitTest(m));
					}

					callDwp = false;

					break;

					// Catch the case where the user is clicking the minimize button and use this opportunity to update the AeroPeek thumbnail for the current tab
				case WM.WM_NCLBUTTONDOWN:
					if (((HT) m.WParam.ToInt32()) == HT.HTMINBUTTON && AeroPeekEnabled && SelectedTab != null)
					{
						UpdateTabThumbnail(SelectedTab);
					}

					break;
			}

			if (callDwp)
			{
				base.WndProc(ref m);
			}
		}

		/// <summary>Calls <see cref="CreateTab" />, adds the resulting tab to the <see cref="Tabs" /> collection, and activates it.</summary>
		public virtual void AddNewTab()
		{
			TitleBarTab newTab = CreateTab();

			Tabs.Add(newTab);
			ResizeTabContents(newTab);

			SelectedTabIndex = _tabs.Count - 1;
		}

		/// <summary>Removes <paramref name="closingTab" /> from <see cref="Tabs" /> and selects the next applicable tab in the list.</summary>
		/// <param name="closingTab">Tab that is being closed.</param>
		protected virtual void CloseTab(TitleBarTab closingTab)
		{
			int removeIndex = Tabs.IndexOf(closingTab);
			int selectedTabIndex = SelectedTabIndex;

			Tabs.Remove(closingTab);

			if (selectedTabIndex > removeIndex)
			{
				SelectedTabIndex = selectedTabIndex - 1;
			}

			else if (selectedTabIndex == removeIndex)
			{
				SelectedTabIndex = Math.Min(selectedTabIndex, Tabs.Count - 1);
			}

			else
			{
				SelectedTabIndex = selectedTabIndex;
			}

			if (_previews.ContainsKey(closingTab.Content))
			{
				_previews[closingTab.Content].Dispose();
				_previews.Remove(closingTab.Content);
			}

			if (_previousActiveTab != null && closingTab.Content == _previousActiveTab.Content)
			{
				_previousActiveTab = null;
			}

			if (Tabs.Count == 0 && ExitOnLastTabClose)
			{
				Close();
			}
		}

		private HT HitTest(Message m)
		{
			// Get the point that the user clicked
			int lParam = (int) m.LParam;
			Point point = new Point(lParam & 0xffff, lParam >> 16);

			return HitTest(point, m.HWnd);
		}

		/// <summary>Called when a <see cref="WM.WM_NCHITTEST" /> message is received to see where in the non-client area the user clicked.</summary>
		/// <param name="point">Screen location that we are to test.</param>
		/// <param name="windowHandle">Handle to the window for which we are performing the test.</param>
		/// <returns>One of the <see cref="HT" /> values, depending on where the user clicked.</returns>
		private HT HitTest(Point point, IntPtr windowHandle)
		{
			RECT rect;

			User32.GetWindowRect(windowHandle, out rect);
			Rectangle area = new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);

			int row = 1;
			int column = 1;
			bool onResizeBorder = false;

			// Determine if we are on the top or bottom border
			if (point.Y >= area.Top && point.Y < area.Top + SystemInformation.VerticalResizeBorderThickness + _nonClientAreaHeight - 2)
			{
				onResizeBorder = point.Y < (area.Top + SystemInformation.VerticalResizeBorderThickness);
				row = 0;
			}

			else if (point.Y < area.Bottom && point.Y > area.Bottom - SystemInformation.VerticalResizeBorderThickness)
			{
				row = 2;
			}

			// Determine if we are on the left border or the right border
			if (point.X >= area.Left && point.X < area.Left + SystemInformation.HorizontalResizeBorderThickness)
			{
				column = 0;
			}

			else if (point.X < area.Right && point.X >= area.Right - SystemInformation.HorizontalResizeBorderThickness)
			{
				column = 2;
			}

			HT[,] hitTests =
			{
				{
					onResizeBorder
						? HT.HTTOPLEFT
						: HT.HTLEFT,
					onResizeBorder
						? HT.HTTOP
						: HT.HTCAPTION,
					onResizeBorder
						? HT.HTTOPRIGHT
						: HT.HTRIGHT
				},
				{
					HT.HTLEFT, HT.HTNOWHERE, HT.HTRIGHT
				},
				{
					HT.HTBOTTOMLEFT, HT.HTBOTTOM,
					HT.HTBOTTOMRIGHT
				}
			};

			return hitTests[row, column];
		}
	}
}