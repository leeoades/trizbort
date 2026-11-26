using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Setup;
using CoreRect = Trizbort.Domain.Misc.Rect;
using CoreVector = Trizbort.Domain.Misc.Vector;
using CoreColor = System.Drawing.Color;
using CoreColors = Trizbort.Domain.Misc.Colors;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaDrawingContext = Avalonia.Media.DrawingContext;

namespace Trizbort.Avalonia.Drawing;

public static class MapLabelDrawingAdapter
{
    private class LabelCache
    {
        public TextAlignment LastAcross { get; set; }
        public double LastDown { get; set; }
        public CoreRect LastBounds;
        public RoomShape LastShape;
        public bool LastHandDrawn;
        public StreamGeometry? CachedGeometry;
        public StreamGeometry? CachedHaloGeometry;
        public CoreRect LastHaloBounds;
        public RoomShape LastHaloShape;
        public bool LastHaloHandDrawn;

        public string? LastText;
        public string? LastFontName;
        public int LastFontStyle;
        public float LastFontSize;
        public uint LastTextColor;
        public CoreRect LastTextBounds;
        public FormattedText? CachedText;
        public double CachedStartY;

        /// <summary>A Markdown label laid out, and what it was laid out for.</summary>
        public MarkdownLabelRenderer? Markdown;
        public string? MarkdownKey;
    }

    /// <summary>The label's font, from its style (Lee, 24 Nov 2026): before styles, always the room font.</summary>
    /// <summary>
    ///   Where the text goes in a label of this look: its alignment across, and how far down the spare height it sits
    ///   (0 top, 0.5 middle, 1 bottom). Usual: centred for plain text, top left for Markdown. Lee, 25 Nov 2026.
    /// </summary>
    public static (TextAlignment Across, double Down) PlaceOf(ResolvedLabelStyle look, bool markdown)
    {
        var across = look.Align switch
        {
            LabelAlign.Left => TextAlignment.Left,
            LabelAlign.Centre => TextAlignment.Center,
            LabelAlign.Right => TextAlignment.Right,
            _ => markdown ? TextAlignment.Left : TextAlignment.Center
        };
        var down = look.VerticalAlign switch
        {
            LabelVerticalAlign.Top => 0,
            LabelVerticalAlign.Middle => 0.5,
            LabelVerticalAlign.Bottom => 1,
            _ => markdown ? 0 : 0.5
        };
        return (across, down);
    }

    public static ThemeFont FontOf(MapLabel label)
    {
        var look = label.Look;
        return new ThemeFont { Name = look.FontName, Size = look.FontSize, Style = look.FontStyle };
    }

    /// <summary>The link drawn at <paramref name="point" /> (map coordinates) in a Markdown label, or null.</summary>
    public static string? LinkAt(MapLabel label, global::Avalonia.Point point) =>
        label.Markdown && _labelCaches.TryGetValue(label, out var cache) ? cache.Markdown?.LinkAt(point) : null;

    /// <summary>The colour of links in a Markdown label.</summary>
    private static readonly IBrush _linkBrush = new SolidColorBrush(global::Avalonia.Media.Color.FromRgb(0x1A, 0x5F, 0xB4));

    private static readonly ConditionalWeakTable<MapLabel, LabelCache> _labelCaches = new();

