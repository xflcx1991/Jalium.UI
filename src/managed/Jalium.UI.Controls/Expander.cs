using Jalium.UI.Input;
using Jalium.UI.Media;

namespace Jalium.UI.Controls;

/// <summary>
/// Represents a control that displays a header and has a collapsible content area.
/// </summary>
public class Expander : ContentControl
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the IsExpanded dependency property.
    /// </summary>
    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(Expander),
            new PropertyMetadata(false, OnIsExpandedChanged));

    /// <summary>
    /// Identifies the Header dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(object), typeof(Expander),
            new PropertyMetadata(null, OnHeaderChanged));

    /// <summary>
    /// Identifies the ExpandDirection dependency property.
    /// </summary>
    public static readonly DependencyProperty ExpandDirectionProperty =
        DependencyProperty.Register(nameof(ExpandDirection), typeof(ExpandDirection), typeof(Expander),
            new PropertyMetadata(ExpandDirection.Down, OnLayoutPropertyChanged));

    /// <summary>
    /// Identifies the HeaderBackground dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderBackgroundProperty =
        DependencyProperty.Register(nameof(HeaderBackground), typeof(Brush), typeof(Expander),
            new PropertyMetadata(null, OnVisualPropertyChanged));

    #endregion

    #region Routed Events

    /// <summary>
    /// Identifies the Expanded routed event.
    /// </summary>
    public static readonly RoutedEvent ExpandedEvent =
        EventManager.RegisterRoutedEvent(nameof(Expanded), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(Expander));

    /// <summary>
    /// Identifies the Collapsed routed event.
    /// </summary>
    public static readonly RoutedEvent CollapsedEvent =
        EventManager.RegisterRoutedEvent(nameof(Collapsed), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(Expander));

    /// <summary>
    /// Occurs when the expander is expanded.
    /// </summary>
    public event RoutedEventHandler Expanded
    {
        add => AddHandler(ExpandedEvent, value);
        remove => RemoveHandler(ExpandedEvent, value);
    }

    /// <summary>
    /// Occurs when the expander is collapsed.
    /// </summary>
    public event RoutedEventHandler Collapsed
    {
        add => AddHandler(CollapsedEvent, value);
        remove => RemoveHandler(CollapsedEvent, value);
    }

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets a value indicating whether the expander is expanded.
    /// </summary>
    public bool IsExpanded
    {
        get => (bool)(GetValue(IsExpandedProperty) ?? false);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>
    /// Gets or sets the header content.
    /// </summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Gets or sets the direction in which the content area expands.
    /// </summary>
    public ExpandDirection ExpandDirection
    {
        get => (ExpandDirection)(GetValue(ExpandDirectionProperty) ?? ExpandDirection.Down);
        set => SetValue(ExpandDirectionProperty, value);
    }

    /// <summary>
    /// Gets or sets the background brush for the header area.
    /// </summary>
    public Brush? HeaderBackground
    {
        get => (Brush?)GetValue(HeaderBackgroundProperty);
        set => SetValue(HeaderBackgroundProperty, value);
    }

    #endregion

    #region Private Fields

    private const double HeaderHeight = 36;
    private const double ChevronSize = 12;
    private const double ChevronMargin = 12;
    private UIElement? _headerElement;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="Expander"/> class.
    /// </summary>
    public Expander()
    {
        Focusable = true;

        // Register input handlers
        AddHandler(MouseDownEvent, new RoutedEventHandler(OnMouseDownHandler));
        AddHandler(KeyDownEvent, new RoutedEventHandler(OnKeyDownHandler));
    }

    #endregion

    #region Input Handling

    private void OnMouseDownHandler(object sender, RoutedEventArgs e)
    {
        if (!IsEnabled) return;

        if (e is MouseButtonEventArgs mouseArgs && mouseArgs.ChangedButton == MouseButton.Left)
        {
            var position = mouseArgs.GetPosition(this);

            // Check if click is in header area
            var headerRect = GetHeaderRect();
            if (headerRect.Contains(position))
            {
                Focus();
                Toggle();
                e.Handled = true;
            }
        }
    }

    private void OnKeyDownHandler(object sender, RoutedEventArgs e)
    {
        if (!IsEnabled) return;

        if (e is KeyEventArgs keyArgs)
        {
            if (keyArgs.Key == Key.Space || keyArgs.Key == Key.Enter)
            {
                Toggle();
                e.Handled = true;
            }
        }
    }

    private void Toggle()
    {
        IsExpanded = !IsExpanded;
    }

    private Rect GetHeaderRect()
    {
        switch (ExpandDirection)
        {
            case ExpandDirection.Down:
                return new Rect(0, 0, RenderSize.Width, HeaderHeight);
            case ExpandDirection.Up:
                return new Rect(0, RenderSize.Height - HeaderHeight, RenderSize.Width, HeaderHeight);
            case ExpandDirection.Left:
                return new Rect(RenderSize.Width - HeaderHeight, 0, HeaderHeight, RenderSize.Height);
            case ExpandDirection.Right:
                return new Rect(0, 0, HeaderHeight, RenderSize.Height);
            default:
                return new Rect(0, 0, RenderSize.Width, HeaderHeight);
        }
    }

    #endregion

    #region Layout

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;
        var border = BorderThickness;

        var headerSize = MeasureHeader(availableSize);
        var contentSize = Size.Empty;

        if (IsExpanded && Content != null)
        {
            var contentAvailable = new Size(
                Math.Max(0, availableSize.Width - padding.TotalWidth - border.TotalWidth),
                Math.Max(0, availableSize.Height - HeaderHeight - padding.TotalHeight - border.TotalHeight));

            contentSize = MeasureContent(contentAvailable);
        }

        var isVertical = ExpandDirection == ExpandDirection.Down || ExpandDirection == ExpandDirection.Up;

        if (isVertical)
        {
            return new Size(
                Math.Max(headerSize.Width, contentSize.Width) + padding.TotalWidth + border.TotalWidth,
                HeaderHeight + (IsExpanded ? contentSize.Height : 0) + padding.TotalHeight + border.TotalHeight);
        }
        else
        {
            return new Size(
                HeaderHeight + (IsExpanded ? contentSize.Width : 0) + padding.TotalWidth + border.TotalWidth,
                Math.Max(headerSize.Height, contentSize.Height) + padding.TotalHeight + border.TotalHeight);
        }
    }

    private Size MeasureHeader(Size availableSize)
    {
        if (Header is string text)
        {
            var fontFamily = FontFamily ?? "Segoe UI";
            var fontSize = FontSize > 0 ? FontSize : 14;
            var formattedText = new FormattedText(text, fontFamily, fontSize);
            Interop.TextMeasurement.MeasureText(formattedText);
            return new Size(formattedText.Width + ChevronSize + ChevronMargin * 2, HeaderHeight);
        }

        if (Header is UIElement element)
        {
            element.Measure(availableSize);
            return new Size(element.DesiredSize.Width + ChevronSize + ChevronMargin * 2, HeaderHeight);
        }

        return new Size(ChevronSize + ChevronMargin * 2, HeaderHeight);
    }

    private Size MeasureContent(Size availableSize)
    {
        if (Content is UIElement element)
        {
            element.Measure(availableSize);
            return element.DesiredSize;
        }

        if (Content is string text)
        {
            var fontFamily = FontFamily ?? "Segoe UI";
            var fontSize = FontSize > 0 ? FontSize : 14;
            var formattedText = new FormattedText(text, fontFamily, fontSize);
            Interop.TextMeasurement.MeasureText(formattedText);
            return new Size(formattedText.Width, formattedText.Height);
        }

        return Size.Empty;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (IsExpanded && ContentElement is FrameworkElement fe)
        {
            var padding = Padding;
            var border = BorderThickness;

            Rect contentRect;
            switch (ExpandDirection)
            {
                case ExpandDirection.Down:
                    contentRect = new Rect(
                        padding.Left + border.Left,
                        HeaderHeight + padding.Top + border.Top,
                        Math.Max(0, finalSize.Width - padding.TotalWidth - border.TotalWidth),
                        Math.Max(0, finalSize.Height - HeaderHeight - padding.TotalHeight - border.TotalHeight));
                    break;
                case ExpandDirection.Up:
                    contentRect = new Rect(
                        padding.Left + border.Left,
                        padding.Top + border.Top,
                        Math.Max(0, finalSize.Width - padding.TotalWidth - border.TotalWidth),
                        Math.Max(0, finalSize.Height - HeaderHeight - padding.TotalHeight - border.TotalHeight));
                    break;
                default:
                    contentRect = new Rect(
                        padding.Left + border.Left,
                        HeaderHeight + padding.Top + border.Top,
                        Math.Max(0, finalSize.Width - padding.TotalWidth - border.TotalWidth),
                        Math.Max(0, finalSize.Height - HeaderHeight - padding.TotalHeight - border.TotalHeight));
                    break;
            }

            fe.Arrange(contentRect);
        }

        return finalSize;
    }

    #endregion

    #region Rendering

    /// <inheritdoc />
    protected override void OnRender(object drawingContext)
    {
        if (drawingContext is not DrawingContext dc)
            return;

        var rect = new Rect(RenderSize);
        var cornerRadius = CornerRadius;
        var hasCornerRadius = cornerRadius.TopLeft > 0 || cornerRadius.TopRight > 0 ||
                              cornerRadius.BottomLeft > 0 || cornerRadius.BottomRight > 0;

        // Draw background
        if (Background != null)
        {
            if (hasCornerRadius)
            {
                dc.DrawRoundedRectangle(Background, null, rect, cornerRadius.TopLeft, cornerRadius.TopLeft);
            }
            else
            {
                dc.DrawRectangle(Background, null, rect);
            }
        }

        // Draw border
        if (BorderBrush != null && BorderThickness.TotalWidth > 0)
        {
            var pen = new Pen(BorderBrush, BorderThickness.Left);
            if (hasCornerRadius)
            {
                dc.DrawRoundedRectangle(null, pen, rect, cornerRadius.TopLeft, cornerRadius.TopLeft);
            }
            else
            {
                dc.DrawRectangle(null, pen, rect);
            }
        }

        // Draw header
        DrawHeader(dc);

        // Draw content if expanded
        if (IsExpanded)
        {
            DrawContent(dc);
        }
    }

    private void DrawHeader(DrawingContext dc)
    {
        var headerRect = GetHeaderRect();

        // Draw header background
        var headerBg = HeaderBackground ?? new SolidColorBrush(Color.FromRgb(45, 45, 45));
        dc.DrawRectangle(headerBg, null, headerRect);

        // Draw chevron
        var chevronX = headerRect.X + ChevronMargin;
        var chevronY = headerRect.Y + (headerRect.Height - ChevronSize) / 2;
        DrawChevron(dc, chevronX, chevronY, IsExpanded);

        // Draw header text
        if (Header is string headerText && Foreground != null)
        {
            var formattedText = new FormattedText(headerText, FontFamily ?? "Segoe UI", FontSize > 0 ? FontSize : 14)
            {
                Foreground = Foreground
            };
            Interop.TextMeasurement.MeasureText(formattedText);

            var textX = headerRect.X + ChevronSize + ChevronMargin * 2;
            var textY = headerRect.Y + (headerRect.Height - formattedText.Height) / 2;
            dc.DrawText(formattedText, new Point(textX, textY));
        }
    }

    private void DrawChevron(DrawingContext dc, double x, double y, bool expanded)
    {
        var chevronBrush = Foreground ?? new SolidColorBrush(Color.White);
        var chevronPen = new Pen(chevronBrush, 2);

        if (expanded)
        {
            // Down arrow (expanded)
            dc.DrawLine(chevronPen, new Point(x, y + 4), new Point(x + ChevronSize / 2, y + ChevronSize - 4));
            dc.DrawLine(chevronPen, new Point(x + ChevronSize / 2, y + ChevronSize - 4), new Point(x + ChevronSize, y + 4));
        }
        else
        {
            // Right arrow (collapsed)
            dc.DrawLine(chevronPen, new Point(x + 4, y), new Point(x + ChevronSize - 4, y + ChevronSize / 2));
            dc.DrawLine(chevronPen, new Point(x + ChevronSize - 4, y + ChevronSize / 2), new Point(x + 4, y + ChevronSize));
        }
    }

    private void DrawContent(DrawingContext dc)
    {
        if (Content is string contentText && Foreground != null)
        {
            var formattedText = new FormattedText(contentText, FontFamily ?? "Segoe UI", FontSize > 0 ? FontSize : 14)
            {
                Foreground = Foreground
            };
            Interop.TextMeasurement.MeasureText(formattedText);

            var padding = Padding;
            var textX = padding.Left;
            var textY = HeaderHeight + padding.Top;
            dc.DrawText(formattedText, new Point(textX, textY));
        }
    }

    #endregion

    #region Property Changed Callbacks

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Expander expander)
        {
            expander.OnExpandedChanged((bool)e.OldValue, (bool)e.NewValue);
        }
    }

    private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Expander expander)
        {
            expander.InvalidateMeasure();
        }
    }

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Expander expander)
        {
            expander.InvalidateMeasure();
        }
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Expander expander)
        {
            expander.InvalidateVisual();
        }
    }

    /// <summary>
    /// Called when the expanded state changes.
    /// </summary>
    protected virtual void OnExpandedChanged(bool oldValue, bool newValue)
    {
        InvalidateMeasure();

        if (newValue)
        {
            RaiseEvent(new RoutedEventArgs(ExpandedEvent, this));
        }
        else
        {
            RaiseEvent(new RoutedEventArgs(CollapsedEvent, this));
        }
    }

    #endregion
}

/// <summary>
/// Specifies the direction in which an Expander expands.
/// </summary>
public enum ExpandDirection
{
    /// <summary>
    /// The content expands downward.
    /// </summary>
    Down,

    /// <summary>
    /// The content expands upward.
    /// </summary>
    Up,

    /// <summary>
    /// The content expands to the left.
    /// </summary>
    Left,

    /// <summary>
    /// The content expands to the right.
    /// </summary>
    Right
}
