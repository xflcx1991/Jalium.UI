namespace Jalium.UI.Controls;

/// <summary>
/// Specifies the type of value that a <see cref="GridLength"/> holds.
/// </summary>
public enum GridUnitType
{
    /// <summary>
    /// The size is determined by the size properties of the content object.
    /// </summary>
    Auto,

    /// <summary>
    /// The value is expressed as a pixel.
    /// </summary>
    Pixel,

    /// <summary>
    /// The value is expressed as a weighted proportion of available space.
    /// </summary>
    Star
}

/// <summary>
/// Represents the length of elements that support star sizing and auto sizing.
/// </summary>
public readonly struct GridLength : IEquatable<GridLength>
{
    /// <summary>
    /// Gets the value of the grid length.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Gets the type of the grid length.
    /// </summary>
    public GridUnitType GridUnitType { get; }

    /// <summary>
    /// Gets a value indicating whether this is an auto-sized length.
    /// </summary>
    public bool IsAuto => GridUnitType == GridUnitType.Auto;

    /// <summary>
    /// Gets a value indicating whether this is a pixel-sized length.
    /// </summary>
    public bool IsAbsolute => GridUnitType == GridUnitType.Pixel;

    /// <summary>
    /// Gets a value indicating whether this is a star-sized length.
    /// </summary>
    public bool IsStar => GridUnitType == GridUnitType.Star;

    /// <summary>
    /// Initializes a new instance of the <see cref="GridLength"/> struct with an absolute value.
    /// </summary>
    /// <param name="pixels">The size in pixels.</param>
    public GridLength(double pixels)
        : this(pixels, GridUnitType.Pixel)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GridLength"/> struct.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="type">The type of the value.</param>
    public GridLength(double value, GridUnitType type)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentException("Value cannot be NaN or Infinity.", nameof(value));
        }

        if (value < 0 && type != GridUnitType.Auto)
        {
            throw new ArgumentException("Value cannot be negative.", nameof(value));
        }

        Value = type == GridUnitType.Auto ? 1.0 : value;
        GridUnitType = type;
    }

    /// <summary>
    /// Gets an auto-sized <see cref="GridLength"/>.
    /// </summary>
    public static GridLength Auto => new(1.0, GridUnitType.Auto);

    /// <summary>
    /// Creates a star-sized <see cref="GridLength"/> with value 1.
    /// </summary>
    public static GridLength Star => new(1.0, GridUnitType.Star);

    /// <summary>
    /// Creates a star-sized <see cref="GridLength"/> with the specified value.
    /// </summary>
    /// <param name="value">The star value (weight).</param>
    /// <returns>A star-sized grid length.</returns>
    public static GridLength FromStar(double value) => new(value, GridUnitType.Star);

    /// <summary>
    /// Creates a pixel-sized <see cref="GridLength"/>.
    /// </summary>
    /// <param name="pixels">The size in pixels.</param>
    /// <returns>A pixel-sized grid length.</returns>
    public static GridLength FromPixels(double pixels) => new(pixels, GridUnitType.Pixel);

    /// <inheritdoc />
    public bool Equals(GridLength other) =>
        Value == other.Value && GridUnitType == other.GridUnitType;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is GridLength other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(Value, GridUnitType);

    /// <inheritdoc />
    public override string ToString()
    {
        return GridUnitType switch
        {
            GridUnitType.Auto => "Auto",
            GridUnitType.Star => Value == 1.0 ? "*" : $"{Value}*",
            _ => Value.ToString()
        };
    }

    public static bool operator ==(GridLength left, GridLength right) => left.Equals(right);
    public static bool operator !=(GridLength left, GridLength right) => !left.Equals(right);
}
