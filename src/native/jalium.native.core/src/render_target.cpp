#include "jalium_internal.h"

// ============================================================================
// C API Implementation
// ============================================================================

extern "C" {

JALIUM_API JaliumRenderTarget* jalium_render_target_create_for_hwnd(
    JaliumContext* ctx,
    void* hwnd,
    int32_t width,
    int32_t height)
{
    if (!ctx || !hwnd || width <= 0 || height <= 0) {
        return nullptr;
    }

    auto backend = jalium::GetBackendFromContext(ctx);
    if (!backend) {
        return nullptr;
    }

    auto rt = backend->CreateRenderTarget(hwnd, width, height);
    return reinterpret_cast<JaliumRenderTarget*>(rt);
}

JALIUM_API void jalium_render_target_destroy(JaliumRenderTarget* rt) {
    if (rt) {
        delete reinterpret_cast<jalium::RenderTarget*>(rt);
    }
}

JALIUM_API JaliumResult jalium_render_target_resize(JaliumRenderTarget* rt, int32_t width, int32_t height) {
    if (!rt || width <= 0 || height <= 0) {
        return JALIUM_ERROR_INVALID_ARGUMENT;
    }
    return reinterpret_cast<jalium::RenderTarget*>(rt)->Resize(width, height);
}

JALIUM_API JaliumResult jalium_render_target_begin_draw(JaliumRenderTarget* rt) {
    if (!rt) return JALIUM_ERROR_INVALID_ARGUMENT;
    return reinterpret_cast<jalium::RenderTarget*>(rt)->BeginDraw();
}

JALIUM_API JaliumResult jalium_render_target_end_draw(JaliumRenderTarget* rt) {
    if (!rt) return JALIUM_ERROR_INVALID_ARGUMENT;
    return reinterpret_cast<jalium::RenderTarget*>(rt)->EndDraw();
}

JALIUM_API void jalium_render_target_clear(JaliumRenderTarget* rt, float r, float g, float b, float a) {
    if (rt) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->Clear(r, g, b, a);
    }
}

JALIUM_API void jalium_render_target_set_vsync(JaliumRenderTarget* rt, int32_t enabled) {
    if (rt) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->SetVSyncEnabled(enabled != 0);
    }
}

JALIUM_API void jalium_draw_fill_rectangle(
    JaliumRenderTarget* rt,
    float x, float y, float width, float height,
    JaliumBrush* brush)
{
    if (rt && brush) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->FillRectangle(
            x, y, width, height,
            reinterpret_cast<jalium::Brush*>(brush));
    }
}

JALIUM_API void jalium_draw_rectangle(
    JaliumRenderTarget* rt,
    float x, float y, float width, float height,
    JaliumBrush* brush,
    float strokeWidth)
{
    if (rt && brush) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->DrawRectangle(
            x, y, width, height,
            reinterpret_cast<jalium::Brush*>(brush),
            strokeWidth);
    }
}

JALIUM_API void jalium_draw_fill_rounded_rectangle(
    JaliumRenderTarget* rt,
    float x, float y, float width, float height,
    float radiusX, float radiusY,
    JaliumBrush* brush)
{
    if (rt && brush) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->FillRoundedRectangle(
            x, y, width, height, radiusX, radiusY,
            reinterpret_cast<jalium::Brush*>(brush));
    }
}

JALIUM_API void jalium_draw_rounded_rectangle(
    JaliumRenderTarget* rt,
    float x, float y, float width, float height,
    float radiusX, float radiusY,
    JaliumBrush* brush,
    float strokeWidth)
{
    if (rt && brush) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->DrawRoundedRectangle(
            x, y, width, height, radiusX, radiusY,
            reinterpret_cast<jalium::Brush*>(brush),
            strokeWidth);
    }
}

JALIUM_API void jalium_draw_fill_ellipse(
    JaliumRenderTarget* rt,
    float centerX, float centerY, float radiusX, float radiusY,
    JaliumBrush* brush)
{
    if (rt && brush) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->FillEllipse(
            centerX, centerY, radiusX, radiusY,
            reinterpret_cast<jalium::Brush*>(brush));
    }
}

JALIUM_API void jalium_draw_ellipse(
    JaliumRenderTarget* rt,
    float centerX, float centerY, float radiusX, float radiusY,
    JaliumBrush* brush,
    float strokeWidth)
{
    if (rt && brush) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->DrawEllipse(
            centerX, centerY, radiusX, radiusY,
            reinterpret_cast<jalium::Brush*>(brush),
            strokeWidth);
    }
}

JALIUM_API void jalium_draw_line(
    JaliumRenderTarget* rt,
    float x1, float y1, float x2, float y2,
    JaliumBrush* brush,
    float strokeWidth)
{
    if (rt && brush) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->DrawLine(
            x1, y1, x2, y2,
            reinterpret_cast<jalium::Brush*>(brush),
            strokeWidth);
    }
}

JALIUM_API void jalium_fill_polygon(
    JaliumRenderTarget* rt,
    const float* points,
    uint32_t pointCount,
    JaliumBrush* brush,
    int32_t fillRule)
{
    if (rt && brush && points && pointCount >= 3) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->FillPolygon(
            points, pointCount,
            reinterpret_cast<jalium::Brush*>(brush),
            fillRule);
    }
}

