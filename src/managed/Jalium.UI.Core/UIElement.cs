namespace Jalium.UI;

/// <summary>
/// Base class for UI elements that participate in layout, input, and rendering.
/// </summary>
public abstract partial class UIElement : Visual, IInputElement
{
    #region Event Handlers

    private Dictionary<RoutedEvent, List<RoutedEventHandlerInfo>>? _eventHandlers;

    /// <summary>
    /// Adds a handler for the specified routed event.
    /// </summary>
    /// <param name="routedEvent">The routed event.</param>
    /// <param name="handler">The event handler.</param>
    public void AddHandler(RoutedEvent routedEvent, Delegate handler)
    {
        AddHandler(routedEvent, handler, handledEventsToo: false);
    }

    /// <summary>
    /// Adds a handler for the specified routed event.
    /// </summary>
    /// <param name="routedEvent">The routed event.</param>
    /// <param name="handler">The event handler.</param>
    /// <param name="handledEventsToo">Whether to invoke the handler even if the event is already handled.</param>
    public void AddHandler(RoutedEvent routedEvent, Delegate handler, bool handledEventsToo)
    {
        ArgumentNullException.ThrowIfNull(routedEvent);
        ArgumentNullException.ThrowIfNull(handler);

        _eventHandlers ??= new Dictionary<RoutedEvent, List<RoutedEventHandlerInfo>>();

        if (!_eventHandlers.TryGetValue(routedEvent, out var handlers))
        {
            handlers = new List<RoutedEventHandlerInfo>();
            _eventHandlers[routedEvent] = handlers;
        }

        handlers.Add(new RoutedEventHandlerInfo(handler, handledEventsToo));
    }

    /// <summary>
    /// Removes a handler for the specified routed event.
    /// </summary>
    /// <param name="routedEvent">The routed event.</param>
    /// <param name="handler">The event handler to remove.</param>
    public void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
    {
        ArgumentNullException.ThrowIfNull(routedEvent);
        ArgumentNullException.ThrowIfNull(handler);

        if (_eventHandlers == null || !_eventHandlers.TryGetValue(routedEvent, out var handlers))
        {
            return;
        }

        for (int i = handlers.Count - 1; i >= 0; i--)
        {
            if (handlers[i].Handler == handler)
            {
                handlers.RemoveAt(i);
                break;
            }
        }
    }

    /// <summary>
    /// Raises the specified routed event.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    public void RaiseEvent(RoutedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ArgumentNullException.ThrowIfNull(e.RoutedEvent);

        e.SetOriginalSource(this);
        e.Source ??= this;

