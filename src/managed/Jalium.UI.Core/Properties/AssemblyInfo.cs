using System.Runtime.CompilerServices;
using Jalium.UI.Markup;

[assembly: InternalsVisibleTo("Jalium.UI.Input")]
[assembly: InternalsVisibleTo("Jalium.UI.Controls")]
[assembly: InternalsVisibleTo("Jalium.UI.Interop")]
[assembly: InternalsVisibleTo("Jalium.UI.Xaml")]
[assembly: InternalsVisibleTo("Jalium.UI.Media")]
[assembly: InternalsVisibleTo("Jalium.UI.Tests")]
[assembly: InternalsVisibleTo("ReactiveUI.Wpf")]

// Expose CLR namespaces defined in Jalium.UI.Core under the canonical JALXAML namespace so that
// documents using xmlns="http://schemas.jalium.com/jalxaml" can reference these types directly.
[assembly: XmlnsDefinition(JalxamlNamespaces.Presentation, "Jalium.UI")]
[assembly: XmlnsDefinition(JalxamlNamespaces.Presentation, "Jalium.UI.Automation")]
[assembly: XmlnsDefinition(JalxamlNamespaces.Presentation, "Jalium.UI.Collections")]
[assembly: XmlnsDefinition(JalxamlNamespaces.Presentation, "Jalium.UI.Data")]
[assembly: XmlnsDefinition(JalxamlNamespaces.Presentation, "Jalium.UI.Documents")]
[assembly: XmlnsDefinition(JalxamlNamespaces.Presentation, "Jalium.UI.Input")]
[assembly: XmlnsDefinition(JalxamlNamespaces.Presentation, "Jalium.UI.Interactivity")]
[assembly: XmlnsDefinition(JalxamlNamespaces.Presentation, "Jalium.UI.Markup")]
[assembly: XmlnsDefinition(JalxamlNamespaces.Presentation, "Jalium.UI.Media.Animation")]
[assembly: XmlnsDefinition(JalxamlNamespaces.Presentation, "Jalium.UI.Threading")]
