using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Win32Interop.Enums;

namespace EasyTabs
{
	/// <summary>
	/// Provides the base functionality for any tab renderer, taking care of actually rendering and detecting whether the cursor is over a tab.  Any custom
	/// tab renderer needs to inherit from this class, just as <see cref="ChromeTabRenderer" /> does.
	/// </summary>
	public abstract class BaseTabRenderer
	{
		bool? _isWindows10 = null;

		/// <summary>
		/// Background of the content area for the tab when the tab is active; its width also determines how wide the default content area for the tab
		/// is.
		/// </summary>
		protected Image _activeCenterImage;
		
		//<summary>Color of the title text. To change colors of the entire tab, change it from resource actively or permanently. </summary>
		protected Color ForeColor = Color.Black;

		/// <summary>Image to display on the left side of an active tab.</summary>
		protected Image _activeLeftSideImage;

		/// <summary>Image to display on the right side of an active tab.</summary>
		protected Image _activeRightSideImage;

		/// <summary>Area on the screen where the add button is located.</summary>
		protected Rectangle _addButtonArea;

		/// <summary>Image to display when the user hovers over the add button.</summary>
		protected Bitmap _addButtonHoverImage;

		/// <summary>Image to display for the add button when the user is not hovering over it.</summary>
		protected Bitmap _addButtonImage;

		/// <summary>The background, if any, that should be displayed in the non-client area behind the actual tabs.</summary>
		protected Image _background;

		/// <summary>The hover-over image that should be displayed on each tab to close that tab.</summary>
		protected Image _closeButtonHoverImage;

		/// <summary>The image that should be displayed on each tab to close that tab.</summary>
		protected Image _closeButtonImage;

		/// <summary>When the user is dragging a tab, this represents the point where the user first clicked to start the drag operation.</summary>
		protected Point? _dragStart = null;

		/// <summary>
		/// Background of the content area for the tab when the tab is inactive; its width also determines how wide the default content area for the tab
		/// is.
		/// </summary>
		protected Image _inactiveCenterImage;

		/// <summary>Image to display on the left side of an inactive tab.</summary>
		protected Image _inactiveLeftSideImage;

		/// <summary>Image to display on the right side of an inactive tab.</summary>
		protected Image _inactiveRightSideImage;

		/// <summary>Flag indicating whether or not a tab is being repositioned.</summary>
		protected bool _isTabRepositioning = false;

		/// <summary>Maximum area on the screen that tabs may take up for this application.</summary>
		protected Rectangle _maxTabArea = new Rectangle();

		/// <summary>The parent window that this renderer instance belongs to.</summary>
		protected TitleBarTabs _parentWindow;

		/// <summary>The number of tabs that were present when we last rendered; used to determine whether or not we need to redraw tab instances.</summary>
		protected int _previousTabCount;

		/// <summary>Flag indicating whether or not rendering has been suspended while we perform some operation.</summary>
		protected bool _suspendRendering = false;

		/// <summary>When the user is dragging a tab, this represents the horizontal offset within the tab where the user clicked to start the drag operation.</summary>
		protected int? _tabClickOffset = null;

		/// <summary>The width of the content area that we should use for each tab.</summary>
		protected int _tabContentWidth;

		/// <summary>Flag indicating whether or not a tab was being repositioned.</summary>
		protected bool _wasTabRepositioning = false;

		/// <summary>Default constructor that initializes the <see cref="_parentWindow" /> and <see cref="ShowAddButton" /> properties.</summary>
		/// <param name="parentWindow">The parent window that this renderer instance belongs to.</param>
		protected BaseTabRenderer(TitleBarTabs parentWindow)
		{
			_parentWindow = parentWindow;
			ShowAddButton = true;
			TabRepositionDragDistance = 10;
			TabTearDragDistance = 10;

			parentWindow.Tabs.CollectionModified += Tabs_CollectionModified;

			if (parentWindow._overlay != null)
			{
				parentWindow._overlay.MouseMove += Overlay_MouseMove;
				parentWindow._overlay.MouseUp += Overlay_MouseUp;
				parentWindow._overlay.MouseDown += Overlay_MouseDown;
			}
		}

