using Jalium.UI;

namespace Jalium.UI.Input;

/// <summary>
/// Provides data for keyboard events.
/// </summary>
public class KeyEventArgs : InputEventArgs
{
    /// <summary>
    /// Gets the key that was pressed or released.
    /// </summary>
    public Key Key { get; }

    /// <summary>
    /// Gets the system key if this is a system key event.
    /// </summary>
    public Key SystemKey { get; }

    /// <summary>
    /// Gets the modifier keys that were pressed during the event.
    /// </summary>
    public ModifierKeys KeyboardModifiers { get; }

    /// <summary>
    /// Gets a value indicating whether this is a repeated key event.
    /// </summary>
    public bool IsRepeat { get; }

    /// <summary>
    /// Gets a value indicating whether the key is currently down.
    /// </summary>
    public bool IsDown { get; }

    /// <summary>
    /// Gets a value indicating whether the key is currently up.
    /// </summary>
    public bool IsUp => !IsDown;

    /// <summary>
    /// Gets a value indicating whether the Alt key was pressed.
    /// </summary>
    public bool IsAltDown => (KeyboardModifiers & ModifierKeys.Alt) != 0;

    /// <summary>
    /// Gets a value indicating whether the Control key was pressed.
    /// </summary>
    public bool IsControlDown => (KeyboardModifiers & ModifierKeys.Control) != 0;

    /// <summary>
    /// Gets a value indicating whether the Shift key was pressed.
    /// </summary>
    public bool IsShiftDown => (KeyboardModifiers & ModifierKeys.Shift) != 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyEventArgs"/> class.
    /// </summary>
    /// <param name="routedEvent">The routed event.</param>
    /// <param name="key">The key.</param>
    /// <param name="modifiers">The modifier keys.</param>
    /// <param name="isDown">Whether the key is down.</param>
    /// <param name="isRepeat">Whether this is a repeat event.</param>
    /// <param name="timestamp">The event timestamp.</param>
    public KeyEventArgs(RoutedEvent routedEvent, Key key, ModifierKeys modifiers, bool isDown, bool isRepeat, int timestamp)
        : base(routedEvent, timestamp)
    {
        Key = key;
        KeyboardModifiers = modifiers;
        IsDown = isDown;
        IsRepeat = isRepeat;
    }

    /// <inheritdoc />
    protected internal override void InvokeEventHandler(Delegate handler, object target)
    {
        if (handler is KeyEventHandler keyHandler)
        {
            keyHandler(target, this);
        }
        else
        {
            base.InvokeEventHandler(handler, target);
        }
    }
}

/// <summary>
/// Delegate for handling keyboard events.
/// </summary>
/// <param name="sender">The sender of the event.</param>
/// <param name="e">The event arguments.</param>
public delegate void KeyEventHandler(object sender, KeyEventArgs e);

/// <summary>
/// Provides data for text input events.
/// </summary>
public class TextCompositionEventArgs : InputEventArgs
{
    /// <summary>
    /// Gets the text that was input.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextCompositionEventArgs"/> class.
    /// </summary>
    /// <param name="routedEvent">The routed event.</param>
    /// <param name="text">The input text.</param>
    /// <param name="timestamp">The event timestamp.</param>
    public TextCompositionEventArgs(RoutedEvent routedEvent, string text, int timestamp)
        : base(routedEvent, timestamp)
    {
        Text = text;
    }

    /// <inheritdoc />
    protected internal override void InvokeEventHandler(Delegate handler, object target)
    {
        if (handler is TextCompositionEventHandler textHandler)
        {
            textHandler(target, this);
        }
        else
        {
            base.InvokeEventHandler(handler, target);
        }
    }
}

/// <summary>
/// Delegate for handling text composition events.
/// </summary>
/// <param name="sender">The sender of the event.</param>
/// <param name="e">The event arguments.</param>
public delegate void TextCompositionEventHandler(object sender, TextCompositionEventArgs e);
