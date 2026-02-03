namespace Jalium.UI.Media;

/// <summary>
/// Provides options for controlling rendering behavior of visual objects.
/// </summary>
public static class RenderOptions
{
    /// <summary>
    /// Identifies the EdgeMode attached property.
    /// </summary>
    public static readonly DependencyProperty EdgeModeProperty =
        DependencyProperty.RegisterAttached(
            "EdgeMode",
            typeof(EdgeMode),
            typeof(RenderOptions),
            new PropertyMetadata(EdgeMode.Unspecified));

    /// <summary>
    /// Sets the edge mode for the specified dependency object.
    /// </summary>
    /// <param name="target">The dependency object to set the edge mode for.</param>
    /// <param name="edgeMode">The edge mode value.</param>
    public static void SetEdgeMode(DependencyObject target, EdgeMode edgeMode)
    {
        target.SetValue(EdgeModeProperty, edgeMode);
    }

    /// <summary>
    /// Gets the edge mode for the specified dependency object.
    /// </summary>
    /// <param name="target">The dependency object to get the edge mode from.</param>
    /// <returns>The edge mode value.</returns>
    public static EdgeMode GetEdgeMode(DependencyObject target)
    {
        return (EdgeMode)target.GetValue(EdgeModeProperty);
    }
}
