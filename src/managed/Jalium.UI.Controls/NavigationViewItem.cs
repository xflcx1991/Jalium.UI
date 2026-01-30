using Jalium.UI.Input;
using Jalium.UI.Interop;
using Jalium.UI.Media;

namespace Jalium.UI.Controls;

/// <summary>
/// Represents an item in a NavigationView with WinUI-style appearance.
/// </summary>
public class NavigationViewItem : ContentControl
{
    #region Constants

    // Layout constants matching WinUI Gallery style
    private const double SelectionIndicatorWidth = 3;
    private const double SelectionIndicatorHeight = 16;
    private const double IconSize = 16;
    private const double ItemHeight = 36;
    private const double ItemPaddingLeft = 12;
    private const double ItemPaddingRight = 12;
    private const double IconToContentSpacing = 12;
    private const double ChevronSize = 12;
    private const double IndentPerLevel = 28;
    private const double ItemCornerRadius = 4;

    #endregion

    #region Fields

    private bool _isPointerOver;
    private bool _isPressed;
    private readonly StackPanel _childrenPanel;
    private int _indentLevel;

    #endregion

    #region Dependency Properties

    /// <summary>
    /// Identifies the Icon dependency property.
    /// </summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(object), typeof(NavigationViewItem),
            new PropertyMetadata(null, OnVisualPropertyChanged));

    /// <summary>
    /// Identifies the IsSelected dependency property.
    /// </summary>
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(NavigationViewItem),
            new PropertyMetadata(false, OnIsSelectedChanged));

    /// <summary>
    /// Identifies the IsExpanded dependency property.
    /// </summary>
    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(NavigationViewItem),
            new PropertyMetadata(false, OnIsExpandedChanged));

    /// <summary>
    /// Identifies the SelectsOnInvoked dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectsOnInvokedProperty =
        DependencyProperty.Register(nameof(SelectsOnInvoked), typeof(bool), typeof(NavigationViewItem),
            new PropertyMetadata(true));

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the icon for this item. Can be a string (glyph), IconElement, or any UIElement.
    /// </summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this item is selected.
    /// </summary>
    public bool IsSelected
    {
        get => (bool)(GetValue(IsSelectedProperty) ?? false);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this item is expanded.
    /// </summary>
    public bool IsExpanded
    {
        get => (bool)(GetValue(IsExpandedProperty) ?? false);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this item becomes selected when invoked.
    /// </summary>
    public bool SelectsOnInvoked
    {
        get => (bool)(GetValue(SelectsOnInvokedProperty) ?? true);
        set => SetValue(SelectsOnInvokedProperty, value);
    }

    /// <summary>
    /// Gets the collection of child menu items.
    /// </summary>
    public List<NavigationViewItem> MenuItems { get; } = new();

    /// <summary>
    /// Gets a value indicating whether this item has child menu items.
    /// </summary>
    public bool HasUnrealizedChildren => MenuItems.Count > 0;

    /// <summary>
    /// Gets or sets the indentation level of this item.
    /// </summary>
    public int IndentLevel
    {
        get => _indentLevel;
        set
        {
            _indentLevel = value;
            // Also update children's indent level
            foreach (var child in MenuItems)
            {
                child.IndentLevel = value + 1;
            }
        }
    }

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationViewItem"/> class.
    /// </summary>
    public NavigationViewItem()
    {
        Focusable = true;
        Height = ItemHeight;
        Margin = new Thickness(4, 2, 4, 2);

        // Create panel for child items
        _childrenPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Visibility = Visibility.Collapsed
        };

        // Hook up mouse events for hover/pressed states
        MouseEnter += OnMouseEnterHandler;
        MouseLeave += OnMouseLeaveHandler;
        MouseDown += OnMouseDownHandler;
        MouseUp += OnMouseUpHandler;
    }

    /// <summary>
    /// Override to prevent Content from being added to visual tree.
    /// NavigationViewItem renders its content manually in OnRender.
    /// </summary>
    protected override void OnContentChanged(object? oldContent, object? newContent)
    {
        // Don't call base - we don't want Content added to visual tree
        // NavigationViewItem handles all rendering in OnRender
        InvalidateMeasure();
        InvalidateVisual();
    }

    #region Events

    /// <summary>
    /// Occurs when the item is invoked (clicked or tapped).
    /// </summary>
    public event EventHandler<NavigationViewItemInvokedEventArgs>? Invoked;

    /// <summary>
    /// Occurs when the selection state changes.
    /// </summary>
    public event EventHandler<bool>? SelectionChanged;

    /// <summary>
    /// Occurs when the expansion state changes.
    /// </summary>
    public event EventHandler<bool>? ExpansionChanged;

    #endregion

    #region Mouse Event Handlers

    private void OnMouseEnterHandler(object? sender, RoutedEventArgs e)
    {
        _isPointerOver = true;
        InvalidateVisual();
    }

    private void OnMouseLeaveHandler(object? sender, RoutedEventArgs e)
    {
        _isPointerOver = false;
        _isPressed = false;
        InvalidateVisual();
    }

    private void OnMouseDownHandler(object? sender, RoutedEventArgs e)
    {
        if (e is MouseButtonEventArgs mouseArgs && mouseArgs.ChangedButton == MouseButton.Left)
        {
            _isPressed = true;
            InvalidateVisual();

            // Check if click is on the chevron area (expand/collapse)
            if (HasUnrealizedChildren)
            {
                var pos = mouseArgs.GetPosition(this);
                var chevronX = ActualWidth - ItemPaddingRight - ChevronSize;
                if (pos.X >= chevronX)
                {
                    IsExpanded = !IsExpanded;
                    mouseArgs.Handled = true;
                    return;
                }
            }
        }
    }

    private void OnMouseUpHandler(object? sender, RoutedEventArgs e)
    {
        if (e is MouseButtonEventArgs mouseArgs && mouseArgs.ChangedButton == MouseButton.Left)
        {
            _isPressed = false;
            InvalidateVisual();
        }
    }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Invokes this item.
    /// </summary>
    internal void Invoke()
    {
        Invoked?.Invoke(this, new NavigationViewItemInvokedEventArgs(this));
    }

    /// <summary>
    /// Gets the children panel for adding child items.
    /// </summary>
    internal StackPanel GetChildrenPanel() => _childrenPanel;

    #endregion

    #region Layout

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = !double.IsNaN(Width) && Width > 0 ? Width : availableSize.Width;
        var height = ItemHeight;

        // Measure children panel if expanded
        if (HasUnrealizedChildren && IsExpanded)
        {
            _childrenPanel.Measure(new Size(width, double.PositiveInfinity));
            height += _childrenPanel.DesiredSize.Height;
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // Arrange children panel below this item
        if (HasUnrealizedChildren && IsExpanded)
        {
            var childrenRect = new Rect(0, ItemHeight, finalSize.Width, _childrenPanel.DesiredSize.Height);
            _childrenPanel.Arrange(childrenRect);
        }

        return finalSize;
    }

    #endregion

    #region Rendering

    protected override void OnRender(object drawingContextObj)
    {
        if (drawingContextObj is not DrawingContext dc)
        {
            base.OnRender(drawingContextObj);
            return;
        }

        var width = ActualWidth;
        var height = Math.Min(ActualHeight, ItemHeight);

        // Calculate indent
        var indent = IndentLevel * IndentPerLevel;

        // Get colors based on state
        var (bgColor, fgColor, indicatorColor) = GetStateColors();

        // Draw background with rounded corners
        if (bgColor.A > 0)
        {
            var bgBrush = new SolidColorBrush(bgColor);
            dc.DrawRoundedRectangle(bgBrush, null,
                new Rect(0, 0, width, height),
                ItemCornerRadius, ItemCornerRadius);
        }

        // Draw selection indicator (left vertical bar)
        if (IsSelected)
        {
            var indicatorBrush = new SolidColorBrush(indicatorColor);
            var indicatorY = (height - SelectionIndicatorHeight) / 2;
            dc.DrawRoundedRectangle(indicatorBrush, null,
                new Rect(4, indicatorY, SelectionIndicatorWidth, SelectionIndicatorHeight),
                1.5, 1.5);
        }

        // Calculate positions
        var x = ItemPaddingLeft + indent;
        var centerY = height / 2;

        // Get font metrics for proper vertical centering
        var textFontMetrics = TextMeasurement.GetFontMetrics("Segoe UI", 14);
        var chevronFontMetrics = TextMeasurement.GetFontMetrics("Segoe UI", 10);

        // Note: Icon rendering is disabled until proper font support is implemented
        // The Segoe Fluent Icons font is not rendering correctly in the current text engine

        // Draw content text
        var contentText = GetContentText();
        if (!string.IsNullOrEmpty(contentText))
        {
            var textBrush = new SolidColorBrush(fgColor);
            var textY = centerY - textFontMetrics.LineHeight / 2;
            var maxTextWidth = width - x - ItemPaddingRight;

            if (HasUnrealizedChildren)
            {
                maxTextWidth -= ChevronSize + 8; // Leave room for chevron
            }

            var formattedText = new FormattedText(contentText, "Segoe UI", 14)
            {
                Foreground = textBrush,
                MaxTextWidth = maxTextWidth
            };
            dc.DrawText(formattedText, new Point(x, textY));
        }

        // Draw expand/collapse chevron for items with children
        if (HasUnrealizedChildren)
        {
            var chevronBrush = new SolidColorBrush(Color.FromRgb(150, 150, 150));
            var chevronX = width - ItemPaddingRight - ChevronSize;
            var chevronY = centerY - chevronFontMetrics.LineHeight / 2;

            // Use simple ASCII characters instead of icon font
            var chevronChar = IsExpanded ? "▼" : "▶";
            var formattedChevron = new FormattedText(chevronChar, "Segoe UI", 10)
            {
                Foreground = chevronBrush
            };
            dc.DrawText(formattedChevron, new Point(chevronX, chevronY));
        }

        // Don't call base.OnRender - we handle all rendering ourselves
        // Calling base would cause Content (e.g., TextBlock) to render again
    }

    private (Color bg, Color fg, Color indicator) GetStateColors()
    {
        // WinUI Gallery dark theme colors
        var transparent = Color.FromArgb(0, 0, 0, 0);
        var normalFg = Color.FromRgb(255, 255, 255);
        var accentColor = Color.FromRgb(96, 205, 255); // Light blue accent (WinUI default)

        if (IsSelected)
        {
            if (_isPressed)
            {
                return (Color.FromArgb(20, 255, 255, 255), normalFg, accentColor);
            }
            if (_isPointerOver)
            {
                return (Color.FromArgb(15, 255, 255, 255), normalFg, accentColor);
            }
            return (Color.FromArgb(10, 255, 255, 255), normalFg, accentColor);
        }
        else
        {
            if (_isPressed)
            {
                return (Color.FromArgb(10, 255, 255, 255), Color.FromRgb(200, 200, 200), transparent);
            }
            if (_isPointerOver)
            {
                return (Color.FromArgb(8, 255, 255, 255), normalFg, transparent);
            }
            return (transparent, Color.FromRgb(230, 230, 230), transparent);
        }
    }

    private string? GetContentText()
    {
        if (Content == null) return null;

        if (Content is string str)
            return str;

        if (Content is TextBlock textBlock)
            return textBlock.Text;

        return Content.ToString();
    }

    #endregion

    #region Property Changed Callbacks

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NavigationViewItem item)
        {
            item.InvalidateVisual();
        }
    }

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NavigationViewItem item)
        {
            item.SelectionChanged?.Invoke(item, (bool)(e.NewValue ?? false));
            item.InvalidateVisual();
        }
    }

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NavigationViewItem item)
        {
            var isExpanded = (bool)(e.NewValue ?? false);
            item._childrenPanel.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
            item.ExpansionChanged?.Invoke(item, isExpanded);
            item.InvalidateMeasure();
            item.InvalidateVisual();
        }
    }

    #endregion
}

