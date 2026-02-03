namespace Jalium.UI;

/// <summary>
/// Specifies how new animations interact with existing animations on a property.
/// </summary>
public enum HandoffBehavior
{
    /// <summary>
    /// New animations replace existing animations on the property.
    /// The current animated value is captured as the starting point for the new animation.
    /// </summary>
    SnapshotAndReplace,

    /// <summary>
    /// New animations are combined with existing animations.
    /// Note: Currently behaves the same as SnapshotAndReplace.
    /// </summary>
    Compose
}
