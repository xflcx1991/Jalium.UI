namespace Jalium.UI;

/// <summary>
/// Specifies the timing function used by automatic property transitions.
/// </summary>
public enum TransitionTimingFunction
{
    /// <summary>
    /// Uses the framework's recommended easing curve.
    /// </summary>
    Recommended,

    /// <summary>
    /// Uses a linear progression.
    /// </summary>
    Linear,

    /// <summary>
    /// Uses an ease-in progression.
    /// </summary>
    EaseIn,

    /// <summary>
    /// Uses an ease-out progression.
    /// </summary>
    EaseOut,

    /// <summary>
    /// Uses an ease-in-out progression.
    /// </summary>
    EaseInOut
}
