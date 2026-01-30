using Jalium.UI.Controls.Themes;

namespace Jalium.UI.Markup;

/// <summary>
/// Provides theme loading utilities for the Jalium.UI framework.
/// </summary>
public static class ThemeLoader
{
    /// <summary>
    /// Loads a ResourceDictionary from a stream containing XAML content.
    /// </summary>
    /// <param name="stream">The stream containing the XAML content.</param>
    /// <returns>The loaded ResourceDictionary, or null if loading failed.</returns>
    public static ResourceDictionary? LoadResourceDictionary(Stream stream)
    {
        try
        {
            return XamlReader.Load(stream) as ResourceDictionary;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load ResourceDictionary: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Loads the Generic theme ResourceDictionary.
    /// </summary>
    /// <returns>The Generic theme ResourceDictionary, or null if loading failed.</returns>
    public static ResourceDictionary? LoadGenericTheme()
    {
        using var stream = ThemeManager.GetGenericThemeStream();
        if (stream == null)
            return null;

        return LoadResourceDictionary(stream);
    }
}
