using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Jalium.UI.Media;

namespace Jalium.UI.Controls;

/// <summary>
/// Represents a control that displays a list of data items.
/// </summary>
public class ListView : ListBox
{
    /// <summary>
    /// Identifies the View dependency property.
    /// </summary>
    public static readonly DependencyProperty ViewProperty =
        DependencyProperty.Register(nameof(View), typeof(ViewBase), typeof(ListView),
            new PropertyMetadata(null, OnViewChanged));

    /// <summary>
    /// Gets or sets an object that defines how the data is styled and organized in a ListView control.
    /// </summary>
    public ViewBase? View
    {
        get => (ViewBase?)GetValue(ViewProperty);
        set => SetValue(ViewProperty, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListView"/> class.
    /// </summary>
    public ListView()
    {
    }

    private static void OnViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var listView = (ListView)d;
        var oldView = (ViewBase?)e.OldValue;
        var newView = (ViewBase?)e.NewValue;

        oldView?.OnViewDetached(listView);
        newView?.OnViewAttached(listView);
    }
}

/// <summary>
/// Represents an item in a ListView control.
/// </summary>
public class ListViewItem : ListBoxItem
{
    static ListViewItem()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListViewItem"/> class.
    /// </summary>
    public ListViewItem()
    {
    }
}

/// <summary>
/// Represents the base class for views that define the appearance of data in a ListView control.
/// </summary>
public abstract class ViewBase : DependencyObject
{
    /// <summary>
    /// Gets the default style key.
    /// </summary>
    protected internal virtual object DefaultStyleKey => typeof(ViewBase);

    /// <summary>
    /// Gets the item container default style key.
    /// </summary>
    protected internal virtual object ItemContainerDefaultStyleKey => typeof(ListViewItem);

    /// <summary>
    /// Called when the view is attached to a ListView.
    /// </summary>
    protected internal virtual void OnViewAttached(DependencyObject listView)
    {
    }

    /// <summary>
    /// Called when the view is detached from a ListView.
    /// </summary>
    protected internal virtual void OnViewDetached(DependencyObject listView)
    {
    }
}

/// <summary>
/// Represents a view mode that displays data items in columns for a ListView control.
/// </summary>
public class GridView : ViewBase
{
    private readonly GridViewColumnCollection _columns;

    /// <summary>
    /// Identifies the ColumnHeaderContainerStyle dependency property.
    /// </summary>
    public static readonly DependencyProperty ColumnHeaderContainerStyleProperty =
        DependencyProperty.Register(nameof(ColumnHeaderContainerStyle), typeof(Style), typeof(GridView),
            new PropertyMetadata(null));

    /// <summary>
    /// Identifies the ColumnHeaderTemplate dependency property.
    /// </summary>
    public static readonly DependencyProperty ColumnHeaderTemplateProperty =
        DependencyProperty.Register(nameof(ColumnHeaderTemplate), typeof(DataTemplate), typeof(GridView),
            new PropertyMetadata(null));

    /// <summary>
    /// Identifies the ColumnHeaderTemplateSelector dependency property.
    /// </summary>
    public static readonly DependencyProperty ColumnHeaderTemplateSelectorProperty =
        DependencyProperty.Register(nameof(ColumnHeaderTemplateSelector), typeof(DataTemplateSelector), typeof(GridView),
            new PropertyMetadata(null));

    /// <summary>
    /// Identifies the ColumnHeaderContextMenu dependency property.
    /// </summary>
    public static readonly DependencyProperty ColumnHeaderContextMenuProperty =
        DependencyProperty.Register(nameof(ColumnHeaderContextMenu), typeof(ContextMenu), typeof(GridView),
            new PropertyMetadata(null));

    /// <summary>
    /// Identifies the ColumnHeaderToolTip dependency property.
    /// </summary>
    public static readonly DependencyProperty ColumnHeaderToolTipProperty =
        DependencyProperty.Register(nameof(ColumnHeaderToolTip), typeof(object), typeof(GridView),
            new PropertyMetadata(null));

