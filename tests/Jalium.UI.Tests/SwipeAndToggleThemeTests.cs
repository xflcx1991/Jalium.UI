using System.Reflection;
using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Controls.Themes;
using Jalium.UI.Media;

namespace Jalium.UI.Tests;

[Collection("Application")]
public class SwipeAndToggleThemeTests
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
    public void SwipeControl_InternalResolvers_ShouldUseThemeResources()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var accentBrush = Assert.IsAssignableFrom<Brush>(app.Resources["AccentBrush"]);
            var textOnAccent = Assert.IsAssignableFrom<Brush>(app.Resources["TextOnAccent"]);
            var swipeControl = new SwipeControl();
            var item = new SwipeItem { Text = "Delete" };

            Assert.Same(accentBrush, InvokePrivateBrushResolver(swipeControl, "ResolveSwipeItemBackground", item));
            Assert.Same(textOnAccent, InvokePrivateBrushResolver(swipeControl, "ResolveSwipeItemForeground", item));
        }
        finally
        {
            ResetApplicationState();
        }
    }

    [Fact]
    public void ToggleMenuFlyoutItem_CheckGlyphResolver_ShouldUseThemeResource()
    {
        ResetApplicationState();
        var app = new Application();

        try
        {
            var textPrimary = Assert.IsAssignableFrom<Brush>(app.Resources["TextPrimary"]);
            var item = new ToggleMenuFlyoutItem();

            Assert.Same(textPrimary, InvokePrivateBrushResolver(item, "ResolveCheckGlyphBrush"));
        }
        finally
        {
            ResetApplicationState();
        }
    }

    private static Brush InvokePrivateBrushResolver(object owner, string methodName, params object[] args)
    {
        var method = owner.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return Assert.IsAssignableFrom<Brush>(method!.Invoke(owner, args));
    }
}
