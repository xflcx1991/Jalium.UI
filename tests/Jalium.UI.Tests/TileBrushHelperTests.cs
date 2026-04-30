using Jalium.UI;
using Jalium.UI.Media;

namespace Jalium.UI.Tests;

/// <summary>
/// Layout tests for <see cref="TileBrushHelper"/>. Exercises the
/// <see cref="TileBrush.Stretch"/> / <see cref="TileBrush.Viewport"/> /
/// <see cref="TileBrush.Viewbox"/> / <see cref="TileBrush.AlignmentX"/> /
/// <see cref="TileBrush.AlignmentY"/> / <see cref="TileBrush.TileMode"/> matrix
/// against expected screen-space placements. The renderer is a thin pass-through
/// over these placements, so verifying the math here keeps the rendering layer
/// honest without spinning up a native context.
/// </summary>
public class TileBrushHelperTests
{
    private static ImageBrush DefaultBrush() => new()
    {
        // Default is RelativeToBoundingBox / (0,0,1,1) for both Viewport and Viewbox,
        // Stretch.Fill, AlignmentX.Center, AlignmentY.Center, TileMode.None.
    };

    [Fact]
    public void Default_StretchFill_TileNone_FillsShapeWithSingleTile()
    {
        var brush = DefaultBrush();
        var shape = new Rect(0, 0, 200, 100);

        var placements = TileBrushHelper.ComputeTilePlacements(brush, shape, 50, 25);

        Assert.Single(placements);
        var p = placements[0];
        Assert.Equal(shape, p.ImageDestRect);
        Assert.Equal(shape, p.ClipRect);
        Assert.False(p.FlipX);
        Assert.False(p.FlipY);
    }

    [Fact]
    public void StretchUniform_PreservesAspectAndCenters()
    {
        var brush = new ImageBrush { Stretch = Stretch.Uniform };
        // 2:1 aspect shape; 1:1 image — uniform fits the image height to the
        // shape height (100x100), leaving 50px of empty space on each side.
        var shape = new Rect(0, 0, 200, 100);

        var placements = TileBrushHelper.ComputeTilePlacements(brush, shape, 50, 50);

        Assert.Single(placements);
        var img = placements[0].ImageDestRect;
        Assert.Equal(50, img.X);
        Assert.Equal(0, img.Y);
        Assert.Equal(100, img.Width);
        Assert.Equal(100, img.Height);
    }

    [Fact]
    public void StretchUniform_AlignmentLeftTop_PinsToTopLeft()
    {
        var brush = new ImageBrush
        {
            Stretch = Stretch.Uniform,
            AlignmentX = AlignmentX.Left,
            AlignmentY = AlignmentY.Top
        };
        var shape = new Rect(0, 0, 200, 100);

        var placements = TileBrushHelper.ComputeTilePlacements(brush, shape, 50, 50);

        Assert.Single(placements);
        Assert.Equal(new Rect(0, 0, 100, 100), placements[0].ImageDestRect);
    }

    [Fact]
    public void StretchUniform_AlignmentRightBottom_PinsToBottomRight()
    {
        var brush = new ImageBrush
        {
            Stretch = Stretch.Uniform,
            AlignmentX = AlignmentX.Right,
            AlignmentY = AlignmentY.Bottom
        };
        var shape = new Rect(0, 0, 200, 100);

        var placements = TileBrushHelper.ComputeTilePlacements(brush, shape, 50, 50);

        Assert.Single(placements);
        Assert.Equal(new Rect(100, 0, 100, 100), placements[0].ImageDestRect);
    }

    [Fact]
    public void StretchUniformToFill_OverflowsViewportPreservingAspect()
    {
        var brush = new ImageBrush { Stretch = Stretch.UniformToFill };
        // 2:1 shape, 1:1 image — uniform-to-fill must scale image to width=200,
        // overflowing 200x200 (centered) within the 200x100 viewport.
        var shape = new Rect(0, 0, 200, 100);

        var placements = TileBrushHelper.ComputeTilePlacements(brush, shape, 50, 50);

        Assert.Single(placements);
        var img = placements[0].ImageDestRect;
        Assert.Equal(0, img.X);
        Assert.Equal(-50, img.Y);
        Assert.Equal(200, img.Width);
        Assert.Equal(200, img.Height);
        // ClipRect equals the viewport — anything that overflows gets clipped.
        Assert.Equal(shape, placements[0].ClipRect);
    }

