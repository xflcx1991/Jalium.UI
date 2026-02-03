namespace Jalium.UI;

/// <summary>
/// Interface for drawing contexts that support offset-based positioning.
/// </summary>
public interface IOffsetDrawingContext
{
    /// <summary>
    /// Gets or sets the current drawing offset.
    /// </summary>
    Point Offset { get; set; }
}

/// <summary>
/// Interface for drawing contexts that support clipping.
/// </summary>
public interface IClipDrawingContext
{
    /// <summary>
    /// Pushes a clip region onto the clip stack.
    /// </summary>
    /// <param name="clipGeometry">The clipping geometry (Media.Geometry).</param>
    void PushClip(object clipGeometry);

    /// <summary>
    /// Pops the most recent clip from the clip stack.
    /// </summary>
    void Pop();
}

/// <summary>
/// Interface for drawing contexts that support opacity.
/// </summary>
public interface IOpacityDrawingContext
{
    /// <summary>
    /// Pushes an opacity value onto the opacity stack.
    /// </summary>
    /// <param name="opacity">The opacity (0.0 - 1.0).</param>
    void PushOpacity(double opacity);

    /// <summary>
    /// Pops the most recent opacity from the opacity stack.
    /// </summary>
    void PopOpacity();
}
