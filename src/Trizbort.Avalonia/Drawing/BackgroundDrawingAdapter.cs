using System.Runtime.CompilerServices;
using Avalonia.Media;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Setup;
using CoreRect = Trizbort.Domain.Misc.Rect;
using CoreColor = System.Drawing.Color;
using CoreColors = Trizbort.Domain.Misc.Colors;
using AvaloniaDrawingContext = Avalonia.Media.DrawingContext;

namespace Trizbort.Avalonia.Drawing;

/// <summary>Draws a <see cref="Background" />: its shape filled, with its border if it has one. Lee, 24 Nov 2026.</summary>
public static class BackgroundDrawingAdapter
{
    private sealed class Cache
    {
        public CoreRect Bounds;
        public RoomShape Shape;
        public bool HandDrawn;
        public StreamGeometry? Geometry;
    }

    private static readonly ConditionalWeakTable<Background, Cache> _caches = new();

    /// <summary>The background's outline, as drawn (cached until its size, shape or hand-drawn style changes).</summary>
    public static StreamGeometry? Geometry(Background background)
    {
        var bounds = background.InnerBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return null;

        var handDrawn = background.IsHandDrawn;
        var cache = _caches.GetOrCreateValue(background);
        if (cache.Geometry == null || cache.Shape != background.Shape || cache.HandDrawn != handDrawn ||
            !cache.Bounds.Equals(bounds))
        {
            var seed = background.Id;
            cache.Geometry = background.Shape switch
            {
                RoomShape.RoundedCorners => DrawingAdapters.CreateRoundedRectangleGeometry(bounds, new CornerRadii(), handDrawn, seed),
                RoomShape.Ellipse => DrawingAdapters.CreateEllipseGeometry(bounds, handDrawn, seed),
                RoomShape.Octagonal => DrawingAdapters.CreateOctagonalGeometry(bounds, new CornerRadii(), handDrawn, seed),
                _ => DrawingAdapters.CreateSquareGeometry(bounds, handDrawn, seed)
            };
            (cache.Shape, cache.HandDrawn, cache.Bounds) = (background.Shape, handDrawn, bounds);
        }

        return cache.Geometry;
    }

    public static void DrawBackground(this Background background, AvaloniaDrawingContext context, bool isSelected, bool isHover)
    {
        var geometry = Geometry(background);
        if (geometry == null) return;

        IPen? borderPen = null;
        if (background.BorderStyle != BorderDashStyle.None)
        {
            var color = background.BorderColor != CoreColor.Transparent ? background.BorderColor : Settings.Color[CoreColors.Border];
            borderPen = color.ToPen(Settings.LineWidth, background.BorderStyle.ToDashStyle());
        }

        context.DrawGeometry(background.FillColor.ToBrush(), borderPen, geometry);

        if (isSelected)
        {
            context.DrawGeometry(null, Settings.Color[CoreColors.SelectedLine].ToPen(Settings.LineWidth + 1.5), geometry);
        }
        else if (isHover)
        {
            context.DrawGeometry(null, Settings.Color[CoreColors.HoverLine].ToPen(Settings.LineWidth + 1.0), geometry);
        }
    }
}
