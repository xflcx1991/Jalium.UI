using System.Reflection;

namespace Jalium.UI.Controls.Themes;

/// <summary>
/// Manages theme initialization and application.
/// </summary>
public static class ThemeManager
{
    private static bool _initialized;

    /// <summary>
    /// The resource name of the Generic theme file.
    /// </summary>
    public const string GenericThemeResourceName = "Jalium.UI.Controls.Themes.Generic.jalxaml";

    /// <summary>
    /// Initializes the default theme for the application.
    /// Call this method once at application startup.
    /// </summary>
    /// <param name="app">The application instance.</param>
    public static void Initialize(Application app)
    {
        if (_initialized)
            return;

        ArgumentNullException.ThrowIfNull(app);

        System.Diagnostics.Debug.WriteLine("[ThemeManager.Initialize] Loading Generic theme...");
        Console.WriteLine("[ThemeManager.Initialize] Loading Generic theme...");

        // Try to load the Generic theme using XamlReader via reflection
        // This avoids circular dependency between Controls and Xaml projects
        var theme = LoadGenericThemeViaReflection();
        if (theme != null)
        {
            app.Resources.MergedDictionaries.Add(theme);
            System.Diagnostics.Debug.WriteLine($"[ThemeManager.Initialize] Theme loaded. Resources: {theme.Count}, MergedDicts: {theme.MergedDictionaries.Count}");
            Console.WriteLine($"[ThemeManager.Initialize] Theme loaded. Resources: {theme.Count}, MergedDicts: {theme.MergedDictionaries.Count}");

            // Debug: Check for Button style specifically
            if (theme.TryGetValue(typeof(Button), out var buttonStyle))
            {
                System.Diagnostics.Debug.WriteLine($"[ThemeManager.Initialize] Found Button style: {buttonStyle?.GetType().Name}");
                Console.WriteLine($"[ThemeManager.Initialize] Found Button style: {buttonStyle?.GetType().Name}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[ThemeManager.Initialize] WARNING: Button style NOT found in theme!");
                Console.WriteLine("[ThemeManager.Initialize] WARNING: Button style NOT found in theme!");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[ThemeManager.Initialize] WARNING: Theme loading returned null!");
            Console.WriteLine("[ThemeManager.Initialize] WARNING: Theme loading returned null!");
        }

        _initialized = true;
    }

    /// <summary>
    /// Loads the Generic theme using XamlReader via reflection.
    /// This avoids compile-time dependency on the Xaml project.
    /// </summary>
    private static ResourceDictionary? LoadGenericThemeViaReflection()
    {
        try
        {
            // Try to find the Jalium.UI.Xaml assembly
            var xamlAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Jalium.UI.Xaml");

            if (xamlAssembly == null)
            {
                // Try to load it
                try
                {
                    xamlAssembly = Assembly.Load("Jalium.UI.Xaml");
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("Warning: Jalium.UI.Xaml assembly not found. Theme loading skipped.");
                    return null;
                }
            }

            // Get XamlReader type
            var xamlReaderType = xamlAssembly.GetType("Jalium.UI.Markup.XamlReader");
            if (xamlReaderType == null)
            {
                System.Diagnostics.Debug.WriteLine("Warning: XamlReader type not found in Jalium.UI.Xaml assembly.");
                return null;
            }

            // Get the Load(Stream, string, Assembly) method for proper context
            var loadMethod = xamlReaderType.GetMethod("Load", [typeof(Stream), typeof(string), typeof(Assembly)]);
            if (loadMethod == null)
            {
                // Fallback to Load(Stream) if new overload not available
                loadMethod = xamlReaderType.GetMethod("Load", [typeof(Stream)]);
                if (loadMethod == null)
                {
                    System.Diagnostics.Debug.WriteLine("Warning: XamlReader.Load method not found.");
                    return null;
                }

                // Get the theme stream
                using var stream = GetGenericThemeStream();
                if (stream == null)
                {
                    return null;
                }

                // Invoke XamlReader.Load(stream)
                var result = loadMethod.Invoke(null, [stream]);
                return result as ResourceDictionary;
            }

            // Get the theme stream
            using var themeStream = GetGenericThemeStream();
            if (themeStream == null)
            {
                return null;
            }

            // Invoke XamlReader.Load(stream, resourceName, assembly) with proper context
            var themeResult = loadMethod.Invoke(null, [themeStream, "Themes/Generic.jalxaml", ControlsAssembly]);
            return themeResult as ResourceDictionary;
        }
        catch (Exception ex)
        {
            // Get the real exception (reflection wraps in TargetInvocationException)
            var realException = ex is System.Reflection.TargetInvocationException tie ? tie.InnerException ?? ex : ex;
            System.Diagnostics.Debug.WriteLine($"ERROR: Failed to load theme: {realException.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {realException.StackTrace}");
            // Re-throw to make the error visible during development
            throw new InvalidOperationException($"Failed to load Generic theme: {realException.Message}", realException);
        }
    }

    /// <summary>
    /// Gets the embedded resource stream for the Generic theme.
    /// </summary>
    /// <returns>The stream containing the Generic.jalxaml content, or null if not found.</returns>
    public static Stream? GetGenericThemeStream()
    {
        var assembly = typeof(ThemeManager).Assembly;
        var stream = assembly.GetManifestResourceStream(GenericThemeResourceName);

        if (stream == null)
        {
            // Fallback: try to find the resource with different naming
            var resourceNames = assembly.GetManifestResourceNames();
            var genericResource = resourceNames.FirstOrDefault(n => n.EndsWith("Generic.jalxaml", StringComparison.OrdinalIgnoreCase));

            if (genericResource != null)
            {
                stream = assembly.GetManifestResourceStream(genericResource);
            }

            if (stream == null)
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Could not find embedded resource '{GenericThemeResourceName}'");
            }
        }

        return stream;
    }

    /// <summary>
    /// Gets the Controls assembly for theme resource loading.
    /// </summary>
    public static Assembly ControlsAssembly => typeof(ThemeManager).Assembly;

    /// <summary>
    /// Gets a value indicating whether the theme has been initialized.
    /// </summary>
    public static bool IsInitialized => _initialized;

    /// <summary>
    /// Resets the theme system, allowing re-initialization.
    /// Primarily for testing purposes.
    /// </summary>
    internal static void Reset()
    {
        _initialized = false;
    }
}
