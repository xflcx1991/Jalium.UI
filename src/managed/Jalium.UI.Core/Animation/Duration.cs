namespace Jalium.UI.Media.Animation;

/// <summary>
/// Represents a duration of time.
/// </summary>
public readonly struct Duration : IEquatable<Duration>
{
    private readonly TimeSpan _timeSpan;
    private readonly DurationType _durationType;

    /// <summary>
    /// Creates a duration from a TimeSpan.
    /// </summary>
    public Duration(TimeSpan timeSpan)
    {
        _timeSpan = timeSpan;
        _durationType = DurationType.TimeSpan;
    }

    /// <summary>
    /// Gets the TimeSpan value of this duration.
    /// </summary>
    public TimeSpan TimeSpan => _timeSpan;

    /// <summary>
    /// Gets whether this duration has a TimeSpan value.
    /// </summary>
    public bool HasTimeSpan => _durationType == DurationType.TimeSpan;

    /// <summary>
    /// Gets an automatic duration.
    /// </summary>
    public static Duration Automatic { get; } = new(DurationType.Automatic);

    /// <summary>
    /// Gets a forever duration.
    /// </summary>
    public static Duration Forever { get; } = new(DurationType.Forever);

    private Duration(DurationType durationType)
    {
        _timeSpan = TimeSpan.Zero;
        _durationType = durationType;
    }

    /// <summary>
    /// Implicitly converts a TimeSpan to a Duration.
    /// </summary>
    public static implicit operator Duration(TimeSpan timeSpan) => new(timeSpan);

    /// <inheritdoc />
    public bool Equals(Duration other) =>
        _durationType == other._durationType && _timeSpan == other._timeSpan;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is Duration other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(_durationType, _timeSpan);

    public static bool operator ==(Duration left, Duration right) => left.Equals(right);
    public static bool operator !=(Duration left, Duration right) => !left.Equals(right);

    private enum DurationType
    {
        Automatic,
        TimeSpan,
        Forever
    }
}
