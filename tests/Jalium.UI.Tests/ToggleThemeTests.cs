using System.Reflection;
using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Controls.Shapes;
using Jalium.UI.Controls.Themes;
using Jalium.UI.Media;
using ShapePath = Jalium.UI.Controls.Shapes.Path;

namespace Jalium.UI.Tests;

[Collection("Application")]
public class ToggleThemeTests
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
    public void CheckGlyphs_ShouldUseTextOnAccentThemeResource()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var accentText = Assert.IsAssignableFrom<Brush>(app.Resources["TextOnAccent"]);

            var checkBox = new CheckBox { IsChecked = true };
            var radioButton = new RadioButton { IsChecked = true };
            var host = new StackPanel { Width = 320, Height = 120 };
            host.Children.Add(checkBox);
            host.Children.Add(radioButton);

            host.Measure(new Size(320, 120));
            host.Arrange(new Rect(0, 0, 320, 120));

            var checkMark = Assert.IsType<ShapePath>(checkBox.FindName("CheckMark"));
            var radioDot = Assert.IsType<Ellipse>(radioButton.FindName("RadioDot"));

            Assert.Same(accentText, checkMark.Stroke);
            Assert.Same(accentText, radioDot.Fill);
        }
        finally
        {
            ResetApplicationState();
        }
    }

    [Fact]
    public void ToggleSwitch_ShouldResolveBorderAndDisabledBrushesFromTheme()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var uncheckedBorder = Assert.IsType<SolidColorBrush>(app.Resources["ToggleUncheckedBorder"]);
            var checkedBorder = Assert.IsType<SolidColorBrush>(app.Resources["ToggleCheckedBorder"]);
            var disabledBackground = Assert.IsAssignableFrom<Brush>(app.Resources["ToggleDisabledBackground"]);
            var disabledBorder = Assert.IsAssignableFrom<Brush>(app.Resources["ToggleDisabledBorder"]);

            var toggleSwitch = new ToggleSwitch();
            var host = new StackPanel { Width = 320, Height = 80 };
            host.Children.Add(toggleSwitch);

            host.Measure(new Size(320, 80));
            host.Arrange(new Rect(0, 0, 320, 80));

            var getOffBorderColorMethod = typeof(ToggleSwitch).GetMethod("GetOffBorderColor",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var getOnBorderColorMethod = typeof(ToggleSwitch).GetMethod("GetOnBorderColor",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var disabledBackgroundMethod = typeof(ToggleSwitch).GetMethod("ResolveDisabledTrackBackground",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var disabledBorderMethod = typeof(ToggleSwitch).GetMethod("ResolveDisabledTrackBorderBrush",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(getOffBorderColorMethod);
            Assert.NotNull(getOnBorderColorMethod);
            Assert.NotNull(disabledBackgroundMethod);
            Assert.NotNull(disabledBorderMethod);

            Assert.Equal(uncheckedBorder.Color, (Color)getOffBorderColorMethod!.Invoke(toggleSwitch, null)!);
            Assert.Equal(checkedBorder.Color, (Color)getOnBorderColorMethod!.Invoke(toggleSwitch, null)!);
            Assert.Same(disabledBackground, disabledBackgroundMethod!.Invoke(toggleSwitch, null));
            Assert.Same(disabledBorder, disabledBorderMethod!.Invoke(toggleSwitch, null));

            var track = Assert.IsType<Border>(toggleSwitch.FindName("PART_SwitchTrack"));
            toggleSwitch.IsEnabled = false;

            Assert.Same(disabledBackground, track.Background);
            Assert.Same(disabledBorder, track.BorderBrush);
        }
        finally
        {
            ResetApplicationState();
        }
    }
}
