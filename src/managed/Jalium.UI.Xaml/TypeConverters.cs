using System.Globalization;
using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Media;

namespace Jalium.UI.Markup;

/// <summary>
/// Base class for type converters.
/// </summary>
public abstract class TypeConverter
{
    /// <summary>
    /// Returns whether this converter can convert from the specified type.
    /// </summary>
    public virtual bool CanConvertFrom(Type sourceType) => sourceType == typeof(string);

    /// <summary>
    /// Converts the given value to the type of this converter.
    /// </summary>
    public abstract object? ConvertFrom(object? value);
}

/// <summary>
/// Converts strings to Thickness values.
/// </summary>
public class ThicknessConverter : TypeConverter
{
    public override object? ConvertFrom(object? value)
    {
        if (value is not string str) return null;
        str = str.Trim();

        var parts = str.Split(',', ' ').Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

        return parts.Length switch
        {
            1 => new Thickness(double.Parse(parts[0], CultureInfo.InvariantCulture)),
            2 => new Thickness(
                double.Parse(parts[0], CultureInfo.InvariantCulture),
                double.Parse(parts[1], CultureInfo.InvariantCulture)),
            4 => new Thickness(
                double.Parse(parts[0], CultureInfo.InvariantCulture),
                double.Parse(parts[1], CultureInfo.InvariantCulture),
                double.Parse(parts[2], CultureInfo.InvariantCulture),
                double.Parse(parts[3], CultureInfo.InvariantCulture)),
            _ => throw new FormatException($"Invalid Thickness format: {str}")
        };
    }
}

/// <summary>
/// Converts strings to CornerRadius values.
/// </summary>
public class CornerRadiusConverter : TypeConverter
{
    public override object? ConvertFrom(object? value)
    {
        if (value is not string str) return null;
        str = str.Trim();

        var parts = str.Split(',', ' ').Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

        return parts.Length switch
        {
            1 => new CornerRadius(double.Parse(parts[0], CultureInfo.InvariantCulture)),
            4 => new CornerRadius(
                double.Parse(parts[0], CultureInfo.InvariantCulture),
                double.Parse(parts[1], CultureInfo.InvariantCulture),
                double.Parse(parts[2], CultureInfo.InvariantCulture),
                double.Parse(parts[3], CultureInfo.InvariantCulture)),
            _ => throw new FormatException($"Invalid CornerRadius format: {str}")
        };
    }
}

/// <summary>
/// Converts strings to Brush values.
/// </summary>
public class BrushConverter : TypeConverter
{
    private static readonly Dictionary<string, Color> _namedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Transparent"] = Color.FromArgb(0, 255, 255, 255),
        ["Black"] = Color.FromRgb(0, 0, 0),
        ["White"] = Color.FromRgb(255, 255, 255),
        ["Red"] = Color.FromRgb(255, 0, 0),
        ["Green"] = Color.FromRgb(0, 128, 0),
        ["Blue"] = Color.FromRgb(0, 0, 255),
        ["Yellow"] = Color.FromRgb(255, 255, 0),
        ["Orange"] = Color.FromRgb(255, 165, 0),
        ["Purple"] = Color.FromRgb(128, 0, 128),
        ["Pink"] = Color.FromRgb(255, 192, 203),
        ["Gray"] = Color.FromRgb(128, 128, 128),
        ["LightGray"] = Color.FromRgb(211, 211, 211),
        ["DarkGray"] = Color.FromRgb(169, 169, 169),
        ["Cyan"] = Color.FromRgb(0, 255, 255),
        ["Magenta"] = Color.FromRgb(255, 0, 255),
        ["Brown"] = Color.FromRgb(165, 42, 42),
        ["Navy"] = Color.FromRgb(0, 0, 128),
        ["Teal"] = Color.FromRgb(0, 128, 128),
        ["Olive"] = Color.FromRgb(128, 128, 0),
        ["Maroon"] = Color.FromRgb(128, 0, 0),
        ["Silver"] = Color.FromRgb(192, 192, 192),
        ["Lime"] = Color.FromRgb(0, 255, 0),
        ["Aqua"] = Color.FromRgb(0, 255, 255),
        ["Fuchsia"] = Color.FromRgb(255, 0, 255),
    };

    public override object? ConvertFrom(object? value)
    {
        if (value is not string str) return null;
        str = str.Trim();

        // Try named color
        if (_namedColors.TryGetValue(str, out var namedColor))
        {
            return new SolidColorBrush(namedColor);
        }

        // Try hex color
        if (str.StartsWith('#'))
        {
            var color = ParseHexColor(str);
            return new SolidColorBrush(color);
        }

        throw new FormatException($"Invalid brush format: {str}");
    }

    private static Color ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');

        return hex.Length switch
        {
            3 => Color.FromRgb(
                (byte)(Convert.ToByte(hex.Substring(0, 1), 16) * 17),
                (byte)(Convert.ToByte(hex.Substring(1, 1), 16) * 17),
                (byte)(Convert.ToByte(hex.Substring(2, 1), 16) * 17)),
            4 => Color.FromArgb(
                (byte)(Convert.ToByte(hex.Substring(0, 1), 16) * 17),
                (byte)(Convert.ToByte(hex.Substring(1, 1), 16) * 17),
                (byte)(Convert.ToByte(hex.Substring(2, 1), 16) * 17),
                (byte)(Convert.ToByte(hex.Substring(3, 1), 16) * 17)),
            6 => Color.FromRgb(
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16)),
            8 => Color.FromArgb(
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16),
                Convert.ToByte(hex.Substring(6, 2), 16)),
            _ => throw new FormatException($"Invalid hex color format: #{hex}")
        };
    }
}

