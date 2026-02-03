using Jalium.UI.Automation;
using Jalium.UI.Controls.Automation;

namespace Jalium.UI.Controls;

/// <summary>
/// Represents a button that can be selected, but not cleared, by a user.
/// RadioButtons in the same group are mutually exclusive.
/// </summary>
public class RadioButton : ToggleButton
{
    #region Automation

    /// <inheritdoc />
    protected override AutomationPeer? OnCreateAutomationPeer()
    {
        return new RadioButtonAutomationPeer(this);
    }

    #endregion

    #region Static Fields

    /// <summary>
    /// Tracks RadioButtons by group name for mutual exclusion.
    /// </summary>
    private static readonly Dictionary<string, List<WeakReference<RadioButton>>> _groupMap = new();

    #endregion

    #region Dependency Properties

    /// <summary>
    /// Identifies the GroupName dependency property.
    /// </summary>
    public static readonly DependencyProperty GroupNameProperty =
        DependencyProperty.Register(nameof(GroupName), typeof(string), typeof(RadioButton),
            new PropertyMetadata(string.Empty, OnGroupNameChanged));

    #endregion

    #region CLR Properties

    /// <summary>
    /// Gets or sets the name of the group that the RadioButton belongs to.
    /// RadioButtons in the same group are mutually exclusive.
    /// </summary>
    public string GroupName
    {
        get => (string)(GetValue(GroupNameProperty) ?? string.Empty);
        set => SetValue(GroupNameProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RadioButton"/> class.
    /// </summary>
    public RadioButton()
    {
        // RadioButton uses ControlTemplate for visual appearance (inherited from ButtonBase)
        RegisterInGroup(GroupName);
    }

    #endregion

    #region Group Management

    private void RegisterInGroup(string groupName)
    {
        var effectiveGroup = string.IsNullOrEmpty(groupName) ? GetDefaultGroupName() : groupName;

        if (!_groupMap.TryGetValue(effectiveGroup, out var list))
        {
            list = new List<WeakReference<RadioButton>>();
            _groupMap[effectiveGroup] = list;
        }

        // Clean up dead references and add ourselves
        list.RemoveAll(wr => !wr.TryGetTarget(out _));
        list.Add(new WeakReference<RadioButton>(this));
    }

    private void UnregisterFromGroup(string groupName)
    {
        var effectiveGroup = string.IsNullOrEmpty(groupName) ? GetDefaultGroupName() : groupName;

        if (_groupMap.TryGetValue(effectiveGroup, out var list))
        {
            list.RemoveAll(wr => !wr.TryGetTarget(out var target) || target == this);
            if (list.Count == 0)
            {
                _groupMap.Remove(effectiveGroup);
            }
        }
    }

    private string GetDefaultGroupName()
    {
        // Use parent as default group scope
        return VisualParent?.GetHashCode().ToString() ?? "__default__";
    }

    private void UncheckOthersInGroup()
    {
        var effectiveGroup = string.IsNullOrEmpty(GroupName) ? GetDefaultGroupName() : GroupName;

        if (_groupMap.TryGetValue(effectiveGroup, out var list))
        {
            foreach (var wr in list)
            {
                if (wr.TryGetTarget(out var radioButton) && radioButton != this && radioButton.IsChecked == true)
                {
                    radioButton.IsChecked = false;
                }
            }
        }
    }

    #endregion

    #region Toggle Handling

    /// <inheritdoc />
    protected override void OnToggle()
    {
        // RadioButton can only be checked, not unchecked by clicking
        if (IsChecked != true)
        {
            IsChecked = true;
        }
    }

    /// <inheritdoc />
    protected override void OnIsCheckedChanged(bool? oldValue, bool? newValue)
    {
        if (newValue == true)
        {
            // Uncheck other RadioButtons in the same group
            UncheckOthersInGroup();
        }

        base.OnIsCheckedChanged(oldValue, newValue);
    }

    #endregion

    #region Property Changed Callbacks

    private static void OnGroupNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RadioButton radioButton)
        {
            radioButton.UnregisterFromGroup((string?)e.OldValue ?? string.Empty);
            radioButton.RegisterInGroup((string?)e.NewValue ?? string.Empty);
        }
    }

    #endregion
}
