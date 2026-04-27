using Jalium.UI.Media;

namespace Jalium.UI.Controls.DevTools;

/// <summary>
/// Factory helpers that produce consistently-styled UI parts for the DevTools
/// surfaces. Tabs should use these instead of hand-rolling Borders so visual
/// behavior (hover, active, typography, spacing) stays uniform.
/// </summary>
internal static class DevToolsUi
{
    // ── Typography ───────────────────────────────────────────────────────

    public static TextBlock Text(string content, double size = DevToolsTheme.FontBase, Brush? color = null, FontWeight? weight = null, bool mono = false)
    {
        return new TextBlock
        {
            Text = content,
            FontSize = size,
            FontFamily = mono ? DevToolsTheme.MonoFont : DevToolsTheme.UiFont,
            Foreground = color ?? DevToolsTheme.TextPrimary,
            FontWeight = weight ?? FontWeights.Normal,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }

    public static TextBlock Muted(string content, double size = DevToolsTheme.FontSm)
        => Text(content, size, DevToolsTheme.TextSecondary);

    public static TextBlock Mono(string content, double size = DevToolsTheme.FontSm, Brush? color = null)
        => Text(content, size, color ?? DevToolsTheme.TextPrimary, mono: true);

    public static TextBlock SectionHeading(string content)
    {
        return new TextBlock
        {
            Text = content,
            FontSize = DevToolsTheme.FontLg,
            FontFamily = DevToolsTheme.UiFont,
            FontWeight = FontWeights.SemiBold,
            Foreground = DevToolsTheme.Accent,
            Margin = new Thickness(0, DevToolsTheme.GutterLg, 0, DevToolsTheme.GutterSm),
        };
    }

    public static TextBlock PanelHeading(string content)
    {
        return new TextBlock
        {
            Text = content,
            FontSize = DevToolsTheme.FontBase,
            FontFamily = DevToolsTheme.UiFont,
            FontWeight = FontWeights.SemiBold,
            Foreground = DevToolsTheme.TextPrimary,
            Margin = new Thickness(0, 0, 0, DevToolsTheme.GutterSm),
        };
    }

    // ── Panels / surfaces ────────────────────────────────────────────────

    public static Border Card(UIElement child, bool alternate = false)
    {
        return new Border
        {
            Background = alternate ? DevToolsTheme.SurfaceAlt : DevToolsTheme.Surface,
            BorderBrush = DevToolsTheme.BorderSubtle,
            BorderThickness = DevToolsTheme.ThicknessHairline,
            CornerRadius = DevToolsTheme.RadiusBase,
            Padding = new Thickness(DevToolsTheme.GutterBase),
            Child = child,
            ClipToBounds = true,
        };
    }

    /// <summary>
    /// A bar that sits along the top of a panel, hosting buttons/filters.
    /// Bottom hairline divider mirrors IDE toolbars.
    /// </summary>
    public static Border Toolbar(UIElement child)
    {
        return new Border
        {
            Background = DevToolsTheme.Chrome,
            BorderBrush = DevToolsTheme.BorderSubtle,
            BorderThickness = DevToolsTheme.ThicknessBottom,
            Padding = new Thickness(DevToolsTheme.GutterBase, DevToolsTheme.GutterSm, DevToolsTheme.GutterBase, DevToolsTheme.GutterSm),
            Child = child,
        };
    }

    public static Border StatusBar(UIElement child)
    {
        return new Border
        {
            Background = DevToolsTheme.Chrome,
            BorderBrush = DevToolsTheme.BorderSubtle,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(DevToolsTheme.GutterBase, DevToolsTheme.GutterXS, DevToolsTheme.GutterBase, DevToolsTheme.GutterXS),
            Child = child,
        };
    }

    // ── Buttons ──────────────────────────────────────────────────────────

    public sealed class DevToolsButton : Border
    {
        private readonly Border _content;
        private bool _isActive;
        private bool _isHovered;
        private bool _isPressed;
        private ButtonStyle _style;
        private readonly TextBlock? _iconText;
        private readonly TextBlock _labelText;
        private string _labelValue;

        /// <summary>
        /// Change the visual style at runtime. Useful for toggle buttons that
        /// should swap between Primary (start) and Danger (stop) palettes.
        /// </summary>
        public new ButtonStyle Style
        {
            get => _style;
            set
            {
                if (_style == value) return;
                _style = value;
                ApplyVisualState();
            }
        }

        public DevToolsButton(string label, Action? onClick, ButtonStyle style = ButtonStyle.Default, string? iconGlyph = null)
        {
            _style = style;
            _labelValue = label;

            BorderThickness = DevToolsTheme.ThicknessHairline;
            CornerRadius = DevToolsTheme.RadiusSm;
            Padding = new Thickness(0);
            Margin = new Thickness(0, 0, DevToolsTheme.GutterSm, 0);

            _iconText = iconGlyph == null ? null : new TextBlock
            {
                Text = iconGlyph,
                FontSize = DevToolsTheme.FontSm,
                FontFamily = DevToolsTheme.UiFont,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, DevToolsTheme.GutterSm, 0),
            };
            _labelText = new TextBlock
            {
                Text = label,
                FontSize = DevToolsTheme.FontSm,
                FontFamily = DevToolsTheme.UiFont,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            if (_iconText != null) row.Children.Add(_iconText);
            row.Children.Add(_labelText);

            _content = new Border
            {
                Padding = new Thickness(DevToolsTheme.GutterBase, DevToolsTheme.GutterSm - 1, DevToolsTheme.GutterBase, DevToolsTheme.GutterSm - 1),
                Child = row,
            };
            Child = _content;

            ApplyVisualState();

            MouseEnter += (_, _) => { _isHovered = true; ApplyVisualState(); };
            MouseLeave += (_, _) => { _isHovered = false; _isPressed = false; ApplyVisualState(); };
            MouseDown += (_, _) => { _isPressed = true; ApplyVisualState(); };
            MouseUp += (_, _) =>
            {
                if (_isPressed && _isHovered)
                    onClick?.Invoke();
                _isPressed = false;
                ApplyVisualState();
            };
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value) return;
                _isActive = value;
                ApplyVisualState();
            }
        }