		public bool IsWindows10
		{
			get
			{
				if (_isWindows10 == null)
				{
					RegistryKey reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
					string productName = (string)reg.GetValue("ProductName");

					_isWindows10 = productName.StartsWith("Windows 10");
				}

				return _isWindows10.Value;
			}
		}

		public virtual Font CaptionFont
        {
			get
            {
				return SystemFonts.CaptionFont;
            }
        }

		/// <summary>Height of the tab content area; derived from the height of <see cref="_activeCenterImage" />.</summary>
		public virtual int TabHeight
		{
			get
			{
				return _activeCenterImage.Height;
			}
		}

		public virtual int TopPadding
        {
			get
            {
				return 0;
            }
        }

		/// <summary>Flag indicating whether or not we should display the add button.</summary>
		public virtual bool ShowAddButton
		{
			get;
			set;
		}

		/// <summary>Amount of space we should put to the left of the caption when rendering the content area of the tab.</summary>
		public virtual int CaptionMarginLeft
		{
			get;
			set;
		}

		/// <summary>Amount of space that we should leave to the right of the caption when rendering the content area of the tab.</summary>
		public virtual int CaptionMarginRight
		{
			get;
			set;
		}

		/// <summary>Amount of space that we should leave between the top of the content area and the top of the caption text.</summary>
		public virtual int CaptionMarginTop
		{
			get;
			set;
		}

		/// <summary>Amount of space we should put to the left of the tab icon when rendering the content area of the tab.</summary>
		public virtual int IconMarginLeft
		{
			get;
			set;
		}

		/// <summary>Amount of space that we should leave to the right of the icon when rendering the content area of the tab.</summary>
		public virtual int IconMarginRight
		{
			get;
			set;
		}

		/// <summary>Amount of space that we should leave between the top of the content area and the top of the icon.</summary>
		public virtual int IconMarginTop
		{
			get;
			set;
		}

		/// <summary>Amount of space that we should put to the left of the close button when rendering the content area of the tab.</summary>
		public virtual int CloseButtonMarginLeft
		{
			get;
			set;
		}

		/// <summary>Amount of space that we should leave to the right of the close button when rendering the content area of the tab.</summary>
		public virtual int CloseButtonMarginRight
		{
			get;
			set;
		}

		/// <summary>Amount of space that we should leave between the top of the content area and the top of the close button.</summary>
		public virtual int CloseButtonMarginTop
		{
			get;
			set;
		}

		/// <summary>Amount of space that we should put to the left of the add tab button when rendering the content area of the tab.</summary>
		public virtual int AddButtonMarginLeft
		{
			get;
			set;
		}

		/// <summary>Amount of space that we should leave to the right of the add tab button when rendering the content area of the tab.</summary>
		public virtual int AddButtonMarginRight
		{
			get;
			set;
		}

		/// <summary>Amount of space that we should leave between the top of the content area and the top of the add tab button.</summary>
		public virtual int AddButtonMarginTop
		{
			get;
			set;
		}

		/// <summary>
		/// If the renderer overlaps the tabs (like Chrome), this is the width that the tabs should overlap by.  For renderers that do not overlap tabs (like
		/// Firefox), this should be left at 0.
		/// </summary>
		public virtual int OverlapWidth
		{
			get
			{
				return 0;
			}
		}

		/// <summary>Horizontal distance that a tab must be dragged before it starts to be repositioned.</summary>
		public virtual int TabRepositionDragDistance
		{
			get;
			set;
		}

		/// <summary>Distance that a user must drag a tab outside of the tab area before it shows up as "torn" from its parent window.</summary>
		public virtual int TabTearDragDistance
		{
			get;
			set;
		}

		/// <summary>Flag indicating whether or not a tab is being repositioned.</summary>
		public bool IsTabRepositioning
		{
			get
			{
				return _isTabRepositioning;
			}

			internal set
			{
				_isTabRepositioning = value;

				if (!_isTabRepositioning)
				{
					_dragStart = null;
				}
			}
		}

		/// <summary>Width of the content area of the tabs.</summary>
		public int TabContentWidth
		{
			get
			{
				return _tabContentWidth;
			}
		}

		/// <summary>Maximum area that the tabs can occupy.  Excludes the add button.</summary>
		public Rectangle MaxTabArea
		{
			get
			{
				return _maxTabArea;
			}
		}

