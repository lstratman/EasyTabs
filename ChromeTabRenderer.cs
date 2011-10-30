using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace Stratman.Windows.Forms.TitleBarTabs
{
    public class ChromeTabRenderer : BaseTabRenderer
    {
        public ChromeTabRenderer(TitleBarTabs parentWindow) 
            : base(parentWindow)
        {
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

            CloseButtonMarginTop = 9;
            CloseButtonMarginLeft = 5;
            AddButtonMarginTop = 5;
            AddButtonMarginLeft = -3;
            CaptionMarginTop = 5;
            IconMarginTop = 5;
            IconMarginRight = 5;
        }

        public override int OverlapWidth
        {
            get
            {
                return 16;
            }
        }
    }
}
