using System.Runtime.CompilerServices;
using Jalium.UI.Markup;

[assembly: InternalsVisibleTo("Jalium.UI.Tests")]

// Expose CLR namespaces defined in Jalium.UI.Xaml under the canonical JALXAML namespace.
[assembly: XmlnsDefinition(JalxamlNamespaces.Presentation, "Jalium.UI.Markup")]
[assembly: XmlnsDefinition(JalxamlNamespaces.Presentation, "Jalium.UI.Markup.Localizer")]
[assembly: XmlnsDefinition(JalxamlNamespaces.Presentation, "Jalium.UI.Xaml")]
