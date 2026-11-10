using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using CoreRect = Trizbort.Domain.Misc.Rect;
using CoreVector = Trizbort.Domain.Misc.Vector;
using CoreColor = System.Drawing.Color;
using CoreColors = Trizbort.Domain.Misc.Colors;
using CoreLineSegment = Trizbort.Domain.Misc.LineSegment;
using AvaloniaRect = Avalonia.Rect;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaDrawingContext = Avalonia.Media.DrawingContext;

namespace Trizbort.Avalonia.Drawing;

public class SpatialLineIndex
{
    private const float CellSize = 128f;
    private readonly Dictionary<long, List<CoreLineSegment>> _cells = new();
    private readonly List<CoreLineSegment> _allLines = new();

    public int Count => _allLines.Count;
    public IReadOnlyList<CoreLineSegment> AllLines => _allLines;

    public void Clear()
    {
        _cells.Clear();
        _allLines.Clear();
    }

    public void Add(CoreLineSegment line)
    {
        _allLines.Add(line);
        var minX = (int)Math.Floor(Math.Min(line.Start.X, line.End.X) / CellSize);
        var maxX = (int)Math.Floor(Math.Max(line.Start.X, line.End.X) / CellSize);
        var minY = (int)Math.Floor(Math.Min(line.Start.Y, line.End.Y) / CellSize);
        var maxY = (int)Math.Floor(Math.Max(line.Start.Y, line.End.Y) / CellSize);

        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                var key = ((long)x << 32) | (uint)y;
                if (!_cells.TryGetValue(key, out var list))
                {
                    list = new List<CoreLineSegment>();
                    _cells[key] = list;
                }
                list.Add(line);
            }
        }
    }

    public void Query(float minX, float maxX, float minY, float maxY, List<CoreLineSegment> results)
    {
        var startCellX = (int)Math.Floor(minX / CellSize);
        var endCellX = (int)Math.Floor(maxX / CellSize);
        var startCellY = (int)Math.Floor(minY / CellSize);
        var endCellY = (int)Math.Floor(maxY / CellSize);

        for (var x = startCellX; x <= endCellX; x++)
        {
            for (var y = startCellY; y <= endCellY; y++)
            {
                var key = ((long)x << 32) | (uint)y;
                if (_cells.TryGetValue(key, out var list))
                {
                    for (var i = 0; i < list.Count; i++)
                    {
                        var item = list[i];
                        if (!results.Contains(item))
                        {
                            results.Add(item);
                        }
                    }
                }
            }
        }
    }
}

public static class ConnectionDrawingAdapter
{
    private class ConnectionCache
    {
        public List<CoreLineSegment> SmartSegments = new();
        public float LastLineWidth;
        public int VerticesHash;
        public int LinesDrawnVersion;

        public string? LastStartText;
        public string? LastMidText;
        public string? LastEndText;
        public FormattedText? CachedStartText;
        public FormattedText? CachedMidText;
        public FormattedText? CachedEndText;
        public float LastAnnotationFontSize;
        public string? LastAnnotationFontName;
        public int LastAnnotationFontStyle;
        public uint LastAnnotationColor;
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Connection, ConnectionCache> _connectionCaches = new();

    private static int ComputeSegmentsHash(List<CoreLineSegment> segments)
    {
        var hash = new HashCode();
        foreach (var s in segments)
        {
            hash.Add(s.Start.X);
            hash.Add(s.Start.Y);
            hash.Add(s.End.X);
            hash.Add(s.End.Y);
        }
        return hash.ToHashCode();
    }

    public static void RebuildSpatialIndex(
        IEnumerable<Element> elements,
        SpatialLineIndex spatialIndex,
        int spatialVersion)
    {
        spatialIndex.Clear();
        var elementList = elements.ToList();

        // Add connection segments in Depth order. This mirrors the stable WinForms
        // smart-line cache: room borders are not split targets.
        var connections = elementList.OfType<Connection>().OrderBy(c => c.Depth).ToList();
        foreach (var conn in connections)
        {
            var segments = conn.GetSegments();
            var cache = _connectionCaches.GetOrCreateValue(conn);
            cache.SmartSegments.Clear();

            if (segments != null && segments.Count > 0)
            {
                var segsHash = ComputeSegmentsHash(segments);
                if (!conn.HasCurveWaypoints || conn.IsDrawnSplit)
                {
                    foreach (var seg in segments)
                    {
                        var split = SplitSegment(seg, spatialIndex);
                        cache.SmartSegments.AddRange(split);
                        foreach (var s in split)
                        {
                            spatialIndex.Add(new CoreLineSegment(s.Start, s.End));
                        }
                    }
                }
                else
                {
                    cache.SmartSegments.AddRange(segments);
                }

                cache.VerticesHash = segsHash;
                cache.LastLineWidth = (float)Settings.LineWidth;
                cache.LinesDrawnVersion = spatialVersion;
            }
        }
    }

