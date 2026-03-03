using Jalium.UI.Controls.Primitives;

namespace Jalium.UI.Tests;

public class SelectorSelectedValueTests
{
    [Fact]
    public void SelectedValue_ShouldDriveSelectedItem_UsingSelectedValuePath()
    {
        var selector = new TestSelector();
        var first = new TestItem { Id = 10, Name = "A" };
        var second = new TestItem { Id = 20, Name = "B" };

        selector.Items.Add(first);
        selector.Items.Add(second);
        selector.SelectedValuePath = nameof(TestItem.Id);

        selector.SelectedValue = 20;

        Assert.Equal(1, selector.SelectedIndex);
        Assert.Same(second, selector.SelectedItem);
    }

    [Fact]
    public void SelectedItem_ShouldUpdateSelectedValue_UsingSelectedValuePath()
    {
        var selector = new TestSelector();
        var first = new TestItem { Id = 1, Name = "Alpha" };
        var second = new TestItem { Id = 2, Name = "Beta" };

        selector.Items.Add(first);
        selector.Items.Add(second);
        selector.SelectedValuePath = nameof(TestItem.Name);

        selector.SelectedItem = second;

        Assert.Equal(1, selector.SelectedIndex);
        Assert.Equal("Beta", selector.SelectedValue);
    }

    [Fact]
    public void ChangingSelectedValuePath_ShouldRecomputeSelectedValue()
    {
        var selector = new TestSelector();
        var item = new TestItem { Id = 42, Name = "Meaning" };

        selector.Items.Add(item);
        selector.SelectedItem = item;

        selector.SelectedValuePath = nameof(TestItem.Id);
        Assert.Equal(42, selector.SelectedValue);

        selector.SelectedValuePath = nameof(TestItem.Name);
        Assert.Equal("Meaning", selector.SelectedValue);
    }

    private sealed class TestSelector : Selector
    {
    }

    private sealed class TestItem
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}
