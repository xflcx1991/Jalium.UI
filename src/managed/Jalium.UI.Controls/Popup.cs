using Jalium.UI.Input;
using Jalium.UI.Interop;
using Jalium.UI.Media;
using System.Runtime.InteropServices;

namespace Jalium.UI.Controls;

/// <summary>
/// Specifies the placement of a Popup relative to its target element.
/// </summary>
public enum PlacementMode
{
    /// <summary>
    /// Popup is positioned at the bottom-left of the target element.
    /// </summary>
    Bottom,

    /// <summary>
    /// Popup is positioned centered over the target element.
    /// </summary>
    Center,

    /// <summary>
    /// Popup is positioned to the right of the target element.
    /// </summary>
    Right,

    /// <summary>
    /// Popup is positioned at the top-left of the target element.
    /// </summary>
    Top,

    /// <summary>
    /// Popup is positioned to the left of the target element.
    /// </summary>
    Left,

    /// <summary>
    /// Popup is positioned relative to the mouse cursor.
    /// </summary>
    Mouse,

    /// <summary>
    /// Position at the mouse pointer location.
    /// </summary>
    MousePoint,

    /// <summary>
    /// Popup is positioned relative to the top-left of the target element.
    /// </summary>
    Relative,

    /// <summary>
    /// Popup is positioned at the specified absolute screen position.
    /// </summary>
    Absolute,

    /// <summary>
    /// Popup is positioned at the bottom-left of the target element,
    /// but repositioned if it would go off screen.
    /// </summary>
    Custom
}

/// <summary>
/// Represents a popup window that displays content in a separate top-level window.
/// This is implemented as a Win32 HWND with WS_POPUP style, separate from the main window.
/// </summary>
[ContentProperty("Child")]
public partial class Popup : FrameworkElement
{
    private PopupWindow? _popupWindow;
    private Window? _parentWindow;

    #region Dependency Properties

