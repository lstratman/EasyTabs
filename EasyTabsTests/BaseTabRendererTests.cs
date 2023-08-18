using EasyTabs;
using NSubstitute;
using NUnit.Framework;

namespace EasyTabsTests;

[TestFixture]
public class BaseTabRendererTests
{
    [Test]
    public void IsWindows10_ReturnsTrueForWindows10()
    {
        // Arrange
        var registry = Substitute.For<IRegistry>();
        var registryKey = Substitute.For<IRegistryKey>();
        registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion").Returns(registryKey);
        registryKey.GetValue("ProductName").Returns("Windows 10");

        var renderer = new TestableBaseTabRenderer(null);
        renderer.Registry = registry;

        // Act
        var result = renderer.IsWindows10;

        // Assert
        Assert.IsTrue(result);
    }

    [Test]
    public void IsWindows10_ReturnsFalseForNonWindows10()
    {
        // Arrange
        var registry = Substitute.For<IRegistry>();
        var registryKey = Substitute.For<IRegistryKey>();
        registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion").Returns(registryKey);
        registryKey.GetValue("ProductName").Returns("Windows 7");

        var renderer = new TestableBaseTabRenderer(null);
        renderer._isWindows10 = null;
        renderer.Registry = registry;

        // Act
        var result = renderer.IsWindows10;

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public void TabHeight_ReturnsCorrectValue()
    {
        // Arrange
        var renderer = new TestableBaseTabRenderer(null);
        renderer._isWindows10 = null;
        renderer.SetActiveCenterImage(new Bitmap(10, 20)); // Set a sample image

        // Act
        int result = renderer.TabHeight;

        // Assert
        Assert.AreEqual(20, result);
    }

    [Test]
    public void IsTabRepositioning_SetValue_GetReturnsCorrectValue()
    {
        // Arrange
        var renderer = new TestableBaseTabRenderer(null);

        // Act
        renderer.IsTabRepositioning = true;
        bool result1 = renderer.IsTabRepositioning;

        renderer.IsTabRepositioning = false;
        bool result2 = renderer.IsTabRepositioning;

        // Assert
        Assert.IsTrue(result1);
        Assert.IsFalse(result2);
    }

    [Test]
    public void IsOverAddButton_WhenNotRepositioningAndCursorIsOver_ReturnsTrue()
    {
        // Arrange
        var renderer = new TestableBaseTabRenderer(null);
        renderer.SetAddButtonArea(new Rectangle(10, 10, 20, 20)); // Set a sample area
        renderer._addButtonHoverImage = new Bitmap(Properties.Resources.TestImage);
        var cursor = new Point(15, 15);

        // Act
        bool result = renderer.IsOverAddButton(cursor);

        // Assert
        Assert.IsTrue(result);
    }

    [Test]
    public void IsOverAddButton_WhenRepositioning_ReturnsFalse()
    {
        // Arrange
        var renderer = new TestableBaseTabRenderer(null);
        renderer.SetIsTabRepositioning(true);
        renderer.SetAddButtonArea(new Rectangle(10, 10, 20, 20)); // Set a sample area
        var cursor = new Point(15, 15);

        // Act
        bool result = renderer.IsOverAddButton(cursor);

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public void IsOverCloseButton_WhenNotOver_ReturnsFalse()
    {
        // Arrange
        var renderer = new TestableBaseTabRenderer(null);
        var tab = new TitleBarTab(null);
        tab.ShowCloseButton = true;
        tab.CloseButtonArea = new Rectangle(10, 10, 20, 20); // Set a sample area
        var cursor = new Point(5, 5);

        // Act
        bool result = renderer.IsOverCloseButton(tab, cursor);

        // Assert
        Assert.IsFalse(result);
    }

    // More tests for other methods and properties
}

