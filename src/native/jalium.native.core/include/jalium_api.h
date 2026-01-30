#pragma once

#include "jalium_types.h"

// Platform-specific export macros
#ifdef _WIN32
    #ifdef JALIUM_EXPORTS
        #define JALIUM_API __declspec(dllexport)
    #else
        #define JALIUM_API __declspec(dllimport)
    #endif
#else
    #define JALIUM_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

// ============================================================================
// Context Management
// ============================================================================

/// Creates a new Jalium rendering context with the specified backend.
/// @param backend The rendering backend to use.
/// @return A handle to the created context, or nullptr on failure.
JALIUM_API JaliumContext* jalium_context_create(JaliumBackend backend);

/// Destroys a Jalium rendering context and releases all associated resources.
/// @param ctx The context to destroy.
JALIUM_API void jalium_context_destroy(JaliumContext* ctx);

/// Gets the backend type of a context.
/// @param ctx The context.
/// @return The backend type.
JALIUM_API JaliumBackend jalium_context_get_backend(JaliumContext* ctx);

/// Gets the last error code.
/// @param ctx The context.
/// @return The error code, or JALIUM_OK if no error.
JALIUM_API JaliumResult jalium_context_get_last_error(JaliumContext* ctx);

/// Gets the last error message.
/// @param ctx The context.
/// @return The error message, or nullptr if no error.
JALIUM_API const wchar_t* jalium_context_get_error_message(JaliumContext* ctx);

// ============================================================================
// Render Target Management
// ============================================================================

/// Creates a render target for a window handle.
/// @param ctx The rendering context.
/// @param hwnd The native window handle (HWND on Windows).
/// @param width The width in pixels.
/// @param height The height in pixels.
/// @return A handle to the created render target, or nullptr on failure.
JALIUM_API JaliumRenderTarget* jalium_render_target_create_for_hwnd(
    JaliumContext* ctx,
    void* hwnd,
    int32_t width,
    int32_t height
);

/// Destroys a render target.
/// @param rt The render target to destroy.
JALIUM_API void jalium_render_target_destroy(JaliumRenderTarget* rt);

/// Resizes a render target.
/// @param rt The render target.
/// @param width The new width in pixels.
/// @param height The new height in pixels.
/// @return JALIUM_OK on success.
JALIUM_API JaliumResult jalium_render_target_resize(JaliumRenderTarget* rt, int32_t width, int32_t height);

/// Begins a drawing session on the render target.
/// @param rt The render target.
/// @return JALIUM_OK on success.
JALIUM_API JaliumResult jalium_render_target_begin_draw(JaliumRenderTarget* rt);

/// Ends a drawing session and presents the result.
/// @param rt The render target.
/// @return JALIUM_OK on success.
JALIUM_API JaliumResult jalium_render_target_end_draw(JaliumRenderTarget* rt);

/// Clears the render target with a color.
/// @param rt The render target.
/// @param r Red component (0.0 - 1.0).
/// @param g Green component (0.0 - 1.0).
/// @param b Blue component (0.0 - 1.0).
/// @param a Alpha component (0.0 - 1.0).
JALIUM_API void jalium_render_target_clear(JaliumRenderTarget* rt, float r, float g, float b, float a);

/// Sets whether VSync is enabled for the render target.
/// When disabled, Present returns immediately for faster frame updates during resize.
/// @param rt The render target.
/// @param enabled 1 to enable VSync, 0 to disable.
JALIUM_API void jalium_render_target_set_vsync(JaliumRenderTarget* rt, int32_t enabled);

// ============================================================================
// Drawing Commands
// ============================================================================

/// Draws a filled rectangle.
/// @param rt The render target.
/// @param x The x coordinate.
/// @param y The y coordinate.
/// @param width The width.
/// @param height The height.
/// @param brush The brush to fill with.
JALIUM_API void jalium_draw_fill_rectangle(
    JaliumRenderTarget* rt,
    float x, float y, float width, float height,
    JaliumBrush* brush
);

/// Draws a rectangle outline.
/// @param rt The render target.
/// @param x The x coordinate.
/// @param y The y coordinate.
/// @param width The width.
/// @param height The height.
/// @param brush The brush for the outline.
/// @param strokeWidth The stroke width.
JALIUM_API void jalium_draw_rectangle(
    JaliumRenderTarget* rt,
    float x, float y, float width, float height,
    JaliumBrush* brush,
    float strokeWidth
);

/// Draws a filled ellipse.
/// @param rt The render target.
/// @param centerX The center x coordinate.
/// @param centerY The center y coordinate.
/// @param radiusX The x radius.
/// @param radiusY The y radius.
/// @param brush The brush to fill with.
JALIUM_API void jalium_draw_fill_ellipse(
    JaliumRenderTarget* rt,
    float centerX, float centerY, float radiusX, float radiusY,
    JaliumBrush* brush
);

