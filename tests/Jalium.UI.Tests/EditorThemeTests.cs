using System.Reflection;
using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Controls.Themes;

namespace Jalium.UI.Tests;

[Collection("Application")]
public class EditorThemeTests
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
    public void RichTextBox_ImplicitThemeStyle_ShouldApplyWithoutLocalVisualOverrides()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var richTextBox = new RichTextBox();
            var host = new StackPanel { Width = 360, Height = 120 };
            host.Children.Add(richTextBox);

            host.Measure(new Size(360, 120));
            host.Arrange(new Rect(0, 0, 360, 120));

            Assert.True(app.Resources.TryGetValue(typeof(RichTextBox), out var styleObj));
            Assert.IsType<Style>(styleObj);

            Assert.False(richTextBox.HasLocalValue(Control.BackgroundProperty));
            Assert.False(richTextBox.HasLocalValue(Control.BorderBrushProperty));
            Assert.False(richTextBox.HasLocalValue(Control.PaddingProperty));
            Assert.NotNull(richTextBox.Background);
            Assert.NotNull(richTextBox.BorderBrush);
            Assert.Equal(4, richTextBox.Padding.Left);
        }
        finally
        {
            ResetApplicationState();
        }
    }

    [Fact]
    public void EditControl_ImplicitThemeStyle_ShouldApplyWithoutLocalVisualOverrides()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var editor = new EditControl();
            var host = new StackPanel { Width = 360, Height = 200 };
            host.Children.Add(editor);

            host.Measure(new Size(360, 200));
            host.Arrange(new Rect(0, 0, 360, 200));

            Assert.True(app.Resources.TryGetValue(typeof(EditControl), out var styleObj));
            Assert.IsType<Style>(styleObj);

            Assert.False(editor.HasLocalValue(Control.BackgroundProperty));
            Assert.False(editor.HasLocalValue(Control.ForegroundProperty));
            Assert.False(editor.HasLocalValue(Control.FontFamilyProperty));
            Assert.False(editor.HasLocalValue(Control.FontSizeProperty));
            Assert.NotNull(editor.Background);
            Assert.NotNull(editor.Foreground);
            Assert.Equal("Cascadia Code", editor.FontFamily);
            Assert.Equal(14, editor.FontSize);
        }
        finally
        {
            ResetApplicationState();
        }
    }
}
