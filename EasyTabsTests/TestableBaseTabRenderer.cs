using EasyTabs;
using EasyTabs.Drawing;

namespace EasyTabsTests;

public class TestableBaseTabRenderer : BaseTabRenderer
{
    public TestableBaseTabRenderer(TitleBarTabs parentWindow) : base(parentWindow)
    {
    }

    protected IRegistry CreateRegistry()
    {
        return Registry;
    }

    public void SetActiveCenterImage(Image image)
    {
        _activeCenterImage = image;
    }

    public void SetAddButtonArea(Rectangle area)
    {
        _addButtonArea = area;
    }

    public void SetIsTabRepositioning(bool value)
    {
        _isTabRepositioning = value;
    }

    public Point? GetDragStart()
    {
        return _dragStart;
    }

    public int? GetTabClickOffset()
    {
        return _tabClickOffset;
    }

    public bool GetIsTabRepositioning()
    {
        return _isTabRepositioning;
    }

    public void SetDragStart(Point? point)
    {
        _dragStart = point;
    }

    public void SetTabClickOffset(int offset)
    {
        _tabClickOffset = offset;
    }

    // Override the protected methods to make them accessible for testing
    public void InvokeOverlay_MouseDown(object sender, MouseEventArgs e)
    {
        Overlay_MouseDown(sender, e);
    }

    public void InvokeOverlay_MouseUp(object sender, MouseEventArgs e)
    {
        Overlay_MouseUp(sender, e);
    }

    public void InvokeOverlay_MouseMove(object sender, MouseEventArgs e)
    {
        Overlay_MouseMove(sender, e);
    }
}