/// Draws an ellipse outline.
/// @param rt The render target.
/// @param centerX The center x coordinate.
/// @param centerY The center y coordinate.
/// @param radiusX The x radius.
/// @param radiusY The y radius.
/// @param brush The brush for the outline.
/// @param strokeWidth The stroke width.
JALIUM_API void jalium_draw_ellipse(
    JaliumRenderTarget* rt,
    float centerX, float centerY, float radiusX, float radiusY,
    JaliumBrush* brush,
    float strokeWidth
);

/// Draws a line.
/// @param rt The render target.
/// @param x1 The start x coordinate.
/// @param y1 The start y coordinate.
/// @param x2 The end x coordinate.
/// @param y2 The end y coordinate.
/// @param brush The brush for the line.
/// @param strokeWidth The stroke width.
JALIUM_API void jalium_draw_line(
    JaliumRenderTarget* rt,
    float x1, float y1, float x2, float y2,
    JaliumBrush* brush,
    float strokeWidth
);

/// Draws text.
/// @param rt The render target.
/// @param text The text to draw.
/// @param textLength The length of the text (number of characters).
/// @param format The text format.
/// @param x The x coordinate of the layout box.
/// @param y The y coordinate of the layout box.
/// @param width The width of the layout box.
/// @param height The height of the layout box.
/// @param brush The brush for the text.
JALIUM_API void jalium_draw_text(
    JaliumRenderTarget* rt,
    const wchar_t* text,
    uint32_t textLength,
    JaliumTextFormat* format,
    float x, float y, float width, float height,
    JaliumBrush* brush
);

/// Draws a filled rounded rectangle.
/// @param rt The render target.
/// @param x The x coordinate.
/// @param y The y coordinate.
/// @param width The width.
/// @param height The height.
/// @param radiusX The x radius of the corners.
/// @param radiusY The y radius of the corners.
/// @param brush The brush to fill with.
JALIUM_API void jalium_draw_fill_rounded_rectangle(
    JaliumRenderTarget* rt,
    float x, float y, float width, float height,
    float radiusX, float radiusY,
    JaliumBrush* brush
);

/// Draws a rounded rectangle outline.
/// @param rt The render target.
/// @param x The x coordinate.
/// @param y The y coordinate.
/// @param width The width.
/// @param height The height.
/// @param radiusX The x radius of the corners.
/// @param radiusY The y radius of the corners.
/// @param brush The brush for the outline.
/// @param strokeWidth The stroke width.
JALIUM_API void jalium_draw_rounded_rectangle(
    JaliumRenderTarget* rt,
    float x, float y, float width, float height,
    float radiusX, float radiusY,
    JaliumBrush* brush,
    float strokeWidth
);

// ============================================================================
// Transform Stack
// ============================================================================

/// Pushes a transform matrix onto the stack.
/// @param rt The render target.
/// @param matrix The 3x2 transform matrix (column-major, 6 floats).
JALIUM_API void jalium_push_transform(JaliumRenderTarget* rt, const float* matrix);

/// Pops the top transform from the stack.
/// @param rt The render target.
JALIUM_API void jalium_pop_transform(JaliumRenderTarget* rt);

/// Pushes a clip rectangle onto the stack.
/// @param rt The render target.
/// @param x The x coordinate.
/// @param y The y coordinate.
/// @param width The width.
/// @param height The height.
JALIUM_API void jalium_push_clip(JaliumRenderTarget* rt, float x, float y, float width, float height);

/// Pops the top clip from the stack.
/// @param rt The render target.
JALIUM_API void jalium_pop_clip(JaliumRenderTarget* rt);

/// Pushes an opacity value onto the stack.
/// @param rt The render target.
/// @param opacity The opacity (0.0 - 1.0).
JALIUM_API void jalium_push_opacity(JaliumRenderTarget* rt, float opacity);

/// Pops the top opacity from the stack.
/// @param rt The render target.
JALIUM_API void jalium_pop_opacity(JaliumRenderTarget* rt);

// ============================================================================
// Brush Management
// ============================================================================

/// Creates a solid color brush.
/// @param ctx The rendering context.
/// @param r Red component (0.0 - 1.0).
/// @param g Green component (0.0 - 1.0).
/// @param b Blue component (0.0 - 1.0).
/// @param a Alpha component (0.0 - 1.0).
/// @return A handle to the created brush, or nullptr on failure.
JALIUM_API JaliumBrush* jalium_brush_create_solid(JaliumContext* ctx, float r, float g, float b, float a);

/// Creates a linear gradient brush.
/// @param ctx The rendering context.
/// @param startX The start x coordinate.
/// @param startY The start y coordinate.
/// @param endX The end x coordinate.
/// @param endY The end y coordinate.
/// @param stops Array of gradient stops (position, r, g, b, a for each stop).
/// @param stopCount Number of gradient stops.
/// @return A handle to the created brush, or nullptr on failure.
JALIUM_API JaliumBrush* jalium_brush_create_linear_gradient(
    JaliumContext* ctx,
    float startX, float startY, float endX, float endY,
    const JaliumGradientStop* stops,
    uint32_t stopCount
);

