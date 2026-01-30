using Jalium.UI.Interop;
using Jalium.UI.Media;

namespace Jalium.UI.Controls;

/// <summary>
/// Represents a button control.
/// </summary>
public class Button : ButtonBase
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the IsDefault dependency property.
    /// </summary>
    public static readonly DependencyProperty IsDefaultProperty =
        DependencyProperty.Register(nameof(IsDefault), typeof(bool), typeof(Button),
            new PropertyMetadata(false));

    /// <summary>
    /// Identifies the IsCancel dependency property.
    /// </summary>
    public static readonly DependencyProperty IsCancelProperty =
        DependencyProperty.Register(nameof(IsCancel), typeof(bool), typeof(Button),
            new PropertyMetadata(false));

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets a value indicating whether this is the default button.
    /// </summary>
    public bool IsDefault
    {
        get => (bool)(GetValue(IsDefaultProperty) ?? false);
        set => SetValue(IsDefaultProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this is the cancel button.
    /// </summary>
    public bool IsCancel
    {
        get => (bool)(GetValue(IsCancelProperty) ?? false);
        set => SetValue(IsCancelProperty, value);
    }

    #endregion

    #region Layout

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        // Measure content
        var padding = Padding;
        var border = BorderThickness;
        var contentAvailable = new Size(
            Math.Max(0, availableSize.Width - padding.TotalWidth - border.TotalWidth),
            Math.Max(0, availableSize.Height - padding.TotalHeight - border.TotalHeight));

        // Get content size
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
        if (drawingContext is not DrawingContext dc)
            return;

        var rect = new Rect(RenderSize);
        var cornerRadius = CornerRadius;
        var hasCornerRadius = cornerRadius.TopLeft > 0 || cornerRadius.TopRight > 0 ||
                              cornerRadius.BottomLeft > 0 || cornerRadius.BottomRight > 0;

        // Use property values directly - styles and triggers handle state changes
        var bgBrush = Background;
        var borderBrush = BorderBrush;
        var fgBrush = Foreground;

        // Draw background
        if (bgBrush != null)
        {
            if (hasCornerRadius)
            {
                dc.DrawRoundedRectangle(bgBrush, null, rect, cornerRadius.TopLeft, cornerRadius.TopLeft);
            }
            else
            {
                dc.DrawRectangle(bgBrush, null, rect);
            }
        }

        // Draw border
        if (borderBrush != null && BorderThickness.TotalWidth > 0)
        {
            var pen = new Pen(borderBrush, BorderThickness.Left);
            if (hasCornerRadius)
            {
                dc.DrawRoundedRectangle(null, pen, rect, cornerRadius.TopLeft, cornerRadius.TopLeft);
            }
            else
            {
                dc.DrawRectangle(null, pen, rect);
            }
        }

        // Draw content
        if (Content is string text && fgBrush != null)
        {
            var formattedText = new FormattedText(text, FontFamily, FontSize)
            {
                Foreground = fgBrush
            };

            // Use proper text measurement for centering
            TextMeasurement.MeasureText(formattedText);
            var textX = (rect.Width - formattedText.Width) / 2;
            var textY = (rect.Height - formattedText.Height) / 2;

            dc.DrawText(formattedText, new Point(textX, textY));
        }
    }

    #endregion
}
