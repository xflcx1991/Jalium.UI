using System.Reflection;
using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Controls.Themes;
using Jalium.UI.Markup;

namespace Jalium.UI.Tests;

[Collection("Application")]
public class ToolBarThemeTests
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
    public void ToolBar_And_ToolBarTray_Styles_ShouldBeRegisteredAndLoadable()
    {
        ResetApplicationState();
        ThemeLoader.Initialize();
        var app = new Application();

        try
        {
            Assert.True(app.Resources.TryGetValue(typeof(ToolBar), out var toolBarStyleObj));
            Assert.True(app.Resources.TryGetValue(typeof(ToolBarTray), out var toolBarTrayStyleObj));
            Assert.IsType<Style>(toolBarStyleObj);
            Assert.IsType<Style>(toolBarTrayStyleObj);
        }
        finally
        {
            ResetApplicationState();
        }
    }
}