        public virtual bool RendersEntireTitleBar
        {
            get
            {
                return false;
            }
        }

        public virtual HT NonClientHitTest(Message message, Point cursor)
        {
            return HT.HTCAPTION;
        }
        
        public virtual bool IsOverSizingBox(Point cursor)
        {
            return false;
        }

		/// <summary>Initialize the <see cref="_dragStart" /> and <see cref="_tabClickOffset" /> fields in case the user starts dragging a tab.</summary>
		/// <param name="sender">Object from which this event originated.</param>
		/// <param name="e">Arguments associated with the event.</param>
		protected internal virtual void Overlay_MouseDown(object sender, MouseEventArgs e)
		{
			_wasTabRepositioning = false;
			_dragStart = e.Location;
			_tabClickOffset = _parentWindow._overlay.GetRelativeCursorPosition(e.Location).X - _parentWindow.SelectedTab.Area.Location.X;
		}

		/// <summary>
		/// End the drag operation by resetting the <see cref="_dragStart" /> and <see cref="_tabClickOffset" /> fields and setting
		/// <see cref="IsTabRepositioning" /> to false.
		/// </summary>
		/// <param name="sender">Object from which this event originated.</param>
		/// <param name="e">Arguments associated with the event.</param>
		protected internal virtual void Overlay_MouseUp(object sender, MouseEventArgs e)
		{
			_dragStart = null;
			_tabClickOffset = null;

			_wasTabRepositioning = IsTabRepositioning;

			IsTabRepositioning = false;

			if (_wasTabRepositioning)
			{
				_parentWindow._overlay.Render(true);
			}
		}

		/// <summary>
		/// If the user is dragging the mouse, see if they have passed the <see cref="TabRepositionDragDistance" /> threshold and, if so, officially begin the
		/// tab drag operation.
		/// </summary>
		/// <param name="sender">Object from which this event originated.</param>
		/// <param name="e">Arguments associated with the event.</param>
		protected internal virtual void Overlay_MouseMove(object sender, MouseEventArgs e)
		{
			if (_dragStart != null && !IsTabRepositioning &&
				(Math.Abs(e.X - _dragStart.Value.X) > TabRepositionDragDistance || Math.Abs(e.Y - _dragStart.Value.Y) > TabRepositionDragDistance))
			{
				IsTabRepositioning = true;
			}
		}

		/// <summary>
		/// When items are added to the tabs collection, we need to ensure that the <see cref="_parentWindow" />'s minimum width is set so that we can display at
		/// least each tab and its close buttons.
		/// </summary>
		/// <param name="sender">List of tabs in the <see cref="_parentWindow" />.</param>
		/// <param name="e">Arguments associated with the event.</param>
		private void Tabs_CollectionModified(object sender, ListModificationEventArgs e)
		{
			ListWithEvents<TitleBarTab> tabs = (ListWithEvents<TitleBarTab>) sender;

			if (tabs.Count == 0)
			{
				return;
			}

			int minimumWidth = tabs.Sum(
				tab => GetTabLeftImage(tab).Width + GetTabRightImage(tab).Width + (tab.ShowCloseButton
						   ? tab.CloseButtonArea.Width + CloseButtonMarginLeft
						   : 0));

			minimumWidth += OverlapWidth;

			minimumWidth += (_parentWindow.ControlBox
								? SystemInformation.CaptionButtonSize.Width
								: 0) -
							(_parentWindow.MinimizeBox
								? SystemInformation.CaptionButtonSize.Width
								: 0) -
							(_parentWindow.MaximizeBox
								? SystemInformation.CaptionButtonSize.Width
								: 0) + (ShowAddButton
								? _addButtonImage.Width + AddButtonMarginLeft +
								  AddButtonMarginRight
								: 0);

			_parentWindow.MinimumSize = new Size(minimumWidth, 0);
		}