    internal static IReadOnlyList<CoreLineSegment> GetCachedSmartSegments(Connection connection, int spatialVersion)
    {
        var cache = _connectionCaches.GetOrCreateValue(connection);
        if (cache.SmartSegments.Count > 0 && cache.LinesDrawnVersion == spatialVersion)
        {
            return cache.SmartSegments;
        }
        return connection.GetSegments() ?? new List<CoreLineSegment>();
    }

    /// <summary>
    ///   The selection / hover highlight pen. It is wider than the line, so a dashed connection gets the same dash
    ///   pattern in absolute length (Avalonia dashes are multiples of the pen thickness); a solid pen would hide the gaps.
    /// </summary>
    internal static IPen HighlightPen(int colorKey, double extraWidth, ConnectionStyle style)
    {
        var width = Settings.LineWidth + extraWidth;
        var dash = style.ToDashStyle();
        if (dash == null || dash.Dashes == null || dash.Dashes.Count == 0)
        {
            return Settings.Color[colorKey].ToPen(width);
        }

        var ratio = Math.Max(0.5, Settings.LineWidth) / Math.Max(0.5, width);
        var scaled = new List<double>(dash.Dashes.Count);
        foreach (var d in dash.Dashes)
        {
            scaled.Add(d * ratio);
        }
        return Settings.Color[colorKey].ToPen(width, new DashStyle(scaled, dash.Offset));
    }

