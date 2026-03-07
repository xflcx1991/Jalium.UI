using System.Reflection;
using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Controls.Themes;
using Jalium.UI.Media;

namespace Jalium.UI.Tests;

[Collection("Application")]
public class PrimitiveThemeTests
{
    private static void ResetApplicationState()
    {
        var currentField = typeof(Application).GetField("_current",
            BindingFlags.NonPublic | BindingFlags.Static);
        currentField?.SetValue(null, null);

        var resetMethod = typeof(ThemeManager).GetMethod("Reset",
            BindingFlags.NonPublic | BindingFlags.Static);
        resetMethod?.Invoke(null, null);
    }

    [Fact]
    public void HyperlinkButton_ImplicitThemeStyle_ShouldApplyWithoutLocalVisualOverrides()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var hyperlinkButton = new HyperlinkButton();
            var host = new StackPanel { Width = 320, Height = 80 };
            host.Children.Add(hyperlinkButton);

            host.Measure(new Size(320, 80));
            host.Arrange(new Rect(0, 0, 320, 80));

            Assert.True(app.Resources.TryGetValue(typeof(HyperlinkButton), out var styleObj));
            Assert.IsType<Style>(styleObj);

            Assert.False(hyperlinkButton.HasLocalValue(Control.ForegroundProperty));
            Assert.False(hyperlinkButton.HasLocalValue(Control.BorderThicknessProperty));
            Assert.False(hyperlinkButton.HasLocalValue(Control.PaddingProperty));
            Assert.NotNull(hyperlinkButton.Foreground);
        }
        finally
        {
            ResetApplicationState();
        }
    }

    [Fact]
    public void HyperlinkButton_DirectRenderBrushResolution_ShouldUseThemeResource()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            Assert.True(app.Resources.TryGetValue("HyperlinkForegroundHover", out var hoverObj));
            var hyperlinkButton = new HyperlinkButton();

            var resolveMethod = typeof(HyperlinkButton).GetMethod("ResolveHyperlinkBrush",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(resolveMethod);

            var brush = resolveMethod!.Invoke(hyperlinkButton, new object[]
            {
                "HyperlinkForegroundHover",
                new SolidColorBrush(Color.Black)
            });

            Assert.Same(hoverObj, brush);
        }
        finally
        {
            ResetApplicationState();
        }
    }

    [Fact]
    public void Slider_ImplicitThemeStyle_ShouldApplyWithoutLocalHeightOverride()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var slider = new Slider();
            var host = new StackPanel { Width = 320, Height = 80 };
            host.Children.Add(slider);

            host.Measure(new Size(320, 80));
            host.Arrange(new Rect(0, 0, 320, 80));

            Assert.True(app.Resources.TryGetValue(typeof(Slider), out var styleObj));
            Assert.IsType<Style>(styleObj);

            Assert.False(slider.HasLocalValue(FrameworkElement.HeightProperty));
            Assert.Equal(24, slider.Height);
            Assert.True(slider.RenderSize.Height >= 24);
        }
        finally
        {
            ResetApplicationState();
        }
    }

    [Fact]
    public void AppBarSeparator_OnRender_ShouldHonorForegroundBrush()
    {
        var brush = new SolidColorBrush(Color.FromRgb(12, 34, 56));
        var separator = new AppBarSeparator
        {
            Foreground = brush,
            Width = 12,
            Height = 32
        };

        separator.Measure(new Size(12, 32));
        separator.Arrange(new Rect(0, 0, 12, 32));

        var drawingContext = new RecordingDrawingContext();
        separator.Render(drawingContext);

        Assert.Same(brush, drawingContext.LastPen?.Brush);
    }

    [Fact]
    public void AppBarSeparator_ThemeFallback_ShouldUseThemeResource()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var separator = new AppBarSeparator
            {
                Width = 12,
                Height = 32
            };

            var resolveMethod = typeof(AppBarSeparator).GetMethod("ResolveSeparatorBrush",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(resolveMethod);

            var brush = resolveMethod!.Invoke(separator, null);
            Assert.Same(app.Resources["AppBarSeparatorForeground"], brush);
        }
        finally
        {
            ResetApplicationState();
        }
    }

    [Fact]
    public void Separator_ImplicitThemeStyle_ShouldApplyWithoutLocalMarginOverride()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var separator = new Separator();
            var host = new StackPanel { Width = 320, Height = 80 };
            host.Children.Add(separator);

            host.Measure(new Size(320, 80));
            host.Arrange(new Rect(0, 0, 320, 80));

            Assert.False(separator.HasLocalValue(FrameworkElement.MarginProperty));
        }
        finally
        {
            ResetApplicationState();
        }
    }

    private sealed class RecordingDrawingContext : DrawingContext
    {
        public Pen? LastPen { get; private set; }

        public override void DrawLine(Pen pen, Point point0, Point point1)
        {
            LastPen = pen;
        }

        public override void DrawRectangle(Brush? brush, Pen? pen, Rect rectangle)
        {
        }

        public override void DrawRoundedRectangle(Brush? brush, Pen? pen, Rect rectangle, double radiusX, double radiusY)
        {
        }

        public override void DrawEllipse(Brush? brush, Pen? pen, Point center, double radiusX, double radiusY)
        {
        }

        public override void DrawText(FormattedText formattedText, Point origin)
        {
        }

        public override void DrawGeometry(Brush? brush, Pen? pen, Geometry geometry)
        {
        }

        public override void DrawImage(ImageSource imageSource, Rect rectangle)
        {
        }

        public override void DrawBackdropEffect(Rect rectangle, IBackdropEffect effect, CornerRadius cornerRadius)
        {
        }

        public override void PushTransform(Transform transform)
        {
        }

        public override void PushClip(Geometry clipGeometry)
        {
        }

        public override void PushOpacity(double opacity)
        {
        }

        public override void Pop()
        {
        }

        public override void Close()
        {
        }
    }
}