		/// <summary>
		/// Called from the <see cref="_parentWindow" /> to determine which, if any, of the <paramref name="tabs" /> the <paramref name="cursor" /> is
		/// over.
		/// </summary>
		/// <param name="tabs">The list of tabs that we should check.</param>
		/// <param name="cursor">The relative position of the cursor within the window.</param>
		/// <returns>The tab within <paramref name="tabs" /> that the <paramref name="cursor" /> is over; if none, then null is returned.</returns>
		public virtual TitleBarTab OverTab(IEnumerable<TitleBarTab> tabs, Point cursor)
		{
			TitleBarTab overTab = null;

			foreach (TitleBarTab tab in tabs.Where(tab => tab.TabImage != null))
			{
				// We have to loop through each of the tabs in turn and check their status; if the tabs overlap, then their areas overlap as well, which means 
				// that we may find see that the cursor is over an inactive tab, but we need to check the active tabs as well, since they may overlap their 
				// areas and take precedence.
				if (tab.Active && IsOverTab(tab, cursor))
				{
					overTab = tab;
					break;
				}

				if (IsOverTab(tab, cursor))
				{
					overTab = tab;
				}
			}

			return overTab;
		}

		/// <summary>
		/// Helper method to detect whether the <paramref name="cursor" /> is within the given <paramref name="area" /> and, if it is, whether it is over a
		/// non-transparent pixel in the given <paramref name="image" />.
		/// </summary>
		/// <param name="area">Screen area that we should check to see if the <paramref name="cursor" /> is within.</param>
		/// <param name="image">
		/// Image contained within <paramref name="area" /> that we should check to see if the <paramref name="cursor" /> is over a non-
		/// transparent pixel.
		/// </param>
		/// <param name="cursor">Current location of the cursor.</param>
		/// <returns>
		/// True if the <paramref name="cursor" /> is within the given <paramref name="area" /> and is over a non-transparent pixel in the
		/// <paramref name="image" />.
		/// </returns>
		protected bool IsOverNonTransparentArea(Rectangle area, Bitmap image, Point cursor)
		{
			if (!area.Contains(cursor))
			{
				return false;
			}

			// Get the relative location of the cursor within the image and then get the RGBA value of that pixel
			Point relativePoint = new Point(cursor.X - area.Location.X, cursor.Y - area.Location.Y);
			Color pixel = image.GetPixel(relativePoint.X, relativePoint.Y);

			// If the alpha channel of the pixel is greater than 0, then we're considered "over" the image
			return pixel.A > 0;
		}

		/// <summary>Tests whether the <paramref name="cursor" /> is hovering over the add tab button.</summary>
		/// <param name="cursor">Current location of the cursor.</param>
		/// <returns>
		/// True if the <paramref name="cursor" /> is within <see cref="_addButtonArea" /> and is over a non-transparent pixel of
		/// <see cref="_addButtonHoverImage" />, false otherwise.
		/// </returns>
		public virtual bool IsOverAddButton(Point cursor)
		{
			return !_wasTabRepositioning && IsOverNonTransparentArea(_addButtonArea, _addButtonHoverImage, cursor);
		}

		/// <summary>Tests whether the <paramref name="cursor" /> is hovering over the given <paramref name="tab" />.</summary>
		/// <param name="tab">Tab that we are to see if the cursor is hovering over.</param>
		/// <param name="cursor">Current location of the cursor.</param>
		/// <returns>
		/// True if the <paramref name="cursor" /> is within the <see cref="TitleBarTab.Area" /> of the <paramref name="tab" /> and is over a non- transparent
		/// pixel of <see cref="TitleBarTab.TabImage" />, false otherwise.
		/// </returns>
		protected virtual bool IsOverTab(TitleBarTab tab, Point cursor)
		{
			return IsOverNonTransparentArea(tab.Area, tab.TabImage, cursor);
		}

		/// <summary>Checks to see if the <paramref name="cursor" /> is over the <see cref="TitleBarTab.CloseButtonArea" /> of the given <paramref name="tab" />.</summary>
		/// <param name="tab">The tab whose <see cref="TitleBarTab.CloseButtonArea" /> we are to check to see if it contains <paramref name="cursor" />.</param>
		/// <param name="cursor">Current position of the cursor.</param>
		/// <returns>True if the <paramref name="tab" />'s <see cref="TitleBarTab.CloseButtonArea" /> contains <paramref name="cursor" />, false otherwise.</returns>
		public virtual bool IsOverCloseButton(TitleBarTab tab, Point cursor)
		{
			if (!tab.ShowCloseButton || _wasTabRepositioning)
			{
				return false;
			}

			Rectangle absoluteCloseButtonArea = new Rectangle(
				tab.Area.X + tab.CloseButtonArea.X, tab.Area.Y + tab.CloseButtonArea.Y, tab.CloseButtonArea.Width, tab.CloseButtonArea.Height);

			return absoluteCloseButtonArea.Contains(cursor);
		}

