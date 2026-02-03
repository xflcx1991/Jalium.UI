using System.Globalization;
using Jalium.UI.Input;
using Jalium.UI.Interop;
using Jalium.UI.Media;

namespace Jalium.UI.Controls;

/// <summary>
/// Represents a text box for entering numeric values with spin buttons for increment/decrement.
/// Inherits from TextBoxBase for full text editing support.
/// </summary>
public class NumberBox : TextBoxBase, IImeSupport
{
    #region Fields

    // Internal text storage
    private string _text = "0";
    private bool _linesDirty = true;

    // Text width measurement cache
    private Dictionary<string, double> _textWidthCache = new();
    private string? _cachedFontFamily;
    private double _cachedFontSize;
    private const int MaxCacheSize = 256;

    // IME support
    private bool _isImeComposing;
    private string _imeCompositionString = string.Empty;
    private int _imeCompositionCursor;
    private int _imeCompositionStart;

    // Layout regions
    private Rect _upButtonRect;
    private Rect _downButtonRect;
    private Rect _textRect;
    private bool _isUpButtonHovered;
    private bool _isDownButtonHovered;
    private bool _isUpButtonPressed;
    private bool _isDownButtonPressed;

    // Edit state
    private bool _isEditing;
    private bool _isUpdatingValue;

    // Constants
    private const double SpinButtonWidth = 32;
    private const double DefaultHeight = 32;

    #endregion

    #region Dependency Properties