    public static void DrawConnection(
        this Connection connection,
        AvaloniaDrawingContext context,
        bool isSelected,
        bool isHover,
        SpatialLineIndex? spatialIndex,
        int spatialVersion = 0)
    {
        var color = connection.ConnectionColor != CoreColor.Transparent
            ? connection.ConnectionColor
            : Settings.Color[CoreColors.Line];
        var dashStyle = connection.Style.ToDashStyle();
        var pen = color.ToPen(Settings.LineWidth, dashStyle);
        var brush = color.ToBrush();
        var handDrawn = Settings.HandDrawn;
        var seed = connection.Id;
        var cache = _connectionCaches.GetOrCreateValue(connection);
        // (a split join is two straight stubs, whatever its curve)
        var curved = connection.HasCurveWaypoints && !connection.IsDrawnSplit;

        // a way out that leads straight back in: a small curl on the room's side, arrow at its head
        if (!curved && TryDrawLoop(connection, context, pen, brush, isSelected, isHover, cache)) return;
        var chevrons = new List<(CoreVector position, CoreVector direction)>();
        var sketched = new List<IReadOnlyList<CoreVector>>();

        // 1. Draw Connection Line / Path
        var activeSegments = new List<CoreLineSegment>();
        if (curved)
        {
            var segments = connection.GetSegments();
            if (segments != null && segments.Count > 0)
            {
                activeSegments.AddRange(segments);
                var points = new List<CoreVector>(segments.Count + 1);
                foreach (var seg in segments)
                {
                    points.Add(seg.Start);
                }
                points.Add(segments[^1].End);

                if (handDrawn)
                {
                    var polyline = Sketch.Polyline(points, Sketch.Seeded(seed));
                    sketched.Add(polyline);
                    var curveGeo = DrawingAdapters.ToPolylineGeometry(polyline);
                    context.DrawGeometry(null, pen, curveGeo);

                    if (isSelected)
                    {
                        var selPen = HighlightPen(CoreColors.SelectedLine, 1.5, connection.Style);
                        context.DrawGeometry(null, selPen, curveGeo);
                    }
                    else if (isHover)
                    {
                        var hoverPen = HighlightPen(CoreColors.HoverLine, 1.0, connection.Style);
                        context.DrawGeometry(null, hoverPen, curveGeo);
                    }
                }
                else
                {
                    var curveGeo = DrawingAdapters.ToPolylineGeometry(points);
                    context.DrawGeometry(null, pen, curveGeo);

                    if (isSelected)
                    {
                        var selPen = HighlightPen(CoreColors.SelectedLine, 1.5, connection.Style);
                        context.DrawGeometry(null, selPen, curveGeo);
                    }
                    else if (isHover)
                    {
                        var hoverPen = HighlightPen(CoreColors.HoverLine, 1.0, connection.Style);
                        context.DrawGeometry(null, hoverPen, curveGeo);
                    }
                }
            }

            if (connection.Flow == ConnectionFlow.OneWay)
            {
                // one arrow per curve span, rather than one per flattened line segment
                connection.GetCurvedSegments(out var spans);
                foreach (var span in spans)
                {
                    var mid = CurveGeometry.PolylineMidpoint(span, out var direction);
                    if (direction != CoreVector.Zero)
                    {
                        chevrons.Add((mid, direction));
                    }
                }
            }
        }
        else
        {
            // Retrieve cached smart line segments if valid
            if (cache.SmartSegments.Count > 0 && cache.LinesDrawnVersion == spatialVersion)
            {
                activeSegments.AddRange(cache.SmartSegments);
            }
            else
            {
                var segments = connection.GetSegments();
                if (segments != null && segments.Count > 0)
                {
                    if (spatialIndex != null && spatialIndex.Count > 0)
                    {
                        var segsHash = ComputeSegmentsHash(segments);
                        if (cache.SmartSegments.Count > 0
                            && cache.VerticesHash == segsHash
                            && Math.Abs(cache.LastLineWidth - Settings.LineWidth) < 0.01f
                            && cache.LinesDrawnVersion == spatialVersion)
                        {
                            activeSegments.AddRange(cache.SmartSegments);
                        }
                        else
                        {
                            foreach (var seg in segments)
                            {
                                var splitSegs = SplitSegment(seg, spatialIndex);
                                activeSegments.AddRange(splitSegs);
                            }
                            cache.SmartSegments.Clear();
                            cache.SmartSegments.AddRange(activeSegments);
                            cache.VerticesHash = segsHash;
                            cache.LastLineWidth = (float)Settings.LineWidth;
                            cache.LinesDrawnVersion = spatialVersion;
                        }
                    }
                    else
                    {
                        activeSegments.AddRange(segments);
                    }
                }
            }

            foreach (var seg in activeSegments)
            {
                if (handDrawn)
                {
                    var stroke = Sketch.Line(seg.Start, seg.End, Sketch.Seeded(seed));
                    sketched.Add(stroke);
                    var lineGeo = DrawingAdapters.ToPolylineGeometry(stroke);
                    context.DrawGeometry(null, pen, lineGeo);

                    if (isSelected)
                    {
                        var selPen = HighlightPen(CoreColors.SelectedLine, 1.5, connection.Style);
                        context.DrawGeometry(null, selPen, lineGeo);
                    }
                    else if (isHover)
                    {
                        var hoverPen = HighlightPen(CoreColors.HoverLine, 1.0, connection.Style);
                        context.DrawGeometry(null, hoverPen, lineGeo);
                    }
                }
                else
                {
                    var p1 = seg.Start.ToAvaloniaPoint();
                    var p2 = seg.End.ToAvaloniaPoint();
                    context.DrawLine(pen, p1, p2);

                    if (isSelected)
                    {
                        var selPen = HighlightPen(CoreColors.SelectedLine, 1.5, connection.Style);
                        context.DrawLine(selPen, p1, p2);
                    }
                    else if (isHover)
                    {
                        var hoverPen = HighlightPen(CoreColors.HoverLine, 1.0, connection.Style);
                        context.DrawLine(hoverPen, p1, p2);
                    }
                }

                var delta = seg.Delta;
                if (connection.Flow == ConnectionFlow.OneWay && delta.Length > Settings.ConnectionArrowSize)
                {
                    chevrons.Add((seg.Mid, delta));
                }
            }
        }

        // 2. Draw Directional Chevrons (One-Way Flow)
        if (chevrons.Count > 0)
        {
            var arrowSize = (double)Settings.ConnectionArrowSize;
            if (handDrawn && sketched.Count > 0)
            {
                foreach (var (position, direction) in chevrons)
                {
                    var target = position;
                    var bestPoint = target;
                    var arrowDirection = direction;
                    var bestDistance = double.MaxValue;

                    foreach (var stroke in sketched)
                    {
                        var point = Sketch.Nearest(stroke, target, out var strokeDirection);
                        var distance = (point.X - target.X) * (point.X - target.X) + (point.Y - target.Y) * (point.Y - target.Y);
                        if (distance >= bestDistance) continue;
                        bestDistance = distance;
                        bestPoint = point;

                        var sign = strokeDirection.X * direction.X + strokeDirection.Y * direction.Y >= 0 ? 1 : -1;
                        arrowDirection = new CoreVector(strokeDirection.X * sign, strokeDirection.Y * sign);
                    }

                    if (arrowDirection == CoreVector.Zero) arrowDirection = direction;
                    var angleRad = Math.Atan2(arrowDirection.Y, arrowDirection.X);
                    var angleDeg = angleRad * 180.0 / Math.PI;
                    DrawingAdapters.DrawChevron(context, bestPoint.ToAvaloniaPoint(), angleDeg, arrowSize, brush, seed + 5000, true);
                }
            }
            else
            {
                foreach (var (position, direction) in chevrons)
                {
                    var angleRad = Math.Atan2(direction.Y, direction.X);
                    var angleDeg = angleRad * 180.0 / Math.PI;
                    DrawingAdapters.DrawChevron(context, position.ToAvaloniaPoint(), angleDeg, arrowSize, brush, seed, false);
                }
            }
        }

        // 3. Draw Door / Lock Icons
        if (connection.Door != null && activeSegments.Count > 0)
        {
            var midSegment = activeSegments[activeSegments.Count / 2];
            DrawDoorIcons(context, connection.Door, midSegment);
        }

        // 4. Draw Annotations (StartText, MidText, EndText)
        var connectionCache = _connectionCaches.GetOrCreateValue(connection);
        DrawAnnotations(connection, context, activeSegments, connectionCache);
    }

