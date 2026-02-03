namespace Jalium.UI.Controls;

/// <summary>
/// Specifies the editing modes of an <see cref="InkCanvas"/>.
/// </summary>
public enum InkCanvasEditingMode
{
    /// <summary>
    /// No editing interaction is available.
    /// </summary>
    None,

    /// <summary>
    /// The user can draw ink strokes.
    /// </summary>
    Ink,

    /// <summary>
    /// The user can select strokes by drawing a lasso around them.
    /// </summary>
    Select,

    /// <summary>
    /// The user can erase portions of strokes by touching them.
    /// </summary>
    EraseByPoint,

    /// <summary>
    /// The user can erase entire strokes by touching them.
    /// </summary>
    EraseByStroke
}
