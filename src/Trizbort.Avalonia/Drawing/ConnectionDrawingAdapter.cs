using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
using CoreLineSegment = Trizbort.Domain.Misc.LineSegment;
using AvaloniaRect = Avalonia.Rect;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaColor = Avalonia.Media.Color;
using AvaloniaDrawingContext = Avalonia.Media.DrawingContext;

namespace Trizbort.Avalonia.Drawing;

public static class ConnectionDrawingAdapter
{
    public static void DrawConnection(
        this Connection connection,
        AvaloniaDrawingContext context,
        bool isSelected,
        bool isHover,
        List<CoreLineSegment>? linesDrawn = null)
    {
        var segments = connection.GetSegments();
        if (segments == null || segments.Count == 0) return;

        var color = connection.ConnectionColor != CoreColor.Transparent
            ? connection.ConnectionColor
            : Settings.Color[CoreColors.Line];
        var dashStyle = connection.Style.ToDashStyle();
        var pen = color.ToPen(Settings.LineWidth, dashStyle);
        var brush = color.ToBrush();
        var handDrawn = Settings.HandDrawn;
        var seed = connection.Id;

        // 1. Draw Connection Line / Path
        if (connection.HasCurveWaypoints)
        {
            var points = new List<CoreVector>();
            foreach (var seg in segments)
            {
                points.Add(seg.Start);
            }
            points.Add(segments[^1].End);

            var curveGeo = handDrawn
                ? DrawingAdapters.ToPolylineGeometry(Sketch.Polyline(points, Sketch.Seeded(seed)))
                : DrawingAdapters.ToPolylineGeometry(points);

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
            // Calculate smart line segments (smart jump-overs across existing lines)
            var activeSegments = new List<CoreLineSegment>();
            if (linesDrawn != null && linesDrawn.Count > 0)
            {
                foreach (var seg in segments)
                {
                    var splitSegs = SplitSegment(seg, linesDrawn);
                    activeSegments.AddRange(splitSegs);
                }
            }
            else
            {
                activeSegments.AddRange(segments);
            }

            foreach (var seg in activeSegments)
            {
                if (handDrawn)
                {
                    var sketched = Sketch.Line(seg.Start, seg.End, Sketch.Seeded(seed));
                    var lineGeo = DrawingAdapters.ToPolylineGeometry(sketched);
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

                linesDrawn?.Add(seg);
            }
        }

        // 2. Draw Directional Chevron (One-Way Flow)
        if (connection.Flow == ConnectionFlow.OneWay && segments.Count > 0)
        {
            var lastSegment = segments[^1];
            var angleRad = Math.Atan2(lastSegment.Delta.Y, lastSegment.Delta.X);
            var angleDeg = angleRad * 180.0 / Math.PI + 90.0;
            var arrowSize = Math.Max(8.0, 2.0 * Settings.LineWidth);
            DrawingAdapters.DrawChevron(context, lastSegment.End.ToAvaloniaPoint(), angleDeg, arrowSize, brush, seed, handDrawn);
        }

        // 3. Draw Door / Lock Icons
        if (connection.Door != null && segments.Count > 0)
        {
            var midSegment = segments[segments.Count / 2];
            DrawDoorIcons(context, connection.Door, midSegment);
        }

        // 4. Draw Annotations (StartText, MidText, EndText)
        DrawAnnotations(connection, context, segments);
    }

    private static List<CoreLineSegment> SplitSegment(CoreLineSegment lineSegment, List<CoreLineSegment> linesDrawn)
    {
        var result = new List<CoreLineSegment>();
        var gap = Math.Max(1.0f, Settings.LineWidth) * 3.0f;

        foreach (var previous in linesDrawn)
        {
            if (lineSegment.Intersect(previous, true, out var intersects))
            {
                foreach (var intersect in intersects)
                {
                    if (intersect.Type == LineSegmentIntersectType.MidPointA)
                    {
                        var seg1 = new CoreLineSegment(lineSegment.Start, intersect.Position);
                        var seg2 = new CoreLineSegment(intersect.Position, lineSegment.End);
                        if (seg1.Shorten(gap))
                        {
                            result.AddRange(SplitSegment(seg1, linesDrawn));
                        }
                        else
                        {
                            result.Add(seg1);
                        }

                        if (seg2.Forshorten(gap))
                        {
                            result.AddRange(SplitSegment(seg2, linesDrawn));
                        }
                        else
                        {
                            result.Add(seg2);
                        }
                        return result;
                    }
                }
            }
        }

        result.Add(lineSegment);
        return result;
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
            var shackleTop = isLocked ? pos.Y - 6 : pos.Y - 8;
            ctx.BeginFigure(new AvaloniaPoint(pos.X - 3, pos.Y - 1), false);
            ctx.ArcTo(new AvaloniaPoint(pos.X + 3, pos.Y - 1), new Size(3, 4), 0, false, SweepDirection.Clockwise);
            ctx.EndFigure(false);
        }
        context.DrawGeometry(null, pen, shackleGeo);

        var bodyRect = new AvaloniaRect(pos.X - 5, pos.Y - 1, 10, 8);
        context.DrawRectangle(bgBrush, pen, bodyRect, 1, 1);
        context.DrawEllipse(pen.Brush, null, new AvaloniaPoint(pos.X, pos.Y + 3), 1, 1);
    }

    private static void DrawAnnotations(Connection connection, AvaloniaDrawingContext context, List<CoreLineSegment> segments)
    {
        if (segments.Count == 0) return;

        var font = Settings.LineFontMetadata;
        var typeface = font.ToTypeface();
        var brush = DrawingAdapters.ToBrush(CoreColors.LineText);

        if (!string.IsNullOrEmpty(connection.StartText))
        {
            DrawSingleAnnotation(context, connection.StartText, segments[0], font, typeface, brush, true);
        }

        if (!string.IsNullOrEmpty(connection.EndText))
        {
            DrawSingleAnnotation(context, connection.EndText, segments[^1], font, typeface, brush, false);
        }

        if (!string.IsNullOrEmpty(connection.MidText))
        {
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
                    DrawSingleAnnotation(context, connection.MidText, dummySeg, font, typeface, brush, null);
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
        bool? isStart)
    {
        var delta = segment.Delta;
        CoreVector anchor;
        if (isStart == true)
        {
            anchor = segment.Start;
            delta.Negate();
        }
        else if (isStart == false)
        {
            anchor = segment.End;
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

        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            font.Size,
            brush)
        {
            TextAlignment = alignment
        };
        formattedText.MaxTextWidth = formattedText.Width;

        double drawX = pos.X;
        double drawY = pos.Y;

        if (alignment == TextAlignment.Center)
        {
            drawX -= formattedText.Width / 2.0;
        }
        else if (alignment == TextAlignment.Right)
        {
            drawX -= formattedText.Width;
        }

        if (compass == CompassPoint.North || compass == CompassPoint.NorthEast || compass == CompassPoint.NorthWest)
        {
            drawY -= formattedText.Height;
        }

        context.DrawText(formattedText, new AvaloniaPoint(drawX, drawY));
    }
}
