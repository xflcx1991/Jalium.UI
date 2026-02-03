namespace Jalium.UI.Interactivity;

/// <summary>
/// An interface for an object that can be attached to another object.
/// </summary>
public interface IAttachedObject
{
    /// <summary>
    /// Gets the DependencyObject to which this object is attached.
    /// </summary>
    DependencyObject? AssociatedObject { get; }

    /// <summary>
    /// Attaches to the specified object.
    /// </summary>
    /// <param name="dependencyObject">The DependencyObject to attach to.</param>
    void Attach(DependencyObject dependencyObject);

    /// <summary>
    /// Detaches this instance from its associated object.
    /// </summary>
    void Detach();
}