    [Fact]
    public void StretchNone_DrawsImageAtSourceSizeCenteredByDefault()
    {
        var brush = new ImageBrush { Stretch = Stretch.None };
        var shape = new Rect(0, 0, 200, 100);

        var placements = TileBrushHelper.ComputeTilePlacements(brush, shape, 50, 30);

        Assert.Single(placements);
        var img = placements[0].ImageDestRect;
        Assert.Equal(75, img.X);   // (200 - 50) / 2
        Assert.Equal(35, img.Y);   // (100 - 30) / 2
        Assert.Equal(50, img.Width);
        Assert.Equal(30, img.Height);
    }

    [Fact]
    public void TileModeTile_RepeatsViewportAcrossShape()
    {
        var brush = new ImageBrush
        {
            // Half-size viewport: 4 tiles cover the shape (2 × 2 grid).
            Viewport = new Rect(0, 0, 0.5, 0.5),
            ViewportUnits = BrushMappingMode.RelativeToBoundingBox,
            TileMode = TileMode.Tile
        };
        var shape = new Rect(0, 0, 100, 80);

        var placements = TileBrushHelper.ComputeTilePlacements(brush, shape, 50, 40);

        Assert.Equal(4, placements.Count);
        Assert.All(placements, p => Assert.False(p.FlipX || p.FlipY));

        // Two distinct X / Y starts → confirms the grid actually tiled.
        var distinctXs = placements.Select(p => p.ClipRect.X).Distinct().ToList();
        var distinctYs = placements.Select(p => p.ClipRect.Y).Distinct().ToList();
        Assert.Equal(2, distinctXs.Count);
        Assert.Equal(2, distinctYs.Count);
    }

    [Fact]
    public void TileModeFlipX_FlipsAlternateColumns()
    {
        var brush = new ImageBrush
        {
            Viewport = new Rect(0, 0, 0.5, 1.0),
            ViewportUnits = BrushMappingMode.RelativeToBoundingBox,
            TileMode = TileMode.FlipX
        };
        var shape = new Rect(0, 0, 100, 50);

        var placements = TileBrushHelper.ComputeTilePlacements(brush, shape, 50, 50);

        Assert.Equal(2, placements.Count);
        Assert.False(placements[0].FlipX);
        Assert.True(placements[1].FlipX);
        Assert.False(placements[0].FlipY);
        Assert.False(placements[1].FlipY);
    }

    [Fact]
    public void TileModeFlipXY_FlipsAlternateRowsAndColumns()
    {
        var brush = new ImageBrush
        {
            Viewport = new Rect(0, 0, 0.5, 0.5),
            ViewportUnits = BrushMappingMode.RelativeToBoundingBox,
            TileMode = TileMode.FlipXY
        };
        var shape = new Rect(0, 0, 100, 100);

        var placements = TileBrushHelper.ComputeTilePlacements(brush, shape, 50, 50);

        Assert.Equal(4, placements.Count);
        // Order is row-major, so: (0,0), (1,0), (0,1), (1,1).
        Assert.False(placements[0].FlipX); Assert.False(placements[0].FlipY);
        Assert.True(placements[1].FlipX);  Assert.False(placements[1].FlipY);
        Assert.False(placements[2].FlipX); Assert.True(placements[2].FlipY);
        Assert.True(placements[3].FlipX);  Assert.True(placements[3].FlipY);
    }

    [Fact]
    public void AbsoluteViewport_IgnoresShapeBounds()
    {
        var brush = new ImageBrush
        {
            Viewport = new Rect(10, 20, 50, 40),
            ViewportUnits = BrushMappingMode.Absolute,
            TileMode = TileMode.None
        };
        var shape = new Rect(0, 0, 200, 200);

        var placements = TileBrushHelper.ComputeTilePlacements(brush, shape, 100, 100);

        Assert.Single(placements);
        Assert.Equal(new Rect(10, 20, 50, 40), placements[0].ClipRect);
    }