    /// <summary>
    /// Identifies the IsOpen dependency property.
    /// </summary>
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(Popup),
            new PropertyMetadata(false, OnIsOpenChanged));

    /// <summary>
    /// Identifies the Child dependency property.
    /// </summary>
    public static readonly DependencyProperty ChildProperty =
        DependencyProperty.Register(nameof(Child), typeof(UIElement), typeof(Popup),
            new PropertyMetadata(null, OnChildChanged));

    /// <summary>
    /// Identifies the PlacementTarget dependency property.
    /// </summary>
    public static readonly DependencyProperty PlacementTargetProperty =
        DependencyProperty.Register(nameof(PlacementTarget), typeof(UIElement), typeof(Popup),
            new PropertyMetadata(null));

    /// <summary>
    /// Identifies the Placement dependency property.
    /// </summary>
    public static readonly DependencyProperty PlacementProperty =
        DependencyProperty.Register(nameof(Placement), typeof(PlacementMode), typeof(Popup),
            new PropertyMetadata(PlacementMode.Bottom, OnPlacementChanged));

    /// <summary>
    /// Identifies the HorizontalOffset dependency property.
    /// </summary>
    public static readonly DependencyProperty HorizontalOffsetProperty =
        DependencyProperty.Register(nameof(HorizontalOffset), typeof(double), typeof(Popup),
            new PropertyMetadata(0.0, OnOffsetChanged));

    /// <summary>
    /// Identifies the VerticalOffset dependency property.
    /// </summary>
    public static readonly DependencyProperty VerticalOffsetProperty =
        DependencyProperty.Register(nameof(VerticalOffset), typeof(double), typeof(Popup),
            new PropertyMetadata(0.0, OnOffsetChanged));

    /// <summary>
    /// Identifies the StaysOpen dependency property.
    /// </summary>
    public static readonly DependencyProperty StaysOpenProperty =
        DependencyProperty.Register(nameof(StaysOpen), typeof(bool), typeof(Popup),
            new PropertyMetadata(true));

    /// <summary>
    /// Identifies the AllowsTransparency dependency property.
    /// </summary>
    public static readonly DependencyProperty AllowsTransparencyProperty =
        DependencyProperty.Register(nameof(AllowsTransparency), typeof(bool), typeof(Popup),
            new PropertyMetadata(false));

    /// <summary>
    /// Identifies the PopupAnimation dependency property.
    /// </summary>
    public static readonly DependencyProperty PopupAnimationProperty =
        DependencyProperty.Register(nameof(PopupAnimation), typeof(PopupAnimation), typeof(Popup),
            new PropertyMetadata(PopupAnimation.None));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets whether the popup is open.
    /// </summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>
    /// Gets or sets the content of the popup.
    /// </summary>
    public UIElement? Child
    {
        get => (UIElement?)GetValue(ChildProperty);
        set => SetValue(ChildProperty, value);
    }

    /// <summary>
    /// Gets or sets the element relative to which the popup is positioned.
    /// </summary>
    public UIElement? PlacementTarget
    {
        get => (UIElement?)GetValue(PlacementTargetProperty);
        set => SetValue(PlacementTargetProperty, value);
    }

    /// <summary>
    /// Gets or sets how the popup is positioned.
    /// </summary>
    public PlacementMode Placement
    {
        get => (PlacementMode)GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    /// <summary>
    /// Gets or sets the horizontal offset from the placement position.
    /// </summary>
    public double HorizontalOffset
    {
        get => (double)GetValue(HorizontalOffsetProperty);
        set => SetValue(HorizontalOffsetProperty, value);
    }

    /// <summary>
    /// Gets or sets the vertical offset from the placement position.
    /// </summary>
    public double VerticalOffset
    {
        get => (double)GetValue(VerticalOffsetProperty);
        set => SetValue(VerticalOffsetProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the popup stays open when it loses focus.
    /// If false, the popup will close when clicking outside of it.
    /// </summary>
    public bool StaysOpen
    {
        get => (bool)GetValue(StaysOpenProperty);
        set => SetValue(StaysOpenProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the popup window allows transparency.
    /// </summary>
    public bool AllowsTransparency
    {
        get => (bool)GetValue(AllowsTransparencyProperty);
        set => SetValue(AllowsTransparencyProperty, value);
    }

    /// <summary>
    /// Gets or sets the animation to use when opening/closing the popup.
    /// </summary>
    public PopupAnimation PopupAnimation
    {
        get => (PopupAnimation)GetValue(PopupAnimationProperty);
        set => SetValue(PopupAnimationProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Occurs when the popup is opened.
    /// </summary>
    public event EventHandler? Opened;

    /// <summary>
    /// Occurs when the popup is closed.
    /// </summary>
    public event EventHandler? Closed;

    #endregion

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Popup popup)
        {
            if ((bool)e.NewValue)
            {
                popup.OpenPopup();
            }
            else
            {
                popup.ClosePopup();
            }
        }
    }

    private static void OnChildChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Popup popup && popup._popupWindow != null)
        {
            popup._popupWindow.SetContent((UIElement?)e.NewValue);
        }
    }

    private static void OnPlacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Popup popup && popup.IsOpen)
        {
            popup.UpdatePosition();
        }
    }

    private static void OnOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Popup popup && popup.IsOpen)
        {
            popup.UpdatePosition();
        }
    }

    private void OpenPopup()
    {
        if (_popupWindow != null) return;

        var child = Child;
        if (child == null) return;

        // Get minimum width from multiple sources:
        // 1. Popup's own MinWidth (if explicitly set)
        // 2. TemplatedParent's ActualWidth (for popups inside control templates like ComboBox)
        // 3. PlacementTarget's ActualWidth
        // 4. Fallback to 50
        var popupMinWidth = MinWidth > 0 && !double.IsNaN(MinWidth) && !double.IsInfinity(MinWidth) ? MinWidth : 0;
        var templatedParentWidth = TemplatedParent is FrameworkElement tp ? tp.ActualWidth : 0;
        var targetWidth = PlacementTarget is FrameworkElement target ? target.ActualWidth : 0;
        var minWidth = Math.Max(50, Math.Max(popupMinWidth, Math.Max(templatedParentWidth, targetWidth)));

        // Calculate the popup size from child's desired size
        // Use the min width as available width for measurement
        child.Measure(new Size(minWidth, double.PositiveInfinity));
        var childSize = child is FrameworkElement fe ? fe.DesiredSize : new Size(100, 100);

        // Clamp to reasonable maximum to avoid overflow issues
        var maxReasonableSize = 4096.0;

        var childWidth = double.IsInfinity(childSize.Width) || childSize.Width > maxReasonableSize
            ? minWidth : Math.Max(childSize.Width, minWidth);
        var childHeight = double.IsInfinity(childSize.Height) || childSize.Height > maxReasonableSize
            ? 200.0 : childSize.Height;

        var width = Math.Max((int)minWidth, (int)childWidth);
        var height = Math.Max(20, (int)childHeight);

        // If child has explicit Width/Height set, use those
        if (child is FrameworkElement childFe)
        {
            if (!double.IsNaN(childFe.Width) && childFe.Width > 0)
                width = Math.Max(width, (int)childFe.Width);
            if (!double.IsNaN(childFe.Height) && childFe.Height > 0)
                height = Math.Max(height, (int)childFe.Height);
            // Also check MinWidth/MinHeight
            if (childFe.MinWidth > 0)
                width = Math.Max(width, (int)childFe.MinWidth);
            if (childFe.MinHeight > 0)
                height = Math.Max(height, (int)childFe.MinHeight);
        }

        // Calculate the position in screen coordinates
        var screenPosition = CalculateScreenPosition(new Size(width, height));

        // Create the popup window
        _popupWindow = new PopupWindow(this, AllowsTransparency);
        _popupWindow.SetContent(child);
        _popupWindow.Show((int)screenPosition.X, (int)screenPosition.Y, width, height);

        // Register for deactivation if not StaysOpen
        if (!StaysOpen)
        {
            _popupWindow.Deactivated += OnPopupDeactivated;
            // Note: We don't use the global mouse hook anymore because it fires BEFORE
            // WM_LBUTTONDOWN is posted, causing BeginInvoke to process before the clicked
            // element receives its MouseDown event. Instead, the parent window closes
            // light-dismiss popups at the start of its mouse down handling.
        }

        // Subscribe to parent window's LocationChanged to reposition popup when window moves
        _parentWindow = GetParentWindow();
        if (_parentWindow != null)
        {
            _parentWindow.LocationChanged += OnParentWindowLocationChanged;
            // Register with the parent window so light-dismiss clicks are handled centrally
            if (!StaysOpen)
            {
                _parentWindow.RegisterLightDismissPopup(this);
            }
        }

        Opened?.Invoke(this, EventArgs.Empty);
    }

    private void ClosePopup()
    {
        if (_popupWindow == null) return;

        // Unsubscribe from parent window events
        if (_parentWindow != null)
        {
            _parentWindow.LocationChanged -= OnParentWindowLocationChanged;
            _parentWindow.UnregisterLightDismissPopup(this);
            _parentWindow = null;
        }

        _popupWindow.Deactivated -= OnPopupDeactivated;
        _popupWindow.Close();
        _popupWindow = null;

        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void OnPopupDeactivated(object? sender, EventArgs e)
    {
        // Close the popup when it loses focus (if StaysOpen is false)
        if (!StaysOpen)
        {
            IsOpen = false;
        }
    }

    private void OnParentWindowLocationChanged(object? sender, EventArgs e)
    {
        // Reposition popup when parent window moves
        UpdatePosition();
    }

    /// <summary>
    /// Updates the position of the popup window.
    /// </summary>
    public void UpdatePosition()
    {
        if (_popupWindow == null || Child == null) return;

        var childSize = Child is FrameworkElement fe ? fe.DesiredSize : new Size(100, 100);
        var screenPosition = CalculateScreenPosition(childSize);
        _popupWindow.Move((int)screenPosition.X, (int)screenPosition.Y);
    }

    private Point CalculateScreenPosition(Size popupSize)
    {
        var target = PlacementTarget ?? this;
        var targetScreenBounds = GetElementScreenBounds(target);

        double x = 0, y = 0;

        switch (Placement)
        {
            case PlacementMode.Bottom:
                x = targetScreenBounds.X;
                y = targetScreenBounds.Y + targetScreenBounds.Height;
                break;

            case PlacementMode.Top:
                x = targetScreenBounds.X;
                y = targetScreenBounds.Y - popupSize.Height;
                break;

            case PlacementMode.Left:
                x = targetScreenBounds.X - popupSize.Width;
                y = targetScreenBounds.Y;
                break;

            case PlacementMode.Right:
                x = targetScreenBounds.X + targetScreenBounds.Width;
                y = targetScreenBounds.Y;
                break;

            case PlacementMode.Center:
                x = targetScreenBounds.X + (targetScreenBounds.Width - popupSize.Width) / 2;
                y = targetScreenBounds.Y + (targetScreenBounds.Height - popupSize.Height) / 2;
                break;

            case PlacementMode.Relative:
                x = targetScreenBounds.X;
                y = targetScreenBounds.Y;
                break;

            case PlacementMode.Absolute:
                // Absolute uses the offsets directly as screen coordinates
                x = 0;
                y = 0;
                break;

            case PlacementMode.Mouse:
                var mousePos = GetCursorPosition();
                x = mousePos.X;
                y = mousePos.Y + 20; // Offset below cursor
                break;

            case PlacementMode.Custom:
                // Start at bottom, but adjust if off screen
                x = targetScreenBounds.X;
                y = targetScreenBounds.Y + targetScreenBounds.Height;
                AdjustForScreenBounds(ref x, ref y, popupSize);
                break;
        }

        x += HorizontalOffset;
        y += VerticalOffset;

        return new Point(x, y);
    }

    private Rect GetElementScreenBounds(UIElement element)
    {
        // Get the element's bounds in client coordinates
        var bounds = element.VisualBounds;

        // Walk up the visual tree to accumulate offsets and find the window
        Window? window = null;
        var current = element.VisualParent;
        while (current != null)
        {
            if (current is Window w)
            {
                window = w;
                break;
            }
            if (current is UIElement uiElement)
            {
                var parentBounds = uiElement.VisualBounds;
                bounds = new Rect(
                    bounds.X + parentBounds.X,
                    bounds.Y + parentBounds.Y,
                    bounds.Width,
                    bounds.Height);
            }
            current = current.VisualParent;
        }

        // Convert to screen coordinates using the parent window
        if (window != null && window.Handle != nint.Zero)
        {
            var pt = new POINT { X = (int)bounds.X, Y = (int)bounds.Y };
            ClientToScreen(window.Handle, ref pt);
            bounds = new Rect(pt.X, pt.Y, bounds.Width, bounds.Height);
        }

        return bounds;
    }

    private void AdjustForScreenBounds(ref double x, ref double y, Size popupSize)
    {
        // Get the work area of the primary monitor
        var workArea = GetPrimaryWorkArea();

        // Adjust horizontal position
        if (x + popupSize.Width > workArea.Right)
        {
            x = workArea.Right - popupSize.Width;
        }
        if (x < workArea.Left)
        {
            x = workArea.Left;
        }

        // Adjust vertical position
        if (y + popupSize.Height > workArea.Bottom)
        {
            // Try placing above the target instead
            var target = PlacementTarget ?? this;
            var targetBounds = GetElementScreenBounds(target);
            y = targetBounds.Y - popupSize.Height;
        }
        if (y < workArea.Top)
        {
            y = workArea.Top;
        }
    }

    private static Rect GetPrimaryWorkArea()
    {
        var rect = new RECT();
        SystemParametersInfo(SPI_GETWORKAREA, 0, ref rect, 0);
        return new Rect(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
    }

    private static Point GetCursorPosition()
    {
        GetCursorPos(out var pt);
        return new Point(pt.X, pt.Y);
    }

    private Window? GetParentWindow()
    {
        // First try from this popup
        Visual? current = this;
        while (current != null)
        {
            if (current is Window window)
                return window;
            current = current.VisualParent;
        }

        // If not found, try from PlacementTarget
        current = PlacementTarget;
        while (current != null)
        {
            if (current is Window window)
                return window;
            current = current.VisualParent;
        }

        return null;
    }

    #region Win32 Interop

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left, top, right, bottom;
    }

    private const int SPI_GETWORKAREA = 0x0030;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ClientToScreen(nint hWnd, ref POINT lpPoint);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out POINT lpPoint);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SystemParametersInfo(int uiAction, int uiParam, ref RECT pvParam, int fWinIni);

    #endregion
}

