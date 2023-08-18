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


}