using Jalium.UI;

namespace Jalium.UI.Input;

/// <summary>
/// Base class for all input event arguments.
/// </summary>
public class InputEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public int Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InputEventArgs"/> class.
    /// </summary>
    public InputEventArgs()
    {
        Timestamp = Environment.TickCount;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InputEventArgs"/> class with a timestamp.
    /// </summary>
    /// <param name="timestamp">The event timestamp.</param>
    public InputEventArgs(int timestamp)
    {
        Timestamp = timestamp;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InputEventArgs"/> class.
    /// </summary>
    /// <param name="routedEvent">The routed event.</param>
    /// <param name="timestamp">The event timestamp.</param>
    public InputEventArgs(RoutedEvent routedEvent, int timestamp) : base(routedEvent)
    {
        Timestamp = timestamp;
    }
}
