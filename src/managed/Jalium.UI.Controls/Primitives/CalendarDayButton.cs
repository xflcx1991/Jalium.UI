using Jalium.UI.Interop;
using Jalium.UI.Media;

namespace Jalium.UI.Controls.Primitives;

/// <summary>
/// Represents a button for a single day in a Calendar control.
/// </summary>
public class CalendarDayButton : Button
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the IsSelected dependency property.
    /// </summary>
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(CalendarDayButton),
            new PropertyMetadata(false, OnVisualPropertyChanged));

    /// <summary>
    /// Identifies the IsToday dependency property.
    /// </summary>
    public static readonly DependencyProperty IsTodayProperty =
        DependencyProperty.Register(nameof(IsToday), typeof(bool), typeof(CalendarDayButton),
            new PropertyMetadata(false, OnVisualPropertyChanged));

    /// <summary>
    /// Identifies the IsBlackedOut dependency property.
    /// </summary>
    public static readonly DependencyProperty IsBlackedOutProperty =
        DependencyProperty.Register(nameof(IsBlackedOut), typeof(bool), typeof(CalendarDayButton),
            new PropertyMetadata(false, OnVisualPropertyChanged));

    /// <summary>
    /// Identifies the IsInactive dependency property.
    /// </summary>
    public static readonly DependencyProperty IsInactiveProperty =
        DependencyProperty.Register(nameof(IsInactive), typeof(bool), typeof(CalendarDayButton),
            new PropertyMetadata(false, OnVisualPropertyChanged));

    /// <summary>
    /// Identifies the IsHighlighted dependency property.
    /// </summary>
    public static readonly DependencyProperty IsHighlightedProperty =
        DependencyProperty.Register(nameof(IsHighlighted), typeof(bool), typeof(CalendarDayButton),
            new PropertyMetadata(false, OnVisualPropertyChanged));

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets a value indicating whether this day is selected.
    /// </summary>
    public bool IsSelected
    {
        get => (bool)(GetValue(IsSelectedProperty) ?? false);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this day is today.
    /// </summary>
    public bool IsToday
    {
        get => (bool)(GetValue(IsTodayProperty) ?? false);
        set => SetValue(IsTodayProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this day is blacked out (not selectable).
    /// </summary>
    public bool IsBlackedOut
    {
        get => (bool)(GetValue(IsBlackedOutProperty) ?? false);
        set => SetValue(IsBlackedOutProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this day is from an adjacent month.
    /// </summary>
    public bool IsInactive
    {
        get => (bool)(GetValue(IsInactiveProperty) ?? false);
        set => SetValue(IsInactiveProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this day is highlighted.
    /// </summary>
    public bool IsHighlighted
    {
        get => (bool)(GetValue(IsHighlightedProperty) ?? false);
        set => SetValue(IsHighlightedProperty, value);
    }

    /// <summary>
    /// Gets or sets the Calendar that owns this button.
    /// </summary>
    public Calendar? Owner { get; internal set; }

    /// <summary>
    /// Gets or sets the date this button represents.
    /// </summary>
    public DateTime? Date { get; internal set; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarDayButton"/> class.
    /// </summary>
    public CalendarDayButton()
    {
        HorizontalContentAlignment = HorizontalAlignment.Center;
        VerticalContentAlignment = VerticalAlignment.Center;
        Width = 32;
        Height = 32;
    }

    #endregion

    #region Rendering

    /// <inheritdoc />
    protected override void OnRender(object drawingContext)
    {
        if (drawingContext is not DrawingContext dc)
            return;

        var rect = new Rect(RenderSize);
        var inset = new Rect(2, 2, rect.Width - 4, rect.Height - 4);

        // Draw selection background
        if (IsSelected)
        {
            var selectionBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212));
            dc.DrawEllipse(selectionBrush, null, new Point(rect.Width / 2, rect.Height / 2),
                inset.Width / 2, inset.Height / 2);
        }
        else if (IsHighlighted || IsPressed)
        {
            var highlightBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            dc.DrawEllipse(highlightBrush, null, new Point(rect.Width / 2, rect.Height / 2),
                inset.Width / 2, inset.Height / 2);
        }

        // Draw today indicator (ring around the day)
        if (IsToday && !IsSelected)
        {
            var todayPen = new Pen(new SolidColorBrush(Color.FromRgb(0, 120, 212)), 2);
            dc.DrawEllipse(null, todayPen, new Point(rect.Width / 2, rect.Height / 2),
                inset.Width / 2 - 1, inset.Height / 2 - 1);
        }

        // Draw day number
        if (Content is string text)
        {
            Brush fgBrush;
            if (IsBlackedOut)
            {
                fgBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
            }
            else if (!IsEnabled || IsInactive)
            {
                fgBrush = new SolidColorBrush(Color.FromRgb(128, 128, 128));
            }
            else if (IsSelected)
            {
                fgBrush = new SolidColorBrush(Color.White);
            }
            else
            {
                fgBrush = Foreground ?? new SolidColorBrush(Color.White);
            }

            var formattedText = new FormattedText(text, FontFamily ?? "Segoe UI", FontSize > 0 ? FontSize : 14)
            {
                Foreground = fgBrush
            };
            TextMeasurement.MeasureText(formattedText);

            var textX = (rect.Width - formattedText.Width) / 2;
            var textY = (rect.Height - formattedText.Height) / 2;
            dc.DrawText(formattedText, new Point(textX, textY));
        }

        // Draw blacked out strikethrough
        if (IsBlackedOut)
        {
            var strikePen = new Pen(new SolidColorBrush(Color.FromRgb(150, 80, 80)), 1);
            dc.DrawLine(strikePen, new Point(4, rect.Height / 2), new Point(rect.Width - 4, rect.Height / 2));
        }
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CalendarDayButton button)
        {
            button.InvalidateVisual();
        }
    }

    #endregion
}
