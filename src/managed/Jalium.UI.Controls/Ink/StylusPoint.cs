namespace Jalium.UI.Controls.Ink;

/// <summary>
/// Represents a single sampling point from a stylus or mouse input device.
/// </summary>
public struct StylusPoint : IEquatable<StylusPoint>
{
    /// <summary>
    /// The default pressure factor value.
    /// </summary>
    public const float DefaultPressure = 0.5f;

    private double _x;
    private double _y;
    private float _pressureFactor;

    /// <summary>
    /// Initializes a new instance of the <see cref="StylusPoint"/> struct.
    /// </summary>
    /// <param name="x">The x-coordinate of the point.</param>
    /// <param name="y">The y-coordinate of the point.</param>
    public StylusPoint(double x, double y)
    {
        _x = x;
        _y = y;
        _pressureFactor = DefaultPressure;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StylusPoint"/> struct.
    /// </summary>
    /// <param name="x">The x-coordinate of the point.</param>
    /// <param name="y">The y-coordinate of the point.</param>
    /// <param name="pressureFactor">The pressure factor (0.0 to 1.0).</param>
    public StylusPoint(double x, double y, float pressureFactor)
    {
        _x = x;
        _y = y;
        _pressureFactor = Math.Clamp(pressureFactor, 0f, 1f);
    }

    /// <summary>
    /// Gets or sets the x-coordinate of this point.
    /// </summary>
    public double X
    {
        readonly get => _x;
        set => _x = value;
    }

    /// <summary>
    /// Gets or sets the y-coordinate of this point.
    /// </summary>
    public double Y
    {
        readonly get => _y;
        set => _y = value;
    }

    /// <summary>
    /// Gets or sets the pressure factor of this point.
    /// </summary>
    /// <remarks>
    /// The pressure factor ranges from 0.0 to 1.0, where 0.5 is the default.
    /// </remarks>
    public float PressureFactor
    {
        readonly get => _pressureFactor;
        set => _pressureFactor = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Converts this <see cref="StylusPoint"/> to a <see cref="Point"/>.
    /// </summary>
    /// <returns>A <see cref="Point"/> with the same X and Y coordinates.</returns>
    public readonly Point ToPoint() => new(_x, _y);

    /// <summary>
    /// Implicitly converts a <see cref="StylusPoint"/> to a <see cref="Point"/>.
    /// </summary>
    public static implicit operator Point(StylusPoint sp) => sp.ToPoint();

    /// <summary>
    /// Creates a <see cref="StylusPoint"/> from a <see cref="Point"/> with default pressure.
    /// </summary>
    public static StylusPoint FromPoint(Point point) => new(point.X, point.Y);

    /// <inheritdoc/>
    public readonly bool Equals(StylusPoint other)
    {
        return _x.Equals(other._x) &&
               _y.Equals(other._y) &&
               _pressureFactor.Equals(other._pressureFactor);
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj)
    {
        return obj is StylusPoint other && Equals(other);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(_x, _y, _pressureFactor);
    }

    /// <summary>
    /// Determines whether two <see cref="StylusPoint"/> instances are equal.
    /// </summary>
    public static bool operator ==(StylusPoint left, StylusPoint right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two <see cref="StylusPoint"/> instances are not equal.
    /// </summary>
    public static bool operator !=(StylusPoint left, StylusPoint right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public override readonly string ToString()
    {
        return $"{_x},{_y},{_pressureFactor}";
    }
}
