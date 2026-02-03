using Jalium.UI.Input;
using Jalium.UI.Media;

namespace Jalium.UI.Controls;

/// <summary>
/// Represents a switch that can be toggled between on and off states.
/// </summary>
public class ToggleSwitch : Control
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the IsOn dependency property.
    /// </summary>
    public static readonly DependencyProperty IsOnProperty =
        DependencyProperty.Register(nameof(IsOn), typeof(bool), typeof(ToggleSwitch),
            new PropertyMetadata(false, OnIsOnChanged));

    /// <summary>
    /// Identifies the OnContent dependency property.
    /// </summary>
    public static readonly DependencyProperty OnContentProperty =
        DependencyProperty.Register(nameof(OnContent), typeof(object), typeof(ToggleSwitch),
            new PropertyMetadata("On", OnVisualPropertyChanged));

    /// <summary>
    /// Identifies the OffContent dependency property.
    /// </summary>
    public static readonly DependencyProperty OffContentProperty =
        DependencyProperty.Register(nameof(OffContent), typeof(object), typeof(ToggleSwitch),
            new PropertyMetadata("Off", OnVisualPropertyChanged));

    /// <summary>
    /// Identifies the Header dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(object), typeof(ToggleSwitch),
            new PropertyMetadata(null, OnLayoutPropertyChanged));

    /// <summary>
    /// Identifies the OnBackground dependency property.
    /// </summary>
    public static readonly DependencyProperty OnBackgroundProperty =
        DependencyProperty.Register(nameof(OnBackground), typeof(Brush), typeof(ToggleSwitch),
            new PropertyMetadata(null, OnVisualPropertyChanged));

    /// <summary>
    /// Identifies the OffBackground dependency property.
    /// </summary>
    public static readonly DependencyProperty OffBackgroundProperty =
        DependencyProperty.Register(nameof(OffBackground), typeof(Brush), typeof(ToggleSwitch),
            new PropertyMetadata(null, OnVisualPropertyChanged));

    #endregion

    #region Routed Events

    /// <summary>
    /// Identifies the Toggled routed event.
    /// </summary>
    public static readonly RoutedEvent ToggledEvent =
        EventManager.RegisterRoutedEvent(nameof(Toggled), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(ToggleSwitch));

    /// <summary>
    /// Occurs when the switch is toggled.
    /// </summary>
    public event RoutedEventHandler Toggled
    {
        add => AddHandler(ToggledEvent, value);
        remove => RemoveHandler(ToggledEvent, value);
    }

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets a value indicating whether the switch is on.
    /// </summary>
    public bool IsOn
    {
        get => (bool)(GetValue(IsOnProperty) ?? false);
        set => SetValue(IsOnProperty, value);
    }

    /// <summary>
    /// Gets or sets the content to display when the switch is on.
    /// </summary>
    public object? OnContent
    {
        get => GetValue(OnContentProperty);
        set => SetValue(OnContentProperty, value);
    }

    /// <summary>
    /// Gets or sets the content to display when the switch is off.
    /// </summary>
    public object? OffContent
    {
        get => GetValue(OffContentProperty);
        set => SetValue(OffContentProperty, value);
    }

    /// <summary>
    /// Gets or sets the header content.
    /// </summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Gets or sets the background brush for the on state.
    /// </summary>
    public Brush? OnBackground
    {
        get => (Brush?)GetValue(OnBackgroundProperty);
        set => SetValue(OnBackgroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the background brush for the off state.
    /// </summary>
    public Brush? OffBackground
    {
        get => (Brush?)GetValue(OffBackgroundProperty);
        set => SetValue(OffBackgroundProperty, value);
    }

    #endregion

    #region Private Fields

    private const double SwitchWidth = 44;
    private const double SwitchHeight = 20;
    private const double ThumbSize = 14;
    private const double ThumbMargin = 3;

    #endregion

    #region Template Parts

    private Border? _switchTrack;
    private Border? _switchThumb;
    private ContentPresenter? _contentPresenter;
    private ContentPresenter? _headerPresenter;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ToggleSwitch"/> class.
    /// </summary>
    public ToggleSwitch()
    {
        Focusable = true;
        Height = 32;

        // Register input handlers
        AddHandler(MouseDownEvent, new RoutedEventHandler(OnMouseDownHandler));
        AddHandler(KeyDownEvent, new RoutedEventHandler(OnKeyDownHandler));
    }

    #endregion

    #region Template

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _switchTrack = GetTemplateChild("PART_SwitchTrack") as Border;
        _switchThumb = GetTemplateChild("PART_SwitchThumb") as Border;
        _contentPresenter = GetTemplateChild("PART_ContentPresenter") as ContentPresenter;
        _headerPresenter = GetTemplateChild("PART_Header") as ContentPresenter;

        UpdateSwitchContent();
    }

    private void UpdateSwitchContent()
    {
        if (_contentPresenter != null)
        {
            _contentPresenter.Content = IsOn ? OnContent : OffContent;
        }
        if (_headerPresenter != null)
        {
            _headerPresenter.Visibility = Header != null ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    #endregion

    #region Input Handling

    private void OnMouseDownHandler(object sender, RoutedEventArgs e)
    {
        if (!IsEnabled) return;

        if (e is MouseButtonEventArgs mouseArgs && mouseArgs.ChangedButton == MouseButton.Left)
        {
            Focus();
            Toggle();
            e.Handled = true;
        }
    }

    private void OnKeyDownHandler(object sender, RoutedEventArgs e)
    {
        if (!IsEnabled) return;

        if (e is KeyEventArgs keyArgs)
        {
            if (keyArgs.Key == Key.Space || keyArgs.Key == Key.Enter)
            {
                Toggle();
                e.Handled = true;
            }
        }
    }

    private void Toggle()
    {
        IsOn = !IsOn;
    }

    #endregion

    #region Layout

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;
        var totalWidth = SwitchWidth + padding.TotalWidth;
        var totalHeight = SwitchHeight + padding.TotalHeight;

        // Add space for header if present
        if (Header != null)
        {
            totalHeight += 20; // Additional space for header
        }

        // Add space for on/off content
        var contentWidth = 0.0;
        var content = IsOn ? OnContent : OffContent;
        if (content is string text && !string.IsNullOrEmpty(text))
        {
            var formattedText = new FormattedText(text, FontFamily ?? "Segoe UI", FontSize > 0 ? FontSize : 14);
            Interop.TextMeasurement.MeasureText(formattedText);
            contentWidth = formattedText.Width + 8; // Gap between switch and content
        }

        totalWidth += contentWidth;

        return new Size(totalWidth, totalHeight);
    }

    #endregion

    #region Rendering

    /// <inheritdoc />
    protected override void OnRender(object drawingContext)
    {
        // If using template, let the template handle rendering
        if (_switchTrack != null)
        {
            UpdateSwitchContent();
            return;
        }

        if (drawingContext is not DrawingContext dc)
            return;

        var padding = Padding;
        var startY = padding.Top;

        // Draw header if present
        if (Header is string headerText && !string.IsNullOrEmpty(headerText))
        {
            var headerFormatted = new FormattedText(headerText, FontFamily ?? "Segoe UI", FontSize > 0 ? FontSize : 14)
            {
                Foreground = Foreground ?? new SolidColorBrush(Color.White)
            };
            Interop.TextMeasurement.MeasureText(headerFormatted);
            dc.DrawText(headerFormatted, new Point(padding.Left, startY));
            startY += headerFormatted.Height + 4;
        }

        // Draw switch track
        var trackRect = new Rect(padding.Left, startY, SwitchWidth, SwitchHeight);
        var trackBrush = IsOn
            ? (OnBackground ?? new SolidColorBrush(Color.FromRgb(0, 120, 212)))
            : (OffBackground ?? new SolidColorBrush(Color.FromRgb(60, 60, 60)));

        dc.DrawRoundedRectangle(trackBrush, null, trackRect, SwitchHeight / 2, SwitchHeight / 2);

        // Draw track border
        var borderBrush = new SolidColorBrush(IsOn ? Color.FromRgb(0, 100, 190) : Color.FromRgb(100, 100, 100));
        var borderPen = new Pen(borderBrush, 1);
        dc.DrawRoundedRectangle(null, borderPen, trackRect, SwitchHeight / 2, SwitchHeight / 2);

        // Draw thumb
        var thumbX = IsOn
            ? trackRect.X + trackRect.Width - ThumbMargin - ThumbSize
            : trackRect.X + ThumbMargin;
        var thumbY = trackRect.Y + (trackRect.Height - ThumbSize) / 2;
        var thumbBrush = new SolidColorBrush(Color.White);

        dc.DrawEllipse(thumbBrush, null, new Point(thumbX + ThumbSize / 2, thumbY + ThumbSize / 2), ThumbSize / 2, ThumbSize / 2);

        // Draw on/off content
        var content = IsOn ? OnContent : OffContent;
        if (content is string contentText && !string.IsNullOrEmpty(contentText))
        {
            var contentFormatted = new FormattedText(contentText, FontFamily ?? "Segoe UI", FontSize > 0 ? FontSize : 14)
            {
                Foreground = Foreground ?? new SolidColorBrush(Color.White)
            };
            Interop.TextMeasurement.MeasureText(contentFormatted);

            var contentX = trackRect.Right + 8;
            var contentY = startY + (SwitchHeight - contentFormatted.Height) / 2;
            dc.DrawText(contentFormatted, new Point(contentX, contentY));
        }
    }

    #endregion

    #region Property Changed Callbacks

    private static void OnIsOnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToggleSwitch toggleSwitch)
        {
            toggleSwitch.OnToggled();
            toggleSwitch.InvalidateVisual();
        }
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToggleSwitch toggleSwitch)
        {
            toggleSwitch.InvalidateVisual();
        }
    }

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToggleSwitch toggleSwitch)
        {
            toggleSwitch.InvalidateMeasure();
        }
    }

    /// <summary>
    /// Called when the switch is toggled.
    /// </summary>
    protected virtual void OnToggled()
    {
        RaiseEvent(new RoutedEventArgs(ToggledEvent, this));
    }

    #endregion
}
