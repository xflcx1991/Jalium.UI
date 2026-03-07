using System.Reflection;
using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Controls.Themes;

namespace Jalium.UI.Tests;

[Collection("Application")]
public class AutoCompleteBoxThemeTests
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
    public void AutoCompleteBox_ImplicitThemeStyle_ShouldApplyWithoutLocalVisualOverrides()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var autoCompleteBox = new AutoCompleteBox();
            var host = new StackPanel { Width = 320, Height = 80 };
            host.Children.Add(autoCompleteBox);

            host.Measure(new Size(320, 80));
            host.Arrange(new Rect(0, 0, 320, 80));

            Assert.True(app.Resources.TryGetValue(typeof(AutoCompleteBox), out var styleObj));
            Assert.IsType<Style>(styleObj);

            Assert.False(autoCompleteBox.HasLocalValue(Control.BackgroundProperty));
            Assert.False(autoCompleteBox.HasLocalValue(Control.ForegroundProperty));
            Assert.False(autoCompleteBox.HasLocalValue(Control.BorderBrushProperty));
            Assert.False(autoCompleteBox.HasLocalValue(FrameworkElement.HeightProperty));

            Assert.NotNull(autoCompleteBox.Background);
            Assert.NotNull(autoCompleteBox.Foreground);
            Assert.NotNull(autoCompleteBox.BorderBrush);
            Assert.Equal(32, autoCompleteBox.MinHeight);
            Assert.True(autoCompleteBox.RenderSize.Height >= 32);
        }
        finally
        {
            ResetApplicationState();
        }
    }

    [Fact]
    public void AutoCompleteBox_InternalResolvers_ShouldUseThemeResources()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            Assert.True(app.Resources.TryGetValue("TextPlaceholder", out var placeholderObj));
            Assert.True(app.Resources.TryGetValue("AccentBrush", out var accentObj));
            Assert.True(app.Resources.TryGetValue("ControlBorderFocused", out var focusedObj));

            var autoCompleteBox = new AutoCompleteBox();

            var placeholderMethod = typeof(AutoCompleteBox).GetMethod("ResolvePlaceholderBrush",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var selectedMethod = typeof(AutoCompleteBox).GetMethod("ResolveSelectedSuggestionBackground",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var focusedMethod = typeof(AutoCompleteBox).GetMethod("ResolveFocusedBorderBrush",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(placeholderMethod);
            Assert.NotNull(selectedMethod);
            Assert.NotNull(focusedMethod);

            Assert.Same(placeholderObj, placeholderMethod!.Invoke(autoCompleteBox, null));
            Assert.Same(accentObj, selectedMethod!.Invoke(autoCompleteBox, null));
            Assert.Same(focusedObj, focusedMethod!.Invoke(autoCompleteBox, null));
        }
        finally
        {
            ResetApplicationState();
        }
    }
}
