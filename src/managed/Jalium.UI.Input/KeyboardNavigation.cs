namespace Jalium.UI.Input;

/// <summary>
/// Provides logical and directional navigation between focusable elements.
/// </summary>
public static class KeyboardNavigation
{
    #region Attached Properties

    /// <summary>
    /// Identifies the TabIndex attached property.
    /// </summary>
    public static readonly DependencyProperty TabIndexProperty =
        DependencyProperty.RegisterAttached("TabIndex", typeof(int), typeof(KeyboardNavigation),
            new PropertyMetadata(int.MaxValue));

    /// <summary>
    /// Identifies the IsTabStop attached property.
    /// </summary>
    public static readonly DependencyProperty IsTabStopProperty =
        DependencyProperty.RegisterAttached("IsTabStop", typeof(bool), typeof(KeyboardNavigation),
            new PropertyMetadata(true));

    /// <summary>
    /// Identifies the TabNavigation attached property.
    /// </summary>
    public static readonly DependencyProperty TabNavigationProperty =
        DependencyProperty.RegisterAttached("TabNavigation", typeof(KeyboardNavigationMode), typeof(KeyboardNavigation),
            new PropertyMetadata(KeyboardNavigationMode.Continue));

    /// <summary>
    /// Identifies the DirectionalNavigation attached property.
    /// </summary>
    public static readonly DependencyProperty DirectionalNavigationProperty =
        DependencyProperty.RegisterAttached("DirectionalNavigation", typeof(KeyboardNavigationMode), typeof(KeyboardNavigation),
            new PropertyMetadata(KeyboardNavigationMode.Continue));

    /// <summary>
    /// Gets the tab index for the specified element.
    /// </summary>
    public static int GetTabIndex(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (int)(element.GetValue(TabIndexProperty) ?? int.MaxValue);
    }

    /// <summary>
    /// Sets the tab index for the specified element.
    /// </summary>
    public static void SetTabIndex(DependencyObject element, int value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(TabIndexProperty, value);
    }

    /// <summary>
    /// Gets whether the specified element is a tab stop.
    /// </summary>
    public static bool GetIsTabStop(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (bool)(element.GetValue(IsTabStopProperty) ?? true);
    }

    /// <summary>
    /// Sets whether the specified element is a tab stop.
    /// </summary>
    public static void SetIsTabStop(DependencyObject element, bool value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(IsTabStopProperty, value);
    }

    /// <summary>
    /// Gets the tab navigation mode for the specified element.
    /// </summary>
    public static KeyboardNavigationMode GetTabNavigation(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (KeyboardNavigationMode)(element.GetValue(TabNavigationProperty) ?? KeyboardNavigationMode.Continue);
    }

    /// <summary>
    /// Sets the tab navigation mode for the specified element.
    /// </summary>
    public static void SetTabNavigation(DependencyObject element, KeyboardNavigationMode value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(TabNavigationProperty, value);
    }

    /// <summary>
    /// Gets the directional navigation mode for the specified element.
    /// </summary>
    public static KeyboardNavigationMode GetDirectionalNavigation(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (KeyboardNavigationMode)(element.GetValue(DirectionalNavigationProperty) ?? KeyboardNavigationMode.Continue);
    }

    /// <summary>
    /// Sets the directional navigation mode for the specified element.
    /// </summary>
    public static void SetDirectionalNavigation(DependencyObject element, KeyboardNavigationMode value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(DirectionalNavigationProperty, value);
    }

    #endregion

    #region Navigation Methods

    /// <summary>
    /// Moves focus to the next element in the tab order.
    /// </summary>
    /// <param name="currentElement">The element that currently has focus.</param>
    /// <param name="reverse">True to move backward in the tab order.</param>
    /// <returns>True if focus was successfully moved; otherwise, false.</returns>
    public static bool MoveFocus(UIElement currentElement, bool reverse = false)
    {
        ArgumentNullException.ThrowIfNull(currentElement);

        // Get the root of the visual tree
        var root = GetVisualRoot(currentElement);
        if (root == null) return false;

        // Collect all focusable elements
        var focusableElements = new List<UIElement>();
        CollectFocusableElements(root, focusableElements);

        if (focusableElements.Count == 0) return false;

        // Sort by tab index, then by visual tree order
        focusableElements.Sort((a, b) =>
        {
            var indexA = GetTabIndex(a);
            var indexB = GetTabIndex(b);
            return indexA.CompareTo(indexB);
        });

        // Find the current element
        int currentIndex = focusableElements.IndexOf(currentElement);
        if (currentIndex < 0)
        {
            // Current element not in list, focus first element
            return FocusElement(focusableElements[0]);
        }

        // Calculate next index
        int nextIndex;
        if (reverse)
        {
            nextIndex = currentIndex - 1;
            if (nextIndex < 0) nextIndex = focusableElements.Count - 1;
        }
        else
        {
            nextIndex = currentIndex + 1;
            if (nextIndex >= focusableElements.Count) nextIndex = 0;
        }

        // Focus the next element
        return FocusElement(focusableElements[nextIndex]);
    }

