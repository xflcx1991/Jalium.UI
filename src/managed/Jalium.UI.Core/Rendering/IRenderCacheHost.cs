namespace Jalium.UI.Rendering;

/// <summary>
/// Installs retained-mode drawing-command caching for <see cref="Visual"/>.
/// Implemented by <c>Jalium.UI.Media.Rendering.MediaRenderCacheHost</c> and
/// registered at runtime via <c>Visual.RenderCacheHost</c>. Core only sees the
/// opaque <see cref="object"/> contract so the Media/Drawing types stay on one
/// side of the layer boundary.
/// </summary>
/// <remarks>
/// <para>
/// The contract is: when a visual is about to invoke <c>OnRender(object)</c>
/// the render loop asks the host for a recorder, passes the recorder to
/// <c>OnRender</c> instead of the live drawing context, finishes recording to
/// obtain an opaque <em>Drawing</em> handle, caches the handle on the visual,
/// and replays it against the live drawing context. Subsequent frames skip
/// <c>OnRender</c> entirely until the visual marks itself render-dirty — then
/// the cached handle is discarded and the cycle repeats.
/// </para>
/// <para>
/// The recorder returned by <see cref="CreateRecorder"/> must simultaneously
/// satisfy the abstract <c>Jalium.UI.Media.DrawingContext</c> surface (so it
/// accepts every draw / push / pop call) and the Core-side drawing-context
/// interfaces (<c>IOffsetDrawingContext</c>, <c>IClipDrawingContext</c>,
/// <c>IOpacityDrawingContext</c>, <c>ITransformDrawingContext</c>,
/// <c>IClipBoundsDrawingContext</c>) so the existing <c>RenderDirect</c>
/// pipeline can use it interchangeably with the live context.
/// </para>
/// </remarks>
public interface IRenderCacheHost
{
    /// <summary>
    /// Starts a recording scope for a visual's <c>OnRender</c>. The returned
    /// recorder shadows <paramref name="targetDrawingContext"/>'s ambient
    /// state (offset, clip bounds) so user code observing these values during
    /// <c>OnRender</c> still sees correct values, while all draw calls are
    /// captured into an internal command list instead of being issued live.
    /// </summary>
    /// <param name="targetDrawingContext">
    /// The live drawing context currently in use. The recorder does not
    /// forward draws to it; it only mirrors ambient state.
    /// </param>
    /// <returns>
    /// An opaque recorder that callers must pass to <see cref="FinishRecord"/>
    /// exactly once. Do not retain it after <see cref="FinishRecord"/>.
    /// </returns>
    object CreateRecorder(object targetDrawingContext);

    /// <summary>
    /// Closes the recording and returns an immutable handle representing the
    /// captured draw commands. The recorder object becomes invalid — typical
    /// implementations pool it for reuse.
    /// </summary>
    /// <param name="recorder">The recorder previously returned by
    /// <see cref="CreateRecorder"/>.</param>
    /// <returns>An opaque <em>Drawing</em> handle suitable for
    /// <see cref="Replay"/>.</returns>
    object FinishRecord(object recorder);

    /// <summary>
    /// Plays every captured command from <paramref name="drawing"/> back
    /// against <paramref name="targetDrawingContext"/>. Equivalent in
    /// observable effect to re-invoking the original <c>OnRender</c> body,
    /// but without allocating temporary brushes / pens / geometries or
    /// re-running user code.
    /// </summary>
    void Replay(object drawing, object targetDrawingContext);
}