    /// <summary>
    /// Identifies the Value dependency property.
    /// </summary>
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(NumberBox),
            new PropertyMetadata(0.0, OnValueChanged, CoerceValue));

    /// <summary>
    /// Identifies the Minimum dependency property.
    /// </summary>
    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(NumberBox),
            new PropertyMetadata(double.MinValue, OnRangeChanged));

    /// <summary>
    /// Identifies the Maximum dependency property.
    /// </summary>
    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(NumberBox),
            new PropertyMetadata(double.MaxValue, OnRangeChanged));

    /// <summary>
    /// Identifies the SmallChange dependency property.
    /// </summary>
    public static readonly DependencyProperty SmallChangeProperty =
        DependencyProperty.Register(nameof(SmallChange), typeof(double), typeof(NumberBox),
            new PropertyMetadata(1.0));

    /// <summary>
    /// Identifies the LargeChange dependency property.
    /// </summary>
    public static readonly DependencyProperty LargeChangeProperty =
        DependencyProperty.Register(nameof(LargeChange), typeof(double), typeof(NumberBox),
            new PropertyMetadata(10.0));

    /// <summary>
    /// Identifies the SpinButtonPlacementMode dependency property.
    /// </summary>
    public static readonly DependencyProperty SpinButtonPlacementModeProperty =
        DependencyProperty.Register(nameof(SpinButtonPlacementMode), typeof(NumberBoxSpinButtonPlacementMode), typeof(NumberBox),
            new PropertyMetadata(NumberBoxSpinButtonPlacementMode.Inline, OnLayoutPropertyChanged));

    /// <summary>
    /// Identifies the PlaceholderText dependency property.
    /// </summary>
    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(NumberBox),
            new PropertyMetadata(null, OnVisualPropertyChanged));

    /// <summary>
    /// Identifies the Header dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(object), typeof(NumberBox),
            new PropertyMetadata(null, OnLayoutPropertyChanged));

    /// <summary>
    /// Identifies the NumberFormatter dependency property.
    /// </summary>
    public static readonly DependencyProperty NumberFormatterProperty =
        DependencyProperty.Register(nameof(NumberFormatter), typeof(string), typeof(NumberBox),
            new PropertyMetadata("G", OnFormatChanged));

    /// <summary>
    /// Identifies the AcceptsExpression dependency property.
    /// </summary>
    public static readonly DependencyProperty AcceptsExpressionProperty =
        DependencyProperty.Register(nameof(AcceptsExpression), typeof(bool), typeof(NumberBox),
            new PropertyMetadata(false));

    /// <summary>
    /// Identifies the IsWrapEnabled dependency property.
    /// </summary>
    public static readonly DependencyProperty IsWrapEnabledProperty =
        DependencyProperty.Register(nameof(IsWrapEnabled), typeof(bool), typeof(NumberBox),
            new PropertyMetadata(false));

    /// <summary>
    /// Identifies the DecimalPlaces dependency property.
    /// </summary>
    public static readonly DependencyProperty DecimalPlacesProperty =
        DependencyProperty.Register(nameof(DecimalPlaces), typeof(int), typeof(NumberBox),
            new PropertyMetadata(-1, OnFormatChanged));

    #endregion

    #region Routed Events

    /// <summary>
    /// Identifies the ValueChanged routed event.
    /// </summary>
    public static readonly RoutedEvent ValueChangedEvent =
        EventManager.RegisterRoutedEvent(nameof(ValueChanged), RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<double>), typeof(NumberBox));

    /// <summary>
    /// Occurs when the Value property changes.
    /// </summary>
    public event RoutedPropertyChangedEventHandler<double> ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the numeric value.
    /// </summary>
    public double Value
    {
        get => (double)(GetValue(ValueProperty) ?? 0.0);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    public double Minimum
    {
        get => (double)(GetValue(MinimumProperty) ?? double.MinValue);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    public double Maximum
    {
        get => (double)(GetValue(MaximumProperty) ?? double.MaxValue);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>
    /// Gets or sets the small change value (for arrow keys and spin buttons).
    /// </summary>
    public double SmallChange
    {
        get => (double)(GetValue(SmallChangeProperty) ?? 1.0);
        set => SetValue(SmallChangeProperty, value);
    }

    /// <summary>
    /// Gets or sets the large change value (for page up/down).
    /// </summary>
    public double LargeChange
    {
        get => (double)(GetValue(LargeChangeProperty) ?? 10.0);
        set => SetValue(LargeChangeProperty, value);
    }

    /// <summary>
    /// Gets or sets the spin button placement mode.
    /// </summary>
    public NumberBoxSpinButtonPlacementMode SpinButtonPlacementMode
    {
        get => (NumberBoxSpinButtonPlacementMode)(GetValue(SpinButtonPlacementModeProperty) ?? NumberBoxSpinButtonPlacementMode.Inline);
        set => SetValue(SpinButtonPlacementModeProperty, value);
    }

    /// <summary>
    /// Gets or sets the placeholder text.
    /// </summary>
    public string? PlaceholderText
    {
        get => (string?)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
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
    /// Gets or sets the number format string.
    /// </summary>
    public string NumberFormatter
    {
        get => (string)(GetValue(NumberFormatterProperty) ?? "G");
        set => SetValue(NumberFormatterProperty, value);
    }

    /// <summary>
    /// Gets or sets whether mathematical expressions are accepted.
    /// </summary>
    public bool AcceptsExpression
    {
        get => (bool)(GetValue(AcceptsExpressionProperty) ?? false);
        set => SetValue(AcceptsExpressionProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the value wraps around at min/max.
    /// </summary>
    public bool IsWrapEnabled
    {
        get => (bool)(GetValue(IsWrapEnabledProperty) ?? false);
        set => SetValue(IsWrapEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets the number of decimal places (-1 for auto).
    /// </summary>
    public int DecimalPlaces
    {
        get => (int)(GetValue(DecimalPlacesProperty) ?? -1);
        set => SetValue(DecimalPlacesProperty, value);
    }

    /// <summary>
    /// Gets or sets the text representation of the value.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value ?? "0";
                _linesDirty = true;

                // Clamp caret
                if (_caretIndex > _text.Length)
                    _caretIndex = _text.Length;

                InvalidateVisual();
            }
        }
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="NumberBox"/> class.
    /// </summary>
    public NumberBox()
    {
        // Dark theme appearance
        Background = new SolidColorBrush(Color.FromRgb(45, 45, 45));
        BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100));
        Foreground = new SolidColorBrush(Color.White);
        BorderThickness = new Thickness(1);
        Padding = new Thickness(8, 4, 8, 4);
        CornerRadius = new CornerRadius(4);
        FontSize = 14;
        Height = DefaultHeight;

        // Set IBeam cursor for text input
        Cursor = Jalium.UI.Cursors.IBeam;

        // Subscribe to IME events
        InputMethod.CompositionStarted += OnImeCompositionStarted;
        InputMethod.CompositionUpdated += OnImeCompositionUpdated;
        InputMethod.CompositionEnded += OnImeCompositionEnded;

        // Subscribe to focus events
        AddHandler(GotKeyboardFocusEvent, new RoutedEventHandler(OnGotFocusHandler));
        AddHandler(LostKeyboardFocusEvent, new RoutedEventHandler(OnLostFocusHandler));
    }

    private void OnGotFocusHandler(object sender, RoutedEventArgs e)
    {
        InputMethod.SetTarget(this);
        _isEditing = true;
        SelectAll();
        InvalidateVisual();
    }

    private void OnLostFocusHandler(object sender, RoutedEventArgs e)
    {
        if (InputMethod.Current == this)
        {
            InputMethod.SetTarget(null);
        }
        _isEditing = false;
        CommitValue();
        InvalidateVisual();
    }

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

    #endregion

    #region Abstract Method Implementations

    /// <inheritdoc />
    protected override string GetText() => _text;

    /// <inheritdoc />
    protected override void SetText(string value)
    {
        Text = value;
    }

    /// <inheritdoc />
    protected override double GetLineHeight()
    {
        var fontFamily = FontFamily ?? "Segoe UI";
        var fontSize = FontSize > 0 ? FontSize : 14;
        var fontMetrics = TextMeasurement.GetFontMetrics(fontFamily, fontSize);
        return fontMetrics.LineHeight;
    }

    /// <inheritdoc />
    protected override double MeasureTextWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        var fontFamily = FontFamily ?? "Segoe UI";
        var fontSize = FontSize > 0 ? FontSize : 14;

        if (_cachedFontFamily != fontFamily || _cachedFontSize != fontSize)
        {
            _textWidthCache.Clear();
            _cachedFontFamily = fontFamily;
            _cachedFontSize = fontSize;
        }

        if (_textWidthCache.TryGetValue(text, out var cachedWidth))
            return cachedWidth;

        var formattedText = new FormattedText(text, fontFamily, fontSize);
        var usedNative = TextMeasurement.MeasureText(formattedText);

        double width;
        if (usedNative && formattedText.IsMeasured)
        {
            width = formattedText.Width;
        }
        else
        {
            width = text.Length * fontSize * 0.6;
        }

        if (_textWidthCache.Count >= MaxCacheSize)
        {
            var keysToRemove = _textWidthCache.Keys.Take(MaxCacheSize / 2).ToList();
            foreach (var key in keysToRemove)
                _textWidthCache.Remove(key);
        }
        _textWidthCache[text] = width;

        return width;
    }

    /// <inheritdoc />
    protected override int GetLineCount() => 1;

    /// <inheritdoc />
    protected override (int lineIndex, int columnIndex) GetLineColumnFromCharIndex(int charIndex)
    {
        return (0, Math.Max(0, Math.Min(charIndex, _text.Length)));
    }

    /// <inheritdoc />
    protected override int GetCharIndexFromLineColumn(int lineIndex, int columnIndex)
    {
        return Math.Max(0, Math.Min(columnIndex, _text.Length));
    }

    /// <inheritdoc />
    protected override string GetLineTextInternal(int lineIndex)
    {
        return _text;
    }

    /// <inheritdoc />
    protected override int GetLineStartIndex(int lineIndex)
    {
        return 0;
    }

    /// <inheritdoc />
    protected override int GetLineLengthInternal(int lineIndex)
    {
        return _text.Length;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Increases the value by SmallChange.
    /// </summary>
    public void StepUp()
    {
        var newValue = Value + SmallChange;
        if (IsWrapEnabled && newValue > Maximum)
        {
            newValue = Minimum;
        }
        Value = newValue;
    }

    /// <summary>
    /// Decreases the value by SmallChange.
    /// </summary>
    public void StepDown()
    {
        var newValue = Value - SmallChange;
        if (IsWrapEnabled && newValue < Minimum)
        {
            newValue = Maximum;
        }
        Value = newValue;
    }

    #endregion

    #region Key Handling Override

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
                StepUp();
                e.Handled = true;
                return;

            case Key.Down:
                StepDown();
                e.Handled = true;
                return;

            case Key.PageUp:
                Value += LargeChange;
                e.Handled = true;
                return;

            case Key.PageDown:
                Value -= LargeChange;
                e.Handled = true;
                return;

            case Key.Enter:
                CommitValue();
                e.Handled = true;
                return;

            case Key.Escape:
                // Revert to current value
                _text = FormatValue(Value);
                _caretIndex = _text.Length;
                _selectionLength = 0;
                InvalidateVisual();
                e.Handled = true;
                return;
        }

        base.OnKeyDown(e);
    }

    #endregion

    #region Text Input Override

    /// <inheritdoc />
    protected override void InsertText(string textToInsert)
    {
        if (IsReadOnly || string.IsNullOrEmpty(textToInsert))
            return;

        // Filter to only allow valid numeric characters
        var filteredText = FilterNumericInput(textToInsert);
        if (string.IsNullOrEmpty(filteredText))
            return;

        PushUndo();

        // Delete selection if any
        if (_selectionLength > 0)
        {
            DeleteSelectionInternal();
        }

        // Ensure caret is within bounds
        if (_caretIndex < 0) _caretIndex = 0;
        if (_caretIndex > _text.Length) _caretIndex = _text.Length;

        // Insert text
        _text = _text.Substring(0, _caretIndex) + filteredText + _text.Substring(_caretIndex);
        _caretIndex += filteredText.Length;
        _linesDirty = true;

        ResetCaretBlink();
        EnsureCaretVisible();
        InvalidateVisual();
    }

    private string FilterNumericInput(string input)
    {
        var result = new System.Text.StringBuilder();
        var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
        var negativeSeparator = CultureInfo.CurrentCulture.NumberFormat.NegativeSign[0];

        foreach (var c in input)
        {
            if (char.IsDigit(c))
            {
                result.Append(c);
            }
            else if (c == decimalSeparator || c == '.')
            {
                // Only allow one decimal point
                if (!_text.Contains(decimalSeparator) && !_text.Contains('.'))
                {
                    result.Append(decimalSeparator);
                }
            }
            else if (c == negativeSeparator || c == '-')
            {
                // Only allow negative sign at the beginning
                if (_caretIndex == 0 && !_text.StartsWith(negativeSeparator.ToString()) && !_text.StartsWith("-"))
                {
                    result.Append(negativeSeparator);
                }
            }
            else if (AcceptsExpression && (c == '+' || c == '*' || c == '/' || c == '(' || c == ')' || c == ' '))
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    #endregion

    #region Value Handling

    private void CommitValue()
    {
        if (_isUpdatingValue)
            return;

        if (double.TryParse(_text, NumberStyles.Any, CultureInfo.CurrentCulture, out var newValue))
        {
            Value = newValue;
        }
        else if (AcceptsExpression)
        {
            // Try to evaluate as expression
            var result = TryEvaluateExpression(_text);
            if (result.HasValue)
            {
                Value = result.Value;
            }
        }

        // Update text to formatted value
        _text = FormatValue(Value);
        _caretIndex = _text.Length;
        _selectionLength = 0;
        InvalidateVisual();
    }

    private double? TryEvaluateExpression(string expression)
    {
        // Simple expression evaluation (basic arithmetic only)
        try
        {
            expression = expression.Replace(" ", "");
            return EvaluateSimpleExpression(expression);
        }
        catch
        {
            return null;
        }
    }

    private double? EvaluateSimpleExpression(string expr)
    {
        // Very basic expression parser for +, -, *, /
        if (string.IsNullOrEmpty(expr))
            return null;

        // Handle parentheses first (simple case)
        while (expr.Contains('('))
        {
            var start = expr.LastIndexOf('(');
            var end = expr.IndexOf(')', start);
            if (end < 0) return null;

            var inner = expr.Substring(start + 1, end - start - 1);
            var result = EvaluateSimpleExpression(inner);
            if (!result.HasValue) return null;

            expr = expr.Substring(0, start) + result.Value.ToString(CultureInfo.InvariantCulture) + expr.Substring(end + 1);
        }

        // Parse and evaluate
        var tokens = TokenizeExpression(expr);
        if (tokens == null || tokens.Count == 0) return null;

        return EvaluateTokens(tokens);
    }

    private List<object>? TokenizeExpression(string expr)
    {
        var tokens = new List<object>();
        var currentNumber = "";

        for (int i = 0; i < expr.Length; i++)
        {
            var c = expr[i];

            if (char.IsDigit(c) || c == '.' || (c == '-' && currentNumber.Length == 0 && (tokens.Count == 0 || tokens[^1] is char)))
            {
                currentNumber += c;
            }
            else if (c == '+' || c == '-' || c == '*' || c == '/')
            {
                if (currentNumber.Length > 0)
                {
                    if (double.TryParse(currentNumber, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
                    {
                        tokens.Add(num);
                    }
                    else
                    {
                        return null;
                    }
                    currentNumber = "";
                }
                tokens.Add(c);
            }
            else
            {
                return null;
            }
        }

        if (currentNumber.Length > 0)
        {
            if (double.TryParse(currentNumber, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
            {
                tokens.Add(num);
            }
            else
            {
                return null;
            }
        }

        return tokens;
    }

    private double? EvaluateTokens(List<object> tokens)
    {
        // Handle * and / first
        for (int i = 1; i < tokens.Count - 1; i++)
        {
            if (tokens[i] is char op && (op == '*' || op == '/'))
            {
                if (tokens[i - 1] is double left && tokens[i + 1] is double right)
                {
                    double result = op == '*' ? left * right : (right != 0 ? left / right : 0);
                    tokens[i - 1] = result;
                    tokens.RemoveAt(i);
                    tokens.RemoveAt(i);
                    i--;
                }
            }
        }

        // Handle + and -
        for (int i = 1; i < tokens.Count - 1; i++)
        {
            if (tokens[i] is char op && (op == '+' || op == '-'))
            {
                if (tokens[i - 1] is double left && tokens[i + 1] is double right)
                {
                    double result = op == '+' ? left + right : left - right;
                    tokens[i - 1] = result;
                    tokens.RemoveAt(i);
                    tokens.RemoveAt(i);
                    i--;
                }
            }
        }

        return tokens.Count == 1 && tokens[0] is double final ? final : null;
    }

    private string FormatValue(double value)
    {
        if (DecimalPlaces >= 0)
        {
            return value.ToString($"F{DecimalPlaces}", CultureInfo.CurrentCulture);
        }
        return value.ToString(NumberFormatter, CultureInfo.CurrentCulture);
    }

    #endregion

    #region Mouse Handling

    /// <inheritdoc />
    protected override int GetCaretIndexFromPosition(Point position)
    {
        // Check if click is on spin buttons
        if (SpinButtonPlacementMode != NumberBoxSpinButtonPlacementMode.Hidden)
        {
            if (_upButtonRect.Contains(position) || _downButtonRect.Contains(position))
            {
                return _caretIndex; // Don't move caret for button clicks
            }
        }

        return base.GetCaretIndexFromPosition(position);
    }

    #endregion

    #region Layout

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;
        var border = BorderThickness;
        var headerHeight = 0.0;

        if (Header is string headerText)
        {
            var headerFormatted = new FormattedText(headerText, FontFamily ?? "Segoe UI", FontSize > 0 ? FontSize : 14);
            TextMeasurement.MeasureText(headerFormatted);
            headerHeight = headerFormatted.Height + 4;
        }

        var width = double.IsPositiveInfinity(availableSize.Width) ? 120 : availableSize.Width;
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

        var bounds = new Rect(RenderSize);
        var padding = Padding;
        var border = BorderThickness;
        var cornerRadius = CornerRadius;
        var hasCornerRadius = cornerRadius.TopLeft > 0;
        var headerHeight = 0.0;
        var lineHeight = Math.Round(GetLineHeight());

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

        // Input area rect
        var inputRect = new Rect(0, headerHeight, bounds.Width, bounds.Height - headerHeight);

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
        var borderBrush = IsKeyboardFocused ? new SolidColorBrush(Color.FromRgb(0, 120, 212)) : BorderBrush;
        if (borderBrush != null && border.TotalWidth > 0)
        {
            var pen = new Pen(borderBrush, border.Left);
            if (hasCornerRadius)
            {
                dc.DrawRoundedRectangle(null, pen, inputRect, cornerRadius.TopLeft, cornerRadius.TopLeft);
            }
            else
            {
                dc.DrawRectangle(null, pen, inputRect);
            }
        }

        // Calculate regions
        var spinButtonWidth = SpinButtonPlacementMode == NumberBoxSpinButtonPlacementMode.Hidden ? 0 : SpinButtonWidth;
        _textRect = new Rect(
            padding.Left,
            inputRect.Top + padding.Top,
            inputRect.Width - spinButtonWidth - padding.Left - padding.Right,
            inputRect.Height - padding.Top - padding.Bottom);

        if (SpinButtonPlacementMode == NumberBoxSpinButtonPlacementMode.Inline)
        {
            _upButtonRect = new Rect(inputRect.Right - SpinButtonWidth, inputRect.Top, SpinButtonWidth, inputRect.Height / 2);
            _downButtonRect = new Rect(inputRect.Right - SpinButtonWidth, inputRect.Top + inputRect.Height / 2, SpinButtonWidth, inputRect.Height / 2);

            DrawSpinButton(dc, _upButtonRect, true);
            DrawSpinButton(dc, _downButtonRect, false);
        }

        // Clip to text area
        dc.PushClip(new RectangleGeometry(_textRect));

        // Draw selection background
        if (_selectionLength > 0 && IsKeyboardFocused)
        {
            DrawSelection(dc, _textRect, lineHeight);
        }

        // Draw text or placeholder
        var displayText = _text;
        if (string.IsNullOrEmpty(displayText) && !string.IsNullOrEmpty(PlaceholderText))
        {
            var placeholderFormatted = new FormattedText(PlaceholderText, FontFamily ?? "Segoe UI", FontSize > 0 ? FontSize : 14)
            {
                Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128))
            };
            TextMeasurement.MeasureText(placeholderFormatted);
            var textY = _textRect.Top + (_textRect.Height - placeholderFormatted.Height) / 2;
            dc.DrawText(placeholderFormatted, new Point(_textRect.Left - Math.Round(_horizontalOffset), textY));
        }
        else if (!string.IsNullOrEmpty(displayText))
        {
            var valueFormatted = new FormattedText(displayText, FontFamily ?? "Segoe UI", FontSize > 0 ? FontSize : 14)
            {
                Foreground = Foreground ?? new SolidColorBrush(Color.White)
            };
            TextMeasurement.MeasureText(valueFormatted);
            var textY = _textRect.Top + (_textRect.Height - valueFormatted.Height) / 2;
            dc.DrawText(valueFormatted, new Point(_textRect.Left - Math.Round(_horizontalOffset), textY));
        }

        // Draw IME composition
        if (_isImeComposing && !string.IsNullOrEmpty(_imeCompositionString))
        {
            DrawImeComposition(dc, _textRect, lineHeight);
        }

        // Draw caret
        if (IsFocused && !IsReadOnly)
        {
            DrawCaret(dc, _textRect, lineHeight);
        }

        dc.Pop(); // Pop clip
    }

    private void DrawSelection(DrawingContext dc, Rect contentRect, double lineHeight)
    {
        if (SelectionBrush == null)
            return;

        var roundedHorizontalOffset = Math.Round(_horizontalOffset);
        var textBefore = _text.Substring(0, Math.Min(_selectionStart, _text.Length));
        var selectedText = _text.Substring(_selectionStart, Math.Min(_selectionLength, _text.Length - _selectionStart));

        var startX = Math.Round(contentRect.X + MeasureTextWidth(textBefore) - roundedHorizontalOffset);
        var width = Math.Max(Math.Round(MeasureTextWidth(selectedText)), 1);
        var textY = contentRect.Top + (contentRect.Height - lineHeight) / 2;

        var selRect = new Rect(startX, textY, width, lineHeight);
        dc.DrawRectangle(SelectionBrush, null, selRect);
    }

    private void DrawImeComposition(DrawingContext dc, Rect contentRect, double lineHeight)
    {
        if (string.IsNullOrEmpty(_imeCompositionString))
            return;

        var roundedHorizontalOffset = Math.Round(_horizontalOffset);
        var textBeforeCaret = _text.Substring(0, Math.Min(_caretIndex, _text.Length));
        var x = Math.Round(contentRect.X + MeasureTextWidth(textBeforeCaret) - roundedHorizontalOffset);
        var textY = contentRect.Top + (contentRect.Height - lineHeight) / 2;

        var compositionWidth = MeasureTextWidth(_imeCompositionString);
        var compositionBgBrush = new SolidColorBrush(Color.FromRgb(60, 60, 80));
        dc.DrawRectangle(compositionBgBrush, null, new Rect(x, textY, compositionWidth, lineHeight));

        var compositionText = new FormattedText(_imeCompositionString, FontFamily ?? "Segoe UI", FontSize)
        {
            Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 200)),
            MaxTextWidth = contentRect.Width,
            MaxTextHeight = lineHeight
        };
        dc.DrawText(compositionText, new Point(x, textY));

        var underlinePen = new Pen(new SolidColorBrush(Color.FromRgb(200, 200, 100)), 1);
        dc.DrawLine(underlinePen, new Point(x, textY + lineHeight - 2), new Point(x + compositionWidth, textY + lineHeight - 2));
    }

    private void DrawCaret(DrawingContext dc, Rect contentRect, double lineHeight)
    {
        var caretOpacity = UpdateCaretAnimation();

        if (CaretBrush == null || _isImeComposing || caretOpacity < 0.01)
            return;

        var columnIndex = Math.Min(_caretIndex, _text.Length);
        var textBeforeCaret = _text.Substring(0, columnIndex);

        var roundedHorizontalOffset = Math.Round(_horizontalOffset);
        var x = Math.Round(contentRect.X + MeasureTextWidth(textBeforeCaret) - roundedHorizontalOffset);
        var textY = contentRect.Top + (contentRect.Height - lineHeight) / 2;

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

        var caretPen = new Pen(caretBrushWithOpacity, 1.5);
        dc.DrawLine(caretPen, new Point(x, textY), new Point(x, textY + lineHeight));
    }

    private void DrawSpinButton(DrawingContext dc, Rect rect, bool isUp)
    {
        var isHovered = isUp ? _isUpButtonHovered : _isDownButtonHovered;
        var isPressed = isUp ? _isUpButtonPressed : _isDownButtonPressed;

        // Button background
        var buttonBg = isPressed ? new SolidColorBrush(Color.FromRgb(50, 50, 50))
            : isHovered ? new SolidColorBrush(Color.FromRgb(70, 70, 70))
            : new SolidColorBrush(Color.FromRgb(60, 60, 60));
        dc.DrawRectangle(buttonBg, null, rect);

        // Arrow
        var arrowBrush = new SolidColorBrush(Color.White);
        var arrowPen = new Pen(arrowBrush, 1.5);
        var centerX = rect.X + rect.Width / 2;
        var centerY = rect.Y + rect.Height / 2;
        var arrowSize = 4;

        if (isUp)
        {
            dc.DrawLine(arrowPen, new Point(centerX - arrowSize, centerY + arrowSize / 2), new Point(centerX, centerY - arrowSize / 2));
            dc.DrawLine(arrowPen, new Point(centerX, centerY - arrowSize / 2), new Point(centerX + arrowSize, centerY + arrowSize / 2));
        }
        else
        {
            dc.DrawLine(arrowPen, new Point(centerX - arrowSize, centerY - arrowSize / 2), new Point(centerX, centerY + arrowSize / 2));
            dc.DrawLine(arrowPen, new Point(centerX, centerY + arrowSize / 2), new Point(centerX + arrowSize, centerY - arrowSize / 2));
        }
    }

    #endregion

    #region Property Changed Callbacks

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NumberBox numberBox)
        {
            if (!numberBox._isEditing)
            {
                numberBox._isUpdatingValue = true;
                numberBox._text = numberBox.FormatValue((double)e.NewValue);
                numberBox._caretIndex = numberBox._text.Length;
                numberBox._isUpdatingValue = false;
            }

            numberBox.RaiseEvent(new RoutedPropertyChangedEventArgs<double>(
                (double)e.OldValue, (double)e.NewValue, ValueChangedEvent));
            numberBox.InvalidateVisual();
        }
    }

    private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NumberBox numberBox)
        {
            var coerced = (double)(CoerceValue(numberBox, numberBox.Value) ?? numberBox.Value);
            if (coerced != numberBox.Value)
            {
                numberBox.Value = coerced;
            }
        }
    }

    private static object? CoerceValue(DependencyObject d, object? value)
    {
        if (d is NumberBox numberBox && value is double doubleValue)
        {
            return Math.Clamp(doubleValue, numberBox.Minimum, numberBox.Maximum);
        }
        return value;
    }

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NumberBox numberBox)
        {
            numberBox.InvalidateMeasure();
        }
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NumberBox numberBox)
        {
            numberBox.InvalidateVisual();
        }
    }

    private static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NumberBox numberBox)
        {
            numberBox._text = numberBox.FormatValue(numberBox.Value);
            numberBox._caretIndex = numberBox._text.Length;
            numberBox.InvalidateVisual();
        }
    }

    #endregion

    #region IME Support

    /// <summary>
    /// Gets whether IME composition is currently active.
    /// </summary>
    public bool IsImeComposing => _isImeComposing;

    /// <inheritdoc />
    public Point GetImeCaretPosition()
    {
        var caretPos = GetCaretScreenPosition();

        var element = this as UIElement;
        var parent = element?.VisualParent;
        while (parent != null)
        {
            if (parent is FrameworkElement fe)
            {
                caretPos = new Point(caretPos.X + fe.Margin.Left, caretPos.Y + fe.Margin.Top);
            }
            if (parent is Window)
                break;
            parent = parent.VisualParent;
        }

        return caretPos;
    }

    private Point GetCaretScreenPosition()
    {
        var lineHeight = Math.Round(GetLineHeight());
        var columnIndex = Math.Min(_caretIndex, _text.Length);
        var textBeforeCaret = _text.Substring(0, columnIndex);

        double x = Padding.Left - _horizontalOffset + MeasureTextWidth(textBeforeCaret);
        double y = Padding.Top;

        return new Point(x, y + lineHeight);
    }

    /// <inheritdoc />
    public void OnImeCompositionStart()
    {
        _isImeComposing = true;
        _imeCompositionStart = _caretIndex;
        _imeCompositionString = string.Empty;
        _imeCompositionCursor = 0;

        if (_selectionLength > 0)
        {
            DeleteSelection();
        }

        InvalidateVisual();
    }

    /// <inheritdoc />
    public void OnImeCompositionUpdate(string compositionString, int cursorPosition)
    {
        _imeCompositionString = compositionString;
        _imeCompositionCursor = cursorPosition;
        InvalidateVisual();
    }

    /// <inheritdoc />
    public void OnImeCompositionEnd(string? resultString)
    {
        _isImeComposing = false;
        _imeCompositionString = string.Empty;
        _imeCompositionCursor = 0;
        InvalidateVisual();
    }

    #endregion
}

/// <summary>
/// Specifies the placement mode for NumberBox spin buttons.
/// </summary>
public enum NumberBoxSpinButtonPlacementMode
{
    /// <summary>
    /// Spin buttons are hidden.
    /// </summary>
    Hidden,

    /// <summary>
    /// Spin buttons are placed inline (right side).
    /// </summary>
    Inline,

    /// <summary>
    /// Spin buttons are placed compactly.
    /// </summary>
    Compact
}
