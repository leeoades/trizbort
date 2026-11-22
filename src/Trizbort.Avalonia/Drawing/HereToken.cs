using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;
using Svg.Skia;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Elements;
using AvaloniaColor = Avalonia.Media.Color;
using AvaloniaDrawingContext = Avalonia.Media.DrawingContext;
using Point = Avalonia.Point;
using Rect = Avalonia.Rect;

namespace Trizbort.Avalonia.Drawing;

/// <summary>
///   Where you are on the map: a token on the room automap has you in, like a playing piece on a board, seen from
///   above as the map is, with its shadow falling down and to the right. Lee, 22 Nov 2026: the dotted ring didn't stand
///   out beside the yellow selection; a wooden disc with a hurricane lantern burnt into it, a brass coin, a pawn, a "You are here" sign,
///   painted and pewter figures, or your own picture (a transparent PNG, or an SVG).
/// </summary>
internal static class HereToken
{
    public const string WoodenDisc = "wooden-disc";
    public const string BrassCoin = "brass-coin";
    public const string WoodenPawn = "wooden-pawn";
    public const string YouAreHere = "you-are-here";
    public const string Own = "own";

    /// <summary>
    ///   The tokens to choose from, in order, with their names: lying flat, then standing (painted, then pewter), then
    ///   your own. Each is Assets/Tokens/&lt;key&gt;.svg.
    /// </summary>
    public static readonly IReadOnlyList<(string Key, string Name)> Choices = new[]
    {
        (WoodenDisc, "Wooden disc"),
        (BrassCoin, "Brass coin"),
        (WoodenPawn, "Wooden pawn"),
        (YouAreHere, "You are here"),
        ("adventurer", "Adventurer"),
        ("detective", "Detective"),
        ("valkyrie", "Valkyrie"),
        ("pewter-adventurer", "Pewter adventurer"),
        ("pewter-detective", "Pewter detective"),
        ("pewter-valkyrie", "Pewter valkyrie"),
        ("pewter-wizard", "Pewter wizard"),
        (Own, "Your own")
    };

    /// <summary>Across, on the map at 100%.</summary>
    public const double Size = 46;

    /// <summary>Never smaller than this on screen, however far out you zoom, so you can always find it.</summary>
    public const double SmallestOnScreen = 30;

    private static readonly Dictionary<(string Source, int Pixels), Bitmap?> _images = new();

    /// <summary>The token chosen in the settings, or null when it isn't shown.</summary>
    public static string? Chosen
    {
        get
        {
            var settings = ApplicationSettingsController.AppSettings;
            if (!settings.HereTokenShown) return null;
            var key = string.IsNullOrWhiteSpace(settings.HereToken) ? WoodenDisc : settings.HereToken;
            if (key == Own) return string.IsNullOrWhiteSpace(settings.HereTokenFile) ? null : settings.HereTokenFile;
            return Choices.Any(choice => choice.Key == key) ? key : WoodenDisc;
        }
    }

    /// <summary>The glow round the room you're in, or null for none.</summary>
    public static AvaloniaColor? Glow
    {
        get
        {
            var settings = ApplicationSettingsController.AppSettings;
            if (!settings.HereGlow) return null;
            return AvaloniaColor.TryParse(settings.HereGlowColor, out var colour) ? colour : AvaloniaColor.Parse("#14B8A6");
        }
    }

    /// <summary>Across, on the map, at <paramref name="zoom" />.</summary>
    public static double SizeAt(double zoom) => Math.Max(Size, SmallestOnScreen / Math.Max(zoom, 0.01));

    /// <summary>Where the token sits for <paramref name="room" />: on its top right corner.</summary>
    public static Point PlaceOn(Room room, double zoom)
    {
        var bounds = room.InnerBounds;
        // a little in from the corner; grown bigger when zoomed out, it grows outward, off the room's name
        var grown = SizeAt(zoom) - Size;
        return new Point(bounds.Right - Size * 0.12 + grown * 0.5, bounds.Top + Size * 0.12 - grown * 0.5);
    }

    /// <summary>The square the token fills, centred on <paramref name="centre" />.</summary>
    public static Rect At(Point centre, double zoom)
    {
        var size = SizeAt(zoom);
        return new Rect(centre.X - size / 2, centre.Y - size / 2, size, size);
    }

    /// <summary>
    ///   Where <paramref name="token" /> is drawn for <paramref name="centre" />: a flat piece fills the square round
    ///   it; a standing one (taller than it's wide, like the wooden pawn) stands there, its base on that spot and
    ///   the rest of it above, a little bigger.
    /// </summary>
    public static Rect Area(string token, Point centre, double zoom)
    {
        var rect = At(centre, zoom);
        var image = Image(token, 128);
        if (image == null || !Standing(image.Size)) return Fit(image?.Size ?? rect.Size, rect);
        var height = rect.Height * 1.45;
        var width = height * image.Size.Width / image.Size.Height;
        var bottom = centre.Y + rect.Height * 0.22;
        return new Rect(centre.X - width / 2, bottom - height, width, height);
    }