    /// <summary>
    /// Identifies the AllowsColumnReorder dependency property.
    /// </summary>
    public static readonly DependencyProperty AllowsColumnReorderProperty =
        DependencyProperty.Register(nameof(AllowsColumnReorder), typeof(bool), typeof(GridView),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets the collection of GridViewColumn objects that is defined for this GridView.
    /// </summary>
    public GridViewColumnCollection Columns => _columns;

    /// <summary>
    /// Gets or sets the style to apply to column headers.
    /// </summary>
    public Style? ColumnHeaderContainerStyle
    {
        get => (Style?)GetValue(ColumnHeaderContainerStyleProperty);
        set => SetValue(ColumnHeaderContainerStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the template to use to display the column headers.
    /// </summary>
    public DataTemplate? ColumnHeaderTemplate
    {
        get => (DataTemplate?)GetValue(ColumnHeaderTemplateProperty);
        set => SetValue(ColumnHeaderTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the selector object that provides logic for selecting a template to use for each column header.
    /// </summary>
    public DataTemplateSelector? ColumnHeaderTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(ColumnHeaderTemplateSelectorProperty);
        set => SetValue(ColumnHeaderTemplateSelectorProperty, value);
    }

    /// <summary>
    /// Gets or sets a ContextMenu for the GridView column headers.
    /// </summary>
    public ContextMenu? ColumnHeaderContextMenu
    {
        get => (ContextMenu?)GetValue(ColumnHeaderContextMenuProperty);
        set => SetValue(ColumnHeaderContextMenuProperty, value);
    }

    /// <summary>
    /// Gets or sets the content of a tooltip that appears when the mouse pointer pauses over one of the column headers.
    /// </summary>
    public object? ColumnHeaderToolTip
    {
        get => GetValue(ColumnHeaderToolTipProperty);
        set => SetValue(ColumnHeaderToolTipProperty, value);
    }

    /// <summary>
    /// Gets or sets whether columns in a GridView can be reordered by a drag-and-drop operation.
    /// </summary>
    public bool AllowsColumnReorder
    {
        get => (bool)(GetValue(AllowsColumnReorderProperty) ?? true);
        set => SetValue(AllowsColumnReorderProperty, value);
    }

    /// <inheritdoc />
    protected internal override object DefaultStyleKey => typeof(ListView);

    /// <inheritdoc />
    protected internal override object ItemContainerDefaultStyleKey => typeof(ListViewItem);

    /// <summary>
    /// Initializes a new instance of the <see cref="GridView"/> class.
    /// </summary>
    public GridView()
    {
        _columns = new GridViewColumnCollection();
    }
}

/// <summary>
/// Represents a column that displays data in a GridView.
/// </summary>
public class GridViewColumn : DependencyObject
{
    /// <summary>
    /// Identifies the Header dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(object), typeof(GridViewColumn),
            new PropertyMetadata(null));

    /// <summary>
    /// Identifies the HeaderContainerStyle dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderContainerStyleProperty =
        DependencyProperty.Register(nameof(HeaderContainerStyle), typeof(Style), typeof(GridViewColumn),
            new PropertyMetadata(null));

    /// <summary>
    /// Identifies the HeaderTemplate dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(nameof(HeaderTemplate), typeof(DataTemplate), typeof(GridViewColumn),
            new PropertyMetadata(null));

    /// <summary>
    /// Identifies the HeaderTemplateSelector dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderTemplateSelectorProperty =
        DependencyProperty.Register(nameof(HeaderTemplateSelector), typeof(DataTemplateSelector), typeof(GridViewColumn),
            new PropertyMetadata(null));

    /// <summary>
    /// Identifies the HeaderStringFormat dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderStringFormatProperty =
        DependencyProperty.Register(nameof(HeaderStringFormat), typeof(string), typeof(GridViewColumn),
            new PropertyMetadata(null));

    /// <summary>
    /// Identifies the Width dependency property.
    /// </summary>
    public static readonly DependencyProperty WidthProperty =
        DependencyProperty.Register(nameof(Width), typeof(double), typeof(GridViewColumn),
            new PropertyMetadata(double.NaN));

    /// <summary>
    /// Identifies the CellTemplate dependency property.
    /// </summary>
    public static readonly DependencyProperty CellTemplateProperty =
        DependencyProperty.Register(nameof(CellTemplate), typeof(DataTemplate), typeof(GridViewColumn),
            new PropertyMetadata(null));

    /// <summary>
    /// Identifies the CellTemplateSelector dependency property.
    /// </summary>
    public static readonly DependencyProperty CellTemplateSelectorProperty =
        DependencyProperty.Register(nameof(CellTemplateSelector), typeof(DataTemplateSelector), typeof(GridViewColumn),
            new PropertyMetadata(null));

    /// <summary>
    /// Identifies the DisplayMemberBinding dependency property.
    /// </summary>
    public static readonly DependencyProperty DisplayMemberBindingProperty =
        DependencyProperty.Register(nameof(DisplayMemberBinding), typeof(BindingBase), typeof(GridViewColumn),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the content of the header of a GridViewColumn.
    /// </summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Gets or sets the style to use for the header of the GridViewColumn.
    /// </summary>
    public Style? HeaderContainerStyle
    {
        get => (Style?)GetValue(HeaderContainerStyleProperty);
        set => SetValue(HeaderContainerStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the template to use to display the content of the column header.
    /// </summary>
    public DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets a DataTemplateSelector that provides logic for choosing the template to use to display the column header.
    /// </summary>
    public DataTemplateSelector? HeaderTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(HeaderTemplateSelectorProperty);
        set => SetValue(HeaderTemplateSelectorProperty, value);
    }

    /// <summary>
    /// Gets or sets a composite string that specifies how to format the Header property if it is displayed as a string.
    /// </summary>
    public string? HeaderStringFormat
    {
        get => (string?)GetValue(HeaderStringFormatProperty);
        set => SetValue(HeaderStringFormatProperty, value);
    }

    /// <summary>
    /// Gets or sets the width of the column.
    /// </summary>
    public double Width
    {
        get => (double)(GetValue(WidthProperty) ?? double.NaN);
        set => SetValue(WidthProperty, value);
    }

    /// <summary>
    /// Gets the actual width of a GridViewColumn.
    /// </summary>
    public double ActualWidth { get; internal set; }

    /// <summary>
    /// Gets or sets the template to use to display the contents of a column cell.
    /// </summary>
    public DataTemplate? CellTemplate
    {
        get => (DataTemplate?)GetValue(CellTemplateProperty);
        set => SetValue(CellTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets a DataTemplateSelector that determines the template to use to display cells in a column.
    /// </summary>
    public DataTemplateSelector? CellTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(CellTemplateSelectorProperty);
        set => SetValue(CellTemplateSelectorProperty, value);
    }

    /// <summary>
    /// Gets or sets the data item to bind to for this column.
    /// </summary>
    public BindingBase? DisplayMemberBinding
    {
        get => (BindingBase?)GetValue(DisplayMemberBindingProperty);
        set => SetValue(DisplayMemberBindingProperty, value);
    }
}

/// <summary>
/// Represents a collection of GridViewColumn objects.
/// </summary>
public class GridViewColumnCollection : ObservableCollection<GridViewColumn>
{
}

/// <summary>
/// Represents the header for a GridViewColumn.
/// </summary>
public class GridViewColumnHeader : ButtonBase
{
    /// <summary>
    /// Identifies the Column dependency property.
    /// </summary>
    public static readonly DependencyProperty ColumnProperty =
        DependencyProperty.Register(nameof(Column), typeof(GridViewColumn), typeof(GridViewColumnHeader),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the GridViewColumn that is associated with this header.
    /// </summary>
    public GridViewColumn? Column
    {
        get => (GridViewColumn?)GetValue(ColumnProperty);
        set => SetValue(ColumnProperty, value);
    }

    /// <summary>
    /// Gets the role of the column header.
    /// </summary>
    public GridViewColumnHeaderRole Role { get; internal set; }
}

/// <summary>
/// Defines the role of a GridViewColumnHeader control.
/// </summary>
public enum GridViewColumnHeaderRole
{
    /// <summary>
    /// The column header is not floating.
    /// </summary>
    Normal,

    /// <summary>
    /// The column header is floating.
    /// </summary>
    Floating,

    /// <summary>
    /// The column header displays a filler.
    /// </summary>
    Padding
}
