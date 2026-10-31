using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Trizbort.Avalonia.Tests;

/// <summary>Reads one pixel from a rendered bitmap as (B, G, R, A), whatever the platform's pixel byte order.</summary>
internal static class TestPixels
{
    /// <summary>
    ///   Skia renders BGRA on Windows and Linux but RGBA on macOS, so the bytes are swapped when the bitmap says it
    ///   is RGBA.
    /// </summary>
    public static (byte B, byte G, byte R, byte A) Read(Bitmap bitmap, int x, int y)
    {
        var buffer = new byte[4];
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            bitmap.CopyPixels(new PixelRect(x, y, 1, 1), handle.AddrOfPinnedObject(), buffer.Length, 4);
        }
        finally
        {
            handle.Free();
        }

        return IsRgba(bitmap)
            ? (buffer[2], buffer[1], buffer[0], buffer[3])
            : (buffer[0], buffer[1], buffer[2], buffer[3]);
    }

    private static bool IsRgba(Bitmap bitmap) => bitmap.Format is { } format && format == PixelFormats.Rgba8888;
}

/// <summary>Keyboard modifiers as the app sees them on the host OS.</summary>
internal static class TestKeys
{
    /// <summary>The app's "Primary" modifier: Cmd (Meta) on macOS, Ctrl elsewhere.</summary>
    public static RawInputModifiers Primary => OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control;
}