        switch (e.RoutedEvent.RoutingStrategy)
        {
            case RoutingStrategy.Direct:
                RaiseEventDirect(e);
                break;

            case RoutingStrategy.Bubble:
                RaiseEventBubble(e);
                break;

            case RoutingStrategy.Tunnel:
                RaiseEventTunnel(e);
                break;
        }
    }

    private void RaiseEventDirect(RoutedEventArgs e)
    {
        InvokeHandlers(this, e);
    }

    private void RaiseEventBubble(RoutedEventArgs e)
    {
        UIElement? current = this;
        while (current != null)
        {
            current.InvokeHandlers(current, e);
            current = current.VisualParent as UIElement;
        }
    }

    // Reusable list for tunnel event path to avoid allocations on every mouse move
    [ThreadStatic]
    private static List<UIElement>? _tunnelPath;

    private void RaiseEventTunnel(RoutedEventArgs e)
    {
        // Reuse static list to avoid allocations
        _tunnelPath ??= new List<UIElement>(32);
        var path = _tunnelPath;
        path.Clear();

        // Build the path from root to source
        UIElement? current = this;
        while (current != null)
        {
            path.Add(current);
            current = current.VisualParent as UIElement;
        }

        // Tunnel from root to source
        for (int i = path.Count - 1; i >= 0; i--)
        {
            path[i].InvokeHandlers(path[i], e);
        }
    }

    private void InvokeHandlers(object sender, RoutedEventArgs e)
    {
        var routedEvent = e.RoutedEvent!;

        // Invoke class handlers first
        foreach (var classHandler in EventManager.GetClassHandlers(routedEvent, GetType()))
        {
            if (!e.Handled || classHandler.HandledEventsToo)
            {
                e.InvokeEventHandler(classHandler.Handler, sender);
            }
        }

        // Invoke instance handlers
        if (_eventHandlers != null && _eventHandlers.TryGetValue(routedEvent, out var handlers))
        {
            foreach (var handler in handlers)
            {
                if (!e.Handled || handler.HandledEventsToo)
                {
                    e.InvokeEventHandler(handler.Handler, sender);
                }
            }
        }
    }

    #endregion

    #region Dependency Properties

    /// <summary>
    /// Identifies the Visibility dependency property.
    /// </summary>
    public static readonly DependencyProperty VisibilityProperty =
        DependencyProperty.Register(nameof(Visibility), typeof(Visibility), typeof(UIElement),
            new PropertyMetadata(Visibility.Visible, OnVisibilityChanged));

    /// <summary>
    /// Identifies the IsEnabled dependency property.
    /// </summary>
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(UIElement),
            new PropertyMetadata(true, OnIsEnabledChanged));

    /// <summary>
    /// Identifies the Opacity dependency property.
    /// </summary>
    public static readonly DependencyProperty OpacityProperty =
        DependencyProperty.Register(nameof(Opacity), typeof(double), typeof(UIElement),
            new PropertyMetadata(1.0));

    /// <summary>
    /// Identifies the BackdropEffect dependency property.
    /// </summary>
    public static readonly DependencyProperty BackdropEffectProperty =
        DependencyProperty.Register(nameof(BackdropEffect), typeof(IBackdropEffect), typeof(UIElement),
            new PropertyMetadata(null, OnBackdropEffectChanged));

    /// <summary>
    /// Identifies the Effect dependency property.
    /// This is for element-level bitmap effects like DropShadowEffect, distinct from BackdropEffect.
    /// </summary>
    public static readonly DependencyProperty EffectProperty =
        DependencyProperty.Register(nameof(Effect), typeof(object), typeof(UIElement),
            new PropertyMetadata(null, OnEffectChanged));

    /// <summary>
    /// Identifies the OpacityMask dependency property.
    /// </summary>
    public static readonly DependencyProperty OpacityMaskProperty =
        DependencyProperty.Register(nameof(OpacityMask), typeof(object), typeof(UIElement),
            new PropertyMetadata(null, OnOpacityMaskChanged));

    /// <summary>
    /// Identifies the RenderTransform dependency property.
    /// </summary>
    public static readonly DependencyProperty RenderTransformProperty =
        DependencyProperty.Register(nameof(RenderTransform), typeof(object), typeof(UIElement),
            new PropertyMetadata(null));

    /// <summary>
    /// Identifies the Focusable dependency property.
    /// </summary>
    public static readonly DependencyProperty FocusableProperty =
        DependencyProperty.Register(nameof(Focusable), typeof(bool), typeof(UIElement),
            new PropertyMetadata(false));

    /// <summary>
    /// Identifies the IsMouseOver read-only dependency property key.
    /// </summary>
    private static readonly DependencyPropertyKey IsMouseOverPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(IsMouseOver), typeof(bool), typeof(UIElement),
            new PropertyMetadata(false, OnIsMouseOverChanged));

    /// <summary>
    /// Identifies the IsMouseOver dependency property.
    /// </summary>
    public static readonly DependencyProperty IsMouseOverProperty = IsMouseOverPropertyKey.DependencyProperty;

    /// <summary>
    /// Identifies the ClipToBounds dependency property.
    /// </summary>
    public static readonly DependencyProperty ClipToBoundsProperty =
        DependencyProperty.Register(nameof(ClipToBounds), typeof(bool), typeof(UIElement),
            new PropertyMetadata(false, OnClipToBoundsChanged));

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the visibility of this element.
    /// </summary>
    public Visibility Visibility
    {
        get => (Visibility)(GetValue(VisibilityProperty) ?? Visibility.Visible);
        set => SetValue(VisibilityProperty, value);
    }

    /// <summary>
    /// Gets or sets whether this element is enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => (bool)(GetValue(IsEnabledProperty) ?? true);
        set => SetValue(IsEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets the opacity of this element.
    /// </summary>
    public double Opacity
    {
        get => (double)(GetValue(OpacityProperty) ?? 1.0);
        set => SetValue(OpacityProperty, value);
    }

    /// <summary>
    /// Gets or sets the backdrop effect.
    /// Use implementations like BlurEffect, AcrylicEffect, MicaEffect, etc.
    /// </summary>
    public IBackdropEffect? BackdropEffect
    {
        get => (IBackdropEffect?)GetValue(BackdropEffectProperty);
        set => SetValue(BackdropEffectProperty, value);
    }

    /// <summary>
    /// Gets or sets the bitmap effect applied to the element's rendered content.
    /// Use DropShadowEffect, ElementBlurEffect, etc. from Jalium.UI.Media.Effects.
    /// This is distinct from BackdropEffect which affects content behind the element.
    /// </summary>
    public object? Effect
    {
        get => GetValue(EffectProperty);
        set => SetValue(EffectProperty, value);
    }

    /// <summary>
    /// Gets or sets a brush that specifies the opacity mask for this element.
    /// The alpha channel of the brush determines the opacity of corresponding parts of the element.
    /// </summary>
    public object? OpacityMask
    {
        get => GetValue(OpacityMaskProperty);
        set => SetValue(OpacityMaskProperty, value);
    }

    /// <summary>
    /// Gets or sets the render transform.
    /// </summary>
    public object? RenderTransform
    {
        get => GetValue(RenderTransformProperty);
        set => SetValue(RenderTransformProperty, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates whether this element can receive focus.
    /// </summary>
    public bool Focusable
    {
        get => (bool)(GetValue(FocusableProperty) ?? false);
        set => SetValue(FocusableProperty, value);
    }

    /// <summary>
    /// Gets a value indicating whether the mouse pointer is over this element.
    /// </summary>
    public bool IsMouseOver => (bool)(GetValue(IsMouseOverProperty) ?? false);

    /// <summary>
    /// Sets the IsMouseOver property value. Called internally by mouse tracking.
    /// </summary>
    internal void SetIsMouseOver(bool value)
    {
        SetValue(IsMouseOverPropertyKey.DependencyProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to clip the content of this element to its bounds.
    /// </summary>
    public bool ClipToBounds
    {
        get => (bool)(GetValue(ClipToBoundsProperty) ?? false);
        set => SetValue(ClipToBoundsProperty, value);
    }

    #endregion

    #region Focus

    private bool _isKeyboardFocused;
    private bool _isKeyboardFocusWithin;

    /// <summary>
    /// Gets a value indicating whether this element has keyboard focus.
    /// </summary>
    public bool IsKeyboardFocused => _isKeyboardFocused;

    /// <summary>
    /// Gets a value indicating whether keyboard focus is anywhere within this element or its visual subtree.
    /// </summary>
    public bool IsKeyboardFocusWithin => _isKeyboardFocusWithin;

    /// <summary>
    /// Gets a value indicating whether this element has logical focus.
    /// </summary>
    public bool IsFocused => IsKeyboardFocused;

    /// <summary>
    /// Attempts to set focus to this element.
    /// </summary>
    /// <returns>True if focus was successfully set; otherwise, false.</returns>
    public bool Focus()
    {
        if (!Focusable || !IsEnabled || Visibility != Visibility.Visible)
        {
            return false;
        }

        var result = FocusService.Focus(this);
        return result == this;
    }

    /// <summary>
    /// Moves focus from this element.
    /// </summary>
    /// <param name="direction">The direction to move focus.</param>
    /// <returns>True if focus was successfully moved; otherwise, false.</returns>
    public bool MoveFocus(FocusNavigationDirection direction)
    {
        return FocusService.MoveFocus(this, direction);
    }

    /// <summary>
    /// Updates the IsKeyboardFocused state. Called by the Keyboard class.
    /// </summary>
    internal void UpdateIsKeyboardFocused(bool isFocused)
    {
        if (_isKeyboardFocused != isFocused)
        {
            _isKeyboardFocused = isFocused;
            OnIsKeyboardFocusedChanged(isFocused);
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Updates the IsKeyboardFocusWithin state. Called by the Keyboard class.
    /// </summary>
    internal void UpdateIsKeyboardFocusWithin(bool isFocusWithin)
    {
        if (_isKeyboardFocusWithin != isFocusWithin)
        {
            _isKeyboardFocusWithin = isFocusWithin;
            OnIsKeyboardFocusWithinChanged(isFocusWithin);
        }
    }

    /// <summary>
    /// Called when the IsKeyboardFocused property changes.
    /// </summary>
    protected virtual void OnIsKeyboardFocusedChanged(bool isFocused)
    {
    }

    /// <summary>
    /// Called when the IsKeyboardFocusWithin property changes.
    /// </summary>
    protected virtual void OnIsKeyboardFocusWithinChanged(bool isFocusWithin)
    {
    }

    #endregion

    #region Keyboard Focus Events

    /// <summary>
    /// Identifies the PreviewGotKeyboardFocus routed event.
    /// </summary>
    public static readonly RoutedEvent PreviewGotKeyboardFocusEvent =
        FocusService.PreviewGotKeyboardFocusEvent.AddOwner(typeof(UIElement));

    /// <summary>
    /// Identifies the GotKeyboardFocus routed event.
    /// </summary>
    public static readonly RoutedEvent GotKeyboardFocusEvent =
        FocusService.GotKeyboardFocusEvent.AddOwner(typeof(UIElement));

    /// <summary>
    /// Identifies the PreviewLostKeyboardFocus routed event.
    /// </summary>
    public static readonly RoutedEvent PreviewLostKeyboardFocusEvent =
        FocusService.PreviewLostKeyboardFocusEvent.AddOwner(typeof(UIElement));

    /// <summary>
    /// Identifies the LostKeyboardFocus routed event.
    /// </summary>
    public static readonly RoutedEvent LostKeyboardFocusEvent =
        FocusService.LostKeyboardFocusEvent.AddOwner(typeof(UIElement));

    /// <summary>
    /// Identifies the GotFocus routed event.
    /// </summary>
    public static readonly RoutedEvent GotFocusEvent =
        FocusService.GotFocusEvent.AddOwner(typeof(UIElement));

    /// <summary>
    /// Identifies the LostFocus routed event.
    /// </summary>
    public static readonly RoutedEvent LostFocusEvent =
        FocusService.LostFocusEvent.AddOwner(typeof(UIElement));

    /// <summary>
    /// Occurs when keyboard focus is received (tunnel).
    /// </summary>
    public event KeyboardFocusChangedEventHandler PreviewGotKeyboardFocus
    {
        add => AddHandler(PreviewGotKeyboardFocusEvent, value);
        remove => RemoveHandler(PreviewGotKeyboardFocusEvent, value);
    }

    /// <summary>
    /// Occurs when keyboard focus is received (bubble).
    /// </summary>
    public event KeyboardFocusChangedEventHandler GotKeyboardFocus
    {
        add => AddHandler(GotKeyboardFocusEvent, value);
        remove => RemoveHandler(GotKeyboardFocusEvent, value);
    }

    /// <summary>
    /// Occurs when keyboard focus is lost (tunnel).
    /// </summary>
    public event KeyboardFocusChangedEventHandler PreviewLostKeyboardFocus
    {
        add => AddHandler(PreviewLostKeyboardFocusEvent, value);
        remove => RemoveHandler(PreviewLostKeyboardFocusEvent, value);
    }

    /// <summary>
    /// Occurs when keyboard focus is lost (bubble).
    /// </summary>
    public event KeyboardFocusChangedEventHandler LostKeyboardFocus
    {
        add => AddHandler(LostKeyboardFocusEvent, value);
        remove => RemoveHandler(LostKeyboardFocusEvent, value);
    }

    /// <summary>
    /// Occurs when this element receives logical focus.
    /// </summary>
    public event RoutedEventHandler GotFocus
    {
        add => AddHandler(GotFocusEvent, value);
        remove => RemoveHandler(GotFocusEvent, value);
    }

    /// <summary>
    /// Occurs when this element loses logical focus.
    /// </summary>
    public event RoutedEventHandler LostFocus
    {
        add => AddHandler(LostFocusEvent, value);
        remove => RemoveHandler(LostFocusEvent, value);
    }

    #endregion

    #region Layout

    private Size _desiredSize;
    private Size _renderSize;
    private bool _isMeasureValid;
    private bool _isArrangeValid;

    /// <summary>
    /// Gets the desired size computed during the measure pass.
    /// </summary>
    public Size DesiredSize => _desiredSize;

    /// <summary>
    /// Gets the final render size after arrangement.
    /// </summary>
    public Size RenderSize => _renderSize;

    /// <summary>
    /// Gets the visual bounds of this element in parent coordinates.
    /// Override in FrameworkElement to provide actual bounds.
    /// </summary>
    public virtual Rect VisualBounds => new Rect(0, 0, _renderSize.Width, _renderSize.Height);

    /// <summary>
    /// Returns a geometry for clipping the contents of this element.
    /// Override in derived classes to provide custom clipping (e.g., ScrollViewer).
    /// When ClipToBounds is true, returns a Rect matching the element's RenderSize.
    /// </summary>
    /// <returns>The clipping geometry (Media.Geometry or Rect), or null if no clipping should be applied.</returns>
    protected internal virtual object? GetLayoutClip()
    {
        if (ClipToBounds)
        {
            return new Rect(0, 0, _renderSize.Width, _renderSize.Height);
        }
        return null;
    }

    /// <summary>
    /// Gets a value indicating whether the measure pass is valid.
    /// </summary>
    public bool IsMeasureValid => _isMeasureValid;

    /// <summary>
    /// Gets a value indicating whether the arrange pass is valid.
    /// </summary>
    public bool IsArrangeValid => _isArrangeValid;

    /// <summary>
    /// Invalidates the measure pass for this element.
    /// </summary>
    public void InvalidateMeasure()
    {
        _isMeasureValid = false;
        // Schedule layout and render update
        ScheduleRender();
    }

    /// <summary>
    /// Invalidates the arrange pass for this element.
    /// </summary>
    public void InvalidateArrange()
    {
        _isArrangeValid = false;
        // Schedule layout and render update
        ScheduleRender();
    }

    /// <summary>
    /// Invalidates the visual rendering of this element.
    /// </summary>
    public void InvalidateVisual()
    {
        // Schedule render update
        ScheduleRender();
    }

    /// <summary>
    /// Schedules a render by finding the root window and invalidating it.
    /// </summary>
    private void ScheduleRender()
    {
        // Find the root window by traversing up the visual tree
        Visual? current = this;
        while (current != null)
        {
            if (current is IWindowHost windowHost)
            {
                windowHost.InvalidateWindow();
                return;
            }
            current = current.VisualParent;
        }
    }

    /// <summary>
    /// Updates the desired size of this element.
    /// </summary>
    /// <param name="availableSize">The available size for the element.</param>
    public void Measure(Size availableSize)
    {
        if (Visibility == Visibility.Collapsed)
        {
            _desiredSize = Size.Empty;
            _isMeasureValid = true;
            return;
        }

        _desiredSize = MeasureCore(availableSize);
        _isMeasureValid = true;
    }

    /// <summary>
    /// Positions child elements and determines a size for this element.
    /// </summary>
    /// <param name="finalRect">The final area for this element.</param>
    public void Arrange(Rect finalRect)
    {
        if (Visibility == Visibility.Collapsed)
        {
            _renderSize = Size.Empty;
            _isArrangeValid = true;
            return;
        }

        _renderSize = ArrangeCore(finalRect);
        _isArrangeValid = true;
    }

    /// <summary>
    /// Override to implement custom measure logic.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The desired size.</returns>
    protected virtual Size MeasureCore(Size availableSize)
    {
        return Size.Empty;
    }

    /// <summary>
    /// Override to implement custom arrange logic.
    /// </summary>
    /// <param name="finalRect">The final rectangle.</param>
    /// <returns>The render size.</returns>
    protected virtual Size ArrangeCore(Rect finalRect)
    {
        return new Size(finalRect.Width, finalRect.Height);
    }

    #endregion

    #region Property Changed Callbacks

    private static void OnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            element.InvalidateMeasure();
        }
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            element.OnIsEnabledChanged((bool)(e.OldValue ?? true), (bool)(e.NewValue ?? true));
        }
    }

    /// <summary>
    /// Called when the IsEnabled property changes.
    /// </summary>
    protected virtual void OnIsEnabledChanged(bool oldValue, bool newValue)
    {
    }

    private static void OnIsMouseOverChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            element.OnIsMouseOverChanged((bool)(e.OldValue ?? false), (bool)(e.NewValue ?? false));
        }
    }

    /// <summary>
    /// Called when the IsMouseOver property changes.
    /// </summary>
    protected virtual void OnIsMouseOverChanged(bool oldValue, bool newValue)
    {
        InvalidateVisual();
    }

    private static void OnBackdropEffectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            element.OnBackdropEffectChanged((IBackdropEffect?)e.OldValue, (IBackdropEffect?)e.NewValue);
        }
    }

    /// <summary>
    /// Called when the BackdropEffect property changes.
    /// </summary>
    protected virtual void OnBackdropEffectChanged(IBackdropEffect? oldValue, IBackdropEffect? newValue)
    {
        InvalidateVisual();
    }

    private static void OnEffectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            element.OnEffectChanged(e.OldValue, e.NewValue);
        }
    }

    /// <summary>
    /// Called when the Effect property changes.
    /// </summary>
    protected virtual void OnEffectChanged(object? oldValue, object? newValue)
    {
        // Unsubscribe from old effect changes
        if (oldValue is IEffect oldEffect)
        {
            oldEffect.EffectChanged -= OnEffectPropertyChanged;
        }

        // Subscribe to new effect changes
        if (newValue is IEffect newEffect)
        {
            newEffect.EffectChanged += OnEffectPropertyChanged;
        }

        InvalidateVisual();
    }

    private void OnEffectPropertyChanged(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    private static void OnOpacityMaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            element.InvalidateVisual();
        }
    }

    private static void OnClipToBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            element.InvalidateVisual();
        }
    }

    #endregion

    #region Mouse Capture

    private static UIElement? _mouseCaptured;

    /// <summary>
    /// Gets the element that currently has mouse capture.
    /// </summary>
    public static UIElement? MouseCapturedElement => _mouseCaptured;

    /// <summary>
    /// Gets a value indicating whether this element has captured the mouse.
    /// </summary>
    public bool IsMouseCaptured => _mouseCaptured == this;

    /// <summary>
    /// Gets a value indicating whether the mouse is captured to this element or any of its child elements.
    /// </summary>
    public bool IsMouseCaptureWithin
    {
        get
        {
            var captured = _mouseCaptured;
            if (captured == null) return false;
            if (captured == this) return true;

            // Check if captured element is a descendant
            Visual? current = captured;
            while (current != null)
            {
                if (current == this) return true;
                current = current.VisualParent;
            }
            return false;
        }
    }

    /// <summary>
    /// Captures the mouse to this element.
    /// </summary>
    /// <returns>True if capture was successful; otherwise, false.</returns>
    public bool CaptureMouse()
    {
        if (!IsEnabled || Visibility != Visibility.Visible)
        {
            return false;
        }

        var previousCaptured = _mouseCaptured;
        _mouseCaptured = this;

        // Notify the previously captured element
        if (previousCaptured != null && previousCaptured != this)
        {
            previousCaptured.RaiseMouseCaptureChanged(false);
        }

        // Notify the newly captured element
        RaiseMouseCaptureChanged(true);
        return true;
    }

    /// <summary>
    /// Releases mouse capture from this element.
    /// </summary>
    public void ReleaseMouseCapture()
    {
        if (_mouseCaptured == this)
        {
            _mouseCaptured = null;
            RaiseMouseCaptureChanged(false);
        }
    }

    /// <summary>
    /// Forces release of mouse capture from any element.
    /// </summary>
    internal static void ForceReleaseMouseCapture()
    {
        var captured = _mouseCaptured;
        if (captured != null)
        {
            _mouseCaptured = null;
            captured.RaiseMouseCaptureChanged(false);
        }
    }

    /// <summary>
    /// Identifies the GotMouseCapture routed event.
    /// </summary>
    public static readonly RoutedEvent GotMouseCaptureEvent =
        EventManager.RegisterRoutedEvent(nameof(GotMouseCapture), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UIElement));

    /// <summary>
    /// Identifies the LostMouseCapture routed event.
    /// </summary>
    public static readonly RoutedEvent LostMouseCaptureEvent =
        EventManager.RegisterRoutedEvent(nameof(LostMouseCapture), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UIElement));

    /// <summary>
    /// Occurs when this element captures the mouse.
    /// </summary>
    public event RoutedEventHandler GotMouseCapture
    {
        add => AddHandler(GotMouseCaptureEvent, value);
        remove => RemoveHandler(GotMouseCaptureEvent, value);
    }

    /// <summary>
    /// Occurs when this element loses mouse capture.
    /// </summary>
    public event RoutedEventHandler LostMouseCapture
    {
        add => AddHandler(LostMouseCaptureEvent, value);
        remove => RemoveHandler(LostMouseCaptureEvent, value);
    }

    /// <summary>
    /// Called when mouse capture changes for this element.
    /// </summary>
    /// <param name="captured">True if this element now has capture; false if it lost capture.</param>
    internal void RaiseMouseCaptureChanged(bool captured)
    {
        if (captured)
        {
            OnGotMouseCapture();
            var args = new RoutedEventArgs(GotMouseCaptureEvent, this);
            RaiseEvent(args);
        }
        else
        {
            OnLostMouseCapture();
            var args = new RoutedEventArgs(LostMouseCaptureEvent, this);
            RaiseEvent(args);
        }
    }

    /// <summary>
    /// Called when this element captures the mouse.
    /// </summary>
    protected virtual void OnGotMouseCapture()
    {
    }

    /// <summary>
    /// Called when this element loses mouse capture.
    /// </summary>
    protected virtual void OnLostMouseCapture()
    {
    }

    #endregion

    #region Routed Input Events

    /// <summary>
    /// Identifies the PreviewKeyDown routed event.
    /// </summary>
    public static readonly RoutedEvent PreviewKeyDownEvent =
        EventManager.RegisterRoutedEvent(nameof(PreviewKeyDown), RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(UIElement));

    /// <summary>
    /// Identifies the KeyDown routed event.
    /// </summary>
    public static readonly RoutedEvent KeyDownEvent =
        EventManager.RegisterRoutedEvent(nameof(KeyDown), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UIElement));

    /// <summary>
    /// Identifies the PreviewKeyUp routed event.
    /// </summary>
    public static readonly RoutedEvent PreviewKeyUpEvent =
        EventManager.RegisterRoutedEvent(nameof(PreviewKeyUp), RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(UIElement));

    /// <summary>
    /// Identifies the KeyUp routed event.
    /// </summary>
    public static readonly RoutedEvent KeyUpEvent =
        EventManager.RegisterRoutedEvent(nameof(KeyUp), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UIElement));

    /// <summary>
    /// Identifies the PreviewTextInput routed event.
    /// </summary>
    public static readonly RoutedEvent PreviewTextInputEvent =
        EventManager.RegisterRoutedEvent(nameof(PreviewTextInput), RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(UIElement));

    /// <summary>
    /// Identifies the TextInput routed event.
    /// </summary>
    public static readonly RoutedEvent TextInputEvent =
        EventManager.RegisterRoutedEvent(nameof(TextInput), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UIElement));

    /// <summary>
    /// Identifies the PreviewMouseDown routed event.
    /// </summary>
    public static readonly RoutedEvent PreviewMouseDownEvent =
        EventManager.RegisterRoutedEvent(nameof(PreviewMouseDown), RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(UIElement));

    /// <summary>
    /// Identifies the MouseDown routed event.
    /// </summary>
    public static readonly RoutedEvent MouseDownEvent =
        EventManager.RegisterRoutedEvent(nameof(MouseDown), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UIElement));

    /// <summary>
    /// Identifies the PreviewMouseUp routed event.
    /// </summary>
    public static readonly RoutedEvent PreviewMouseUpEvent =
        EventManager.RegisterRoutedEvent(nameof(PreviewMouseUp), RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(UIElement));

    /// <summary>
    /// Identifies the MouseUp routed event.
    /// </summary>
    public static readonly RoutedEvent MouseUpEvent =
        EventManager.RegisterRoutedEvent(nameof(MouseUp), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UIElement));

    /// <summary>
    /// Identifies the PreviewMouseMove routed event.
    /// </summary>
    public static readonly RoutedEvent PreviewMouseMoveEvent =
        EventManager.RegisterRoutedEvent(nameof(PreviewMouseMove), RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(UIElement));

    /// <summary>
    /// Identifies the MouseMove routed event.
    /// </summary>
    public static readonly RoutedEvent MouseMoveEvent =
        EventManager.RegisterRoutedEvent(nameof(MouseMove), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UIElement));

    /// <summary>
    /// Identifies the MouseEnter routed event.
    /// </summary>
    public static readonly RoutedEvent MouseEnterEvent =
        EventManager.RegisterRoutedEvent(nameof(MouseEnter), RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(UIElement));

    /// <summary>
    /// Identifies the MouseLeave routed event.
    /// </summary>
    public static readonly RoutedEvent MouseLeaveEvent =
        EventManager.RegisterRoutedEvent(nameof(MouseLeave), RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(UIElement));

    /// <summary>
    /// Identifies the PreviewMouseWheel routed event.
    /// </summary>
    public static readonly RoutedEvent PreviewMouseWheelEvent =
        EventManager.RegisterRoutedEvent(nameof(PreviewMouseWheel), RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(UIElement));

    /// <summary>
    /// Identifies the MouseWheel routed event.
    /// </summary>
    public static readonly RoutedEvent MouseWheelEvent =
        EventManager.RegisterRoutedEvent(nameof(MouseWheel), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UIElement));

    /// <summary>
    /// Occurs when a key is pressed (tunnel).
    /// </summary>
    public event RoutedEventHandler PreviewKeyDown
    {
        add => AddHandler(PreviewKeyDownEvent, value);
        remove => RemoveHandler(PreviewKeyDownEvent, value);
    }

    /// <summary>
    /// Occurs when a key is pressed (bubble).
    /// </summary>
    public event RoutedEventHandler KeyDown
    {
        add => AddHandler(KeyDownEvent, value);
        remove => RemoveHandler(KeyDownEvent, value);
    }

    /// <summary>
    /// Occurs when a key is released (tunnel).
    /// </summary>
    public event RoutedEventHandler PreviewKeyUp
    {
        add => AddHandler(PreviewKeyUpEvent, value);
        remove => RemoveHandler(PreviewKeyUpEvent, value);
    }

    /// <summary>
    /// Occurs when a key is released (bubble).
    /// </summary>
    public event RoutedEventHandler KeyUp
    {
        add => AddHandler(KeyUpEvent, value);
        remove => RemoveHandler(KeyUpEvent, value);
    }

    /// <summary>
    /// Occurs when text is input (tunnel).
    /// </summary>
    public event RoutedEventHandler PreviewTextInput
    {
        add => AddHandler(PreviewTextInputEvent, value);
        remove => RemoveHandler(PreviewTextInputEvent, value);
    }

    /// <summary>
    /// Occurs when text is input (bubble).
    /// </summary>
    public event RoutedEventHandler TextInput
    {
        add => AddHandler(TextInputEvent, value);
        remove => RemoveHandler(TextInputEvent, value);
    }

    /// <summary>
    /// Occurs when a mouse button is pressed (tunnel).
    /// </summary>
    public event RoutedEventHandler PreviewMouseDown
    {
        add => AddHandler(PreviewMouseDownEvent, value);
        remove => RemoveHandler(PreviewMouseDownEvent, value);
    }

    /// <summary>
    /// Occurs when a mouse button is pressed (bubble).
    /// </summary>
    public event RoutedEventHandler MouseDown
    {
        add => AddHandler(MouseDownEvent, value);
        remove => RemoveHandler(MouseDownEvent, value);
    }

    /// <summary>
    /// Occurs when a mouse button is released (tunnel).
    /// </summary>
    public event RoutedEventHandler PreviewMouseUp
    {
        add => AddHandler(PreviewMouseUpEvent, value);
        remove => RemoveHandler(PreviewMouseUpEvent, value);
    }

    /// <summary>
    /// Occurs when a mouse button is released (bubble).
    /// </summary>
    public event RoutedEventHandler MouseUp
    {
        add => AddHandler(MouseUpEvent, value);
        remove => RemoveHandler(MouseUpEvent, value);
    }

    /// <summary>
    /// Occurs when the mouse moves (tunnel).
    /// </summary>
    public event RoutedEventHandler PreviewMouseMove
    {
        add => AddHandler(PreviewMouseMoveEvent, value);
        remove => RemoveHandler(PreviewMouseMoveEvent, value);
    }

    /// <summary>
    /// Occurs when the mouse moves (bubble).
    /// </summary>
    public event RoutedEventHandler MouseMove
    {
        add => AddHandler(MouseMoveEvent, value);
        remove => RemoveHandler(MouseMoveEvent, value);
    }

    /// <summary>
    /// Occurs when the mouse enters this element.
    /// </summary>
    public event RoutedEventHandler MouseEnter
    {
        add => AddHandler(MouseEnterEvent, value);
        remove => RemoveHandler(MouseEnterEvent, value);
    }

    /// <summary>
    /// Occurs when the mouse leaves this element.
    /// </summary>
    public event RoutedEventHandler MouseLeave
    {
        add => AddHandler(MouseLeaveEvent, value);
        remove => RemoveHandler(MouseLeaveEvent, value);
    }

    /// <summary>
    /// Occurs when the mouse wheel is rotated (tunnel).
    /// </summary>
    public event RoutedEventHandler PreviewMouseWheel
    {
        add => AddHandler(PreviewMouseWheelEvent, value);
        remove => RemoveHandler(PreviewMouseWheelEvent, value);
    }

    /// <summary>
    /// Occurs when the mouse wheel is rotated (bubble).
    /// </summary>
    public event RoutedEventHandler MouseWheel
    {
        add => AddHandler(MouseWheelEvent, value);
        remove => RemoveHandler(MouseWheelEvent, value);
    }

    #endregion

    #region Animation

    /// <summary>
    /// Tracks active animations on this element.
    /// </summary>
    private Dictionary<DependencyProperty, ElementAnimation>? _activeAnimations;
    private System.Threading.Timer? _animationTimer;
    private static readonly object _animationTimerLock = new();

    private sealed class ElementAnimation
    {
        public IAnimationTimeline Animation { get; }
        public IAnimationClock Clock { get; }
        public object? BaseValue { get; }

        public ElementAnimation(IAnimationTimeline animation, IAnimationClock clock, object? baseValue)
        {
            Animation = animation;
            Clock = clock;
            BaseValue = baseValue;
        }
    }

    /// <summary>
    /// Starts an animation for the specified dependency property.
    /// </summary>
    /// <param name="dp">The dependency property to animate.</param>
    /// <param name="animation">The animation timeline, or null to stop any existing animation.</param>
    public void BeginAnimation(DependencyProperty dp, IAnimationTimeline? animation)
    {
        BeginAnimation(dp, animation, HandoffBehavior.SnapshotAndReplace);
    }

    /// <summary>
    /// Starts an animation for the specified dependency property with a handoff behavior.
    /// </summary>
    /// <param name="dp">The dependency property to animate.</param>
    /// <param name="animation">The animation timeline, or null to stop any existing animation.</param>
    /// <param name="handoffBehavior">How to handle existing animations (currently only Replace is supported).</param>
    public void BeginAnimation(DependencyProperty dp, IAnimationTimeline? animation, HandoffBehavior handoffBehavior)
    {
        ArgumentNullException.ThrowIfNull(dp);

        _activeAnimations ??= new Dictionary<DependencyProperty, ElementAnimation>();

        // Stop any existing animation on this property
        if (_activeAnimations.TryGetValue(dp, out var existing))
        {
            existing.Clock.Stop();
            existing.Clock.Completed -= OnAnimationClockCompleted;
            ClearAnimatedValue(dp);
            _activeAnimations.Remove(dp);
        }

        if (animation == null)
        {
            StopAnimationTimerIfNeeded();
            return;
        }

        // Store base value and create clock
        var baseValue = GetAnimationBaseValue(dp);
        var clock = animation.CreateClock();

        _activeAnimations[dp] = new ElementAnimation(animation, clock, baseValue);

        // Subscribe to completion
        clock.Completed += OnAnimationClockCompleted;

        // Start the clock
        clock.Begin();

        // Start the animation timer
        StartAnimationTimerIfNeeded();

        // Set initial animated value
        UpdateAnimatedValue(dp);
    }

    /// <summary>
    /// Gets a value indicating whether the specified property has an active animation.
    /// </summary>
    /// <param name="dp">The dependency property to check.</param>
    /// <returns>True if the property has an active animation; otherwise, false.</returns>
    public bool HasAnimation(DependencyProperty dp)
    {
        return _activeAnimations?.ContainsKey(dp) == true;
    }

    private void OnAnimationClockCompleted(object? sender, EventArgs e)
    {
        if (sender is not IAnimationClock clock)
            return;

        // Find the property this clock belongs to
        if (_activeAnimations == null)
            return;

        DependencyProperty? completedProperty = null;
        ElementAnimation? completedAnimation = null;

        foreach (var (dp, anim) in _activeAnimations)
        {
            if (anim.Clock == clock)
            {
                completedProperty = dp;
                completedAnimation = anim;
                break;
            }
        }

        if (completedProperty == null || completedAnimation == null)
            return;

        // Handle fill behavior
        var fillBehavior = completedAnimation.Animation.AnimationFillBehavior;

        if (fillBehavior == AnimationFillBehavior.Stop)
        {
            // Remove animation and restore base value
            _activeAnimations.Remove(completedProperty);
            clock.Completed -= OnAnimationClockCompleted;
            ClearAnimatedValue(completedProperty);
        }
        // For HoldEnd, keep the animation record but mark as completed
        // The final value remains via the animated value layer

        StopAnimationTimerIfNeeded();
        InvalidateVisual();
    }

    private void StartAnimationTimerIfNeeded()
    {
        lock (_animationTimerLock)
        {
            if (_animationTimer == null && _activeAnimations?.Count > 0)
            {
                // ~60 FPS
                _animationTimer = new System.Threading.Timer(OnAnimationTick, null, 0, 16);
            }
        }
    }

    private void StopAnimationTimerIfNeeded()
    {
        lock (_animationTimerLock)
        {
            if (_activeAnimations == null || _activeAnimations.Count == 0 ||
                !_activeAnimations.Values.Any(a => a.Clock.IsRunning))
            {
                _animationTimer?.Dispose();
                _animationTimer = null;
            }
        }
    }

    private void OnAnimationTick(object? state)
    {
        // Must marshal to UI thread
        Dispatcher?.InvokeAsync(ProcessAnimationFrame);
    }

    private void ProcessAnimationFrame()
    {
        if (_activeAnimations == null || _activeAnimations.Count == 0)
            return;

        var hasRunningAnimation = false;

        foreach (var (dp, anim) in _activeAnimations.ToArray())
        {
            if (!anim.Clock.IsRunning)
                continue;

            hasRunningAnimation = true;

            // Update clock progress
            anim.Clock.Tick();

            // Update animated value
            UpdateAnimatedValue(dp);
        }

        if (hasRunningAnimation)
        {
            InvalidateVisual();
        }
        else
        {
            StopAnimationTimerIfNeeded();
        }
    }

    private void UpdateAnimatedValue(DependencyProperty dp)
    {
        if (_activeAnimations == null || !_activeAnimations.TryGetValue(dp, out var anim))
            return;

        try
        {
            var baseValue = anim.BaseValue ?? dp.DefaultMetadata.DefaultValue ?? GetDefaultAnimationValue(dp);
            var currentValue = anim.Animation.GetCurrentValue(baseValue, baseValue, anim.Clock);

            var holdEnd = anim.Animation.AnimationFillBehavior == AnimationFillBehavior.HoldEnd;
            SetAnimatedValue(dp, currentValue, holdEnd);
        }
        catch
        {
            // If animation value calculation fails, silently continue
        }
    }

    private static object GetDefaultAnimationValue(DependencyProperty dp)
    {
        var type = dp.PropertyType;

        if (type == typeof(double))
            return 0.0;
        if (type == typeof(float))
            return 0f;
        if (type == typeof(int))
            return 0;

        return type.IsValueType ? Activator.CreateInstance(type)! : null!;
    }

    #endregion

    #region Commands

    private Input.CommandBindingCollection? _commandBindings;
    private Input.InputBindingCollection? _inputBindings;

    /// <summary>
    /// Gets the collection of command bindings associated with this element.
    /// </summary>
    public Input.CommandBindingCollection CommandBindings => _commandBindings ??= new Input.CommandBindingCollection();

    /// <summary>
    /// Gets the collection of input bindings associated with this element.
    /// </summary>
    public Input.InputBindingCollection InputBindings => _inputBindings ??= new Input.InputBindingCollection();

    #endregion

    #region Automation

    private Automation.AutomationPeer? _automationPeer;

    /// <summary>
    /// Creates the automation peer for this element.
    /// </summary>
    /// <returns>The automation peer, or null if no peer should be created.</returns>
    protected virtual Automation.AutomationPeer? OnCreateAutomationPeer() => null;

    /// <summary>
    /// Gets or creates the automation peer for this element.
    /// </summary>
    /// <returns>The automation peer, or null if the element doesn't support automation.</returns>
    public Automation.AutomationPeer? GetAutomationPeer()
    {
        _automationPeer ??= OnCreateAutomationPeer();
        return _automationPeer;
    }

    /// <summary>
    /// Invalidates the automation peer, causing it to be recreated on next access.
    /// </summary>
    protected void InvalidateAutomationPeer()
    {
        _automationPeer = null;
    }

    #endregion
}

/// <summary>
/// Specifies the visibility of an element.
/// </summary>
public enum Visibility
{
    /// <summary>
    /// Display the element.
    /// </summary>
    Visible,

    /// <summary>
    /// Do not display the element, but reserve space for it in layout.
    /// </summary>
    Hidden,

    /// <summary>
    /// Do not display the element, and do not reserve space for it in layout.
    /// </summary>
    Collapsed
}

/// <summary>
/// Information about an instance-level event handler.
/// </summary>
internal sealed class RoutedEventHandlerInfo
{
    public Delegate Handler { get; }
    public bool HandledEventsToo { get; }

    public RoutedEventHandlerInfo(Delegate handler, bool handledEventsToo)
    {
        Handler = handler;
        HandledEventsToo = handledEventsToo;
    }
}

/// <summary>
/// Interface for elements that can host a window and handle invalidation.
/// </summary>
public interface IWindowHost
{
    /// <summary>
    /// Invalidates the window, causing it to repaint.
    /// </summary>
    void InvalidateWindow();
}
