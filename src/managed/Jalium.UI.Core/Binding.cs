using System.ComponentModel;
using System.Reflection;

namespace Jalium.UI;

/// <summary>
/// Specifies the direction of data flow in a binding.
/// </summary>
public enum BindingMode
{
    /// <summary>
    /// Binding mode is automatically chosen based on the target property.
    /// </summary>
    Default,

    /// <summary>
    /// Updates the target when the source changes.
    /// </summary>
    OneWay,

    /// <summary>
    /// Updates both target and source when either changes.
    /// </summary>
    TwoWay,

    /// <summary>
    /// Updates the target only once when the binding is created.
    /// </summary>
    OneTime,

    /// <summary>
    /// Updates the source when the target changes.
    /// </summary>
    OneWayToSource
}

/// <summary>
/// Specifies when the binding source is updated.
/// </summary>
public enum UpdateSourceTrigger
{
    /// <summary>
    /// Default update trigger for the property.
    /// </summary>
    Default,

    /// <summary>
    /// Updates the source whenever the target property value changes.
    /// </summary>
    PropertyChanged,

    /// <summary>
    /// Updates the source when the target element loses focus.
    /// </summary>
    LostFocus,

    /// <summary>
    /// Updates the source only when you call UpdateSource explicitly.
    /// </summary>
    Explicit
}

/// <summary>
/// Provides an abstract base class for value conversion.
/// </summary>
public interface IValueConverter
{
    /// <summary>
    /// Converts a value from the source to the target type.
    /// </summary>
    /// <param name="value">The value produced by the binding source.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>A converted value.</returns>
    object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture);

    /// <summary>
    /// Converts a value from the target back to the source type.
    /// </summary>
    /// <param name="value">The value that is produced by the binding target.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>A converted value.</returns>
    object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture);
}

/// <summary>
/// Provides a way to apply custom logic to a multi-binding.
/// </summary>
public interface IMultiValueConverter
{
    /// <summary>
    /// Converts source values to a value for the binding target.
    /// </summary>
    /// <param name="values">The array of values that the source bindings in the MultiBinding produces.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>A converted value.</returns>
    object? Convert(object?[] values, Type targetType, object? parameter, System.Globalization.CultureInfo culture);

    /// <summary>
    /// Converts a binding target value to the source binding values.
    /// </summary>
    /// <param name="value">The value that the binding target produces.</param>
    /// <param name="targetTypes">The array of types to convert to.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>An array of values that have been converted from the target value back to the source values.</returns>
    object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, System.Globalization.CultureInfo culture);
}

/// <summary>
/// Abstract base class for binding classes.
/// </summary>
public abstract class BindingBase
{
    /// <summary>
    /// Gets or sets the value to use when the binding cannot return a value.
    /// </summary>
    public object? FallbackValue { get; set; }

    /// <summary>
    /// Gets or sets the value to use when the source value is null.
    /// </summary>
    public object? TargetNullValue { get; set; }

    /// <summary>
    /// Gets or sets a string that specifies how to format the binding if it displays the bound value as a string.
    /// </summary>
    public string? StringFormat { get; set; }

    /// <summary>
    /// Gets or sets the delay (in milliseconds) before updating the source.
    /// </summary>
    public int Delay { get; set; }

    /// <summary>
    /// Creates a new binding expression for this binding.
    /// </summary>
    internal abstract BindingExpressionBase CreateBindingExpression(DependencyObject target, DependencyProperty targetProperty);
}

/// <summary>
/// Describes the binding between a binding target and a binding source.
/// </summary>
public class Binding : BindingBase
{
    /// <summary>
    /// Gets or sets the path to the binding source property.
    /// </summary>
    public PropertyPath? Path { get; set; }

    /// <summary>
    /// Gets or sets the source object to use for the binding.
    /// </summary>
    public object? Source { get; set; }

    /// <summary>
    /// Gets or sets the binding source by specifying its location relative to the position of the binding target.
    /// </summary>
    public RelativeSource? RelativeSource { get; set; }

    /// <summary>
    /// Gets or sets the name of the element to use as the binding source.
    /// </summary>
    public string? ElementName { get; set; }

    /// <summary>
    /// Gets or sets the binding mode.
    /// </summary>
    public BindingMode Mode { get; set; } = BindingMode.Default;

    /// <summary>
    /// Gets or sets the trigger that determines when the source is updated.
    /// </summary>
    public UpdateSourceTrigger UpdateSourceTrigger { get; set; } = UpdateSourceTrigger.Default;

    /// <summary>
    /// Gets or sets the converter to use.
    /// </summary>
    public IValueConverter? Converter { get; set; }

