using Jalium.UI.Controls;

namespace Jalium.UI.Tests;

public class WebBrowserAdapterTests
{
    [Fact]
    public void Source_ShouldForwardToInnerWebView()
    {
        var browser = new WebBrowser();
        var source = new Uri("https://example.com/");

        browser.Source = source;

        var inner = Assert.IsType<WebView>(browser.GetVisualChild(0));
        Assert.Equal(source, inner.Source);
        Assert.Equal(1, browser.VisualChildrenCount);
    }

    [Fact]
    public void InvokeScript_WithEmptyName_ShouldThrow()
    {
        var browser = new WebBrowser();

        Assert.Throws<ArgumentException>(() => browser.InvokeScript(string.Empty));
    }
}