        protected virtual int GetMaxTabAreaWidth(List<TitleBarTab> tabs, Point offset)
        {
            return _parentWindow.ClientRectangle.Width - offset.X -
                        (ShowAddButton
                            ? _addButtonImage.Width + AddButtonMarginLeft + AddButtonMarginRight
                            : 0) - 
                        (tabs.Count * OverlapWidth) -
                        (_parentWindow.ControlBox
                            ? SystemInformation.CaptionButtonSize.Width
                            : 0) -
                        (_parentWindow.MinimizeBox
                            ? SystemInformation.CaptionButtonSize.Width
                            : 0) -
                        (_parentWindow.MaximizeBox
                            ? SystemInformation.CaptionButtonSize.Width
                            : 0);
        }

		/// <summary>Renders the list of <paramref name="tabs" /> to the screen using the given <paramref name="graphicsContext" />.</summary>
		/// <param name="tabs">List of tabs that we are to render.</param>
		/// <param name="graphicsContext">Graphics context that we should use while rendering.</param>
		/// <param name="cursor">Current location of the cursor on the screen.</param>
		/// <param name="forceRedraw">Flag indicating whether or not the redraw should be forced.</param>
		/// <param name="offset">Offset within <paramref name="graphicsContext" /> that the tabs should be rendered.</param>
		public virtual void Render(List<TitleBarTab> tabs, Graphics graphicsContext, Point offset, Point cursor, bool forceRedraw = false)
		{
			if (_suspendRendering)
			{
				return;
			}

			if (tabs == null || tabs.Count == 0)
			{
				return;
			}

			Point screenCoordinates = _parentWindow.PointToScreen(_parentWindow.ClientRectangle.Location);

			// Calculate the maximum tab area, excluding the add button and any minimize/maximize/close buttons in the window
			_maxTabArea.Location = new Point(SystemInformation.BorderSize.Width + offset.X + screenCoordinates.X, offset.Y + screenCoordinates.Y);
			_maxTabArea.Width = GetMaxTabAreaWidth(tabs, offset);
			_maxTabArea.Height = TabHeight;

			// Get the width of the content area for each tab by taking the parent window's client width, subtracting the left and right border widths and the 
			// add button area (if applicable) and then dividing by the number of tabs
			int tabContentWidth = Math.Min(_activeCenterImage.Width, Convert.ToInt32(Math.Floor(Convert.ToDouble(_maxTabArea.Width / tabs.Count))));

			// Determine if we need to redraw the TabImage properties for each tab by seeing if the content width that we calculated above is equal to content 
			// width we had in the previous rendering pass
			bool redraw = tabContentWidth != _tabContentWidth || forceRedraw;

			if (redraw)
			{
				_tabContentWidth = tabContentWidth;
			}

			int i = tabs.Count - 1;
			List<Tuple<TitleBarTab, int, Rectangle>> activeTabs = new List<Tuple<TitleBarTab, int, Rectangle>>();

			// Render the background image
			if (_background != null)
			{
				graphicsContext.DrawImage(_background, offset.X, offset.Y, _parentWindow.Width, TabHeight);
			}

			int selectedIndex = tabs.FindIndex(t => t.Active);
            Image tabCenterImage = null;

			if (selectedIndex != -1)
			{
				TitleBarTab selectedTab = tabs[selectedIndex];

				Image tabLeftImage = GetTabLeftImage(selectedTab);
				Image tabRightImage = GetTabRightImage(selectedTab);
				tabCenterImage = GetTabCenterImage(selectedTab);

				Rectangle tabArea = new Rectangle(
					SystemInformation.BorderSize.Width + offset.X +
					selectedIndex * (tabContentWidth + tabLeftImage.Width + tabRightImage.Width - OverlapWidth),
					offset.Y + (TabHeight - tabCenterImage.Height), tabContentWidth + tabLeftImage.Width + tabRightImage.Width,
					tabCenterImage.Height);

				if (IsTabRepositioning && _tabClickOffset != null)
				{
					// Make sure that the user doesn't move the tab past the beginning of the list or the outside of the window
					tabArea.X = cursor.X - _tabClickOffset.Value;
					tabArea.X = Math.Max(SystemInformation.BorderSize.Width + offset.X, tabArea.X);
					tabArea.X =
						Math.Min(
							SystemInformation.BorderSize.Width + (_parentWindow.WindowState == FormWindowState.Maximized
								? _parentWindow.ClientRectangle.Width - (_parentWindow.ControlBox
									? SystemInformation.CaptionButtonSize.Width
									: 0) -
								  (_parentWindow.MinimizeBox
									  ? SystemInformation.CaptionButtonSize.Width
									  : 0) -
								  (_parentWindow.MaximizeBox
									  ? SystemInformation.CaptionButtonSize.Width
									  : 0)
								: _parentWindow.ClientRectangle.Width) - tabArea.Width, tabArea.X);

					int dropIndex = 0;

					// Figure out which slot the active tab is being "dropped" over
					if (tabArea.X - SystemInformation.BorderSize.Width - offset.X - TabRepositionDragDistance > 0)
					{
						dropIndex =
							Math.Min(
								Convert.ToInt32(
									Math.Round(
										Convert.ToDouble(tabArea.X - SystemInformation.BorderSize.Width - offset.X - TabRepositionDragDistance) /
										Convert.ToDouble(tabArea.Width - OverlapWidth))), tabs.Count - 1);
					}

					// If the tab has been moved over another slot, move the tab object in the window's tab list
					if (dropIndex != selectedIndex)
					{
						TitleBarTab tab = tabs[selectedIndex];

						_parentWindow.Tabs.SuppressEvents();
						_parentWindow.Tabs.Remove(tab);
						_parentWindow.Tabs.Insert(dropIndex, tab);
						_parentWindow.Tabs.ResumeEvents();
					}
				}

				activeTabs.Add(new Tuple<TitleBarTab, int, Rectangle>(tabs[selectedIndex], selectedIndex, tabArea));
			}

			// Loop through the tabs in reverse order since we need the ones farthest on the left to overlap those to their right
			foreach (TitleBarTab tab in ((IEnumerable<TitleBarTab>) tabs).Reverse())
			{
				Image tabLeftImage = GetTabLeftImage(tab);
				tabCenterImage = GetTabCenterImage(tab);
				Image tabRightImage = GetTabRightImage(tab);

				Rectangle tabArea =
					new Rectangle(
						SystemInformation.BorderSize.Width + offset.X +
						(i * (tabContentWidth + tabLeftImage.Width + tabRightImage.Width - OverlapWidth)),
						offset.Y + (TabHeight - tabCenterImage.Height), tabContentWidth + tabLeftImage.Width + tabRightImage.Width,
						tabCenterImage.Height);

				// If we need to redraw the tab image, null out the property so that it will be recreated in the call to Render() below
				if (redraw)
				{
					tab.TabImage = null;
				}

				// In this first pass, we only render the inactive tabs since we need the active tabs to show up on top of everything else
				if (!tab.Active)
				{
					Render(graphicsContext, tab, i, tabArea, cursor, tabLeftImage, tabCenterImage, tabRightImage);
				}

				i--;
			}

			// In the second pass, render all of the active tabs identified in the previous pass
			foreach (Tuple<TitleBarTab, int, Rectangle> tab in activeTabs)
			{
				Image tabLeftImage = GetTabLeftImage(tab.Item1);
				tabCenterImage = GetTabCenterImage(tab.Item1);
				Image tabRightImage = GetTabRightImage(tab.Item1);

				Render(graphicsContext, tab.Item1, tab.Item2, tab.Item3, cursor, tabLeftImage, tabCenterImage, tabRightImage);
			}

			_previousTabCount = tabs.Count;

			// Render the add tab button to the screen
			if (ShowAddButton && !IsTabRepositioning)
			{
				_addButtonArea =
					new Rectangle(
						(_previousTabCount *
						 (tabContentWidth + _activeLeftSideImage.Width + _activeRightSideImage.Width - OverlapWidth)) +
						_activeRightSideImage.Width + AddButtonMarginLeft + offset.X,
						AddButtonMarginTop + offset.Y + (TabHeight - tabCenterImage.Height), _addButtonImage.Width, _addButtonImage.Height);

				bool cursorOverAddButton = IsOverAddButton(cursor);

				graphicsContext.DrawImage(
					cursorOverAddButton
						? _addButtonHoverImage
						: _addButtonImage, _addButtonArea, 0, 0, cursorOverAddButton
							? _addButtonHoverImage.Width
							: _addButtonImage.Width,
					cursorOverAddButton
						? _addButtonHoverImage.Height
						: _addButtonImage.Height, GraphicsUnit.Pixel);
			}
		}

