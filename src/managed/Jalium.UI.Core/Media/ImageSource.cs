namespace Jalium.UI.Media;

/// <summary>
/// Represents the source of an image.
/// </summary>
public abstract class ImageSource
{
    /// <summary>
    /// Gets the width of the image in pixels.
    /// </summary>
    public abstract double Width { get; }

    /// <summary>
    /// Gets the height of the image in pixels.
    /// </summary>
    public abstract double Height { get; }

    /// <summary>
    /// Gets the native handle of the image (platform-specific).
    /// </summary>
    public abstract nint NativeHandle { get; }

    /// <summary>
    /// Raised when an image source wants every GPU-side bitmap cache (one per
    /// active <c>RenderTargetDrawingContext</c>) to drop its cached upload of
    /// the source so the underlying <c>NativeBitmap</c> texture is released.
    /// Used by the idle-resource reclaimer when an <c>IReclaimableResource</c>
    /// element decides its source has been off-screen long enough to free GPU
    /// memory; the upload is rebuilt from the bitmap's raw or encoded data on
    /// the next render.
    /// </summary>
    /// <remarks>
    /// Each <c>RenderTargetDrawingContext</c> subscribes in its constructor and
    /// unsubscribes when the context closes. Handlers run synchronously on the
    /// thread that raised the event — typically the UI thread — and must be
    /// allocation-free; the source is the <see cref="ImageSource"/> whose GPU
    /// upload should be dropped.
    /// </remarks>
    internal static event Action<ImageSource>? GpuCacheEvictionRequested;

    /// <summary>
    /// Asks every subscribed bitmap cache to drop its GPU upload of
    /// <paramref name="source"/>. No-op when nothing is subscribed.
    /// </summary>
    internal static void RaiseGpuCacheEviction(ImageSource source)
    {
        var handler = GpuCacheEvictionRequested;
        if (handler != null)
        {
            handler(source);
        }
    }
}
