using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jalium.UI.Hosting;

/// <summary>
/// Opt-in switches for Jalium.UI's built-in developer surfaces.
/// Default: both features are <see langword="false"/> so they never show up in
/// shipped applications. Opt in from <see cref="AppBuilder"/>:
/// <code>
/// var builder = AppBuilder.CreateBuilder(args);
/// builder.UseDevTools();    // F12 + Ctrl+Shift+C picker
/// builder.UseDebugHud();    // F3 overlay
/// </code>
/// Without these calls the key handlers silently ignore F3/F12 — no DevToolsWindow
/// is ever constructed, no HUD overlay is ever toggled on.
/// </summary>
public sealed class DeveloperToolsOptions
{
    /// <summary>
    /// When <see langword="true"/>, F12 opens the DevTools inspector window and
    /// Ctrl+Shift+C activates the element-picker gesture. Otherwise those keys
    /// are inert.
    /// </summary>
    public bool EnableDevTools { get; set; } = false;

    /// <summary>
    /// When <see langword="true"/>, F3 toggles the on-screen debug HUD overlay
    /// (frame timings, dirty regions, backend info). Otherwise F3 is a no-op.
    /// </summary>
    public bool EnableDebugHud { get; set; } = false;
}

/// <summary>
/// Static resolver used by low-level framework hooks (Window, input dispatcher)
/// that need to check the developer-tools switches without forcing a reference
/// through <see cref="Application.Services"/> at every call site. Returns
/// safe defaults when the application was not built via <see cref="AppBuilder"/>
/// or when the options weren't registered.
/// </summary>
internal static class DeveloperToolsResolver
{
    public static DeveloperToolsOptions Current
    {
        get
        {
            var services = Application.Current?.Services;
            if (services == null) return DefaultsInstance;

            try
            {
                return services.GetService<IOptions<DeveloperToolsOptions>>()?.Value
                       ?? DefaultsInstance;
            }
            catch
            {
                // ObjectDisposedException during shutdown, missing options
                // registration, etc. — treat as "not enabled".
                return DefaultsInstance;
            }
        }
    }

    public static bool IsDevToolsEnabled => Current.EnableDevTools;
    public static bool IsDebugHudEnabled => Current.EnableDebugHud;

    // Shared read-only defaults so the hot path doesn't allocate.
    private static readonly DeveloperToolsOptions DefaultsInstance = new();
}
