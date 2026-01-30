using Jalium.UI.Controls;
using Jalium.UI.Media;

namespace Jalium.UI.Documents;

/// <summary>
/// Abstract base class for block flow content elements.
/// </summary>
public abstract class Block : TextElement
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the Margin dependency property.
    /// </summary>
    public static readonly DependencyProperty MarginProperty =
        DependencyProperty.Register(nameof(Margin), typeof(Thickness), typeof(Block),
            new PropertyMetadata(new Thickness(0)));

    /// <summary>
    /// Identifies the Padding dependency property.
    /// </summary>
    public static readonly DependencyProperty PaddingProperty =
        DependencyProperty.Register(nameof(Padding), typeof(Thickness), typeof(Block),
            new PropertyMetadata(new Thickness(0)));

    /// <summary>
    /// Identifies the BorderThickness dependency property.
    /// </summary>
    public static readonly DependencyProperty BorderThicknessProperty =
        DependencyProperty.Register(nameof(BorderThickness), typeof(Thickness), typeof(Block),
            new PropertyMetadata(new Thickness(0)));

    /// <summary>
    /// Identifies the BorderBrush dependency property.
    /// </summary>
    public static readonly DependencyProperty BorderBrushProperty =
        DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(Block),
            new PropertyMetadata(null));

    /// <summary>
    /// Identifies the TextAlignment dependency property.
    /// </summary>
    public static readonly DependencyProperty TextAlignmentProperty =
        DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(Block),
            new PropertyMetadata(TextAlignment.Left));

    /// <summary>
    /// Identifies the LineHeight dependency property.
    /// </summary>
    public static readonly DependencyProperty LineHeightProperty =
        DependencyProperty.Register(nameof(LineHeight), typeof(double), typeof(Block),
            new PropertyMetadata(double.NaN));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the margin.
    /// </summary>
    public Thickness Margin
    {
        get => (Thickness)(GetValue(MarginProperty) ?? new Thickness(0));
        set => SetValue(MarginProperty, value);
    }

    /// <summary>
    /// Gets or sets the padding.
    /// </summary>
    public Thickness Padding
    {
        get => (Thickness)(GetValue(PaddingProperty) ?? new Thickness(0));
        set => SetValue(PaddingProperty, value);
    }

    /// <summary>
    /// Gets or sets the border thickness.
    /// </summary>
    public Thickness BorderThickness
    {
        get => (Thickness)(GetValue(BorderThicknessProperty) ?? new Thickness(0));
        set => SetValue(BorderThicknessProperty, value);
    }

    /// <summary>
    /// Gets or sets the border brush.
    /// </summary>
    public Brush? BorderBrush
    {
        get => (Brush?)GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
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
    /// Gets or sets the next sibling block.
    /// </summary>
    public Block? NextBlock { get; internal set; }

    /// <summary>
    /// Gets or sets the previous sibling block.
    /// </summary>
    public Block? PreviousBlock { get; internal set; }

    #endregion
}

/// <summary>
/// A collection of block elements.
/// </summary>
public class BlockCollection : List<Block>
{
    private readonly TextElement _parent;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockCollection"/> class.
    /// </summary>
    public BlockCollection(TextElement parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// Adds a block element to the collection.
    /// </summary>
    public new void Add(Block item)
    {
        item.Parent = _parent;
        if (Count > 0)
        {
            var last = this[Count - 1];
            last.NextBlock = item;
            item.PreviousBlock = last;
        }
        base.Add(item);
    }

    /// <summary>
    /// Removes a block element from the collection.
    /// </summary>
    public new bool Remove(Block item)
    {
        var result = base.Remove(item);
        if (result)
        {
            item.Parent = null;
            if (item.PreviousBlock != null)
                item.PreviousBlock.NextBlock = item.NextBlock;
            if (item.NextBlock != null)
                item.NextBlock.PreviousBlock = item.PreviousBlock;
            item.NextBlock = null;
            item.PreviousBlock = null;
        }
        return result;
    }

    /// <summary>
    /// Clears all block elements from the collection.
    /// </summary>
    public new void Clear()
    {
        foreach (var item in this)
        {
            item.Parent = null;
            item.NextBlock = null;
            item.PreviousBlock = null;
        }
        base.Clear();
    }
}

/// <summary>
/// A block element that contains inline content.
/// </summary>
public class Paragraph : Block
{
    /// <summary>
    /// Identifies the TextIndent dependency property.
    /// </summary>
    public static readonly DependencyProperty TextIndentProperty =
        DependencyProperty.Register(nameof(TextIndent), typeof(double), typeof(Paragraph),
            new PropertyMetadata(0.0));

