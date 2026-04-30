using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Media;

namespace Jalium.UI.Tests;

public class BorderClipTests
{
    [Fact]
    public void Border_ClipToBounds_WithRoundedCorners_UsesFullRenderBounds()
    {
        var border = new TestBorder
        {
            ClipToBounds = true,
            BorderThickness = new Thickness(3),
            CornerRadius = new CornerRadius(8)
        };

        border.Measure(new Size(120, 40));
        border.Arrange(new Rect(0, 0, 120, 40));

        var clip = Assert.IsType<RectangleGeometry>(border.InvokeGetLayoutClip());

        Assert.Equal(new Rect(0, 0, 120, 40), clip.Rect);
        Assert.Equal(8, clip.RadiusX);
        Assert.Equal(8, clip.RadiusY);
    }

    [Fact]
    public void Border_Child_VisualBounds_OffsetByBorderThickness()
    {
        var child = new FrameworkElement();
        var border = new Border
        {
            BorderThickness = new Thickness(4),
            Child = child,
        };

        border.Measure(new Size(100, 100));
        border.Arrange(new Rect(0, 0, 100, 100));

        Assert.Equal(new Rect(4, 4, 92, 92), child.VisualBounds);
    }

    [Fact]
    public void Border_Child_VisualBounds_OffsetByPaddingAndBorderThickness()
    {
        var child = new FrameworkElement();
        var border = new Border
        {
            BorderThickness = new Thickness(2),
            Padding = new Thickness(6),
            Child = child,
        };

        border.Measure(new Size(100, 100));
        border.Arrange(new Rect(0, 0, 100, 100));

        Assert.Equal(new Rect(8, 8, 84, 84), child.VisualBounds);
    }

    [Fact]
    public void Border_Child_VisualBounds_RespectsAsymmetricBorderThickness()
    {
        var child = new FrameworkElement();
        var border = new Border
        {
            BorderThickness = new Thickness(left: 8, top: 0, right: 0, bottom: 4),
            Child = child,
        };

        border.Measure(new Size(100, 100));
        border.Arrange(new Rect(0, 0, 100, 100));

        Assert.Equal(new Rect(8, 0, 92, 96), child.VisualBounds);
    }

    [Fact]
    public void Border_Child_VisualBounds_StaysConsistentWithFractionalBorderThickness()
    {
        // Mid-transition BorderThickness values like 1.5 used to produce a
        // 0.5 px disagreement between the child's _visualBounds and the rect
        // OnRender paints the background/stroke into. Both sides snap each
        // border edge the same way now, so the child rect computed from
        // snapped edges must match what OnRender will draw at the snapped
        // BorderThickness.
        var child = new FrameworkElement();
        var border = new Border
        {
            BorderThickness = new Thickness(1.5),
            Child = child,
        };

        border.Measure(new Size(100, 100));
        border.Arrange(new Rect(0, 0, 100, 100));

        // BT=1.5 snaps to 2 on each side via AwayFromZero rounding.
        Assert.Equal(new Rect(2, 2, 96, 96), child.VisualBounds);
    }

    private sealed class TestBorder : Border
    {
        public object? InvokeGetLayoutClip() => GetLayoutClip();
    }
}
