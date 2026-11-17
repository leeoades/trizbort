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
///   inverted variants used on a dark canvas, as the reference does. "Select add" and "select remove" (an arrow with a
///   + or a -, while Shift or Ctrl is held) are bundled PNGs too, white with a black edge as the system arrow is (so
///   one each, light or dark canvas), with the arrow's tip as the hotspot. Everything else
///   is a standard cursor.
/// </summary>
internal static class CanvasCursors
{
    private static readonly PixelPoint _hotspot = new(15, 14);
    private static readonly PixelPoint _arrowTip = new(1, 1);
    private static readonly Dictionary<string, Cursor> _custom = new();

    /// <summary>The reference's rule (Drawing.IsDark): every channel below 128.</summary>
    public static bool IsDark(DrawingColor color) => Math.Max(color.R, Math.Max(color.G, color.B)) < 128;

    public static Cursor Get(CursorKind kind, bool darkCanvas)
    {
        var asset = kind switch
        {
            CursorKind.DrawLine => "DrawLine",
            CursorKind.MoveLine => "MoveLine",
            CursorKind.SelectAdd => "SelectAdd",
            CursorKind.SelectRemove => "SelectRemove",
            _ => null
        };
        if (asset == null) return new Cursor(InputMapper.ToCursor(kind));

        // the + and - arrows are white with a black edge, as the system arrow, so they show on any canvas
        if (darkCanvas && kind is not (CursorKind.SelectAdd or CursorKind.SelectRemove)) asset += "Inverted";
        lock (_custom)
        {
            if (_custom.TryGetValue(asset, out var cached)) return cached;

            Cursor cursor;
            try
            {
                using var stream = AssetLoader.Open(new Uri($"avares://Trizbort.Avalonia/Assets/Cursors/{asset}.png"));
                var hotspot = kind is CursorKind.SelectAdd or CursorKind.SelectRemove ? _arrowTip : _hotspot;
                cursor = new Cursor(new Bitmap(stream), hotspot);
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
