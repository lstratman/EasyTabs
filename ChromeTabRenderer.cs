using System.Drawing;

namespace EasyTabs
{
	/// <summary>Renderer that produces tabs that mimic the appearance of the Chrome browser.</summary>
	public class ChromeTabRenderer : BaseTabRenderer
	{
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
			_background = Resources.ChromeBackground;
			_addButtonImage = new Bitmap(Resources.ChromeAdd);
			_addButtonHoverImage = new Bitmap(Resources.ChromeAddHover);

			// Set the various positioning properties
			CloseButtonMarginTop = 6;
			CloseButtonMarginLeft = 2;
			AddButtonMarginTop = 5;
			AddButtonMarginLeft = -3;
			CaptionMarginTop = 5;
			IconMarginTop = 6;
			IconMarginRight = 5;
			AddButtonMarginRight = 5;
		}

		/// <summary>Since Chrome tabs overlap, we set this property to the amount that they overlap by.</summary>
		public override int OverlapWidth
		{
			get
			{
				return 16;
			}
		}
	}
}