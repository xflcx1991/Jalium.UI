using System.Runtime.InteropServices;

namespace Jalium.UI.Interop;

/// <summary>
/// Rendering backend types.
/// </summary>
public enum RenderBackend
{
    Auto = 0,
    D3D12 = 1,
    D3D11 = 2,
    Vulkan = 3,
    OpenGL = 4,
    Metal = 5,
    Software = 99
}

/// <summary>
/// Text metrics structure returned by native text measurement.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct TextMetrics
{
    /// <summary>The width of the text layout area.</summary>
    public float Width;

    /// <summary>The height of the text layout area.</summary>
    public float Height;

    /// <summary>The natural line height (ascent + descent + lineGap).</summary>
    public float LineHeight;

    /// <summary>The baseline offset from the top.</summary>
    public float Baseline;

    /// <summary>The ascent of the font (above baseline).</summary>
    public float Ascent;

    /// <summary>The descent of the font (below baseline).</summary>
    public float Descent;

    /// <summary>The recommended line gap.</summary>
    public float LineGap;

    /// <summary>The number of lines in the layout.</summary>
    public uint LineCount;
}

/// <summary>
/// Native method imports for the Jalium rendering engine.
/// </summary>
internal static partial class NativeMethods
{
    private const string CoreLib = "jalium.native.core";
    private const string D3D12Lib = "jalium.native.d3d12";

    private static nint _d3d12Module;

    /// <summary>
    /// Static constructor to load backend DLLs.
    /// </summary>
    static NativeMethods()
    {
        // Load the D3D12 backend DLL
        _d3d12Module = NativeLibrary.Load(D3D12Lib, typeof(NativeMethods).Assembly, null);

        // Call the explicit init function to register the backend
        // This is done outside DllMain to avoid loader lock issues with mutex operations
        if (_d3d12Module != nint.Zero)
        {
            // Try to find and call the init function
            // If it doesn't exist (old DLL version), the DLL might still work via DllMain registration
            if (NativeLibrary.TryGetExport(_d3d12Module, "jalium_d3d12_init", out var initFunc) && initFunc != nint.Zero)
            {
                var init = Marshal.GetDelegateForFunctionPointer<Action>(initFunc);
                init();
            }
        }
    }

    #region Context Management

    /// <summary>
    /// Creates a new Jalium rendering context.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_context_create")]
    internal static partial nint ContextCreate(RenderBackend backend);

    /// <summary>
    /// Destroys a Jalium rendering context.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_context_destroy")]
    internal static partial void ContextDestroy(nint context);

    /// <summary>
    /// Gets the backend type of a context.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_context_get_backend")]
    internal static partial RenderBackend ContextGetBackend(nint context);

    /// <summary>
    /// Gets the last error code.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_context_get_last_error")]
    internal static partial int ContextGetLastError(nint context);

    #endregion

    #region Render Target Management

    /// <summary>
    /// Creates a render target for a window handle.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_render_target_create_for_hwnd")]
    internal static partial nint RenderTargetCreateForHwnd(nint context, nint hwnd, int width, int height);

    /// <summary>
    /// Destroys a render target.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_render_target_destroy")]
    internal static partial void RenderTargetDestroy(nint renderTarget);

    /// <summary>
    /// Resizes a render target.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_render_target_resize")]
    internal static partial int RenderTargetResize(nint renderTarget, int width, int height);

    /// <summary>
    /// Begins a drawing session.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_render_target_begin_draw")]
    internal static partial int RenderTargetBeginDraw(nint renderTarget);

    /// <summary>
    /// Ends a drawing session.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_render_target_end_draw")]
    internal static partial int RenderTargetEndDraw(nint renderTarget);

    /// <summary>
    /// Clears the render target.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_render_target_clear")]
    internal static partial void RenderTargetClear(nint renderTarget, float r, float g, float b, float a);

    /// <summary>
    /// Sets whether VSync is enabled.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_render_target_set_vsync")]
    internal static partial void RenderTargetSetVSync(nint renderTarget, int enabled);

    #endregion

    #region Drawing Commands

    /// <summary>
    /// Draws a filled rectangle.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_draw_fill_rectangle")]
    internal static partial void DrawFillRectangle(nint renderTarget, float x, float y, float width, float height, nint brush);

    /// <summary>
    /// Draws a rectangle outline.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_draw_rectangle")]
    internal static partial void DrawRectangle(nint renderTarget, float x, float y, float width, float height, nint brush, float strokeWidth);

    /// <summary>
    /// Draws a filled rounded rectangle.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_draw_fill_rounded_rectangle")]
    internal static partial void DrawFillRoundedRectangle(nint renderTarget, float x, float y, float width, float height, float radiusX, float radiusY, nint brush);

    /// <summary>
    /// Draws a rounded rectangle outline.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_draw_rounded_rectangle")]
    internal static partial void DrawRoundedRectangle(nint renderTarget, float x, float y, float width, float height, float radiusX, float radiusY, nint brush, float strokeWidth);

