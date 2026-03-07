using System.Reflection;
using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Controls.Themes;
using Jalium.UI.Media;

namespace Jalium.UI.Tests;

[Collection("Application")]
public class DataGridThemeTests
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
    public void DataGrid_ThemeStyle_ShouldApplyTemplateResources()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var controlBackgroundPressed = Assert.IsAssignableFrom<Brush>(app.Resources["ControlBackgroundPressed"]);
            var controlBackground = Assert.IsAssignableFrom<Brush>(app.Resources["ControlBackground"]);

            var dataGrid = new DataGrid();
            var host = new StackPanel { Width = 400, Height = 200 };
            host.Children.Add(dataGrid);

            host.Measure(new Size(400, 200));
            host.Arrange(new Rect(0, 0, 400, 200));

            Assert.True(app.Resources.TryGetValue(typeof(DataGrid), out var styleObj));
            Assert.IsType<Style>(styleObj);

            AssertBrushMatches(controlBackgroundPressed, dataGrid.AlternatingRowBackground);

            var headersBorder = Assert.IsType<Border>(dataGrid.FindName("PART_ColumnHeadersBorder"));
            AssertBrushMatches(controlBackground, headersBorder.Background);
        }
        finally
        {
            ResetApplicationState();
        }
    }

    [Fact]
    public void DataGridRow_And_Header_ShouldUseThemeSelectionAndHeaderBrushes()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var accentBrush = Assert.IsAssignableFrom<Brush>(app.Resources["AccentBrush"]);
            var accentText = Assert.IsAssignableFrom<Brush>(app.Resources["TextOnAccent"]);
            var controlBackground = Assert.IsAssignableFrom<Brush>(app.Resources["ControlBackground"]);

            var row = new DataGridRow { IsSelected = true };
            var header = new DataGridColumnHeader { Content = "Name" };
            var host = new StackPanel { Width = 400, Height = 120 };
            host.Children.Add(row);
            host.Children.Add(header);

            host.Measure(new Size(400, 120));
            host.Arrange(new Rect(0, 0, 400, 120));

            AssertBrushMatches(accentBrush, row.Background);
            AssertBrushMatches(accentText, row.Foreground);
            AssertBrushMatches(controlBackground, header.Background);
        }
        finally
        {
            ResetApplicationState();
        }
    }

    private static void AssertBrushMatches(Brush expected, Brush? actual)
    {
        var actualBrush = Assert.IsAssignableFrom<Brush>(actual);

        if (expected is SolidColorBrush expectedSolid && actualBrush is SolidColorBrush actualSolid)
        {
            Assert.Equal(expectedSolid.Color, actualSolid.Color);
            return;
        }

        Assert.Same(expected, actualBrush);
    }
}
