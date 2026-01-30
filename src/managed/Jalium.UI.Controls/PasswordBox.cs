using Jalium.UI.Input;
using Jalium.UI.Interop;
using Jalium.UI.Media;

namespace Jalium.UI.Controls;

/// <summary>
/// A control for entering passwords with masked display.
/// </summary>
public class PasswordBox : Control
{
    #region Fields

    private int _caretIndex;
    private bool _caretVisible = true;
    private DateTime _lastCaretBlink;
    private const int CaretBlinkInterval = 500;

    #endregion

    #region Dependency Properties

    /// <summary>
    /// Identifies the PasswordChar dependency property.
    /// </summary>
    public static readonly DependencyProperty PasswordCharProperty =
        DependencyProperty.Register(nameof(PasswordChar), typeof(char), typeof(PasswordBox),
            new PropertyMetadata('●', OnVisualPropertyChanged));

    /// <summary>
    /// Identifies the MaxLength dependency property.
    /// </summary>
    public static readonly DependencyProperty MaxLengthProperty =
        DependencyProperty.Register(nameof(MaxLength), typeof(int), typeof(PasswordBox),
            new PropertyMetadata(0));

    /// <summary>
    /// Identifies the CaretBrush dependency property.
    /// </summary>
    public static readonly DependencyProperty CaretBrushProperty =
        DependencyProperty.Register(nameof(CaretBrush), typeof(Brush), typeof(PasswordBox),
            new PropertyMetadata(new SolidColorBrush(Color.Black), OnVisualPropertyChanged));