        public string Label
        {
            get => _labelValue;
            set
            {
                _labelValue = value;
                _labelText.Text = value;
            }
        }

        public void SetIcon(string? glyph)
        {
            if (_iconText != null)
                _iconText.Text = glyph ?? string.Empty;
        }

        private void ApplyVisualState()
        {
            Brush bg, border, fg;
            switch (_style)
            {
                case ButtonStyle.Primary:
                    bg = _isPressed ? DevToolsTheme.AccentPressed
                        : (_isHovered ? DevToolsTheme.AccentHover : DevToolsTheme.Accent);
                    border = bg;
                    fg = DevToolsTheme.TextPrimary;
                    break;

                case ButtonStyle.Danger:
                    // Solid red fill — used for "Stop" / destructive toggles.
                    var baseRed = DevToolsTheme.ErrorColor;
                    var hoverRed = BlendTowardWhite(baseRed, 0.15);
                    var pressRed = BlendTowardWhite(baseRed, -0.15);
                    bg = new SolidColorBrush(_isPressed ? pressRed : _isHovered ? hoverRed : baseRed);
                    border = bg;
                    fg = DevToolsTheme.TextPrimary;
                    break;

                default:
                    bg = _isActive
                        ? DevToolsTheme.AccentSoft
                        : (_isPressed ? DevToolsTheme.ControlPressed : (_isHovered ? DevToolsTheme.ControlHover : DevToolsTheme.Control));
                    border = _isActive ? DevToolsTheme.Accent : DevToolsTheme.Border;
                    fg = _isActive ? DevToolsTheme.Accent : DevToolsTheme.TextPrimary;
                    break;
            }

            Background = bg;
            BorderBrush = border;
            _labelText.Foreground = fg;
            if (_iconText != null) _iconText.Foreground = fg;
        }

