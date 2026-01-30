namespace Jalium.UI.Interop;

/// <summary>
/// Represents a native render target for drawing.
/// </summary>
public sealed class RenderTarget : IDisposable
{
    private readonly RenderContext _context;
    private nint _handle;
    private bool _disposed;
    private bool _isDrawing;

    /// <summary>
    /// Gets the native handle.
    /// </summary>
    public nint Handle => _handle;

    /// <summary>
    /// Gets whether the render target is valid.
    /// </summary>
    public bool IsValid => _handle != nint.Zero && !_disposed;

    /// <summary>
    /// Gets whether a drawing session is active.
    /// </summary>
    public bool IsDrawing => _isDrawing;

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    public int Height { get; private set; }

    internal RenderTarget(RenderContext context, nint hwnd, int width, int height)
    {
        _context = context;
        Width = width;
        Height = height;

        _handle = NativeMethods.RenderTargetCreateForHwnd(context.Handle, hwnd, width, height);
        if (_handle == nint.Zero)
        {
            throw new InvalidOperationException("Failed to create render target");
        }
    }

    /// <summary>
    /// Resizes the render target.
    /// </summary>
    /// <param name="width">The new width.</param>
    /// <param name="height">The new height.</param>
    public void Resize(int width, int height)
    {
        ThrowIfDisposed();
        if (width <= 0 || height <= 0) return;

        var result = NativeMethods.RenderTargetResize(_handle, width, height);
        if (result == 0)
        {
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// Begins a drawing session.
    /// </summary>
    public void BeginDraw()
    {
        ThrowIfDisposed();
        if (_isDrawing) return;

        NativeMethods.RenderTargetBeginDraw(_handle);
        _isDrawing = true;
    }

    /// <summary>
    /// Ends a drawing session and presents the content.
    /// </summary>
    public void EndDraw()
    {
        ThrowIfDisposed();
        if (!_isDrawing) return;

        NativeMethods.RenderTargetEndDraw(_handle);
        _isDrawing = false;
    }

    /// <summary>
    /// Clears the render target with the specified color.
    /// </summary>
    /// <param name="r">Red component (0-1).</param>
    /// <param name="g">Green component (0-1).</param>
    /// <param name="b">Blue component (0-1).</param>
    /// <param name="a">Alpha component (0-1).</param>
    public void Clear(float r, float g, float b, float a = 1.0f)
    {
        ThrowIfDisposed();
        NativeMethods.RenderTargetClear(_handle, r, g, b, a);
    }

    /// <summary>
    /// Draws a filled rectangle.
    /// </summary>
    public void FillRectangle(float x, float y, float width, float height, NativeBrush brush)
    {
        ThrowIfDisposed();
        if (brush == null || !brush.IsValid) return;
        NativeMethods.DrawFillRectangle(_handle, x, y, width, height, brush.Handle);
    }

    /// <summary>
    /// Draws a rectangle outline.
    /// </summary>
    public void DrawRectangle(float x, float y, float width, float height, NativeBrush brush, float strokeWidth = 1.0f)
    {
        ThrowIfDisposed();
        if (brush == null || !brush.IsValid) return;
        NativeMethods.DrawRectangle(_handle, x, y, width, height, brush.Handle, strokeWidth);
    }

    /// <summary>
    /// Draws a filled rounded rectangle.
    /// </summary>
    public void FillRoundedRectangle(float x, float y, float width, float height, float radiusX, float radiusY, NativeBrush brush)
    {
        ThrowIfDisposed();
        if (brush == null || !brush.IsValid) return;
        NativeMethods.DrawFillRoundedRectangle(_handle, x, y, width, height, radiusX, radiusY, brush.Handle);
    }

    /// <summary>
    /// Draws a rounded rectangle outline.
    /// </summary>
    public void DrawRoundedRectangle(float x, float y, float width, float height, float radiusX, float radiusY, NativeBrush brush, float strokeWidth = 1.0f)
    {
        ThrowIfDisposed();
        if (brush == null || !brush.IsValid) return;
        NativeMethods.DrawRoundedRectangle(_handle, x, y, width, height, radiusX, radiusY, brush.Handle, strokeWidth);
    }

    /// <summary>
    /// Draws a filled ellipse.
    /// </summary>
    public void FillEllipse(float centerX, float centerY, float radiusX, float radiusY, NativeBrush brush)
    {
        ThrowIfDisposed();
        if (brush == null || !brush.IsValid) return;
        NativeMethods.DrawFillEllipse(_handle, centerX, centerY, radiusX, radiusY, brush.Handle);
    }

    /// <summary>
    /// Draws an ellipse outline.
    /// </summary>
    public void DrawEllipse(float centerX, float centerY, float radiusX, float radiusY, NativeBrush brush, float strokeWidth = 1.0f)
    {
        ThrowIfDisposed();
        if (brush == null || !brush.IsValid) return;
        NativeMethods.DrawEllipse(_handle, centerX, centerY, radiusX, radiusY, brush.Handle, strokeWidth);
    }

    /// <summary>
    /// Draws a line.
    /// </summary>
    public void DrawLine(float x1, float y1, float x2, float y2, NativeBrush brush, float strokeWidth = 1.0f)
    {
        ThrowIfDisposed();
        if (brush == null || !brush.IsValid) return;
        NativeMethods.DrawLine(_handle, x1, y1, x2, y2, brush.Handle, strokeWidth);
    }

    /// <summary>
    /// Draws text.
    /// </summary>
    public void DrawText(string text, NativeTextFormat format, float x, float y, float width, float height, NativeBrush brush)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(text) || format == null || !format.IsValid || brush == null || !brush.IsValid) return;
        NativeMethods.DrawText(_handle, text, text.Length, format.Handle, x, y, width, height, brush.Handle);
    }

