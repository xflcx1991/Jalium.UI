using Jalium.UI;

namespace Jalium.UI.Media.Animation;

/// <summary>
/// Factory for creating animations for common property types.
/// </summary>
public static class AnimationFactory
{
    /// <summary>
    /// Creates an animation for the specified property type.
    /// </summary>
    /// <param name="propertyType">The type of property to animate.</param>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The ending value.</param>
    /// <param name="duration">The animation duration.</param>
    /// <returns>An animation timeline, or null if the type is not supported.</returns>
    public static IAnimationTimeline? CreateAnimation(Type propertyType, object? from, object? to, TimeSpan duration)
    {
        return CreateAnimation(propertyType, from, to, duration, null);
    }

    /// <summary>
    /// Creates an animation for the specified property type with an optional easing function.
    /// </summary>
    /// <param name="propertyType">The type of property to animate.</param>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The ending value.</param>
    /// <param name="duration">The animation duration.</param>
    /// <param name="easingFunction">Optional easing function.</param>
    /// <returns>An animation timeline, or null if the type is not supported.</returns>
    public static IAnimationTimeline? CreateAnimation(Type propertyType, object? from, object? to, TimeSpan duration, IEasingFunction? easingFunction)
    {
        if (propertyType == typeof(double))
        {
            return new DoubleAnimation
            {
                From = from as double?,
                To = to as double?,
                Duration = new Duration(duration),
                EasingFunction = easingFunction
            };
        }

        if (propertyType == typeof(Color))
        {
            return new ColorAnimation
            {
                From = from as Color?,
                To = to as Color?,
                Duration = new Duration(duration),
                EasingFunction = easingFunction
            };
        }

        if (propertyType == typeof(Thickness))
        {
            return new ThicknessAnimation
            {
                From = from as Thickness?,
                To = to as Thickness?,
                Duration = new Duration(duration),
                EasingFunction = easingFunction
            };
        }

        if (propertyType == typeof(Point))
        {
            return new PointAnimation
            {
                From = from as Point?,
                To = to as Point?,
                Duration = new Duration(duration),
                EasingFunction = easingFunction
            };
        }

        // Not a type we can animate
        return null;
    }

    /// <summary>
    /// Gets a default animation factory delegate that can be used with VisualTransition.
    /// </summary>
    public static Func<Type, object?, object?, TimeSpan, IAnimationTimeline?> DefaultFactory { get; } =
        (propertyType, from, to, duration) => CreateAnimation(propertyType, from, to, duration);

    /// <summary>
    /// Gets an animation factory delegate with the specified easing function.
    /// </summary>
    /// <param name="easingFunction">The easing function to use.</param>
    /// <returns>An animation factory delegate.</returns>
    public static Func<Type, object?, object?, TimeSpan, IAnimationTimeline?> WithEasing(IEasingFunction easingFunction)
    {
        return (propertyType, from, to, duration) => CreateAnimation(propertyType, from, to, duration, easingFunction);
    }
}
