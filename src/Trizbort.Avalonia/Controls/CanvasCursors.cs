using Avalonia;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Trizbort.Editor;
using DrawingColor = System.Drawing.Color;

namespace Trizbort.Avalonia.Controls;

/// <summary>
///   Canvas cursors. The reference's custom "draw line" (cross with a pencil) and "move line" (four-way arrow)
///   cursors are bundled as PNGs converted from legacy/src/Trizbort/Images/*.cur (32x32, hotspot 15,14), with the
///   inverted variants used on a dark canvas, as the reference does. Everything else is a standard cursor.
/// </summary>
internal static class CanvasCursors
{
    private static readonly PixelPoint _hotspot = new(15, 14);
    private static readonly Dictionary<string, Cursor> _custom = new();

    /// <summary>The reference's rule (Drawing.IsDark): every channel below 128.</summary>
    public static bool IsDark(DrawingColor color) => Math.Max(color.R, Math.Max(color.G, color.B)) < 128;

    public static Cursor Get(CursorKind kind, bool darkCanvas)
    {
        var asset = kind switch
        {
            CursorKind.DrawLine => "DrawLine",
            CursorKind.MoveLine => "MoveLine",
            _ => null
        };
        if (asset == null) return new Cursor(InputMapper.ToCursor(kind));

        if (darkCanvas) asset += "Inverted";
        lock (_custom)
        {
            if (_custom.TryGetValue(asset, out var cached)) return cached;

            Cursor cursor;
            try
            {
                using var stream = AssetLoader.Open(new Uri($"avares://Trizbort.Avalonia/Assets/Cursors/{asset}.png"));
                cursor = new Cursor(new Bitmap(stream), _hotspot);
            }
            catch (Exception)
            {
                // Platforms without custom cursor support (e.g. the headless test platform) get the standard shape.
                cursor = new Cursor(InputMapper.ToCursor(kind));
            }

            _custom[asset] = cursor;
            return cursor;
        }
    }
}
