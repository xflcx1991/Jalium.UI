using System.Runtime.InteropServices;

namespace Jalium.UI.Interop;

/// <summary>
/// Provides clipboard operations via Windows API.
/// </summary>
public static partial class Clipboard
{
    private const uint CF_UNICODETEXT = 13;

    /// <summary>
    /// Gets text from the clipboard.
    /// </summary>
    /// <returns>The text from the clipboard, or null if no text is available.</returns>
    public static string? GetText()
    {
        if (!OpenClipboard(nint.Zero))
            return null;

        try
        {
            var handle = GetClipboardData(CF_UNICODETEXT);
            if (handle == nint.Zero)
                return null;

            var ptr = GlobalLock(handle);
            if (ptr == nint.Zero)
                return null;

            try
            {
                return Marshal.PtrToStringUni(ptr);
            }
            finally
            {
                GlobalUnlock(handle);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    /// <summary>
    /// Sets text to the clipboard.
    /// </summary>
    /// <param name="text">The text to set.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public static bool SetText(string? text)
    {
        if (text == null)
            return false;

        if (!OpenClipboard(nint.Zero))
            return false;

        try
        {
            EmptyClipboard();

            // Calculate the number of bytes needed (including null terminator)
            var byteCount = (text.Length + 1) * sizeof(char);
            var hGlobal = GlobalAlloc(GMEM_MOVEABLE, (nuint)byteCount);

            if (hGlobal == nint.Zero)
                return false;

            var ptr = GlobalLock(hGlobal);
            if (ptr == nint.Zero)
            {
                GlobalFree(hGlobal);
                return false;
            }

            try
            {
                // Copy the string to the global memory
                Marshal.Copy(text.ToCharArray(), 0, ptr, text.Length);
                // Add null terminator
                Marshal.WriteInt16(ptr + text.Length * sizeof(char), 0);
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }

            if (SetClipboardData(CF_UNICODETEXT, hGlobal) == nint.Zero)
            {
                GlobalFree(hGlobal);
                return false;
            }

            return true;
        }
        finally
        {
            CloseClipboard();
        }
    }

    /// <summary>
    /// Checks if the clipboard contains text.
    /// </summary>
    /// <returns>True if the clipboard contains text, false otherwise.</returns>
    public static bool ContainsText()
    {
        return IsClipboardFormatAvailable(CF_UNICODETEXT);
    }

    /// <summary>
    /// Clears the clipboard.
    /// </summary>
    /// <returns>True if successful, false otherwise.</returns>
    public static bool Clear()
    {
        if (!OpenClipboard(nint.Zero))
            return false;

        try
        {
            return EmptyClipboard();
        }
        finally
        {
            CloseClipboard();
        }
    }

    #region Native Methods

    private const uint GMEM_MOVEABLE = 0x0002;

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenClipboard(nint hWndNewOwner);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EmptyClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial nint GetClipboardData(uint uFormat);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial nint SetClipboardData(uint uFormat, nint hMem);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsClipboardFormatAvailable(uint format);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint GlobalAlloc(uint uFlags, nuint dwBytes);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint GlobalLock(nint hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalUnlock(nint hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint GlobalFree(nint hMem);

    #endregion
}
