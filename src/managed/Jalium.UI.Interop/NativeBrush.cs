namespace Jalium.UI.Interop;

/// <summary>
/// Represents a native brush for painting.
/// </summary>
public sealed class NativeBrush : IDisposable
{
    private nint _handle;
    private bool _disposed;

    /// <summary>
    /// Gets the native handle.
    /// </summary>
    public nint Handle => _handle;

    /// <summary>
    /// Gets whether the brush is valid.
    /// </summary>
    public bool IsValid => _handle != nint.Zero && !_disposed;

    /// <summary>
    /// Gets the color components.
    /// </summary>
    public float R { get; }
    public float G { get; }
    public float B { get; }
    public float A { get; }

    internal NativeBrush(RenderContext context, float r, float g, float b, float a)
    {
        R = r;
        G = g;
        B = b;
        A = a;

        _handle = NativeMethods.BrushCreateSolid(context.Handle, r, g, b, a);
        if (_handle == nint.Zero)
        {
            throw new InvalidOperationException("Failed to create brush");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_handle != nint.Zero)
        {
            NativeMethods.BrushDestroy(_handle);
            _handle = nint.Zero;
        }

        GC.SuppressFinalize(this);
    }

    ~NativeBrush()
    {
        Dispose();
    }
}
