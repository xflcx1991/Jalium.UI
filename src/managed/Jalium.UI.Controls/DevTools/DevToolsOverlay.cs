using Jalium.UI.Interop;
using Jalium.UI.Media;

namespace Jalium.UI.Controls.DevTools;

#if DEBUG
/// <summary>
/// Provides visual overlay functionality for highlighting elements in the DevTools.
/// </summary>
internal class DevToolsOverlay
{
    private readonly Window _targetWindow;
    private FrameworkElement? _highlightedElement;
    private readonly SolidColorBrush _highlightBorderBrush;
    private readonly SolidColorBrush _highlightFillBrush;
    private readonly SolidColorBrush _marginBrush;
    private readonly SolidColorBrush _paddingBrush;

    /// <summary>
    /// Gets the currently highlighted element.
    /// </summary>
    public FrameworkElement? HighlightedElement => _highlightedElement;

    public DevToolsOverlay(Window targetWindow)
    {
        _targetWindow = targetWindow ?? throw new ArgumentNullException(nameof(targetWindow));

        // Create brushes for highlighting
        _highlightBorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215));
        _highlightFillBrush = new SolidColorBrush(Color.FromArgb(50, 0, 120, 215));
        _marginBrush = new SolidColorBrush(Color.FromArgb(100, 255, 165, 0)); // Orange for margin
        _paddingBrush = new SolidColorBrush(Color.FromArgb(100, 144, 238, 144)); // Green for padding

        // Hook into window's paint event
        // Note: This is a simplified approach. In a real implementation,
        // you might want to create a separate overlay window.
    }

    /// <summary>
    /// Highlights the specified element in the target window.
    /// </summary>
    /// <param name="element">The element to highlight, or null to clear highlighting.</param>
    public void HighlightElement(UIElement? element)
    {
        _highlightedElement = element as FrameworkElement;

        // Request redraw of target window to show the highlight
        _targetWindow.InvalidateWindow();
    }

    /// <summary>
    /// Gets the bounds of the highlighted element in window coordinates.
    /// </summary>
    /// <returns>The bounds, or null if no element is highlighted.</returns>
    public Rect? GetHighlightBounds()
    {
        if (_highlightedElement == null)
        {
            return null;
        }

        // Calculate the element's bounds relative to the window
        return GetElementBoundsInWindow(_highlightedElement);
    }

    /// <summary>
    /// Calculates the element's bounds relative to the window.
    /// </summary>
    private Rect? GetElementBoundsInWindow(FrameworkElement element)
    {
        double x = 0;
        double y = 0;

        Visual? current = element;
        while (current != null && current != _targetWindow)
        {
            if (current is UIElement uiElement)
            {
                var bounds = uiElement.VisualBounds;
                x += bounds.X;
                y += bounds.Y;
            }

            current = current.VisualParent;
        }

        if (current == _targetWindow)
        {
            return new Rect(x, y, element.ActualWidth, element.ActualHeight);
        }

        return null;
    }

    /// <summary>
    /// Draws the highlight overlay onto a drawing context.
    /// Call this from the target window's render method.
    /// </summary>
    /// <param name="dc">The drawing context.</param>
    public void DrawOverlay(DrawingContext dc)
    {
        if (_highlightedElement == null)
        {
            return;
        }

        var bounds = GetHighlightBounds();
        if (!bounds.HasValue)
        {
            return;
        }

        var rect = bounds.Value;

        // Draw margin area (if the element is a FrameworkElement with margin)
        if (_highlightedElement is FrameworkElement fe && HasNonZeroMargin(fe.Margin))
        {
            var marginRect = new Rect(
                rect.X - fe.Margin.Left,
                rect.Y - fe.Margin.Top,
                rect.Width + fe.Margin.Left + fe.Margin.Right,
                rect.Height + fe.Margin.Top + fe.Margin.Bottom);

            dc.DrawRectangle(_marginBrush, null, marginRect);
        }

        // Draw the main element bounds
        dc.DrawRectangle(_highlightFillBrush, null, rect);

        // Draw padding area (if the element is a Control with padding)
        if (_highlightedElement is Control control && HasNonZeroThickness(control.Padding))
        {
            var paddingRect = new Rect(
                rect.X + control.Padding.Left,
                rect.Y + control.Padding.Top,
                rect.Width - control.Padding.Left - control.Padding.Right,
                rect.Height - control.Padding.Top - control.Padding.Bottom);

            if (paddingRect is { Width: > 0, Height: > 0 })
            {
                dc.DrawRectangle(_paddingBrush, null, paddingRect);
            }
        }

        // Draw border
        var pen = new Pen(_highlightBorderBrush, 2);
        dc.DrawRectangle(null, pen, rect);

        // Draw size label
        DrawSizeLabel(dc, rect);
    }

    private void DrawSizeLabel(DrawingContext dc, Rect elementBounds)
    {
        var labelText = $"{elementBounds.Width:F0} × {elementBounds.Height:F0}";
        var formattedText = new FormattedText(labelText, "Segoe UI", 11)
        {
            Foreground = new SolidColorBrush(Color.White)
        };

        // Measure the text to get accurate Width/Height
        TextMeasurement.MeasureText(formattedText);

        // Position label below or above the element
        double labelX = elementBounds.X;
        double labelY = elementBounds.Bottom + 2;

        // If label would go off screen, place it above
        if (labelY + formattedText.Height > _targetWindow.Height)
        {
            labelY = elementBounds.Y - formattedText.Height - 2;
        }

        // Draw background for label
        var labelBounds = new Rect(
            labelX,
            labelY,
            formattedText.Width + 8,
            formattedText.Height + 4);

        dc.DrawRoundedRectangle(
            new SolidColorBrush(Color.FromArgb(220, 0, 0, 0)),
            null,
            labelBounds,
            3, 3);

        dc.DrawText(formattedText, new Point(labelX + 4, labelY + 2));
    }

    private static bool HasNonZeroMargin(Thickness thickness)
    {
        return thickness.Left != 0 || thickness.Top != 0 ||
               thickness.Right != 0 || thickness.Bottom != 0;
    }

    private static bool HasNonZeroThickness(Thickness thickness)
    {
        return thickness.Left > 0 || thickness.Top > 0 ||
               thickness.Right > 0 || thickness.Bottom > 0;
    }

    /// <summary>
    /// Removes the overlay from the target window.
    /// </summary>
    public void RemoveOverlay()
    {
        _highlightedElement = null;
        _targetWindow.InvalidateWindow();
    }
}
#endif
