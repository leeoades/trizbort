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
                if (!conn.HasCurveWaypoints)
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
        var curved = connection.HasCurveWaypoints;
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
                        var selPen = Settings.Color[CoreColors.SelectedLine].ToPen(Settings.LineWidth + 1.5);
                        context.DrawGeometry(null, selPen, curveGeo);
                    }
                    else if (isHover)
                    {
                        var hoverPen = Settings.Color[CoreColors.HoverLine].ToPen(Settings.LineWidth + 1.0);
                        context.DrawGeometry(null, hoverPen, curveGeo);
                    }
                }
                else
                {
                    var curveGeo = DrawingAdapters.ToPolylineGeometry(points);
                    context.DrawGeometry(null, pen, curveGeo);

                    if (isSelected)
                    {
                        var selPen = Settings.Color[CoreColors.SelectedLine].ToPen(Settings.LineWidth + 1.5);
                        context.DrawGeometry(null, selPen, curveGeo);
                    }
                    else if (isHover)
                    {
                        var hoverPen = Settings.Color[CoreColors.HoverLine].ToPen(Settings.LineWidth + 1.0);
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
                        var selPen = Settings.Color[CoreColors.SelectedLine].ToPen(Settings.LineWidth + 1.5);
                        context.DrawGeometry(null, selPen, lineGeo);
                    }
                    else if (isHover)
                    {
                        var hoverPen = Settings.Color[CoreColors.HoverLine].ToPen(Settings.LineWidth + 1.0);
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
                        var selPen = Settings.Color[CoreColors.SelectedLine].ToPen(Settings.LineWidth + 1.5);
                        context.DrawLine(selPen, p1, p2);
                    }
                    else if (isHover)
                    {
                        var hoverPen = Settings.Color[CoreColors.HoverLine].ToPen(Settings.LineWidth + 1.0);
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

        if (!string.IsNullOrEmpty(connection.StartText))
        {
            if (cache.LastStartText != connection.StartText || cache.CachedStartText == null)
            {
                cache.LastStartText = connection.StartText;
                cache.CachedStartText = null;
            }
            var startVertex = connection.VertexList.Count > 0 ? connection.VertexList[0] : null;
            DrawSingleAnnotation(context, connection.StartText, segments[0], font, typeface, brush, true, startVertex, ref cache.CachedStartText);
        }

        if (!string.IsNullOrEmpty(connection.EndText))
        {
            if (cache.LastEndText != connection.EndText || cache.CachedEndText == null)
            {
                cache.LastEndText = connection.EndText;
                cache.CachedEndText = null;
            }
            var endVertex = connection.VertexList.Count > 1 ? connection.VertexList[^1] : null;
            DrawSingleAnnotation(context, connection.EndText, segments[^1], font, typeface, brush, false, endVertex, ref cache.CachedEndText);
        }

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

        context.DrawText(cachedFormattedText, new AvaloniaPoint(drawX, drawY));
    }
}
