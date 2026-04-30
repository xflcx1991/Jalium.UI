#ifndef JALIUM_ROUNDED_CLIP_HLSLI
#define JALIUM_ROUNDED_CLIP_HLSLI

// ----------------------------------------------------------------------------
// Rounded-rect clip cbuffer (b2)
//
// Written every batch by D3D12DirectRenderer::RecordDrawCommands via
// SetGraphicsRoot32BitConstants(3, 12, ...).  All four batched pixel shaders
// (sdf_rect / bitmap_quad / bitmap_text / triangle) include this header and
// call DiscardOutsideRoundedClip(input.clipPos.xy) early in main, so the
// rounded clip is honored by every primitive the batched renderer emits.
//
// Coordinate space: physical-pixel (post-DPI) screen space, same as
// SV_Position.xy — the managed side passes the clip rect/radii in DIPs and
// ResolveRoundedClipForBatch multiplies through dpiScale_ before recording.
// ----------------------------------------------------------------------------

cbuffer RoundedClipConstants : register(b2)
{
    uint  hasRoundedClip;
    uint3 _padRoundedClip;
    float4 roundedClipRect;        // (left, top, right, bottom)
    float4 roundedClipCornerRadii; // per-corner: (TL, TR, BR, BL)
}

// Per-corner signed-distance to the rounded rectangle.  Picks the radius for
// the quadrant the fragment falls in, then runs the standard iquilezles SDF.
// <= 0 inside, > 0 outside.
float JaliumRoundedClipSdf(float2 p)
{
    float2 center   = (roundedClipRect.xy + roundedClipRect.zw) * 0.5;
    float2 halfSize = max((roundedClipRect.zw - roundedClipRect.xy) * 0.5, float2(0.0001, 0.0001));
    float2 q        = p - center;
    float  minDim   = min(halfSize.x, halfSize.y);

    // Layout: roundedClipCornerRadii = (TL, TR, BR, BL).
    // Pick by quadrant:
    //   q.x < 0, q.y < 0  → TL  (.x)
    //   q.x > 0, q.y < 0  → TR  (.y)
    //   q.x > 0, q.y > 0  → BR  (.z)
    //   q.x < 0, q.y > 0  → BL  (.w)
    float radius = (q.x > 0.0)
        ? ((q.y > 0.0) ? roundedClipCornerRadii.z : roundedClipCornerRadii.y)
        : ((q.y > 0.0) ? roundedClipCornerRadii.w : roundedClipCornerRadii.x);
    radius = clamp(radius, 0.0, minDim);

    float2 d = abs(q) - halfSize + radius;
    return min(max(d.x, d.y), 0.0) + length(max(d, 0.0)) - radius;
}

// Discards the fragment when the rounded clip is active and the pixel falls
// outside the SDF.  Half-pixel slack matches the smoothstep AA used by the
// SDF rect shader for the rounded rect itself, so clip edges align with
// fill edges to avoid one-pixel seams along the corner.
void DiscardOutsideRoundedClip(float2 fragPos)
{
    if (hasRoundedClip != 0u)
    {
        if (JaliumRoundedClipSdf(fragPos) > 0.5)
            discard;
    }
}

#endif // JALIUM_ROUNDED_CLIP_HLSLI
