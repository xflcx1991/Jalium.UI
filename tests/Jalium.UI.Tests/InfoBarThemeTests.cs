using System.Reflection;
using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Controls.Themes;

namespace Jalium.UI.Tests;

[Collection("Application")]
public class InfoBarThemeTests
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
    public void InfoBar_ImplicitThemeStyle_ShouldApplyWithoutLocalLayoutOverrides()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var infoBar = new InfoBar
            {
                Title = "Heads up",
                Message = "Theme-driven layout"
            };
            var host = new StackPanel { Width = 360, Height = 120 };
            host.Children.Add(infoBar);

            host.Measure(new Size(360, 120));
            host.Arrange(new Rect(0, 0, 360, 120));

            Assert.True(app.Resources.TryGetValue(typeof(InfoBar), out var styleObj));
            Assert.IsType<Style>(styleObj);

            Assert.False(infoBar.HasLocalValue(Control.PaddingProperty));
            Assert.False(infoBar.HasLocalValue(Control.CornerRadiusProperty));
            Assert.Equal(12, infoBar.Padding.Left);
            Assert.Equal(8, infoBar.Padding.Top);
            Assert.Equal(4, infoBar.CornerRadius.TopLeft);
            Assert.True(infoBar.RenderSize.Height >= 48);
        }
        finally
        {
            ResetApplicationState();
        }
    }

    [Fact]
    public void InfoBar_SeverityBrushes_ShouldResolveFromThemeResources()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var infoBar = new InfoBar
            {
                Severity = InfoBarSeverity.Warning
            };

            Assert.True(app.Resources.TryGetValue("InfoBarWarningBackground", out var bgObj));
            Assert.True(app.Resources.TryGetValue("InfoBarWarningBrush", out var iconObj));

            var getSeverityBrushes = typeof(InfoBar).GetMethod("GetSeverityBrushes",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(getSeverityBrushes);

            var tuple = getSeverityBrushes!.Invoke(infoBar, null);
            Assert.NotNull(tuple);

            var backgroundField = tuple!.GetType().GetField("Item1");
            var iconField = tuple.GetType().GetField("Item2");
            Assert.NotNull(backgroundField);
            Assert.NotNull(iconField);

            Assert.Same(bgObj, backgroundField!.GetValue(tuple));
            Assert.Same(iconObj, iconField!.GetValue(tuple));
        }
        finally
        {
            ResetApplicationState();
        }
    }
}
