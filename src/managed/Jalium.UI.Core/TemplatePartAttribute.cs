namespace Jalium.UI;

/// <summary>
/// Represents an attribute that identifies the types of named parts used in a control's template.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class TemplatePartAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the part.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the named part.
    /// </summary>
    public Type? Type { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplatePartAttribute"/> class.
    /// </summary>
    public TemplatePartAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplatePartAttribute"/> class
    /// with the specified name and type.
    /// </summary>
    /// <param name="name">The name of the part.</param>
    /// <param name="type">The type of the named part.</param>
    public TemplatePartAttribute(string name, Type type)
    {
        Name = name;
        Type = type;
    }
}

/// <summary>
/// Represents an attribute that identifies the visual states used in a control.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class TemplateVisualStateAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the visual state.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the state group.
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateVisualStateAttribute"/> class.
    /// </summary>
    public TemplateVisualStateAttribute()
    {
    }
}