    /// <summary>
    /// Draws a filled ellipse.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_draw_fill_ellipse")]
    internal static partial void DrawFillEllipse(nint renderTarget, float centerX, float centerY, float radiusX, float radiusY, nint brush);

    /// <summary>
    /// Draws an ellipse outline.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_draw_ellipse")]
    internal static partial void DrawEllipse(nint renderTarget, float centerX, float centerY, float radiusX, float radiusY, nint brush, float strokeWidth);

    /// <summary>
    /// Draws a line.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_draw_line")]
    internal static partial void DrawLine(nint renderTarget, float x1, float y1, float x2, float y2, nint brush, float strokeWidth);

    /// <summary>
    /// Draws text.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_draw_text", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial void DrawText(nint renderTarget, string text, int textLength, nint textFormat, float x, float y, float width, float height, nint brush);

    /// <summary>
    /// Draws a backdrop filter effect.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_draw_backdrop_filter", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void DrawBackdropFilter(
        nint renderTarget,
        float x, float y, float width, float height,
        string backdropFilter,
        string material,
        string materialTint,
        float materialTintOpacity,
        float materialBlurRadius,
        float cornerRadiusTL,
        float cornerRadiusTR,
        float cornerRadiusBR,
        float cornerRadiusBL);

    #endregion

    #region Transform and Clip

    /// <summary>
    /// Pushes a transform matrix.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_push_transform")]
    internal static partial void PushTransform(nint renderTarget, [In] float[] matrix);

    /// <summary>
    /// Pops the current transform.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_pop_transform")]
    internal static partial void PopTransform(nint renderTarget);

    /// <summary>
    /// Pushes a clip rectangle.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_push_clip")]
    internal static partial void PushClip(nint renderTarget, float x, float y, float width, float height);

    /// <summary>
    /// Pops the current clip.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_pop_clip")]
    internal static partial void PopClip(nint renderTarget);

    /// <summary>
    /// Pushes an opacity value.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_push_opacity")]
    internal static partial void PushOpacity(nint renderTarget, float opacity);

    /// <summary>
    /// Pops the current opacity.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_pop_opacity")]
    internal static partial void PopOpacity(nint renderTarget);

    #endregion

    #region Brush Management

    /// <summary>
    /// Creates a solid color brush.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_brush_create_solid")]
    internal static partial nint BrushCreateSolid(nint context, float r, float g, float b, float a);

    /// <summary>
    /// Destroys a brush.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_brush_destroy")]
    internal static partial void BrushDestroy(nint brush);

    #endregion

    #region Text Format Management

    /// <summary>
    /// Creates a text format.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_text_format_create", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint TextFormatCreate(nint context, string fontFamily, float fontSize, int fontWeight, int fontStyle);

    /// <summary>
    /// Destroys a text format.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_text_format_destroy")]
    internal static partial void TextFormatDestroy(nint textFormat);

    /// <summary>
    /// Sets text alignment.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_text_format_set_alignment")]
    internal static partial void TextFormatSetAlignment(nint textFormat, int alignment);

    /// <summary>
    /// Sets paragraph alignment.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_text_format_set_paragraph_alignment")]
    internal static partial void TextFormatSetParagraphAlignment(nint textFormat, int alignment);

    /// <summary>
    /// Sets text trimming mode.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_text_format_set_trimming")]
    internal static partial void TextFormatSetTrimming(nint textFormat, int trimming);

    /// <summary>
    /// Measures text and returns metrics.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_text_format_measure_text", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int TextFormatMeasureText(nint textFormat, string text, int textLength, float maxWidth, float maxHeight, out TextMetrics metrics);

    /// <summary>
    /// Gets font metrics without measuring text.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_text_format_get_font_metrics")]
    internal static partial int TextFormatGetFontMetrics(nint textFormat, out TextMetrics metrics);

    #endregion

    #region Bitmap Management

    /// <summary>
    /// Creates a bitmap from encoded image data.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_bitmap_create_from_memory")]
    internal static partial nint BitmapCreateFromMemory(nint context, [In] byte[] data, uint dataSize);

    /// <summary>
    /// Gets the width of a bitmap.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_bitmap_get_width")]
    internal static partial uint BitmapGetWidth(nint bitmap);

    /// <summary>
    /// Gets the height of a bitmap.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_bitmap_get_height")]
    internal static partial uint BitmapGetHeight(nint bitmap);

    /// <summary>
    /// Destroys a bitmap.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_bitmap_destroy")]
    internal static partial void BitmapDestroy(nint bitmap);

    /// <summary>
    /// Draws a bitmap.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_draw_bitmap")]
    internal static partial void DrawBitmap(nint renderTarget, nint bitmap, float x, float y, float width, float height, float opacity);

    #endregion

    #region Backend Registration

    /// <summary>
    /// Checks if a backend is available.
    /// </summary>
    [LibraryImport(CoreLib, EntryPoint = "jalium_is_backend_available")]
    internal static partial int IsBackendAvailable(RenderBackend backend);

    #endregion
}
