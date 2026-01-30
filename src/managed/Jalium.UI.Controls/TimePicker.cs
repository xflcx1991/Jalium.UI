using Jalium.UI.Input;
using Jalium.UI.Interop;
using Jalium.UI.Media;

namespace Jalium.UI.Controls;

/// <summary>
/// Represents a control that allows the user to select a time.
/// </summary>
public class TimePicker : Control
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the SelectedTime dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectedTimeProperty =
        DependencyProperty.Register(nameof(SelectedTime), typeof(TimeSpan?), typeof(TimePicker),
            new PropertyMetadata(null, OnSelectedTimeChanged));

    /// <summary>
    /// Identifies the ClockIdentifier dependency property.
    /// </summary>
    public static readonly DependencyProperty ClockIdentifierProperty =
        DependencyProperty.Register(nameof(ClockIdentifier), typeof(string), typeof(TimePicker),
            new PropertyMetadata("12HourClock", OnVisualPropertyChanged));

    /// <summary>
    /// Identifies the MinuteIncrement dependency property.
    /// </summary>
    public static readonly DependencyProperty MinuteIncrementProperty =
        DependencyProperty.Register(nameof(MinuteIncrement), typeof(int), typeof(TimePicker),
            new PropertyMetadata(1));

    /// <summary>
    /// Identifies the Header dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(object), typeof(TimePicker),
            new PropertyMetadata(null, OnLayoutPropertyChanged));

    /// <summary>
    /// Identifies the PlaceholderText dependency property.
    /// </summary>
    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(TimePicker),
            new PropertyMetadata("Select a time", OnVisualPropertyChanged));

    /// <summary>
    /// Identifies the IsDropDownOpen dependency property.
    /// </summary>
    public static readonly DependencyProperty IsDropDownOpenProperty =
        DependencyProperty.Register(nameof(IsDropDownOpen), typeof(bool), typeof(TimePicker),
            new PropertyMetadata(false));

    #endregion

    #region Routed Events

    /// <summary>
    /// Identifies the SelectedTimeChanged routed event.
    /// </summary>
    public static readonly RoutedEvent SelectedTimeChangedEvent =
        EventManager.RegisterRoutedEvent(nameof(SelectedTimeChanged), RoutingStrategy.Bubble,
            typeof(EventHandler<TimePickerSelectedValueChangedEventArgs>), typeof(TimePicker));

    /// <summary>
    /// Occurs when the selected time changes.
    /// </summary>
    public event EventHandler<TimePickerSelectedValueChangedEventArgs> SelectedTimeChanged
    {
        add => AddHandler(SelectedTimeChangedEvent, value);
        remove => RemoveHandler(SelectedTimeChangedEvent, value);
    }

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the currently selected time.
    /// </summary>
    public TimeSpan? SelectedTime
    {
        get => (TimeSpan?)GetValue(SelectedTimeProperty);
        set => SetValue(SelectedTimeProperty, value);
    }

    /// <summary>
    /// Gets or sets the clock format (12HourClock or 24HourClock).
    /// </summary>
    public string ClockIdentifier
    {
        get => (string)(GetValue(ClockIdentifierProperty) ?? "12HourClock");
        set => SetValue(ClockIdentifierProperty, value);
    }

    /// <summary>
    /// Gets or sets the increment for the minute picker.
    /// </summary>
    public int MinuteIncrement
    {
        get => (int)(GetValue(MinuteIncrementProperty) ?? 1);
        set => SetValue(MinuteIncrementProperty, value);
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
    /// Gets or sets the placeholder text displayed when no time is selected.
    /// </summary>
    public string? PlaceholderText
    {
        get => (string?)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the time picker dropdown is open.
    /// </summary>
    public bool IsDropDownOpen
    {
        get => (bool)(GetValue(IsDropDownOpenProperty) ?? false);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    #endregion

    #region Private Fields

    private const double DefaultHeight = 32;
    private const double DropdownButtonWidth = 32;
    private Rect _dropdownButtonRect;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="TimePicker"/> class.
    /// </summary>
    public TimePicker()
    {
        Focusable = true;
        Height = DefaultHeight;
        Background = new SolidColorBrush(Color.FromRgb(45, 45, 45));
        BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100));
        BorderThickness = new Thickness(1);
        Padding = new Thickness(8, 4, 8, 4);
        CornerRadius = new CornerRadius(4);

        AddHandler(MouseDownEvent, new RoutedEventHandler(OnMouseDownHandler));
        AddHandler(KeyDownEvent, new RoutedEventHandler(OnKeyDownHandler));
    }

    #endregion

    #region Input Handling

    private void OnMouseDownHandler(object sender, RoutedEventArgs e)
    {
        if (!IsEnabled) return;

        if (e is MouseButtonEventArgs mouseArgs && mouseArgs.ChangedButton == MouseButton.Left)
        {
            Focus();
            IsDropDownOpen = !IsDropDownOpen;
            e.Handled = true;
        }
    }

    private void OnKeyDownHandler(object sender, RoutedEventArgs e)
    {
        if (!IsEnabled) return;

        if (e is KeyEventArgs keyArgs)
        {
            switch (keyArgs.Key)
            {
                case Key.Enter:
                case Key.Space:
                    IsDropDownOpen = !IsDropDownOpen;
                    e.Handled = true;
                    break;
                case Key.Escape when IsDropDownOpen:
                    IsDropDownOpen = false;
                    e.Handled = true;
                    break;
                case Key.Up:
                    IncrementTime(TimeSpan.FromMinutes(MinuteIncrement));
                    e.Handled = true;
                    break;
                case Key.Down:
                    IncrementTime(TimeSpan.FromMinutes(-MinuteIncrement));
                    e.Handled = true;
                    break;
            }
        }
    }

    private void IncrementTime(TimeSpan delta)
    {
        var current = SelectedTime ?? TimeSpan.Zero;
        var newTime = current.Add(delta);

        // Wrap around midnight
        if (newTime.TotalHours >= 24)
            newTime = newTime.Subtract(TimeSpan.FromHours(24));
        else if (newTime.TotalHours < 0)
            newTime = newTime.Add(TimeSpan.FromHours(24));

        SelectedTime = newTime;
    }

    #endregion

    #region Formatting

    private string FormatTime(TimeSpan time)
    {
        var is24Hour = ClockIdentifier == "24HourClock";

        if (is24Hour)
        {
            return $"{time.Hours:D2}:{time.Minutes:D2}";
        }
        else
        {
            var hour = time.Hours % 12;
            if (hour == 0) hour = 12;
            var period = time.Hours >= 12 ? "PM" : "AM";
            return $"{hour}:{time.Minutes:D2} {period}";
        }
    }

    #endregion

    #region Layout

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;
        var headerHeight = 0.0;

        // Measure header
        if (Header is string headerText)
        {
            var headerFormatted = new FormattedText(headerText, FontFamily ?? "Segoe UI", FontSize > 0 ? FontSize : 14);
            TextMeasurement.MeasureText(headerFormatted);
            headerHeight = headerFormatted.Height + 4;
        }

        var width = double.IsPositiveInfinity(availableSize.Width) ? 150 : availableSize.Width;
        var height = DefaultHeight + headerHeight;

        return new Size(width, height);
    }

    #endregion

    #region Rendering

    /// <inheritdoc />
    protected override void OnRender(object drawingContext)
    {
        if (drawingContext is not DrawingContext dc)
            return;

        var rect = new Rect(RenderSize);
        var padding = Padding;
        var cornerRadius = CornerRadius;
        var hasCornerRadius = cornerRadius.TopLeft > 0;
        var headerHeight = 0.0;

        // Draw header
        if (Header is string headerText && Foreground != null)
        {
            var headerFormatted = new FormattedText(headerText, FontFamily ?? "Segoe UI", FontSize > 0 ? FontSize : 14)
            {
                Foreground = Foreground
            };
            TextMeasurement.MeasureText(headerFormatted);
            dc.DrawText(headerFormatted, new Point(0, 0));
            headerHeight = headerFormatted.Height + 4;
        }

        // Adjust rect for header
        var inputRect = new Rect(0, headerHeight, rect.Width, rect.Height - headerHeight);

        // Draw background
        if (Background != null)
        {
            if (hasCornerRadius)
            {
                dc.DrawRoundedRectangle(Background, null, inputRect, cornerRadius.TopLeft, cornerRadius.TopLeft);
            }
            else
            {
                dc.DrawRectangle(Background, null, inputRect);
            }
        }

        // Draw border
        var borderBrush = IsFocused ? new SolidColorBrush(Color.FromRgb(0, 120, 212)) : BorderBrush;
        if (borderBrush != null && BorderThickness.TotalWidth > 0)
        {
            var pen = new Pen(borderBrush, BorderThickness.Left);
            if (hasCornerRadius)
            {
                dc.DrawRoundedRectangle(null, pen, inputRect, cornerRadius.TopLeft, cornerRadius.TopLeft);
            }
            else
            {
                dc.DrawRectangle(null, pen, inputRect);
            }
        }

        // Draw time text or placeholder
        string displayText;
        Brush textBrush;

        if (SelectedTime.HasValue)
        {
            displayText = FormatTime(SelectedTime.Value);
            textBrush = Foreground ?? new SolidColorBrush(Color.White);
        }
        else
        {
            displayText = PlaceholderText ?? "Select a time";
            textBrush = new SolidColorBrush(Color.FromRgb(128, 128, 128));
        }

        var textFormatted = new FormattedText(displayText, FontFamily ?? "Segoe UI", FontSize > 0 ? FontSize : 14)
        {
            Foreground = textBrush
        };
        TextMeasurement.MeasureText(textFormatted);

        var textY = inputRect.Top + (inputRect.Height - textFormatted.Height) / 2;
        dc.DrawText(textFormatted, new Point(padding.Left, textY));

        // Draw dropdown button
        _dropdownButtonRect = new Rect(inputRect.Right - DropdownButtonWidth, inputRect.Top, DropdownButtonWidth, inputRect.Height);
        DrawDropdownButton(dc, _dropdownButtonRect);
    }

    private void DrawDropdownButton(DrawingContext dc, Rect rect)
    {
        // Draw clock icon
        var iconBrush = new SolidColorBrush(Color.FromRgb(160, 160, 160));
        var iconPen = new Pen(iconBrush, 1.5);

        var centerX = rect.X + rect.Width / 2;
        var centerY = rect.Y + rect.Height / 2;
        var radius = 6;

        // Draw clock circle
        dc.DrawEllipse(null, iconPen, new Point(centerX, centerY), radius, radius);

        // Draw clock hands
        dc.DrawLine(iconPen, new Point(centerX, centerY), new Point(centerX, centerY - 4)); // Hour hand
        dc.DrawLine(iconPen, new Point(centerX, centerY), new Point(centerX + 3, centerY + 2)); // Minute hand
    }

    #endregion

    #region Property Changed Callbacks

    private static void OnSelectedTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimePicker timePicker)
        {
            timePicker.InvalidateVisual();

            var args = new TimePickerSelectedValueChangedEventArgs(SelectedTimeChangedEvent,
                e.OldValue as TimeSpan?, e.NewValue as TimeSpan?);
            timePicker.RaiseEvent(args);
        }
    }

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimePicker timePicker)
        {
            timePicker.InvalidateMeasure();
        }
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimePicker timePicker)
        {
            timePicker.InvalidateVisual();
        }
    }

    #endregion
}

/// <summary>
/// Provides data for the SelectedTimeChanged event.
/// </summary>
public class TimePickerSelectedValueChangedEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Gets the old selected time.
    /// </summary>
    public TimeSpan? OldTime { get; }

    /// <summary>
    /// Gets the new selected time.
    /// </summary>
    public TimeSpan? NewTime { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimePickerSelectedValueChangedEventArgs"/> class.
    /// </summary>
    public TimePickerSelectedValueChangedEventArgs(RoutedEvent routedEvent, TimeSpan? oldTime, TimeSpan? newTime)
    {
        RoutedEvent = routedEvent;
        OldTime = oldTime;
        NewTime = newTime;
    }
}
