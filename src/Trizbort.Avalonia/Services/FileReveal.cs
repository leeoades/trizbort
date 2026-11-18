using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Trizbort.Avalonia.Services;

/// <summary>Shows a file in its folder: selected in Explorer or the Finder, or its folder opened elsewhere.</summary>
public static class FileReveal
{
    /// <summary>What to run to show <paramref name="path" /> on this system; null for no path.</summary>
    public static ProcessStartInfo? For(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var full = Path.GetFullPath(path);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new ProcessStartInfo("explorer.exe", $"/select,\"{full}\"") { UseShellExecute = false };
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var mac = new ProcessStartInfo("open") { UseShellExecute = false };
            mac.ArgumentList.Add("-R");
            mac.ArgumentList.Add(full);
            return mac;
        }

        var folder = Path.GetDirectoryName(full) ?? full;
        var other = new ProcessStartInfo("xdg-open") { UseShellExecute = false };
        other.ArgumentList.Add(folder);
        return other;
    }

    public static void Reveal(string? path)
    {
        var start = For(path);
        if (start == null) return;
        try
        {
            using var _ = Process.Start(start);
        }
        catch (Exception exception)
        {
            Trace.WriteLine("Couldn't show " + path + ": " + exception.Message);
        }
    }
}
