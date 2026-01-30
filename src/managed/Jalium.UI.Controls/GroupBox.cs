using Jalium.UI.Media;

namespace Jalium.UI.Controls;

/// <summary>
/// Represents a control that displays a frame around a group of controls with an optional caption.
/// </summary>
public class GroupBox : ContentControl
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the Header dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(object), typeof(GroupBox),
            new PropertyMetadata(null, OnHeaderChanged));

    /// <summary>
    /// Identifies the HeaderBackground dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderBackgroundProperty =
        DependencyProperty.Register(nameof(HeaderBackground), typeof(Brush), typeof(GroupBox),
            new PropertyMetadata(null, OnVisualPropertyChanged));

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
    /// Gets or sets the background brush for the header area.
    /// </summary>
    public Brush? HeaderBackground
    {
        get => (Brush?)GetValue(HeaderBackgroundProperty);
        set => SetValue(HeaderBackgroundProperty, value);
    }

    #endregion

    #region Private Fields

    private const double HeaderMarginLeft = 8;
    private const double HeaderPaddingHorizontal = 4;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupBox"/> class.
    /// </summary>
    public GroupBox()
    {
        // Default styling
        BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100));
        BorderThickness = new Thickness(1);
        Padding = new Thickness(8, 8, 8, 8);
    }

    #endregion

    #region Layout

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;
        var border = BorderThickness;
        var headerSize = MeasureHeader(availableSize);

        var contentAvailable = new Size(
            Math.Max(0, availableSize.Width - padding.TotalWidth - border.TotalWidth),
            Math.Max(0, availableSize.Height - headerSize.Height / 2 - padding.TotalHeight - border.TotalHeight));

        var contentSize = MeasureContent(contentAvailable);

        return new Size(
            Math.Max(headerSize.Width + HeaderMarginLeft * 2, contentSize.Width + padding.TotalWidth + border.TotalWidth),
            headerSize.Height / 2 + contentSize.Height + padding.TotalHeight + border.TotalHeight);
    }

    private Size MeasureHeader(Size availableSize)
    {
        if (Header is string text)
        {
            var fontFamily = FontFamily ?? "Segoe UI";
            var fontSize = FontSize > 0 ? FontSize : 14;
            var formattedText = new FormattedText(text, fontFamily, fontSize);
            Interop.TextMeasurement.MeasureText(formattedText);
            return new Size(formattedText.Width + HeaderPaddingHorizontal * 2, formattedText.Height);
        }

        if (Header is UIElement element)
        {
            element.Measure(availableSize);
            return new Size(element.DesiredSize.Width + HeaderPaddingHorizontal * 2, element.DesiredSize.Height);
        }

        return Size.Empty;
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
        if (ContentElement is FrameworkElement fe)
        {
            var padding = Padding;
            var border = BorderThickness;
            var headerSize = MeasureHeader(finalSize);

            var contentRect = new Rect(
                padding.Left + border.Left,
                headerSize.Height / 2 + padding.Top + border.Top,
                Math.Max(0, finalSize.Width - padding.TotalWidth - border.TotalWidth),
                Math.Max(0, finalSize.Height - headerSize.Height / 2 - padding.TotalHeight - border.TotalHeight));

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
        var headerSize = MeasureHeader(RenderSize);
        var headerTextHeight = headerSize.Height;
        var cornerRadius = CornerRadius;
        var hasCornerRadius = cornerRadius.TopLeft > 0;

        // Calculate the border rect (starts at half header height)
        var borderRect = new Rect(0, headerTextHeight / 2, rect.Width, rect.Height - headerTextHeight / 2);

        // Draw background
        if (Background != null)
        {
            if (hasCornerRadius)
            {
                dc.DrawRoundedRectangle(Background, null, borderRect, cornerRadius.TopLeft, cornerRadius.TopLeft);
            }
            else
            {
                dc.DrawRectangle(Background, null, borderRect);
            }
        }

        // Draw border (with gap for header)
        if (BorderBrush != null && BorderThickness.TotalWidth > 0)
        {
            var pen = new Pen(BorderBrush, BorderThickness.Left);

            if (Header != null && headerSize.Width > 0)
            {
                // Draw border with gap for header text
                var headerStartX = HeaderMarginLeft;
                var headerEndX = HeaderMarginLeft + headerSize.Width;

                // Top-left to header start
                dc.DrawLine(pen, new Point(0, borderRect.Top), new Point(headerStartX, borderRect.Top));

                // Header end to top-right
                dc.DrawLine(pen, new Point(headerEndX, borderRect.Top), new Point(borderRect.Right, borderRect.Top));

                // Right edge
                dc.DrawLine(pen, new Point(borderRect.Right, borderRect.Top), new Point(borderRect.Right, borderRect.Bottom));

                // Bottom edge
                dc.DrawLine(pen, new Point(borderRect.Right, borderRect.Bottom), new Point(borderRect.Left, borderRect.Bottom));

                // Left edge
                dc.DrawLine(pen, new Point(borderRect.Left, borderRect.Bottom), new Point(borderRect.Left, borderRect.Top));
            }
            else
            {
                // Draw full border
                if (hasCornerRadius)
                {
                    dc.DrawRoundedRectangle(null, pen, borderRect, cornerRadius.TopLeft, cornerRadius.TopLeft);
                }
                else
                {
                    dc.DrawRectangle(null, pen, borderRect);
                }
            }
        }

        // Draw header
        if (Header != null)
        {
            // Draw header background if set
            if (HeaderBackground != null)
            {
                var headerBgRect = new Rect(HeaderMarginLeft, 0, headerSize.Width, headerTextHeight);
                dc.DrawRectangle(HeaderBackground, null, headerBgRect);
            }
            else if (Background != null)
            {
                // Use parent background to "cover" the border
                var headerBgRect = new Rect(HeaderMarginLeft, 0, headerSize.Width, headerTextHeight);
                dc.DrawRectangle(Background, null, headerBgRect);
            }

            // Draw header text
            if (Header is string headerText && Foreground != null)
            {
                var formattedText = new FormattedText(headerText, FontFamily ?? "Segoe UI", FontSize > 0 ? FontSize : 14)
                {
                    Foreground = Foreground
                };
                Interop.TextMeasurement.MeasureText(formattedText);

                var textX = HeaderMarginLeft + HeaderPaddingHorizontal;
                var textY = (headerTextHeight - formattedText.Height) / 2;
                dc.DrawText(formattedText, new Point(textX, textY));
            }
        }

        // Draw content
        if (Content is string contentText && Foreground != null)
        {
            var formattedText = new FormattedText(contentText, FontFamily ?? "Segoe UI", FontSize > 0 ? FontSize : 14)
            {
                Foreground = Foreground
            };
            Interop.TextMeasurement.MeasureText(formattedText);

            var padding = Padding;
            var textX = padding.Left;
            var textY = headerTextHeight / 2 + padding.Top;
            dc.DrawText(formattedText, new Point(textX, textY));
        }
    }

    #endregion

    #region Property Changed Callbacks

    private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GroupBox groupBox)
        {
            groupBox.InvalidateMeasure();
        }
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GroupBox groupBox)
        {
            groupBox.InvalidateVisual();
        }
    }

    #endregion
}
