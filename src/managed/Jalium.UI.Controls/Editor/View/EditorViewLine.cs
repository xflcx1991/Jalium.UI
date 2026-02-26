namespace Jalium.UI.Controls.Editor;

/// <summary>
/// Cached rendering data for a single visible line in the editor viewport.
/// </summary>
internal sealed class EditorViewLine
{
    /// <summary>
    /// Gets the 1-based document line number.
    /// </summary>
    public int LineNumber { get; }

    /// <summary>
    /// Gets the document offset of this line's start.
    /// </summary>
    public int DocumentOffset { get; set; }

    /// <summary>
    /// Gets the length of this line's content.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// Gets or sets the Y position of this line in the viewport.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Gets or sets whether this line is collapsed due to folding.
    /// </summary>
    public bool IsCollapsed { get; set; }

    /// <summary>
    /// Gets or sets the syntax highlighting data for this line.
    /// </summary>
    public HighlightedLine? Highlighting { get; set; }

    /// <summary>
    /// Gets or sets whether highlighting needs to be recalculated.
    /// </summary>
    public bool IsHighlightingDirty { get; set; } = true;

    /// <summary>
    /// Gets the cached prefix widths (column -> pixel width) for this line.
    /// </summary>
    public Dictionary<int, double> PrefixWidths { get; } = [];

    /// <summary>
    /// Gets the line text used for prefix-width cache generation.
    /// </summary>
    public string? GeometryText { get; private set; }

    /// <summary>
    /// Gets the font family used for prefix-width cache generation.
    /// </summary>
    public string? GeometryFontFamily { get; private set; }

    /// <summary>
    /// Gets the font size used for prefix-width cache generation.
    /// </summary>
    public double GeometryFontSize { get; private set; }

    public EditorViewLine(int lineNumber)
    {
        LineNumber = lineNumber;
    }

    /// <summary>
    /// Invalidates the highlighting data.
    /// </summary>
    public void InvalidateHighlighting()
    {
        Highlighting = null;
        IsHighlightingDirty = true;
    }

    /// <summary>
    /// Invalidates cached text geometry for this line.
    /// </summary>
    public void InvalidateGeometry()
    {
        PrefixWidths.Clear();
        GeometryText = null;
        GeometryFontFamily = null;
        GeometryFontSize = 0;
    }

    /// <summary>
    /// Ensures the geometry cache context matches the current line text and font settings.
    /// </summary>
    public void EnsureGeometryContext(string lineText, string fontFamily, double fontSize)
    {
        if (string.Equals(GeometryText, lineText, StringComparison.Ordinal) &&
            string.Equals(GeometryFontFamily, fontFamily, StringComparison.Ordinal) &&
            Math.Abs(GeometryFontSize - fontSize) <= 0.001)
        {
            return;
        }

        InvalidateGeometry();
        GeometryText = lineText;
        GeometryFontFamily = fontFamily;
        GeometryFontSize = fontSize;
        PrefixWidths[0] = 0;
    }
}
