namespace Jalium.UI;

/// <summary>
/// Represents the fill behavior for an animation timeline.
/// Mirrors FillBehavior from Jalium.UI.Media.Animation.
/// </summary>
public enum AnimationFillBehavior
{
    /// <summary>
    /// The timeline holds its final value after its active period ends.
    /// </summary>
    HoldEnd,

    /// <summary>
    /// The timeline stops when its active period ends and the property reverts to its base value.
    /// </summary>
    Stop
}

/// <summary>
/// Interface for animation timelines. Implemented by AnimationTimeline in Jalium.UI.Media.
/// </summary>
public interface IAnimationTimeline
{
    /// <summary>
    /// Gets the type of value that this animation produces.
    /// </summary>
    Type TargetPropertyType { get; }

    /// <summary>
    /// Gets the fill behavior for this animation.
    /// </summary>
    AnimationFillBehavior AnimationFillBehavior { get; }

    /// <summary>
    /// Gets the current animated value.
    /// </summary>
    /// <param name="defaultOriginValue">The default origin value.</param>
    /// <param name="defaultDestinationValue">The default destination value.</param>
    /// <param name="clock">The animation clock.</param>
    /// <returns>The current animated value.</returns>
    object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, IAnimationClock clock);

    /// <summary>
    /// Creates a clock for this timeline.
    /// </summary>
    /// <returns>A new animation clock.</returns>
    IAnimationClock CreateClock();
}

/// <summary>
/// Interface for animation clocks. Implemented by AnimationClock in Jalium.UI.Media.
/// </summary>
public interface IAnimationClock
{
    /// <summary>
    /// Gets the current progress of the animation (0.0 to 1.0).
    /// </summary>
    double CurrentProgress { get; }

    /// <summary>
    /// Gets whether this clock is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets the timeline associated with this clock.
    /// </summary>
    IAnimationTimeline? Timeline { get; }

    /// <summary>
    /// Occurs when the animation completes.
    /// </summary>
    event EventHandler? Completed;

    /// <summary>
    /// Starts the animation.
    /// </summary>
    void Begin();

    /// <summary>
    /// Stops the animation.
    /// </summary>
    void Stop();

    /// <summary>
    /// Pauses the animation.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes a paused animation.
    /// </summary>
    void Resume();

    /// <summary>
    /// Updates the animation progress. Called each frame.
    /// </summary>
    void Tick();
}