    public static void DrawConnection(
        this Connection connection,
        AvaloniaDrawingContext context,
        bool isSelected,
        bool isHover,
        List<CoreLineSegment>? linesDrawn)
    {
        SpatialLineIndex? spatial = null;
        if (linesDrawn != null && linesDrawn.Count > 0)
        {
            spatial = new SpatialLineIndex();
            foreach (var line in linesDrawn)
            {
                spatial.Add(line);
            }
        }
        DrawConnection(connection, context, isSelected, isHover, spatial, 0);
    }

    /// <summary>
    ///   Draws a join from a room back to itself (two different sides of one room, no bends) as a small curl out of
    ///   the side it leaves from, with a single arrowhead where it comes back in. Returns false for anything else.
    /// </summary>
    private static bool TryDrawLoop(
        Connection connection,
        AvaloniaDrawingContext context,
        IPen pen,
        IBrush brush,
        bool isSelected,
        bool isHover,
        ConnectionCache cache)
    {
        if (connection.VertexList.Count != 2) return false;
        var start = connection.VertexList[0];
        var end = connection.VertexList[1];
        if (start.Port == null || end.Port == null || start.Port == end.Port) return false;
        if (start.Port.Owner is not Room room || end.Port.Owner != room) return false;

        // outwards from the middle of the room through the side the loop leaves from
        var centre = room.InnerBounds.Center;
        var foot = start.Position;
        var outward = foot - centre;
        if (outward.Length < 0.01f) return false;
        outward = CoreVector.Normalize(outward);
        var across = new CoreVector(-outward.Y, outward.X);

        // a teardrop: leave on one side of the foot, swing out and round, come back in on the other side
        var size = Math.Max(14f, (float)Settings.ConnectionArrowSize * 2.2f);
        var width = size * 0.35f;
        var p0 = foot - across * width;
        var p3 = foot + across * width;
        var p1 = p0 + outward * size - across * (width * 1.6f);
        var p2 = p3 + outward * size + across * (width * 1.6f);

        var geometry = new StreamGeometry();
        using (var figure = geometry.Open())
        {
            figure.BeginFigure(p0.ToAvaloniaPoint(), false);
            figure.CubicBezierTo(p1.ToAvaloniaPoint(), p2.ToAvaloniaPoint(), p3.ToAvaloniaPoint());
            figure.EndFigure(false);
        }

        context.DrawGeometry(null, pen, geometry);
        if (isSelected) context.DrawGeometry(null, HighlightPen(CoreColors.SelectedLine, 1.5, connection.Style), geometry);
        else if (isHover) context.DrawGeometry(null, HighlightPen(CoreColors.HoverLine, 1.0, connection.Style), geometry);

        // one arrowhead near the end, pointing back into the room
        var t = 0.86f;
        var head = Bezier(p0, p1, p2, p3, t);
        var heading = Bezier(p0, p1, p2, p3, Math.Min(1f, t + 0.05f)) - Bezier(p0, p1, p2, p3, t - 0.05f);
        if (heading.Length > 0.01f)
        {
            var angle = Math.Atan2(heading.Y, heading.X) * 180.0 / Math.PI;
            DrawingAdapters.DrawChevron(context, head.ToAvaloniaPoint(), angle, Settings.ConnectionArrowSize, brush,
                connection.Id, Settings.HandDrawn);
        }

        // labels go beyond the top of the curl
        var apex = Bezier(p0, p1, p2, p3, 0.5f);
        var labelLine = new List<CoreLineSegment> { new(apex, apex + outward * 8f) };
        DrawAnnotations(connection, context, labelLine, cache);
        return true;
    }

