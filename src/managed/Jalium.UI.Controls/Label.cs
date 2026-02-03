using Jalium.UI.Input;
using Jalium.UI.Interop;
using Jalium.UI.Media;

namespace Jalium.UI.Controls;

/// <summary>
/// Represents a label control that displays text and can be associated with a target element.
/// When the label's access key is pressed, focus moves to the target element.
/// </summary>
public class Label : ContentControl
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the Target dependency property.
    /// </summary>
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register(nameof(Target), typeof(UIElement), typeof(Label),
            new PropertyMetadata(null));

    /// <summary>
    /// Identifies the AccessKey dependency property.
    /// </summary>
    public static readonly DependencyProperty AccessKeyProperty =
        DependencyProperty.Register(nameof(AccessKey), typeof(char?), typeof(Label),
            new PropertyMetadata(null, OnVisualPropertyChanged));

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the target element that receives focus when the label's access key is pressed.
    /// </summary>
    public UIElement? Target
    {
        get => (UIElement?)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    /// <summary>
    /// Gets or sets the access key character for this label.
    /// </summary>
    public char? AccessKey
    {
        get => (char?)GetValue(AccessKeyProperty);
        set => SetValue(AccessKeyProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="Label"/> class.
    /// </summary>
    public Label()
    {
        // Labels are not focusable by default
        Focusable = false;

        // Register mouse click handler to focus target
        AddHandler(MouseDownEvent, new RoutedEventHandler(OnMouseDownHandler));
    }

    #endregion

    #region Template Parts

    private Border? _labelBorder;

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _labelBorder = GetTemplateChild("LabelBorder") as Border;
    }

    #endregion

    #region Input Handling

    private void OnMouseDownHandler(object sender, RoutedEventArgs e)
    {
        if (e is MouseButtonEventArgs mouseArgs && mouseArgs.ChangedButton == MouseButton.Left)
        {
            FocusTarget();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Moves focus to the target element if one is set.
    /// </summary>
    private void FocusTarget()
    {
        if (Target is FrameworkElement fe && fe.Focusable)
        {
            fe.Focus();
        }
    }

    #endregion

    #region Layout

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;
        var border = BorderThickness;
        var contentAvailable = new Size(
            Math.Max(0, availableSize.Width - padding.TotalWidth - border.TotalWidth),
            Math.Max(0, availableSize.Height - padding.TotalHeight - border.TotalHeight));

        var contentSize = MeasureContent(contentAvailable);

        return new Size(
            contentSize.Width + padding.TotalWidth + border.TotalWidth,
            contentSize.Height + padding.TotalHeight + border.TotalHeight);
    }

    private Size MeasureContent(Size availableSize)
    {
        if (Content is string text)
        {
            var fontFamily = FontFamily ?? "Segoe UI";
            var fontSize = FontSize > 0 ? FontSize : 14;
            var formattedText = new FormattedText(text, fontFamily, fontSize);
            TextMeasurement.MeasureText(formattedText);
            return new Size(formattedText.Width, formattedText.Height);
        }

        if (Content is UIElement element)
        {
            element.Measure(availableSize);
            return element.DesiredSize;
        }

        return new Size(0, 0);
    }

    #endregion

    #region Rendering

    /// <inheritdoc />
    protected override void OnRender(object drawingContext)
    {
        // If using template, let the template handle rendering
        if (_labelBorder != null)
            return;

        if (drawingContext is not DrawingContext dc)
            return;

        var rect = new Rect(RenderSize);
        var padding = Padding;

        // Draw background if set
        if (Background != null)
        {
            dc.DrawRectangle(Background, null, rect);
        }

        // Draw content
        if (Content is string text && Foreground != null)
        {
            var formattedText = new FormattedText(text, FontFamily, FontSize)
            {
                Foreground = Foreground
            };

            TextMeasurement.MeasureText(formattedText);

            // Calculate text position based on alignment
            var textX = padding.Left;
            var textY = padding.Top;

            switch (HorizontalAlignment)
            {
                case HorizontalAlignment.Center:
                    textX = (rect.Width - formattedText.Width) / 2;
                    break;
                case HorizontalAlignment.Right:
                    textX = rect.Width - formattedText.Width - padding.Right;
                    break;
            }

            switch (VerticalAlignment)
            {
                case VerticalAlignment.Center:
                    textY = (rect.Height - formattedText.Height) / 2;
                    break;
                case VerticalAlignment.Bottom:
                    textY = rect.Height - formattedText.Height - padding.Bottom;
                    break;
            }

            dc.DrawText(formattedText, new Point(textX, textY));

            // Draw access key underline if set
            if (AccessKey.HasValue)
            {
                var accessKeyIndex = text.IndexOf(AccessKey.Value.ToString(), StringComparison.OrdinalIgnoreCase);
                if (accessKeyIndex >= 0)
                {
                    // Calculate underline position (approximate)
                    var preText = text.Substring(0, accessKeyIndex);
                    var preFormattedText = new FormattedText(preText, FontFamily, FontSize);
                    TextMeasurement.MeasureText(preFormattedText);

                    var charText = text.Substring(accessKeyIndex, 1);
                    var charFormattedText = new FormattedText(charText, FontFamily, FontSize);
                    TextMeasurement.MeasureText(charFormattedText);

                    var underlineX = textX + preFormattedText.Width;
                    var underlineY = textY + formattedText.Height - 2;
                    var underlineWidth = charFormattedText.Width;

                    var underlinePen = new Pen(Foreground, 1);
                    dc.DrawLine(underlinePen, new Point(underlineX, underlineY), new Point(underlineX + underlineWidth, underlineY));
                }
            }
        }
    }

    #endregion

    #region Property Changed Callbacks

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Label label)
        {
            label.InvalidateVisual();
        }
    }

    #endregion
}
