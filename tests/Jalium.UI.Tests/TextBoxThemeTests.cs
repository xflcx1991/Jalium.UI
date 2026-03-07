using System.Reflection;
using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Controls.Themes;

namespace Jalium.UI.Tests;

[Collection("Application")]
public class TextBoxThemeTests
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
    public void TextBox_ImplicitThemeStyle_ShouldApplyWithoutLocalVisualOverrides()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var textBox = new TextBox();
            var host = new StackPanel { Width = 320, Height = 80 };
            host.Children.Add(textBox);

            host.Measure(new Size(320, 80));
            host.Arrange(new Rect(0, 0, 320, 80));

            Assert.True(app.Resources.TryGetValue(typeof(TextBox), out var styleObj));
            Assert.IsType<Style>(styleObj);

            Assert.False(textBox.HasLocalValue(Control.BackgroundProperty));
            Assert.False(textBox.HasLocalValue(Control.ForegroundProperty));
            Assert.False(textBox.HasLocalValue(Control.BorderBrushProperty));

            Assert.NotNull(textBox.Background);
            Assert.NotNull(textBox.Foreground);
            Assert.NotNull(textBox.BorderBrush);
            Assert.Equal(32, textBox.MinHeight);
            Assert.True(textBox.RenderSize.Height >= 32);
        }
        finally
        {
            ResetApplicationState();
        }
    }

    [Fact]
    public void TextBox_InternalResolvers_ShouldUseThemeResources()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            Assert.True(app.Resources.TryGetValue("TextPlaceholder", out var placeholderObj));
            Assert.True(app.Resources.TryGetValue("ControlBorderFocused", out var focusedObj));

            var textBox = new TextBox();

            var placeholderMethod = typeof(TextBox).GetMethod("ResolvePlaceholderBrush",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var focusedMethod = typeof(TextBox).GetMethod("ResolveFocusedBorderBrush",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(placeholderMethod);
            Assert.NotNull(focusedMethod);

            Assert.Same(placeholderObj, placeholderMethod!.Invoke(textBox, null));
            Assert.Same(focusedObj, focusedMethod!.Invoke(textBox, null));
        }
        finally
        {
            ResetApplicationState();
        }
    }
}
