using Jalium.UI.Media;

namespace Jalium.UI.Controls.Primitives;

/// <summary>
/// Internal element that renders text content for TextBoxBase controls.
/// This element is inserted into the PART_ContentHost of the control's template.
/// Similar to WPF's TextBoxView.
/// </summary>
internal sealed class TextBoxContentHost : FrameworkElement
{
    private readonly TextBoxBase _owner;

    // Track the width used during MeasureOverride so ArrangeOverride can spot
    // the Infinity-measure / finite-arrange mismatch that otherwise leaves
    // the reported DesiredSize based on unwrapped text while the renderer
    // wraps to the (narrower) arrange width.
    private double _lastMeasureWidth = double.NaN;

    /// <summary>
    /// Initializes a new instance of the TextBoxContentHost class.
    /// </summary>
    /// <param name="owner">The owning TextBoxBase control.</param>
    public TextBoxContentHost(TextBoxBase owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));

        // This element should be hit-testable so clicks are routed through it
        IsHitTestVisible = true;
    }

    /// <summary>
    /// Gets the owning TextBoxBase control.
    /// </summary>
    public TextBoxBase Owner => _owner;

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        _lastMeasureWidth = availableSize.Width;
        return _owner.MeasureTextContent(availableSize);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        // If the width we were arranged with differs from the width we were
        // last measured with, the DesiredSize we reported is based on the
        // wrong wrap width — the renderer will actually wrap to finalSize.Width
        // and may produce more (or fewer) rows than we reported. Ask for
        // another measure pass so the enclosing ScrollViewer/parent picks up
        // the correct height and the user can scroll to the end of wrapped
        // content. Guard against infinite re-measure loops with a small
        // tolerance and by only triggering when either side is finite.
        bool measureFinite = !double.IsNaN(_lastMeasureWidth) && !double.IsInfinity(_lastMeasureWidth);
        bool arrangeFinite = !double.IsInfinity(finalSize.Width);
        if ((measureFinite || arrangeFinite)
            && (double.IsInfinity(_lastMeasureWidth) || Math.Abs(_lastMeasureWidth - finalSize.Width) > 0.5))
        {
            InvalidateMeasure();
        }

        _owner.ArrangeTextContent(finalSize);
        return finalSize;
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {
        // Delegate rendering to the owner
        _owner.RenderTextContent(drawingContext);
    }

    /// <summary>
    /// Opts out of the retained-mode drawing cache.
    /// </summary>
    /// <remarks>
    /// <see cref="OnRender"/> is a pure delegator to <see cref="TextBoxBase.RenderTextContent"/>;
    /// all rendering state (caret index, selection span, scroll offset,
    /// spell-check squiggles, syntax colours, …) lives on the owner, not on
    /// this visual. The retained-mode cache keyed off this visual's own
    /// <c>_isRenderDirty</c> flag therefore cannot track the real dirty
    /// state — an <c>InvalidateVisual</c> on the owner flips only its own
    /// flag, and this proxy keeps replaying last frame's command list (e.g.
    /// the old selection rectangle). Rendering in immediate mode every
    /// frame is both correct and cheap here: the owner's draw routine is
    /// already a small fixed set of text/selection/caret primitives.
    /// </remarks>
    protected override bool ParticipatesInRenderCache => false;
}