    /// <summary>
    /// Gets the collection of inline elements.
    /// </summary>
    public InlineCollection Inlines { get; }

    /// <summary>
    /// Gets or sets the text indentation for the first line.
    /// </summary>
    public double TextIndent
    {
        get => (double)(GetValue(TextIndentProperty) ?? 0.0);
        set => SetValue(TextIndentProperty, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Paragraph"/> class.
    /// </summary>
    public Paragraph()
    {
        Inlines = new InlineCollection(this);
        Margin = new Thickness(0, 0, 0, 10);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Paragraph"/> class with the specified inline.
    /// </summary>
    public Paragraph(Inline inline) : this()
    {
        Inlines.Add(inline);
    }
}

/// <summary>
/// A block element that groups other blocks with a visual boundary.
/// </summary>
public class Section : Block
{
    /// <summary>
    /// Gets the collection of block elements.
    /// </summary>
    public BlockCollection Blocks { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Section"/> class.
    /// </summary>
    public Section()
    {
        Blocks = new BlockCollection(this);
    }
}

/// <summary>
/// A block element that represents a list.
/// </summary>
public class List : Block
{
    /// <summary>
    /// Identifies the MarkerStyle dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkerStyleProperty =
        DependencyProperty.Register(nameof(MarkerStyle), typeof(TextMarkerStyle), typeof(List),
            new PropertyMetadata(TextMarkerStyle.Disc));

    /// <summary>
    /// Identifies the StartIndex dependency property.
    /// </summary>
    public static readonly DependencyProperty StartIndexProperty =
        DependencyProperty.Register(nameof(StartIndex), typeof(int), typeof(List),
            new PropertyMetadata(1));

    /// <summary>
    /// Gets the collection of list items.
    /// </summary>
    public ListItemCollection ListItems { get; }

    /// <summary>
    /// Gets or sets the marker style.
    /// </summary>
    public TextMarkerStyle MarkerStyle
    {
        get => (TextMarkerStyle)(GetValue(MarkerStyleProperty) ?? TextMarkerStyle.Disc);
        set => SetValue(MarkerStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the starting index for numbered lists.
    /// </summary>
    public int StartIndex
    {
        get => (int)(GetValue(StartIndexProperty) ?? 1);
        set => SetValue(StartIndexProperty, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="List"/> class.
    /// </summary>
    public List()
    {
        ListItems = new ListItemCollection(this);
        Margin = new Thickness(0, 0, 0, 10);
    }
}

/// <summary>
/// A collection of list items.
/// </summary>
public class ListItemCollection : List<ListItem>
{
    private readonly List _parent;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListItemCollection"/> class.
    /// </summary>
    public ListItemCollection(List parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// Adds a list item to the collection.
    /// </summary>
    public new void Add(ListItem item)
    {
        item.Parent = _parent;
        base.Add(item);
    }
}

/// <summary>
/// A list item element.
/// </summary>
public class ListItem : TextElement
{
    /// <summary>
    /// Gets the collection of block elements.
    /// </summary>
    public BlockCollection Blocks { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListItem"/> class.
    /// </summary>
    public ListItem()
    {
        Blocks = new BlockCollection(this);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListItem"/> class with a paragraph.
    /// </summary>
    public ListItem(Paragraph paragraph) : this()
    {
        Blocks.Add(paragraph);
    }
}

/// <summary>
/// Specifies the marker style for list items.
/// </summary>
public enum TextMarkerStyle
{
    None,
    Disc,
    Circle,
    Square,
    Box,
    LowerRoman,
    UpperRoman,
    LowerLatin,
    UpperLatin,
    Decimal
}

/// <summary>
/// A block element that represents a block UI container.
/// </summary>
public class BlockUIContainer : Block
{
    /// <summary>
    /// Gets or sets the child UI element.
    /// </summary>
    public UIElement? Child { get; set; }
}
