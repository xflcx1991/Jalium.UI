using Jalium.UI.Input;
using Jalium.UI.Interop;
using Jalium.UI.Media;

namespace Jalium.UI.Controls;

/// <summary>
/// Represents a control that displays hierarchical data in a tree structure.
/// </summary>
public class TreeView : ItemsControl
{
    private TreeViewItem? _selectedItem;

    #region Dependency Properties

    /// <summary>
    /// Identifies the SelectedItem dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(TreeView),
            new PropertyMetadata(null, OnSelectedItemChanged));

    /// <summary>
    /// Identifies the SelectedValue dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectedValueProperty =
        DependencyProperty.Register(nameof(SelectedValue), typeof(object), typeof(TreeView),
            new PropertyMetadata(null));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the currently selected item.
    /// </summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// Gets or sets the value of the selected item.
    /// </summary>
    public object? SelectedValue
    {
        get => GetValue(SelectedValueProperty);
        set => SetValue(SelectedValueProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Identifies the SelectedItemChanged routed event.
    /// </summary>
    public static readonly RoutedEvent SelectedItemChangedEvent =
        EventManager.RegisterRoutedEvent(nameof(SelectedItemChanged), RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<object>), typeof(TreeView));

    /// <summary>
    /// Occurs when the selected item changes.
    /// </summary>
    public event RoutedPropertyChangedEventHandler<object?>? SelectedItemChanged
    {
        add => AddHandler(SelectedItemChangedEvent, value);
        remove => RemoveHandler(SelectedItemChangedEvent, value);
    }

    #endregion

    public TreeView()
    {
        Background = new SolidColorBrush(Color.White);
        BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));
        BorderThickness = new Thickness(1);

        Items.CollectionChanged += OnTreeItemsChanged;
    }

    private void OnTreeItemsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        try
        {
            // Handle removed items
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is TreeViewItem tvi)
                    {
                        if (tvi.VisualParent == this)
                        {
                            RemoveVisualChild(tvi);
                        }
                        tvi.ParentTreeView = null;
                    }
                }
            }

            // Handle added items
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is TreeViewItem tvi)
                    {
                        tvi.ParentTreeView = this;
                        tvi.Level = 0;
                        // Only add if not already a child
                        if (tvi.VisualParent == null)
                        {
                            AddVisualChild(tvi);
                        }
                    }
                }
            }

            // Handle reset
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                // Re-add all items
                foreach (var item in Items)
                {
                    if (item is TreeViewItem tvi)
                    {
                        tvi.ParentTreeView = this;
                        tvi.Level = 0;
                        // Only add if not already a child
                        if (tvi.VisualParent == null)
                        {
                            AddVisualChild(tvi);
                        }
                    }
                }
            }

            InvalidateMeasure();
            InvalidateVisual();
        }
        catch
        {
            // Ignore errors during collection changes (can happen during window cleanup)
        }
    }

    /// <inheritdoc />
    public override int VisualChildrenCount => Items.Count;

    /// <inheritdoc />
    public override Visual? GetVisualChild(int index)
    {
        if (index < 0 || index >= Items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return Items[index] as Visual;
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TreeView treeView)
        {
            var oldItem = e.OldValue as TreeViewItem;
            var newItem = e.NewValue as TreeViewItem;

            // Update selection state
            if (oldItem != null)
            {
                oldItem.IsSelected = false;
            }

            if (newItem != null)
            {
                newItem.IsSelected = true;
                treeView._selectedItem = newItem;
            }
            else
            {
                treeView._selectedItem = null;
            }

            // Raise event
            var args = new RoutedPropertyChangedEventArgs<object?>(
                e.OldValue, e.NewValue, SelectedItemChangedEvent);
            treeView.RaiseEvent(args);
        }
    }

    internal void SelectItem(TreeViewItem? item)
    {
        if (_selectedItem != item)
        {
            SelectedItem = item;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double totalHeight = 0;
        double maxWidth = 0;

        foreach (var item in Items)
        {
            if (item is TreeViewItem tvi)
            {
                tvi.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                totalHeight += tvi.DesiredSize.Height;
                maxWidth = Math.Max(maxWidth, tvi.DesiredSize.Width);
            }
        }

        return new Size(
            Math.Min(maxWidth, availableSize.Width),
            Math.Min(totalHeight, availableSize.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double y = 0;

        foreach (var item in Items)
        {
            if (item is TreeViewItem tvi)
            {
                var itemRect = new Rect(0, y, finalSize.Width, tvi.DesiredSize.Height);
                tvi.Arrange(itemRect);
                // Note: Do NOT call SetVisualBounds here - ArrangeCore already handles margin
                y += tvi.DesiredSize.Height;
            }
        }

        return finalSize;
    }

    protected override void OnRender(object drawingContextObj)
    {
        if (drawingContextObj is not DrawingContext dc)
        {
            base.OnRender(drawingContextObj);
            return;
        }

        var bounds = new Rect(0, 0, ActualWidth, ActualHeight);

        // Draw background
        if (Background != null)
        {
            dc.DrawRectangle(Background, null, bounds);
        }

        // Draw border
        if (BorderBrush != null && BorderThickness.Left > 0)
        {
            var pen = new Pen(BorderBrush, BorderThickness.Left);
            dc.DrawRectangle(null, pen, bounds);
        }

        base.OnRender(drawingContextObj);
    }
}

/// <summary>
/// Represents an item in a TreeView control.
/// </summary>
public class TreeViewItem : HeaderedItemsControl
{
    private const double IndentSize = 16;
    private const double ItemHeight = 24;
    private const double ExpanderSize = 16;

    internal TreeView? ParentTreeView { get; set; }
    internal int Level { get; set; }

    #region Dependency Properties

    /// <summary>
    /// Identifies the IsExpanded dependency property.
    /// </summary>
    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(TreeViewItem),
            new PropertyMetadata(false, OnIsExpandedChanged));

    /// <summary>
    /// Identifies the IsSelected dependency property.
    /// </summary>
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(TreeViewItem),
            new PropertyMetadata(false, OnIsSelectedChanged));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets whether this item is expanded.
    /// </summary>
    public bool IsExpanded
    {
        get => (bool)(GetValue(IsExpandedProperty) ?? false);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>
    /// Gets or sets whether this item is selected.
    /// </summary>
    public bool IsSelected
    {
        get => (bool)(GetValue(IsSelectedProperty) ?? false);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>
    /// Gets whether this item has child items.
    /// </summary>
    public bool HasItems => Items.Count > 0;

    #endregion

    #region Events

    /// <summary>
    /// Identifies the Expanded routed event.
    /// </summary>
    public static readonly RoutedEvent ExpandedEvent =
        EventManager.RegisterRoutedEvent(nameof(Expanded), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(TreeViewItem));

    /// <summary>
    /// Identifies the Collapsed routed event.
    /// </summary>
    public static readonly RoutedEvent CollapsedEvent =
        EventManager.RegisterRoutedEvent(nameof(Collapsed), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(TreeViewItem));

    /// <summary>
    /// Occurs when the item is expanded.
    /// </summary>
    public event RoutedEventHandler Expanded
    {
        add => AddHandler(ExpandedEvent, value);
        remove => RemoveHandler(ExpandedEvent, value);
    }

    /// <summary>
    /// Occurs when the item is collapsed.
    /// </summary>
    public event RoutedEventHandler Collapsed
    {
        add => AddHandler(CollapsedEvent, value);
        remove => RemoveHandler(CollapsedEvent, value);
    }

    #endregion

    public TreeViewItem()
    {
        Padding = new Thickness(4, 2, 4, 2);

        AddHandler(MouseDownEvent, new RoutedEventHandler(OnMouseDownHandler));

        Items.CollectionChanged += OnChildItemsChanged;
    }

    private void OnChildItemsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        try
        {
            // Handle removed items
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is TreeViewItem childTvi)
                    {
                        if (childTvi.VisualParent == this)
                        {
                            RemoveVisualChild(childTvi);
                        }
                        childTvi.ParentTreeView = null;
                    }
                }
            }

            // Handle added items
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is TreeViewItem childTvi)
                    {
                        childTvi.ParentTreeView = ParentTreeView;
                        childTvi.Level = Level + 1;
                        // Only add if not already a child
                        if (childTvi.VisualParent == null)
                        {
                            AddVisualChild(childTvi);
                        }
                    }
                }
            }

            // Handle reset
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                foreach (var item in Items)
                {
                    if (item is TreeViewItem childTvi)
                    {
                        childTvi.ParentTreeView = ParentTreeView;
                        childTvi.Level = Level + 1;
                        // Only add if not already a child
                        if (childTvi.VisualParent == null)
                        {
                            AddVisualChild(childTvi);
                        }
                    }
                }
            }

            InvalidateMeasure();
            InvalidateVisual();
        }
        catch
        {
            // Ignore errors during collection changes (can happen during window cleanup)
        }
    }

    /// <inheritdoc />
    public override int VisualChildrenCount => IsExpanded ? Items.Count : 0;

    /// <inheritdoc />
    public override Visual? GetVisualChild(int index)
    {
        if (!IsExpanded || index < 0 || index >= Items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return Items[index] as Visual;
    }

    private void OnMouseDownHandler(object sender, RoutedEventArgs e)
    {
        if (e is MouseButtonEventArgs mouseArgs)
        {
            var clickX = mouseArgs.GetPosition(this).X;
            var expanderX = Level * IndentSize;

            // Check if clicked on expander
            if (HasItems && clickX >= expanderX && clickX <= expanderX + ExpanderSize)
            {
                IsExpanded = !IsExpanded;
            }
            else
            {
                // Select this item
                ParentTreeView?.SelectItem(this);
            }

            e.Handled = true;
        }
    }

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TreeViewItem tvi)
        {
            if ((bool)e.NewValue)
            {
                tvi.RaiseEvent(new RoutedEventArgs(ExpandedEvent, tvi));
            }
            else
            {
                tvi.RaiseEvent(new RoutedEventArgs(CollapsedEvent, tvi));
            }

            // Invalidate layout to show/hide children
            tvi.InvalidateMeasure();
            tvi.ParentTreeView?.InvalidateMeasure();
            tvi.ParentTreeView?.InvalidateVisual();
        }
    }

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TreeViewItem tvi)
        {
            if ((bool)e.NewValue && tvi.ParentTreeView != null)
            {
                tvi.ParentTreeView.SelectItem(tvi);
            }

            tvi.InvalidateVisual();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Header row height
        double totalHeight = ItemHeight;
        double maxWidth = Level * IndentSize + ExpanderSize;

        // Measure header text width
        var headerText = Header?.ToString() ?? "";
        var charWidth = 14 * 0.6;
        maxWidth += headerText.Length * charWidth + Padding.Left + Padding.Right;

        // Measure children if expanded
        if (IsExpanded)
        {
            foreach (var item in Items)
            {
                if (item is TreeViewItem childTvi)
                {
                    childTvi.Level = Level + 1;
                    childTvi.ParentTreeView = ParentTreeView;
                    childTvi.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                    totalHeight += childTvi.DesiredSize.Height;
                    maxWidth = Math.Max(maxWidth, childTvi.DesiredSize.Width);
                }
            }
        }

        return new Size(
            Math.Min(maxWidth, availableSize.Width),
            totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (IsExpanded)
        {
            double y = ItemHeight;

            foreach (var item in Items)
            {
                if (item is TreeViewItem childTvi)
                {
                    var itemRect = new Rect(0, y, finalSize.Width, childTvi.DesiredSize.Height);
                    childTvi.Arrange(itemRect);
                    // Note: Do NOT call SetVisualBounds here - ArrangeCore already handles margin
                    y += childTvi.DesiredSize.Height;
                }
            }
        }

        return finalSize;
    }

    protected override void OnRender(object drawingContextObj)
    {
        if (drawingContextObj is not DrawingContext dc)
        {
            base.OnRender(drawingContextObj);
            return;
        }

        var indent = Level * IndentSize;
        var headerBounds = new Rect(0, 0, ActualWidth, ItemHeight);

        // Draw selection background
        if (IsSelected)
        {
            var selectionBrush = new SolidColorBrush(Color.FromRgb(204, 232, 255));
            dc.DrawRectangle(selectionBrush, null, headerBounds);
        }

        // Get font metrics for consistent vertical centering
        var fontMetrics = TextMeasurement.GetFontMetrics("Segoe UI", 14);
        var textHeight = fontMetrics.LineHeight;
        var expanderFontMetrics = TextMeasurement.GetFontMetrics("Segoe UI", 8);
        var expanderHeight = expanderFontMetrics.LineHeight;

        // Draw expander if has children
        if (HasItems)
        {
            // Use Foreground color for expander if available, otherwise use default gray
            var expanderBrush = Foreground ?? new SolidColorBrush(Color.FromRgb(100, 100, 100));
            var expanderY = (ItemHeight - expanderHeight) / 2;

            if (IsExpanded)
            {
                // Draw down arrow (▼)
                var arrowText = new FormattedText("▼", "Segoe UI", 8)
                {
                    Foreground = expanderBrush
                };
                dc.DrawText(arrowText, new Point(indent + 2, expanderY));
            }
            else
            {
                // Draw right arrow (►)
                var arrowText = new FormattedText("►", "Segoe UI", 8)
                {
                    Foreground = expanderBrush
                };
                dc.DrawText(arrowText, new Point(indent + 2, expanderY));
            }
        }

        // Draw header text
        var headerText = Header?.ToString() ?? "";
        if (!string.IsNullOrEmpty(headerText))
        {
            // Use Foreground if set, otherwise fall back to default colors
            Brush? textBrush;
            if (IsSelected)
            {
                textBrush = new SolidColorBrush(Color.FromRgb(0, 90, 158));
            }
            else if (Foreground != null)
            {
                textBrush = Foreground;
            }
            else
            {
                textBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            }

            var text = new FormattedText(headerText, "Segoe UI", 14)
            {
                Foreground = textBrush
            };

            var textX = indent + ExpanderSize + Padding.Left;
            var textY = (ItemHeight - textHeight) / 2;

            dc.DrawText(text, new Point(textX, textY));
        }

        // Render children (handled by child TreeViewItems)
        base.OnRender(drawingContextObj);
    }
}

// Note: HeaderedItemsControl is defined in Menu.cs
