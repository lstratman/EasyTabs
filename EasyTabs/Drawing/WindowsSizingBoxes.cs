using Svg;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using EasyTabs.Properties;
using Win32Interop.Enums;

namespace EasyTabs.Drawing;

/// <summary>
/// WindowsSizingBoxes class.
/// </summary>
public class WindowsSizingBoxes
{
    /// <summary>
    /// The parentWindow
    /// </summary>
    protected TitleBarTabs? _parentWindow;

    /// <summary>
    /// The minimizeImage
    /// </summary>
    protected Image? _minimizeImage;

    /// <summary>
    /// The restoreImage
    /// </summary>
    protected Image? _restoreImage;

    /// <summary>
    /// The maximizeImage
    /// </summary>
    protected Image? _maximizeImage;

    /// <summary>
    /// The closeImage
    /// </summary>
    protected Image? _closeImage;

    /// <summary>
    /// The closeHighlightImage
    /// </summary>
    protected Image? _closeHighlightImage;

    /// <summary>
    /// The minimizeMaximizeButtonHighlight
    /// </summary>
    protected Brush _minimizeMaximizeButtonHighlight = new SolidBrush(Color.FromArgb(27, Color.Black));

    /// <summary>
    /// The closeButtonHighlight
    /// </summary>
    protected Brush _closeButtonHighlight = new SolidBrush(Color.FromArgb(232, 17, 35));

    /// <summary>
    /// The minimizeButtonArea
    /// </summary>
    protected Rectangle _minimizeButtonArea = new(0, 0, 45, 29);

    /// <summary>
    /// The maximizeRestoreButtonArea
    /// </summary>
    protected Rectangle _maximizeRestoreButtonArea = new(45, 0, 45, 29);

    /// <summary>
    /// The closeButtonArea
    /// </summary>
    protected Rectangle _closeButtonArea = new(90, 0, 45, 29);

    /// <summary>
    /// Creates a WindowsSizingBoxes object.
    /// </summary>
    /// <param name="parentWindow"></param>
    public WindowsSizingBoxes(TitleBarTabs? parentWindow)
    {
        _parentWindow = parentWindow;
        _minimizeImage = LoadSvg(Encoding.UTF8.GetString(Resources.Minimize), 10, 10);
        _restoreImage = LoadSvg(Encoding.UTF8.GetString(Resources.Restore), 10, 10);
        _maximizeImage = LoadSvg(Encoding.UTF8.GetString(Resources.Maximize), 10, 10);
        _closeImage = LoadSvg(Encoding.UTF8.GetString(Resources.Close), 10, 10);
        _closeHighlightImage = LoadSvg(Encoding.UTF8.GetString(Resources.CloseHighlight), 10, 10);
    }

    /// <summary>
    /// Loads a Svg
    /// </summary>
    /// <param name="svgXml"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    protected Image LoadSvg(string svgXml, int width, int height)
    {
        XmlDocument xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(svgXml);

        return SvgDocument.Open(xmlDocument).Draw(width, height);
    }

    /// <summary>
    /// The width.
    /// </summary>
    public int Width => _minimizeButtonArea.Width + _maximizeRestoreButtonArea.Width + _closeButtonArea.Width;

    /// <summary>
    /// Says if contains the cursor.
    /// </summary>
    /// <param name="cursor"></param>
    /// <returns></returns>
    public bool Contains(Point cursor)
    {
        return _minimizeButtonArea.Contains(cursor) || _maximizeRestoreButtonArea.Contains(cursor) || _closeButtonArea.Contains(cursor);
    }

    /// <summary>
    /// Renders.
    /// </summary>
    /// <param name="graphicsContext"></param>
    /// <param name="cursor"></param>
    public virtual void Render(Graphics graphicsContext, Point cursor)
    {
        if (_parentWindow != null)
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

            var closeHighlightImage = closeButtonHighlighted ? _closeHighlightImage : _closeImage;
            if (closeHighlightImage != null)
            {
                graphicsContext.DrawImage(closeHighlightImage, _closeButtonArea.X + 17, _closeButtonArea.Y + 9);
            }
        }

        var maximizeImage = _parentWindow != null && _parentWindow.WindowState == FormWindowState.Maximized ? _restoreImage : _maximizeImage;
        if (maximizeImage != null)
        {
            graphicsContext.DrawImage(maximizeImage, _maximizeRestoreButtonArea.X + 17, _maximizeRestoreButtonArea.Y + 9);
        }

        if (_minimizeImage != null)
        {
            graphicsContext.DrawImage(_minimizeImage, _minimizeButtonArea.X + 17, _minimizeButtonArea.Y + 9);
        }
    }

    /// <summary>
    /// NonClientHitTest
    /// </summary>
    /// <param name="cursor"></param>
    /// <returns></returns>
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