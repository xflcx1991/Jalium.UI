using System.Runtime.InteropServices;
using System.Text;

namespace Jalium.UI.Controls;

/// <summary>
/// Displays the Win32 icon picker dialog.
/// </summary>
public sealed class PickIconDialog
{
    private const int DefaultBufferLength = 260;

    /// <summary>
    /// Gets or sets the icon resource path.
    /// </summary>
    public string IconPath { get; set; } = Environment.SystemDirectory + @"\shell32.dll";

    /// <summary>
    /// Gets or sets the selected icon index.
    /// </summary>
    public int IconIndex { get; set; }

    /// <summary>
    /// Shows the dialog.
    /// </summary>
    public bool ShowDialog()
    {
        return ShowDialog(DialogOwnerResolver.Resolve());
    }

    /// <summary>
    /// Shows the dialog with the specified owner window.
    /// </summary>
    public bool ShowDialog(IntPtr owner)
    {
        owner = DialogOwnerResolver.Resolve(owner);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ShowWindowsDialog(owner);
        }

        return ShowFallbackDialog();
    }

    private bool ShowWindowsDialog(IntPtr owner)
    {
        var buffer = new StringBuilder(string.IsNullOrWhiteSpace(IconPath) ? Environment.SystemDirectory + @"\shell32.dll" : IconPath, DefaultBufferLength);
        if (buffer.Capacity < DefaultBufferLength)
        {
            buffer.EnsureCapacity(DefaultBufferLength);
        }

        var iconIndex = IconIndex;
        if (PickIconDlg(owner, buffer, buffer.Capacity, ref iconIndex) == 0)
        {
            return false;
        }

        IconPath = buffer.ToString();
        IconIndex = iconIndex;
        return true;
    }

    private bool ShowFallbackDialog()
    {
        Console.Write($"Icon resource path [{IconPath}]: ");
        var path = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(path))
        {
            IconPath = path.Trim();
        }

        Console.Write($"Icon index [{IconIndex}]: ");
        var indexInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(indexInput) && int.TryParse(indexInput, out var parsedIndex))
        {
            IconIndex = parsedIndex;
        }

        return !string.IsNullOrWhiteSpace(IconPath);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int PickIconDlg(IntPtr hwnd, StringBuilder pszIconPath, int cchIconPath, ref int piIconIndex);
}

/// <summary>
/// Flags that control the Open With dialog.
/// </summary>
[Flags]
public enum OpenWithDialogFlags : uint
{
    /// <summary>
    /// No special behavior.
    /// </summary>
    None = 0,

    /// <summary>
    /// Enable the "always use this program" option.
    /// </summary>
    AllowRegistration = 0x00000001,

    /// <summary>
    /// Register the selected handler after the user confirms.
    /// </summary>
    RegisterExtension = 0x00000002,

    /// <summary>
    /// Execute the file after the user chooses an application.
    /// </summary>
    Execute = 0x00000004,

    /// <summary>
    /// Force the "always use this program" option to be checked.
    /// </summary>
    ForceRegistration = 0x00000008,

    /// <summary>
    /// Hide the registration option.
    /// </summary>
    HideRegistration = 0x00000020,

    /// <summary>
    /// Treat the class value as a URI protocol instead of a file extension.
    /// </summary>
    UrlProtocol = 0x00000040,

    /// <summary>
    /// Indicates that the file path is a URI.
    /// </summary>
    FileIsUri = 0x00000080
}

/// <summary>
/// Displays the Win32 Open With dialog.
/// </summary>
public sealed class OpenWithDialog
{
    private const int ErrorCancelled = unchecked((int)0x800704C7);

    /// <summary>
    /// Gets or sets the file path to open with another application.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Gets or sets the file class or protocol name.
    /// </summary>
    public string? ClassName { get; set; }

    /// <summary>
    /// Gets or sets the dialog behavior flags.
    /// </summary>
    public OpenWithDialogFlags Flags { get; set; } = OpenWithDialogFlags.Execute;

    /// <summary>
    /// Shows the dialog.
    /// </summary>
    public bool ShowDialog()
    {
        return ShowDialog(DialogOwnerResolver.Resolve());
    }

    /// <summary>
    /// Shows the dialog with the specified owner window.
    /// </summary>
    public bool ShowDialog(IntPtr owner)
    {
        owner = DialogOwnerResolver.Resolve(owner);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ShowWindowsDialog(owner);
        }

        return ShowFallbackDialog();
    }

    private bool ShowWindowsDialog(IntPtr owner)
    {
        if (string.IsNullOrWhiteSpace(FileName))
        {
            throw new InvalidOperationException("FileName must be set before showing the dialog.");
        }

        var info = new OPENASINFO
        {
            pcszFile = FileName,
            pcszClass = ClassName,
            oaifInFlags = Flags
        };

        var hr = SHOpenWithDialog(owner, in info);
        if (hr == 0)
        {
            return true;
        }

        if (hr == ErrorCancelled)
        {
            return false;
        }

        Marshal.ThrowExceptionForHR(hr);
        return false;
    }

    private bool ShowFallbackDialog()
    {
        Console.WriteLine($"Open With is only implemented on Windows. Target: {FileName ?? "(none)"}");
        return false;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OPENASINFO
    {
        public string pcszFile;
        public string? pcszClass;
        public OpenWithDialogFlags oaifInFlags;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHOpenWithDialog(IntPtr hwndParent, in OPENASINFO poainfo);
}
