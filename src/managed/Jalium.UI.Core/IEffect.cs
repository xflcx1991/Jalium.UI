namespace Jalium.UI;

/// <summary>
/// Interface for effects that can be applied to UI elements.
/// Implement this interface to create custom element effects.
/// This is different from IBackdropEffect which affects content behind an element.
/// </summary>
public interface IEffect
{
    /// <summary>
    /// Gets a value indicating whether the effect produces any visual change.
    /// </summary>
    bool HasEffect { get; }

    /// <summary>
    /// Gets the effect type identifier for native rendering dispatch.
    /// </summary>
    int EffectTypeId { get; }

    /// <summary>
    /// Gets the padding that this effect adds around the element bounds.
    /// This is used to expand the render area to accommodate the effect.
    /// </summary>
    Thickness EffectPadding { get; }

    /// <summary>
    /// Occurs when a property changes that affects the visual output of the effect.
    /// </summary>
    event EventHandler? EffectChanged;
}

/// <summary>
/// Standard effect type identifiers.
/// </summary>
public static class EffectTypeIds
{
    /// <summary>
    /// No effect.
    /// </summary>
    public const int None = 0;

    /// <summary>
    /// Drop shadow effect.
    /// </summary>
    public const int DropShadow = 1;

    /// <summary>
    /// Blur effect applied to the element itself.
    /// </summary>
    public const int Blur = 2;

    /// <summary>
    /// Custom shader effect.
    /// </summary>
    public const int Shader = 3;
}
