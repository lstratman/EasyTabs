using System.Drawing;

namespace EasyTabs
{
	/// <summary>Renderer that produces tabs that mimic the appearance of the Chrome browser.</summary>
	public class ChromeTabRenderer : BaseTabRenderer
	{
        /// <summary>
        /// A Chrome-specific right-side tab image that allows the separation between inactive tabs to be more clearly defined.
        /// </summary>
	    protected Image _inactiveRightSideShadowImage = Resources.ChromeInactiveRightShadow;

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
			AddButtonMarginTop = 7;
			AddButtonMarginLeft = -1;
			CaptionMarginTop = 6;
			IconMarginTop = 7;
			IconMarginRight = 5;
			AddButtonMarginRight = 5;
		}

        /// <summary>
        /// Gets the image to use for the right side of the tab.  For Chrome, we pick a specific image for inactive tabs that aren't at
        /// the end of the list to allow for the separation between inactive tabs to be more clearly defined.
        /// </summary>
        /// <param name="tab">Tab that we are retrieving the image for.</param>
        /// <returns>Right-side image for <paramref name="tab"/>.</returns>
	    protected override Image GetTabRightImage(TitleBarTab tab)
	    {
	        ListWithEvents<TitleBarTab> allTabs = tab.Parent.Tabs;

            if (tab.Active || allTabs.IndexOf(tab) == allTabs.Count - 1)
	            return base.GetTabRightImage(tab);

	        return _inactiveRightSideShadowImage;
	    }

	    /// <summary>Since Chrome tabs overlap, we set this property to the amount that they overlap by.</summary>
		public override int OverlapWidth
		{
			get
			{
				return 15;
			}
		}
	}
}