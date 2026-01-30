#pragma once

#include "jalium_types.h"
#include <memory>
#include <string>

namespace jalium {

// Forward declarations
class RenderTarget;
class Brush;
class TextFormat;
class Bitmap;

/// Abstract interface for rendering backends.
/// Each rendering backend (D3D12, Vulkan, etc.) implements this interface.
class IRenderBackend {
public:
    virtual ~IRenderBackend() = default;

    /// Gets the backend type.
    virtual JaliumBackend GetType() const = 0;

    /// Gets the backend name for debugging.
    virtual const wchar_t* GetName() const = 0;

    /// Creates a render target for a window handle.
    virtual RenderTarget* CreateRenderTarget(void* hwnd, int32_t width, int32_t height) = 0;

    /// Creates a solid color brush.
    virtual Brush* CreateSolidBrush(float r, float g, float b, float a) = 0;

    /// Creates a linear gradient brush.
    virtual Brush* CreateLinearGradientBrush(
        float startX, float startY, float endX, float endY,
        const JaliumGradientStop* stops, uint32_t stopCount) = 0;

    /// Creates a text format.
    virtual TextFormat* CreateTextFormat(
        const wchar_t* fontFamily,
        float fontSize,
        int32_t fontWeight,
        int32_t fontStyle) = 0;

    /// Creates a bitmap from encoded image data (PNG, JPEG, etc.).
    virtual Bitmap* CreateBitmapFromMemory(const uint8_t* data, uint32_t dataSize) = 0;
};

/// Abstract base class for render targets.
class RenderTarget {
public:
    virtual ~RenderTarget() = default;

    /// Resizes the render target.
    virtual JaliumResult Resize(int32_t width, int32_t height) = 0;

    /// Begins a drawing session.
    virtual JaliumResult BeginDraw() = 0;

    /// Ends a drawing session and presents.
    virtual JaliumResult EndDraw() = 0;

    /// Clears with a color.
    virtual void Clear(float r, float g, float b, float a) = 0;

    /// Draws a filled rectangle.
    virtual void FillRectangle(float x, float y, float w, float h, Brush* brush) = 0;

    /// Draws a rectangle outline.
    virtual void DrawRectangle(float x, float y, float w, float h, Brush* brush, float strokeWidth) = 0;

    /// Draws a filled rounded rectangle.
    virtual void FillRoundedRectangle(float x, float y, float w, float h, float rx, float ry, Brush* brush) = 0;

    /// Draws a rounded rectangle outline.
    virtual void DrawRoundedRectangle(float x, float y, float w, float h, float rx, float ry, Brush* brush, float strokeWidth) = 0;

    /// Draws a filled ellipse.
    virtual void FillEllipse(float cx, float cy, float rx, float ry, Brush* brush) = 0;

    /// Draws an ellipse outline.
    virtual void DrawEllipse(float cx, float cy, float rx, float ry, Brush* brush, float strokeWidth) = 0;

    /// Draws a line.
    virtual void DrawLine(float x1, float y1, float x2, float y2, Brush* brush, float strokeWidth) = 0;

    /// Draws text.
    virtual void RenderText(
        const wchar_t* text, uint32_t textLength,
        TextFormat* format,
        float x, float y, float w, float h,
        Brush* brush) = 0;

    /// Pushes a transform.
    virtual void PushTransform(const float* matrix) = 0;

    /// Pops a transform.
    virtual void PopTransform() = 0;

    /// Pushes a clip rectangle.
    virtual void PushClip(float x, float y, float w, float h) = 0;

    /// Pops a clip.
    virtual void PopClip() = 0;

    /// Pushes an opacity.
    virtual void PushOpacity(float opacity) = 0;

    /// Pops an opacity.
    virtual void PopOpacity() = 0;

    /// Sets whether VSync is enabled.
    /// When disabled, Present returns immediately for faster frame updates during resize.
    virtual void SetVSyncEnabled(bool enabled) = 0;

    /// Draws a bitmap.
    virtual void DrawBitmap(Bitmap* bitmap, float x, float y, float w, float h, float opacity) = 0;

    /// Draws a backdrop filter effect.
    /// @param x X position.
    /// @param y Y position.
    /// @param w Width.
    /// @param h Height.
    /// @param backdropFilter CSS-style backdrop filter string (e.g., "blur(20px)").
    /// @param material Material type (e.g., "acrylic", "mica").
    /// @param materialTint Tint color in hex format.
    /// @param tintOpacity Tint opacity (0-1).
    /// @param blurRadius Blur radius in pixels.
    /// @param cornerRadiusTL Top-left corner radius.
    /// @param cornerRadiusTR Top-right corner radius.
    /// @param cornerRadiusBR Bottom-right corner radius.
    /// @param cornerRadiusBL Bottom-left corner radius.
    virtual void DrawBackdropFilter(
        float x, float y, float w, float h,
        const char* backdropFilter,
        const char* material,
        const char* materialTint,
        float tintOpacity,
        float blurRadius,
        float cornerRadiusTL, float cornerRadiusTR,
        float cornerRadiusBR, float cornerRadiusBL) = 0;

protected:
    int32_t width_ = 0;
    int32_t height_ = 0;
    bool vsyncEnabled_ = true;
};

/// Abstract base class for brushes.
class Brush {
public:
    virtual ~Brush() = default;
    virtual JaliumBrushType GetType() const = 0;
};

/// Abstract base class for text formats.
class TextFormat {
public:
    virtual ~TextFormat() = default;
    virtual void SetAlignment(int32_t alignment) = 0;
    virtual void SetParagraphAlignment(int32_t alignment) = 0;
    virtual void SetTrimming(int32_t trimming) = 0;

    /// Measures text and fills out the metrics structure.
    /// @param text The text to measure.
    /// @param textLength The length of the text.
    /// @param maxWidth Maximum layout width.
    /// @param maxHeight Maximum layout height.
    /// @param metrics Output metrics.
    /// @return JALIUM_OK on success.
    virtual JaliumResult MeasureText(
        const wchar_t* text,
        uint32_t textLength,
        float maxWidth,
        float maxHeight,
        JaliumTextMetrics* metrics) = 0;

    /// Gets font metrics without text.
    /// @param metrics Output metrics (only font-related fields are filled).
    /// @return JALIUM_OK on success.
    virtual JaliumResult GetFontMetrics(JaliumTextMetrics* metrics) = 0;
};

/// Abstract base class for bitmaps.
class Bitmap {
public:
    virtual ~Bitmap() = default;
    virtual uint32_t GetWidth() const = 0;
    virtual uint32_t GetHeight() const = 0;
};

} // namespace jalium
