namespace Jalium.UI.Automation;

/// <summary>
/// Specifies the control pattern interface that is supported by an AutomationPeer.
/// </summary>
public enum PatternInterface
{
    /// <summary>
    /// The Invoke pattern interface.
    /// </summary>
    Invoke,

    /// <summary>
    /// The Selection pattern interface.
    /// </summary>
    Selection,

    /// <summary>
    /// The Value pattern interface.
    /// </summary>
    Value,

    /// <summary>
    /// The RangeValue pattern interface.
    /// </summary>
    RangeValue,

    /// <summary>
    /// The Scroll pattern interface.
    /// </summary>
    Scroll,

    /// <summary>
    /// The ScrollItem pattern interface.
    /// </summary>
    ScrollItem,

    /// <summary>
    /// The ExpandCollapse pattern interface.
    /// </summary>
    ExpandCollapse,

    /// <summary>
    /// The Grid pattern interface.
    /// </summary>
    Grid,

    /// <summary>
    /// The GridItem pattern interface.
    /// </summary>
    GridItem,

    /// <summary>
    /// The MultipleView pattern interface.
    /// </summary>
    MultipleView,

    /// <summary>
    /// The Window pattern interface.
    /// </summary>
    Window,

    /// <summary>
    /// The SelectionItem pattern interface.
    /// </summary>
    SelectionItem,

    /// <summary>
    /// The Dock pattern interface.
    /// </summary>
    Dock,

    /// <summary>
    /// The Table pattern interface.
    /// </summary>
    Table,

    /// <summary>
    /// The TableItem pattern interface.
    /// </summary>
    TableItem,

    /// <summary>
    /// The Text pattern interface.
    /// </summary>
    Text,

    /// <summary>
    /// The Toggle pattern interface.
    /// </summary>
    Toggle,

    /// <summary>
    /// The Transform pattern interface.
    /// </summary>
    Transform,

    /// <summary>
    /// The ItemContainer pattern interface.
    /// </summary>
    ItemContainer,

    /// <summary>
    /// The VirtualizedItem pattern interface.
    /// </summary>
    VirtualizedItem,

    /// <summary>
    /// The SynchronizedInput pattern interface.
    /// </summary>
    SynchronizedInput
}

/// <summary>
/// Specifies the toggle state of a control.
/// </summary>
public enum ToggleState
{
    /// <summary>
    /// The control is not toggled on.
    /// </summary>
    Off,

    /// <summary>
    /// The control is toggled on.
    /// </summary>
    On,

    /// <summary>
    /// The control is in an indeterminate state.
    /// </summary>
    Indeterminate
}

/// <summary>
/// Specifies the expand/collapse state of a control.
/// </summary>
public enum ExpandCollapseState
{
    /// <summary>
    /// The control is collapsed.
    /// </summary>
    Collapsed,

    /// <summary>
    /// The control is expanded.
    /// </summary>
    Expanded,

    /// <summary>
    /// The control is partially expanded.
    /// </summary>
    PartiallyExpanded,

    /// <summary>
    /// No children are visible.
    /// </summary>
    LeafNode
}

/// <summary>
/// Specifies automation events.
/// </summary>
public enum AutomationEvents
{
    /// <summary>
    /// Tool tip opened event.
    /// </summary>
    ToolTipOpened,

    /// <summary>
    /// Tool tip closed event.
    /// </summary>
    ToolTipClosed,

    /// <summary>
    /// Menu opened event.
    /// </summary>
    MenuOpened,

    /// <summary>
    /// Menu closed event.
    /// </summary>
    MenuClosed,

    /// <summary>
    /// Automation focus changed event.
    /// </summary>
    AutomationFocusChanged,

    /// <summary>
    /// Invoke pattern invoked event.
    /// </summary>
    InvokePatternOnInvoked,

    /// <summary>
    /// Selection item pattern element added to selection event.
    /// </summary>
    SelectionItemPatternOnElementAddedToSelection,

    /// <summary>
    /// Selection item pattern element removed from selection event.
    /// </summary>
    SelectionItemPatternOnElementRemovedFromSelection,

    /// <summary>
    /// Selection item pattern element selected event.
    /// </summary>
    SelectionItemPatternOnElementSelected,

    /// <summary>
    /// Selection pattern invalidated event.
    /// </summary>
    SelectionPatternOnInvalidated,

    /// <summary>
    /// Text pattern text selection changed event.
    /// </summary>
    TextPatternOnTextSelectionChanged,

    /// <summary>
    /// Text pattern text changed event.
    /// </summary>
    TextPatternOnTextChanged,

    /// <summary>
    /// Async content loaded event.
    /// </summary>
    AsyncContentLoaded,

    /// <summary>
    /// Property changed event.
    /// </summary>
    PropertyChanged,

    /// <summary>
    /// Structure changed event.
    /// </summary>
    StructureChanged,

    /// <summary>
    /// Input reached target event.
    /// </summary>
    InputReachedTarget,

    /// <summary>
    /// Input reached other element event.
    /// </summary>
    InputReachedOtherElement,

    /// <summary>
    /// Input discarded event.
    /// </summary>
    InputDiscarded,

    /// <summary>
    /// Live region changed event.
    /// </summary>
    LiveRegionChanged
}
