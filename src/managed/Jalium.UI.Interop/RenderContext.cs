namespace Jalium.UI.Interop;

/// <summary>
/// Represents a native rendering context.
/// </summary>
public sealed class RenderContext : IDisposable
{
    private static RenderContext? _current;
    private nint _handle;
    private bool _disposed;

    /// <summary>
    /// Gets the current render context.
    /// </summary>
    public static RenderContext? Current => _current;

    /// <summary>
    /// Gets the native handle.
    /// </summary>
    public nint Handle => _handle;

    /// <summary>
    /// Gets the active backend type.
    /// </summary>
    public RenderBackend Backend { get; }

    /// <summary>
    /// Gets whether the context is valid.
    /// </summary>
    public bool IsValid => _handle != nint.Zero && !_disposed;

    /// <summary>
    /// Creates a new render context with the specified backend.
    /// </summary>
    /// <param name="backend">The rendering backend to use.</param>
    public RenderContext(RenderBackend backend = RenderBackend.Auto)
    {
        // Check if backend is available before trying to create context
        if (backend != RenderBackend.Auto && NativeMethods.IsBackendAvailable(backend) == 0)
        {
            System.Diagnostics.Debug.WriteLine($"Backend {backend} is not available. Check if native DLL was loaded and initialized correctly.");
            throw new InvalidOperationException($"Rendering backend {backend} is not available. Make sure the native DLL is properly loaded.");
        }

        _handle = NativeMethods.ContextCreate(backend);
        if (_handle == nint.Zero)
        {
            // Check if any backend is available
            bool d3d12Available = NativeMethods.IsBackendAvailable(RenderBackend.D3D12) != 0;
            bool d3d11Available = NativeMethods.IsBackendAvailable(RenderBackend.D3D11) != 0;
            System.Diagnostics.Debug.WriteLine($"Context creation failed. D3D12 available: {d3d12Available}, D3D11 available: {d3d11Available}");
            throw new InvalidOperationException($"Failed to create render context with backend {backend}. No rendering backends are available.");
        }

        Backend = NativeMethods.ContextGetBackend(_handle);
        _current ??= this;
        System.Diagnostics.Debug.WriteLine($"RenderContext created successfully with backend: {Backend}");
    }

    /// <summary>
    /// Creates a render target for a window handle.
    /// </summary>
    /// <param name="hwnd">The window handle.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <returns>A new render target.</returns>
    public RenderTarget CreateRenderTarget(nint hwnd, int width, int height)
    {
        ThrowIfDisposed();
        return new RenderTarget(this, hwnd, width, height);
    }

    /// <summary>
    /// Creates a solid color brush.
    /// </summary>
    /// <param name="r">Red component (0-1).</param>
    /// <param name="g">Green component (0-1).</param>
    /// <param name="b">Blue component (0-1).</param>
    /// <param name="a">Alpha component (0-1).</param>
    /// <returns>A new brush.</returns>
    public NativeBrush CreateSolidBrush(float r, float g, float b, float a = 1.0f)
    {
        ThrowIfDisposed();
        return new NativeBrush(this, r, g, b, a);
    }

    /// <summary>
    /// Creates a text format.
    /// </summary>
    /// <param name="fontFamily">The font family name.</param>
    /// <param name="fontSize">The font size.</param>
    /// <param name="fontWeight">The font weight (400 = normal, 700 = bold).</param>
    /// <param name="fontStyle">The font style (0 = normal, 1 = italic).</param>
    /// <returns>A new text format.</returns>
    public NativeTextFormat CreateTextFormat(string fontFamily, float fontSize, int fontWeight = 400, int fontStyle = 0)
    {
        ThrowIfDisposed();
        return new NativeTextFormat(this, fontFamily, fontSize, fontWeight, fontStyle);
    }

    /// <summary>
    /// Creates a bitmap from encoded image data (PNG, JPEG, etc.).
    /// </summary>
    /// <param name="imageData">The encoded image data.</param>
    /// <returns>A new bitmap.</returns>
    public NativeBitmap CreateBitmap(byte[] imageData)
    {
        ThrowIfDisposed();
        return new NativeBitmap(this, imageData);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_handle != nint.Zero)
        {
            NativeMethods.ContextDestroy(_handle);
            _handle = nint.Zero;
        }

        if (_current == this)
        {
            _current = null;
        }

        GC.SuppressFinalize(this);
    }

    ~RenderContext()
    {
        Dispose();
    }
}
