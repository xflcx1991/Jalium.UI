namespace Jalium.UI;

/// <summary>
/// Contains property setters that can be shared between instances of a type.
/// </summary>
[ContentProperty("Setters")]
public class Style
{
    private readonly List<Setter> _setters = new();
    private readonly List<Trigger> _triggers = new();
    private bool _isSealed;

    /// <summary>
    /// Gets or sets the type for which this style is intended.
    /// </summary>
    public Type? TargetType { get; set; }

    /// <summary>
    /// Gets or sets a style that is the basis of the current style.
    /// </summary>
    public Style? BasedOn { get; set; }

    /// <summary>
    /// Gets the collection of property setters.
    /// </summary>
    public IList<Setter> Setters => _setters;

    /// <summary>
    /// Gets the collection of triggers.
    /// </summary>
    public IList<Trigger> Triggers => _triggers;

    /// <summary>
    /// Gets a value that indicates whether the style is read-only.
    /// </summary>
    public bool IsSealed => _isSealed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Style"/> class.
    /// </summary>
    public Style()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Style"/> class with the specified target type.
    /// </summary>
    /// <param name="targetType">The type for which this style is intended.</param>
    public Style(Type targetType)
    {
        TargetType = targetType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Style"/> class with the specified target type and base style.
    /// </summary>
    /// <param name="targetType">The type for which this style is intended.</param>
    /// <param name="basedOn">A style that is the basis of the current style.</param>
    public Style(Type targetType, Style? basedOn)
    {
        TargetType = targetType;
        BasedOn = basedOn;
    }

    /// <summary>
    /// Seals the style so that it can no longer be modified.
    /// </summary>
    public void Seal()
    {
        _isSealed = true;
    }

    /// <summary>
    /// Applies this style to the specified framework element.
    /// </summary>
    /// <param name="element">The element to apply the style to.</param>
    internal void Apply(FrameworkElement element)
    {
        // Apply base style first
        BasedOn?.Apply(element);

        // Apply setters
        foreach (var setter in _setters)
        {
            setter.Apply(element);
        }

        // Apply triggers
        foreach (var trigger in _triggers)
        {
            trigger.ParentStyle = this;
            trigger.Attach(element);
        }
    }

    /// <summary>
    /// Removes this style from the specified framework element.
    /// </summary>
    /// <param name="element">The element to remove the style from.</param>
    internal void Remove(FrameworkElement element)
    {
        // Remove triggers
        foreach (var trigger in _triggers)
        {
            trigger.Detach(element);
        }

        // Remove setters (in reverse order)
        for (int i = _setters.Count - 1; i >= 0; i--)
        {
            _setters[i].Remove(element);
        }

        // Remove base style
        BasedOn?.Remove(element);
    }
}

/// <summary>
/// Represents a setter that sets a property value.
/// </summary>
[ContentProperty("Value")]
public class Setter
{
    /// <summary>
    /// Gets or sets the property to set.
    /// </summary>
    public DependencyProperty? Property { get; set; }

