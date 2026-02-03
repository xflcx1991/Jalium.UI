namespace Jalium.UI.Interactivity;

/// <summary>
/// Encapsulates state information and zero or more ICommands into an attachable object.
/// </summary>
public abstract class Behavior : DependencyObject, IAttachedObject
{
    private DependencyObject? _associatedObject;

    /// <summary>
    /// Gets the DependencyObject to which this behavior is attached.
    /// </summary>
    public DependencyObject? AssociatedObject => _associatedObject;

    /// <summary>
    /// Gets the type constraint for the associated object.
    /// </summary>
    protected virtual Type AssociatedObjectTypeConstraint => typeof(DependencyObject);

    /// <summary>
    /// Attaches to the specified object.
    /// </summary>
    /// <param name="dependencyObject">The DependencyObject to attach to.</param>
    public void Attach(DependencyObject dependencyObject)
    {
        if (dependencyObject == _associatedObject)
            return;

        if (_associatedObject != null)
            throw new InvalidOperationException("Cannot host behavior multiple times.");

        if (dependencyObject != null && !AssociatedObjectTypeConstraint.IsAssignableFrom(dependencyObject.GetType()))
            throw new InvalidOperationException($"Object is not of type {AssociatedObjectTypeConstraint.Name}.");

        _associatedObject = dependencyObject;
        OnAttached();
    }

    /// <summary>
    /// Detaches this instance from its associated object.
    /// </summary>
    public void Detach()
    {
        OnDetaching();
        _associatedObject = null;
    }

    /// <summary>
    /// Called after the behavior is attached to an AssociatedObject.
    /// </summary>
    protected virtual void OnAttached()
    {
    }

    /// <summary>
    /// Called when the behavior is being detached from its AssociatedObject, but before it has actually occurred.
    /// </summary>
    protected virtual void OnDetaching()
    {
    }
}

/// <summary>
/// Encapsulates state information and zero or more ICommands into an attachable object.
/// </summary>
/// <typeparam name="T">The type of object this behavior can be attached to.</typeparam>
public abstract class Behavior<T> : Behavior where T : DependencyObject
{
    /// <summary>
    /// Gets the object to which this behavior is attached.
    /// </summary>
    public new T? AssociatedObject => (T?)base.AssociatedObject;

    /// <summary>
    /// Gets the type constraint for the associated object.
    /// </summary>
    protected override Type AssociatedObjectTypeConstraint => typeof(T);
}
