using Jalium.UI.Input;

namespace Jalium.UI.Controls;

/// <summary>
/// Represents a control that displays a list of items and allows the user to select one or more.
/// </summary>
public class ListBox : Selector
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the SelectionMode dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectionModeProperty =
        DependencyProperty.Register(nameof(SelectionMode), typeof(SelectionMode), typeof(ListBox),
            new PropertyMetadata(SelectionMode.Single));

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the selection mode.
    /// </summary>
    public SelectionMode SelectionMode
    {
        get => (SelectionMode)(GetValue(SelectionModeProperty) ?? SelectionMode.Single);
        set => SetValue(SelectionModeProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ListBox"/> class.
    /// </summary>
    public ListBox()
    {
        // Register input event handlers
        AddHandler(MouseDownEvent, new RoutedEventHandler(OnMouseDownHandler));
        AddHandler(KeyDownEvent, new RoutedEventHandler(OnKeyDownHandler));
    }

    #endregion

    #region Item Container

    /// <inheritdoc />
    protected override FrameworkElement GetContainerForItem(object item)
    {
        return new ListBoxItem();
    }

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainer(object item)
    {
        return item is ListBoxItem;
    }

    /// <inheritdoc />
    protected override void PrepareContainerForItem(FrameworkElement element, object item)
    {
        base.PrepareContainerForItem(element, item);

        if (element is ListBoxItem listBoxItem)
        {
            listBoxItem.Content = item;
            listBoxItem.ContentTemplate = ItemTemplate;
            listBoxItem.ParentListBox = this;

            // Set selection state
            listBoxItem.IsSelected = item == SelectedItem;
        }
    }

    #endregion

    #region Selection

    internal void SelectItem(ListBoxItem item)
    {
        var content = item.Content;
        var oldSelectedItem = SelectedItem;

        if (SelectionMode == SelectionMode.Single)
        {
            // Deselect all other items
            if (ItemsHost != null)
            {
                foreach (var child in ItemsHost.Children)
                {
                    if (child is ListBoxItem lbi && lbi != item)
                    {
                        lbi.IsSelected = false;
                    }
                }
            }
        }

        item.IsSelected = true;
        SelectedItem = content;
    }

    /// <inheritdoc />
    protected override void UpdateContainerSelection()
    {
        if (ItemsHost == null) return;

        var index = 0;
        foreach (var child in ItemsHost.Children)
        {
            if (child is ListBoxItem lbi)
            {
                lbi.IsSelected = (index == SelectedIndex) || (lbi.Content == SelectedItem);
            }
            index++;
        }
    }

    #endregion

    #region Input Handling

    private void OnMouseDownHandler(object sender, RoutedEventArgs e)
    {
        Focus();
    }

    private void OnKeyDownHandler(object sender, RoutedEventArgs e)
    {
        if (e is KeyEventArgs keyArgs)
        {
            var handled = false;
            var itemCount = GetItemCount();

            switch (keyArgs.Key)
            {
                case Key.Up:
                    if (SelectedIndex > 0)
                    {
                        SelectedIndex--;
                        handled = true;
                    }
                    break;

                case Key.Down:
                    if (SelectedIndex < itemCount - 1)
                    {
                        SelectedIndex++;
                        handled = true;
                    }
                    break;

                case Key.Home:
                    if (itemCount > 0)
                    {
                        SelectedIndex = 0;
                        handled = true;
                    }
                    break;

                case Key.End:
                    if (itemCount > 0)
                    {
                        SelectedIndex = itemCount - 1;
                        handled = true;
                    }
                    break;
            }

            if (handled)
            {
                e.Handled = true;
            }
        }
    }

    #endregion
}

/// <summary>
/// Represents a selectable item in a ListBox.
/// </summary>
public class ListBoxItem : ContentControl
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the IsSelected dependency property.
    /// </summary>
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(ListBoxItem),
            new PropertyMetadata(false, OnIsSelectedChanged));

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets whether the item is selected.
    /// </summary>
    public bool IsSelected
    {
        get => (bool)(GetValue(IsSelectedProperty) ?? false);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>
    /// Gets or sets the parent ListBox.
    /// </summary>
    internal ListBox? ParentListBox { get; set; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ListBoxItem"/> class.
    /// </summary>
    public ListBoxItem()
    {
        Focusable = true;
        Height = 32;
        Padding = new Thickness(8, 4, 8, 4);

        // Register input event handlers
        AddHandler(MouseDownEvent, new RoutedEventHandler(OnMouseDownHandler));
        AddHandler(MouseEnterEvent, new RoutedEventHandler(OnMouseEnterHandler));
    }

    #endregion

    #region Input Handling

    private void OnMouseDownHandler(object sender, RoutedEventArgs e)
    {
        if (!IsEnabled) return;

        if (e is MouseButtonEventArgs mouseArgs && mouseArgs.ChangedButton == MouseButton.Left)
        {
            Focus();
            // Select immediately on mouse down (standard ListBox behavior)
            ParentListBox?.SelectItem(this);
            e.Handled = true;
        }
    }

    private void OnMouseEnterHandler(object sender, RoutedEventArgs e)
    {
        // If left mouse button is down while entering, select this item (drag selection)
        if (e is MouseEventArgs mouseArgs && mouseArgs.LeftButton == MouseButtonState.Pressed)
        {
            ParentListBox?.SelectItem(this);
        }
    }

    #endregion

    #region Property Changed

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Triggers handle visual changes now
    }

    #endregion
}
