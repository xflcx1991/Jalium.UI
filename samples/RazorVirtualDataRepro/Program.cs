using Jalium.UI;
using Jalium.UI.Interop;

namespace RazorVirtualDataRepro;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var app = new Application();

        var ctx = RenderContext.GetOrCreateCurrent(RenderBackend.Auto);
        ctx.DefaultRenderingEngine = RenderingEngine.Impeller;

        var window = new MainWindow();
        app.Run(window);
    }
}
