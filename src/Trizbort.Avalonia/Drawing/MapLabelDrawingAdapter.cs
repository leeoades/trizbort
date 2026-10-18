using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using CoreRect = Trizbort.Domain.Misc.Rect;
using CoreVector = Trizbort.Domain.Misc.Vector;
using CoreColor = System.Drawing.Color;
using CoreColors = Trizbort.Domain.Misc.Colors;
using AvaloniaRect = Avalonia.Rect;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaColor = Avalonia.Media.Color;
using AvaloniaDrawingContext = Avalonia.Media.DrawingContext;

namespace Trizbort.Avalonia.Drawing;

public static class MapLabelDrawingAdapter
{
    private class LabelCache
    {
        public CoreRect LastBounds;
        public RoomShape LastShape;
        public bool LastHandDrawn;
        public StreamGeometry? CachedGeometry;

        public string? LastText;
        public string? LastFontName;
        public int LastFontStyle;
        public float LastFontSize;
        public uint LastTextColor;
        public CoreRect LastTextBounds;
        public FormattedText? CachedText;
        public double CachedStartY;
    }

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

        // Selection / Hover
        if (isSelected)
        {
            var selPen = Settings.Color[CoreColors.SelectedLine].ToPen(Settings.LineWidth + 1.5);
            context.DrawGeometry(null, selPen, geometry);
        }
        else if (isHover)
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

            if (textBounds.Width > 0 && textBounds.Height > 0)
            {
                var font = Settings.RoomFontMetadata;
                var textColor = label.TextColor != CoreColor.Transparent
                    ? label.TextColor
                    : Settings.Color[CoreColors.SmallText];

                var textValid = cache.LastText == label.Text
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
                        TextAlignment = TextAlignment.Center,
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
                    cache.CachedStartY = textBounds.Top + Math.Max(0, (textBounds.Height - formattedText.Height) / 2.0);
                }

                context.DrawText(cache.CachedText, new AvaloniaPoint(textBounds.Left, cache.CachedStartY));
            }
        }
    }
}
