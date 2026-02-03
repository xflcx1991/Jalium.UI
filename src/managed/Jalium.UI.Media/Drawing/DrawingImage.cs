namespace Jalium.UI.Media;

/// <summary>
/// An ImageSource that uses a Drawing for its content.
/// </summary>
public sealed class DrawingImage : ImageSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DrawingImage"/> class.
    /// </summary>
    public DrawingImage()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DrawingImage"/> class
    /// with the specified Drawing.
    /// </summary>
    /// <param name="drawing">The Drawing to use as the image content.</param>
    public DrawingImage(Drawing? drawing)
    {
        Drawing = drawing;
    }

    /// <summary>
    /// Gets or sets the Drawing that provides the content for this ImageSource.
    /// </summary>
    public Drawing? Drawing { get; set; }

    /// <summary>
    /// Gets the width of the DrawingImage.
    /// </summary>
    public override double Width => Drawing?.Bounds.Width ?? 0;

    /// <summary>
    /// Gets the height of the DrawingImage.
    /// </summary>
    public override double Height => Drawing?.Bounds.Height ?? 0;

    /// <summary>
    /// Gets the native handle. DrawingImage does not have a native handle.
    /// </summary>
    public override nint NativeHandle => 0;
}
