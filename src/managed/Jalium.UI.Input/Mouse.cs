namespace Jalium.UI.Input;

/// <summary>
/// Represents the mouse device.
/// </summary>
public static class Mouse
{
    private static Point _lastPosition;

    /// <summary>
    /// Gets the element that has captured the mouse, if any.
    /// </summary>
    public static UIElement? Captured => UIElement.MouseCapturedElement;

    /// <summary>
    /// Gets or sets the last known mouse position in window coordinates.
    /// </summary>
    public static Point Position
    {
        get => _lastPosition;
        internal set => _lastPosition = value;
    }

    /// <summary>
    /// Captures the mouse to the specified element.
    /// </summary>
    /// <param name="element">The element to capture the mouse to.</param>
    /// <returns>True if capture was successful; otherwise, false.</returns>
    public static bool Capture(UIElement? element)
    {
        if (element == null)
        {
            // Release capture
            UIElement.ForceReleaseMouseCapture();
            return true;
        }

        return element.CaptureMouse();
    }

    /// <summary>
    /// Gets the element that should receive mouse events at the current position.
    /// If an element has capture, that element receives all mouse events.
    /// </summary>
    /// <param name="hitTestResult">The result of hit testing at the mouse position.</param>
    /// <returns>The element that should receive mouse events.</returns>
    public static UIElement? GetMouseTarget(UIElement? hitTestResult)
    {
        // If an element has capture, it receives all mouse events
        var captured = UIElement.MouseCapturedElement;
        if (captured != null)
        {
            return captured;
        }

        return hitTestResult;
    }

    /// <summary>
    /// Called when the mouse leaves the window entirely.
    /// </summary>
    internal static void OnMouseLeaveWindow()
    {
        // Don't release capture when mouse leaves window - capture should persist
        // This allows drag operations to continue even when mouse moves outside window
    }

    /// <summary>
    /// Forces release of mouse capture. Called when window loses focus.
    /// </summary>
    internal static void ForceReleaseCapture()
    {
        UIElement.ForceReleaseMouseCapture();
    }
}