		/// <summary>Internal method for rendering an individual <paramref name="tab" /> to the screen.</summary>
		/// <param name="graphicsContext">Graphics context to use when rendering the tab.</param>
		/// <param name="tab">Individual tab that we are to render.</param>
		/// <param name="area">Area of the screen that the tab should be rendered to.</param>
		/// <param name="cursor">Current position of the cursor.</param>
		/// <param name="tabLeftImage">Image to use for the left side of the tab.</param>
		/// <param name="tabCenterImage">Image to use for the center of the tab.</param>
		/// <param name="tabRightImage">Image to use for the right side of the tab.</param>
		protected virtual void Render(Graphics graphicsContext, TitleBarTab tab, int index, Rectangle area, Point cursor, Image tabLeftImage, Image tabCenterImage, Image tabRightImage)
		{
			if (_suspendRendering)
			{
				return;
			}

			// If we need to redraw the tab image
			if (tab.TabImage == null)
			{
				// We render the tab to an internal property so that we don't necessarily have to redraw it in every rendering pass, only if its width or 
				// status have changed
				tab.TabImage = new Bitmap(area.Width <= 0 ? 1 : area.Width, tabCenterImage.Height <= 0 ? 1 : tabCenterImage.Height);

				using (Graphics tabGraphicsContext = Graphics.FromImage(tab.TabImage))
				{
					// Draw the left, center, and right portions of the tab
					tabGraphicsContext.DrawImage(
						tabLeftImage, new Rectangle(0, 0, tabLeftImage.Width, tabLeftImage.Height), 0, 0, tabLeftImage.Width, tabLeftImage.Height, GraphicsUnit.Pixel);

					tabGraphicsContext.DrawImage(
						tabCenterImage, new Rectangle(tabLeftImage.Width, 0, _tabContentWidth, tabCenterImage.Height), 0, 0, _tabContentWidth, tabCenterImage.Height,
						GraphicsUnit.Pixel);

					tabGraphicsContext.DrawImage(
						tabRightImage, new Rectangle(tabLeftImage.Width + _tabContentWidth, 0, tabRightImage.Width, tabRightImage.Height), 0, 0, tabRightImage.Width,
						tabRightImage.Height, GraphicsUnit.Pixel);

					// Draw the close button
					if (tab.ShowCloseButton)
					{
						Image closeButtonImage = IsOverCloseButton(tab, cursor)
							? _closeButtonHoverImage
							: _closeButtonImage;

						tab.CloseButtonArea = new Rectangle(
							area.Width - tabRightImage.Width - CloseButtonMarginRight - closeButtonImage.Width, CloseButtonMarginTop, closeButtonImage.Width,
							closeButtonImage.Height);

						tabGraphicsContext.DrawImage(
							closeButtonImage, tab.CloseButtonArea, 0, 0,
							closeButtonImage.Width, closeButtonImage.Height,
							GraphicsUnit.Pixel);
					}
				}

				tab.Area = area;
			}

			// Render the tab's saved image to the screen
			graphicsContext.DrawImage(
				tab.TabImage, area, 0, 0, tab.TabImage.Width, tab.TabImage.Height,
				GraphicsUnit.Pixel);

			// Render the icon for the tab's content, if it exists and there's room for it in the tab's content area
			if (tab.Content.ShowIcon && _tabContentWidth > 16 + IconMarginLeft + (tab.ShowCloseButton
				? CloseButtonMarginLeft +
				  tab.CloseButtonArea.Width +
				  CloseButtonMarginRight
				: 0))
			{
				graphicsContext.DrawIcon(
					new Icon(tab.Content.Icon, 16, 16),
					new Rectangle(area.X + OverlapWidth + IconMarginLeft, IconMarginTop + area.Y, 16, 16));
			}

			// Render the caption for the tab's content if there's room for it in the tab's content area
			if (_tabContentWidth > (tab.Content.ShowIcon
				? 16 + IconMarginLeft + IconMarginRight
				: 0) + CaptionMarginLeft + CaptionMarginRight + (tab.ShowCloseButton
					? CloseButtonMarginLeft +
					  tab.CloseButtonArea.Width +
					  CloseButtonMarginRight
					: 0))
			{
				graphicsContext.DrawString(
					tab.Caption, CaptionFont, new SolidBrush(ForeColor),
					new Rectangle(
						area.X + OverlapWidth + CaptionMarginLeft + (tab.Content.ShowIcon
							? IconMarginLeft +
							  16 +
							  IconMarginRight
							: 0),
						CaptionMarginTop + area.Y,
						_tabContentWidth - (tab.Content.ShowIcon
							? IconMarginLeft + 16 + IconMarginRight
							: 0) - (tab.ShowCloseButton
								? _closeButtonImage.Width +
								  CloseButtonMarginRight +
								  CloseButtonMarginLeft
								: 0), tab.TabImage.Height),
					new StringFormat(StringFormatFlags.NoWrap)
					{
						Trimming = StringTrimming.EllipsisCharacter
					});
			}
		}