/// Destroys a brush.
/// @param brush The brush to destroy.
JALIUM_API void jalium_brush_destroy(JaliumBrush* brush);

// ============================================================================
// Text Format Management
// ============================================================================

/// Creates a text format.
/// @param ctx The rendering context.
/// @param fontFamily The font family name.
/// @param fontSize The font size in DIPs.
/// @param fontWeight The font weight (100-900, 400 = normal, 700 = bold).
/// @param fontStyle The font style (0 = normal, 1 = italic, 2 = oblique).
/// @return A handle to the created text format, or nullptr on failure.
JALIUM_API JaliumTextFormat* jalium_text_format_create(
    JaliumContext* ctx,
    const wchar_t* fontFamily,
    float fontSize,
    int32_t fontWeight,
    int32_t fontStyle
);

/// Destroys a text format.
/// @param format The text format to destroy.
JALIUM_API void jalium_text_format_destroy(JaliumTextFormat* format);

/// Sets the text alignment.
/// @param format The text format.
/// @param alignment The alignment (0 = leading, 1 = trailing, 2 = center, 3 = justified).
JALIUM_API void jalium_text_format_set_alignment(JaliumTextFormat* format, int32_t alignment);

/// Sets the paragraph alignment.
/// @param format The text format.
/// @param alignment The alignment (0 = near/top, 1 = far/bottom, 2 = center).
JALIUM_API void jalium_text_format_set_paragraph_alignment(JaliumTextFormat* format, int32_t alignment);

/// Sets the text trimming mode.
/// @param format The text format.
/// @param trimming The trimming mode (0 = none, 1 = character ellipsis, 2 = word ellipsis).
JALIUM_API void jalium_text_format_set_trimming(JaliumTextFormat* format, int32_t trimming);

/// Measures text and returns metrics.
/// Uses DirectWrite's IDWriteTextLayout for accurate measurement.
/// @param format The text format.
/// @param text The text to measure.
/// @param textLength The length of the text (number of characters).
/// @param maxWidth The maximum width constraint (use a large value for no constraint).
/// @param maxHeight The maximum height constraint (use a large value for no constraint).
/// @param metrics Output structure to receive the text metrics.
/// @return JALIUM_OK on success.
JALIUM_API JaliumResult jalium_text_format_measure_text(
    JaliumTextFormat* format,
    const wchar_t* text,
    uint32_t textLength,
    float maxWidth,
    float maxHeight,
    JaliumTextMetrics* metrics
);

/// Gets font metrics for a text format.
/// Returns the font's ascent, descent, and line gap without needing text.
/// @param format The text format.
/// @param metrics Output structure to receive the metrics.
/// @return JALIUM_OK on success.
JALIUM_API JaliumResult jalium_text_format_get_font_metrics(
    JaliumTextFormat* format,
    JaliumTextMetrics* metrics
);

// ============================================================================
// Bitmap Management
// ============================================================================

/// Creates a bitmap from encoded image data (PNG, JPEG, etc.).
/// @param ctx The rendering context.
/// @param data The encoded image data.
/// @param dataSize The size of the data in bytes.
/// @return A handle to the created bitmap, or nullptr on failure.
JALIUM_API JaliumImage* jalium_bitmap_create_from_memory(
    JaliumContext* ctx,
    const uint8_t* data,
    uint32_t dataSize
);

/// Gets the width of a bitmap.
/// @param bitmap The bitmap.
/// @return The width in pixels.
JALIUM_API uint32_t jalium_bitmap_get_width(JaliumImage* bitmap);

/// Gets the height of a bitmap.
/// @param bitmap The bitmap.
/// @return The height in pixels.
JALIUM_API uint32_t jalium_bitmap_get_height(JaliumImage* bitmap);

/// Destroys a bitmap.
/// @param bitmap The bitmap to destroy.
JALIUM_API void jalium_bitmap_destroy(JaliumImage* bitmap);

/// Draws a bitmap.
/// @param rt The render target.
/// @param bitmap The bitmap to draw.
/// @param x The destination x coordinate.
/// @param y The destination y coordinate.
/// @param width The destination width.
/// @param height The destination height.
/// @param opacity The opacity (0.0 - 1.0).
JALIUM_API void jalium_draw_bitmap(
    JaliumRenderTarget* rt,
    JaliumImage* bitmap,
    float x, float y, float width, float height,
    float opacity
);

// ============================================================================
// Backend Registration
// ============================================================================

/// Registers a rendering backend.
/// @param backend The backend type.
/// @param factory The factory function to create the backend.
/// @return JALIUM_OK on success.
JALIUM_API JaliumResult jalium_register_backend(JaliumBackend backend, JaliumBackendFactory factory);

/// Checks if a backend is available.
/// @param backend The backend type.
/// @return 1 if available, 0 if not.
JALIUM_API int32_t jalium_is_backend_available(JaliumBackend backend);

#ifdef __cplusplus
}
#endif
