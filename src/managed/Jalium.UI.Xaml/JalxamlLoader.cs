using System.Reflection;

namespace Jalium.UI.Markup;

/// <summary>
/// Provides methods for loading JALXAML (Jalium XAML) files.
/// </summary>
public static class JalxamlLoader
{
    /// <summary>
    /// Loads a JALXAML file from an embedded resource.
    /// </summary>
    /// <param name="resourceName">The name of the embedded resource (e.g., "Pages/HomePage.jalxaml").</param>
    /// <param name="assembly">The assembly containing the resource. If null, uses the calling assembly.</param>
    /// <returns>The root object created from the JALXAML.</returns>
    public static object LoadFromResource(string resourceName, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();

        // Try different resource name formats
        var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            // Try with assembly name prefix
            var assemblyName = assembly.GetName().Name;
            stream = assembly.GetManifestResourceStream($"{assemblyName}.{resourceName}");
        }

        if (stream == null)
        {
            // Try replacing path separators
            var normalizedName = resourceName.Replace('/', '.').Replace('\\', '.');
            stream = assembly.GetManifestResourceStream(normalizedName);

            if (stream == null)
            {
                var assemblyName = assembly.GetName().Name;
                stream = assembly.GetManifestResourceStream($"{assemblyName}.{normalizedName}");
            }
        }

        if (stream == null)
        {
            // List available resources for debugging
            var availableResources = assembly.GetManifestResourceNames();
            throw new JalxamlLoadException(
                $"Cannot find embedded resource '{resourceName}' in assembly '{assembly.GetName().Name}'. " +
                $"Available resources: [{string.Join(", ", availableResources)}]");
        }

