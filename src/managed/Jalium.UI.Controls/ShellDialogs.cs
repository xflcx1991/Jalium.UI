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