    /// <summary>
    /// Moves focus in the specified direction.
    /// </summary>
    public static bool MoveFocus(UIElement currentElement, FocusNavigationDirection direction)
    {
        ArgumentNullException.ThrowIfNull(currentElement);

        return direction switch
        {
            FocusNavigationDirection.Next => MoveFocus(currentElement, reverse: false),
            FocusNavigationDirection.Previous => MoveFocus(currentElement, reverse: true),
            FocusNavigationDirection.First => MoveFocusToFirst(currentElement),
            FocusNavigationDirection.Last => MoveFocusToLast(currentElement),
            FocusNavigationDirection.Left or
            FocusNavigationDirection.Right or
            FocusNavigationDirection.Up or
            FocusNavigationDirection.Down => MoveFocusDirectional(currentElement, direction),
            _ => false
        };
    }

    private static bool MoveFocusToFirst(UIElement currentElement)
    {
        var root = GetVisualRoot(currentElement);
        if (root == null) return false;

        var focusableElements = new List<UIElement>();
        CollectFocusableElements(root, focusableElements);

        if (focusableElements.Count == 0) return false;

        focusableElements.Sort((a, b) => GetTabIndex(a).CompareTo(GetTabIndex(b)));
        return FocusElement(focusableElements[0]);
    }

    private static bool MoveFocusToLast(UIElement currentElement)
    {
        var root = GetVisualRoot(currentElement);
        if (root == null) return false;

        var focusableElements = new List<UIElement>();
        CollectFocusableElements(root, focusableElements);

        if (focusableElements.Count == 0) return false;

        focusableElements.Sort((a, b) => GetTabIndex(a).CompareTo(GetTabIndex(b)));
        return FocusElement(focusableElements[^1]);
    }

    private static bool MoveFocusDirectional(UIElement currentElement, FocusNavigationDirection direction)
    {
        var root = GetVisualRoot(currentElement);
        if (root == null) return false;

        var focusableElements = new List<UIElement>();
        CollectFocusableElements(root, focusableElements);

        if (focusableElements.Count <= 1) return false;

        // Get current element bounds
        var currentBounds = currentElement.VisualBounds;
        var currentCenter = new Point(
            currentBounds.X + currentBounds.Width / 2,
            currentBounds.Y + currentBounds.Height / 2);

        UIElement? bestCandidate = null;
        double bestScore = double.MaxValue;

        foreach (var candidate in focusableElements)
        {
            if (candidate == currentElement) continue;

            var candidateBounds = candidate.VisualBounds;
            var candidateCenter = new Point(
                candidateBounds.X + candidateBounds.Width / 2,
                candidateBounds.Y + candidateBounds.Height / 2);

            // Check if candidate is in the correct direction
            bool isInDirection = direction switch
            {
                FocusNavigationDirection.Left => candidateCenter.X < currentCenter.X,
                FocusNavigationDirection.Right => candidateCenter.X > currentCenter.X,
                FocusNavigationDirection.Up => candidateCenter.Y < currentCenter.Y,
                FocusNavigationDirection.Down => candidateCenter.Y > currentCenter.Y,
                _ => false
            };

            if (!isInDirection) continue;

            // Calculate score (distance)
            double dx = candidateCenter.X - currentCenter.X;
            double dy = candidateCenter.Y - currentCenter.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            // Prefer elements that are more aligned in the navigation direction
            double alignment = direction switch
            {
                FocusNavigationDirection.Left or FocusNavigationDirection.Right => Math.Abs(dy),
                FocusNavigationDirection.Up or FocusNavigationDirection.Down => Math.Abs(dx),
                _ => 0
            };

            double score = distance + alignment * 2;

            if (score < bestScore)
            {
                bestScore = score;
                bestCandidate = candidate;
            }
        }

        if (bestCandidate != null)
        {
            return FocusElement(bestCandidate);
        }

        return false;
    }

    private static UIElement? GetVisualRoot(UIElement element)
    {
        var current = element;
        while (current.VisualParent is UIElement parent)
        {
            current = parent;
        }
        return current;
    }

    private static void CollectFocusableElements(UIElement element, List<UIElement> results)
    {
        if (!element.IsEnabled || element.Visibility != Visibility.Visible)
            return;

        if (element.Focusable && GetIsTabStop(element))
        {
            results.Add(element);
        }

        // Recursively check children
        for (int i = 0; i < element.VisualChildrenCount; i++)
        {
            if (element.GetVisualChild(i) is UIElement child)
            {
                CollectFocusableElements(child, results);
            }
        }
    }

    private static bool FocusElement(UIElement element)
    {
        if (element.Focusable && element.IsEnabled)
        {
            return element.Focus();
        }
        return false;
    }

    #endregion
}

/// <summary>
/// Specifies the keyboard navigation mode.
/// </summary>
public enum KeyboardNavigationMode
{
    /// <summary>
    /// Tab navigation continues to the next element.
    /// </summary>
    Continue,

    /// <summary>
    /// Tab navigation cycles within the container.
    /// </summary>
    Cycle,

    /// <summary>
    /// Tab navigation stops when reaching the last element.
    /// </summary>
    Once,

    /// <summary>
    /// Tab navigation is contained within the element.
    /// </summary>
    Contained,

    /// <summary>
    /// Tab navigation is locally contained.
    /// </summary>
    Local,

    /// <summary>
    /// No tab navigation.
    /// </summary>
    None
}

// FocusNavigationDirection enum is defined in Jalium.UI.Core