        using (stream)
        {
            return XamlReader.Load(stream);
        }
    }

    /// <summary>
    /// Loads a JALXAML file from the file system.
    /// </summary>
    /// <param name="filePath">The path to the JALXAML file.</param>
    /// <returns>The root object created from the JALXAML.</returns>
    public static object LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new JalxamlLoadException($"JALXAML file not found: {filePath}");
        }

        var content = File.ReadAllText(filePath);
        return XamlReader.Parse(content);
    }

    /// <summary>
    /// Loads a JALXAML string and creates an object tree.
    /// </summary>
    /// <param name="jalxaml">The JALXAML content string.</param>
    /// <returns>The root object created from the JALXAML.</returns>
    public static object Parse(string jalxaml)
    {
        return XamlReader.Parse(jalxaml);
    }

    /// <summary>
    /// Loads a JALXAML file from an embedded resource and casts to the specified type.
    /// </summary>
    /// <typeparam name="T">The expected type of the root element.</typeparam>
    /// <param name="resourceName">The name of the embedded resource.</param>
    /// <param name="assembly">The assembly containing the resource. If null, uses the calling assembly.</param>
    /// <returns>The root object cast to type T.</returns>
    public static T LoadFromResource<T>(string resourceName, Assembly? assembly = null) where T : class
    {
        assembly ??= Assembly.GetCallingAssembly();
        var result = LoadFromResource(resourceName, assembly);

        if (result is T typedResult)
        {
            return typedResult;
        }

        throw new JalxamlLoadException(
            $"Expected root element of type '{typeof(T).Name}' but got '{result?.GetType().Name ?? "null"}'.");
    }

    /// <summary>
    /// Loads a JALXAML file from the file system and casts to the specified type.
    /// </summary>
    /// <typeparam name="T">The expected type of the root element.</typeparam>
    /// <param name="filePath">The path to the JALXAML file.</param>
    /// <returns>The root object cast to type T.</returns>
    public static T LoadFromFile<T>(string filePath) where T : class
    {
        var result = LoadFromFile(filePath);

        if (result is T typedResult)
        {
            return typedResult;
        }

        throw new JalxamlLoadException(
            $"Expected root element of type '{typeof(T).Name}' but got '{result?.GetType().Name ?? "null"}'.");
    }

    /// <summary>
    /// Loads JALXAML content into an existing component instance (for code-behind support).
    /// This is typically called from InitializeComponent() in code-behind classes.
    /// </summary>
    /// <param name="component">The component instance to load into.</param>
    /// <param name="resourceName">The embedded resource name of the JALXAML file.</param>
    /// <param name="assembly">The assembly containing the resource. If null, uses the assembly of the component type.</param>
    public static void LoadComponent(object component, string resourceName, Assembly? assembly = null)
    {
        XamlReader.LoadComponent(component, resourceName, assembly);
    }

    /// <summary>
    /// Loads JALXAML content into an existing component instance using a Pack URI.
    /// This is the preferred method called from generated InitializeComponent() methods.
    /// </summary>
    /// <param name="component">The component instance to load into.</param>
    /// <param name="resourceUri">Pack URI in the format: /AssemblyName;component/Path/To/File.jalxaml</param>
    /// <remarks>
    /// Pack URI format: /AssemblyName;component/relative/path/file.jalxaml
    /// Example: /Jalium.UI.Gallery;component/Views/ButtonPage.jalxaml
    /// </remarks>
    public static void LoadComponent(object component, Uri resourceUri)
    {
        var (assemblyName, componentPath) = ParsePackUri(resourceUri);

        // Find the assembly by name
        Assembly? assembly = null;
        if (!string.IsNullOrEmpty(assemblyName))
        {
            // First try to get from already loaded assemblies
            assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName);

            // If not found, try the component's assembly
            assembly ??= component.GetType().Assembly;
        }
        else
        {
            assembly = component.GetType().Assembly;
        }

        // Convert component path to embedded resource name
        // Views/ButtonPage.jalxaml -> AssemblyName.Views.ButtonPage.jalxaml
        var resourceName = componentPath.Replace('/', '.');
        var fullResourceName = $"{assembly.GetName().Name}.{resourceName}";

        XamlReader.LoadComponent(component, fullResourceName, assembly);
    }

    /// <summary>
    /// Parses a Pack URI to extract assembly name and component path.
    /// </summary>
    /// <param name="packUri">The Pack URI to parse.</param>
    /// <returns>A tuple of (assemblyName, componentPath).</returns>
    /// <exception cref="JalxamlLoadException">Thrown when the URI format is invalid.</exception>
    private static (string assemblyName, string componentPath) ParsePackUri(Uri packUri)
    {
        // Expected format: /AssemblyName;component/Path/To/File.jalxaml
        var uriString = packUri.OriginalString;

        // Remove leading slash
        if (uriString.StartsWith("/"))
        {
            uriString = uriString.Substring(1);
        }

        // Find ";component/" separator
        const string componentSeparator = ";component/";
        var separatorIndex = uriString.IndexOf(componentSeparator, StringComparison.OrdinalIgnoreCase);

        if (separatorIndex < 0)
        {
            throw new JalxamlLoadException(
                $"Invalid Pack URI format: '{packUri.OriginalString}'. " +
                $"Expected format: /AssemblyName;component/Path/To/File.jalxaml");
        }

        var assemblyName = uriString.Substring(0, separatorIndex);
        var componentPath = uriString.Substring(separatorIndex + componentSeparator.Length);

        return (assemblyName, componentPath);
    }

    /// <summary>
    /// Gets the default JALXAML resource name for a component type.
    /// Convention: TypeName.jalxaml in the same namespace folder structure.
    /// </summary>
    /// <param name="componentType">The type of the component.</param>
    /// <returns>The conventional resource name.</returns>
    public static string GetDefaultResourceName(Type componentType)
    {
        // Convert namespace to path: Jalium.UI.Gallery.Views.BorderPage -> Jalium.UI.Gallery.Views.BorderPage.jalxaml
        return $"{componentType.FullName}.jalxaml";
    }
}

/// <summary>
/// Exception thrown when JALXAML loading fails.
/// </summary>
public class JalxamlLoadException : Exception
{
    public JalxamlLoadException(string message) : base(message) { }
    public JalxamlLoadException(string message, Exception innerException) : base(message, innerException) { }
}