    private static CoreVector Bezier(CoreVector p0, CoreVector p1, CoreVector p2, CoreVector p3, float t)
    {
        var u = 1 - t;
        return p0 * (u * u * u) + p1 * (3 * u * u * t) + p2 * (3 * u * t * t) + p3 * (t * t * t);
    }

    public static List<CoreLineSegment> SplitSegment(CoreLineSegment lineSegment, SpatialLineIndex spatialIndex, int depth = 0)
    {
        var result = new List<CoreLineSegment>();
        var current = new CoreLineSegment(lineSegment.Start, lineSegment.End);
        if (depth > 10)
        {
            result.Add(current);
            return result;
        }

        var gap = Math.Max(1.0f, Settings.LineWidth) * 3.0f;
        var segMinX = Math.Min(current.Start.X, current.End.X) - gap;
        var segMaxX = Math.Max(current.Start.X, current.End.X) + gap;
        var segMinY = Math.Min(current.Start.Y, current.End.Y) - gap;
        var segMaxY = Math.Max(current.Start.Y, current.End.Y) + gap;

        var candidates = new List<CoreLineSegment>();
        spatialIndex.Query(segMinX, segMaxX, segMinY, segMaxY, candidates);

        for (var i = 0; i < candidates.Count; i++)
        {
            var previous = candidates[i];
            var prevMinX = Math.Min(previous.Start.X, previous.End.X);
            var prevMaxX = Math.Max(previous.Start.X, previous.End.X);
            var prevMinY = Math.Min(previous.Start.Y, previous.End.Y);
            var prevMaxY = Math.Max(previous.Start.Y, previous.End.Y);

            if (segMaxX < prevMinX || segMinX > prevMaxX || segMaxY < prevMinY || segMinY > prevMaxY)
            {
                continue;
            }

            if (current.Intersect(previous, true, out var intersects))
            {
                foreach (var intersect in intersects)
                {
                    if (intersect.Type == LineSegmentIntersectType.MidPointA)
                    {
                        var seg1 = new CoreLineSegment(current.Start, intersect.Position);
                        var seg2 = new CoreLineSegment(intersect.Position, current.End);
                        if (seg1.Shorten(gap))
                        {
                            result.AddRange(SplitSegment(seg1, spatialIndex, depth + 1));
                        }
                        else
                        {
                            result.Add(seg1);
                        }

                        if (seg2.Forshorten(gap))
                        {
                            result.AddRange(SplitSegment(seg2, spatialIndex, depth + 1));
                        }
                        else
                        {
                            result.Add(seg2);
                        }
                        return result;
                    }
                    else if (intersect.Type == LineSegmentIntersectType.StartA)
                    {
                        if (current.Forshorten(gap))
                        {
                            return SplitSegment(current, spatialIndex, depth + 1);
                        }
                        result.Add(current);
                        return result;
                    }
                    else if (intersect.Type == LineSegmentIntersectType.EndA)
                    {
                        if (current.Shorten(gap))
                        {
                            return SplitSegment(current, spatialIndex, depth + 1);
                        }
                        result.Add(current);
                        return result;
                    }
                }
            }
        }

        result.Add(current);
        return result;
    }

    public static List<CoreLineSegment> SplitSegment(CoreLineSegment lineSegment, List<CoreLineSegment> linesDrawn)
    {
        if (linesDrawn == null || linesDrawn.Count == 0)
        {
            return [new CoreLineSegment(lineSegment.Start, lineSegment.End)];
        }

        var spatialIndex = new SpatialLineIndex();
        foreach (var line in linesDrawn)
        {
            spatialIndex.Add(line);
        }
        return SplitSegment(lineSegment, spatialIndex);
    }

    private static void DrawDoorIcons(AvaloniaDrawingContext context, Door door, CoreLineSegment lineSegment)
    {
        var dir = CompassPointHelper.GetCompassPointFromDirectionVector(lineSegment.Delta);
        var basePos = lineSegment.Mid;

        var offset1 = GetDoorIconOffset(dir, 0);
        var offset2 = GetDoorIconOffset(dir, 1);

        DrawVectorDoor(context, new AvaloniaPoint(basePos.X + offset1.X, basePos.Y + offset1.Y), door.Open);
        DrawVectorLock(context, new AvaloniaPoint(basePos.X + offset2.X, basePos.Y + offset2.Y), door.Locked);
    }

    private static CoreVector GetDoorIconOffset(CompassPoint dir, int index)
    {
        const int step = 14;
        return dir switch
        {
            CompassPoint.East or CompassPoint.West => new CoreVector((index - 0.5f) * step, -16),
            CompassPoint.North or CompassPoint.South => new CoreVector(16, (index - 0.5f) * step),
            CompassPoint.NorthEast or CompassPoint.SouthWest => new CoreVector((index - 0.5f) * step + 10, -(index - 0.5f) * step - 10),
            _ => new CoreVector((index - 0.5f) * step + 10, (index - 0.5f) * step + 10)
        };
    }

