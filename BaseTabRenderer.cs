using System;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace Stratman.Windows.Forms.TitleBarTabs
{
    public abstract class BaseTabRenderer
    {
        protected Image _activeLeftSideImage = null;
        protected Image _activeRightSideImage = null;
        protected Image _activeCenterImage = null;
        protected Image _inactiveLeftSideImage = null;
        protected Image _inactiveRightSideImage = null;
        protected Image _inactiveCenterImage = null;
        protected Image _closeButtonImage = null;
        protected Image _closeButtonHoverImage = null;
        protected Image _background = null;
        protected Bitmap _addButtonImage = null;
        protected Bitmap _addButtonHoverImage = null;
        protected TitleBarTabs _parentWindow = null;
        protected int _tabContentWidth = 0;
        protected Rectangle _addButtonArea = new Rectangle();
        protected int _previousTabCount = 0;

        public BaseTabRenderer(TitleBarTabs parentWindow)
        {
            _parentWindow = parentWindow;
            ShowAddButton = true;
        }

        public virtual int TabHeight
        {
            get
            {
                return _activeCenterImage.Height;
            }
        }

        public bool ShowAddButton
        {
            get;
            set;
        }

        public int CaptionMarginLeft
        {
            get;
            set;
        }

        public int CaptionMarginRight
        {
            get;
            set;
        }

        public int CaptionMarginTop
        {
            get;
            set;
        }

        public int IconMarginLeft
        {
            get;
            set;
        }

        public int IconMarginRight
        {
            get;
            set;
        }

        public int IconMarginTop
        {
            get;
            set;
        }

        public int CloseButtonMarginLeft
        {
            get;
            set;
        }

        public int CloseButtonMarginRight
        {
            get;
            set;
        }

        public int CloseButtonMarginTop
        {
            get;
            set;
        }

        public int AddButtonMarginLeft
        {
            get;
            set;
        }

        public int AddButtonMarginRight
        {
            get;
            set;
        }

        public int AddButtonMarginTop
        {
            get;
            set;
        }

        public virtual int OverlapWidth
        {
            get
            {
                return 0;
            }
        }

        public virtual TitleBarTab OverTab(IEnumerable<TitleBarTab> tabs, Point cursor)
        {
            TitleBarTab overTab = null;

            foreach (TitleBarTab tab in tabs)
            {
                if (tab.Active && IsOverTab(tab, cursor))
                {
                    overTab = tab;
                    break;
                }

                else if (IsOverTab(tab, cursor))
                    overTab = tab;
            }

            return overTab;
        }

        protected bool IsOverNonTransparentArea(Rectangle area, Bitmap image, Point cursor)
        {
            if (!area.Contains(cursor))
                return false;

            Point relativePoint = new Point(cursor.X - area.Location.X, cursor.Y - area.Location.Y);
            Color pixel = image.GetPixel(relativePoint.X, relativePoint.Y);

            return pixel.A > 0;
        }

        public virtual bool IsOverAddButton(Point cursor)
        {
            return IsOverNonTransparentArea(_addButtonArea, _addButtonHoverImage, cursor);
        }

        protected virtual bool IsOverTab(TitleBarTab tab, Point cursor)
        {
            return IsOverNonTransparentArea(tab.Area, tab.TabImage, cursor);
        }

        public virtual void Render(IEnumerable<TitleBarTab> tabs, Graphics graphicsContext, Point cursor)
        {
            int tabContentWidth = Math.Min(_activeCenterImage.Width, Convert.ToInt32(Math.Floor(Convert.ToDouble((_parentWindow.ClientRectangle.Width - (ShowAddButton ? _addButtonImage.Width + AddButtonMarginLeft + AddButtonMarginRight : 0) - (tabs.Count() * OverlapWidth) - OverlapWidth - 14) / tabs.Count()))));
            bool redraw = (tabContentWidth != _tabContentWidth);

            if (redraw)
                _tabContentWidth = tabContentWidth;

            int i = tabs.Count() - 1;
            List<Tuple<TitleBarTab, Rectangle>> activeTabs = new List<Tuple<TitleBarTab, Rectangle>>();

            if (_background != null)
                graphicsContext.DrawImage(_background, 6, _parentWindow.WindowState != FormWindowState.Maximized ? 21 : 8, _parentWindow.ClientRectangle.Width - 12, _activeCenterImage.Height);

            foreach (TitleBarTab tab in tabs.Reverse())
            {
                Rectangle tabArea = new Rectangle(7 + (i * (tabContentWidth + _activeLeftSideImage.Width + _activeRightSideImage.Width - OverlapWidth)), _parentWindow.WindowState != FormWindowState.Maximized ? 21 : 8, tabContentWidth + _activeLeftSideImage.Width + _activeRightSideImage.Width, _activeCenterImage.Height);

                if (redraw)
                    tab.TabImage = null;

                if (!tab.Active)
                    Render(graphicsContext, tab, tabArea);

                else
                    activeTabs.Add(new Tuple<TitleBarTab, Rectangle>(tab, tabArea));

                i--;
            }

            foreach (Tuple<TitleBarTab, Rectangle> tab in activeTabs)
                Render(graphicsContext, tab.Item1, tab.Item2);

            if (_previousTabCount != tabs.Count())
                _previousTabCount = tabs.Count();

            if (ShowAddButton)
            {
                _addButtonArea = new Rectangle(7 + (_previousTabCount * (tabContentWidth + _activeLeftSideImage.Width + _activeRightSideImage.Width - OverlapWidth)) + _activeRightSideImage.Width + AddButtonMarginLeft, (_parentWindow.WindowState != FormWindowState.Maximized ? 21 : 8) + AddButtonMarginTop, _addButtonImage.Width, _addButtonImage.Height);

                bool cursorOverAddButton = IsOverAddButton(cursor);

                //if (cursorOverAddButton)
                //    Debug.WriteLine("Hovering over add.");

                //else
                //    Debug.WriteLine("Outside at {0}, {1}.", cursor.X, cursor.Y);

                graphicsContext.DrawImage(cursorOverAddButton ? _addButtonHoverImage : _addButtonImage, _addButtonArea, 0, 0, cursorOverAddButton ? _addButtonHoverImage.Width : _addButtonImage.Width, cursorOverAddButton ? _addButtonHoverImage.Height : _addButtonImage.Height, GraphicsUnit.Pixel);
            }
        }

        protected virtual void Render(Graphics graphicsContext, TitleBarTab tab, Rectangle area)
        {
            if (tab.TabImage == null)
            {
                tab.TabImage = new Bitmap(area.Width, tab.Active ? _activeCenterImage.Height : _inactiveCenterImage.Height);

                using (Graphics tabGraphicsContext = Graphics.FromImage(tab.TabImage))
                {
                    tabGraphicsContext.DrawImage(tab.Active ? _activeLeftSideImage : _inactiveLeftSideImage, new Rectangle(0, 0, tab.Active ? _activeLeftSideImage.Width : _inactiveLeftSideImage.Width, tab.Active ? _activeLeftSideImage.Height : _inactiveLeftSideImage.Height), 0, 0, tab.Active ? _activeLeftSideImage.Width : _inactiveLeftSideImage.Width, tab.Active ? _activeLeftSideImage.Height : _inactiveLeftSideImage.Height, GraphicsUnit.Pixel);
                    tabGraphicsContext.DrawImage(tab.Active ? _activeCenterImage : _inactiveCenterImage, new Rectangle((tab.Active ? _activeLeftSideImage.Width : _inactiveLeftSideImage.Width), 0, _tabContentWidth, tab.Active ? _activeCenterImage.Height : _inactiveCenterImage.Height), 0, 0, _tabContentWidth, tab.Active ? _activeCenterImage.Height : _inactiveCenterImage.Height, GraphicsUnit.Pixel);
                    tabGraphicsContext.DrawImage(tab.Active ? _activeRightSideImage : _inactiveRightSideImage, new Rectangle((tab.Active ? _activeLeftSideImage.Width : _inactiveLeftSideImage.Width) + _tabContentWidth, 0, tab.Active ? _activeRightSideImage.Width : _inactiveRightSideImage.Width, tab.Active ? _activeRightSideImage.Height : _inactiveRightSideImage.Height), 0, 0, tab.Active ? _activeRightSideImage.Width : _inactiveRightSideImage.Width, tab.Active ? _activeRightSideImage.Height : _inactiveRightSideImage.Height, GraphicsUnit.Pixel);

                    if (tab.ShowCloseButton)
                    {
                        tab.CloseButtonArea = new Rectangle(area.Width - (tab.Active ? _activeRightSideImage.Width : _inactiveRightSideImage.Width) - CloseButtonMarginRight - _closeButtonImage.Width, CloseButtonMarginTop, _closeButtonImage.Width, _closeButtonImage.Height);
                        tabGraphicsContext.DrawImage(_closeButtonImage, tab.CloseButtonArea, 0, 0, _closeButtonImage.Width, _closeButtonImage.Height, GraphicsUnit.Pixel);
                    }
                }

                tab.Area = area;
            }

            graphicsContext.DrawImage(tab.TabImage, area, 0, 0, tab.TabImage.Width, tab.TabImage.Height, GraphicsUnit.Pixel);

            if (tab.Content.ShowIcon)
                graphicsContext.DrawIcon(tab.Content.Icon, new Rectangle(area.X + OverlapWidth + IconMarginLeft, (_parentWindow.WindowState != FormWindowState.Maximized ? 21 : 8) + IconMarginTop, 16, 16));

            graphicsContext.DrawString(tab.Caption, SystemFonts.CaptionFont, Brushes.Black, new Rectangle(area.X + OverlapWidth + CaptionMarginLeft + (tab.Content.ShowIcon ? IconMarginLeft + 16 + IconMarginRight : 0), (_parentWindow.WindowState != FormWindowState.Maximized ? 21 : 8) + CaptionMarginTop, _tabContentWidth - (tab.Content.ShowIcon ? IconMarginLeft + 16 + IconMarginRight : 0) - (tab.ShowCloseButton ? _closeButtonImage.Width + CloseButtonMarginRight + CloseButtonMarginLeft : 0), tab.TabImage.Height), new StringFormat(StringFormatFlags.NoWrap) { Trimming = StringTrimming.EllipsisCharacter });
        }
    }
}
