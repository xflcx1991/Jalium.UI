using Jalium.UI.Media;

namespace Jalium.UI.Controls;

/// <summary>
/// Displays the content of a ContentControl.
/// </summary>
public class ContentPresenter : FrameworkElement
{
    #region Dependency Properties

    /// <summary>
    /// Identifies the Content dependency property.
    /// </summary>
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(nameof(Content), typeof(object), typeof(ContentPresenter),
            new PropertyMetadata(null, OnContentChanged));

    /// <summary>
    /// Identifies the ContentTemplate dependency property.
    /// </summary>
    public static readonly DependencyProperty ContentTemplateProperty =
        DependencyProperty.Register(nameof(ContentTemplate), typeof(DataTemplate), typeof(ContentPresenter),
            new PropertyMetadata(null, OnContentTemplateChanged));

    /// <summary>
    /// Identifies the ContentSource dependency property.
    /// </summary>
    public static readonly DependencyProperty ContentSourceProperty =
        DependencyProperty.Register(nameof(ContentSource), typeof(string), typeof(ContentPresenter),
            new PropertyMetadata("Content"));

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the content to display.
    /// </summary>
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>
    /// Gets or sets the template used to display the content.
    /// </summary>
    public DataTemplate? ContentTemplate
    {
        get => (DataTemplate?)GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the name of the property on the templated parent to use as content.
    /// </summary>
    public string ContentSource
    {
        get => (string)(GetValue(ContentSourceProperty) ?? "Content");
        set => SetValue(ContentSourceProperty, value);
    }

    #endregion

    #region Fields

    private FrameworkElement? _contentElement;
    private bool _templateBindingsApplied;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentPresenter"/> class.
    /// </summary>
    public ContentPresenter()
    {
    }

    #endregion

    #region Content Changed

    private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ContentPresenter presenter)
        {
            presenter.OnContentChanged(e.OldValue, e.NewValue);
        }
    }

    private void OnContentChanged(object? oldContent, object? newContent)
    {
        // Remove old content element
        if (_contentElement != null)
        {
            RemoveVisualChild(_contentElement);
            _contentElement = null;
        }

        // Add new content
        if (newContent != null)
        {
            _contentElement = CreateContentElement(newContent);
            if (_contentElement != null)
            {
                AddVisualChild(_contentElement);
            }
        }

        InvalidateMeasure();
    }

    private static void OnContentTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ContentPresenter presenter)
        {
            // Re-create content with new template
            presenter.OnContentChanged(presenter.Content, presenter.Content);
        }
    }

    private FrameworkElement? CreateContentElement(object content)
    {
        // If content is already a UIElement, use it directly
        if (content is FrameworkElement fe)
        {
            return fe;
        }

        // If we have a template, use it
        if (ContentTemplate != null)
        {
            var templateContent = ContentTemplate.LoadContent();
            if (templateContent != null)
            {
                templateContent.DataContext = content;
                return templateContent;
            }
        }

        // Default: create a TextBlock for string content
        if (content is string text)
        {
            return new TextBlock { Text = text };
        }

        // For other objects, use ToString()
        return new TextBlock { Text = content.ToString() ?? string.Empty };
    }

    #endregion

    #region Template Binding

    /// <inheritdoc />
    protected override void OnTemplatedParentChanged(FrameworkElement? oldParent, FrameworkElement? newParent)
    {
        base.OnTemplatedParentChanged(oldParent, newParent);

        // When TemplatedParent is set, apply template bindings
        if (!_templateBindingsApplied && newParent != null)
        {
            ApplyTemplateBindings();
        }
    }

    private void ApplyTemplateBindings()
    {
        if (TemplatedParent == null)
            return;

        _templateBindingsApplied = true;

        // Get the content source property name
        var contentSource = ContentSource;
        if (string.IsNullOrEmpty(contentSource))
            return;

        // Find the property on the templated parent
        var parentType = TemplatedParent.GetType();

        // Try to find Content property and bind it
        // Only apply if no explicit binding or local value is set
        if (!HasLocalValue(ContentProperty) && GetBindingExpression(ContentProperty) == null)
        {
            var contentPropInfo = parentType.GetProperty(contentSource);
            if (contentPropInfo != null)
            {
                // Get the DependencyProperty
                var dpField = parentType.GetField($"{contentSource}Property",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
                if (dpField?.GetValue(null) is DependencyProperty contentDp)
                {
                    this.SetTemplateBinding(ContentProperty, contentDp);
                }
            }
        }

        // Try to find ContentTemplate property
        // Only apply if no explicit binding or local value is set
        if (!HasLocalValue(ContentTemplateProperty) && GetBindingExpression(ContentTemplateProperty) == null)
        {
            var templatePropInfo = parentType.GetProperty($"{contentSource}Template");
            if (templatePropInfo != null)
            {
                var dpField = parentType.GetField($"{contentSource}TemplateProperty",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
                if (dpField?.GetValue(null) is DependencyProperty templateDp)
                {
                    this.SetTemplateBinding(ContentTemplateProperty, templateDp);
                }
            }
        }

        // Bind HorizontalContentAlignment -> HorizontalAlignment
        // Only apply if no explicit binding or local value is set
        if (!HasLocalValue(HorizontalAlignmentProperty) && GetBindingExpression(HorizontalAlignmentProperty) == null)
        {
            var hcaDpField = parentType.GetField("HorizontalContentAlignmentProperty",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
            if (hcaDpField?.GetValue(null) is DependencyProperty hcaDp)
            {
                this.SetTemplateBinding(HorizontalAlignmentProperty, hcaDp);
            }
        }

        // Bind VerticalContentAlignment -> VerticalAlignment
        // Only apply if no explicit binding or local value is set
        if (!HasLocalValue(VerticalAlignmentProperty) && GetBindingExpression(VerticalAlignmentProperty) == null)
        {
            var vcaDpField = parentType.GetField("VerticalContentAlignmentProperty",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
            if (vcaDpField?.GetValue(null) is DependencyProperty vcaDp)
            {
                this.SetTemplateBinding(VerticalAlignmentProperty, vcaDp);
            }
        }
    }

    #endregion

    #region Layout

    /// <inheritdoc />
    public override int VisualChildrenCount => _contentElement != null ? 1 : 0;

    /// <inheritdoc />
    public override Visual? GetVisualChild(int index)
    {
        if (index != 0 || _contentElement == null)
            throw new ArgumentOutOfRangeException(nameof(index));

        return _contentElement;
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        if (_contentElement == null)
            return Size.Empty;

        _contentElement.Measure(availableSize);
        return _contentElement.DesiredSize;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_contentElement != null)
        {
            _contentElement.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            // Note: Do NOT call SetVisualBounds here - ArrangeCore already handles margin
        }

        return finalSize;
    }

    #endregion
}