		/// <summary>
		/// Gets the image to use for the left side of the <paramref name="tab"/>.
		/// </summary>
		/// <param name="tab">Tab that we are retrieving the image for.</param>
		/// <returns>The image for the left side of <paramref name="tab"/>.</returns>
		protected virtual Image GetTabLeftImage(TitleBarTab tab)
		{
			return tab.Active
				? _activeLeftSideImage
				: _inactiveLeftSideImage;
		}

		/// <summary>
		/// Gets the image to use for the center of the <paramref name="tab"/>.
		/// </summary>
		/// <param name="tab">Tab that we are retrieving the image for.</param>
		/// <returns>The image for the center of <paramref name="tab"/>.</returns>
		protected virtual Image GetTabCenterImage(TitleBarTab tab)
		{
			return tab.Active
				? _activeCenterImage
				: _inactiveCenterImage;
		}

		/// <summary>
		/// Gets the image to use for the right side of the <paramref name="tab"/>.
		/// </summary>
		/// <param name="tab">Tab that we are retrieving the image for.</param>
		/// <returns>The image for the right side of <paramref name="tab"/>.</returns>
		protected virtual Image GetTabRightImage(TitleBarTab tab)
		{
			return tab.Active
				? _activeRightSideImage
				: _inactiveRightSideImage;
		}