/// <summary>
/// Provides data for the NavigationViewItem.Invoked event.
/// </summary>
public class NavigationViewItemInvokedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the invoked item.
    /// </summary>
    public NavigationViewItem InvokedItem { get; }

    /// <summary>
    /// Gets a value indicating whether the invoked item is a settings item.
    /// </summary>
    public bool IsSettingsInvoked { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationViewItemInvokedEventArgs"/> class.
    /// </summary>
    public NavigationViewItemInvokedEventArgs(NavigationViewItem invokedItem, bool isSettingsInvoked = false)
    {
        InvokedItem = invokedItem;
        IsSettingsInvoked = isSettingsInvoked;
    }
}

/// <summary>
/// Represents a header item in a NavigationView.
/// </summary>
public class NavigationViewItemHeader : ContentControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationViewItemHeader"/> class.
    /// </summary>
    public NavigationViewItemHeader()
    {
        Focusable = false;
        Height = 40;
        Margin = new Thickness(12, 8, 12, 4);
        Foreground = new SolidColorBrush(Color.FromRgb(157, 157, 157));
    }

    /// <summary>
    /// Override to prevent Content from being added to visual tree.
    /// </summary>
    protected override void OnContentChanged(object? oldContent, object? newContent)
    {
        // Don't call base - we render content manually
        InvalidateMeasure();
        InvalidateVisual();
    }

    protected override void OnRender(object drawingContextObj)
    {
        if (drawingContextObj is not DrawingContext dc)
            return;

        var text = GetContentText();
        if (!string.IsNullOrEmpty(text))
        {
            var fontMetrics = TextMeasurement.GetFontMetrics("Segoe UI Semibold", 14);
            var brush = Foreground ?? new SolidColorBrush(Color.FromRgb(157, 157, 157));
            var formattedText = new FormattedText(text, "Segoe UI Semibold", 14)
            {
                Foreground = brush
            };
            var textY = (ActualHeight - fontMetrics.LineHeight) / 2;
            dc.DrawText(formattedText, new Point(0, textY));
        }
    }

    private string? GetContentText()
    {
        if (Content == null) return null;
        if (Content is string str) return str;
        if (Content is TextBlock textBlock) return textBlock.Text;
        return Content.ToString();
    }
}

/// <summary>
/// Represents a separator in a NavigationView.
/// </summary>
public class NavigationViewItemSeparator : Control
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationViewItemSeparator"/> class.
    /// </summary>
    public NavigationViewItemSeparator()
    {
        Focusable = false;
        Height = 1;
        Margin = new Thickness(16, 8, 16, 8);
        Background = new SolidColorBrush(Color.FromRgb(60, 60, 60));
    }

    protected override void OnRender(object drawingContextObj)
    {
        if (drawingContextObj is not DrawingContext dc)
        {
            base.OnRender(drawingContextObj);
            return;
        }

        var brush = Background ?? new SolidColorBrush(Color.FromRgb(60, 60, 60));
        dc.DrawRectangle(brush, null, new Rect(0, 0, ActualWidth, 1));
    }
}
