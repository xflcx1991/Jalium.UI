using System.Reflection;
using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Controls.Themes;
using Jalium.UI.Media;

namespace Jalium.UI.Tests;

[Collection("Application")]
public class CalendarThemeTests
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
    public void Calendar_ImplicitThemeStyle_ShouldApplyWithoutLocalVisualOverrides()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var calendar = new Calendar();
            var host = new StackPanel { Width = 320, Height = 320 };
            host.Children.Add(calendar);

            host.Measure(new Size(320, 320));
            host.Arrange(new Rect(0, 0, 320, 320));

            Assert.True(app.Resources.TryGetValue(typeof(Calendar), out var styleObj));
            Assert.IsType<Style>(styleObj);

            Assert.False(calendar.HasLocalValue(Control.BackgroundProperty));
            Assert.False(calendar.HasLocalValue(Control.BorderBrushProperty));
            Assert.NotNull(calendar.Background);
            Assert.NotNull(calendar.BorderBrush);
            Assert.Equal(8, calendar.Padding.Left);
        }
        finally
        {
            ResetApplicationState();
        }
    }

    [Fact]
    public void Calendar_InternalBrushResolution_ShouldUseThemeAccentBrush()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            Assert.True(app.Resources.TryGetValue("AccentBrush", out var accentObj));
            var calendar = new Calendar();

            var resolveMethod = typeof(Calendar).GetMethod("ResolveCalendarBrush",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(resolveMethod);

            var brush = resolveMethod!.Invoke(calendar, new object[] { "AccentBrush", new SolidColorBrush(Color.Black) });
            Assert.Same(accentObj, brush);
        }
        finally
        {
            ResetApplicationState();
        }
    }

    [Fact]
    public void Calendar_TextResolvers_ShouldUseThemeResources()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var calendar = new Calendar();

            var primaryMethod = typeof(Calendar).GetMethod("ResolvePrimaryTextBrush",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var selectedMethod = typeof(Calendar).GetMethod("ResolveSelectedTextBrush",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(primaryMethod);
            Assert.NotNull(selectedMethod);

            Assert.Same(app.Resources["TextPrimary"], primaryMethod!.Invoke(calendar, null));
            Assert.Same(app.Resources["TextOnAccent"], selectedMethod!.Invoke(calendar, null));
        }
        finally
        {
            ResetApplicationState();
        }
    }
}
