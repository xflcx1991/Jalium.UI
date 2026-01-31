namespace Jalium.UI.Controls;

/// <summary>
/// Base class for content controls.
/// Uses direct content management by default. Controls with ControlTemplate (like Button)
/// rely on the template's ContentPresenter to display content instead.
/// </summary>
public class ContentControl : Control
{
    private UIElement? _contentElement;
    private bool _usesDirectContent = true; // Default to direct content management

    /// <summary>
    /// Identifies the Content dependency property.
    /// </summary>
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(nameof(Content), typeof(object), typeof(ContentControl),
            new PropertyMetadata(null, OnContentChanged));

    /// <summary>
    /// Identifies the ContentTemplate dependency property.
    /// </summary>
    public static readonly DependencyProperty ContentTemplateProperty =
        DependencyProperty.Register(nameof(ContentTemplate), typeof(DataTemplate), typeof(ContentControl),
            new PropertyMetadata(null, OnContentTemplateChanged));

    /// <summary>
    /// Gets or sets the content of this control.
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
    /// Gets the content element for direct content management.
    /// </summary>
    protected UIElement? ContentElement => _contentElement;

    /// <summary>
    /// Disables direct content management. Call this in the constructor of controls
    /// that use ControlTemplate with ContentPresenter (e.g., Button).
    /// </summary>
    protected void UseTemplateContentManagement()
    {
        _usesDirectContent = false;
    }

    /// <summary>
    /// Gets whether this control uses direct content management.
    /// Returns true for most controls; false for controls using ControlTemplate.
    /// </summary>
    protected bool UsesDirectContent => _usesDirectContent;

    private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ContentControl control)
        {
            control.OnContentChanged(e.OldValue, e.NewValue);
        }
    }

    private static void OnContentTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ContentControl control)
        {
            // Re-apply content with new template
            control.OnContentChanged(null, control.Content);
        }
    }

    /// <summary>
    /// Called when the Content property changes.
    /// </summary>
    protected virtual void OnContentChanged(object? oldContent, object? newContent)
    {
        if (_usesDirectContent)
        {
            // Direct content management
            if (_contentElement != null)
            {
                RemoveVisualChild(_contentElement);
                _contentElement = null;
            }

            if (newContent is UIElement newElement)
            {
                _contentElement = newElement;
                AddVisualChild(newElement);
            }
        }

        InvalidateMeasure();
    }

    #region Visual Children

    /// <inheritdoc />
    public override int VisualChildrenCount
    {
        get
        {
            if (_usesDirectContent)
            {
                return _contentElement != null ? 1 : 0;
            }
            // Template-based: use Control's implementation
            return base.VisualChildrenCount;
        }
    }

    /// <inheritdoc />
    public override Visual? GetVisualChild(int index)
    {
        if (_usesDirectContent)
        {
            if (index == 0 && _contentElement != null)
            {
                return _contentElement;
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        // Template-based: use Control's implementation
        return base.GetVisualChild(index);
    }

    #endregion

    #region Layout

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        if (_usesDirectContent)
        {
            if (_contentElement != null)
            {
                var padding = Padding;
                var border = BorderThickness;
                var contentAvailable = new Size(
                    Math.Max(0, availableSize.Width - padding.TotalWidth - border.TotalWidth),
                    Math.Max(0, availableSize.Height - padding.TotalHeight - border.TotalHeight));

                _contentElement.Measure(contentAvailable);

                return new Size(
                    _contentElement.DesiredSize.Width + padding.TotalWidth + border.TotalWidth,
                    _contentElement.DesiredSize.Height + padding.TotalHeight + border.TotalHeight);
            }
            return Size.Empty;
        }
        // Template-based: use Control's implementation
        return base.MeasureOverride(availableSize);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_usesDirectContent)
        {
            if (_contentElement is FrameworkElement fe)
            {
                var padding = Padding;
                var border = BorderThickness;

                var contentRect = new Rect(
                    padding.Left + border.Left,
                    padding.Top + border.Top,
                    Math.Max(0, finalSize.Width - padding.TotalWidth - border.TotalWidth),
                    Math.Max(0, finalSize.Height - padding.TotalHeight - border.TotalHeight));

                fe.Arrange(contentRect);
            }
            return finalSize;
        }
        // Template-based: use Control's implementation
        return base.ArrangeOverride(finalSize);
    }

    #endregion
}