		/// <summary>
		/// Called when a torn tab is dragged into the <see cref="TitleBarTabs.TabDropArea" /> of <see cref="_parentWindow" />.  Places the tab in the list and
		/// sets <see cref="IsTabRepositioning" /> to true to simulate the user continuing to drag the tab around in the window.
		/// </summary>
		/// <param name="tab">Tab that was dragged into this window.</param>
		/// <param name="cursorLocation">Location of the user's cursor.</param>
		internal virtual void CombineTab(TitleBarTab tab, Point cursorLocation)
		{
			// Stop rendering to prevent weird stuff from happening like the wrong tab being focused
			_suspendRendering = true;

			// Find out where to insert the tab in the list
			int dropIndex = _parentWindow.Tabs.FindIndex(t => t.Area.Left <= cursorLocation.X && t.Area.Right >= cursorLocation.X);

			// Simulate the user having clicked in the middle of the tab when they started dragging it so that the tab will move correctly within the window
			// when the user continues to move the mouse
			if (_parentWindow.Tabs.Count > 0)
			{
				_tabClickOffset = _parentWindow.Tabs.First().Area.Width / 2;
			}
			else
			{
				_tabClickOffset = 0;
			}
			IsTabRepositioning = true;

			tab.Parent = _parentWindow;

			if (dropIndex == -1)
			{
				_parentWindow.Tabs.Add(tab);
				dropIndex = _parentWindow.Tabs.Count - 1;
			}

			else
			{
				_parentWindow.Tabs.Insert(dropIndex, tab);
			}

			// Resume rendering
			_suspendRendering = false;

			_parentWindow.SelectedTabIndex = dropIndex;
			_parentWindow.ResizeTabContents();
		}
	}
}