/// <summary>
/// Converts strings to Color values.
/// </summary>
public class ColorConverter : TypeConverter
{
    private static readonly Dictionary<string, Color> _namedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Transparent"] = Color.FromArgb(0, 255, 255, 255),
        ["Black"] = Color.FromRgb(0, 0, 0),
        ["White"] = Color.FromRgb(255, 255, 255),
        ["Red"] = Color.FromRgb(255, 0, 0),
        ["Green"] = Color.FromRgb(0, 128, 0),
        ["Blue"] = Color.FromRgb(0, 0, 255),
        ["Yellow"] = Color.FromRgb(255, 255, 0),
        ["Orange"] = Color.FromRgb(255, 165, 0),
        ["Purple"] = Color.FromRgb(128, 0, 128),
        ["Pink"] = Color.FromRgb(255, 192, 203),
        ["Gray"] = Color.FromRgb(128, 128, 128),
        ["LightGray"] = Color.FromRgb(211, 211, 211),
        ["DarkGray"] = Color.FromRgb(169, 169, 169),
        ["Cyan"] = Color.FromRgb(0, 255, 255),
        ["Magenta"] = Color.FromRgb(255, 0, 255),
        ["Brown"] = Color.FromRgb(165, 42, 42),
        ["Navy"] = Color.FromRgb(0, 0, 128),
        ["Teal"] = Color.FromRgb(0, 128, 128),
        ["Olive"] = Color.FromRgb(128, 128, 0),
        ["Maroon"] = Color.FromRgb(128, 0, 0),
        ["Silver"] = Color.FromRgb(192, 192, 192),
        ["Lime"] = Color.FromRgb(0, 255, 0),
        ["Aqua"] = Color.FromRgb(0, 255, 255),
        ["Fuchsia"] = Color.FromRgb(255, 0, 255),
    };

    public override object? ConvertFrom(object? value)
    {
        if (value is not string str) return null;
        str = str.Trim();

        // Try named color
        if (_namedColors.TryGetValue(str, out var namedColor))
        {
            return namedColor;
        }

        // Try hex color
        if (str.StartsWith('#'))
        {
            return ParseHexColor(str);
        }

        throw new FormatException($"Invalid color format: {str}");
    }

    private static Color ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');

        return hex.Length switch
        {
            3 => Color.FromRgb(
                (byte)(Convert.ToByte(hex.Substring(0, 1), 16) * 17),
                (byte)(Convert.ToByte(hex.Substring(1, 1), 16) * 17),
                (byte)(Convert.ToByte(hex.Substring(2, 1), 16) * 17)),
            4 => Color.FromArgb(
                (byte)(Convert.ToByte(hex.Substring(0, 1), 16) * 17),
                (byte)(Convert.ToByte(hex.Substring(1, 1), 16) * 17),
                (byte)(Convert.ToByte(hex.Substring(2, 1), 16) * 17),
                (byte)(Convert.ToByte(hex.Substring(3, 1), 16) * 17)),
            6 => Color.FromRgb(
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16)),
            8 => Color.FromArgb(
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16),
                Convert.ToByte(hex.Substring(6, 2), 16)),
            _ => throw new FormatException($"Invalid hex color format: #{hex}")
        };
    }
}