    private static void DrawVectorDoor(AvaloniaDrawingContext context, AvaloniaPoint pos, bool isOpen)
    {
        var pen = DrawingAdapters.ToPen(Settings.Color[CoreColors.Line], 1.2);
        var bgBrush = DrawingAdapters.ToBrush(CoreColors.Canvas);

        var frameRect = new AvaloniaRect(pos.X - 6, pos.Y - 7, 12, 14);
        context.DrawRectangle(bgBrush, pen, frameRect, 1, 1);

        if (isOpen)
        {
            var openGeo = new StreamGeometry();
            using (var ctx = openGeo.Open())
            {
                ctx.BeginFigure(new AvaloniaPoint(pos.X - 5, pos.Y + 6), true);
                ctx.LineTo(new AvaloniaPoint(pos.X + 2, pos.Y + 3));
                ctx.LineTo(new AvaloniaPoint(pos.X + 2, pos.Y - 5));
                ctx.LineTo(new AvaloniaPoint(pos.X - 5, pos.Y - 6));
                ctx.EndFigure(true);
            }
            context.DrawGeometry(bgBrush, pen, openGeo);
        }
        else
        {
            context.DrawEllipse(pen.Brush, null, new AvaloniaPoint(pos.X + 2, pos.Y), 1.5, 1.5);
        }
    }

    private static void DrawVectorLock(AvaloniaDrawingContext context, AvaloniaPoint pos, bool isLocked)
    {
        var pen = DrawingAdapters.ToPen(Settings.Color[CoreColors.Line], 1.2);
        var bgBrush = DrawingAdapters.ToBrush(CoreColors.Canvas);

        var shackleGeo = new StreamGeometry();
        using (var ctx = shackleGeo.Open())
        {
            if (isLocked)
            {
                ctx.BeginFigure(new AvaloniaPoint(pos.X - 3, pos.Y - 1), false);
                ctx.LineTo(new AvaloniaPoint(pos.X - 3, pos.Y - 4));
                ctx.ArcTo(new AvaloniaPoint(pos.X + 3, pos.Y - 4), new Size(3, 3), 0, false, SweepDirection.Clockwise);
                ctx.LineTo(new AvaloniaPoint(pos.X + 3, pos.Y - 1));
                ctx.EndFigure(false);
            }
            else
            {
                ctx.BeginFigure(new AvaloniaPoint(pos.X - 3, pos.Y - 1), false);
                ctx.LineTo(new AvaloniaPoint(pos.X - 3, pos.Y - 6));
                ctx.ArcTo(new AvaloniaPoint(pos.X + 3, pos.Y - 6), new Size(3, 3), 0, false, SweepDirection.Clockwise);
                ctx.LineTo(new AvaloniaPoint(pos.X + 3, pos.Y - 4));
                ctx.EndFigure(false);
            }
        }
        context.DrawGeometry(null, pen, shackleGeo);

        var bodyRect = new AvaloniaRect(pos.X - 5, pos.Y - 1, 10, 8);
        context.DrawRectangle(bgBrush, pen, bodyRect, 1, 1);
        context.DrawEllipse(pen.Brush, null, new AvaloniaPoint(pos.X, pos.Y + 3), 1, 1);
    }

    public static CoreVector GetRoomTypeAdjustments(Vertex vertex)
    {
        if (vertex.Port == null) return CoreVector.Zero;
        CompassPointHelper.FromName(vertex.Port.Id, out var dir);
        return dir switch
        {
            CompassPoint.SouthEast => new CoreVector(8, 6),
            CompassPoint.NorthEast => new CoreVector(10, -6),
            CompassPoint.EastNorthEast or CompassPoint.EastSouthEast => new CoreVector(4, 0),
            CompassPoint.SouthWest => new CoreVector(-10, 4),
            CompassPoint.NorthWest => new CoreVector(-10, -4),
            CompassPoint.WestSouthWest => new CoreVector(-10, 0),
            CompassPoint.WestNorthWest => new CoreVector(-10, -4),
            _ => CoreVector.Zero
        };
    }