    /// <summary>
    /// Gets or sets the value to set.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the name of the element to apply the setter to.
    /// </summary>
    public string? TargetName { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Setter"/> class.
    /// </summary>
    public Setter()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Setter"/> class with the specified property and value.
    /// </summary>
    /// <param name="property">The property to set.</param>
    /// <param name="value">The value to set.</param>
    public Setter(DependencyProperty property, object? value)
    {
        Property = property;
        Value = value;
    }

    /// <summary>
    /// Applies this setter to the specified element.
    /// </summary>
    internal void Apply(FrameworkElement element)
    {
        if (Property == null)
            return;

        var target = GetTarget(element);
        if (target != null)
        {
            // Store original value for restoration
            if (!target._styleOriginalValues.ContainsKey(Property))
            {
                target._styleOriginalValues[Property] = target.GetValue(Property);
            }

            // Convert value to the correct type if needed
            var valueToSet = ConvertValueIfNeeded(Value, Property.PropertyType);
            target.SetValue(Property, valueToSet);
        }
    }

    /// <summary>
    /// Converts a value to the target type if needed.
    /// </summary>
    private static object? ConvertValueIfNeeded(object? value, Type targetType)
    {
        if (value == null) return null;
        if (targetType.IsInstanceOfType(value)) return value;

        // String to target type conversion
        if (value is string stringValue)
        {
            if (targetType == typeof(double))
                return double.Parse(stringValue, System.Globalization.CultureInfo.InvariantCulture);
            if (targetType == typeof(float))
                return float.Parse(stringValue, System.Globalization.CultureInfo.InvariantCulture);
            if (targetType == typeof(int))
                return int.Parse(stringValue, System.Globalization.CultureInfo.InvariantCulture);
            if (targetType == typeof(bool))
                return bool.Parse(stringValue);
            if (targetType.IsEnum)
                return Enum.Parse(targetType, stringValue, ignoreCase: true);
        }

        return value;
    }

    /// <summary>
    /// Removes this setter from the specified element.
    /// </summary>
    internal void Remove(FrameworkElement element)
    {
        if (Property == null) return;

        var target = GetTarget(element);
        if (target != null && target._styleOriginalValues.TryGetValue(Property, out var originalValue))
        {
            target.SetValue(Property, originalValue);
            target._styleOriginalValues.Remove(Property);
        }
    }

    private FrameworkElement? GetTarget(FrameworkElement element)
    {
        if (string.IsNullOrEmpty(TargetName))
            return element;

        // Look up named element in the template scope
        // First, try using the element's FindName method
        var found = element.FindName(TargetName) as FrameworkElement;
        if (found != null)
        {
            return found;
        }

        // If that fails, search the visual tree starting from the element
        return SearchVisualTreeForName(element, TargetName);
    }

    /// <summary>
    /// Recursively searches the visual tree for an element with the specified name.
    /// </summary>
    private static FrameworkElement? SearchVisualTreeForName(Visual? visual, string name)
    {
        if (visual == null) return null;

        // Check if this element has the name we're looking for
        if (visual is FrameworkElement fe && fe.Name == name)
        {
            return fe;
        }

        // Search children
        var childCount = visual.VisualChildrenCount;
        for (int i = 0; i < childCount; i++)
        {
            var child = visual.GetVisualChild(i);
            var result = SearchVisualTreeForName(child, name);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}

/// <summary>
/// Base class for triggers that conditionally apply property values.
/// </summary>
[ContentProperty("Setters")]
public abstract class Trigger
{
    /// <summary>
    /// Gets the collection of setters to apply when the trigger is active.
    /// </summary>
    public IList<Setter> Setters { get; } = new List<Setter>();

    /// <summary>
    /// Tracks which properties this trigger has set for each element.
    /// Used to properly decrement the active count in shared storage.
    /// </summary>
    private readonly HashSet<(FrameworkElement, DependencyProperty)> _activeSetters = new();

    /// <summary>
    /// Gets or sets the parent style that contains this trigger.
    /// </summary>
    internal Style? ParentStyle { get; set; }

    /// <summary>
    /// Attaches this trigger to the specified element.
    /// </summary>
    internal abstract void Attach(FrameworkElement element);

    /// <summary>
    /// Detaches this trigger from the specified element.
    /// </summary>
    internal abstract void Detach(FrameworkElement element);

    /// <summary>
    /// Gets whether this trigger is currently active for the specified element.
    /// </summary>
    internal abstract bool IsActiveForElement(FrameworkElement element);

    /// <summary>
    /// Applies the trigger's setters, storing pre-trigger values for later restoration.
    /// Uses element-level shared storage to ensure original values are preserved correctly
    /// even when multiple triggers affect the same property.
    /// </summary>
    protected void ApplyTriggerSetters(FrameworkElement element)
    {
        foreach (var setter in Setters)
        {
            if (setter.Property == null)
                continue;

            // Get the target element (may be different from element if TargetName is set)
            var target = GetSetterTarget(element, setter.TargetName);
            if (target == null)
                continue;

            var key = (target, setter.Property);

            // Use the styled element's shared storage for original values
            // This ensures we store the value BEFORE any trigger modified it
            if (element._triggerOriginalValues.TryGetValue(key, out var stored))
            {
                // Another trigger already stored the original value, just increment count
                element._triggerOriginalValues[key] = (stored.OriginalValue, stored.ActiveCount + 1);
            }
            else
            {
                // This is the first trigger affecting this property, store the original value
                var originalValue = target.GetValue(setter.Property);
                element._triggerOriginalValues[key] = (originalValue, 1);
            }

            // Track that this trigger has set this property
            _activeSetters.Add(key);

            // Convert value to the correct type if needed and apply
            var valueToSet = ConvertValueIfNeeded(setter.Value, setter.Property.PropertyType);
            target.SetValue(setter.Property, valueToSet);
        }

        // Invalidate visual to ensure re-render
        element.InvalidateVisual();
    }

    /// <summary>
    /// Converts a value to the target type if needed.
    /// </summary>
    protected static object? ConvertValueIfNeeded(object? value, Type targetType)
    {
        if (value == null) return null;
        if (targetType.IsInstanceOfType(value)) return value;

        // Handle nullable types - get the underlying type
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        var actualType = underlyingType ?? targetType;

        // String to target type conversion
        if (value is string stringValue)
        {
            if (actualType == typeof(double))
                return double.Parse(stringValue, System.Globalization.CultureInfo.InvariantCulture);
            if (actualType == typeof(float))
                return float.Parse(stringValue, System.Globalization.CultureInfo.InvariantCulture);
            if (actualType == typeof(int))
                return int.Parse(stringValue, System.Globalization.CultureInfo.InvariantCulture);
            if (actualType == typeof(bool))
                return bool.Parse(stringValue);
            if (actualType.IsEnum)
                return Enum.Parse(actualType, stringValue, ignoreCase: true);
        }

        return value;
    }

    /// <summary>
    /// Removes the trigger's setters, restoring pre-trigger values.
    /// Uses element-level shared storage to ensure correct restoration
    /// when multiple triggers affect the same property.
    /// </summary>
    protected void RemoveTriggerSetters(FrameworkElement element)
    {
        // Collect the properties that need other triggers re-applied
        var needsReapply = new HashSet<(FrameworkElement, DependencyProperty)>();

        foreach (var setter in Setters)
        {
            if (setter.Property == null)
                continue;

            // Get the target element (may be different from element if TargetName is set)
            var target = GetSetterTarget(element, setter.TargetName);
            if (target == null)
                continue;

            var key = (target, setter.Property);

            // Check if this trigger actually set this property
            if (!_activeSetters.Contains(key))
                continue;

            _activeSetters.Remove(key);

            // Decrement the active count in shared storage
            if (element._triggerOriginalValues.TryGetValue(key, out var stored))
            {
                var newCount = stored.ActiveCount - 1;
                if (newCount <= 0)
                {
                    // No more triggers affecting this property, restore original value
                    target.SetValue(setter.Property, stored.OriginalValue);
                    element._triggerOriginalValues.Remove(key);
                }
                else
                {
                    // Other triggers still affect this property
                    element._triggerOriginalValues[key] = (stored.OriginalValue, newCount);
                    needsReapply.Add(key);
                }
            }
        }

        // Re-apply any other still-active triggers that affect the same properties
        // This ensures that if trigger A deactivates but trigger B is still active,
        // trigger B's values are re-applied
        if (ParentStyle != null && needsReapply.Count > 0)
        {
            foreach (var otherTrigger in ParentStyle.Triggers)
            {
                if (otherTrigger == this) continue;
                if (!otherTrigger.IsActiveForElement(element)) continue;

                foreach (var setter in otherTrigger.Setters)
                {
                    if (setter.Property == null) continue;

                    var target = GetSetterTarget(element, setter.TargetName);
                    if (target == null) continue;

                    var key = (target, setter.Property);
                    if (needsReapply.Contains(key))
                    {
                        // This property needs another trigger's value re-applied
                        var valueToSet = ConvertValueIfNeeded(setter.Value, setter.Property.PropertyType);
                        target.SetValue(setter.Property, valueToSet);
                    }
                }
            }
        }

        // Invalidate visual to ensure re-render
        element.InvalidateVisual();
    }

    /// <summary>
    /// Gets the target element for a setter, handling TargetName lookup.
    /// </summary>
    private static FrameworkElement? GetSetterTarget(FrameworkElement element, string? targetName)
    {
        if (string.IsNullOrEmpty(targetName))
            return element;

        // Look up named element in the template scope
        var found = element.FindName(targetName) as FrameworkElement;
        if (found != null)
            return found;

        // If that fails, search the visual tree starting from the element
        return SearchVisualTreeForName(element, targetName);
    }

    /// <summary>
    /// Recursively searches the visual tree for an element with the specified name.
    /// </summary>
    private static FrameworkElement? SearchVisualTreeForName(Visual? visual, string name)
    {
        if (visual == null) return null;

        // Check if this element has the name we're looking for
        if (visual is FrameworkElement fe && fe.Name == name)
            return fe;

        // Search children
        var childCount = visual.VisualChildrenCount;
        for (int i = 0; i < childCount; i++)
        {
            var child = visual.GetVisualChild(i);
            var result = SearchVisualTreeForName(child, name);
            if (result != null)
                return result;
        }

        return null;
    }

    /// <summary>
    /// Clears all stored pre-trigger values for an element.
    /// </summary>
    protected void ClearPreTriggerValues(FrameworkElement element)
    {
        // Clear this trigger's tracking
        var keysToRemove = _activeSetters.Where(k => k.Item1 == element).ToList();
        foreach (var key in keysToRemove)
        {
            _activeSetters.Remove(key);

            // Decrement count in shared storage
            if (element._triggerOriginalValues.TryGetValue(key, out var stored))
            {
                var newCount = stored.ActiveCount - 1;
                if (newCount <= 0)
                {
                    element._triggerOriginalValues.Remove(key);
                }
                else
                {
                    element._triggerOriginalValues[key] = (stored.OriginalValue, newCount);
                }
            }
        }
    }
}

/// <summary>
/// Represents a trigger that applies property values when a property value equals a specified value.
/// </summary>
public class PropertyTrigger : Trigger
{
    /// <summary>
    /// Tracks per-element state since a single trigger can be attached to multiple elements (shared styles).
    /// </summary>
    private class ElementState
    {
        public bool IsActive;
        public Action<DependencyProperty, object?, object?>? Handler;
    }

    private readonly Dictionary<FrameworkElement, ElementState> _elementStates = new();

    /// <summary>
    /// Gets or sets the property that activates the trigger.
    /// </summary>
    public DependencyProperty? Property { get; set; }

    /// <summary>
    /// Gets or sets the value that activates the trigger.
    /// </summary>
    public object? Value { get; set; }

    /// <inheritdoc />
    internal override void Attach(FrameworkElement element)
    {
        // Create per-element state
        var state = new ElementState();
        _elementStates[element] = state;

        // Create a closure that captures this specific element
        state.Handler = (dp, oldValue, newValue) =>
        {
            if (dp == Property)
            {
                EvaluateTriggerForElement(element, newValue);
            }
        };

        // Subscribe to property changes
        element.PropertyChangedInternal += state.Handler;

        // Check initial state
        if (Property != null)
        {
            var currentValue = element.GetValue(Property);
            EvaluateTriggerForElement(element, currentValue);
        }
    }

    /// <inheritdoc />
    internal override void Detach(FrameworkElement element)
    {
        if (_elementStates.TryGetValue(element, out var state))
        {
            // Unsubscribe from property changes
            if (state.Handler != null)
            {
                element.PropertyChangedInternal -= state.Handler;
            }

            if (state.IsActive)
            {
                RemoveTriggerSetters(element);
            }

            _elementStates.Remove(element);
        }

        // Clear any stored pre-trigger values
        ClearPreTriggerValues(element);
    }

    /// <inheritdoc />
    internal override bool IsActiveForElement(FrameworkElement element)
    {
        return _elementStates.TryGetValue(element, out var state) && state.IsActive;
    }

    private void EvaluateTriggerForElement(FrameworkElement element, object? currentValue)
    {
        if (Property == null) return;
        if (!_elementStates.TryGetValue(element, out var state)) return;

        // Convert Value to the property's type for proper comparison
        var triggerValue = ConvertValueIfNeeded(Value, Property.PropertyType);
        var shouldBeActive = Equals(currentValue, triggerValue);

        if (shouldBeActive != state.IsActive)
        {
            state.IsActive = shouldBeActive;

            if (state.IsActive)
            {
                ApplyTriggerSetters(element);
            }
            else
            {
                RemoveTriggerSetters(element);
            }
        }
    }
}

/// <summary>
/// Represents a condition for a MultiTrigger.
/// </summary>
public class Condition
{
    /// <summary>
    /// Gets or sets the property to evaluate.
    /// </summary>
    public DependencyProperty? Property { get; set; }

    /// <summary>
    /// Gets or sets the value that the property must equal for the condition to be true.
    /// </summary>
    public object? Value { get; set; }
}

/// <summary>
/// Represents a condition based on a data binding for a MultiDataTrigger.
/// </summary>
public class BindingCondition
{
    /// <summary>
    /// Gets or sets the binding to evaluate.
    /// </summary>
    public Binding? Binding { get; set; }

    /// <summary>
    /// Gets or sets the value that the binding must equal for the condition to be true.
    /// </summary>
    public object? Value { get; set; }
}

/// <summary>
/// Represents a trigger that applies property values when multiple conditions are all true.
/// </summary>
[ContentProperty("Setters")]
public class MultiTrigger : Trigger
{
    /// <summary>
    /// Tracks per-element state since a single trigger can be attached to multiple elements (shared styles).
    /// </summary>
    private class ElementState
    {
        public bool IsActive;
        public Action<DependencyProperty, object?, object?>? Handler;
    }

    private readonly Dictionary<FrameworkElement, ElementState> _elementStates = new();

    /// <summary>
    /// Gets the collection of conditions that must all be true for the trigger to activate.
    /// </summary>
    public IList<Condition> Conditions { get; } = new List<Condition>();

    /// <inheritdoc />
    internal override void Attach(FrameworkElement element)
    {
        // Create per-element state
        var state = new ElementState();
        _elementStates[element] = state;

        // Create a closure that captures this specific element
        state.Handler = (dp, oldValue, newValue) =>
        {
            // Check if this property is one of our conditions
            bool shouldEvaluate = false;
            foreach (var condition in Conditions)
            {
                // Compare by property reference or by name as fallback
                if (dp == condition.Property ||
                    (condition.Property != null && dp.Name == condition.Property.Name))
                {
                    shouldEvaluate = true;
                    break;
                }
            }

            if (shouldEvaluate)
            {
                EvaluateTriggerForElement(element);
            }
        };

        // Subscribe to property changes
        element.PropertyChangedInternal += state.Handler;

        // Check initial state
        EvaluateTriggerForElement(element);
    }

    /// <inheritdoc />
    internal override void Detach(FrameworkElement element)
    {
        if (_elementStates.TryGetValue(element, out var state))
        {
            // Unsubscribe from property changes
            if (state.Handler != null)
            {
                element.PropertyChangedInternal -= state.Handler;
            }

            if (state.IsActive)
            {
                RemoveTriggerSetters(element);
            }

            _elementStates.Remove(element);
        }

        // Clear any stored pre-trigger values
        ClearPreTriggerValues(element);
    }

    /// <inheritdoc />
    internal override bool IsActiveForElement(FrameworkElement element)
    {
        return _elementStates.TryGetValue(element, out var state) && state.IsActive;
    }

    private void EvaluateTriggerForElement(FrameworkElement element)
    {
        if (!_elementStates.TryGetValue(element, out var state)) return;

        // All conditions must be true
        var shouldBeActive = true;

        foreach (var condition in Conditions)
        {
            if (condition.Property == null)
            {
                shouldBeActive = false;
                break;
            }

            var currentValue = element.GetValue(condition.Property);
            var conditionValue = ConvertValueIfNeeded(condition.Value, condition.Property.PropertyType);

            if (!Equals(currentValue, conditionValue))
            {
                shouldBeActive = false;
                break;
            }
        }

        if (shouldBeActive != state.IsActive)
        {
            state.IsActive = shouldBeActive;

            if (state.IsActive)
            {
                ApplyTriggerSetters(element);
            }
            else
            {
                RemoveTriggerSetters(element);
            }
        }
    }
}

/// <summary>
/// Represents a trigger that applies property values when data equals a specified value.
/// </summary>
public class DataTrigger : Trigger
{
    /// <summary>
    /// Tracks per-element state since a single trigger can be attached to multiple elements (shared styles).
    /// </summary>
    private class ElementState
    {
        public bool IsActive;
        public Action<DependencyProperty, object?, object?>? Handler;
        public BindingExpressionBase? BindingExpression;
    }

    private readonly Dictionary<FrameworkElement, ElementState> _elementStates = new();

    // Shadow property for receiving binding updates
    private static readonly DependencyProperty TriggerValueProperty =
        DependencyProperty.RegisterAttached("_DataTriggerValue", typeof(object),
            typeof(DataTrigger), new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the binding that produces the value to compare with Value.
    /// </summary>
    public Binding? Binding { get; set; }

    /// <summary>
    /// Gets or sets the value that activates the trigger.
    /// </summary>
    public object? Value { get; set; }

    /// <inheritdoc />
    internal override void Attach(FrameworkElement element)
    {
        // Create per-element state
        var state = new ElementState();
        _elementStates[element] = state;

        // Create a closure that captures this specific element
        state.Handler = (dp, oldValue, newValue) =>
        {
            if (dp == TriggerValueProperty)
            {
                EvaluateTriggerForElement(element, newValue);
            }
        };

        // Subscribe to property changes for the shadow property
        element.PropertyChangedInternal += state.Handler;

        // Set up binding to the shadow property
        if (Binding != null)
        {
            state.BindingExpression = BindingOperations.SetBinding(element, TriggerValueProperty, Binding);
        }

        // Check initial state
        var currentValue = element.GetValue(TriggerValueProperty);
        EvaluateTriggerForElement(element, currentValue);
    }

    /// <inheritdoc />
    internal override void Detach(FrameworkElement element)
    {
        if (_elementStates.TryGetValue(element, out var state))
        {
            // Unsubscribe from property changes
            if (state.Handler != null)
            {
                element.PropertyChangedInternal -= state.Handler;
            }

            // Clear binding
            if (state.BindingExpression != null)
            {
                BindingOperations.ClearBinding(element, TriggerValueProperty);
            }

            if (state.IsActive)
            {
                RemoveTriggerSetters(element);
            }

            _elementStates.Remove(element);
        }

        // Clear any stored pre-trigger values
        ClearPreTriggerValues(element);
    }

    /// <inheritdoc />
    internal override bool IsActiveForElement(FrameworkElement element)
    {
        return _elementStates.TryGetValue(element, out var state) && state.IsActive;
    }

    private void EvaluateTriggerForElement(FrameworkElement element, object? currentValue)
    {
        if (!_elementStates.TryGetValue(element, out var state)) return;

        var shouldBeActive = Equals(currentValue, Value);

        if (shouldBeActive != state.IsActive)
        {
            state.IsActive = shouldBeActive;

            if (state.IsActive)
            {
                ApplyTriggerSetters(element);
            }
            else
            {
                RemoveTriggerSetters(element);
            }
        }
    }
}

/// <summary>
/// Represents a trigger that applies property values when an event occurs.
/// </summary>
public class EventTrigger : Trigger
{
    private FrameworkElement? _attachedElement;

    /// <summary>
    /// Gets or sets the name of the event that activates the trigger.
    /// </summary>
    public RoutedEvent? RoutedEvent { get; set; }

    /// <summary>
    /// Gets the collection of actions to perform when the event occurs.
    /// </summary>
    public IList<TriggerAction> Actions { get; } = new List<TriggerAction>();

    /// <inheritdoc />
    internal override void Attach(FrameworkElement element)
    {
        _attachedElement = element;

        if (RoutedEvent != null)
        {
            element.AddHandler(RoutedEvent, new RoutedEventHandler(OnEventRaised));
        }
    }

    /// <inheritdoc />
    internal override void Detach(FrameworkElement element)
    {
        if (RoutedEvent != null)
        {
            element.RemoveHandler(RoutedEvent, new RoutedEventHandler(OnEventRaised));
        }

        _attachedElement = null;
    }

    /// <inheritdoc />
    internal override bool IsActiveForElement(FrameworkElement element)
    {
        // EventTriggers don't have a persistent active state - they fire on events
        return false;
    }

    private void OnEventRaised(object sender, RoutedEventArgs e)
    {
        foreach (var action in Actions)
        {
            action.Invoke(_attachedElement);
        }
    }
}

/// <summary>
/// Represents a trigger that applies property values when multiple data binding conditions are all true.
/// </summary>
[ContentProperty("Setters")]
public class MultiDataTrigger : Trigger
{
    /// <summary>
    /// Tracks per-element state since a single trigger can be attached to multiple elements (shared styles).
    /// </summary>
    private class ElementState
    {
        public bool IsActive;
        public Action<DependencyProperty, object?, object?>? Handler;
        public List<BindingExpressionBase> BindingExpressions = new();
        public List<DependencyProperty> ShadowProperties = new();
    }

    private readonly Dictionary<FrameworkElement, ElementState> _elementStates = new();

    // Counter for generating unique shadow property names
    private static int _propertyCounter;

    /// <summary>
    /// Gets the collection of binding conditions that must all be true for the trigger to activate.
    /// </summary>
    public IList<BindingCondition> Conditions { get; } = new List<BindingCondition>();

    /// <inheritdoc />
    internal override void Attach(FrameworkElement element)
    {
        // Create per-element state
        var state = new ElementState();
        _elementStates[element] = state;

        // Create shadow properties for each condition
        foreach (var condition in Conditions)
        {
            var propId = Interlocked.Increment(ref _propertyCounter);
            var shadowProp = DependencyProperty.RegisterAttached(
                $"_MultiDataTriggerValue{propId}",
                typeof(object),
                typeof(MultiDataTrigger),
                new PropertyMetadata(null));
            state.ShadowProperties.Add(shadowProp);
        }

        // Create a closure that captures this specific element
        state.Handler = (dp, oldValue, newValue) =>
        {
            // Check if this property is one of our shadow properties
            if (state.ShadowProperties.Contains(dp))
            {
                EvaluateTriggerForElement(element);
            }
        };

        // Subscribe to property changes
        element.PropertyChangedInternal += state.Handler;

        // Set up bindings to shadow properties
        for (int i = 0; i < Conditions.Count; i++)
        {
            var condition = Conditions[i];
            if (condition.Binding != null)
            {
                var bindingExpr = BindingOperations.SetBinding(element, state.ShadowProperties[i], condition.Binding);
                state.BindingExpressions.Add(bindingExpr);
            }
        }

        // Check initial state
        EvaluateTriggerForElement(element);
    }

    /// <inheritdoc />
    internal override void Detach(FrameworkElement element)
    {
        if (_elementStates.TryGetValue(element, out var state))
        {
            // Unsubscribe from property changes
            if (state.Handler != null)
            {
                element.PropertyChangedInternal -= state.Handler;
            }

            // Clear all bindings
            foreach (var shadowProp in state.ShadowProperties)
            {
                BindingOperations.ClearBinding(element, shadowProp);
            }

            if (state.IsActive)
            {
                RemoveTriggerSetters(element);
            }

            _elementStates.Remove(element);
        }

        // Clear any stored pre-trigger values
        ClearPreTriggerValues(element);
    }

    /// <inheritdoc />
    internal override bool IsActiveForElement(FrameworkElement element)
    {
        return _elementStates.TryGetValue(element, out var state) && state.IsActive;
    }

    private void EvaluateTriggerForElement(FrameworkElement element)
    {
        if (!_elementStates.TryGetValue(element, out var state)) return;

        // All conditions must be true
        var shouldBeActive = true;

        for (int i = 0; i < Conditions.Count; i++)
        {
            var condition = Conditions[i];
            if (i >= state.ShadowProperties.Count)
            {
                shouldBeActive = false;
                break;
            }

            var currentValue = element.GetValue(state.ShadowProperties[i]);
            if (!Equals(currentValue, condition.Value))
            {
                shouldBeActive = false;
                break;
            }
        }

        if (shouldBeActive != state.IsActive)
        {
            state.IsActive = shouldBeActive;

            if (state.IsActive)
            {
                ApplyTriggerSetters(element);
            }
            else
            {
                RemoveTriggerSetters(element);
            }
        }
    }
}

/// <summary>
/// Base class for actions that can be invoked by event triggers.
/// </summary>
public abstract class TriggerAction
{
    /// <summary>
    /// Invokes the action on the specified element.
    /// </summary>
    internal abstract void Invoke(FrameworkElement? element);
}
