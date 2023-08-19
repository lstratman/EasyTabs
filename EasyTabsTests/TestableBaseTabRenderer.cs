using EasyTabs;
using EasyTabs.Drawing;

namespace EasyTabsTests;

public class TestableBaseTabRenderer : BaseTabRenderer
{
    private Dictionary<TitleBarTab, bool> _overTabStates = new Dictionary<TitleBarTab, bool>();

    private TitleBarTab _selectedTab;

    public TestableBaseTabRenderer(TitleBarTabs parentWindow) : base(parentWindow)
    {
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

    public void SetDragStart(Point? point)
    {
        _dragStart = point;
    }

    public void SetTabClickOffset(int offset)
    {
        _tabClickOffset = offset;
    }

    public void InvokeOverlay_MouseUp(object sender, MouseEventArgs e)
    {
        Overlay_MouseUp(sender, e);
    }

    public void SetIsOverTab(TitleBarTab tab, bool isOver)
    {
        _overTabStates[tab] = isOver;
    }

    protected override bool IsOverTab(TitleBarTab? tab, Point cursor)
    {
        if (tab != null && _overTabStates.ContainsKey(tab))
        {
            return _overTabStates[tab];
        }
        return base.IsOverTab(tab, cursor);
    }

    public void SetParentWindowWithSelectedTab(TitleBarTab selectedTab)
    {
        _selectedTab = selectedTab;
    }

    protected override TitleBarTab? GetSelectedTab()
    {
        return _selectedTab;
    }
}