    private static void DrawAnnotations(
        Connection connection,
        AvaloniaDrawingContext context,
        List<CoreLineSegment> segments,
        ConnectionCache cache)
    {
        if (segments.Count == 0) return;

        var font = Settings.LineFontMetadata;
        var typeface = font.ToTypeface();
        var brush = DrawingAdapters.ToBrush(CoreColors.LineText);
        var textColor = (uint)Settings.Color[CoreColors.LineText].ToArgb();

        var fontValid = cache.LastAnnotationFontName == font.Name
                        && cache.LastAnnotationFontStyle == (int)font.Style
                        && Math.Abs(cache.LastAnnotationFontSize - font.Size) < 0.01f
                        && cache.LastAnnotationColor == textColor;

        if (!fontValid)
        {
            cache.LastAnnotationFontName = font.Name;
            cache.LastAnnotationFontStyle = (int)font.Style;
            cache.LastAnnotationFontSize = font.Size;
            cache.LastAnnotationColor = textColor;
            cache.CachedStartText = null;
            cache.CachedMidText = null;
            cache.CachedEndText = null;
        }

        // an end with no label of its own may show its direction ("ne"), as the map settings say
        var startText = ConnectionEndLabels.Text(connection, true);
        if (!string.IsNullOrEmpty(startText))
        {
            if (cache.LastStartText != startText || cache.CachedStartText == null)
            {
                cache.LastStartText = startText;
                cache.CachedStartText = null;
            }
            var startVertex = connection.VertexList.Count > 0 ? connection.VertexList[0] : null;
            DrawSingleAnnotation(context, startText, segments[0], font, typeface, brush, true, startVertex, ref cache.CachedStartText);
        }

        var endText = ConnectionEndLabels.Text(connection, false);
        if (!string.IsNullOrEmpty(endText))
        {
            if (cache.LastEndText != endText || cache.CachedEndText == null)
            {
                cache.LastEndText = endText;
                cache.CachedEndText = null;
            }
            var endVertex = connection.VertexList.Count > 1 ? connection.VertexList[^1] : null;
            DrawSingleAnnotation(context, endText, segments[^1], font, typeface, brush, false, endVertex, ref cache.CachedEndText);
        }

        // a split join says at the tip of each stub where it goes, the command with it
        if (connection.IsDrawnSplit)
        {
            DrawSplitLabel(connection, context, true, font, typeface, brush);
            DrawSplitLabel(connection, context, false, font, typeface, brush);
            return;
        }

        connection.SplitLabelBounds[0] = null;
        connection.SplitLabelBounds[1] = null;

        if (!string.IsNullOrEmpty(connection.MidText))
        {
            if (cache.LastMidText != connection.MidText || cache.CachedMidText == null)
            {
                cache.LastMidText = connection.MidText;
                cache.CachedMidText = null;
            }
            var totalLength = segments.Sum(s => s.Length);
            var middle = totalLength / 2f;
            foreach (var seg in segments)
            {
                if (middle > seg.Length)
                {
                    middle -= seg.Length;
                }
                else
                {
                    var ratio = seg.Length > 0.001f ? middle / seg.Length : 0.5f;
                    var midPoint = seg.Start + seg.Delta * ratio;
                    var dummySeg = new CoreLineSegment(midPoint - seg.Delta * 0.01f, midPoint + seg.Delta * 0.01f);
                    DrawSingleAnnotation(context, connection.MidText, dummySeg, font, typeface, brush, null, null, ref cache.CachedMidText);
                    break;
                }
            }
        }
    }

    /// <summary>
    ///   The label at the tip of a split join's stub: "to Building" (or "from Debris Room" at the far end of a one-way
    ///   join), with the join's command, "xyzzy", before it.
    /// </summary>
    internal static string SplitLabel(Connection connection, bool atStart)
    {
        var far = connection.FarRoom(atStart);
        var name = far == null ? string.Empty : far.IsPortal ? far.ReferenceRoom?.Name ?? far.Name : far.Name;
        var where = (connection.Flow == ConnectionFlow.OneWay && !atStart ? "from " : "to ") + name;
        return string.IsNullOrWhiteSpace(connection.MidText) ? where : connection.MidText.Trim() + ": " + where;
    }

    private static void DrawSplitLabel(
        Connection connection,
        AvaloniaDrawingContext context,
        bool atStart,
        ThemeFont font,
        Typeface typeface,
        IBrush brush)
    {
        connection.GetSplitStub(atStart, out var foot, out var tip);
        var outward = tip - foot;
        if (outward.Length < 0.01f) return;
        outward = CoreVector.Normalize(outward);
        var text = new FormattedText(SplitLabel(connection, atStart), CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, typeface, font.Size, brush);
        var anchor = tip + outward * 3f;
        double x;
        double y;
        if (outward.X > 0.4f)
        {
            x = anchor.X;
            y = anchor.Y - text.Height / 2;
        }
        else if (outward.X < -0.4f)
        {
            x = anchor.X - text.Width;
            y = anchor.Y - text.Height / 2;
        }
        else
        {
            x = anchor.X - text.Width / 2;
            y = outward.Y < 0 ? anchor.Y - text.Height : anchor.Y;
        }

        context.DrawText(text, new AvaloniaPoint(x, y));
        // clickable: it goes to the room it names
        connection.SplitLabelBounds[atStart ? 0 : 1] = new CoreRect((float)x, (float)y, (float)text.Width, (float)text.Height);
    }

