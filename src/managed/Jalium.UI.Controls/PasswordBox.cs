using System.Timers;
using Jalium.UI.Input;
using Jalium.UI.Interop;
using Jalium.UI.Media;

namespace Jalium.UI.Controls;

/// <summary>
/// A control for entering passwords with masked display.
/// </summary>
public class PasswordBox : Control, IImeSupport
{
    #region Fields

    private int _caretIndex;
    private double _caretOpacity = 1.0;
    private DateTime _caretAnimationStart;
    private DateTime _lastCaretBlink;
    private const int CaretBlinkInterval = 530;
    private const int CaretFadeDuration = 150;
    private const int CaretTimerInterval = 16; // ~60fps for smooth animation

    private System.Timers.Timer? _caretTimer;
    private double _horizontalOffset;

    // IME composition state
    private bool _isImeComposing;
    private string _imeCompositionString = string.Empty;
    private int _imeCursorPosition;

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

        // Dark theme appearance (matching TextBox)
        Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));
        BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 70));
        Foreground = new SolidColorBrush(Color.White);
        CaretBrush = new SolidColorBrush(Color.White);
        BorderThickness = new Thickness(1);
        Padding = new Thickness(6, 4, 6, 4);
        FontSize = 14;

        // Set IBeam cursor for text input
        Cursor = Jalium.UI.Cursors.IBeam;

        _lastCaretBlink = DateTime.Now;

        // Register input event handlers
        AddHandler(MouseDownEvent, new RoutedEventHandler(OnMouseDownHandler));
        AddHandler(KeyDownEvent, new RoutedEventHandler(OnKeyDownHandler));
        AddHandler(TextInputEvent, new RoutedEventHandler(OnTextInputHandler));

        // Subscribe to IME events
        InputMethod.CompositionStarted += OnImeCompositionStarted;
        InputMethod.CompositionUpdated += OnImeCompositionUpdated;
        InputMethod.CompositionEnded += OnImeCompositionEnded;

        // Subscribe to focus events for IME target management
        AddHandler(GotKeyboardFocusEvent, new RoutedEventHandler(OnGotFocusHandler));
        AddHandler(LostKeyboardFocusEvent, new RoutedEventHandler(OnLostFocusHandler));
    }

    private void OnGotFocusHandler(object sender, RoutedEventArgs e)
    {
        InputMethod.SetTarget(this);
    }

    private void OnLostFocusHandler(object sender, RoutedEventArgs e)
    {
        if (InputMethod.Current == this)
        {
            InputMethod.SetTarget(null);
        }
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
        // Round lineHeight to ensure consistent sizing with rendering
        var lineHeight = Math.Round(fontMetrics.LineHeight);

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
            if (cornerRadius.TopLeft > 0)
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
        if (BorderBrush != null && border.Left > 0)
        {
            var borderPen = new Pen(BorderBrush, border.Left);
            var cornerRadius = CornerRadius;
            if (cornerRadius.TopLeft > 0)
            {
                dc.DrawRoundedRectangle(null, borderPen, bounds,
                    cornerRadius.TopLeft, cornerRadius.TopLeft);
            }
            else
            {
                dc.DrawRectangle(null, borderPen, bounds);
            }
        }

        // Content area - round to pixel boundaries
        var contentRect = new Rect(
            Math.Round(border.Left + padding.Left),
            Math.Round(border.Top + padding.Top),
            Math.Max(0, Math.Round(bounds.Width - border.Left - border.Right - padding.Left - padding.Right)),
            Math.Max(0, Math.Round(bounds.Height - border.Top - border.Bottom - padding.Top - padding.Bottom)));

        // Clip to content area
        dc.PushClip(new RectangleGeometry(contentRect));

        var fontFamily = FontFamily ?? "Segoe UI";
        var fontSize = FontSize > 0 ? FontSize : 14;
        var fontMetrics = TextMeasurement.GetFontMetrics(fontFamily, fontSize);
        var lineHeight = Math.Round(fontMetrics.LineHeight);

        // Measure the password character to get actual width
        var passwordCharStr = new string(PasswordChar, 1);
        var passwordCharText = new FormattedText(passwordCharStr, fontFamily, fontSize);
        TextMeasurement.MeasureText(passwordCharText);
        var charWidth = passwordCharText.Width;

        var roundedHorizontalOffset = Math.Round(_horizontalOffset);

        // Round text position to pixel boundaries
        var textX = Math.Round(contentRect.X - roundedHorizontalOffset);
        var textY = Math.Round(contentRect.Y);

        // Draw masked password or placeholder
        if (string.IsNullOrEmpty(_password) && !_isImeComposing)
        {
            // Draw placeholder
            if (!string.IsNullOrEmpty(Placeholder))
            {
                var placeholderBrush = new SolidColorBrush(Color.FromRgb(128, 128, 128));
                var formattedPlaceholder = new FormattedText(Placeholder, fontFamily, fontSize)
                {
                    Foreground = placeholderBrush,
                    MaxTextWidth = contentRect.Width + roundedHorizontalOffset,
                    MaxTextHeight = lineHeight
                };
                dc.DrawText(formattedPlaceholder, new Point(textX, textY));
            }
        }
        else
        {
            // Draw masked password
            var maskedText = new string(PasswordChar, _password.Length);
            var formattedText = new FormattedText(maskedText, fontFamily, fontSize)
            {
                Foreground = Foreground,
                MaxTextWidth = contentRect.Width + roundedHorizontalOffset,
                MaxTextHeight = lineHeight
            };
            dc.DrawText(formattedText, new Point(textX, textY));
        }

        // Draw IME composition (show actual characters during composition)
        if (_isImeComposing && !string.IsNullOrEmpty(_imeCompositionString))
        {
            DrawImeComposition(dc, contentRect, charWidth, lineHeight);
        }

        // Draw caret
        if (IsKeyboardFocused)
        {
            DrawCaret(dc, contentRect, charWidth, lineHeight);
        }

        dc.Pop();

        // Draw focus indicator
        if (IsKeyboardFocused)
        {
            var focusPen = new Pen(new SolidColorBrush(Color.FromRgb(0, 120, 215)), 1);
            dc.DrawRectangle(null, focusPen, bounds);
        }
    }

    private void DrawImeComposition(DrawingContext dc, Rect contentRect, double charWidth, double lineHeight)
    {
        var fontFamily = FontFamily ?? "Segoe UI";
        var fontSize = FontSize > 0 ? FontSize : 14;

        var roundedHorizontalOffset = Math.Round(_horizontalOffset);
        var x = Math.Round(contentRect.X + _caretIndex * charWidth - roundedHorizontalOffset);
        var y = Math.Round(contentRect.Y);

        // Draw composition background
        var compositionText = new FormattedText(_imeCompositionString, fontFamily, fontSize);
        TextMeasurement.MeasureText(compositionText);
        var compositionWidth = compositionText.Width;

        var compositionBgBrush = new SolidColorBrush(Color.FromRgb(60, 60, 80));
        dc.DrawRectangle(compositionBgBrush, null, new Rect(x, y, compositionWidth, lineHeight));

        // Draw composition text (show actual characters, not masked)
        compositionText.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 150));
        dc.DrawText(compositionText, new Point(x, y));

        // Draw underline
        var underlinePen = new Pen(new SolidColorBrush(Color.FromRgb(255, 255, 150)), 1);
        dc.DrawLine(underlinePen, new Point(x, y + lineHeight - 1), new Point(x + compositionWidth, y + lineHeight - 1));
    }

    private void DrawCaret(DrawingContext dc, Rect contentRect, double charWidth, double lineHeight)
    {
        // Update and get the current caret opacity
        var caretOpacity = UpdateCaretAnimation();

        if (CaretBrush == null || caretOpacity <= 0)
            return;

        // During IME composition, position caret within composition string
        var caretCharIndex = _caretIndex;
        if (_isImeComposing)
        {
            // Position caret at the IME cursor position within composition
            caretCharIndex = _caretIndex + _imeCursorPosition;
        }

        var roundedHorizontalOffset = Math.Round(_horizontalOffset);
        var caretX = Math.Round(contentRect.X + caretCharIndex * charWidth - roundedHorizontalOffset);
        var caretY = Math.Round(contentRect.Y);

        // Create a brush with the animated opacity
        Brush caretBrushWithOpacity;
        if (CaretBrush is SolidColorBrush solidBrush)
        {
            var color = solidBrush.Color;
            var alpha = (byte)(color.A * caretOpacity);
            caretBrushWithOpacity = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
        }
        else
        {
            caretBrushWithOpacity = CaretBrush;
        }

        var caretPen = new Pen(caretBrushWithOpacity, 1);
        dc.DrawLine(caretPen,
            new Point(caretX, caretY),
            new Point(caretX, caretY + lineHeight));
    }

    #endregion

    #region Input Handling

    /// <inheritdoc />
    protected override void OnIsKeyboardFocusedChanged(bool isFocused)
    {
        base.OnIsKeyboardFocusedChanged(isFocused);

        if (isFocused)
        {
            StartCaretTimer();
            ResetCaretBlink();
        }
        else
        {
            StopCaretTimer();
            // End any active IME composition
            if (_isImeComposing)
            {
                _isImeComposing = false;
                _imeCompositionString = string.Empty;
            }
        }

        InvalidateVisual();
    }

    private void OnMouseDownHandler(object sender, RoutedEventArgs e)
    {
        if (!IsEnabled) return;

        if (e is MouseButtonEventArgs mouseArgs && mouseArgs.ChangedButton == MouseButton.Left)
        {
            Focus();

            var position = mouseArgs.GetPosition(this);
            var newCaretIndex = GetCaretIndexFromPosition(position);

            _caretIndex = newCaretIndex;
            ResetCaretBlink();
            EnsureCaretVisible();
            InvalidateVisual();
            e.Handled = true;
        }
    }

    private void OnKeyDownHandler(object sender, RoutedEventArgs e)
    {
        if (e is KeyEventArgs keyArgs)
        {
            OnKeyDown(keyArgs);
        }
    }

    private void OnTextInputHandler(object sender, RoutedEventArgs e)
    {
        if (e is TextCompositionEventArgs textArgs)
        {
            OnTextInput(textArgs);
        }
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
                if (e.IsControlDown)
                {
                    // Move to beginning (passwords don't have word boundaries)
                    _caretIndex = 0;
                }
                else
                {
                    _caretIndex = Math.Max(0, _caretIndex - 1);
                }
                ResetCaretBlink();
                EnsureCaretVisible();
                InvalidateVisual();
                e.Handled = true;
                break;

            case Key.Right:
                if (e.IsControlDown)
                {
                    // Move to end (passwords don't have word boundaries)
                    _caretIndex = _password.Length;
                }
                else
                {
                    _caretIndex = Math.Min(_password.Length, _caretIndex + 1);
                }
                ResetCaretBlink();
                EnsureCaretVisible();
                InvalidateVisual();
                e.Handled = true;
                break;

            case Key.Home:
                _caretIndex = 0;
                ResetCaretBlink();
                EnsureCaretVisible();
                InvalidateVisual();
                e.Handled = true;
                break;

            case Key.End:
                _caretIndex = _password.Length;
                ResetCaretBlink();
                EnsureCaretVisible();
                InvalidateVisual();
                e.Handled = true;
                break;

            case Key.Back:
                HandleBackspace(e.IsControlDown);
                e.Handled = true;
                break;

            case Key.Delete:
                HandleDelete(e.IsControlDown);
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

        var text = e.Text;

        // Filter out control characters
        if (text.Length == 1 && char.IsControl(text[0]))
            return;

        if (!string.IsNullOrEmpty(text))
        {
            InsertPassword(text);
            e.Handled = true;
        }
    }

    #endregion

    #region IME Support

    private void OnImeCompositionStarted(object? sender, EventArgs e)
    {
        if (InputMethod.Current == this)
        {
            OnImeCompositionStart();
        }
    }

    private void OnImeCompositionUpdated(object? sender, CompositionEventArgs e)
    {
        if (InputMethod.Current == this)
        {
            OnImeCompositionUpdate(e.Text, e.CursorPosition);
        }
    }

    private void OnImeCompositionEnded(object? sender, CompositionResultEventArgs e)
    {
        if (InputMethod.Current == this)
        {
            OnImeCompositionEnd(e.Result);
        }
    }

    /// <inheritdoc />
    public Point GetImeCaretPosition()
    {
        var caretRect = GetCaretRect();
        // Return position in element-local coordinates
        // The Window will convert to screen coordinates when positioning IME
        return new Point(caretRect.X, caretRect.Y + caretRect.Height);
    }

    /// <inheritdoc />
    public void OnImeCompositionStart()
    {
        _isImeComposing = true;
        _imeCompositionString = string.Empty;
        _imeCursorPosition = 0;
        InvalidateVisual();
    }

    /// <inheritdoc />
    public void OnImeCompositionUpdate(string compositionString, int cursorPosition)
    {
        _imeCompositionString = compositionString;
        _imeCursorPosition = cursorPosition;
        InvalidateVisual();
    }

    /// <inheritdoc />
    public void OnImeCompositionEnd(string? resultString)
    {
        _isImeComposing = false;
        _imeCompositionString = string.Empty;
        // Result string is inserted via TextInput event
        InvalidateVisual();
    }

    /// <summary>
    /// Gets the caret rectangle for IME positioning.
    /// </summary>
    public Rect GetCaretRect()
    {
        var border = BorderThickness;
        var padding = Padding;
        var fontFamily = FontFamily ?? "Segoe UI";
        var fontSize = FontSize > 0 ? FontSize : 14;
        var fontMetrics = TextMeasurement.GetFontMetrics(fontFamily, fontSize);
        var lineHeight = Math.Round(fontMetrics.LineHeight);

        // Measure password char width
        var passwordCharStr = new string(PasswordChar, 1);
        var passwordCharText = new FormattedText(passwordCharStr, fontFamily, fontSize);
        TextMeasurement.MeasureText(passwordCharText);
        var charWidth = passwordCharText.Width;

        var contentX = border.Left + padding.Left;
        var contentY = border.Top + padding.Top;

        var caretX = contentX + _caretIndex * charWidth - _horizontalOffset;
        var caretY = contentY;

        return new Rect(caretX, caretY, 1, lineHeight);
    }

    /// <summary>
    /// Gets whether IME composition is active.
    /// </summary>
    public bool IsComposing => _isImeComposing;

    #endregion

    #region Private Methods

    private void HandleBackspace(bool deleteAll = false)
    {
        if (_caretIndex > 0)
        {
            if (deleteAll)
            {
                // Delete all characters before caret
                _password = _password.Substring(_caretIndex);
                _caretIndex = 0;
            }
            else
            {
                _password = _password.Remove(_caretIndex - 1, 1);
                _caretIndex--;
            }
            ResetCaretBlink();
            EnsureCaretVisible();
            InvalidateMeasure();
            RaisePasswordChanged();
        }
    }

    private void HandleDelete(bool deleteAll = false)
    {
        if (_caretIndex < _password.Length)
        {
            if (deleteAll)
            {
                // Delete all characters after caret
                _password = _password.Substring(0, _caretIndex);
            }
            else
            {
                _password = _password.Remove(_caretIndex, 1);
            }
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
        EnsureCaretVisible();
        InvalidateMeasure();
        RaisePasswordChanged();
    }

    private void ResetCaretBlink()
    {
        _caretOpacity = 1.0;
        _lastCaretBlink = DateTime.Now;
        _caretAnimationStart = DateTime.Now;
    }

    private void RaisePasswordChanged()
    {
        var e = new RoutedEventArgs(PasswordChangedEvent, this);
        RaiseEvent(e);
    }

    private int GetCaretIndexFromPosition(Point position)
    {
        var border = BorderThickness;
        var padding = Padding;
        var fontFamily = FontFamily ?? "Segoe UI";
        var fontSize = FontSize > 0 ? FontSize : 14;

        // Measure password char width
        var passwordCharStr = new string(PasswordChar, 1);
        var passwordCharText = new FormattedText(passwordCharStr, fontFamily, fontSize);
        TextMeasurement.MeasureText(passwordCharText);
        var charWidth = passwordCharText.Width;

        var contentX = border.Left + padding.Left;
        var relativeX = position.X - contentX + _horizontalOffset;

        if (relativeX <= 0)
            return 0;

        // Calculate character index based on position
        var charIndex = (int)Math.Round(relativeX / charWidth);
        return Math.Max(0, Math.Min(charIndex, _password.Length));
    }

    private void EnsureCaretVisible()
    {
        var border = BorderThickness;
        var padding = Padding;
        var contentWidth = Math.Round(RenderSize.Width - border.Left - border.Right - padding.Left - padding.Right);

        if (contentWidth <= 0)
            return;

        var fontFamily = FontFamily ?? "Segoe UI";
        var fontSize = FontSize > 0 ? FontSize : 14;

        // Measure password char width
        var passwordCharStr = new string(PasswordChar, 1);
        var passwordCharText = new FormattedText(passwordCharStr, fontFamily, fontSize);
        TextMeasurement.MeasureText(passwordCharText);
        var charWidth = passwordCharText.Width;

        var caretX = Math.Round(_caretIndex * charWidth);

        // Horizontal scrolling
        if (caretX < _horizontalOffset)
        {
            _horizontalOffset = caretX;
        }
        else if (caretX > _horizontalOffset + contentWidth - 2)
        {
            _horizontalOffset = caretX - contentWidth + 2;
        }

        _horizontalOffset = Math.Round(Math.Max(0, _horizontalOffset));
    }

    #endregion

    #region Caret Animation

    private void StartCaretTimer()
    {
        if (_caretTimer == null)
        {
            _caretTimer = new System.Timers.Timer(CaretTimerInterval);
            _caretTimer.Elapsed += OnCaretTimerElapsed;
            _caretTimer.AutoReset = true;
        }
        _caretTimer.Start();
    }

    private void StopCaretTimer()
    {
        _caretTimer?.Stop();
    }

    private void OnCaretTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // Use Dispatcher to update UI from timer thread
        Dispatcher.Invoke(() =>
        {
            InvalidateVisual();
        });
    }

    private double UpdateCaretAnimation()
    {
        var now = DateTime.Now;
        var timeSinceReset = (now - _lastCaretBlink).TotalMilliseconds;
        var cycleTime = timeSinceReset % (CaretBlinkInterval * 2);

        if (cycleTime < CaretBlinkInterval - CaretFadeDuration)
        {
            // Fully visible
            _caretOpacity = 1.0;
        }
        else if (cycleTime < CaretBlinkInterval)
        {
            // Fading out
            var fadeProgress = (cycleTime - (CaretBlinkInterval - CaretFadeDuration)) / CaretFadeDuration;
            _caretOpacity = 1.0 - fadeProgress;
        }
        else if (cycleTime < CaretBlinkInterval * 2 - CaretFadeDuration)
        {
            // Fully hidden
            _caretOpacity = 0.0;
        }
        else
        {
            // Fading in
            var fadeProgress = (cycleTime - (CaretBlinkInterval * 2 - CaretFadeDuration)) / CaretFadeDuration;
            _caretOpacity = fadeProgress;
        }

        return _caretOpacity;
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
