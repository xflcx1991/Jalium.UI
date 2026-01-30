namespace Jalium.UI.Controls;

/// <summary>
/// Arranges child elements into a single line that can be oriented horizontally or vertically.
/// </summary>
public class StackPanel : Panel
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the Orientation dependency property.
    /// </summary>
    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(StackPanel),
            new PropertyMetadata(Orientation.Vertical, OnLayoutPropertyChanged));

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the dimension by which child elements are stacked.
    /// </summary>
    public Orientation Orientation
    {
        get => (Orientation)(GetValue(OrientationProperty) ?? Orientation.Vertical);
        set => SetValue(OrientationProperty, value);
    }

    #endregion

    #region Layout

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var isVertical = Orientation == Orientation.Vertical;
        double totalWidth = 0;
        double totalHeight = 0;
        double maxCross = 0;

        foreach (var child in Children)
        {
            // Skip collapsed children
            if (child.Visibility == Visibility.Collapsed)
                continue;

            // Give each child infinite space in the stack direction
            var childAvailable = isVertical
                ? new Size(availableSize.Width, double.PositiveInfinity)
                : new Size(double.PositiveInfinity, availableSize.Height);

            child.Measure(childAvailable);
            var childSize = child.DesiredSize;

            if (isVertical)
            {
                totalHeight += childSize.Height;
                maxCross = Math.Max(maxCross, childSize.Width);
            }
            else
            {
                totalWidth += childSize.Width;
                maxCross = Math.Max(maxCross, childSize.Height);
            }
        }

        if (isVertical)
        {
            return new Size(maxCross, totalHeight);
        }
        else
        {
            return new Size(totalWidth, maxCross);
        }
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var isVertical = Orientation == Orientation.Vertical;
        double offset = 0;

        foreach (var child in Children)
        {
            // Skip collapsed children
            if (child.Visibility == Visibility.Collapsed)
                continue;

            var childSize = child.DesiredSize;

            Rect childRect;
            if (isVertical)
            {
                childRect = new Rect(0, offset, finalSize.Width, childSize.Height);
                offset += childSize.Height;
            }
            else
            {
                childRect = new Rect(offset, 0, childSize.Width, finalSize.Height);
                offset += childSize.Width;
            }

            child.Arrange(childRect);
            // Note: Do NOT call SetVisualBounds here - ArrangeCore already calculates
            // the correct visual bounds including margin offsets
        }

        return finalSize;
    }

    #endregion

    #region Property Changed Callbacks

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StackPanel panel)
        {
            panel.InvalidateMeasure();
        }
    }

    #endregion
}

/// <summary>
/// Defines the different orientations that a control or layout can have.
/// </summary>
public enum Orientation
{
    /// <summary>
    /// Horizontal orientation.
    /// </summary>
    Horizontal,

    /// <summary>
    /// Vertical orientation.
    /// </summary>
    Vertical
}