    /// <summary>Whether the room at this end lists its objects above or below itself, on the side this end is at.</summary>
    private static bool ObjectsOnSideOf(Vertex? vertex, CoreVector anchor)
    {
        if (vertex?.Port?.Owner is not Room room) return false;
        if (string.IsNullOrWhiteSpace(ObjectList.FormatForDisplay(room.Objects))) return false;
        var bounds = room.InnerBounds;
        return room.ObjectsPosition switch
        {
            CompassPoint.South => anchor.Y >= bounds.Center.Y,
            CompassPoint.North => anchor.Y <= bounds.Center.Y,
            _ => false
        };
    }

    private static void DrawSingleAnnotation(
        AvaloniaDrawingContext context,
        string text,
        CoreLineSegment segment,
        ThemeFont font,
        Typeface typeface,
        IBrush brush,
        bool? isStart,
        Vertex? vertex,
        ref FormattedText? cachedFormattedText)
    {
        var delta = segment.Delta;
        CoreVector anchor;
        var roomTypeAdjustments = CoreVector.Zero;

        if (isStart == true)
        {
            if (vertex?.Port != null)
            {
                var roomType = vertex.Port.Owner.GetRoomType();
                if (roomType == RoomShape.Ellipse || roomType == RoomShape.Octagonal)
                    roomTypeAdjustments = GetRoomTypeAdjustments(vertex);
            }
            anchor = segment.Start + roomTypeAdjustments;
            delta.Negate();
        }
        else if (isStart == false)
        {
            if (vertex?.Port != null)
            {
                var roomType = vertex.Port.Owner.GetRoomType();
                if (roomType == RoomShape.Ellipse || roomType == RoomShape.Octagonal)
                    roomTypeAdjustments = GetRoomTypeAdjustments(vertex);
            }
            anchor = segment.End + roomTypeAdjustments;
        }
        else
        {
            anchor = segment.Mid;
        }

        var compass = CompassPointHelper.DirectionFromAngle(out var angle, delta);
        var bounds = new CoreRect(anchor, CoreVector.Zero);
        bounds.Inflate(Settings.TextOffsetFromConnection, Settings.TextOffsetFromConnection);
        var pos = bounds.GetCorner(compass);

        var alignment = TextAlignment.Left;
        if (isStart == null && Math.Abs(angle) == 90)
        {
            pos = bounds.GetCorner(CompassPoint.East);
        }
        else if (isStart == null && Numeric.InRange(angle, -10, 10))
        {
            pos = bounds.GetCorner(CompassPoint.South);
            alignment = TextAlignment.Center;
        }
        else
        {
            alignment = compass switch
            {
                CompassPoint.North or CompassPoint.South => TextAlignment.Center,
                CompassPoint.West or CompassPoint.NorthWest or CompassPoint.SouthWest => TextAlignment.Right,
                _ => TextAlignment.Left
            };
        }

        // A label on a join leaving straight up or down from a room whose objects are listed on that side goes to
        // the left of the line, since the objects are written to the right of it. (The compass point is never north
        // or south here: DirectionFromAngle gives a diagonal for a vertical line, so go by the angle.)
        var upright = Math.Abs(Math.Abs(angle) - 90) <= 10;
        if (isStart != null && upright && ObjectsOnSideOf(vertex, anchor))
        {
            pos = new CoreVector(anchor.X - Settings.TextOffsetFromConnection, pos.Y);
            alignment = TextAlignment.Right;
        }

        if (cachedFormattedText == null)
        {
            cachedFormattedText = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                font.Size,
                brush)
            {
                TextAlignment = alignment
            };
            cachedFormattedText.ConstrainToNaturalWidth();
        }
        else
        {
            cachedFormattedText.TextAlignment = alignment;
        }

        double drawX = pos.X;
        double drawY = pos.Y;

        if (alignment == TextAlignment.Center)
        {
            drawX -= cachedFormattedText.Width / 2.0;
        }
        else if (alignment == TextAlignment.Right)
        {
            drawX -= cachedFormattedText.Width;
        }

        if (compass == CompassPoint.North || compass == CompassPoint.NorthEast || compass == CompassPoint.NorthWest)
        {
            drawY -= cachedFormattedText.Height;
        }

        if (TextRenderingGate.IsEnabled) context.DrawText(cachedFormattedText, new AvaloniaPoint(drawX, drawY));
    }
}
