using Jalium.UI.Media;

namespace Jalium.UI.Controls.DevTools;

/// <summary>
/// Centralised palette + type scale for every DevTools surface. Tuned for a dark,
/// dense "developer console" aesthetic comparable to Chrome/VS Code DevTools.
/// All panels should pull brushes/fonts from here so that a single theme change
/// recolors the whole tool window.
/// </summary>
internal static class DevToolsTheme
{
    // ── Surface palette ──────────────────────────────────────────────────
    // Four layers of depth, from window chrome down to inline controls.
    public static readonly Color ChromeColor     = Color.FromRgb(0x1E, 0x1E, 0x20); // window chrome / dialog bg
    public static readonly Color SurfaceColor    = Color.FromRgb(0x25, 0x25, 0x28); // primary tab surface
    public static readonly Color SurfaceAltColor = Color.FromRgb(0x2D, 0x2D, 0x31); // cards / secondary pane
    public static readonly Color RowAltColor     = Color.FromRgb(0x33, 0x33, 0x38); // zebra striping
    public static readonly Color ControlColor    = Color.FromRgb(0x38, 0x38, 0x3F); // input/button idle bg

    // ── Lines & dividers ────────────────────────────────────────────────
    public static readonly Color BorderColor      = Color.FromRgb(0x3D, 0x3D, 0x42);
    public static readonly Color BorderSubtleColor = Color.FromRgb(0x35, 0x35, 0x3A);
    public static readonly Color BorderStrongColor = Color.FromRgb(0x52, 0x52, 0x58);

    // ── Text hierarchy ──────────────────────────────────────────────────
    public static readonly Color TextPrimaryColor   = Color.FromRgb(0xE6, 0xE6, 0xE6);
    public static readonly Color TextSecondaryColor = Color.FromRgb(0xA4, 0xA4, 0xAA);
    public static readonly Color TextMutedColor     = Color.FromRgb(0x70, 0x70, 0x78);
    public static readonly Color TextDisabledColor  = Color.FromRgb(0x55, 0x55, 0x5C);

    // ── Accents ─────────────────────────────────────────────────────────
    public static readonly Color AccentColor        = Color.FromRgb(0x3A, 0x9D, 0xFF); // selected tab / primary action
    public static readonly Color AccentHoverColor   = Color.FromRgb(0x5A, 0xB0, 0xFF);
    public static readonly Color AccentPressedColor = Color.FromRgb(0x2A, 0x85, 0xE5);
    public static readonly Color AccentSoftColor   = Color.FromArgb(0x33, 0x3A, 0x9D, 0xFF); // button-fill when active

    // ── Semantic palette (for logs, stats, diagnostics) ─────────────────
    public static readonly Color SuccessColor = Color.FromRgb(0x6C, 0xC3, 0x78);
    public static readonly Color WarningColor = Color.FromRgb(0xE5, 0xB0, 0x5A);
    public static readonly Color ErrorColor   = Color.FromRgb(0xF0, 0x6C, 0x6C);
    public static readonly Color InfoColor    = Color.FromRgb(0x62, 0xB5, 0xE8);

    // ── Syntax-ish (borrowed from VS Code Dark+) ────────────────────────
    public static readonly Color TokenStringColor   = Color.FromRgb(0xCE, 0x91, 0x78);
    public static readonly Color TokenNumberColor   = Color.FromRgb(0xB5, 0xCE, 0xA8);
    public static readonly Color TokenKeywordColor  = Color.FromRgb(0xC5, 0x86, 0xC0);
    public static readonly Color TokenTypeColor     = Color.FromRgb(0x4E, 0xC9, 0xB0);
    public static readonly Color TokenBoolColor     = Color.FromRgb(0x56, 0x9C, 0xD6);
    public static readonly Color TokenPropertyColor = Color.FromRgb(0x9C, 0xDC, 0xFE);
    public static readonly Color TokenEnumColor     = Color.FromRgb(0xDC, 0xDC, 0xAA);

    // ── Brushes (pre-allocated, shared) ─────────────────────────────────
    public static readonly SolidColorBrush Chrome         = new(ChromeColor);
    public static readonly SolidColorBrush Surface        = new(SurfaceColor);
    public static readonly SolidColorBrush SurfaceAlt     = new(SurfaceAltColor);
    public static readonly SolidColorBrush RowAlt         = new(RowAltColor);
    public static readonly SolidColorBrush Control        = new(ControlColor);
    public static readonly SolidColorBrush ControlHover   = new(Color.FromRgb(0x42, 0x42, 0x48));
    public static readonly SolidColorBrush ControlPressed = new(Color.FromRgb(0x2E, 0x2E, 0x33));
    public static readonly SolidColorBrush Border         = new(BorderColor);
    public static readonly SolidColorBrush BorderSubtle   = new(BorderSubtleColor);
    public static readonly SolidColorBrush BorderStrong   = new(BorderStrongColor);

    public static readonly SolidColorBrush TextPrimary   = new(TextPrimaryColor);
    public static readonly SolidColorBrush TextSecondary = new(TextSecondaryColor);
    public static readonly SolidColorBrush TextMuted     = new(TextMutedColor);
    public static readonly SolidColorBrush TextDisabled  = new(TextDisabledColor);

    public static readonly SolidColorBrush Accent        = new(AccentColor);
    public static readonly SolidColorBrush AccentHover   = new(AccentHoverColor);
    public static readonly SolidColorBrush AccentPressed = new(AccentPressedColor);
    public static readonly SolidColorBrush AccentSoft    = new(AccentSoftColor);

    public static readonly SolidColorBrush Success = new(SuccessColor);
    public static readonly SolidColorBrush Warning = new(WarningColor);
    public static readonly SolidColorBrush Error   = new(ErrorColor);
    public static readonly SolidColorBrush Info    = new(InfoColor);

    public static readonly SolidColorBrush TokenString   = new(TokenStringColor);
    public static readonly SolidColorBrush TokenNumber   = new(TokenNumberColor);
    public static readonly SolidColorBrush TokenKeyword  = new(TokenKeywordColor);
    public static readonly SolidColorBrush TokenType     = new(TokenTypeColor);
    public static readonly SolidColorBrush TokenBool     = new(TokenBoolColor);
    public static readonly SolidColorBrush TokenProperty = new(TokenPropertyColor);
    public static readonly SolidColorBrush TokenEnum     = new(TokenEnumColor);

    // ── Type scale ──────────────────────────────────────────────────────
    public static readonly FontFamily UiFont   = new(FrameworkElement.DefaultFontFamilyName);
    public static readonly FontFamily MonoFont = new("Consolas");

    // Explicit constant sizes — use these instead of inlined literals.
    public const double FontXS   = 10;  // badge / meta
    public const double FontSm   = 11;  // secondary body / table cell
    public const double FontBase = 12;  // default body
    public const double FontLg   = 13;  // section title
    public const double FontXL   = 15;  // panel heading

    // ── Spacing ─────────────────────────────────────────────────────────
    public const double GutterXS = 2;
    public const double GutterSm = 4;
    public const double GutterBase = 8;
    public const double GutterLg = 12;
    public const double GutterXL = 16;

    // ── Radii ────────────────────────────────────────────────────────────
    public static readonly CornerRadius RadiusSm = new(3);
    public static readonly CornerRadius RadiusBase = new(4);

    // ── Thicknesses (single object reuse where possible) ────────────────
    public static readonly Thickness ThicknessHairline = new(1);
    public static readonly Thickness ThicknessBottom   = new(0, 0, 0, 1);
    public static readonly Thickness ThicknessRight    = new(0, 0, 1, 0);
}
