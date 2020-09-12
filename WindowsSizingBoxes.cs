using Svg;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Win32Interop.Enums;

namespace EasyTabs
{
    public class WindowsSizingBoxes
    {
        protected TitleBarTabs _parentWindow;
        protected Image _minimizeImage = null;
        protected Image _restoreImage = null;
        protected Image _maximizeImage = null;
        protected Image _closeImage = null;
        protected Image _closeHighlightImage = null;
        protected Brush _minimizeMaximizeButtonHighlight = new SolidBrush(Color.FromArgb(27, Color.Black));
        protected Brush _closeButtonHighlight = new SolidBrush(Color.FromArgb(232, 17, 35));
        protected Rectangle _minimizeButtonArea = new Rectangle(0, 0, 45, 29);
        protected Rectangle _maximizeRestoreButtonArea = new Rectangle(45, 0, 45, 29);
        protected Rectangle _closeButtonArea = new Rectangle(90, 0, 45, 29);

        public WindowsSizingBoxes(TitleBarTabs parentWindow)
        {
            _parentWindow = parentWindow;
            _minimizeImage = LoadSvg(Encoding.UTF8.GetString(Resources.Minimize), 10, 10);
            _restoreImage = LoadSvg(Encoding.UTF8.GetString(Resources.Restore), 10, 10);
            _maximizeImage = LoadSvg(Encoding.UTF8.GetString(Resources.Maximize), 10, 10);
            _closeImage = LoadSvg(Encoding.UTF8.GetString(Resources.Close), 10, 10);
            _closeHighlightImage = LoadSvg(Encoding.UTF8.GetString(Resources.CloseHighlight), 10, 10);
        }

        protected Image LoadSvg(string svgXml, int width, int height)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(svgXml);

            return SvgDocument.Open(xmlDocument).Draw(width, height);
        }

        public int Width
        {
            get
            {
                return _minimizeButtonArea.Width + _maximizeRestoreButtonArea.Width + _closeButtonArea.Width;
            }
        }

        public bool Contains(Point cursor)
        {
            return _minimizeButtonArea.Contains(cursor) || _maximizeRestoreButtonArea.Contains(cursor) || _closeButtonArea.Contains(cursor);
        }

        public void Render(Graphics graphicsContext, Point cursor)
        {
            int right = _parentWindow.ClientRectangle.Width;
            bool closeButtonHighlighted = false;
            
            _minimizeButtonArea.X = right - 135;
            _maximizeRestoreButtonArea.X = right - 90;
            _closeButtonArea.X = right - 45;

            if (_minimizeButtonArea.Contains(cursor))
            {
                graphicsContext.FillRectangle(_minimizeMaximizeButtonHighlight, _minimizeButtonArea);
            }

            else if (_maximizeRestoreButtonArea.Contains(cursor))
            {
                graphicsContext.FillRectangle(_minimizeMaximizeButtonHighlight, _maximizeRestoreButtonArea);
            }

            else if (_closeButtonArea.Contains(cursor))
            {
                graphicsContext.FillRectangle(_closeButtonHighlight, _closeButtonArea);
                closeButtonHighlighted = true;
            }

            graphicsContext.DrawImage(closeButtonHighlighted ? _closeHighlightImage : _closeImage, _closeButtonArea.X + 17, _closeButtonArea.Y + 9);
            graphicsContext.DrawImage(_parentWindow.WindowState == FormWindowState.Maximized ? _restoreImage : _maximizeImage, _maximizeRestoreButtonArea.X + 17, _maximizeRestoreButtonArea.Y + 9);
            graphicsContext.DrawImage(_minimizeImage, _minimizeButtonArea.X + 17, _minimizeButtonArea.Y + 9);
        }

        public HT NonClientHitTest(Point cursor)
        {
            if (_minimizeButtonArea.Contains(cursor))
            {
                return HT.HTMINBUTTON;
            }

            else if (_maximizeRestoreButtonArea.Contains(cursor))
            {
                return HT.HTMAXBUTTON;
            }

            else if (_closeButtonArea.Contains(cursor))
            {
                return HT.HTCLOSE;
            }

            return HT.HTNOWHERE;
        }
    }
}
