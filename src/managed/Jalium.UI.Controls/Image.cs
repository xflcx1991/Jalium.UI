using Jalium.UI.Media;

namespace Jalium.UI.Controls;

/// <summary>
/// Represents a control that displays an image.
/// </summary>
public class Image : FrameworkElement
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the Source dependency property.
    /// </summary>
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(nameof(Source), typeof(ImageSource), typeof(Image),
            new PropertyMetadata(null, OnSourceChanged));

    /// <summary>
    /// Identifies the Stretch dependency property.
    /// </summary>
    public static readonly DependencyProperty StretchProperty =
        DependencyProperty.Register(nameof(Stretch), typeof(Stretch), typeof(Image),
            new PropertyMetadata(Stretch.Uniform, OnLayoutPropertyChanged));

    /// <summary>
    /// Identifies the StretchDirection dependency property.
    /// </summary>
    public static readonly DependencyProperty StretchDirectionProperty =
        DependencyProperty.Register(nameof(StretchDirection), typeof(StretchDirection), typeof(Image),
            new PropertyMetadata(StretchDirection.Both, OnLayoutPropertyChanged));

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the image source.
    /// </summary>
    public ImageSource? Source
    {
        get => (ImageSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>
    /// Gets or sets how the image is stretched to fill its allocated space.
    /// </summary>
    public Stretch Stretch
    {
        get => (Stretch)(GetValue(StretchProperty) ?? Stretch.Uniform);
        set => SetValue(StretchProperty, value);
    }

    /// <summary>
    /// Gets or sets the direction to stretch the image.
    /// </summary>
    public StretchDirection StretchDirection
    {
        get => (StretchDirection)(GetValue(StretchDirectionProperty) ?? StretchDirection.Both);
        set => SetValue(StretchDirectionProperty, value);
    }

    #endregion

    #region Layout

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Source == null)
            return Size.Empty;

        var imageSize = new Size(Source.Width, Source.Height);

        // Calculate the size based on stretch mode
        var size = CalculateStretchSize(imageSize, availableSize);

        return size;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        return finalSize;
    }

    private Size CalculateStretchSize(Size imageSize, Size availableSize)
    {
        if (imageSize.Width <= 0 || imageSize.Height <= 0)
            return Size.Empty;

        var width = imageSize.Width;
        var height = imageSize.Height;
        var maxWidth = availableSize.Width;
        var maxHeight = availableSize.Height;

        // Handle explicit width/height if set
        if (!double.IsNaN(Width) && Width > 0)
            width = Width;
        if (!double.IsNaN(Height) && Height > 0)
            height = Height;

        switch (Stretch)
        {
            case Stretch.None:
                // Use natural size
                break;

            case Stretch.Fill:
                // Fill the available space (may distort)
                if (!double.IsInfinity(maxWidth) && !double.IsNaN(Width))
                    width = maxWidth;
                if (!double.IsInfinity(maxHeight) && !double.IsNaN(Height))
                    height = maxHeight;
                break;

            case Stretch.Uniform:
                // Scale uniformly to fit within available space
                {
                    var scaleX = double.IsInfinity(maxWidth) ? double.MaxValue : maxWidth / imageSize.Width;
                    var scaleY = double.IsInfinity(maxHeight) ? double.MaxValue : maxHeight / imageSize.Height;
                    var scale = Math.Min(scaleX, scaleY);

                    scale = ApplyStretchDirection(scale);

                    width = imageSize.Width * scale;
                    height = imageSize.Height * scale;
                }
                break;

            case Stretch.UniformToFill:
                // Scale uniformly to fill available space (may clip)
                {
                    if (!double.IsInfinity(maxWidth) && !double.IsInfinity(maxHeight))
                    {
                        var scaleX = maxWidth / imageSize.Width;
                        var scaleY = maxHeight / imageSize.Height;
                        var scale = Math.Max(scaleX, scaleY);

                        scale = ApplyStretchDirection(scale);

                        width = imageSize.Width * scale;
                        height = imageSize.Height * scale;
                    }
                }
                break;
        }

        return new Size(width, height);
    }

    private double ApplyStretchDirection(double scale)
    {
        return StretchDirection switch
        {
            StretchDirection.UpOnly => Math.Max(1.0, scale),
            StretchDirection.DownOnly => Math.Min(1.0, scale),
            _ => scale
        };
    }

    #endregion

    #region Rendering

    /// <inheritdoc />
    protected override void OnRender(object drawingContext)
    {
        if (drawingContext is not DrawingContext dc || Source == null)
            return;

        var imageSize = new Size(Source.Width, Source.Height);
        if (imageSize.Width <= 0 || imageSize.Height <= 0)
            return;

        // Calculate destination rectangle
        var destRect = CalculateDestinationRect(imageSize, RenderSize);

        // Draw the image
        dc.DrawImage(Source, destRect);
    }

    private Rect CalculateDestinationRect(Size imageSize, Size renderSize)
    {
        var stretchedSize = CalculateStretchSize(imageSize, renderSize);

        // Center the image in the render area
        var x = (renderSize.Width - stretchedSize.Width) / 2;
        var y = (renderSize.Height - stretchedSize.Height) / 2;

        return new Rect(x, y, stretchedSize.Width, stretchedSize.Height);
    }

    #endregion

    #region Property Changed Callbacks

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Image image)
        {
            image.InvalidateMeasure();
            image.InvalidateVisual();
        }
    }

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Image image)
        {
            image.InvalidateMeasure();
        }
    }

    #endregion
}
