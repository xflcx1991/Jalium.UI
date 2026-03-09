using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Jalium.UI.Media;

namespace Jalium.UI.Controls;

/// <summary>
/// Represents a specialized container that presents a set of menus in a horizontal row,
/// typically at the top of an app window.
/// </summary>
public class MenuBar : Control
{
    private readonly ObservableCollection<MenuBarItem> _items = new();
    private StackPanel? _panel;

    /// <summary>
    /// Gets the collection of MenuBarItem objects in the MenuBar.
    /// </summary>
    public IList<MenuBarItem> Items => _items;

    internal ObservableCollection<MenuBarItem> ItemCollection => _items;

    /// <summary>
    /// Initializes a new instance of the MenuBar class.
    /// </summary>
    public MenuBar()
    {
        Focusable = true;
        _items.CollectionChanged += OnItemsCollectionChanged;
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        EnsurePanel();
        _panel!.Measure(availableSize);
        return new Size(
            Math.Min(_panel.DesiredSize.Width, availableSize.Width),
            Math.Clamp(_panel.DesiredSize.Height, 32, availableSize.Height));
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        _panel?.Arrange(new Rect(finalSize));
        return finalSize;
    }

    /// <inheritdoc />
    protected override void OnRender(object drawingContext)
    {
        if (drawingContext is not DrawingContext dc) return;
        base.OnRender(drawingContext);

        var bg = Background ?? Jalium.UI.Media.Brushes.Transparent;
        dc.DrawRectangle(bg, null, new Rect(RenderSize));
    }

    /// <inheritdoc />
    public override int VisualChildrenCount => _panel != null ? 1 : 0;

    /// <inheritdoc />
    public override Visual? GetVisualChild(int index)
    {
        if (index == 0 && _panel != null) return _panel;
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <summary>
    /// Refreshes the visual representation of items.
    /// </summary>
    public void UpdateItems()
    {
        RefreshPanelChildren();
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void EnsurePanel()
    {
        if (_panel != null) return;

        _panel = new StackPanel { Orientation = Orientation.Horizontal };
        AddVisualChild(_panel);
        RefreshPanelChildren();
    }

    private void RefreshPanelChildren()
    {
        if (_panel == null)
            return;

        _panel.Children.Clear();
        foreach (var item in _items)
        {
            item.ParentMenuBar = this;
            _panel.Children.Add(item);
        }
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var oldItem in e.OldItems.OfType<MenuBarItem>())
            {
                if (ReferenceEquals(oldItem.ParentMenuBar, this))
                {
                    oldItem.ParentMenuBar = null;
                }
            }
        }

        RefreshPanelChildren();
        InvalidateMeasure();
        InvalidateVisual();
    }

    internal void CloseAllMenus(MenuBarItem? except = null)
    {
        foreach (var item in _items)
        {
            if (item != except)
                item.CloseMenu();
        }
    }

    internal bool IsAnyMenuOpen()
    {
        return _items.Any(item => item.IsMenuOpen);
    }
}
