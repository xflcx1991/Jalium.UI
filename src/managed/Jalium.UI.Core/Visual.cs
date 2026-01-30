namespace Jalium.UI;

/// <summary>
/// Represents a node in the visual tree.
/// This is the base class for all renderable objects.
/// </summary>
public abstract class Visual : DependencyObject
{
    private Visual? _parent;
    private readonly List<Visual> _children = new();

    /// <summary>
    /// Gets the parent visual.
    /// </summary>
    public Visual? VisualParent => _parent;

    /// <summary>
    /// Gets the number of child visuals.
    /// </summary>
    public virtual int VisualChildrenCount => _children.Count;

    /// <summary>
    /// Gets a child visual by index.
    /// </summary>
    /// <param name="index">The index of the child.</param>
    /// <returns>The child visual.</returns>
    public virtual Visual? GetVisualChild(int index)
    {
        if (index < 0 || index >= _children.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _children[index];
    }

    /// <summary>
    /// Adds a child visual.
    /// </summary>
    /// <param name="child">The child to add.</param>
    protected void AddVisualChild(Visual child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (child._parent != null)
        {
            throw new InvalidOperationException("Visual already has a parent.");
        }

        var oldParent = child._parent;
        child._parent = this;
        _children.Add(child);

        OnVisualChildrenChanged(child, null);
        child.OnVisualParentChanged(oldParent);
    }

    /// <summary>
    /// Removes a child visual.
    /// </summary>
    /// <param name="child">The child to remove.</param>
    protected void RemoveVisualChild(Visual child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (child._parent != this)
        {
            return;
        }

        var oldParent = child._parent;
        child._parent = null;
        _children.Remove(child);

        OnVisualChildrenChanged(null, child);
        child.OnVisualParentChanged(oldParent);
    }

    /// <summary>
    /// Called when the visual parent changes.
    /// </summary>
    /// <param name="oldParent">The previous parent visual, or null.</param>
    protected virtual void OnVisualParentChanged(Visual? oldParent)
    {
    }

    /// <summary>
    /// Called when visual children change.
    /// </summary>
    /// <param name="visualAdded">The child that was added, if any.</param>
    /// <param name="visualRemoved">The child that was removed, if any.</param>
    protected virtual void OnVisualChildrenChanged(Visual? visualAdded, Visual? visualRemoved)
    {
    }

    /// <summary>
    /// Performs hit testing at the specified point.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <returns>The hit test result, or null if nothing was hit.</returns>
    protected virtual HitTestResult? HitTestCore(Point point)
    {
        return null;
    }

    /// <summary>
    /// Performs rendering using the specified drawing context.
    /// </summary>
    /// <param name="drawingContext">The drawing context (should be cast to DrawingContext from Media).</param>
    public void Render(object drawingContext)
    {
        OnRender(drawingContext);

        // Check if clipping should be applied before rendering children
        object? clipGeometry = null;
        if (this is UIElement thisElement)
        {
            clipGeometry = thisElement.GetLayoutClip();
        }

        // Push clip if available (clipGeometry should be Media.Geometry)
        bool pushedClip = false;
        if (clipGeometry != null && drawingContext is IClipDrawingContext clipContext)
        {
            clipContext.PushClip(clipGeometry);
            pushedClip = true;
        }

        var childCount = VisualChildrenCount;
        for (int i = 0; i < childCount; i++)
        {
            var child = GetVisualChild(i);
            if (child == null) continue;

            // Skip collapsed children
            if (child is UIElement uiElement && uiElement.Visibility == Visibility.Collapsed)
                continue;

            // Get child's visual bounds and update drawing context offset
            if (child is UIElement uiChild && drawingContext is IOffsetDrawingContext offsetContext)
            {
                var bounds = uiChild.VisualBounds;
                var savedOffset = offsetContext.Offset;
                offsetContext.Offset = new Point(savedOffset.X + bounds.X, savedOffset.Y + bounds.Y);

                child.Render(drawingContext);

                offsetContext.Offset = savedOffset;
            }
            else
            {
                child.Render(drawingContext);
            }
        }

        // Pop clip if it was pushed
        if (pushedClip && drawingContext is IClipDrawingContext clipContext2)
        {
            clipContext2.Pop();
        }

        // Call post-render for overlays (like scrollbars)
        OnPostRender(drawingContext);
    }

    /// <summary>
    /// Override to provide custom rendering.
    /// </summary>
    /// <param name="drawingContext">The drawing context (should be cast to DrawingContext from Media).</param>
    protected virtual void OnRender(object drawingContext)
    {
    }

    /// <summary>
    /// Override to render content after children (useful for overlays like scrollbars).
    /// </summary>
    /// <param name="drawingContext">The drawing context.</param>
    protected virtual void OnPostRender(object drawingContext)
    {
    }
}

/// <summary>
/// Result of a hit test operation.
/// </summary>
public class HitTestResult
{
    // Reusable instance to avoid allocations on every mouse move
    [ThreadStatic]
    private static HitTestResult? _reusable;

    /// <summary>
    /// Gets the visual that was hit.
    /// </summary>
    public Visual VisualHit { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HitTestResult"/> class.
    /// </summary>
    /// <param name="visualHit">The visual that was hit.</param>
    public HitTestResult(Visual visualHit)
    {
        VisualHit = visualHit;
    }

    /// <summary>
    /// Gets a reusable HitTestResult instance to avoid allocations.
    /// </summary>
    /// <param name="visualHit">The visual that was hit.</param>
    /// <returns>A HitTestResult instance.</returns>
    internal static HitTestResult GetReusable(Visual visualHit)
    {
        _reusable ??= new HitTestResult(visualHit);
        _reusable.VisualHit = visualHit;
        return _reusable;
    }
}
