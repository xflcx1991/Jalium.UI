namespace Jalium.UI.Input;

/// <summary>
/// Abstract base class that describes an input device gesture (such as a key combination or mouse action).
/// </summary>
public abstract class InputGesture
{
    /// <summary>
    /// When overridden in a derived class, determines whether the specified InputEventArgs matches this gesture.
    /// </summary>
    /// <param name="targetElement">The target of the command.</param>
    /// <param name="inputEventArgs">The input event data to compare this gesture to.</param>
    /// <returns>true if the event data matches this InputGesture; otherwise, false.</returns>
    public abstract bool Matches(object targetElement, InputEventArgs inputEventArgs);
}

/// <summary>
/// Provides data for input-related events.
/// </summary>
public class InputEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Initializes a new instance of the InputEventArgs class.
    /// </summary>
    public InputEventArgs()
    {
    }

    /// <summary>
    /// Initializes a new instance of the InputEventArgs class with the specified routed event.
    /// </summary>
    /// <param name="routedEvent">The routed event identifier.</param>
    public InputEventArgs(RoutedEvent routedEvent) : base(routedEvent)
    {
    }

    /// <summary>
    /// Initializes a new instance of the InputEventArgs class with the specified routed event and source.
    /// </summary>
    /// <param name="routedEvent">The routed event identifier.</param>
    /// <param name="source">The source of the event.</param>
    public InputEventArgs(RoutedEvent routedEvent, object source) : base(routedEvent, source)
    {
    }

    /// <summary>
    /// Gets the timestamp of this input event.
    /// </summary>
    public int Timestamp { get; init; } = Environment.TickCount;
}