        private static Color BlendTowardWhite(Color c, double amount)
        {
            // amount > 0 → lighter; < 0 → darker.
            double t = Math.Clamp(amount, -1.0, 1.0);
            byte target = t >= 0 ? (byte)255 : (byte)0;
            double mix = Math.Abs(t);
            return Color.FromRgb(
                (byte)(c.R + (target - c.R) * mix),
                (byte)(c.G + (target - c.G) * mix),
                (byte)(c.B + (target - c.B) * mix));
        }
    }

    public enum ButtonStyle
    {
        Default,
        Primary,
        Danger,
    }

    public static DevToolsButton Button(string label, Action onClick, ButtonStyle style = ButtonStyle.Default, string? icon = null)
    {
        return new DevToolsButton(label, onClick, style, icon);
    }

    public static DevToolsButton Toggle(string label, Action onClick, bool isActive, string? icon = null)
    {
        var btn = new DevToolsButton(label, onClick, ButtonStyle.Default, icon) { IsActive = isActive };
        return btn;
    }

    // ── Tables ───────────────────────────────────────────────────────────

    public static TextBlock GridCell(string text, int column, Brush? color = null, bool mono = false)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = DevToolsTheme.FontSm,
            FontFamily = mono ? DevToolsTheme.MonoFont : DevToolsTheme.UiFont,
            Foreground = color ?? DevToolsTheme.TextPrimary,
            Margin = new Thickness(DevToolsTheme.GutterBase, DevToolsTheme.GutterXS, DevToolsTheme.GutterBase, DevToolsTheme.GutterXS),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        Grid.SetColumn(tb, column);
        return tb;
    }

    public static Border GridRow(bool alt, UIElement content)
    {
        return new Border
        {
            Background = alt ? DevToolsTheme.RowAlt : null,
            Child = content,
        };
    }

    // ── Inputs ───────────────────────────────────────────────────────────

    public static TextBox TextInput(double width = double.NaN, string? placeholder = null)
    {
        var tb = new TextBox
        {
            FontSize = DevToolsTheme.FontSm,
            FontFamily = DevToolsTheme.UiFont,
            Foreground = DevToolsTheme.TextPrimary,
            Background = DevToolsTheme.Control,
            BorderBrush = DevToolsTheme.Border,
            BorderThickness = DevToolsTheme.ThicknessHairline,
            Padding = new Thickness(DevToolsTheme.GutterBase, DevToolsTheme.GutterSm - 1, DevToolsTheme.GutterBase, DevToolsTheme.GutterSm - 1),
        };
        if (!double.IsNaN(width)) tb.Width = width;
        if (placeholder != null) tb.PlaceholderText = placeholder;
        return tb;
    }

    // ── Divider / spacer ────────────────────────────────────────────────

    public static Border VerticalDivider(double height = 16)
    {
        return new Border
        {
            Width = 1,
            Height = height,
            Background = DevToolsTheme.BorderSubtle,
            Margin = new Thickness(DevToolsTheme.GutterSm, 0, DevToolsTheme.GutterSm, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
    }

    public static UIElement Spacer(double width = DevToolsTheme.GutterBase)
    {
        return new Border { Width = width };
    }

    // ── Segmented toggle (icon-only mode switcher) ───────────────────────

    /// <summary>
    /// A compact segmented control: one small square per option, with the active
    /// segment tinted and decorated with a bottom "LED" dot. Matches the view-mode
    /// switcher frequently seen in IDE / browser inspector toolbars.
    /// </summary>
    public sealed class SegmentedToggle : Border
    {
        private readonly List<Border> _segments = new();
        private readonly List<Action?> _callbacks = new();
        private int _selectedIndex = -1;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value == _selectedIndex) return;
                _selectedIndex = value;
                UpdateVisualState();
            }
        }

        /// <summary>Select an option without invoking its callback.</summary>
        public void SetSelectedSilent(int index)
        {
            _selectedIndex = index;
            UpdateVisualState();
        }

        public SegmentedToggle()
        {
            Background = DevToolsTheme.Control;
            BorderBrush = DevToolsTheme.BorderSubtle;
            BorderThickness = DevToolsTheme.ThicknessHairline;
            CornerRadius = DevToolsTheme.RadiusBase;
            Padding = new Thickness(2);
            Margin = new Thickness(DevToolsTheme.GutterSm, 0, DevToolsTheme.GutterSm, 0);
            VerticalAlignment = VerticalAlignment.Center;
            var strip = new StackPanel { Orientation = Orientation.Horizontal };
            Child = strip;
        }

        public SegmentedToggle AddSegment(string glyph, string? tooltip, Action onSelect)
        {
            if (Child is not StackPanel strip) return this;

            int index = _segments.Count;
            _callbacks.Add(onSelect);

            var iconText = new TextBlock
            {
                Text = glyph,
                FontSize = 13,
                FontFamily = DevToolsTheme.UiFont,
                Foreground = DevToolsTheme.TextSecondary,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            var dot = new Border
            {
                Width = 4,
                Height = 4,
                CornerRadius = new CornerRadius(2),
                Background = DevToolsTheme.Success,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 2),
                Visibility = Visibility.Collapsed,
            };
            var cell = new Grid();
            cell.Children.Add(iconText);
            cell.Children.Add(dot);

            var seg = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                CornerRadius = DevToolsTheme.RadiusSm,
                Width = 28,
                Height = 24,
                Child = cell,
                Cursor = Cursors.Hand,
            };
            if (tooltip != null)
                seg.SetValue(ToolTipService.ToolTipProperty, tooltip);

            seg.MouseEnter += (_, _) =>
            {
                if (index != _selectedIndex)
                    seg.Background = DevToolsTheme.ControlHover;
            };
            seg.MouseLeave += (_, _) =>
            {
                if (index != _selectedIndex)
                    seg.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            };
            seg.MouseDown += (_, _) =>
            {
                SelectedIndex = index;
                onSelect();
            };

            // Store the icon/dot on the segment so UpdateVisualState can tint them.
            seg.Tag = (iconText, dot);

            _segments.Add(seg);
            strip.Children.Add(seg);
            return this;
        }

        private void UpdateVisualState()
        {
            for (int i = 0; i < _segments.Count; i++)
            {
                var seg = _segments[i];
                var (iconText, dot) = ((TextBlock iconText, Border dot))seg.Tag!;
                bool active = i == _selectedIndex;
                seg.Background = active ? DevToolsTheme.AccentSoft : new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                iconText.Foreground = active ? DevToolsTheme.Accent : DevToolsTheme.TextSecondary;
                dot.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    public static SegmentedToggle Segmented(params (string Glyph, string? Tooltip, Action OnSelect)[] segments)
    {
        var toggle = new SegmentedToggle();
        foreach (var s in segments)
            toggle.AddSegment(s.Glyph, s.Tooltip, s.OnSelect);
        return toggle;
    }

    // ── Status pill (for small stateful badges) ─────────────────────────

    public static Border Pill(string text, Brush foreground, Brush? fill = null)
    {
        return new Border
        {
            Background = fill ?? new SolidColorBrush(Color.FromArgb(0x28,
                ((SolidColorBrush)foreground).Color.R,
                ((SolidColorBrush)foreground).Color.G,
                ((SolidColorBrush)foreground).Color.B)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(DevToolsTheme.GutterBase, 1, DevToolsTheme.GutterBase, 1),
            Child = new TextBlock
            {
                Text = text,
                FontSize = DevToolsTheme.FontXS,
                FontFamily = DevToolsTheme.UiFont,
                FontWeight = FontWeights.SemiBold,
                Foreground = foreground,
            },
            Margin = new Thickness(DevToolsTheme.GutterSm, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
    }
}