    /// <summary>
    /// Identifies the Placeholder dependency property.
    /// </summary>
    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(PasswordBox),
            new PropertyMetadata(string.Empty, OnVisualPropertyChanged));

    #endregion

    #region Password (Non-dependency property for security)

    private string _password = string.Empty;

    /// <summary>
    /// Gets or sets the password.
    /// This is intentionally NOT a dependency property to prevent binding and secure the value.
    /// </summary>
    public string Password
    {
        get => _password;
        set
        {
            if (_password != value)
            {
                _password = value ?? string.Empty;

                // Clamp caret index
                if (_caretIndex > _password.Length)
                {
                    _caretIndex = _password.Length;
                }

                InvalidateMeasure();
                RaisePasswordChanged();
            }
        }
    }

    /// <summary>
    /// Gets the password as a SecureString (for enhanced security scenarios).
    /// </summary>
    public System.Security.SecureString SecurePassword
    {
        get
        {
            var secureString = new System.Security.SecureString();
            foreach (char c in _password)
            {
                secureString.AppendChar(c);
            }
            secureString.MakeReadOnly();
            return secureString;
        }
    }

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the masking character.
    /// </summary>
    public char PasswordChar
    {
        get => (char)(GetValue(PasswordCharProperty) ?? '●');
        set => SetValue(PasswordCharProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum password length (0 = unlimited).
    /// </summary>
    public int MaxLength
    {
        get => (int)(GetValue(MaxLengthProperty) ?? 0);
        set => SetValue(MaxLengthProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush for the caret.
    /// </summary>
    public Brush? CaretBrush
    {
        get => (Brush?)GetValue(CaretBrushProperty);
        set => SetValue(CaretBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the placeholder text shown when the password is empty.
    /// </summary>
    public string Placeholder
    {
        get => (string)(GetValue(PlaceholderProperty) ?? string.Empty);
        set => SetValue(PlaceholderProperty, value);
    }

    #endregion

    #region Routed Events

    /// <summary>
    /// Identifies the PasswordChanged routed event.
    /// </summary>
    public static readonly RoutedEvent PasswordChangedEvent =
        EventManager.RegisterRoutedEvent(nameof(PasswordChanged), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(PasswordBox));

    /// <summary>
    /// Occurs when the password content changes.
    /// </summary>
    public event RoutedEventHandler PasswordChanged
    {
        add => AddHandler(PasswordChangedEvent, value);
        remove => RemoveHandler(PasswordChangedEvent, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordBox"/> class.
    /// </summary>
    public PasswordBox()
    {
        Focusable = true;

        // Default appearance
        Background = new SolidColorBrush(Color.White);
        BorderBrush = new SolidColorBrush(Color.FromRgb(171, 173, 179));
        BorderThickness = new Thickness(1);
        Padding = new Thickness(4, 2, 4, 2);

        _lastCaretBlink = DateTime.Now;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Selects all password characters.
    /// </summary>
    public void SelectAll()
    {
        // PasswordBox doesn't support visible selection for security
        _caretIndex = _password.Length;
        InvalidateVisual();
    }

    /// <summary>
    /// Clears the password.
    /// </summary>
    public void Clear()
    {
        Password = string.Empty;
        _caretIndex = 0;
    }

    /// <summary>
    /// Pastes text from the clipboard as password.
    /// </summary>
    public void Paste()
    {
        var clipboardText = Clipboard.GetText();
        if (!string.IsNullOrEmpty(clipboardText))
        {
            InsertPassword(clipboardText);
        }
    }

    #endregion

    #region Layout

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;
        var border = BorderThickness;

        var fontFamily = FontFamily ?? "Segoe UI";
        var fontSize = FontSize > 0 ? FontSize : 14;
        var fontMetrics = TextMeasurement.GetFontMetrics(fontFamily, fontSize);
        var lineHeight = fontMetrics.LineHeight;

        // Measure the password character to get actual width
        var passwordCharStr = new string(PasswordChar, 1);
        var passwordCharText = new FormattedText(passwordCharStr, fontFamily, fontSize);
        TextMeasurement.MeasureText(passwordCharText);
        var charWidth = passwordCharText.Width;

        double textWidth = _password.Length * charWidth;
        double textHeight = lineHeight;

        var desiredWidth = textWidth + padding.Left + padding.Right + border.Left + border.Right;
        var desiredHeight = textHeight + padding.Top + padding.Bottom + border.Top + border.Bottom;

        // Minimum height for single line
        var minHeight = lineHeight + padding.Top + padding.Bottom + border.Top + border.Bottom;
        desiredHeight = Math.Max(desiredHeight, minHeight);

        return new Size(
            Math.Min(desiredWidth, availableSize.Width),
            Math.Min(desiredHeight, availableSize.Height));
    }

    #endregion

    #region Rendering

    /// <inheritdoc />
    protected override void OnRender(object drawingContext)
    {
        base.OnRender(drawingContext);

        if (drawingContext is not DrawingContext dc)
            return;

        var border = BorderThickness;
        var padding = Padding;
        var bounds = new Rect(0, 0, RenderSize.Width, RenderSize.Height);

        // Draw background
        if (Background != null)
        {
            var cornerRadius = CornerRadius;
            if (cornerRadius.TopLeft > 0 || cornerRadius.TopRight > 0 ||
                cornerRadius.BottomLeft > 0 || cornerRadius.BottomRight > 0)
            {
                dc.DrawRoundedRectangle(Background, null, bounds,
                    cornerRadius.TopLeft, cornerRadius.TopLeft);
            }
            else
            {
                dc.DrawRectangle(Background, null, bounds);
            }
        }

        // Draw border
        if (BorderBrush != null && (border.Left > 0 || border.Top > 0 || border.Right > 0 || border.Bottom > 0))
        {
            var pen = new Pen(BorderBrush, Math.Max(border.Left, Math.Max(border.Top, Math.Max(border.Right, border.Bottom))));
            var cornerRadius = CornerRadius;
            if (cornerRadius.TopLeft > 0 || cornerRadius.TopRight > 0 ||
                cornerRadius.BottomLeft > 0 || cornerRadius.BottomRight > 0)
            {
                dc.DrawRoundedRectangle(null, pen, bounds,
                    cornerRadius.TopLeft, cornerRadius.TopLeft);
            }
            else
            {
                dc.DrawRectangle(null, pen, bounds);
            }
        }

        // Content area
        var contentRect = new Rect(
            border.Left + padding.Left,
            border.Top + padding.Top,
            Math.Max(0, bounds.Width - border.Left - border.Right - padding.Left - padding.Right),
            Math.Max(0, bounds.Height - border.Top - border.Bottom - padding.Top - padding.Bottom));

        var fontFamily = FontFamily ?? "Segoe UI";
        var fontSize = FontSize > 0 ? FontSize : 14;
        var fontMetrics = TextMeasurement.GetFontMetrics(fontFamily, fontSize);
        var lineHeight = fontMetrics.LineHeight;

        // Measure the password character to get actual width
        var passwordCharStr = new string(PasswordChar, 1);
        var passwordCharText = new FormattedText(passwordCharStr, fontFamily, fontSize);
        TextMeasurement.MeasureText(passwordCharText);
        var charWidth = passwordCharText.Width;

        // Draw masked password or placeholder
        if (string.IsNullOrEmpty(_password))
        {
            // Draw placeholder
            if (!string.IsNullOrEmpty(Placeholder))
            {
                var placeholderBrush = new SolidColorBrush(Color.FromRgb(160, 160, 160));
                var formattedPlaceholder = new FormattedText(Placeholder, FontFamily, FontSize)
                {
                    Foreground = placeholderBrush,
                    MaxTextWidth = contentRect.Width,
                    MaxTextHeight = contentRect.Height
                };
                dc.DrawText(formattedPlaceholder, new Point(contentRect.X, contentRect.Y));
            }
        }
        else
        {
            // Draw masked password
            var maskedText = new string(PasswordChar, _password.Length);
            var formattedText = new FormattedText(maskedText, FontFamily, FontSize)
            {
                Foreground = Foreground,
                MaxTextWidth = contentRect.Width,
                MaxTextHeight = contentRect.Height
            };
            dc.DrawText(formattedText, new Point(contentRect.X, contentRect.Y));
        }

        // Draw caret
        if (IsKeyboardFocused && _caretVisible)
        {
            DrawCaret(dc, contentRect, charWidth, lineHeight);
        }

        // Draw focus indicator
        if (IsKeyboardFocused)
        {
            var focusPen = new Pen(new SolidColorBrush(Color.FromRgb(0, 120, 215)), 1);
            dc.DrawRectangle(null, focusPen, bounds);
        }
    }

    private void DrawCaret(DrawingContext dc, Rect contentRect, double charWidth, double lineHeight)
    {
        if (CaretBrush == null)
            return;

        var caretX = contentRect.X + _caretIndex * charWidth;
        var caretPen = new Pen(CaretBrush, 1);

        dc.DrawLine(caretPen,
            new Point(caretX, contentRect.Y),
            new Point(caretX, contentRect.Y + lineHeight));
    }

    #endregion

    #region Input Handling

    /// <inheritdoc />
    protected override void OnIsKeyboardFocusedChanged(bool isFocused)
    {
        base.OnIsKeyboardFocusedChanged(isFocused);

        if (isFocused)
        {
            ResetCaretBlink();
        }

        InvalidateVisual();
    }

    /// <summary>
    /// Handles key down events.
    /// </summary>
    protected virtual void OnKeyDown(KeyEventArgs e)
    {
        if (e.Handled)
            return;

        switch (e.Key)
        {
            case Key.Left:
                _caretIndex = Math.Max(0, _caretIndex - 1);
                ResetCaretBlink();
                InvalidateVisual();
                e.Handled = true;
                break;

            case Key.Right:
                _caretIndex = Math.Min(_password.Length, _caretIndex + 1);
                ResetCaretBlink();
                InvalidateVisual();
                e.Handled = true;
                break;

            case Key.Home:
                _caretIndex = 0;
                ResetCaretBlink();
                InvalidateVisual();
                e.Handled = true;
                break;

            case Key.End:
                _caretIndex = _password.Length;
                ResetCaretBlink();
                InvalidateVisual();
                e.Handled = true;
                break;

            case Key.Back:
                HandleBackspace();
                e.Handled = true;
                break;

            case Key.Delete:
                HandleDelete();
                e.Handled = true;
                break;

            case Key.V:
                if (e.IsControlDown)
                {
                    Paste();
                    e.Handled = true;
                }
                break;

            case Key.A:
                if (e.IsControlDown)
                {
                    SelectAll();
                    e.Handled = true;
                }
                break;
        }
    }

    /// <summary>
    /// Handles text input events.
    /// </summary>
    protected virtual void OnTextInput(TextCompositionEventArgs e)
    {
        if (e.Handled)
            return;

        if (!string.IsNullOrEmpty(e.Text))
        {
            InsertPassword(e.Text);
            e.Handled = true;
        }
    }

    #endregion

    #region Private Methods

    private void HandleBackspace()
    {
        if (_caretIndex > 0)
        {
            _password = _password.Remove(_caretIndex - 1, 1);
            _caretIndex--;
            ResetCaretBlink();
            InvalidateMeasure();
            RaisePasswordChanged();
        }
    }

    private void HandleDelete()
    {
        if (_caretIndex < _password.Length)
        {
            _password = _password.Remove(_caretIndex, 1);
            ResetCaretBlink();
            InvalidateMeasure();
            RaisePasswordChanged();
        }
    }

    private void InsertPassword(string text)
    {
        var maxLength = MaxLength;

        // Enforce max length
        if (maxLength > 0)
        {
            var availableSpace = maxLength - _password.Length;
            if (availableSpace <= 0)
                return;

            if (text.Length > availableSpace)
            {
                text = text.Substring(0, availableSpace);
            }
        }

        // Insert password
        _password = _password.Insert(_caretIndex, text);
        _caretIndex += text.Length;

        ResetCaretBlink();
        InvalidateMeasure();
        RaisePasswordChanged();
    }

    private void ResetCaretBlink()
    {
        _caretVisible = true;
        _lastCaretBlink = DateTime.Now;
    }

    private void RaisePasswordChanged()
    {
        var e = new RoutedEventArgs(PasswordChangedEvent, this);
        RaiseEvent(e);
    }

    #endregion

    #region Property Changed Callbacks

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PasswordBox passwordBox)
        {
            passwordBox.InvalidateVisual();
        }
    }

    #endregion
}
