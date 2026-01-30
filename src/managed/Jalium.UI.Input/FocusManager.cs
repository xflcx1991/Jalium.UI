namespace Jalium.UI.Input;

/// <summary>
/// Provides static methods and attached properties for determining and setting focus scopes
/// and for setting the focused element within the scope.
/// </summary>
public static class FocusManager
{
    #region Attached Properties

    /// <summary>
    /// Identifies the IsFocusScope attached property.
    /// </summary>
    public static readonly DependencyProperty IsFocusScopeProperty =
        DependencyProperty.RegisterAttached("IsFocusScope", typeof(bool), typeof(FocusManager),
            new PropertyMetadata(false));

    /// <summary>
    /// Identifies the FocusedElement attached property.
    /// </summary>
    public static readonly DependencyProperty FocusedElementProperty =
        DependencyProperty.RegisterAttached("FocusedElement", typeof(IInputElement), typeof(FocusManager),
            new PropertyMetadata(null, OnFocusedElementChanged));

    /// <summary>
    /// Gets the value of the IsFocusScope attached property.
    /// </summary>
    public static bool GetIsFocusScope(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (bool)(element.GetValue(IsFocusScopeProperty) ?? false);
    }

    /// <summary>
    /// Sets the value of the IsFocusScope attached property.
    /// </summary>
    public static void SetIsFocusScope(DependencyObject element, bool value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(IsFocusScopeProperty, value);
    }

    /// <summary>
    /// Gets the element with logical focus within the specified focus scope.
    /// </summary>
    public static IInputElement? GetFocusedElement(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(FocusedElementProperty) as IInputElement;
    }

    /// <summary>
    /// Sets logical focus on the specified element within its focus scope.
    /// </summary>
    public static void SetFocusedElement(DependencyObject element, IInputElement? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(FocusedElementProperty, value);
    }

    #endregion

    #region Routed Events (aliases to FocusService events)

    /// <summary>
    /// Identifies the GotFocus routed event.
    /// </summary>
    public static readonly RoutedEvent GotFocusEvent = FocusService.GotFocusEvent;

    /// <summary>
    /// Identifies the LostFocus routed event.
    /// </summary>
    public static readonly RoutedEvent LostFocusEvent = FocusService.LostFocusEvent;

    #endregion

    #region Methods

    /// <summary>
    /// Determines the closest ancestor that is a focus scope for the specified element.
    /// </summary>
    public static DependencyObject? GetFocusScope(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);

        // Walk up the visual tree to find a focus scope
        DependencyObject? current = element;
        while (current != null)
        {
            if (GetIsFocusScope(current))
            {
                return current;
            }

            current = (current as Visual)?.VisualParent;
        }

        return null;
    }

    private static void OnFocusedElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var oldElement = e.OldValue as IInputElement;
        var newElement = e.NewValue as IInputElement;

        // Raise LostFocus on old element
        if (oldElement is UIElement oldUIElement)
        {
            oldUIElement.RaiseEvent(new RoutedEventArgs(LostFocusEvent, oldUIElement));
        }

        // Raise GotFocus on new element
        if (newElement is UIElement newUIElement)
        {
            newUIElement.RaiseEvent(new RoutedEventArgs(GotFocusEvent, newUIElement));
        }
    }

    #endregion
}