    /// <summary>
    /// Gets or sets the parameter to pass to the converter.
    /// </summary>
    public object? ConverterParameter { get; set; }

    /// <summary>
    /// Gets or sets the culture to use in the converter.
    /// </summary>
    public System.Globalization.CultureInfo? ConverterCulture { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether to raise PropertyChanged events.
    /// </summary>
    public bool NotifyOnSourceUpdated { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether to raise PropertyChanged events.
    /// </summary>
    public bool NotifyOnTargetUpdated { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Binding"/> class.
    /// </summary>
    public Binding()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Binding"/> class with the specified path.
    /// </summary>
    /// <param name="path">The property path string.</param>
    public Binding(string path)
    {
        Path = new PropertyPath(path);
    }

    /// <inheritdoc />
    internal override BindingExpressionBase CreateBindingExpression(DependencyObject target, DependencyProperty targetProperty)
    {
        return new BindingExpression(this, target, targetProperty);
    }
}

/// <summary>
/// Represents a property path for data binding.
/// </summary>
public class PropertyPath
{
    /// <summary>
    /// Gets the path string.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the path segments.
    /// </summary>
    public string[] PathSegments { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyPath"/> class.
    /// </summary>
    /// <param name="path">The path string (e.g., "PropertyName" or "Object.Property").</param>
    public PropertyPath(string path)
    {
        Path = path ?? string.Empty;
        PathSegments = string.IsNullOrEmpty(path) ? [] : path.Split('.');
    }

    /// <inheritdoc />
    public override string ToString() => Path;
}

/// <summary>
/// Describes the location of a binding source relative to the position of the binding target.
/// </summary>
public class RelativeSource
{
    /// <summary>
    /// Gets the relative source mode.
    /// </summary>
    public RelativeSourceMode Mode { get; }

    /// <summary>
    /// Gets the type of ancestor to look for.
    /// </summary>
    public Type? AncestorType { get; set; }

    /// <summary>
    /// Gets the level of ancestor to look for.
    /// </summary>
    public int AncestorLevel { get; set; } = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelativeSource"/> class.
    /// </summary>
    public RelativeSource(RelativeSourceMode mode)
    {
        Mode = mode;
    }

    /// <summary>
    /// Gets a static RelativeSource for Self mode.
    /// </summary>
    public static RelativeSource Self { get; } = new(RelativeSourceMode.Self);

    /// <summary>
    /// Gets a static RelativeSource for TemplatedParent mode.
    /// </summary>
    public static RelativeSource TemplatedParent { get; } = new(RelativeSourceMode.TemplatedParent);

    /// <summary>
    /// Gets a static RelativeSource for PreviousData mode.
    /// </summary>
    public static RelativeSource PreviousData { get; } = new(RelativeSourceMode.PreviousData);
}

/// <summary>
/// Specifies the relative source mode.
/// </summary>
public enum RelativeSourceMode
{
    /// <summary>
    /// Refers to the previous data item in the data-bound collection.
    /// </summary>
    PreviousData,

    /// <summary>
    /// Refers to the parent element in the control template.
    /// </summary>
    TemplatedParent,

    /// <summary>
    /// Refers to the element on which you set the binding.
    /// </summary>
    Self,

    /// <summary>
    /// Refers to an ancestor in the parent chain.
    /// </summary>
    FindAncestor
}

/// <summary>
/// Base class for binding expressions.
/// </summary>
public abstract class BindingExpressionBase
{
    /// <summary>
    /// Gets the target element.
    /// </summary>
    public DependencyObject Target { get; }

    /// <summary>
    /// Gets the target property.
    /// </summary>
    public DependencyProperty TargetProperty { get; }

    /// <summary>
    /// Gets a value indicating whether the binding is active.
    /// </summary>
    public bool IsActive { get; protected set; }

    /// <summary>
    /// Gets the binding status.
    /// </summary>
    public BindingStatus Status { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BindingExpressionBase"/> class.
    /// </summary>
    protected BindingExpressionBase(DependencyObject target, DependencyProperty targetProperty)
    {
        Target = target;
        TargetProperty = targetProperty;
    }

    /// <summary>
    /// Activates the binding.
    /// </summary>
    internal abstract void Activate();

    /// <summary>
    /// Deactivates the binding.
    /// </summary>
    internal abstract void Deactivate();

    /// <summary>
    /// Updates the source value.
    /// </summary>
    public abstract void UpdateSource();

    /// <summary>
    /// Updates the target value.
    /// </summary>
    public abstract void UpdateTarget();
}

/// <summary>
/// Represents the runtime instance of a binding.
/// </summary>
public class BindingExpression : BindingExpressionBase
{
    private readonly Binding _binding;
    private INotifyPropertyChanged? _sourceNotify;
    private PropertyInfo? _sourceProperty;
    private bool _isUpdating;

    /// <summary>
    /// Gets the parent binding.
    /// </summary>
    public Binding ParentBinding => _binding;

    /// <summary>
    /// Gets the resolved data source.
    /// </summary>
    public object? ResolvedSource { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BindingExpression"/> class.
    /// </summary>
    internal BindingExpression(Binding binding, DependencyObject target, DependencyProperty targetProperty)
        : base(target, targetProperty)
    {
        _binding = binding;
    }

    /// <inheritdoc />
    internal override void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        Status = BindingStatus.Active;

        // Resolve the data source
        ResolveDataSource();

        // Subscribe to source changes
        SubscribeToSource();

        // Initial update
        UpdateTarget();
    }

    /// <inheritdoc />
    internal override void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        Status = BindingStatus.Inactive;

        // Unsubscribe from source changes
        UnsubscribeFromSource();
    }

    /// <inheritdoc />
    public override void UpdateSource()
    {
        if (!IsActive || _isUpdating)
            return;

        var mode = GetEffectiveMode();
        if (mode != BindingMode.TwoWay && mode != BindingMode.OneWayToSource)
            return;

        if (ResolvedSource == null || _sourceProperty == null)
            return;

        try
        {
            _isUpdating = true;

            var targetValue = Target.GetValue(TargetProperty);
            var sourceValue = ConvertBack(targetValue);

            _sourceProperty.SetValue(ResolvedSource, sourceValue);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    /// <inheritdoc />
    public override void UpdateTarget()
    {
        if (!IsActive || _isUpdating)
            return;

        try
        {
            _isUpdating = true;

            var sourceValue = GetSourceValue();
            var targetValue = Convert(sourceValue);

            Target.SetValue(TargetProperty, targetValue);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void ResolveDataSource()
    {
        // Priority: explicit Source > ElementName > RelativeSource > DataContext
        if (_binding.Source != null)
        {
            ResolvedSource = _binding.Source;
        }
        else if (!string.IsNullOrEmpty(_binding.ElementName))
        {
            // Resolve element by name - walk up the visual tree looking for named element
            ResolvedSource = FindElementByName(Target, _binding.ElementName);
        }
        else if (_binding.RelativeSource != null)
        {
            ResolvedSource = ResolveRelativeSource();
        }
        else
        {
            // Use DataContext
            ResolvedSource = GetDataContext();
        }

        // Resolve property from path
        if (ResolvedSource != null && _binding.Path != null)
        {
            ResolveSourceProperty();
        }
    }

    private static object? FindElementByName(DependencyObject target, string elementName)
    {
        // First, try to find the element in the current scope
        if (target is FrameworkElement fe)
        {
            // Use FindName which walks up the tree looking in each element's named scope
            var found = fe.FindName(elementName);
            if (found != null)
            {
                return found;
            }

            // If not found in named scopes, try walking up the visual tree
            // looking for an element with matching Name property
            return FindElementInVisualTree(fe, elementName);
        }
        return null;
    }

    private static object? FindElementInVisualTree(FrameworkElement start, string elementName)
    {
        // Walk up to find the root or template root
        FrameworkElement? root = start;
        while (root.VisualParent is FrameworkElement parent)
        {
            root = parent;
        }

        // Search down the tree for the named element
        return SearchVisualTreeForName(root, elementName);
    }

    private static FrameworkElement? SearchVisualTreeForName(Visual? visual, string elementName)
    {
        if (visual == null) return null;

        // Check if this element has the name we're looking for
        if (visual is FrameworkElement fe && fe.Name == elementName)
        {
            return fe;
        }

        // Search children
        var childCount = visual.VisualChildrenCount;
        for (int i = 0; i < childCount; i++)
        {
            var child = visual.GetVisualChild(i);
            var found = SearchVisualTreeForName(child, elementName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private object? ResolveRelativeSource()
    {
        var relativeSource = _binding.RelativeSource;
        if (relativeSource == null)
            return null;

        switch (relativeSource.Mode)
        {
            case RelativeSourceMode.Self:
                return Target;

            case RelativeSourceMode.TemplatedParent:
                // Return the control that owns the template containing this element
                if (Target is FrameworkElement fe)
                {
                    return fe.TemplatedParent;
                }
                return null;

            case RelativeSourceMode.FindAncestor:
                return FindAncestor(relativeSource.AncestorType, relativeSource.AncestorLevel);

            default:
                return null;
        }
    }

    private object? FindAncestor(Type? ancestorType, int level)
    {
        if (ancestorType == null || Target is not Visual visual)
            return null;

        var current = visual.VisualParent;
        var count = 0;

        while (current != null)
        {
            if (ancestorType.IsAssignableFrom(current.GetType()))
            {
                count++;
                if (count >= level)
                    return current;
            }
            current = current.VisualParent;
        }

        return null;
    }

    private object? GetDataContext()
    {
        // Walk up the visual tree to find DataContext
        if (Target is FrameworkElement fe)
        {
            return fe.DataContext;
        }
        return null;
    }

    private void ResolveSourceProperty()
    {
        if (ResolvedSource == null || _binding.Path == null)
            return;

        var segments = _binding.Path.PathSegments;
        if (segments.Length == 0)
            return;

        // For now, only support simple property paths (single segment)
        var propertyName = segments[0];
        _sourceProperty = ResolvedSource.GetType().GetProperty(propertyName);
    }

    private object? GetSourceValue()
    {
        if (ResolvedSource == null)
            return _binding.FallbackValue;

        if (_binding.Path == null || _binding.Path.PathSegments.Length == 0)
            return ResolvedSource;

        // Navigate the path
        object? current = ResolvedSource;
        foreach (var segment in _binding.Path.PathSegments)
        {
            if (current == null)
                return _binding.FallbackValue;

            var property = current.GetType().GetProperty(segment);
            if (property == null)
                return _binding.FallbackValue;

            current = property.GetValue(current);
        }

        return current ?? _binding.TargetNullValue;
    }

    private object? Convert(object? value)
    {
        if (_binding.Converter != null)
        {
            value = _binding.Converter.Convert(
                value,
                TargetProperty.PropertyType,
                _binding.ConverterParameter,
                _binding.ConverterCulture ?? System.Globalization.CultureInfo.CurrentCulture);
        }

        if (value != null && !string.IsNullOrEmpty(_binding.StringFormat))
        {
            value = string.Format(_binding.StringFormat, value);
        }

        return value;
    }

    private object? ConvertBack(object? value)
    {
        if (_binding.Converter != null)
        {
            value = _binding.Converter.ConvertBack(
                value,
                _sourceProperty?.PropertyType ?? typeof(object),
                _binding.ConverterParameter,
                _binding.ConverterCulture ?? System.Globalization.CultureInfo.CurrentCulture);
        }

        return value;
    }

    private void SubscribeToSource()
    {
        if (ResolvedSource is INotifyPropertyChanged notify)
        {
            _sourceNotify = notify;
            _sourceNotify.PropertyChanged += OnSourcePropertyChanged;
        }

        // Also subscribe to DataContext changes
        if (Target is FrameworkElement fe)
        {
            fe.DataContextChanged += OnDataContextChanged;
        }
    }

    private void UnsubscribeFromSource()
    {
        if (_sourceNotify != null)
        {
            _sourceNotify.PropertyChanged -= OnSourcePropertyChanged;
            _sourceNotify = null;
        }

        if (Target is FrameworkElement fe)
        {
            fe.DataContextChanged -= OnDataContextChanged;
        }
    }

    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_binding.Path == null)
            return;

        // Check if the changed property is in our path
        if (string.IsNullOrEmpty(e.PropertyName) ||
            _binding.Path.PathSegments.Length > 0 && _binding.Path.PathSegments[0] == e.PropertyName)
        {
            UpdateTarget();
        }
    }

    private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        // Re-resolve the data source when DataContext changes
        UnsubscribeFromSource();
        ResolveDataSource();
        SubscribeToSource();
        UpdateTarget();
    }

    private BindingMode GetEffectiveMode()
    {
        if (_binding.Mode != BindingMode.Default)
            return _binding.Mode;

        // Default mode based on property metadata would go here
        // For now, default to OneWay
        return BindingMode.OneWay;
    }
}

/// <summary>
/// Specifies the status of a binding.
/// </summary>
public enum BindingStatus
{
    /// <summary>
    /// The binding has not been activated yet.
    /// </summary>
    Unattached,

    /// <summary>
    /// The binding is inactive.
    /// </summary>
    Inactive,

    /// <summary>
    /// The binding is active and working.
    /// </summary>
    Active,

    /// <summary>
    /// The binding is detached.
    /// </summary>
    Detached,

    /// <summary>
    /// The binding encountered an error while resolving.
    /// </summary>
    PathError,

    /// <summary>
    /// The binding cannot update because of source validation errors.
    /// </summary>
    UpdateTargetError,

    /// <summary>
    /// The binding cannot update because of target validation errors.
    /// </summary>
    UpdateSourceError
}
