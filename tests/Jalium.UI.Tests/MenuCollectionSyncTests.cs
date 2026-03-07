using System.Reflection;
using Jalium.UI.Controls;

namespace Jalium.UI.Tests;

public class MenuCollectionSyncTests
{
    [Fact]
    public void MenuBar_ItemsChangedAfterInitialLayout_ShouldRefreshPanel()
    {
        var menuBar = new MenuBar();
        var host = new StackPanel { Width = 400, Height = 60 };
        host.Children.Add(menuBar);

        host.Measure(new Size(400, 60));
        host.Arrange(new Rect(0, 0, 400, 60));

        var panel = Assert.IsType<StackPanel>(menuBar.GetVisualChild(0));
        Assert.Empty(panel.Children);

        var fileItem = new MenuBarItem { Title = "File" };
        menuBar.Items.Add(fileItem);
        host.Measure(new Size(400, 60));
        host.Arrange(new Rect(0, 0, 400, 60));

        Assert.Single(panel.Children);
        Assert.Same(fileItem, panel.Children[0]);
        Assert.Same(menuBar, GetInternalProperty<MenuBar>(fileItem, "ParentMenuBar"));

        menuBar.Items.Remove(fileItem);
        host.Measure(new Size(400, 60));
        host.Arrange(new Rect(0, 0, 400, 60));

        Assert.Empty(panel.Children);
        Assert.Null(GetInternalProperty<MenuBar>(fileItem, "ParentMenuBar"));
    }

    [Fact]
    public void MenuBarItem_ItemsChangedAfterFlyoutCreated_ShouldSyncFlyoutItems()
    {
        var menuBarItem = new MenuBarItem { Title = "File" };
        menuBarItem.Items.Add(new MenuFlyoutItem { Text = "Open" });

        menuBarItem.OpenMenu();

        var flyout = GetPrivateField<MenuFlyout>(menuBarItem, "_flyout");
        Assert.Single(flyout.Items);

        menuBarItem.Items.Add(new MenuFlyoutItem { Text = "Save" });
        Assert.Equal(2, flyout.Items.Count);

        menuBarItem.Items.RemoveAt(0);
        Assert.Single(flyout.Items);
        Assert.Equal("Save", Assert.IsType<MenuFlyoutItem>(flyout.Items[0]).Text);
    }

    [Fact]
    public void MenuFlyout_ItemsChangedAfterPresenterCreated_ShouldRefreshPresenter()
    {
        var flyout = new MenuFlyout();
        flyout.Items.Add(new MenuFlyoutItem { Text = "Open" });

        var createPresenter = typeof(MenuFlyout).GetMethod("CreatePresenter",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(createPresenter);

        var presenter = Assert.IsAssignableFrom<Control>(createPresenter!.Invoke(flyout, null));
        var scrollHost = GetPrivateField<Control>(presenter, "_scrollHost");
        var itemsPanel = Assert.IsType<StackPanel>(scrollHost.GetType()
            .GetProperty("ItemsPanel", BindingFlags.Instance | BindingFlags.Public)!
            .GetValue(scrollHost));

        Assert.Single(itemsPanel.Children);

        flyout.Items.Add(new MenuFlyoutItem { Text = "Save" });
        Assert.Equal(2, itemsPanel.Children.Count);

        flyout.Items.RemoveAt(0);
        Assert.Single(itemsPanel.Children);
        Assert.Equal("Save", Assert.IsType<MenuFlyoutItem>(itemsPanel.Children[0]).Text);
    }

    private static T GetPrivateField<T>(object owner, string fieldName)
    {
        var field = owner.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var value = field!.GetValue(owner);
        Assert.NotNull(value);
        return (T)value!;
    }

    private static T? GetInternalProperty<T>(object owner, string propertyName)
        where T : class
    {
        var property = owner.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(property);
        return property!.GetValue(owner) as T;
    }
}
