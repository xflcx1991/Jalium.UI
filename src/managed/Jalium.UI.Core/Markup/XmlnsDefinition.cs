namespace Jalium.UI.Markup;

/// <summary>
/// Associates an XML namespace with a CLR namespace in an assembly, allowing
/// types from that CLR namespace to be referenced from JALXAML by the XML namespace.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute at assembly scope. Multiple attributes may be applied to
/// map a single XML namespace to several CLR namespaces within the same assembly,
/// or to expose the same CLR namespace under multiple XML namespaces.
/// </para>
/// <example>
/// <code>
/// [assembly: XmlnsDefinition("http://schemas.jalium.com/jalxaml", "Jalium.UI.Controls")]
/// [assembly: XmlnsDefinition("http://schemas.jalium.com/jalxaml", "Jalium.UI.Controls.Primitives")]
/// </code>
/// </example>
/// <para>
/// When <see cref="AssemblyName"/> is specified, the mapping refers to a CLR namespace in
/// another assembly (which must be resolvable at load time). Leaving it unset binds the
/// mapping to the declaring assembly, which is the common case.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = true)]
public sealed class XmlnsDefinitionAttribute : Attribute
{
    /// <summary>
    /// Creates a mapping from an XML namespace to a CLR namespace defined in the same assembly.
    /// </summary>
    /// <param name="xmlNamespace">The XML namespace identifier (e.g. <c>http://schemas.jalium.com/jalxaml</c>).</param>
    /// <param name="clrNamespace">The CLR namespace that contains the types (e.g. <c>Jalium.UI.Controls</c>).</param>
    public XmlnsDefinitionAttribute(string xmlNamespace, string clrNamespace)
    {
        ArgumentNullException.ThrowIfNull(xmlNamespace);
        ArgumentNullException.ThrowIfNull(clrNamespace);
        XmlNamespace = xmlNamespace;
        ClrNamespace = clrNamespace;
    }

    /// <summary>
    /// Gets the XML namespace identifier.
    /// </summary>
    public string XmlNamespace { get; }

    /// <summary>
    /// Gets the CLR namespace that contains the types.
    /// </summary>
    public string ClrNamespace { get; }

    /// <summary>
    /// Gets or sets the optional simple name of the assembly that defines <see cref="ClrNamespace"/>.
    /// When unset, the attribute binds to the declaring assembly.
    /// </summary>
    public string? AssemblyName { get; set; }
}

/// <summary>
/// Declares the recommended prefix for a given XML namespace. Used by designers and
/// serialization to emit human-friendly prefixes.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = true)]
public sealed class XmlnsPrefixAttribute : Attribute
{
    /// <summary>
    /// Associates an XML namespace with a preferred prefix.
    /// </summary>
    /// <param name="xmlNamespace">The XML namespace identifier.</param>
    /// <param name="prefix">The preferred prefix (e.g. <c>ui</c>).</param>
    public XmlnsPrefixAttribute(string xmlNamespace, string prefix)
    {
        ArgumentNullException.ThrowIfNull(xmlNamespace);
        ArgumentNullException.ThrowIfNull(prefix);
        XmlNamespace = xmlNamespace;
        Prefix = prefix;
    }

    /// <summary>
    /// Gets the XML namespace identifier.
    /// </summary>
    public string XmlNamespace { get; }

    /// <summary>
    /// Gets the preferred prefix for <see cref="XmlNamespace"/>.
    /// </summary>
    public string Prefix { get; }
}

/// <summary>
/// Declares that one XML namespace should be treated as compatible with (subsumed by)
/// another — references to <see cref="OldNamespace"/> are rewritten to
/// <see cref="NewNamespace"/> during parsing.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = true)]
public sealed class XmlnsCompatibleWithAttribute : Attribute
{
    /// <summary>
    /// Declares that <paramref name="oldNamespace"/> should be redirected to <paramref name="newNamespace"/>.
    /// </summary>
    /// <param name="oldNamespace">The legacy XML namespace identifier.</param>
    /// <param name="newNamespace">The current XML namespace identifier that supersedes it.</param>
    public XmlnsCompatibleWithAttribute(string oldNamespace, string newNamespace)
    {
        ArgumentNullException.ThrowIfNull(oldNamespace);
        ArgumentNullException.ThrowIfNull(newNamespace);
        OldNamespace = oldNamespace;
        NewNamespace = newNamespace;
    }

    /// <summary>
    /// Gets the legacy XML namespace identifier.
    /// </summary>
    public string OldNamespace { get; }

    /// <summary>
    /// Gets the replacement XML namespace identifier.
    /// </summary>
    public string NewNamespace { get; }
}

/// <summary>
/// Canonical XML namespace identifiers used throughout the Jalium.UI framework's JALXAML dialect.
/// Using the constants ensures assembly-level mappings stay in sync across projects.
/// </summary>
public static class JalxamlNamespaces
{
    /// <summary>
    /// The primary XML namespace under which the Jalium.UI framework's XAML types are exposed.
    /// Equivalent to WPF's <c>http://schemas.microsoft.com/winfx/2006/xaml/presentation</c>.
    /// </summary>
    public const string Presentation = "http://schemas.jalium.com/jalxaml";

    /// <summary>
    /// The XML namespace for JALXAML markup-only tokens such as <c>x:Class</c>, <c>x:Name</c>,
    /// and <c>x:Key</c>. Corresponds to WPF's <c>http://schemas.microsoft.com/winfx/2006/xaml</c>.
    /// </summary>
    public const string XamlMarkup = "https://schemas.jalium.dev/jalxaml/markup";

    /// <summary>
    /// Legacy Jalium.UI namespace shipped in 2024 previews. Retained for document compatibility
    /// via <see cref="XmlnsCompatibleWithAttribute"/>.
    /// </summary>
    public const string LegacyJaliumUi = "http://schemas.jalium.ui/2024";

    /// <summary>
    /// Legacy Jalium.dev namespace. Retained for document compatibility via
    /// <see cref="XmlnsCompatibleWithAttribute"/>.
    /// </summary>
    public const string LegacyJaliumDev = "https://schemas.jalium.dev/jalxaml";

    /// <summary>
    /// WPF's presentation namespace. Retained so documents authored against stock WPF tooling
    /// parse correctly under Jalium.UI.
    /// </summary>
    public const string WpfPresentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    /// <summary>
    /// WPF's XAML markup namespace. Parallel to <see cref="XamlMarkup"/>.
    /// </summary>
    public const string WpfXamlMarkup = "http://schemas.microsoft.com/winfx/2006/xaml";
}