/// <summary>
/// Converts strings to GridLength values.
/// </summary>
public class GridLengthConverter : TypeConverter
{
    public override object? ConvertFrom(object? value)
    {
        if (value is not string str) return null;
        str = str.Trim();

        if (str.Equals("Auto", StringComparison.OrdinalIgnoreCase))
        {
            return GridLength.Auto;
        }

        if (str.Equals("*", StringComparison.Ordinal))
        {
            return GridLength.Star;
        }

        if (str.EndsWith('*'))
        {
            var factor = double.Parse(str.TrimEnd('*'), CultureInfo.InvariantCulture);
            return new GridLength(factor, GridUnitType.Star);
        }

        return new GridLength(double.Parse(str, CultureInfo.InvariantCulture), GridUnitType.Pixel);
    }
}

/// <summary>
/// Converts strings to HorizontalAlignment values.
/// </summary>
public class HorizontalAlignmentConverter : TypeConverter
{
    public override object? ConvertFrom(object? value)
    {
        if (value is not string str) return null;
        return Enum.Parse<HorizontalAlignment>(str, ignoreCase: true);
    }
}

/// <summary>
/// Converts strings to VerticalAlignment values.
/// </summary>
public class VerticalAlignmentConverter : TypeConverter
{
    public override object? ConvertFrom(object? value)
    {
        if (value is not string str) return null;
        return Enum.Parse<VerticalAlignment>(str, ignoreCase: true);
    }
}

/// <summary>
/// Converts strings to Orientation values.
/// </summary>
public class OrientationConverter : TypeConverter
{
    public override object? ConvertFrom(object? value)
    {
        if (value is not string str) return null;
        return Enum.Parse<Orientation>(str, ignoreCase: true);
    }
}

/// <summary>
/// Converts string type names to Type objects.
/// AOT-compatible: uses XamlTypeRegistry for type lookup.
/// </summary>
public class TypeTypeConverter : TypeConverter
{
    public override object? ConvertFrom(object? value)
    {
        if (value is not string typeName) return null;

        // Use the static type registry for AOT compatibility
        return XamlTypeRegistry.GetType(typeName);
    }
}

/// <summary>
/// Registry of type converters.
/// </summary>
public static class TypeConverterRegistry
{
    private static readonly Dictionary<Type, TypeConverter> _converters = new()
    {
        [typeof(Thickness)] = new ThicknessConverter(),
        [typeof(CornerRadius)] = new CornerRadiusConverter(),
        [typeof(Brush)] = new BrushConverter(),
        [typeof(SolidColorBrush)] = new BrushConverter(),
        [typeof(Color)] = new ColorConverter(),
        [typeof(GridLength)] = new GridLengthConverter(),
        [typeof(HorizontalAlignment)] = new HorizontalAlignmentConverter(),
        [typeof(VerticalAlignment)] = new VerticalAlignmentConverter(),
        [typeof(Orientation)] = new OrientationConverter(),
        [typeof(Type)] = new TypeTypeConverter(),
    };

    /// <summary>
    /// Gets a type converter for the specified type.
    /// </summary>
    public static TypeConverter? GetConverter(Type type)
    {
        if (_converters.TryGetValue(type, out var converter))
        {
            return converter;
        }

        // Check for base types/interfaces
        foreach (var (converterType, converterInstance) in _converters)
        {
            if (converterType.IsAssignableFrom(type))
            {
                return converterInstance;
            }
        }

        return null;
    }

    /// <summary>
    /// Registers a type converter for the specified type.
    /// </summary>
    public static void Register(Type type, TypeConverter converter)
    {
        _converters[type] = converter;
    }

    /// <summary>
    /// Converts a string value to the target type.
    /// </summary>
    public static object? ConvertValue(string value, Type targetType)
    {
        if (targetType == typeof(string) || targetType == typeof(object))
            return value;

        if (targetType == typeof(double))
            return double.Parse(value, CultureInfo.InvariantCulture);

        if (targetType == typeof(float))
            return float.Parse(value, CultureInfo.InvariantCulture);

        if (targetType == typeof(int))
            return int.Parse(value, CultureInfo.InvariantCulture);

        if (targetType == typeof(bool))
            return bool.Parse(value);

        if (targetType.IsEnum)
            return Enum.Parse(targetType, value, ignoreCase: true);

        var converter = GetConverter(targetType);
        if (converter != null)
        {
            return converter.ConvertFrom(value);
        }

        // Try TypeConverter attribute (future enhancement)
        return null;
    }
}