    /// <summary>
    /// Pushes a transform matrix.
    /// </summary>
    public void PushTransform(float[] matrix)
    {
        ThrowIfDisposed();
        if (matrix == null || matrix.Length < 6) return;
        NativeMethods.PushTransform(_handle, matrix);
    }

    /// <summary>
    /// Pops the current transform.
    /// </summary>
    public void PopTransform()
    {
        ThrowIfDisposed();
        NativeMethods.PopTransform(_handle);
    }

    /// <summary>
    /// Pushes a clip rectangle.
    /// </summary>
    public void PushClip(float x, float y, float width, float height)
    {
        ThrowIfDisposed();
        NativeMethods.PushClip(_handle, x, y, width, height);
    }

    /// <summary>
    /// Pops the current clip.
    /// </summary>
    public void PopClip()
    {
        ThrowIfDisposed();
        NativeMethods.PopClip(_handle);
    }

    /// <summary>
    /// Pushes an opacity value.
    /// </summary>
    public void PushOpacity(float opacity)
    {
        ThrowIfDisposed();
        NativeMethods.PushOpacity(_handle, opacity);
    }

    /// <summary>
    /// Pops the current opacity.
    /// </summary>
    public void PopOpacity()
    {
        ThrowIfDisposed();
        NativeMethods.PopOpacity(_handle);
    }

    /// <summary>
    /// Sets whether VSync is enabled.
    /// When disabled, Present returns immediately for faster frame updates during resize.
    /// </summary>
    /// <param name="enabled">True to enable VSync, false to disable.</param>
    public void SetVSyncEnabled(bool enabled)
    {
        ThrowIfDisposed();
        NativeMethods.RenderTargetSetVSync(_handle, enabled ? 1 : 0);
    }

    /// <summary>
    /// Draws a bitmap.
    /// </summary>
    /// <param name="bitmap">The bitmap to draw.</param>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="opacity">The opacity (0-1).</param>
    public void DrawBitmap(NativeBitmap bitmap, float x, float y, float width, float height, float opacity = 1.0f)
    {
        ThrowIfDisposed();
        if (bitmap == null || !bitmap.IsValid) return;
        NativeMethods.DrawBitmap(_handle, bitmap.Handle, x, y, width, height, opacity);
    }

    /// <summary>
    /// Draws a backdrop filter effect.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <param name="width">The width of the filter area.</param>
    /// <param name="height">The height of the filter area.</param>
    /// <param name="backdropFilter">The CSS-style backdrop filter string.</param>
    /// <param name="material">The material type string.</param>
    /// <param name="materialTint">The material tint color string.</param>
    /// <param name="materialTintOpacity">The material tint opacity.</param>
    /// <param name="materialBlurRadius">The material blur radius.</param>
    /// <param name="cornerRadiusTL">Top-left corner radius.</param>
    /// <param name="cornerRadiusTR">Top-right corner radius.</param>
    /// <param name="cornerRadiusBR">Bottom-right corner radius.</param>
    /// <param name="cornerRadiusBL">Bottom-left corner radius.</param>
    public void DrawBackdropFilter(
        float x, float y, float width, float height,
        string? backdropFilter,
        string? material,
        string? materialTint,
        float materialTintOpacity,
        float materialBlurRadius,
        float cornerRadiusTL,
        float cornerRadiusTR,
        float cornerRadiusBR,
        float cornerRadiusBL)
    {
        ThrowIfDisposed();
        NativeMethods.DrawBackdropFilter(
            _handle,
            x, y, width, height,
            backdropFilter ?? string.Empty,
            material ?? string.Empty,
            materialTint ?? string.Empty,
            materialTintOpacity,
            materialBlurRadius,
            cornerRadiusTL,
            cornerRadiusTR,
            cornerRadiusBR,
            cornerRadiusBL);
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

        if (_isDrawing)
        {
            try { NativeMethods.RenderTargetEndDraw(_handle); } catch { }
            _isDrawing = false;
        }

        if (_handle != nint.Zero)
        {
            NativeMethods.RenderTargetDestroy(_handle);
            _handle = nint.Zero;
        }

        GC.SuppressFinalize(this);
    }

    ~RenderTarget()
    {
        Dispose();
    }
}
