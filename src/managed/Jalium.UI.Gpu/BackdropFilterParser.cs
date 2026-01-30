using System.Globalization;
using System.Text.RegularExpressions;

namespace Jalium.UI.Gpu;

/// <summary>
/// Backdrop Filter 字符串解析器
/// 支持 CSS backdrop-filter 风格的语法
/// </summary>
public static partial class BackdropFilterParser
{
    // 正则表达式用于解析滤镜函数
    [GeneratedRegex(@"(\w+(?:-\w+)?)\s*\(\s*([^)]*)\s*\)", RegexOptions.Compiled)]
    private static partial Regex FilterFunctionRegex();

    [GeneratedRegex(@"^([+-]?\d*\.?\d+)(px|deg|%|rad|turn)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ValueRegex();

    [GeneratedRegex(@"^#([0-9A-Fa-f]{3,8})$", RegexOptions.Compiled)]
    private static partial Regex ColorHexRegex();

    /// <summary>
    /// 解析 backdrop-filter 字符串
    /// </summary>
    /// <param name="filterString">滤镜字符串，如 "blur(20px) brightness(1.2)"</param>
    /// <returns>解析后的滤镜参数</returns>
    public static BackdropFilterParams Parse(string? filterString)
    {
        if (string.IsNullOrWhiteSpace(filterString))
            return BackdropFilterParams.Default;

        // 检查是否是预定义材质
        var materialParams = ParseMaterialPreset(filterString.Trim().ToLowerInvariant());
        if (materialParams.HasValue)
            return materialParams.Value;

        // 解析滤镜函数链
        var result = BackdropFilterParams.Default;
        var matches = FilterFunctionRegex().Matches(filterString);

        foreach (Match match in matches)
        {
            var functionName = match.Groups[1].Value.ToLowerInvariant();
            var argsString = match.Groups[2].Value;
            var args = ParseArguments(argsString);

            var filterParams = ParseFilterFunction(functionName, args);
            result = result.Combine(filterParams);
        }

        return result;
    }

    /// <summary>
    /// 解析预定义材质
    /// </summary>
    private static BackdropFilterParams? ParseMaterialPreset(string preset)
    {
        return preset switch
        {
            "acrylic" => BackdropFilterParams.CreateAcrylic(),
            "mica" => BackdropFilterParams.CreateMica(false),
            "mica-alt" or "micaalt" => BackdropFilterParams.CreateMica(true),
            "frosted" or "frosted-glass" or "frostedglass" => BackdropFilterParams.CreateFrostedGlass(),
            "none" => BackdropFilterParams.Default,
            _ => null
        };
    }

    /// <summary>
    /// 解析单个滤镜函数
    /// </summary>
    private static BackdropFilterParams ParseFilterFunction(string functionName, float[] args)
    {
        return functionName switch
        {
            // 模糊效果
            "blur" => BackdropFilterParams.CreateBlur(args.Length > 0 ? args[0] : 0),
            "gaussian-blur" or "gaussianblur" => new BackdropFilterParams(
                blurRadius: args.Length > 0 ? args[0] : 0,
                blurType: BlurType.Gaussian),
            "box-blur" or "boxblur" => new BackdropFilterParams(
                blurRadius: args.Length > 0 ? args[0] : 0,
                blurType: BlurType.Box),
            "frosted-blur" or "frostedblur" => new BackdropFilterParams(
                blurRadius: args.Length > 0 ? args[0] : 20,
                blurType: BlurType.Frosted,
                noiseIntensity: args.Length > 1 ? args[1] : 0.03f),

            // 颜色调整
            "brightness" => BackdropFilterParams.CreateBrightness(args.Length > 0 ? args[0] : 1),
            "contrast" => BackdropFilterParams.CreateContrast(args.Length > 0 ? args[0] : 1),
            "saturate" or "saturation" => BackdropFilterParams.CreateSaturate(args.Length > 0 ? args[0] : 1),

            // 色彩变换
            "grayscale" or "greyscale" => BackdropFilterParams.CreateGrayscale(args.Length > 0 ? args[0] : 1),
            "sepia" => BackdropFilterParams.CreateSepia(args.Length > 0 ? args[0] : 1),
            "invert" => BackdropFilterParams.CreateInvert(args.Length > 0 ? args[0] : 1),
            "hue-rotate" or "huerotate" => BackdropFilterParams.CreateHueRotate(args.Length > 0 ? args[0] : 0),
            "opacity" => BackdropFilterParams.CreateOpacity(args.Length > 0 ? args[0] : 1),

            // 复合效果
            "frosted-glass" or "frostedglass" => BackdropFilterParams.CreateFrostedGlass(
                blurRadius: args.Length > 0 ? args[0] : 20,
                noiseIntensity: args.Length > 1 ? args[1] : 0.03f),
            "acrylic" => BackdropFilterParams.CreateAcrylic(
                blurRadius: args.Length > 0 ? args[0] : 30),
            "mica" => BackdropFilterParams.CreateMica(false),
            "mica-alt" or "micaalt" => BackdropFilterParams.CreateMica(true),

            // 未知函数返回默认
            _ => BackdropFilterParams.Default
        };
    }

    /// <summary>
    /// 解析参数列表
    /// </summary>
    private static float[] ParseArguments(string argsString)
    {
        if (string.IsNullOrWhiteSpace(argsString))
            return [];

        var args = argsString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = new List<float>();

        foreach (var arg in args)
        {
            var value = ParseValue(arg.Trim());
            if (value.HasValue)
                result.Add(value.Value);
        }

        return [.. result];
    }

    /// <summary>
    /// 解析单个值（支持单位）
    /// </summary>
    private static float? ParseValue(string valueString)
    {
        if (string.IsNullOrWhiteSpace(valueString))
            return null;

        var match = ValueRegex().Match(valueString);
        if (!match.Success)
            return null;

        if (!float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            return null;

        var unit = match.Groups[2].Value.ToLowerInvariant();

        // 根据单位转换值
        return unit switch
        {
            "px" => value,                              // 像素，直接使用
            "%" => value / 100f,                        // 百分比转换为 0-1
            "deg" => value,                             // 角度，保持原样（CreateHueRotate 会转换）
            "rad" => value * 180f / MathF.PI,           // 弧度转角度
            "turn" => value * 360f,                     // 圈数转角度
            "" => value,                                // 无单位，直接使用
            _ => value
        };
    }

    /// <summary>
    /// 解析颜色值
    /// </summary>
    public static uint ParseColor(string colorString)
    {
        if (string.IsNullOrWhiteSpace(colorString))
            return 0;

        colorString = colorString.Trim();

        // 处理 # 开头的十六进制颜色
        var hexMatch = ColorHexRegex().Match(colorString);
        if (hexMatch.Success)
        {
            var hex = hexMatch.Groups[1].Value;
            return hex.Length switch
            {
                3 => ParseShortHex(hex),      // #RGB
                4 => ParseShortHexAlpha(hex), // #ARGB
                6 => ParseLongHex(hex),       // #RRGGBB
                8 => ParseLongHexAlpha(hex),  // #AARRGGBB
                _ => 0
            };
        }

        // 处理命名颜色
        return ParseNamedColor(colorString.ToLowerInvariant());
    }

    private static uint ParseShortHex(string hex)
    {
        // #RGB -> #RRGGBB
        var r = Convert.ToByte(new string(hex[0], 2), 16);
        var g = Convert.ToByte(new string(hex[1], 2), 16);
        var b = Convert.ToByte(new string(hex[2], 2), 16);
        return 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | b;
    }

    private static uint ParseShortHexAlpha(string hex)
    {
        // #ARGB -> #AARRGGBB
        var a = Convert.ToByte(new string(hex[0], 2), 16);
        var r = Convert.ToByte(new string(hex[1], 2), 16);
        var g = Convert.ToByte(new string(hex[2], 2), 16);
        var b = Convert.ToByte(new string(hex[3], 2), 16);
        return ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
    }

    private static uint ParseLongHex(string hex)
    {
        // #RRGGBB
        var rgb = Convert.ToUInt32(hex, 16);
        return 0xFF000000 | rgb;
    }

    private static uint ParseLongHexAlpha(string hex)
    {
        // #AARRGGBB
        return Convert.ToUInt32(hex, 16);
    }

    private static uint ParseNamedColor(string name)
    {
        return name switch
        {
            "transparent" => 0x00000000,
            "black" => 0xFF000000,
            "white" => 0xFFFFFFFF,
            "red" => 0xFFFF0000,
            "green" => 0xFF008000,
            "blue" => 0xFF0000FF,
            "yellow" => 0xFFFFFF00,
            "cyan" or "aqua" => 0xFF00FFFF,
            "magenta" or "fuchsia" => 0xFFFF00FF,
            "gray" or "grey" => 0xFF808080,
            "silver" => 0xFFC0C0C0,
            "maroon" => 0xFF800000,
            "olive" => 0xFF808000,
            "navy" => 0xFF000080,
            "purple" => 0xFF800080,
            "teal" => 0xFF008080,
            "orange" => 0xFFFFA500,
            "pink" => 0xFFFFC0CB,
            "lightgray" or "lightgrey" => 0xFFD3D3D3,
            "darkgray" or "darkgrey" => 0xFFA9A9A9,
            _ => 0
        };
    }

    /// <summary>
    /// 解析材质类型
    /// </summary>
    public static MaterialType ParseMaterialType(string? materialString)
    {
        if (string.IsNullOrWhiteSpace(materialString))
            return MaterialType.None;

        return materialString.Trim().ToLowerInvariant() switch
        {
            "acrylic" => MaterialType.Acrylic,
            "mica" => MaterialType.Mica,
            "mica-alt" or "micaalt" => MaterialType.MicaAlt,
            "frosted" or "frosted-glass" or "frostedglass" => MaterialType.FrostedGlass,
            "custom" => MaterialType.Custom,
            _ => MaterialType.None
        };
    }

    /// <summary>
    /// 从材质字符串创建滤镜参数
    /// </summary>
    public static BackdropFilterParams CreateFromMaterial(
        string? material,
        string? tintColor = null,
        float tintOpacity = 0.6f,
        float blurRadius = 0)
    {
        var materialType = ParseMaterialType(material);
        var tint = ParseColor(tintColor ?? "#FFFFFF");

        return materialType switch
        {
            MaterialType.Acrylic => BackdropFilterParams.CreateAcrylic(
                tintColor: tint,
                tintOpacity: tintOpacity,
                blurRadius: blurRadius > 0 ? blurRadius : 30),

            MaterialType.Mica => BackdropFilterParams.CreateMica(false),

            MaterialType.MicaAlt => BackdropFilterParams.CreateMica(true),

            MaterialType.FrostedGlass => BackdropFilterParams.CreateFrostedGlass(
                blurRadius: blurRadius > 0 ? blurRadius : 20,
                tintColor: tint,
                tintOpacity: tintOpacity),

            _ => BackdropFilterParams.Default
        };
    }

    /// <summary>
    /// 将滤镜参数转换为字符串表示
    /// </summary>
    public static string ToString(BackdropFilterParams param)
    {
        var parts = new List<string>();

        // 模糊
        if (param.BlurRadius > 0)
        {
            var blurFunc = param.BlurType switch
            {
                BlurType.Gaussian => "blur",
                BlurType.Box => "box-blur",
                BlurType.Frosted => "frosted-blur",
                _ => "blur"
            };
            parts.Add($"{blurFunc}({param.BlurRadius:F1}px)");
        }

        // 颜色调整
        if (param.Brightness != 1.0f)
            parts.Add($"brightness({param.Brightness:F2})");
        if (param.Contrast != 1.0f)
            parts.Add($"contrast({param.Contrast:F2})");
        if (param.Saturation != 1.0f)
            parts.Add($"saturate({param.Saturation:F2})");

        // 色彩变换
        if (param.Grayscale > 0)
            parts.Add($"grayscale({param.Grayscale:F2})");
        if (param.Sepia > 0)
            parts.Add($"sepia({param.Sepia:F2})");
        if (param.Invert > 0)
            parts.Add($"invert({param.Invert:F2})");
        if (param.HueRotation != 0)
            parts.Add($"hue-rotate({param.HueRotation * 180f / MathF.PI:F1}deg)");
        if (param.Opacity != 1.0f)
            parts.Add($"opacity({param.Opacity:F2})");

        return parts.Count > 0 ? string.Join(" ", parts) : "none";
    }
}
