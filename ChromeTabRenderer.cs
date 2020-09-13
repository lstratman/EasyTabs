using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Win32Interop.Enums;

namespace EasyTabs
{
    /// <summary>Renderer that produces tabs that mimic the appearance of the Chrome browser.</summary>
    public class ChromeTabRenderer : BaseTabRenderer
	{
        WindowsSizingBoxes _windowsSizingBoxes = null;
        Font _captionFont = null;

		/// <summary>Constructor that initializes the various resources that we use in rendering.</summary>
		/// <param name="parentWindow">Parent window that this renderer belongs to.</param>
		public ChromeTabRenderer(TitleBarTabs parentWindow)
			: base(parentWindow)
		{
			// Initialize the various images to use during rendering
			_activeLeftSideImage = Resources.ChromeLeft;
			_activeRightSideImage = Resources.ChromeRight;
			_activeCenterImage = Resources.ChromeCenter;
			_inactiveLeftSideImage = Resources.ChromeInactiveLeft;
			_inactiveRightSideImage = Resources.ChromeInactiveRight;
			_inactiveCenterImage = Resources.ChromeInactiveCenter;
			_closeButtonImage = Resources.ChromeClose;
			_closeButtonHoverImage = Resources.ChromeCloseHover;
			_background = IsWindows10 ? Resources.ChromeBackground : null;
			_addButtonImage = new Bitmap(Resources.ChromeAdd);
			_addButtonHoverImage = new Bitmap(Resources.ChromeAddHover);

			// Set the various positioning properties
			CloseButtonMarginTop = 9;
			CloseButtonMarginLeft = 2;
            CloseButtonMarginRight = 4;
			AddButtonMarginTop = 3;
			AddButtonMarginLeft = 2;
			CaptionMarginTop = 9;
            IconMarginLeft = 9;
			IconMarginTop = 9;
			IconMarginRight = 5;
			AddButtonMarginRight = 45;

            _windowsSizingBoxes = new WindowsSizingBoxes(parentWindow);
            _captionFont = new Font("Segoe UI", 9);

            if (_captionFont.Name != "Segoe UI")
            {
                _captionFont = new Font(SystemFonts.CaptionFont.Name, 9);
            }
        }

        public override Font CaptionFont
        {
            get
            {
                return _captionFont;
            }
        }

        public override int TabHeight
        {
            get
            {
                return _parentWindow.WindowState == FormWindowState.Maximized ? base.TabHeight : base.TabHeight + TopPadding;
            }
        }

        public override int TopPadding
        {
            get
            {
                return _parentWindow.WindowState == FormWindowState.Maximized ? 0 : 8;
            }
        }

        /// <summary>Since Chrome tabs overlap, we set this property to the amount that they overlap by.</summary>
        public override int OverlapWidth
		    {
			      get
			      {
				        return 14;
			      }
		    }

        public override bool RendersEntireTitleBar
        {
            get
            {
                return IsWindows10;
            }
        }

        public override bool IsOverSizingBox(Point cursor)
        {
            return _windowsSizingBoxes.Contains(cursor);
        }

        public override HT NonClientHitTest(Message message, Point cursor)
        {
            HT result = _windowsSizingBoxes.NonClientHitTest(cursor);
            return result == HT.HTNOWHERE ? HT.HTCAPTION : result;
        }

        public override void Render(List<TitleBarTab> tabs, Graphics graphicsContext, Point offset, Point cursor, bool forceRedraw = false)
        {
            base.Render(tabs, graphicsContext, offset, cursor, forceRedraw);

            if (IsWindows10)
            {
                _windowsSizingBoxes.Render(graphicsContext, cursor);
            }
        }

        protected override void Render(Graphics graphicsContext, TitleBarTab tab, int index, Rectangle area, Point cursor, Image tabLeftImage, Image tabCenterImage, Image tabRightImage)
        {
            if (!IsWindows10 && !tab.Active && index == _parentWindow.Tabs.Count - 1)
            {
                tabRightImage = Resources.ChromeInactiveRightNoDivider;
            }

            base.Render(graphicsContext, tab, index, area, cursor, tabLeftImage, tabCenterImage, tabRightImage);
        }

        protected override int GetMaxTabAreaWidth(List<TitleBarTab> tabs, Point offset)
        {
            return _parentWindow.ClientRectangle.Width - offset.X -
                        (ShowAddButton
                            ? _addButtonImage.Width + AddButtonMarginLeft + AddButtonMarginRight
                            : 0) -
                        (tabs.Count * OverlapWidth) -
                        _windowsSizingBoxes.Width;
        }
    }
}
