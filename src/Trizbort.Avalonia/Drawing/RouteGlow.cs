using Avalonia.Media;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Navigation;
using AvaloniaColor = Avalonia.Media.Color;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaRect = Avalonia.Rect;
using CoreLineSegment = Trizbort.Domain.Misc.LineSegment;

namespace Trizbort.Avalonia.Drawing;

/// <summary>
///     The directions' route, glowing blue on the map under the rooms and lines: a soft band along each join it
///     follows, and a halo round each room on the way, brightest round where it leads. Drawn while the directions
///     panel is open; as the player follows the route, the steps taken stop glowing.
/// </summary>
internal static class RouteGlow
{
    private static readonly AvaloniaColor _blue = AvaloniaColor.FromRgb(0x1E, 0x88, 0xE5);

    private static readonly IPen _bandOuter = Band(48, 18);
    private static readonly IPen _bandInner = Band(90, 9);
    private static readonly IBrush _haloFill = new SolidColorBrush(AvaloniaColor.FromArgb(34, _blue.R, _blue.G, _blue.B));
    private static readonly IPen _haloOuter = Band(44, 10);
    private static readonly IPen _haloInner = Band(80, 4);
    private static readonly IBrush _endFill = new SolidColorBrush(AvaloniaColor.FromArgb(60, _blue.R, _blue.G, _blue.B));
    private static readonly IPen _endInner = Band(150, 4);

    /// <summary>The route to draw (null: none). Set by the directions panel, then the map redrawn.</summary>
    public static IReadOnlyList<RouteStep>? Route { get; set; }

    private static IPen Band(byte alpha, double width) =>
        new Pen(new SolidColorBrush(AvaloniaColor.FromArgb(alpha, _blue.R, _blue.G, _blue.B)), width,
            lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);

    public static void Draw(DrawingContext context, int spatialVersion)
    {
        var route = Route;
        if (route == null || route.Count == 0) return;
        var elements = Project.Current.Elements;

        foreach (var step in route)
        {
            var join = step.Join;
            // a split join is drawn as two stubs: a band across the map between them would mislead
            if (join == null || join.IsDrawnSplit || !elements.Contains(join)) continue;
            var segments = join.HasCurveWaypoints
                ? join.GetSegments()
                : ConnectionDrawingAdapter.GetCachedSmartSegments(join, spatialVersion);
            var geometry = Polyline(segments);
            if (geometry == null) continue;
            context.DrawGeometry(null, _bandOuter, geometry);
            context.DrawGeometry(null, _bandInner, geometry);
        }

        Halo(context, route[0].From, false, elements);
        for (var index = 0; index < route.Count; index++)
            Halo(context, route[index].To, index == route.Count - 1, elements);
    }

    private static void Halo(DrawingContext context, Room room, bool end, IEnumerable<Element> elements)
    {
        if (room == null || !elements.Contains(room)) return;
        var bounds = room.InnerBounds;
        var rect = new AvaloniaRect(bounds.Left - 8, bounds.Top - 8, bounds.Width + 16, bounds.Height + 16);
        var geometry = new RectangleGeometry(rect, 12, 12);
        context.DrawGeometry(end ? _endFill : _haloFill, _haloOuter, geometry);
        context.DrawGeometry(null, end ? _endInner : _haloInner, geometry);
    }

    private static Geometry? Polyline(IReadOnlyList<CoreLineSegment>? segments)
    {
        if (segments == null || segments.Count == 0) return null;
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(new AvaloniaPoint(segments[0].Start.X, segments[0].Start.Y), false);
            foreach (var segment in segments) g.LineTo(new AvaloniaPoint(segment.End.X, segment.End.Y));
            g.EndFigure(false);
        }

        return geometry;
    }
}