    public static void DrawMapLabel(this MapLabel label, AvaloniaDrawingContext context, bool isSelected, bool isHover)
    {
        var bounds = label.InnerBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var handDrawn = Settings.HandDrawn;
        var seed = label.Id;
        var cache = _labelCaches.GetOrCreateValue(label);

        // Build Geometry for Shape
        if (cache.CachedGeometry == null
            || cache.LastShape != label.Shape
            || cache.LastHandDrawn != handDrawn
            || !cache.LastBounds.Equals(bounds))
        {
            cache.CachedGeometry = label.Shape switch
            {
                RoomShape.RoundedCorners => DrawingAdapters.CreateRoundedRectangleGeometry(bounds, new CornerRadii(), handDrawn, seed),
                RoomShape.Ellipse => DrawingAdapters.CreateEllipseGeometry(bounds, handDrawn, seed),
                RoomShape.Octagonal => DrawingAdapters.CreateOctagonalGeometry(bounds, new CornerRadii(), handDrawn, seed),
                _ => DrawingAdapters.CreateSquareGeometry(bounds, handDrawn, seed)
            };
            cache.LastShape = label.Shape;
            cache.LastHandDrawn = handDrawn;
            cache.LastBounds = bounds;
        }

        var geometry = cache.CachedGeometry;

        // Selected: the same yellow halo behind it as a room, so its own border still shows (Lee, 26 Nov 2026)
        if (isSelected)
        {
            var haloBounds = bounds;
            var isPrimary = Project.Current?.ActiveSelectedElement?.Id == label.Id;
            haloBounds.Inflate(isPrimary ? 10 : 5);
            if (cache.CachedHaloGeometry == null || !cache.LastHaloBounds.Equals(haloBounds) || cache.LastHaloShape != label.Shape
                || cache.LastHaloHandDrawn != handDrawn)
            {
                cache.CachedHaloGeometry = label.Shape switch
                {
                    RoomShape.RoundedCorners => DrawingAdapters.CreateRoundedRectangleGeometry(haloBounds, new CornerRadii(), handDrawn, seed),
                    RoomShape.Ellipse => DrawingAdapters.CreateEllipseGeometry(haloBounds, handDrawn, seed),
                    RoomShape.Octagonal => DrawingAdapters.CreateOctagonalGeometry(haloBounds, new CornerRadii(), handDrawn, seed),
                    _ => DrawingAdapters.CreateSquareGeometry(haloBounds, handDrawn, seed)
                };
                cache.LastHaloBounds = haloBounds;
                cache.LastHaloShape = label.Shape;
                cache.LastHaloHandDrawn = handDrawn;
            }

            context.DrawGeometry(RoomDrawingAdapter._selHaloBrush, null, cache.CachedHaloGeometry);
        }

        // Background
        IBrush? fillBrush = label.HasBackground ? label.BackgroundColor.ToBrush() : null;

        // Border
        IPen? borderPen = null;
        if (label.BorderStyle != BorderDashStyle.None)
        {
            var borderColor = label.BorderColor != CoreColor.Transparent ? label.BorderColor : Settings.Color[CoreColors.Border];
            var dashStyle = label.BorderStyle.ToDashStyle();
            borderPen = borderColor.ToPen(Settings.LineWidth, dashStyle);
        }

        if (fillBrush != null || borderPen != null)
        {
            context.DrawGeometry(fillBrush, borderPen, geometry);
        }

        // Hover (selection is the halo behind)
        if (isHover && !isSelected)
        {
            var hoverPen = Settings.Color[CoreColors.HoverLine].ToPen(Settings.LineWidth + 1.0);
            context.DrawGeometry(null, hoverPen, geometry);
        }

        // Text
        if (!string.IsNullOrEmpty(label.Text))
        {
            var textBounds = bounds;
            if (label.Shape == RoomShape.Ellipse)
                textBounds.Inflate(-11.5f, -11.5f);
            else
                textBounds.Inflate(-5, -5);

            if (label.Markdown && textBounds.Width > 0 && textBounds.Height > 0)
            {
                DrawMarkdown(label, context, cache, textBounds);
            }
            else if (textBounds.Width > 0 && textBounds.Height > 0)
            {
                var font = FontOf(label);
                var textColor = label.TextColor != CoreColor.Transparent
                    ? label.TextColor
                    : Settings.Color[CoreColors.SmallText];

                var (across, down) = PlaceOf(label.Look, false);
                var textValid = cache.LastText == label.Text
                                && cache.LastAcross == across && Math.Abs(cache.LastDown - down) < 0.001
                                && cache.LastFontName == font.Name
                                && cache.LastFontStyle == (int)font.Style
                                && Math.Abs(cache.LastFontSize - font.Size) < 0.01f
                                && cache.LastTextColor == (uint)textColor.ToArgb()
                                && cache.LastTextBounds.Equals(textBounds);

                if (!textValid || cache.CachedText == null)
                {
                    var typeface = font.ToTypeface();
                    var textBrush = textColor.ToBrush();

                    var formattedText = new FormattedText(
                        label.Text,
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        font.Size,
                        textBrush)
                    {
                        TextAlignment = across,
                        MaxTextWidth = textBounds.Width,
                        MaxTextHeight = textBounds.Height
                    };

                    cache.LastText = label.Text;
                    cache.LastFontName = font.Name;
                    cache.LastFontStyle = (int)font.Style;
                    cache.LastFontSize = font.Size;
                    cache.LastTextColor = (uint)textColor.ToArgb();
                    cache.LastTextBounds = textBounds;
                    cache.CachedText = formattedText;
                    cache.LastAcross = across;
                    cache.LastDown = down;
                    cache.CachedStartY = textBounds.Top + Math.Max(0, (textBounds.Height - formattedText.Height) * down);
                }

                if (TextRenderingGate.IsEnabled) context.DrawText(cache.CachedText, new AvaloniaPoint(textBounds.Left, cache.CachedStartY));
            }
        }
    }

