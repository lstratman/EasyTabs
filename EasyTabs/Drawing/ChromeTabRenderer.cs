using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EasyTabs.Properties;

using Win32Interop.Enums;

namespace EasyTabs.Drawing;

/// <summary>Renderer that produces tabs that mimic the appearance of the Chrome browser.</summary>
public sealed class ChromeTabRenderer : BaseTabRenderer
{
    WindowsSizingBoxes? _windowsSizingBoxes;
    Font? _captionFont;

    /// <summary>Constructor that initializes the various resources that we use in rendering.</summary>
    /// <param name="parentWindow">Parent window that this renderer belongs to.</param>
    public ChromeTabRenderer(TitleBarTabs? parentWindow)
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

    /// <inheritdoc />
    public override Font? CaptionFont => _captionFont;

    /// <inheritdoc />
    public override int TabHeight => _parentWindow?.WindowState == FormWindowState.Maximized ? base.TabHeight : base.TabHeight + TopPadding;

    /// <inheritdoc />
    public override int TopPadding => _parentWindow?.WindowState == FormWindowState.Maximized ? 0 : 8;

    /// <summary>Since Chrome tabs overlap, we set this property to the amount that they overlap by.</summary>
    public override int OverlapWidth => 14;

    /// <inheritdoc />
    public override bool RendersEntireTitleBar => IsWindows10;

    /// <inheritdoc />
    public override bool IsOverSizingBox(Point cursor)
    {
        return _windowsSizingBoxes?.Contains(cursor)??false;
    }

    /// <inheritdoc />
    public override HT NonClientHitTest(Message message, Point cursor)
    {
        if (_windowsSizingBoxes != null)
        {
            HT result = _windowsSizingBoxes.NonClientHitTest(cursor);
            return result == HT.HTNOWHERE ? HT.HTCAPTION : result;
        }

        return HT.HTCAPTION;
    }

    /// <inheritdoc />
    public override void Render(List<TitleBarTab?>? tabs, Graphics graphicsContext, Point offset, Point cursor, bool forceRedraw = false)
    {
        base.Render(tabs, graphicsContext, offset, cursor, forceRedraw);

        if (IsWindows10)
        {
            _windowsSizingBoxes?.Render(graphicsContext, cursor);
        }
    }

    /// <inheritdoc />
    protected override void Render(Graphics graphicsContext, TitleBarTab? tab, int index, Rectangle area, Point cursor, Image? tabLeftImage, Image? tabCenterImage, Image? tabRightImage)
    {
        if (_parentWindow != null && tab != null && !IsWindows10 && !tab.Active && index == _parentWindow.Tabs.Count - 1)
        {
            tabRightImage = Resources.ChromeInactiveRightNoDivider;
        }

        base.Render(graphicsContext, tab, index, area, cursor, tabLeftImage, tabCenterImage, tabRightImage);
    }

    /// <inheritdoc />
    protected override int GetMaxTabAreaWidth(List<TitleBarTab?> tabs, Point offset)
    {
        if (_parentWindow != null)
        {
            if (_addButtonImage != null)
            {
                return _parentWindow.ClientRectangle.Width - offset.X -
                    (ShowAddButton
                        ? _addButtonImage.Width + AddButtonMarginLeft + AddButtonMarginRight
                        : 0) -
                    tabs.Count * OverlapWidth - _windowsSizingBoxes?.Width ?? 0;
            }
        }

        return 0;
    }
}