    [Fact]
    public void Viewbox_CropsSourceImage()
    {
        var brush = new ImageBrush
        {
            // Pull the center 50% of the source image into the viewport.
            Viewbox = new Rect(0.25, 0.25, 0.5, 0.5),
            ViewboxUnits = BrushMappingMode.RelativeToBoundingBox,
            Stretch = Stretch.Fill
        };
        var shape = new Rect(0, 0, 100, 100);

        var placements = TileBrushHelper.ComputeTilePlacements(brush, shape, 100, 100);

        Assert.Single(placements);
        var img = placements[0].ImageDestRect;
        // The full image is drawn at 200×200 (since the cropped 50% lands on
        // the 100×100 viewport), shifted so the center crop aligns with shape.
        Assert.Equal(-50, img.X);
        Assert.Equal(-50, img.Y);
        Assert.Equal(200, img.Width);
        Assert.Equal(200, img.Height);
        // The clip ensures only the viewport-sized region is visible.
        Assert.Equal(shape, placements[0].ClipRect);
    }

    [Fact]
    public void DegenerateInputs_ReturnEmpty()
    {
        var brush = DefaultBrush();
        Assert.Empty(TileBrushHelper.ComputeTilePlacements(brush, new Rect(0, 0, 0, 100), 50, 50));
        Assert.Empty(TileBrushHelper.ComputeTilePlacements(brush, new Rect(0, 0, 100, 100), 0, 50));
        Assert.Empty(TileBrushHelper.ComputeTilePlacements(brush, new Rect(0, 0, 100, 100), 50, 0));
    }

    [Fact]
    public void TileMode_CapsAtTileLimitForPathologicalInputs()
    {
        // 1px viewport on a 10000px shape would be 10⁴ tiles per axis → 10⁸
        // total. Helper is expected to clamp to a sane upper bound to keep
        // the renderer from drowning in DrawBitmap calls.
        var brush = new ImageBrush
        {
            Viewport = new Rect(0, 0, 1, 1),
            ViewportUnits = BrushMappingMode.Absolute,
            TileMode = TileMode.Tile
        };
        var shape = new Rect(0, 0, 10000, 10000);

        var placements = TileBrushHelper.ComputeTilePlacements(brush, shape, 1, 1);

        // Capped at 1024 × 1024 = ~1M; well below the pathological 100M.
        Assert.True(placements.Count <= 1024 * 1024,
            $"Expected ≤ 1,048,576 tiles, got {placements.Count}");
    }

    [Fact]
    public void ComputeViewport_RelativeUnits_ScalesWithShape()
    {
        var brush = new ImageBrush
        {
            Viewport = new Rect(0.25, 0.5, 0.5, 0.25),
            ViewportUnits = BrushMappingMode.RelativeToBoundingBox
        };
        var shape = new Rect(100, 200, 400, 200);

        var viewport = TileBrushHelper.ComputeViewport(brush, shape);

        Assert.Equal(new Rect(200, 300, 200, 50), viewport);
    }

    [Fact]
    public void ComputeViewbox_AbsoluteUnits_PassesThrough()
    {
        var brush = new ImageBrush
        {
            Viewbox = new Rect(10, 20, 30, 40),
            ViewboxUnits = BrushMappingMode.Absolute
        };

        var viewbox = TileBrushHelper.ComputeViewbox(brush, 100, 200);

        Assert.Equal(new Rect(10, 20, 30, 40), viewbox);
    }

    [Fact]
    public void ComputeContentRect_UniformTooSmallViewport_ProducesZeroSize()
    {
        var brush = new ImageBrush { Stretch = Stretch.Uniform };
        var viewport = new Rect(0, 0, 0, 0);
        var viewbox = new Rect(0, 0, 100, 100);

        var rect = TileBrushHelper.ComputeContentRect(brush, viewport, viewbox);

        Assert.Equal(0, rect.Width);
        Assert.Equal(0, rect.Height);
    }
}
