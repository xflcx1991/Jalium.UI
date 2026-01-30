using System.Runtime.InteropServices;
using Jalium.UI.Controls.Themes;
using Jalium.UI.Input;
using Jalium.UI.Media.Animation;

namespace Jalium.UI.Controls;

/// <summary>
/// Encapsulates a Jalium.UI application.
/// </summary>
public partial class Application
{
    private static Application? _current;

    /// <summary>
    /// Gets the current application instance.
    /// </summary>
    public static Application? Current => _current;

    /// <summary>
    /// Gets or sets the main window.
    /// </summary>
    public Window? MainWindow { get; set; }

    private ResourceDictionary? _resources;

    /// <summary>
    /// Gets or sets the application-level resources.
    /// </summary>
    public ResourceDictionary Resources
    {
        get => _resources ??= new ResourceDictionary();
        set => _resources = value;
    }

    /// <summary>
    /// Occurs when the application is starting.
    /// </summary>
    public event EventHandler? Startup;

    /// <summary>
    /// Occurs when the application is exiting.
    /// </summary>
    public event EventHandler? Exit;

    /// <summary>
    /// Initializes a new instance of the <see cref="Application"/> class.
    /// </summary>
    public Application()
    {
        if (_current != null)
        {
            throw new InvalidOperationException("Only one Application instance can be created.");
        }

        _current = this;

        // Initialize keyboard/focus system
        Keyboard.Initialize();

        // Register application resource lookup callback
        ResourceLookup.ApplicationResourceLookup = LookupApplicationResource;

        // Initialize default theme (loads default styles for all controls)
        ThemeManager.Initialize(this);
    }

    private static object? LookupApplicationResource(object resourceKey)
    {
        if (_current?._resources != null && _current._resources.TryGetValue(resourceKey, out var value))
        {
            return value;
        }
        return null;
    }

    /// <summary>
    /// Starts the application message loop.
    /// </summary>
    public void Run()
    {
        Startup?.Invoke(this, EventArgs.Empty);

        try
        {
            // Message loop
            while (GetMessage(out var msg, nint.Zero, 0, 0))
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }
        finally
        {
            // Cleanup resources to ensure timely exit
            Cleanup();
            Exit?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Cleanup()
    {
        // Stop all active animations
        Storyboard.StopAll();

        // Stop all tooltip timers
        ToolTipService.Cleanup();

        // Clear application reference
        _current = null;
    }

    /// <summary>
    /// Starts the application with the specified main window.
    /// </summary>
    /// <param name="mainWindow">The main window.</param>
    public void Run(Window mainWindow)
    {
        MainWindow = mainWindow;
        mainWindow.Show();
        Run();
    }

    /// <summary>
    /// Shuts down the application.
    /// </summary>
    public void Shutdown()
    {
        PostQuitMessage(0);
    }

    #region Win32 Interop

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public nint hwnd;
        public uint message;
        public nint wParam;
        public nint lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [LibraryImport("user32.dll", EntryPoint = "GetMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetMessage(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool TranslateMessage(ref MSG lpMsg);

    [LibraryImport("user32.dll", EntryPoint = "DispatchMessageW")]
    private static partial nint DispatchMessage(ref MSG lpMsg);

    [LibraryImport("user32.dll")]
    private static partial void PostQuitMessage(int nExitCode);

    #endregion
}
