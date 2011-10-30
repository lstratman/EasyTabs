using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Stratman.Windows.Forms.TitleBarTabs
{
    /// <summary>
    ///   Provides the base functionality for any tab renderer, taking care of actually rendering and detecting whether 
    ///   the cursor is over a tab.  Any custom tab renderer needs to inherit from this class, just as 
    ///   <see cref = "ChromeTabRenderer" /> does.
    /// </summary>
    public abstract class BaseTabRenderer
    {
        /// <summary>
        ///   Background of the content area for the tab when the tab is active; its width also determines how wide the
        ///   default content area for the tab is.
        /// </summary>
        protected Image _activeCenterImage;

        /// <summary>
        ///   Image to display on the left side of an active tab.
        /// </summary>
        protected Image _activeLeftSideImage;

        /// <summary>
        ///   Image to display on the right side of an active tab.
        /// </summary>
        protected Image _activeRightSideImage;

        /// <summary>
        ///   Area on the screen where the add button is located.
        /// </summary>
        protected Rectangle _addButtonArea;

        /// <summary>
        ///   Image to display when the user hovers over the add button.
        /// </summary>
        protected Bitmap _addButtonHoverImage;

        /// <summary>
        ///   Image to display for the add button when the user is not hovering over it.
        /// </summary>
        protected Bitmap _addButtonImage;

        /// <summary>
        ///   The background, if any, that should be displayed in the non-client area behind the actual tabs.
        /// </summary>
        protected Image _background;

        /// <summary>
        ///   The hover-over image that should be displayed on each tab to close that tab.
        /// </summary>
        protected Image _closeButtonHoverImage;

        /// <summary>
        ///   The image that should be displayed on each tab to close that tab.
        /// </summary>
        protected Image _closeButtonImage;

        /// <summary>
        ///   Background of the content area for the tab when the tab is inactive; its width also determines how wide
        ///   the default content area for the tab is.
        /// </summary>
        protected Image _inactiveCenterImage;

        /// <summary>
        ///   Image to display on the left side of an inactive tab.
        /// </summary>
        protected Image _inactiveLeftSideImage;

        /// <summary>
        ///   Image to display on the right side of an inactive tab.
        /// </summary>
        protected Image _inactiveRightSideImage;

        /// <summary>
        ///   The parent window that this renderer instance belongs to.
        /// </summary>
        protected TitleBarTabs _parentWindow;

        /// <summary>
        ///   The number of tabs that were present when we last rendered; used to determine whether or not we need to
        ///   redraw tab instances.
        /// </summary>
        protected int _previousTabCount;

        /// <summary>
        ///   The width of the content area that we should use for each tab.
        /// </summary>
        protected int _tabContentWidth;

        /// <summary>
        ///   Default constructor that initializes the <see cref = "_parentWindow" /> and <see cref = "ShowAddButton" />
        ///   properties.
        /// </summary>
        /// <param name = "parentWindow">The parent window that this renderer instance belongs to.</param>
        protected BaseTabRenderer(TitleBarTabs parentWindow)
        {
            _parentWindow = parentWindow;
            ShowAddButton = true;
        }

        /// <summary>
        ///   Height of the tab content area; derived from the height of <see cref = "_activeCenterImage" />.
        /// </summary>
        public virtual int TabHeight
        {
            get
            {
                return _activeCenterImage.Height;
            }
        }

        /// <summary>
        ///   Flag indicating whether or not we should display the add button.
        /// </summary>
        public bool ShowAddButton
        {
            get;
            set;
        }

        /// <summary>
        ///   Amount of space we should put to the left of the caption when rendering the content area of the tab.
        /// </summary>
        public int CaptionMarginLeft
        {
            get;
            set;
        }

        /// <summary>
        ///   Amount of space that we should leave to the right of the caption when rendering the content area of the
        ///   tab.
        /// </summary>
        public int CaptionMarginRight
        {
            get;
            set;
        }

        /// <summary>
        ///   Amount of space that we should leave between the top of the content area and the top of the caption text.
        /// </summary>
        public int CaptionMarginTop
        {
            get;
            set;
        }

        /// <summary>
        ///   Amount of space we should put to the left of the tab icon when rendering the content area of the tab.
        /// </summary>
        public int IconMarginLeft
        {
            get;
            set;
        }

        /// <summary>
        ///   Amount of space that we should leave to the right of the icon when rendering the content area of the tab.
        /// </summary>
        public int IconMarginRight
        {
            get;
            set;
        }

        /// <summary>
        ///   Amount of space that we should leave between the top of the content area and the top of the icon.
        /// </summary>
        public int IconMarginTop
        {
            get;
            set;
        }

        /// <summary>
        ///   Amount of space that we should put to the left of the close button when rendering the content area of the
        ///   tab.
        /// </summary>
        public int CloseButtonMarginLeft
        {
            get;
            set;
        }

        /// <summary>
        ///   Amount of space that we should leave to the right of the close button when rendering the content area of
        ///   the tab.
        /// </summary>
        public int CloseButtonMarginRight
        {
            get;
            set;
        }

        /// <summary>
        ///   Amount of space that we should leave between the top of the content area and the top of the close button.
        /// </summary>
        public int CloseButtonMarginTop
        {
            get;
            set;
        }

        /// <summary>
        ///   Amount of space that we should put to the left of the add tab button when rendering the content area of
        ///   the tab.
        /// </summary>
        public int AddButtonMarginLeft
        {
            get;
            set;
        }

        /// <summary>
        ///   Amount of space that we should leave to the right of the add tab button when rendering the content area of
        ///   the tab.
        /// </summary>
        public int AddButtonMarginRight
        {
            get;
            set;
        }

        /// <summary>
        ///   Amount of space that we should leave between the top of the content area and the top of the add tab
        ///   button.
        /// </summary>
        public int AddButtonMarginTop
        {
            get;
            set;
        }

        /// <summary>
        ///   If the renderer overlaps the tabs (like Chrome), this is the width that the tabs should overlap by.  For 
        ///   renderers that do not overlap tabs (like Firefox), this should be left at 0.
        /// </summary>
        public virtual int OverlapWidth
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        ///   Called from the <see cref = "_parentWindow" /> to determine which, if any, of the <see cref = "tabs" />
        ///   the <see cref = "cursor" /> is over.
        /// </summary>
        /// <param name = "tabs">The list of tabs that we should check.</param>
        /// <param name = "cursor">The relative position of the cursor within the window.</param>
        /// <returns>The tab within <see cref = "tabs" /> that the <see cref = "cursor" /> is over; if none, then null
        ///   is returned.</returns>
        public virtual TitleBarTab OverTab(IEnumerable<TitleBarTab> tabs, Point cursor)
        {
            TitleBarTab overTab = null;

            foreach (TitleBarTab tab in tabs)
            {
                // We have to loop through each of the tabs in turn and check their status; if the tabs overlap, then
                // their areas overlap as well, which means that we may find see that the cursor is over an inactive
                // tab, but we need to check the active tabs as well, since they may overlap their areas and take
                // precedence.
                if (tab.Active && IsOverTab(tab, cursor))
                {
                    overTab = tab;
                    break;
                }

                if (IsOverTab(tab, cursor))
                    overTab = tab;
            }

            return overTab;
        }

        /// <summary>
        ///   Helper method to detect whether the <see cref = "cursor" /> is within the given <see cref = "area" /> and, 
        ///   if it is, whether it is over a non-transparent pixel in the given <see cref = "image" />.
        /// </summary>
        /// <param name = "area">Screen area that we should check to see if the <see cref = "cursor" /> is 
        ///   within.</param>
        /// <param name = "image">Image contained within <see cref = "area" /> that we should check to see if the
        ///   <see cref = "cursor" /> is over a non-transparent pixel.</param>
        /// <param name = "cursor">Current location of the cursor.</param>
        /// <returns>True if the <see cref = "cursor" /> is within the given <see cref = "area" /> and is over a
        ///   non-transparent pixel in the <see cref = "Image" />.</returns>
        protected bool IsOverNonTransparentArea(Rectangle area, Bitmap image, Point cursor)
        {
            if (!area.Contains(cursor))
                return false;

            // Get the relative location of the cursor within the image and then get the RGBA value of that pixel
            Point relativePoint = new Point(cursor.X - area.Location.X, cursor.Y - area.Location.Y);
            Color pixel = image.GetPixel(relativePoint.X, relativePoint.Y);

            // If the alpha channel of the pixel is greater than 0, then we're considered "over" the image
            return pixel.A > 0;
        }

        /// <summary>
        ///   Tests whether the <see cref = "cursor" /> is hovering over the add tab button.
        /// </summary>
        /// <param name = "cursor">Current location of the cursor.</param>
        /// <returns>True if the <see cref = "cursor" /> is within <see cref = "_addButtonArea" /> and is over a
        ///   non-transparent pixel of <see cref = "_addButtonHoverImage" />, false otherwise.</returns>
        public virtual bool IsOverAddButton(Point cursor)
        {
            return IsOverNonTransparentArea(_addButtonArea, _addButtonHoverImage, cursor);
        }

        /// <summary>
        ///   Tests whether the <see cref = "cursor" /> is hovering over the given <see cref = "tab" />.
        /// </summary>
        /// <param name = "tab">Tab that we are to see if the cursor is hovering over.</param>
        /// <param name = "cursor">Current location of the cursor.</param>
        /// <returns>True if the <see cref = "cursor" /> is within the <see cref = "TitleBarTab.Area" /> of the
        ///   <see cref = "tab" /> and is over a non-transparent pixel of <see cref = "TitleBarTab.TabImage" />,
        ///   false otherwise.</returns>
        protected virtual bool IsOverTab(TitleBarTab tab, Point cursor)
        {
            return IsOverNonTransparentArea(tab.Area, tab.TabImage, cursor);
        }

        /// <summary>
        ///   Renders the list of <see cref = "tabs" /> to the screen using the given <see cref = "graphicsContext" />.
        /// </summary>
        /// <param name = "tabs">List of tabs that we are to render.</param>
        /// <param name = "graphicsContext">Graphics context that we should use while rendering.</param>
        /// <param name = "cursor">Current location of the cursor on the screen.</param>
        public virtual void Render(List<TitleBarTab> tabs, Graphics graphicsContext, Point cursor)
        {
            // Get the width of the content area for each tab by taking the parent window's client width, subtracting
            // the left and right border widths and the add button area (if applicable) and then dividing by the number
            // of tabs
            int tabContentWidth = Math.Min(_activeCenterImage.Width,
                                           Convert.ToInt32(
                                               Math.Floor(
                                                   Convert.ToDouble((_parentWindow.ClientRectangle.Width -
                                                                     (ShowAddButton
                                                                          ? _addButtonImage.Width + AddButtonMarginLeft +
                                                                            AddButtonMarginRight
                                                                          : 0) - (tabs.Count() * OverlapWidth) -
                                                                     OverlapWidth - _parentWindow.BorderLeft -
                                                                     _parentWindow.BorderRight) / tabs.Count()))));

            // Determine if we need to redraw the TabImage properties for each tab by seeing if the content width that
            // we calculated above is equal to content width we had in the previous rendering pass
            bool redraw = (tabContentWidth != _tabContentWidth);

            if (redraw)
                _tabContentWidth = tabContentWidth;

            int i = tabs.Count - 1;
            List<Tuple<TitleBarTab, Rectangle>> activeTabs = new List<Tuple<TitleBarTab, Rectangle>>();

            // Render the background image
            if (_background != null)
            {
                graphicsContext.DrawImage(_background, 6, _parentWindow.WindowState != FormWindowState.Maximized
                                                              ? 21
                                                              : 8,
                                          _parentWindow.ClientRectangle.Width - (_parentWindow.BorderLeft - 1) -
                                          (_parentWindow.BorderRight - 1),
                                          _activeCenterImage.Height);
            }

            // Loop through the tabs in reverse order since we need the ones farthest on the left to overlap those to
            // their right
            foreach (TitleBarTab tab in ((IEnumerable<TitleBarTab>) tabs).Reverse())
            {
                Rectangle tabArea =
                    new Rectangle(
                        _parentWindow.BorderLeft +
                        (i * (tabContentWidth + _activeLeftSideImage.Width + _activeRightSideImage.Width - OverlapWidth)),
                        _parentWindow.WindowState != FormWindowState.Maximized
                            ? 21
                            : 8, tabContentWidth + _activeLeftSideImage.Width + _activeRightSideImage.Width,
                        _activeCenterImage.Height);

                // If we need to redraw the tab image, null out the property so that it will be recreated in the call
                // to Render() below
                if (redraw)
                    tab.TabImage = null;

                // In this first pass, we only render the inactive tabs since we need the active tabs to show up on top
                // of everything else
                if (!tab.Active)
                    Render(graphicsContext, tab, tabArea);

                else
                    activeTabs.Add(new Tuple<TitleBarTab, Rectangle>(tab, tabArea));

                i--;
            }

            // In the second pass, render all of the active tabs identified in the previous pass
            foreach (Tuple<TitleBarTab, Rectangle> tab in activeTabs)
                Render(graphicsContext, tab.Item1, tab.Item2);

            _previousTabCount = tabs.Count;

            // Render the add tab button to the screen
            if (ShowAddButton)
            {
                _addButtonArea =
                    new Rectangle(
                        7 +
                        (_previousTabCount *
                         (tabContentWidth + _activeLeftSideImage.Width + _activeRightSideImage.Width - OverlapWidth)) +
                        _activeRightSideImage.Width + AddButtonMarginLeft,
                        (_parentWindow.WindowState != FormWindowState.Maximized
                             ? 21
                             : 8) + AddButtonMarginTop, _addButtonImage.Width, _addButtonImage.Height);

                bool cursorOverAddButton = IsOverAddButton(cursor);

                graphicsContext.DrawImage(cursorOverAddButton
                                              ? _addButtonHoverImage
                                              : _addButtonImage, _addButtonArea, 0, 0, cursorOverAddButton
                                                                                           ? _addButtonHoverImage.Width
                                                                                           : _addButtonImage.Width,
                                          cursorOverAddButton
                                              ? _addButtonHoverImage.Height
                                              : _addButtonImage.Height, GraphicsUnit.Pixel);
            }
        }

        /// <summary>
        ///   Internal method for rendering an individual <see cref = "tab" /> to the screen.
        /// </summary>
        /// <param name = "graphicsContext">Graphics context to use when rendering the tab.</param>
        /// <param name = "tab">Individual tab that we are to render.</param>
        /// <param name = "area">Area of the screen that the tab should be rendered to.</param>
        protected virtual void Render(Graphics graphicsContext, TitleBarTab tab, Rectangle area)
        {
            // If we need to redraw the tab image
            if (tab.TabImage == null)
            {
                // We render the tab to an internal property so that we don't necessarily have to redraw it in every
                // rendering pass, only if its width or status have changed
                tab.TabImage = new Bitmap(area.Width, tab.Active
                                                          ? _activeCenterImage.Height
                                                          : _inactiveCenterImage.Height);

                using (Graphics tabGraphicsContext = Graphics.FromImage(tab.TabImage))
                {
                    // Draw the left, center, and right portions of the tab
                    tabGraphicsContext.DrawImage(tab.Active
                                                     ? _activeLeftSideImage
                                                     : _inactiveLeftSideImage, new Rectangle(0, 0, tab.Active
                                                                                                       ? _activeLeftSideImage
                                                                                                             .Width
                                                                                                       : _inactiveLeftSideImage
                                                                                                             .Width,
                                                                                             tab.Active
                                                                                                 ? _activeLeftSideImage.
                                                                                                       Height
                                                                                                 : _inactiveLeftSideImage
                                                                                                       .Height), 0, 0,
                                                 tab.Active
                                                     ? _activeLeftSideImage.Width
                                                     : _inactiveLeftSideImage.Width, tab.Active
                                                                                         ? _activeLeftSideImage.Height
                                                                                         : _inactiveLeftSideImage.Height,
                                                 GraphicsUnit.Pixel);
                    tabGraphicsContext.DrawImage(tab.Active
                                                     ? _activeCenterImage
                                                     : _inactiveCenterImage, new Rectangle((tab.Active
                                                                                                ? _activeLeftSideImage.
                                                                                                      Width
                                                                                                : _inactiveLeftSideImage
                                                                                                      .Width), 0,
                                                                                           _tabContentWidth, tab.Active
                                                                                                                 ? _activeCenterImage
                                                                                                                       .
                                                                                                                       Height
                                                                                                                 : _inactiveCenterImage
                                                                                                                       .
                                                                                                                       Height),
                                                 0, 0, _tabContentWidth, tab.Active
                                                                             ? _activeCenterImage.Height
                                                                             : _inactiveCenterImage.Height,
                                                 GraphicsUnit.Pixel);
                    tabGraphicsContext.DrawImage(tab.Active
                                                     ? _activeRightSideImage
                                                     : _inactiveRightSideImage, new Rectangle((tab.Active
                                                                                                   ? _activeLeftSideImage
                                                                                                         .Width
                                                                                                   : _inactiveLeftSideImage
                                                                                                         .Width) +
                                                                                              _tabContentWidth, 0,
                                                                                              tab.Active
                                                                                                  ? _activeRightSideImage
                                                                                                        .Width
                                                                                                  : _inactiveRightSideImage
                                                                                                        .Width,
                                                                                              tab.Active
                                                                                                  ? _activeRightSideImage
                                                                                                        .Height
                                                                                                  : _inactiveRightSideImage
                                                                                                        .Height), 0, 0,
                                                 tab.Active
                                                     ? _activeRightSideImage.Width
                                                     : _inactiveRightSideImage.Width, tab.Active
                                                                                          ? _activeRightSideImage.Height
                                                                                          : _inactiveRightSideImage.
                                                                                                Height,
                                                 GraphicsUnit.Pixel);

                    // Draw the close button
                    if (tab.ShowCloseButton)
                    {
                        tab.CloseButtonArea = new Rectangle(area.Width - (tab.Active
                                                                              ? _activeRightSideImage.Width
                                                                              : _inactiveRightSideImage.Width) -
                                                            CloseButtonMarginRight - _closeButtonImage.Width,
                                                            CloseButtonMarginTop, _closeButtonImage.Width,
                                                            _closeButtonImage.Height);
                        tabGraphicsContext.DrawImage(_closeButtonImage, tab.CloseButtonArea, 0, 0,
                                                     _closeButtonImage.Width, _closeButtonImage.Height,
                                                     GraphicsUnit.Pixel);
                    }
                }

                tab.Area = area;
            }

            // Render the tab's saved image to the screen
            graphicsContext.DrawImage(tab.TabImage, area, 0, 0, tab.TabImage.Width, tab.TabImage.Height,
                                      GraphicsUnit.Pixel);

            // Render the icon for the tab's content, if any
            if (tab.Content.ShowIcon)
            {
                graphicsContext.DrawIcon(new Icon(tab.Content.Icon, 16, 16),
                                         new Rectangle(area.X + OverlapWidth + IconMarginLeft,
                                                       (_parentWindow.WindowState != FormWindowState.Maximized
                                                            ? 21
                                                            : 8) + IconMarginTop, 16, 16));
            }

            // Render the caption for the tab's content
            graphicsContext.DrawString(tab.Caption, SystemFonts.CaptionFont, Brushes.Black,
                                       new Rectangle(area.X + OverlapWidth + CaptionMarginLeft + (tab.Content.ShowIcon
                                                                                                      ? IconMarginLeft +
                                                                                                        16 +
                                                                                                        IconMarginRight
                                                                                                      : 0),
                                                     (_parentWindow.WindowState != FormWindowState.Maximized
                                                          ? 21
                                                          : 8) + CaptionMarginTop,
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
}