namespace Jalium.UI;

/// <summary>
/// Provides helper methods for creating and managing visual states.
/// </summary>
public static class VisualStateHelper
{
    /// <summary>
    /// Creates a common states group with Normal, MouseOver, Pressed, and Disabled states.
    /// </summary>
    /// <param name="normalSetters">Setters for Normal state.</param>
    /// <param name="mouseOverSetters">Setters for MouseOver state.</param>
    /// <param name="pressedSetters">Setters for Pressed state.</param>
    /// <param name="disabledSetters">Setters for Disabled state.</param>
    /// <returns>A configured VisualStateGroup.</returns>
    public static VisualStateGroup CreateCommonStatesGroup(
        IEnumerable<Setter>? normalSetters = null,
        IEnumerable<Setter>? mouseOverSetters = null,
        IEnumerable<Setter>? pressedSetters = null,
        IEnumerable<Setter>? disabledSetters = null)
    {
        var group = new VisualStateGroup(VisualStateNames.CommonStatesGroup);

        var normalState = new VisualState(VisualStateNames.Normal);
        if (normalSetters != null)
        {
            foreach (var setter in normalSetters)
                normalState.Setters.Add(setter);
        }
        group.States.Add(normalState);

        var mouseOverState = new VisualState(VisualStateNames.MouseOver);
        if (mouseOverSetters != null)
        {
            foreach (var setter in mouseOverSetters)
                mouseOverState.Setters.Add(setter);
        }
        group.States.Add(mouseOverState);

        var pressedState = new VisualState(VisualStateNames.Pressed);
        if (pressedSetters != null)
        {
            foreach (var setter in pressedSetters)
                pressedState.Setters.Add(setter);
        }
        group.States.Add(pressedState);

        var disabledState = new VisualState(VisualStateNames.Disabled);
        if (disabledSetters != null)
        {
            foreach (var setter in disabledSetters)
                disabledState.Setters.Add(setter);
        }
        group.States.Add(disabledState);

        return group;
    }

    /// <summary>
    /// Creates a focus states group with Focused and Unfocused states.
    /// </summary>
    /// <param name="focusedSetters">Setters for Focused state.</param>
    /// <param name="unfocusedSetters">Setters for Unfocused state.</param>
    /// <returns>A configured VisualStateGroup.</returns>
    public static VisualStateGroup CreateFocusStatesGroup(
        IEnumerable<Setter>? focusedSetters = null,
        IEnumerable<Setter>? unfocusedSetters = null)
    {
        var group = new VisualStateGroup(VisualStateNames.FocusStatesGroup);

        var focusedState = new VisualState(VisualStateNames.Focused);
        if (focusedSetters != null)
        {
            foreach (var setter in focusedSetters)
                focusedState.Setters.Add(setter);
        }
        group.States.Add(focusedState);

        var unfocusedState = new VisualState(VisualStateNames.Unfocused);
        if (unfocusedSetters != null)
        {
            foreach (var setter in unfocusedSetters)
                unfocusedState.Setters.Add(setter);
        }
        group.States.Add(unfocusedState);

        return group;
    }

    /// <summary>
    /// Creates a check states group with Checked, Unchecked, and Indeterminate states.
    /// </summary>
    /// <param name="checkedSetters">Setters for Checked state.</param>
    /// <param name="uncheckedSetters">Setters for Unchecked state.</param>
    /// <param name="indeterminateSetters">Setters for Indeterminate state.</param>
    /// <returns>A configured VisualStateGroup.</returns>
    public static VisualStateGroup CreateCheckStatesGroup(
        IEnumerable<Setter>? checkedSetters = null,
        IEnumerable<Setter>? uncheckedSetters = null,
        IEnumerable<Setter>? indeterminateSetters = null)
    {
        var group = new VisualStateGroup(VisualStateNames.CheckStatesGroup);

        var checkedState = new VisualState(VisualStateNames.Checked);
        if (checkedSetters != null)
        {
            foreach (var setter in checkedSetters)
                checkedState.Setters.Add(setter);
        }
        group.States.Add(checkedState);

        var uncheckedState = new VisualState(VisualStateNames.Unchecked);
        if (uncheckedSetters != null)
        {
            foreach (var setter in uncheckedSetters)
                uncheckedState.Setters.Add(setter);
        }
        group.States.Add(uncheckedState);

        var indeterminateState = new VisualState(VisualStateNames.Indeterminate);
        if (indeterminateSetters != null)
        {
            foreach (var setter in indeterminateSetters)
                indeterminateState.Setters.Add(setter);
        }
        group.States.Add(indeterminateState);

        return group;
    }

    /// <summary>
    /// Initializes visual state groups on an element.
    /// </summary>
    /// <param name="element">The element to initialize.</param>
    /// <param name="groups">The visual state groups to add.</param>
    public static void InitializeVisualStates(FrameworkElement element, params VisualStateGroup[] groups)
    {
        var list = new List<VisualStateGroup>(groups);
        VisualStateManager.SetVisualStateGroups(element, list);
    }

    /// <summary>
    /// Updates the common visual state based on control properties.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <param name="isEnabled">Whether the control is enabled.</param>
    /// <param name="isMouseOver">Whether the mouse is over the control.</param>
    /// <param name="isPressed">Whether the control is pressed.</param>
    /// <param name="useTransitions">Whether to use transitions.</param>
    public static void UpdateCommonState(
        FrameworkElement control,
        bool isEnabled,
        bool isMouseOver,
        bool isPressed,
        bool useTransitions = true)
    {
        string stateName;
        if (!isEnabled)
        {
            stateName = VisualStateNames.Disabled;
        }
        else if (isPressed)
        {
            stateName = VisualStateNames.Pressed;
        }
        else if (isMouseOver)
        {
            stateName = VisualStateNames.MouseOver;
        }
        else
        {
            stateName = VisualStateNames.Normal;
        }

        VisualStateManager.GoToState(control, stateName, useTransitions);
    }

    /// <summary>
    /// Updates the focus visual state.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <param name="isFocused">Whether the control is focused.</param>
    /// <param name="useTransitions">Whether to use transitions.</param>
    public static void UpdateFocusState(
        FrameworkElement control,
        bool isFocused,
        bool useTransitions = true)
    {
        var stateName = isFocused ? VisualStateNames.Focused : VisualStateNames.Unfocused;
        VisualStateManager.GoToState(control, stateName, useTransitions);
    }

    /// <summary>
    /// Updates the check visual state.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <param name="isChecked">The checked state (true, false, or null for indeterminate).</param>
    /// <param name="useTransitions">Whether to use transitions.</param>
    public static void UpdateCheckState(
        FrameworkElement control,
        bool? isChecked,
        bool useTransitions = true)
    {
        string stateName;
        if (isChecked == true)
        {
            stateName = VisualStateNames.Checked;
        }
        else if (isChecked == false)
        {
            stateName = VisualStateNames.Unchecked;
        }
        else
        {
            stateName = VisualStateNames.Indeterminate;
        }

        VisualStateManager.GoToState(control, stateName, useTransitions);
    }
}
