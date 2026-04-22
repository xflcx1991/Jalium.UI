using Jalium.UI.Media;

namespace Jalium.UI.Controls.DevTools;

public partial class DevToolsWindow
{
    private static readonly SolidColorBrush BrushSourceLocal = new(Color.FromRgb(150, 220, 255));
    private static readonly SolidColorBrush BrushSourceStyle = new(Color.FromRgb(255, 200, 120));
    private static readonly SolidColorBrush BrushSourceTemplate = new(Color.FromRgb(180, 220, 160));
    private static readonly SolidColorBrush BrushSourceInherited = new(Color.FromRgb(200, 180, 240));
    private static readonly SolidColorBrush BrushSourceDefault = new(Color.FromRgb(130, 130, 140));
    private static readonly SolidColorBrush BrushSourceAnimated = new(Color.FromRgb(255, 140, 200));

    private void AppendValueSourceBadge(DependencyObject target, DependencyProperty property)
    {
        try
        {
            if (_propertiesPanel.Children.Count == 0) return;
            var last = _propertiesPanel.Children[_propertiesPanel.Children.Count - 1];
            if (last is not StackPanel row) return;

            var source = DependencyPropertyHelper.GetValueSource(target, property);
            string labelText = source.BaseValueSource.ToString();

            Brush brush = source.BaseValueSource switch
            {
                BaseValueSource.Local => BrushSourceLocal,
                BaseValueSource.Style => BrushSourceStyle,
                BaseValueSource.DefaultStyle => BrushSourceStyle,
                BaseValueSource.StyleTrigger => BrushSourceStyle,
                BaseValueSource.DefaultStyleTrigger => BrushSourceStyle,
                BaseValueSource.ImplicitStyleReference => BrushSourceStyle,
                BaseValueSource.ParentTemplate => BrushSourceTemplate,
                BaseValueSource.ParentTemplateTrigger => BrushSourceTemplate,
                BaseValueSource.TemplateTrigger => BrushSourceTemplate,
                BaseValueSource.Inherited => BrushSourceInherited,
                BaseValueSource.Default => BrushSourceDefault,
                _ => BrushSourceDefault,
            };

            var sb = new System.Text.StringBuilder();
            sb.Append("  · ").Append(labelText);
            if (source.IsAnimated) sb.Append(" · anim");
            if (source.IsExpression) sb.Append(" · bound");
            if (source.IsCoerced) sb.Append(" · coerced");

            var badge = new TextBlock
            {
                Text = sb.ToString(),
                FontSize = 9,
                Foreground = source.IsAnimated ? BrushSourceAnimated : brush,
                Margin = new Thickness(4, 2, 0, 0),
            };
            row.Children.Add(badge);
        }
        catch
        {
            // ValueSource is diagnostic-only — never fail the inspector if lookup errors.
        }
    }
}