/// <summary>
/// Specifies the animation type for popup opening/closing.
/// </summary>
public enum PopupAnimation
{
    /// <summary>
    /// No animation.
    /// </summary>
    None,

    /// <summary>
    /// Fade animation.
    /// </summary>
    Fade,

    /// <summary>
    /// Slide animation.
    /// </summary>
    Slide,

    /// <summary>
    /// Scroll animation.
    /// </summary>
    Scroll
}

/// <summary>
/// Internal window class for hosting popup content.
/// This is a separate Win32 HWND with WS_POPUP | WS_EX_TOOLWINDOW styles.
/// </summary>
internal partial class PopupWindow : IDisposable
{
    private readonly Popup _popup;
    private readonly bool _allowsTransparency;
    private nint _handle;
    private UIElement? _content;
    private RenderTarget? _renderTarget;
    private RenderTargetDrawingContext? _drawingContext;
    private int _width;
    private int _height;
    private readonly HashSet<UIElement> _elementsUnderMouse = new(); // Track all elements currently under mouse
    private nint _mouseHook;
    private LowLevelMouseProc? _mouseHookProc; // Keep reference to prevent GC

    private static bool _classRegistered;
    private static readonly Dictionary<nint, PopupWindow> _popupWindows = new();
    private static WndProcDelegate? _wndProcDelegate;

