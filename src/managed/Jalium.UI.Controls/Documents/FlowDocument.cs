using Jalium.UI.Controls;
using Jalium.UI.Media;

namespace Jalium.UI.Documents;

/// <summary>
/// Represents a flow document that hosts rich flow content.
/// </summary>
public class FlowDocument : DependencyObject
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the FontFamily dependency property.
    /// </summary>
    public static readonly DependencyProperty FontFamilyProperty =
        DependencyProperty.Register(nameof(FontFamily), typeof(string), typeof(FlowDocument),
            new PropertyMetadata("Segoe UI"));

    /// <summary>
    /// Identifies the FontSize dependency property.
    /// </summary>
    public static readonly DependencyProperty FontSizeProperty =
        DependencyProperty.Register(nameof(FontSize), typeof(double), typeof(FlowDocument),
            new PropertyMetadata(14.0));

    /// <summary>
    /// Identifies the Foreground dependency property.
    /// </summary>
    public static readonly DependencyProperty ForegroundProperty =
        DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(FlowDocument),
            new PropertyMetadata(null));

    /// <summary>
    /// Identifies the Background dependency property.
    /// </summary>
    public static readonly DependencyProperty BackgroundProperty =
        DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(FlowDocument),
            new PropertyMetadata(null));

    /// <summary>
    /// Identifies the PageWidth dependency property.
    /// </summary>
    public static readonly DependencyProperty PageWidthProperty =
        DependencyProperty.Register(nameof(PageWidth), typeof(double), typeof(FlowDocument),
            new PropertyMetadata(double.NaN));

    /// <summary>
    /// Identifies the PageHeight dependency property.
    /// </summary>
    public static readonly DependencyProperty PageHeightProperty =
        DependencyProperty.Register(nameof(PageHeight), typeof(double), typeof(FlowDocument),
            new PropertyMetadata(double.NaN));

    /// <summary>
    /// Identifies the PagePadding dependency property.
    /// </summary>
    public static readonly DependencyProperty PagePaddingProperty =
        DependencyProperty.Register(nameof(PagePadding), typeof(Thickness), typeof(FlowDocument),
            new PropertyMetadata(new Thickness(0)));

    /// <summary>
    /// Identifies the ColumnWidth dependency property.
    /// </summary>
    public static readonly DependencyProperty ColumnWidthProperty =
        DependencyProperty.Register(nameof(ColumnWidth), typeof(double), typeof(FlowDocument),
            new PropertyMetadata(double.NaN));

    /// <summary>
    /// Identifies the ColumnGap dependency property.
    /// </summary>
    public static readonly DependencyProperty ColumnGapProperty =
        DependencyProperty.Register(nameof(ColumnGap), typeof(double), typeof(FlowDocument),
            new PropertyMetadata(10.0));

    /// <summary>
    /// Identifies the TextAlignment dependency property.
    /// </summary>
    public static readonly DependencyProperty TextAlignmentProperty =
        DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(FlowDocument),
            new PropertyMetadata(TextAlignment.Left));

    /// <summary>
    /// Identifies the LineHeight dependency property.
    /// </summary>
    public static readonly DependencyProperty LineHeightProperty =
        DependencyProperty.Register(nameof(LineHeight), typeof(double), typeof(FlowDocument),
            new PropertyMetadata(double.NaN));

    /// <summary>
    /// Identifies the IsOptimalParagraphEnabled dependency property.
    /// </summary>
    public static readonly DependencyProperty IsOptimalParagraphEnabledProperty =
        DependencyProperty.Register(nameof(IsOptimalParagraphEnabled), typeof(bool), typeof(FlowDocument),
            new PropertyMetadata(false));

    /// <summary>
    /// Identifies the IsHyphenationEnabled dependency property.
    /// </summary>
    public static readonly DependencyProperty IsHyphenationEnabledProperty =
        DependencyProperty.Register(nameof(IsHyphenationEnabled), typeof(bool), typeof(FlowDocument),
            new PropertyMetadata(false));

    #endregion

    #region Properties

    /// <summary>
    /// Gets the collection of block elements.
    /// </summary>
    public BlockCollection Blocks { get; }

    /// <summary>
    /// Gets or sets the font family.
    /// </summary>
    public string FontFamily
    {
        get => (string)(GetValue(FontFamilyProperty) ?? "Segoe UI");
        set => SetValue(FontFamilyProperty, value);
    }

    /// <summary>
    /// Gets or sets the font size.
    /// </summary>
    public double FontSize
    {
        get => (double)(GetValue(FontSizeProperty) ?? 14.0);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the foreground brush.
    /// </summary>
    public Brush? Foreground
    {
        get => (Brush?)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the background brush.
    /// </summary>
    public Brush? Background
    {
        get => (Brush?)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the page width.
    /// </summary>
    public double PageWidth
    {
        get => (double)(GetValue(PageWidthProperty) ?? double.NaN);
        set => SetValue(PageWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the page height.
    /// </summary>
    public double PageHeight
    {
        get => (double)(GetValue(PageHeightProperty) ?? double.NaN);
        set => SetValue(PageHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the page padding.
    /// </summary>
    public Thickness PagePadding
    {
        get => (Thickness)(GetValue(PagePaddingProperty) ?? new Thickness(0));
        set => SetValue(PagePaddingProperty, value);
    }

    /// <summary>
    /// Gets or sets the column width.
    /// </summary>
    public double ColumnWidth
    {
        get => (double)(GetValue(ColumnWidthProperty) ?? double.NaN);
        set => SetValue(ColumnWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the gap between columns.
    /// </summary>
    public double ColumnGap
    {
        get => (double)(GetValue(ColumnGapProperty) ?? 10.0);
        set => SetValue(ColumnGapProperty, value);
    }

    /// <summary>
    /// Gets or sets the text alignment.
    /// </summary>
    public TextAlignment TextAlignment
    {
        get => (TextAlignment)(GetValue(TextAlignmentProperty) ?? TextAlignment.Left);
        set => SetValue(TextAlignmentProperty, value);
    }

    /// <summary>
    /// Gets or sets the line height.
    /// </summary>
    public double LineHeight
    {
        get => (double)(GetValue(LineHeightProperty) ?? double.NaN);
        set => SetValue(LineHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets whether optimal paragraph layout is enabled.
    /// </summary>
    public bool IsOptimalParagraphEnabled
    {
        get => (bool)(GetValue(IsOptimalParagraphEnabledProperty) ?? false);
        set => SetValue(IsOptimalParagraphEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets whether hyphenation is enabled.
    /// </summary>
    public bool IsHyphenationEnabled
    {
        get => (bool)(GetValue(IsHyphenationEnabledProperty) ?? false);
        set => SetValue(IsHyphenationEnabledProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="FlowDocument"/> class.
    /// </summary>
    public FlowDocument()
    {
        Blocks = new BlockCollection(null!);
        Foreground = new SolidColorBrush(Color.Black);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FlowDocument"/> class with a block.
    /// </summary>
    public FlowDocument(Block block) : this()
    {
        Blocks.Add(block);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets the plain text content of the document.
    /// </summary>
    public string GetText()
    {
        var sb = new System.Text.StringBuilder();
        AppendBlocksText(Blocks, sb);
        return sb.ToString();
    }

    private void AppendBlocksText(IEnumerable<Block> blocks, System.Text.StringBuilder sb)
    {
        foreach (var block in blocks)
        {
            AppendBlockText(block, sb);
        }
    }

    private void AppendBlockText(Block block, System.Text.StringBuilder sb)
    {
        switch (block)
        {
            case Paragraph paragraph:
                AppendInlinesText(paragraph.Inlines, sb);
                sb.AppendLine();
                break;

            case Section section:
                AppendBlocksText(section.Blocks, sb);
                break;

            case List list:
                foreach (var item in list.ListItems)
                {
                    AppendBlocksText(item.Blocks, sb);
                }
                break;

            case Table table:
                foreach (var rowGroup in table.RowGroups)
                {
                    foreach (var row in rowGroup.Rows)
                    {
                        foreach (var cell in row.Cells)
                        {
                            AppendBlocksText(cell.Blocks, sb);
                            sb.Append('\t');
                        }
                        sb.AppendLine();
                    }
                }
                break;
        }
    }

    private void AppendInlinesText(IEnumerable<Inline> inlines, System.Text.StringBuilder sb)
    {
        foreach (var inline in inlines)
        {
            AppendInlineText(inline, sb);
        }
    }

    private void AppendInlineText(Inline inline, System.Text.StringBuilder sb)
    {
        switch (inline)
        {
            case Run run:
                sb.Append(run.Text);
                break;

            case Span span:
                AppendInlinesText(span.Inlines, sb);
                break;

            case LineBreak:
                sb.AppendLine();
                break;
        }
    }

    /// <summary>
    /// Creates a simple FlowDocument from plain text.
    /// </summary>
    public static FlowDocument FromText(string text)
    {
        var doc = new FlowDocument();

        if (string.IsNullOrEmpty(text))
            return doc;

        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        foreach (var line in lines)
        {
            var paragraph = new Paragraph(new Run(line));
            doc.Blocks.Add(paragraph);
        }

        return doc;
    }

    #endregion
}
