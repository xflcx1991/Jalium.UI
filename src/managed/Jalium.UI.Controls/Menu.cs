using Jalium.UI.Input;
using Jalium.UI.Interop;
using Jalium.UI.Media;

namespace Jalium.UI.Controls;

/// <summary>
/// Represents a menu bar that contains menu items.
/// </summary>
public class Menu : ItemsControl
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the IsMainMenu dependency property.
    /// </summary>
    public static readonly DependencyProperty IsMainMenuProperty =
        DependencyProperty.Register(nameof(IsMainMenu), typeof(bool), typeof(Menu),
            new PropertyMetadata(true));

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets a value indicating whether this is the main application menu.
    /// </summary>
    public bool IsMainMenu
    {
        get => (bool)(GetValue(IsMainMenuProperty) ?? true);
        set => SetValue(IsMainMenuProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="Menu"/> class.
    /// </summary>
    public Menu()
    {
        Background = new SolidColorBrush(Color.FromRgb(45, 45, 45));
        Foreground = new SolidColorBrush(Color.White);
        Height = 28;
    }

    #endregion

    #region Item Generation

    /// <inheritdoc />
    protected override Panel CreateItemsPanel()
    {
        return new StackPanel { Orientation = Orientation.Horizontal };
    }

    /// <inheritdoc />
    protected override FrameworkElement GetContainerForItem(object item)
    {
        return new MenuItem();
    }

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainer(object item)
    {
        return item is MenuItem || item is Separator;
    }

    /// <inheritdoc />
    protected override void PrepareContainerForItem(FrameworkElement element, object item)
    {
        if (element is MenuItem menuItem && item is string text)
        {
            menuItem.Header = text;
        }
    }

    #endregion

    #region Rendering

    /// <inheritdoc />
    protected override void OnRender(object drawingContext)
    {
        if (drawingContext is not DrawingContext dc)
            return;

        var rect = new Rect(RenderSize);

        // Draw background
        if (Background != null)
        {
            dc.DrawRectangle(Background, null, rect);
        }

        // Draw bottom border
        if (BorderBrush != null)
        {
            var borderPen = new Pen(BorderBrush, 1);
            dc.DrawLine(borderPen, new Point(0, rect.Height - 1), new Point(rect.Width, rect.Height - 1));
        }
    }

    #endregion
}

/// <summary>
/// Represents an item in a menu.
/// </summary>
public class MenuItem : HeaderedItemsControl
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the Icon dependency property.
    /// </summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(object), typeof(MenuItem),
            new PropertyMetadata(null, OnVisualPropertyChanged));

    /// <summary>
    /// Identifies the InputGestureText dependency property.
    /// </summary>
    public static readonly DependencyProperty InputGestureTextProperty =
        DependencyProperty.Register(nameof(InputGestureText), typeof(string), typeof(MenuItem),
            new PropertyMetadata(null, OnVisualPropertyChanged));

    /// <summary>
    /// Identifies the IsCheckable dependency property.
    /// </summary>
    public static readonly DependencyProperty IsCheckableProperty =
        DependencyProperty.Register(nameof(IsCheckable), typeof(bool), typeof(MenuItem),
            new PropertyMetadata(false));

    /// <summary>
    /// Identifies the IsChecked dependency property.
    /// </summary>
    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(MenuItem),
            new PropertyMetadata(false, OnIsCheckedChanged));

    /// <summary>
    /// Identifies the IsSubmenuOpen dependency property.
    /// </summary>
    public static readonly DependencyProperty IsSubmenuOpenProperty =
        DependencyProperty.Register(nameof(IsSubmenuOpen), typeof(bool), typeof(MenuItem),
            new PropertyMetadata(false, OnIsSubmenuOpenChanged));

    /// <summary>
    /// Identifies the StaysOpenOnClick dependency property.
    /// </summary>
    public static readonly DependencyProperty StaysOpenOnClickProperty =
        DependencyProperty.Register(nameof(StaysOpenOnClick), typeof(bool), typeof(MenuItem),
            new PropertyMetadata(false));

    #endregion

    #region Routed Events

    /// <summary>
    /// Identifies the Click routed event.
    /// </summary>
    public static readonly RoutedEvent ClickEvent =
        EventManager.RegisterRoutedEvent(nameof(Click), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(MenuItem));

    /// <summary>
    /// Identifies the Checked routed event.
    /// </summary>
    public static readonly RoutedEvent CheckedEvent =
        EventManager.RegisterRoutedEvent(nameof(Checked), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(MenuItem));

    /// <summary>
    /// Identifies the Unchecked routed event.
    /// </summary>
    public static readonly RoutedEvent UncheckedEvent =
        EventManager.RegisterRoutedEvent(nameof(Unchecked), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(MenuItem));

    /// <summary>
    /// Identifies the SubmenuOpened routed event.
    /// </summary>
    public static readonly RoutedEvent SubmenuOpenedEvent =
        EventManager.RegisterRoutedEvent(nameof(SubmenuOpened), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(MenuItem));

    /// <summary>
    /// Identifies the SubmenuClosed routed event.
    /// </summary>
    public static readonly RoutedEvent SubmenuClosedEvent =
        EventManager.RegisterRoutedEvent(nameof(SubmenuClosed), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(MenuItem));

    /// <summary>
    /// Occurs when the menu item is clicked.
    /// </summary>
    public event RoutedEventHandler Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    /// <summary>
    /// Occurs when the menu item is checked.
    /// </summary>
    public event RoutedEventHandler Checked
    {
        add => AddHandler(CheckedEvent, value);
        remove => RemoveHandler(CheckedEvent, value);
    }

    /// <summary>
    /// Occurs when the menu item is unchecked.
    /// </summary>
    public event RoutedEventHandler Unchecked
    {
        add => AddHandler(UncheckedEvent, value);
        remove => RemoveHandler(UncheckedEvent, value);
    }

    /// <summary>
    /// Occurs when the submenu is opened.
    /// </summary>
    public event RoutedEventHandler SubmenuOpened
    {
        add => AddHandler(SubmenuOpenedEvent, value);
        remove => RemoveHandler(SubmenuOpenedEvent, value);
    }

    /// <summary>
    /// Occurs when the submenu is closed.
    /// </summary>
    public event RoutedEventHandler SubmenuClosed
    {
        add => AddHandler(SubmenuClosedEvent, value);
        remove => RemoveHandler(SubmenuClosedEvent, value);
    }

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the icon displayed with the menu item.
    /// </summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets the input gesture text (keyboard shortcut) displayed with the menu item.
    /// </summary>
    public string? InputGestureText
    {
        get => (string?)GetValue(InputGestureTextProperty);
        set => SetValue(InputGestureTextProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the menu item can be checked.
    /// </summary>
    public bool IsCheckable
    {
        get => (bool)(GetValue(IsCheckableProperty) ?? false);
        set => SetValue(IsCheckableProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the menu item is checked.
    /// </summary>
    public bool IsChecked
    {
        get => (bool)(GetValue(IsCheckedProperty) ?? false);
        set => SetValue(IsCheckedProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the submenu is open.
    /// </summary>
    public bool IsSubmenuOpen
    {
        get => (bool)(GetValue(IsSubmenuOpenProperty) ?? false);
        set => SetValue(IsSubmenuOpenProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the menu stays open when clicked.
    /// </summary>
    public bool StaysOpenOnClick
    {
        get => (bool)(GetValue(StaysOpenOnClickProperty) ?? false);
        set => SetValue(StaysOpenOnClickProperty, value);
    }

    /// <summary>
    /// Gets a value indicating whether this menu item has sub-items.
    /// </summary>
    public bool HasItems => Items.Count > 0;

    #endregion

    #region Private Fields

    private bool _isHighlighted;
    private const double IconColumnWidth = 24;
    private const double GestureColumnWidth = 80;
    private const double ArrowColumnWidth = 16;
    private const double ItemHeight = 28;
    private const double MenuItemPadding = 8;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MenuItem"/> class.
    /// </summary>
    public MenuItem()
    {
        Focusable = true;
        Height = ItemHeight;
        Padding = new Thickness(MenuItemPadding, 0, MenuItemPadding, 0);

        AddHandler(MouseDownEvent, new RoutedEventHandler(OnMouseDownHandler));
        AddHandler(MouseEnterEvent, new RoutedEventHandler(OnMouseEnterHandler));
        AddHandler(MouseLeaveEvent, new RoutedEventHandler(OnMouseLeaveHandler));
        AddHandler(KeyDownEvent, new RoutedEventHandler(OnKeyDownHandler));
    }

    #endregion

    #region Input Handling

    private void OnMouseDownHandler(object sender, RoutedEventArgs e)
    {
        if (!IsEnabled) return;

        if (e is MouseButtonEventArgs mouseArgs && mouseArgs.ChangedButton == MouseButton.Left)
        {
            if (HasItems)
            {
                IsSubmenuOpen = !IsSubmenuOpen;
            }
            else
            {
                OnClick();
            }
            e.Handled = true;
        }
    }

    private void OnMouseEnterHandler(object sender, RoutedEventArgs e)
    {
        _isHighlighted = true;
        InvalidateVisual();

        // Open submenu on hover if parent menu has an open submenu
        if (HasItems)
        {
            IsSubmenuOpen = true;
        }
    }

    private void OnMouseLeaveHandler(object sender, RoutedEventArgs e)
    {
        _isHighlighted = false;
        InvalidateVisual();
    }

    private void OnKeyDownHandler(object sender, RoutedEventArgs e)
    {
        if (!IsEnabled) return;

        if (e is KeyEventArgs keyArgs)
        {
            if (keyArgs.Key == Key.Enter || keyArgs.Key == Key.Space)
            {
                if (HasItems)
                {
                    IsSubmenuOpen = true;
                }
                else
                {
                    OnClick();
                }
                e.Handled = true;
            }
            else if (keyArgs.Key == Key.Right && HasItems)
            {
                IsSubmenuOpen = true;
                e.Handled = true;
            }
            else if (keyArgs.Key == Key.Left && IsSubmenuOpen)
            {
                IsSubmenuOpen = false;
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// Called when the menu item is clicked.
    /// </summary>
    protected virtual void OnClick()
    {
        if (IsCheckable)
        {
            IsChecked = !IsChecked;
        }

        RaiseEvent(new RoutedEventArgs(ClickEvent, this));

        if (!StaysOpenOnClick)
        {
            CloseParentMenus();
        }
    }

    private void CloseParentMenus()
    {
        // Close all parent menus
        var parent = VisualParent;
        while (parent != null)
        {
            if (parent is MenuItem parentItem)
            {
                parentItem.IsSubmenuOpen = false;
            }
            parent = (parent as Visual)?.VisualParent;
        }
    }

    #endregion

    #region Layout

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;
        var width = padding.TotalWidth;
        var height = ItemHeight;

        // Determine if we're a top-level menu item or a submenu item
        var isTopLevel = VisualParent is Panel p && p.VisualParent is Menu;

        if (isTopLevel)
        {
            // Top-level: just header
            if (Header is string headerText)
            {
                var formattedText = new FormattedText(headerText, FontFamily ?? "Segoe UI", FontSize > 0 ? FontSize : 14);
                TextMeasurement.MeasureText(formattedText);
                width += formattedText.Width;
            }
        }
        else
        {
            // Submenu item: icon + header + gesture + arrow
            width = IconColumnWidth;

            if (Header is string headerText)
            {
                var formattedText = new FormattedText(headerText, FontFamily ?? "Segoe UI", FontSize > 0 ? FontSize : 14);
                TextMeasurement.MeasureText(formattedText);
                width += formattedText.Width + MenuItemPadding * 2;
            }

            if (!string.IsNullOrEmpty(InputGestureText))
            {
                width += GestureColumnWidth;
            }

            if (HasItems)
            {
                width += ArrowColumnWidth;
            }
        }

        return new Size(Math.Max(width, 50), height);
    }

    #endregion

    #region Rendering

    /// <inheritdoc />
    protected override void OnRender(object drawingContext)
    {
        if (drawingContext is not DrawingContext dc)
            return;

        var rect = new Rect(RenderSize);
        var padding = Padding;
        var isTopLevel = VisualParent is Panel p && p.VisualParent is Menu;

        // Draw background
        if (_isHighlighted || IsSubmenuOpen)
        {
            var highlightBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            dc.DrawRectangle(highlightBrush, null, rect);
        }
        else if (Background != null)
        {
            dc.DrawRectangle(Background, null, rect);
        }

        var fgBrush = IsEnabled
            ? (Foreground ?? new SolidColorBrush(Color.White))
            : new SolidColorBrush(Color.FromRgb(128, 128, 128));

        if (isTopLevel)
        {
            // Top-level menu item
            if (Header is string headerText)
            {
                var formattedText = new FormattedText(headerText, FontFamily ?? "Segoe UI", FontSize > 0 ? FontSize : 14)
                {
                    Foreground = fgBrush
                };
                TextMeasurement.MeasureText(formattedText);

                var textX = padding.Left;
                var textY = (rect.Height - formattedText.Height) / 2;
                dc.DrawText(formattedText, new Point(textX, textY));
            }
        }
        else
        {
            // Submenu item
            var currentX = 0.0;

            // Draw check mark or icon
            if (IsCheckable && IsChecked)
            {
                DrawCheckMark(dc, currentX, rect.Height, fgBrush);
            }
            else if (Icon is string iconText)
            {
                var iconFormatted = new FormattedText(iconText, "Segoe UI Symbol", 12)
                {
                    Foreground = fgBrush
                };
                TextMeasurement.MeasureText(iconFormatted);
                dc.DrawText(iconFormatted, new Point(currentX + (IconColumnWidth - iconFormatted.Width) / 2, (rect.Height - iconFormatted.Height) / 2));
            }
            currentX += IconColumnWidth;

            // Draw header
            if (Header is string headerText)
            {
                var headerFormatted = new FormattedText(headerText, FontFamily ?? "Segoe UI", FontSize > 0 ? FontSize : 14)
                {
                    Foreground = fgBrush
                };
                TextMeasurement.MeasureText(headerFormatted);
                dc.DrawText(headerFormatted, new Point(currentX, (rect.Height - headerFormatted.Height) / 2));
            }

            // Draw input gesture text
            if (!string.IsNullOrEmpty(InputGestureText))
            {
                var gestureX = rect.Width - ArrowColumnWidth - GestureColumnWidth;
                var gestureFormatted = new FormattedText(InputGestureText, FontFamily ?? "Segoe UI", FontSize > 0 ? FontSize : 12)
                {
                    Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160))
                };
                TextMeasurement.MeasureText(gestureFormatted);
                dc.DrawText(gestureFormatted, new Point(gestureX, (rect.Height - gestureFormatted.Height) / 2));
            }

            // Draw submenu arrow
            if (HasItems)
            {
                DrawSubmenuArrow(dc, rect.Width - ArrowColumnWidth, rect.Height, fgBrush);
            }
        }
    }

    private void DrawCheckMark(DrawingContext dc, double x, double height, Brush brush)
    {
        var checkPen = new Pen(brush, 2);
        var centerX = x + IconColumnWidth / 2;
        var centerY = height / 2;

        dc.DrawLine(checkPen, new Point(centerX - 4, centerY), new Point(centerX - 1, centerY + 3));
        dc.DrawLine(checkPen, new Point(centerX - 1, centerY + 3), new Point(centerX + 4, centerY - 3));
    }

    private void DrawSubmenuArrow(DrawingContext dc, double x, double height, Brush brush)
    {
        var arrowPen = new Pen(brush, 1.5);
        var centerX = x + ArrowColumnWidth / 2;
        var centerY = height / 2;

        dc.DrawLine(arrowPen, new Point(centerX - 2, centerY - 4), new Point(centerX + 2, centerY));
        dc.DrawLine(arrowPen, new Point(centerX + 2, centerY), new Point(centerX - 2, centerY + 4));
    }

    #endregion

    #region Property Changed Callbacks

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MenuItem menuItem)
        {
            menuItem.InvalidateVisual();
        }
    }

    private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MenuItem menuItem)
        {
            if ((bool)e.NewValue)
            {
                menuItem.RaiseEvent(new RoutedEventArgs(CheckedEvent, menuItem));
            }
            else
            {
                menuItem.RaiseEvent(new RoutedEventArgs(UncheckedEvent, menuItem));
            }
            menuItem.InvalidateVisual();
        }
    }

    private static void OnIsSubmenuOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MenuItem menuItem)
        {
            if ((bool)e.NewValue)
            {
                menuItem.RaiseEvent(new RoutedEventArgs(SubmenuOpenedEvent, menuItem));
            }
            else
            {
                menuItem.RaiseEvent(new RoutedEventArgs(SubmenuClosedEvent, menuItem));
            }
            menuItem.InvalidateVisual();
        }
    }

    #endregion
}

/// <summary>
/// Base class for controls that have both a header and items.
/// </summary>
public class HeaderedItemsControl : ItemsControl
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the Header dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(object), typeof(HeaderedItemsControl),
            new PropertyMetadata(null, OnHeaderChanged));

    /// <summary>
    /// Identifies the HeaderTemplate dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(nameof(HeaderTemplate), typeof(DataTemplate), typeof(HeaderedItemsControl),
            new PropertyMetadata(null));

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the header content.
    /// </summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Gets or sets the template for the header.
    /// </summary>
    public DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    #endregion

    #region Property Changed Callbacks

    private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HeaderedItemsControl control)
        {
            control.InvalidateMeasure();
        }
    }

    #endregion
}