    private static bool Standing(Size size) => size.Height > size.Width * 1.25;

    /// <summary>
    ///   Draws <paramref name="token" /> (a key from <see cref="Choices" />, or a file) centred on
    ///   <paramref name="centre" />, with its shadow; <paramref name="lifted" /> while you drag it, so it stands a
    ///   little off the map.
    /// </summary>
    public static void Draw(AvaloniaDrawingContext context, string token, Point centre, double zoom, double scaling,
        bool lifted = false)
    {
        var fitted = Area(token, centre, zoom);
        var rect = At(centre, zoom);
        var pixels = Pixels(Math.Max(fitted.Width, fitted.Height) * zoom * scaling);
        var image = Image(token, pixels);
        if (image == null) return;

        // the shadow: the token's own shape, dark, down and to the right, softened by drawing it a few times
        var drop = rect.Width * (lifted ? 0.16 : 0.08);
        for (var i = 0; i < 3; i++)
        {
            var spread = rect.Width * 0.025 * i;
            var at = new Rect(fitted.X + drop - spread / 2, fitted.Y + drop - spread / 2, fitted.Width + spread,
                fitted.Height + spread);
            using (context.PushOpacityMask(new ImageBrush(image) { Stretch = Stretch.Fill, DestinationRect = new RelativeRect(at, RelativeUnit.Absolute) }, at))
            {
                context.FillRectangle(new SolidColorBrush(Colors.Black, lifted ? 0.16 - i * 0.04 : 0.22 - i * 0.06), at);
            }
        }

        context.DrawImage(image, fitted);
    }

    /// <summary>The picture inside <paramref name="rect" />, its shape kept.</summary>
    private static Rect Fit(Size size, Rect rect)
    {
        if (size.Width <= 0 || size.Height <= 0) return rect;
        var scale = Math.Min(rect.Width / size.Width, rect.Height / size.Height);
        var width = size.Width * scale;
        var height = size.Height * scale;
        return new Rect(rect.Center.X - width / 2, rect.Center.Y - height / 2, width, height);
    }

    /// <summary>Drawn at a few sizes only, so zooming doesn't make a new picture every step.</summary>
    private static int Pixels(double onScreen)
    {
        var pixels = 32;
        while (pixels < onScreen && pixels < 1024) pixels *= 2;
        return pixels;
    }

    /// <summary>The token's picture, <paramref name="pixels" /> across (an SVG is drawn at that size), or null.</summary>
    internal static Bitmap? Image(string token, int pixels)
    {
        var isFile = Choices.All(choice => choice.Key != token);
        // a PNG is the same at every size; your own file is read again when it changes
        var source = isFile ? token + "|" + Stamp(token) : token;
        if (isFile && !token.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)) pixels = 0;
        if (_images.TryGetValue((source, pixels), out var cached)) return cached;

        Bitmap? image = null;
        try
        {
            if (!isFile)
            {
                using var stream = AssetLoader.Open(new Uri($"avares://Trizbort.Avalonia/Assets/Tokens/{token}.svg"));
                image = FromSvg(stream, pixels);
            }
            else if (File.Exists(token))
            {
                using var stream = File.OpenRead(token);
                image = pixels == 0 ? new Bitmap(stream) : FromSvg(stream, pixels);
            }
        }
        catch (Exception)
        {
            // a file that isn't a picture: no token, rather than no map
            image = null;
        }

        _images[(source, pixels)] = image;
        return image;
    }

    private static string Stamp(string file)
    {
        try
        {
            return File.Exists(file) ? File.GetLastWriteTimeUtc(file).Ticks.ToString() : "missing";
        }
        catch (Exception)
        {
            return "missing";
        }
    }

    /// <summary>An SVG drawn <paramref name="pixels" /> across (the longer side), its shape kept.</summary>
    internal static Bitmap? FromSvg(Stream stream, int pixels)
    {
        using var svg = new SKSvg();
        var picture = svg.Load(stream);
        if (picture == null) return null;
        var cull = picture.CullRect;
        if (cull.Width <= 0 || cull.Height <= 0) return null;
        var scale = pixels / Math.Max(cull.Width, cull.Height);
        var width = Math.Max(1, (int)Math.Ceiling(cull.Width * scale));
        var height = Math.Max(1, (int)Math.Ceiling(cull.Height * scale));
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.Scale(scale);
            canvas.Translate(-cull.Left, -cull.Top);
            canvas.DrawPicture(picture);
        }

        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var png = new MemoryStream(data.ToArray());
        return new Bitmap(png);
    }
}