JALIUM_API void jalium_draw_polygon(
    JaliumRenderTarget* rt,
    const float* points,
    uint32_t pointCount,
    JaliumBrush* brush,
    float strokeWidth,
    int32_t closed)
{
    if (rt && brush && points && pointCount >= 2) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->DrawPolygon(
            points, pointCount,
            reinterpret_cast<jalium::Brush*>(brush),
            strokeWidth,
            closed != 0);
    }
}

JALIUM_API void jalium_draw_text(
    JaliumRenderTarget* rt,
    const wchar_t* text,
    uint32_t textLength,
    JaliumTextFormat* format,
    float x, float y, float width, float height,
    JaliumBrush* brush)
{
    if (rt && text && format && brush) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->RenderText(
            text, textLength,
            reinterpret_cast<jalium::TextFormat*>(format),
            x, y, width, height,
            reinterpret_cast<jalium::Brush*>(brush));
    }
}

JALIUM_API void jalium_push_transform(JaliumRenderTarget* rt, const float* matrix) {
    if (rt && matrix) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->PushTransform(matrix);
    }
}

JALIUM_API void jalium_pop_transform(JaliumRenderTarget* rt) {
    if (rt) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->PopTransform();
    }
}

JALIUM_API void jalium_push_clip(JaliumRenderTarget* rt, float x, float y, float width, float height) {
    if (rt) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->PushClip(x, y, width, height);
    }
}

JALIUM_API void jalium_pop_clip(JaliumRenderTarget* rt) {
    if (rt) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->PopClip();
    }
}

JALIUM_API void jalium_push_opacity(JaliumRenderTarget* rt, float opacity) {
    if (rt) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->PushOpacity(opacity);
    }
}

JALIUM_API void jalium_pop_opacity(JaliumRenderTarget* rt) {
    if (rt) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->PopOpacity();
    }
}

JALIUM_API void jalium_draw_bitmap(
    JaliumRenderTarget* rt,
    JaliumImage* bitmap,
    float x, float y, float width, float height,
    float opacity)
{
    if (rt && bitmap) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->DrawBitmap(
            reinterpret_cast<jalium::Bitmap*>(bitmap),
            x, y, width, height, opacity);
    }
}

JALIUM_API void jalium_draw_backdrop_filter(
    JaliumRenderTarget* rt,
    float x, float y, float width, float height,
    const char* backdropFilter,
    const char* material,
    const char* materialTint,
    float tintOpacity,
    float blurRadius,
    float cornerRadiusTL, float cornerRadiusTR,
    float cornerRadiusBR, float cornerRadiusBL)
{
    if (rt) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->DrawBackdropFilter(
            x, y, width, height,
            backdropFilter ? backdropFilter : "",
            material ? material : "",
            materialTint ? materialTint : "",
            tintOpacity,
            blurRadius,
            cornerRadiusTL, cornerRadiusTR,
            cornerRadiusBR, cornerRadiusBL);
    }
}

JALIUM_API void jalium_draw_glowing_border_highlight(
    JaliumRenderTarget* rt,
    float x, float y, float width, float height,
    float animationPhase,
    float glowColorR, float glowColorG, float glowColorB,
    float strokeWidth,
    float trailLength,
    float dimOpacity,
    float screenWidth, float screenHeight)
{
    if (rt) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->DrawGlowingBorderHighlight(
            x, y, width, height,
            animationPhase,
            glowColorR, glowColorG, glowColorB,
            strokeWidth,
            trailLength,
            dimOpacity,
            screenWidth, screenHeight);
    }
}

JALIUM_API void jalium_draw_glowing_border_transition(
    JaliumRenderTarget* rt,
    float fromX, float fromY, float fromWidth, float fromHeight,
    float toX, float toY, float toWidth, float toHeight,
    float headProgress, float tailProgress,
    float animationPhase,
    float glowColorR, float glowColorG, float glowColorB,
    float strokeWidth,
    float trailLength,
    float dimOpacity,
    float screenWidth, float screenHeight)
{
    if (rt) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->DrawGlowingBorderTransition(
            fromX, fromY, fromWidth, fromHeight,
            toX, toY, toWidth, toHeight,
            headProgress, tailProgress,
            animationPhase,
            glowColorR, glowColorG, glowColorB,
            strokeWidth,
            trailLength,
            dimOpacity,
            screenWidth, screenHeight);
    }
}

JALIUM_API void jalium_draw_ripple_effect(
    JaliumRenderTarget* rt,
    float x, float y, float width, float height,
    float rippleProgress,
    float glowColorR, float glowColorG, float glowColorB,
    float strokeWidth,
    float dimOpacity,
    float screenWidth, float screenHeight)
{
    if (rt) {
        reinterpret_cast<jalium::RenderTarget*>(rt)->DrawRippleEffect(
            x, y, width, height,
            rippleProgress,
            glowColorR, glowColorG, glowColorB,
            strokeWidth,
            dimOpacity,
            screenWidth, screenHeight);
    }
}

} // extern "C"