    private delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);

    public event EventHandler? Deactivated;

    public PopupWindow(Popup popup, bool allowsTransparency)
    {
        _popup = popup;
        _allowsTransparency = allowsTransparency;
    }

    public void SetContent(UIElement? content)
    {
        _content = content;
        if (_handle != nint.Zero)
        {
            InvalidateRect(_handle, nint.Zero, false);
        }
    }

    public void Show(int x, int y, int width, int height)
    {
        // Ensure minimum size to avoid render target creation failure
        _width = Math.Max(1, width);
        _height = Math.Max(1, height);

        RegisterWindowClass();

        // WS_POPUP: No border, no title bar
        // WS_EX_TOOLWINDOW: Not in taskbar, not in Alt+Tab
        // WS_EX_TOPMOST: Always on top
        // WS_EX_NOACTIVATE: Don't activate when shown - allows clicks to pass through to underlying windows
        uint exStyle = WS_EX_TOOLWINDOW | WS_EX_TOPMOST | WS_EX_NOACTIVATE;
        if (_allowsTransparency)
        {
            exStyle |= WS_EX_LAYERED;
        }

        _handle = CreateWindowEx(
            exStyle,
            PopupWindowClassName,
            "",
            WS_POPUP | WS_CLIPSIBLINGS | WS_CLIPCHILDREN,
            x, y, width, height,
            nint.Zero,
            nint.Zero,
            GetModuleHandle(null),
            nint.Zero);

        if (_handle == nint.Zero)
        {
            throw new InvalidOperationException($"Failed to create popup window. Error: {Marshal.GetLastWin32Error()}");
        }

        _popupWindows[_handle] = this;

        // Show window first - D3D12/DXGI swap chain creation may require visible window
        ShowWindow(_handle, SW_SHOWNOACTIVATE);
        UpdateWindow(_handle);

        // Create render target after window is shown
        EnsureRenderTarget();

        // Trigger initial paint
        InvalidateRect(_handle, nint.Zero, false);
    }

    /// <summary>
    /// Installs a global mouse hook to detect clicks outside the popup.
    /// Used for light dismiss behavior when StaysOpen is false.
    /// </summary>
    public void InstallMouseHook()
    {
        if (_mouseHook != nint.Zero) return;

        _mouseHookProc = MouseHookCallback;
        _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProc, nint.Zero, 0);
    }

    /// <summary>
    /// Uninstalls the global mouse hook.
    /// </summary>
    public void UninstallMouseHook()
    {
        if (_mouseHook != nint.Zero)
        {
            UnhookWindowsHookEx(_mouseHook);
            _mouseHook = nint.Zero;
            _mouseHookProc = null;
        }
    }

    private nint MouseHookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= HC_ACTION && _handle != nint.Zero)
        {
            uint msg = (uint)wParam.ToInt64();
            // Check for mouse button down events
            if (msg == WM_LBUTTONDOWN_HOOK || msg == WM_RBUTTONDOWN_HOOK || msg == WM_MBUTTONDOWN_HOOK)
            {
                // Get current cursor position
                if (GetCursorPos(out var cursorPos))
                {
                    // Check if click is inside the popup window
                    if (GetWindowRect(_handle, out var windowRect))
                    {
                        bool isInsidePopup = cursorPos.X >= windowRect.left && cursorPos.X < windowRect.right &&
                                             cursorPos.Y >= windowRect.top && cursorPos.Y < windowRect.bottom;

                        if (!isInsidePopup)
                        {
                            // Click is outside popup - trigger close
                            // Use Dispatcher to avoid issues with hook callback context
                            _popup.Dispatcher?.BeginInvoke(() =>
                            {
                                Deactivated?.Invoke(this, EventArgs.Empty);
                            });
                        }
                    }
                }
            }
        }

        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    public void Move(int x, int y)
    {
        if (_handle != nint.Zero)
        {
            SetWindowPos(_handle, nint.Zero, x, y, 0, 0,
                SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
        }
    }

    public void Resize(int width, int height)
    {
        if (_handle != nint.Zero && (width != _width || height != _height))
        {
            _width = width;
            _height = height;
            SetWindowPos(_handle, nint.Zero, 0, 0, width, height,
                SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
            _renderTarget?.Resize(width, height);
        }
    }

    public void Close()
    {
        // Uninstall mouse hook first
        UninstallMouseHook();

        if (_handle != nint.Zero)
        {
            _popupWindows.Remove(_handle);
            _renderTarget?.Dispose();
            _renderTarget = null;
            _drawingContext = null;
            DestroyWindow(_handle);
            _handle = nint.Zero;
        }
    }

    public void Dispose()
    {
        Close();
    }

    private void EnsureRenderTarget()
    {
        if (_renderTarget != null) return;

        var context = RenderContext.Current;
        if (context == null || !context.IsValid)
        {
            context = new RenderContext(RenderBackend.D3D12);
        }

        _renderTarget = context.CreateRenderTarget(_handle, _width, _height);
    }

    #region Window Class Registration

    private const string PopupWindowClassName = "JaliumUIPopup";

    private static void RegisterWindowClass()
    {
        if (_classRegistered)
            return;

        _wndProcDelegate = WndProc;

        var wc = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            style = CS_DROPSHADOW, // Add drop shadow for popup
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
            hInstance = GetModuleHandle(null),
            hCursor = LoadCursor(nint.Zero, IDC_ARROW),
            hbrBackground = nint.Zero,
            lpszClassName = PopupWindowClassName
        };

        var atom = RegisterClassEx(ref wc);
        if (atom == 0)
        {
            throw new InvalidOperationException($"Failed to register popup window class. Error: {Marshal.GetLastWin32Error()}");
        }

        _classRegistered = true;
    }

    private static nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (_popupWindows.TryGetValue(hWnd, out var popup))
        {
            switch (msg)
            {
                case WM_PAINT:
                    popup.OnPaint();
                    return nint.Zero;

                case WM_ERASEBKGND:
                    return (nint)1;

                case WM_SIZE:
                    int width = (int)(lParam.ToInt64() & 0xFFFF);
                    int height = (int)((lParam.ToInt64() >> 16) & 0xFFFF);
                    if (width > 0 && height > 0)
                    {
                        popup._width = width;
                        popup._height = height;
                        popup._renderTarget?.Resize(width, height);
                    }
                    return nint.Zero;

                case WM_ACTIVATE:
                    int activateState = (int)(wParam.ToInt64() & 0xFFFF);
                    if (activateState == WA_INACTIVE)
                    {
                        popup.Deactivated?.Invoke(popup, EventArgs.Empty);
                    }
                    return nint.Zero;

                case WM_MOUSEACTIVATE:
                    // Don't activate on mouse click - return MA_NOACTIVATE
                    // This keeps focus on the parent window
                    return (nint)MA_NOACTIVATE;

                // Forward mouse events to the content
                case WM_MOUSEMOVE:
                case WM_LBUTTONDOWN:
                case WM_LBUTTONUP:
                case WM_RBUTTONDOWN:
                case WM_RBUTTONUP:
                case WM_MBUTTONDOWN:
                case WM_MBUTTONUP:
                    popup.HandleMouseMessage(msg, wParam, lParam);
                    return nint.Zero;

                case WM_MOUSELEAVE:
                    popup.HandleMouseLeave();
                    return nint.Zero;

                case WM_DESTROY:
                    _popupWindows.Remove(hWnd);
                    return nint.Zero;
            }
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    #endregion

    #region Rendering

    private void OnPaint()
    {
        var ps = new PAINTSTRUCT();
        var hdc = BeginPaint(_handle, out ps);

        try
        {
            if (_renderTarget != null && _renderTarget.IsValid && _content != null)
            {
                // Layout the content
                _content.Measure(new Size(_width, _height));
                _content.Arrange(new Rect(0, 0, _width, _height));

                _renderTarget.BeginDraw();

                // Clear with the content's background color if available, otherwise use dark gray
                float clearR = 0.15f, clearG = 0.15f, clearB = 0.15f, clearA = 1.0f;
                if (_content is Control control && control.Background is SolidColorBrush bgBrush)
                {
                    var color = bgBrush.Color;
                    clearR = color.ScR;
                    clearG = color.ScG;
                    clearB = color.ScB;
                    clearA = color.ScA;
                }
                else if (_content is Border border && border.Background is SolidColorBrush borderBgBrush)
                {
                    var color = borderBgBrush.Color;
                    clearR = color.ScR;
                    clearG = color.ScG;
                    clearB = color.ScB;
                    clearA = color.ScA;
                }
                _renderTarget.Clear(clearR, clearG, clearB, clearA);

                // Render the content
                var context = RenderContext.Current;
                if (context != null)
                {
                    _drawingContext ??= new RenderTargetDrawingContext(_renderTarget, context);
                    _drawingContext.Offset = Point.Zero;
                    _content.Render(_drawingContext);
                }

                _renderTarget.EndDraw();

                // Trim caches to prevent memory from growing unbounded
                _drawingContext?.TrimCacheIfNeeded();
            }
        }
        catch
        {
            // Ignore render errors
        }

        EndPaint(_handle, ref ps);
    }

    #endregion

    #region Input Handling

    private void HandleMouseMessage(uint msg, nint wParam, nint lParam)
    {
        if (_content == null) return;

        int x = (short)(lParam.ToInt64() & 0xFFFF);
        int y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);
        var position = new Point(x, y);

        var target = HitTest(position);
        if (target == null) target = _content; // Fallback to content

        var modifiers = GetModifierKeys();
        var buttonStates = GetMouseButtonStates(wParam);
        int timestamp = Environment.TickCount;

        // Build the chain of elements from target up to content root
        var newElementsUnderMouse = new HashSet<UIElement>();
        UIElement? current = target;
        while (current != null)
        {
            newElementsUnderMouse.Add(current);
            current = (current as Visual)?.VisualParent as UIElement;
        }

        // Raise MouseLeave on elements no longer under mouse
        foreach (var element in _elementsUnderMouse)
        {
            if (!newElementsUnderMouse.Contains(element))
            {
                var leaveArgs = new MouseEventArgs(
                    UIElement.MouseLeaveEvent, position,
                    buttonStates.left, buttonStates.middle, buttonStates.right,
                    buttonStates.xButton1, buttonStates.xButton2, modifiers, timestamp);
                element.RaiseEvent(leaveArgs);
            }
        }

        // Raise MouseEnter on elements newly under mouse
        foreach (var element in newElementsUnderMouse)
        {
            if (!_elementsUnderMouse.Contains(element))
            {
                var enterArgs = new MouseEventArgs(
                    UIElement.MouseEnterEvent, position,
                    buttonStates.left, buttonStates.middle, buttonStates.right,
                    buttonStates.xButton1, buttonStates.xButton2, modifiers, timestamp);
                element.RaiseEvent(enterArgs);
            }
        }

        // Update tracking
        _elementsUnderMouse.Clear();
        foreach (var element in newElementsUnderMouse)
        {
            _elementsUnderMouse.Add(element);
        }

        switch (msg)
        {
            case WM_MOUSEMOVE:
                var moveArgs = new MouseEventArgs(
                    UIElement.MouseMoveEvent, position,
                    buttonStates.left, buttonStates.middle, buttonStates.right,
                    buttonStates.xButton1, buttonStates.xButton2, modifiers, timestamp);
                target.RaiseEvent(moveArgs);
                break;

            case WM_LBUTTONDOWN:
                RaiseMouseButtonEvent(target, MouseButton.Left, MouseButtonState.Pressed, position, buttonStates, modifiers, timestamp);
                break;

            case WM_LBUTTONUP:
                RaiseMouseButtonEvent(target, MouseButton.Left, MouseButtonState.Released, position, buttonStates, modifiers, timestamp);
                // Direct handling for ComboBoxItem clicks - traverse visual tree to find ComboBoxItem
                TryInvokeComboBoxItemClick(target);
                break;

            case WM_RBUTTONDOWN:
                RaiseMouseButtonEvent(target, MouseButton.Right, MouseButtonState.Pressed, position, buttonStates, modifiers, timestamp);
                break;

            case WM_RBUTTONUP:
                RaiseMouseButtonEvent(target, MouseButton.Right, MouseButtonState.Released, position, buttonStates, modifiers, timestamp);
                break;

            case WM_MBUTTONDOWN:
                RaiseMouseButtonEvent(target, MouseButton.Middle, MouseButtonState.Pressed, position, buttonStates, modifiers, timestamp);
                break;

            case WM_MBUTTONUP:
                RaiseMouseButtonEvent(target, MouseButton.Middle, MouseButtonState.Released, position, buttonStates, modifiers, timestamp);
                break;
        }

        // Invalidate after input handling
        InvalidateRect(_handle, nint.Zero, false);
    }

    private void RaiseMouseButtonEvent(UIElement target, MouseButton button, MouseButtonState state, Point position,
        (MouseButtonState left, MouseButtonState middle, MouseButtonState right, MouseButtonState xButton1, MouseButtonState xButton2) buttonStates,
        ModifierKeys modifiers, int timestamp)
    {
        var routedEvent = state == MouseButtonState.Pressed ? UIElement.MouseDownEvent : UIElement.MouseUpEvent;
        var args = new MouseButtonEventArgs(
            routedEvent, position, button, state, clickCount: 1,
            buttonStates.left, buttonStates.middle, buttonStates.right,
            buttonStates.xButton1, buttonStates.xButton2, modifiers, timestamp);
        target.RaiseEvent(args);
    }

    /// <summary>
    /// Directly invokes ComboBoxItem click - workaround for routed event issues in popup context.
    /// </summary>
    private void TryInvokeComboBoxItemClick(UIElement target)
    {
        // Walk up the visual tree to find a ComboBoxItem
        UIElement? current = target;
        while (current != null)
        {
            if (current is ComboBoxItem comboBoxItem)
            {
                comboBoxItem.InvokeClick();
                return;
            }
            current = (current as Visual)?.VisualParent as UIElement;
        }
    }

    private UIElement? HitTest(Point position)
    {
        if (_content == null) return null;

        // HitTest is on FrameworkElement
        if (_content is FrameworkElement fe)
        {
            var result = fe.HitTest(position);
            return result?.VisualHit as UIElement ?? _content;
        }

        // Fallback: return the content itself if it contains the point
        return _content;
    }

    private static ModifierKeys GetModifierKeys()
    {
        ModifierKeys modifiers = ModifierKeys.None;
        if ((GetKeyState(VK_SHIFT) & 0x8000) != 0) modifiers |= ModifierKeys.Shift;
        if ((GetKeyState(VK_CONTROL) & 0x8000) != 0) modifiers |= ModifierKeys.Control;
        if ((GetKeyState(VK_MENU) & 0x8000) != 0) modifiers |= ModifierKeys.Alt;
        return modifiers;
    }

    private static (MouseButtonState left, MouseButtonState middle, MouseButtonState right, MouseButtonState xButton1, MouseButtonState xButton2) GetMouseButtonStates(nint wParam)
    {
        int flags = (int)wParam.ToInt64();
        return (
            (flags & MK_LBUTTON) != 0 ? MouseButtonState.Pressed : MouseButtonState.Released,
            (flags & MK_MBUTTON) != 0 ? MouseButtonState.Pressed : MouseButtonState.Released,
            (flags & MK_RBUTTON) != 0 ? MouseButtonState.Pressed : MouseButtonState.Released,
            (flags & MK_XBUTTON1) != 0 ? MouseButtonState.Pressed : MouseButtonState.Released,
            (flags & MK_XBUTTON2) != 0 ? MouseButtonState.Pressed : MouseButtonState.Released
        );
    }

    private void HandleMouseLeave()
    {
        // Raise MouseLeave on all elements that were under the mouse
        var modifiers = GetModifierKeys();
        int timestamp = Environment.TickCount;

        foreach (var element in _elementsUnderMouse)
        {
            var leaveArgs = new MouseEventArgs(
                UIElement.MouseLeaveEvent, Point.Zero,
                MouseButtonState.Released, MouseButtonState.Released, MouseButtonState.Released,
                MouseButtonState.Released, MouseButtonState.Released, modifiers, timestamp);
            element.RaiseEvent(leaveArgs);
        }
        _elementsUnderMouse.Clear();

        InvalidateRect(_handle, nint.Zero, false);
    }

    #endregion

    #region Win32 Constants and Interop

    private const uint WS_POPUP = 0x80000000;
    private const uint WS_CLIPSIBLINGS = 0x04000000;
    private const uint WS_CLIPCHILDREN = 0x02000000;
    private const uint WS_EX_TOOLWINDOW = 0x00000080;
    private const uint WS_EX_TOPMOST = 0x00000008;
    private const uint WS_EX_LAYERED = 0x00080000;
    private const uint WS_EX_NOACTIVATE = 0x08000000;
    private const uint CS_DROPSHADOW = 0x00020000;

    private const int SW_SHOWNOACTIVATE = 4;

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    private const uint WM_PAINT = 0x000F;
    private const uint WM_ERASEBKGND = 0x0014;
    private const uint WM_SIZE = 0x0005;
    private const uint WM_DESTROY = 0x0002;
    private const uint WM_ACTIVATE = 0x0006;
    private const uint WM_MOUSEACTIVATE = 0x0021;

    private const int WA_INACTIVE = 0;
    private const int MA_NOACTIVATE = 3;

    private const uint WM_MOUSEMOVE = 0x0200;
    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_RBUTTONDOWN = 0x0204;
    private const uint WM_RBUTTONUP = 0x0205;
    private const uint WM_MBUTTONDOWN = 0x0207;
    private const uint WM_MBUTTONUP = 0x0208;
    private const uint WM_MOUSELEAVE = 0x02A3;

    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;

    private const int MK_LBUTTON = 0x0001;
    private const int MK_RBUTTON = 0x0002;
    private const int MK_MBUTTON = 0x0010;
    private const int MK_XBUTTON1 = 0x0020;
    private const int MK_XBUTTON2 = 0x0040;

    private const nint IDC_ARROW = 32512;

    [StructLayout(LayoutKind.Sequential)]
    private struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public nint lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszClassName;
        public nint hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PAINTSTRUCT
    {
        public nint hdc;
        public bool fErase;
        public RECT rcPaint;
        public bool fRestore;
        public bool fIncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[]? rgbReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left, top, right, bottom;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX lpWndClass);

    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint CreateWindowEx(
        uint dwExStyle,
        string lpClassName,
        string lpWindowName,
        uint dwStyle,
        int x, int y, int nWidth, int nHeight,
        nint hWndParent,
        nint hMenu,
        nint hInstance,
        nint lpParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(nint hWnd, int nCmdShow);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UpdateWindow(nint hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyWindow(nint hWnd);

    [LibraryImport("user32.dll", EntryPoint = "DefWindowProcW")]
    private static partial nint DefWindowProc(nint hWnd, uint msg, nint wParam, nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "LoadCursorW")]
    private static partial nint LoadCursor(nint hInstance, nint lpCursorName);

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint GetModuleHandle(string? lpModuleName);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool InvalidateRect(nint hWnd, nint lpRect, [MarshalAs(UnmanagedType.Bool)] bool bErase);

    [DllImport("user32.dll")]
    private static extern nint BeginPaint(nint hWnd, out PAINTSTRUCT lpPaint);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EndPaint(nint hWnd, ref PAINTSTRUCT lpPaint);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [LibraryImport("user32.dll")]
    private static partial short GetKeyState(int nVirtKey);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial nint SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, nint hMod, uint dwThreadId);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnhookWindowsHookEx(nint hhk);

    [LibraryImport("user32.dll")]
    private static partial nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out POINT lpPoint);

    [LibraryImport("user32.dll")]
    private static partial nint WindowFromPoint(POINT point);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(nint hWnd, out RECT lpRect);

    private const int WH_MOUSE_LL = 14;
    private const int HC_ACTION = 0;
    private const uint WM_LBUTTONDOWN_HOOK = 0x0201;
    private const uint WM_RBUTTONDOWN_HOOK = 0x0204;
    private const uint WM_MBUTTONDOWN_HOOK = 0x0207;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public nint dwExtraInfo;
    }

    private delegate nint LowLevelMouseProc(int nCode, nint wParam, nint lParam);

    #endregion
}
