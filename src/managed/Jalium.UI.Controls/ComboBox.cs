using Jalium.UI.Input;
using Jalium.UI.Interop;
using Jalium.UI.Media;

namespace Jalium.UI.Controls;

/// <summary>
/// Represents a selection control with a drop-down list that can be shown or hidden by clicking the control.
/// </summary>
public class ComboBox : Selector
{
    private Popup? _popup;
    private ToggleButton? _toggleButton;
    private StackPanel? _itemsPanel;
    private bool _isDropDownOpen;

    #region Dependency Properties

    /// <summary>
    /// Identifies the IsDropDownOpen dependency property.
    /// </summary>
    public static readonly DependencyProperty IsDropDownOpenProperty =
        DependencyProperty.Register(nameof(IsDropDownOpen), typeof(bool), typeof(ComboBox),
            new PropertyMetadata(false, OnIsDropDownOpenChanged));

    /// <summary>
    /// Identifies the MaxDropDownHeight dependency property.
    /// </summary>
    public static readonly DependencyProperty MaxDropDownHeightProperty =
        DependencyProperty.Register(nameof(MaxDropDownHeight), typeof(double), typeof(ComboBox),
            new PropertyMetadata(200.0));

    /// <summary>
    /// Identifies the PlaceholderText dependency property.
    /// </summary>
    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(ComboBox),
            new PropertyMetadata("Select an item..."));

    /// <summary>
    /// Identifies the SelectionBoxItem dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectionBoxItemProperty =
        DependencyProperty.Register(nameof(SelectionBoxItem), typeof(object), typeof(ComboBox),
            new PropertyMetadata(null, OnSelectionBoxItemChanged));

    private static void OnSelectionBoxItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ComboBox comboBox)
        {
            // Trigger UI update when selection box item changes
            comboBox.InvalidateVisual();
        }
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets whether the dropdown is open.
    /// </summary>
    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum height of the dropdown.
    /// </summary>
    public double MaxDropDownHeight
    {
        get => (double)GetValue(MaxDropDownHeightProperty);
        set => SetValue(MaxDropDownHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the placeholder text shown when no item is selected.
    /// </summary>
    public string PlaceholderText
    {
        get => (string)(GetValue(PlaceholderTextProperty) ?? "Select an item...");
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>
    /// Gets the item displayed in the selection box.
    /// </summary>
    public object? SelectionBoxItem
    {
        get => GetValue(SelectionBoxItemProperty);
        private set => SetValue(SelectionBoxItemProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Occurs when the dropdown opens.
    /// </summary>
    public event EventHandler? DropDownOpened;

    /// <summary>
    /// Occurs when the dropdown closes.
    /// </summary>
    public event EventHandler? DropDownClosed;

    #endregion

    public ComboBox()
    {
        // Default values - will be overridden by style
        MinHeight = 32;
        MinWidth = 120;

        // Initialize SelectionBoxItem with placeholder
        SelectionBoxItem = PlaceholderText;

        // Set up mouse handling for toggle
        AddHandler(MouseDownEvent, new RoutedEventHandler(OnMouseDownHandler));
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Unhook old events
        if (_toggleButton != null)
        {
            _toggleButton.Checked -= OnToggleButtonChecked;
            _toggleButton.Unchecked -= OnToggleButtonUnchecked;
        }

        if (_popup != null)
        {
            _popup.Closed -= OnPopupClosed;
        }

        // Get template parts
        _toggleButton = GetTemplateChild("PART_ToggleButton") as ToggleButton;
        _popup = GetTemplateChild("PART_Popup") as Popup;

        // Wire up toggle button
        if (_toggleButton != null)
        {
            _toggleButton.Checked += OnToggleButtonChecked;
            _toggleButton.Unchecked += OnToggleButtonUnchecked;
        }

        // Wire up popup
        if (_popup != null)
        {
            _popup.Closed += OnPopupClosed;
        }

        // Update selection box
        UpdateSelectionBoxItem();
    }

    private void OnToggleButtonChecked(object? sender, RoutedEventArgs e)
    {
        IsDropDownOpen = true;
    }

    private void OnToggleButtonUnchecked(object? sender, RoutedEventArgs e)
    {
        IsDropDownOpen = false;
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        _isDropDownOpen = false;
        SetValue(IsDropDownOpenProperty, false);
    }

    private void OnMouseDownHandler(object sender, RoutedEventArgs e)
    {
        // Toggle dropdown
        IsDropDownOpen = !IsDropDownOpen;
        e.Handled = true;
    }

    private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ComboBox comboBox)
        {
            var isOpen = (bool)e.NewValue;
            if (isOpen)
            {
                comboBox.OpenDropDown();
            }
            else
            {
                comboBox.CloseDropDown();
            }
        }
    }

    /// <inheritdoc />
    protected override void UpdateContainerSelection()
    {
        UpdateSelectionBoxItem();
    }

    private void UpdateSelectionBoxItem()
    {
        var selectedItem = SelectedItem;
        if (selectedItem != null)
        {
            SelectionBoxItem = GetItemText(selectedItem);
        }
        else
        {
            SelectionBoxItem = PlaceholderText;
        }
    }

    private void OpenDropDown()
    {
        if (_isDropDownOpen) return;

        // Populate dropdown items if we have an items panel
        PopulateDropdownItems();

        if (_popup != null)
        {
            _popup.IsOpen = true;
        }

        _isDropDownOpen = true;

        if (_toggleButton != null)
        {
            _toggleButton.IsChecked = true;
        }

        DropDownOpened?.Invoke(this, EventArgs.Empty);
    }

    private void CloseDropDown()
    {
        if (!_isDropDownOpen) return;

        if (_popup != null)
        {
            _popup.IsOpen = false;
        }

        _isDropDownOpen = false;

        if (_toggleButton != null)
        {
            _toggleButton.IsChecked = false;
        }

        DropDownClosed?.Invoke(this, EventArgs.Empty);
    }

    private void PopulateDropdownItems()
    {
        // Find the items panel in the popup
        if (_popup?.Child is Border border)
        {
            // Look for ScrollViewer containing ItemsPresenter or StackPanel
            if (border.Child is ScrollViewer scrollViewer)
            {
                if (scrollViewer.Content is StackPanel panel)
                {
                    _itemsPanel = panel;
                }
            }
        }

        if (_itemsPanel == null)
        {
            // Create items panel if not found in template
            _itemsPanel = new StackPanel();

            if (_popup?.Child is Border b && b.Child is ScrollViewer sv)
            {
                sv.Content = _itemsPanel;
            }
        }

        _itemsPanel.Children.Clear();

        for (int i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            var index = i;

            var itemContainer = new ComboBoxItem
            {
                Content = GetItemText(item),
                Tag = item,
                IsSelected = item == SelectedItem
            };

            itemContainer.ItemClicked += (s, e) =>
            {
                SelectedIndex = index;
                SelectedItem = item;
                CloseDropDown();
            };

            _itemsPanel.Children.Add(itemContainer);
        }
    }

    private string GetItemText(object? item)
    {
        if (item == null) return string.Empty;

        // If item is a ComboBoxItem, get its content
        if (item is ComboBoxItem cbi)
        {
            return cbi.Content?.ToString() ?? string.Empty;
        }

        return item.ToString() ?? string.Empty;
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        // ComboBox needs to respect MinHeight for proper sizing
        var baseSize = base.MeasureOverride(availableSize);

        // If base returned infinite height (template has no height constraint),
        // use MinHeight instead. ComboBox is a fixed-height control.
        var height = baseSize.Height;
        if (double.IsInfinity(height) || double.IsNaN(height))
        {
            height = MinHeight;
        }
        else
        {
            height = Math.Max(height, MinHeight);
        }

        return new Size(baseSize.Width, height);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        // Ensure MinHeight is respected in arrange
        var arrangedSize = base.ArrangeOverride(finalSize);

        var height = arrangedSize.Height;
        if (double.IsInfinity(height) || double.IsNaN(height))
        {
            height = MinHeight;
        }
        else
        {
            height = Math.Max(height, MinHeight);
        }

        return new Size(arrangedSize.Width, height);
    }
}

/// <summary>
/// Represents an item in a ComboBox.
/// </summary>
public class ComboBoxItem : ContentControl
{
    private bool _isMouseOver;
    private bool _isPressed;

    /// <summary>
    /// Gets or sets whether this item is selected.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Occurs when the item is clicked.
    /// </summary>
    public event EventHandler? ItemClicked;

    /// <summary>
    /// Directly invokes the click action - used by Popup for reliable click handling.
    /// </summary>
    internal void InvokeClick()
    {
        if (IsEnabled)
        {
            ItemClicked?.Invoke(this, EventArgs.Empty);
        }
    }

    public ComboBoxItem()
    {
        Background = new SolidColorBrush(Color.Transparent);
        Foreground = new SolidColorBrush(Color.White);
        Padding = new Thickness(8, 4, 8, 4);
        MinHeight = 28;

        // Set up mouse event handlers
        AddHandler(MouseEnterEvent, new RoutedEventHandler(OnMouseEnterHandler));
        AddHandler(MouseLeaveEvent, new RoutedEventHandler(OnMouseLeaveHandler));
        AddHandler(MouseDownEvent, new RoutedEventHandler(OnMouseDownHandler));
        AddHandler(MouseUpEvent, new RoutedEventHandler(OnMouseUpHandler));
    }

    private void OnMouseEnterHandler(object sender, RoutedEventArgs e)
    {
        _isMouseOver = true;
        // State change handled by style triggers via IsMouseOver property
        InvalidateVisual();
    }

    private void OnMouseLeaveHandler(object sender, RoutedEventArgs e)
    {
        _isMouseOver = false;
        // State change handled by style triggers via IsMouseOver property
        InvalidateVisual();
    }

    private void OnMouseDownHandler(object sender, RoutedEventArgs e)
    {
        if (!IsEnabled) return;

        if (e is MouseButtonEventArgs mouseArgs && mouseArgs.ChangedButton == MouseButton.Left)
        {
            _isPressed = true;
            e.Handled = true;
        }
    }

    private void OnMouseUpHandler(object sender, RoutedEventArgs e)
    {
        if (!IsEnabled) return;

        if (e is MouseButtonEventArgs mouseArgs && mouseArgs.ChangedButton == MouseButton.Left)
        {
            // Fire click if mouse was pressed on this item and is still over it
            // In popup context, we simplify by just checking wasPressed since
            // the user clicking on the same item they pressed is the expected behavior
            if (_isPressed)
            {
                _isPressed = false;
                ItemClicked?.Invoke(this, EventArgs.Empty);
            }
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnLostMouseCapture()
    {
        base.OnLostMouseCapture();
        if (_isPressed)
        {
            _isPressed = false;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(availableSize.Width, MinHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        return finalSize;
    }

    protected override void OnRender(object drawingContextObj)
    {
        if (drawingContextObj is not DrawingContext dc)
        {
            base.OnRender(drawingContextObj);
            return;
        }

        var bounds = new Rect(0, 0, ActualWidth, ActualHeight);

        // Draw background
        if (Background != null)
        {
            dc.DrawRectangle(Background, null, bounds);
        }

        // Draw content text
        var contentText = Content?.ToString() ?? "";
        if (!string.IsNullOrEmpty(contentText))
        {
            var fontMetrics = TextMeasurement.GetFontMetrics("Segoe UI", 14);
            var text = new FormattedText(contentText, "Segoe UI", 14)
            {
                Foreground = Foreground ?? new SolidColorBrush(Color.White),
                MaxTextWidth = ActualWidth - Padding.Left - Padding.Right,
                MaxTextHeight = ActualHeight - Padding.Top - Padding.Bottom
            };

            var textY = (ActualHeight - fontMetrics.LineHeight) / 2;
            dc.DrawText(text, new Point(Padding.Left, textY));
        }

        // Note: Do not call base.OnRender - ComboBoxItem handles all rendering directly
    }
}