    /// <summary>The widest a label grows to fit its text; past that, it wraps.</summary>
    public const float WidestFitted = 640;

    /// <summary>
    ///   The size <paramref name="label" />'s text needs, never smaller than <paramref name="atLeast" />. Lee, 22 Nov
    ///   2026: a label grows as its text does. Markdown: as tall as its text, wide enough for its tables (paragraphs
    ///   wrap at the label's width). Plain text, centred: wide enough for its longest line, as tall as its lines.
    ///   Either way no wider than <see cref="WidestFitted" />, past which the text wraps.
    /// </summary>
    /// <param name="keepWidth">
    ///   Only taller if need be: the text keeps the lines it's wrapped into (a new style, Lee, 25 Nov 2026), rather than
    ///   the label widening to put it on one line.
    /// </param>
    public static CoreVector FitText(MapLabel label, CoreVector atLeast, bool keepWidth = false)
    {
        if (string.IsNullOrEmpty(label.Text)) return atLeast;
        var font = FontOf(label);
        // the text's inset, as drawn: more for an ellipse
        var inset = label.Shape == RoomShape.Ellipse ? 11.5f : 5f;
        var width = atLeast.X;
        double natural;
        Func<float, double> heightAt;

        if (label.Markdown)
        {
            var rule = Settings.Color[CoreColors.Border].ToPen(1);
            MarkdownLabelRenderer Lay(float across) => MarkdownLabelRenderer.Lay(label.Text,
                new global::Avalonia.Rect(0, 0, Math.Max(1, across - inset * 2), 100000), font.ToTypeface(), font.Size,
                Brushes.Black, _linkBrush, rule);
            natural = Lay(width).NaturalWidth;
            heightAt = across => Lay(across).Height;
        }
        else
        {
            FormattedText Lay(double across) => new(label.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                font.ToTypeface(), font.Size, Brushes.Black) { TextAlignment = TextAlignment.Center, MaxTextWidth = across };
            natural = Lay(double.PositiveInfinity).WidthIncludingTrailingWhitespace;
            heightAt = across => Lay(Math.Max(1, across - inset * 2)).Height;
        }

        if (!keepWidth && natural + inset * 2 > width && width < WidestFitted)
            width = (float)Math.Min(WidestFitted, Math.Ceiling(natural + inset * 2));
        var height = (float)Math.Ceiling(heightAt(width) + inset * 2);
        return new CoreVector(width, Math.Max(atLeast.Y, height));
    }

    /// <summary>
    ///     A Markdown label: laid out left-aligned from the top of the label (it grows to fit as you type: FitText), and drawn; laid out again only when the text, size, font or colour change.
    /// </summary>
    private static void DrawMarkdown(MapLabel label, AvaloniaDrawingContext context, LabelCache cache, CoreRect textBounds)
    {
        var font = FontOf(label);
        var textColor = label.TextColor != CoreColor.Transparent ? label.TextColor : Settings.Color[CoreColors.SmallText];
        var (across, down) = PlaceOf(label.Look, true);
        var key = string.Join("\u0001", label.Text, font.Name, (int)font.Style, font.Size, textColor.ToArgb(),
            textBounds.Left, textBounds.Top, textBounds.Width, textBounds.Height, across, down);
        if (cache.Markdown == null || cache.MarkdownKey != key)
        {
            var rule = Settings.Color[CoreColors.Border];
            var area = new global::Avalonia.Rect(textBounds.Left, textBounds.Top, textBounds.Width, textBounds.Height);
            MarkdownLabelRenderer Lay(global::Avalonia.Rect where) => MarkdownLabelRenderer.Lay(label.Text, where,
                font.ToTypeface(), font.Size, textColor.ToBrush(), _linkBrush, rule.ToPen(1), across);
            cache.Markdown = Lay(area);
            // lower down: laid out again where it goes, so its links are where they're drawn
            var spare = textBounds.Height - cache.Markdown.Height;
            if (down > 0 && spare > 0) cache.Markdown = Lay(area.Translate(new global::Avalonia.Vector(0, spare * down)));
            cache.MarkdownKey = key;
        }

        cache.Markdown.Draw(context);
    }
}
