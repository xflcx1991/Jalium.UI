using System.Xml;

namespace Jalium.UI.Xaml.SourceGenerator;

/// <summary>
/// Parses JALXAML files to extract information needed for code generation.
/// </summary>
public static class JalxamlParser
{
    private const string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
    private const string JaliumNamespace = "http://schemas.jalium.ui/2024";
    private const string PresentationNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    // Mapping from XML element names to C# type names
    private static readonly Dictionary<string, string> TypeMappings = new()
    {
        // Controls
        { "Page", "Page" },
        { "Window", "Window" },
        { "Button", "Button" },
        { "TextBlock", "TextBlock" },
        { "TextBox", "TextBox" },
        { "PasswordBox", "PasswordBox" },
        { "CheckBox", "CheckBox" },
        { "RadioButton", "RadioButton" },
        { "ListBox", "ListBox" },
        { "ComboBox", "ComboBox" },
        { "ScrollViewer", "ScrollViewer" },

        // Layout
        { "StackPanel", "StackPanel" },
        { "Grid", "Grid" },
        { "Canvas", "Canvas" },
        { "Border", "Border" },
        { "DockPanel", "DockPanel" },
        { "WrapPanel", "WrapPanel" },

        // Other
        { "ContentControl", "ContentControl" },
        { "ItemsControl", "ItemsControl" },
        { "UserControl", "UserControl" },
        { "NavigationView", "NavigationView" },
        { "Frame", "Frame" },
    };

    public static JalxamlParseResult? Parse(string content, string filePath)
    {
        var result = new JalxamlParseResult();

        var settings = new XmlReaderSettings
        {
            IgnoreComments = true,
            IgnoreWhitespace = true,
            IgnoreProcessingInstructions = true
        };

        using var stringReader = new StringReader(content);
        using var reader = XmlReader.Create(stringReader, settings);

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                // Parse root element
                result.RootElementType = GetTypeName(reader.LocalName);

                // Look for x:Class attribute
                var classAttr = reader.GetAttribute("Class", XamlNamespace);
                if (!string.IsNullOrEmpty(classAttr))
                {
                    result.ClassName = classAttr;
                }

                // Parse the entire document for x:Name elements
                ParseElement(reader, result);
                break;
            }
        }

        return result;
    }

    private static void ParseElement(XmlReader reader, JalxamlParseResult result)
    {
        var elementName = reader.LocalName;
        var typeName = GetTypeName(elementName);

        // Check for x:Name attribute
        var nameAttr = reader.GetAttribute("Name", XamlNamespace);
        if (!string.IsNullOrEmpty(nameAttr))
        {
            result.NamedElements.Add(new NamedElement
            {
                Name = nameAttr,
                TypeName = typeName
            });
        }

        // If empty element, return
        if (reader.IsEmptyElement)
            return;

        // Parse child elements
        var depth = reader.Depth;
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
                break;

            if (reader.NodeType == XmlNodeType.Element)
            {
                // Skip property elements (e.g., Grid.RowDefinitions)
                if (!reader.LocalName.Contains('.'))
                {
                    ParseElement(reader, result);
                }
                else
                {
                    // Skip property element content
                    SkipElement(reader);
                }
            }
        }
    }

    private static void SkipElement(XmlReader reader)
    {
        if (reader.IsEmptyElement)
            return;

        var depth = reader.Depth;
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
                break;
        }
    }

    private static string GetTypeName(string elementName)
    {
        if (TypeMappings.TryGetValue(elementName, out var typeName))
            return typeName;

        // Default to element name (assume it's a valid type)
        return elementName;
    }
}
