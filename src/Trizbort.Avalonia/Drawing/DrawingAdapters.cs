#pragma warning disable CA1416

using System;
using System.Collections.Generic;
using System.Globalization;
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

public static class DrawingAdapters
{
    public static AvaloniaPoint ToAvaloniaPoint(this CoreVector v) => new(v.X, v.Y);

    public static CoreVector ToVector(this AvaloniaPoint p) => new((float)p.X, (float)p.Y);

    public static AvaloniaRect ToRect(this CoreRect r) => new(r.Left, r.Top, Math.Max(0, r.Width), Math.Max(0, r.Height));

    public static CoreRect ToCoreRect(this AvaloniaRect r) => new((float)r.X, (float)r.Y, (float)r.Width, (float)r.Height);

    public static AvaloniaColor ToColor(this CoreColor c) => AvaloniaColor.FromArgb(c.A, c.R, c.G, c.B);

    public static IBrush ToBrush(this CoreColor c) => new SolidColorBrush(c.ToColor());

    public static IBrush ToBrush(int colorKey) => new SolidColorBrush(Settings.Color[colorKey].ToColor());

    public static DashStyle? ToDashStyle(this BorderDashStyle style) => style switch
    {
        BorderDashStyle.Dash => new DashStyle(new double[] { 4, 2 }, 0),
        BorderDashStyle.Dot => new DashStyle(new double[] { 1, 2 }, 0),
        BorderDashStyle.DashDot => new DashStyle(new double[] { 4, 2, 1, 2 }, 0),
        BorderDashStyle.DashDotDot => new DashStyle(new double[] { 4, 2, 1, 2, 1, 2 }, 0),
        _ => null
    };

    public static DashStyle? ToDashStyle(this ConnectionStyle style) => style switch
    {
        ConnectionStyle.Dashed => new DashStyle(new double[] { 2, 2 }, 0),
        _ => null
    };

    public static IPen ToPen(this CoreColor c, double thickness = 1.0, DashStyle? dashStyle = null)
    {
        return new Pen(c.ToBrush(), Math.Max(0.5, thickness), dashStyle, PenLineCap.Round, PenLineJoin.Round);
    }

    public static Typeface ToTypeface(this ThemeFont font)
    {
        var style = ((int)font.Style & (int)System.Drawing.FontStyle.Italic) != 0 ? global::Avalonia.Media.FontStyle.Italic : global::Avalonia.Media.FontStyle.Normal;
        var weight = ((int)font.Style & (int)System.Drawing.FontStyle.Bold) != 0 ? FontWeight.Bold : FontWeight.Normal;
        var familyName = string.IsNullOrWhiteSpace(font.Name) ? "Segoe UI, Inter, Sans-Serif" : font.Name;
        return new Typeface(new FontFamily(familyName), style, weight);
    }

    public static StreamGeometry ToPolygonGeometry(IReadOnlyList<CoreVector> vertices, bool isClosed = true)
    {
        var geometry = new StreamGeometry();
        if (vertices.Count == 0) return geometry;

        using var ctx = geometry.Open();
        ctx.BeginFigure(vertices[0].ToAvaloniaPoint(), true);
        for (var i = 1; i < vertices.Count; i++)
        {
            ctx.LineTo(vertices[i].ToAvaloniaPoint());
        }
        ctx.EndFigure(isClosed);
        return geometry;
    }

    public static StreamGeometry ToPolylineGeometry(IReadOnlyList<CoreVector> vertices)
    {
        var geometry = new StreamGeometry();
        if (vertices.Count == 0) return geometry;

        using var ctx = geometry.Open();
        ctx.BeginFigure(vertices[0].ToAvaloniaPoint(), false);
        for (var i = 1; i < vertices.Count; i++)
        {
            ctx.LineTo(vertices[i].ToAvaloniaPoint());
        }
        ctx.EndFigure(false);
        return geometry;
    }

    public static StreamGeometry CreateSquareGeometry(CoreRect bounds, bool handDrawn = false, int seed = 0)
    {
        var vertices = new[]
        {
            new CoreVector(bounds.Left, bounds.Top),
            new CoreVector(bounds.Right, bounds.Top),
            new CoreVector(bounds.Right, bounds.Bottom),
            new CoreVector(bounds.Left, bounds.Bottom)
        };

        if (handDrawn)
        {
            var sketchPoints = Sketch.Polygon(vertices, Sketch.Seeded(seed));
            return ToPolygonGeometry(sketchPoints, true);
        }

        return ToPolygonGeometry(vertices, true);
    }

    public static StreamGeometry CreateRoundedRectangleGeometry(CoreRect bounds, CornerRadii corners, bool handDrawn = false, int seed = 0)
    {
        var tl = Math.Max(0, (float)Math.Min(corners.TopLeft, Math.Min(bounds.Width, bounds.Height) / 2));
        var tr = Math.Max(0, (float)Math.Min(corners.TopRight, Math.Min(bounds.Width, bounds.Height) / 2));
        var br = Math.Max(0, (float)Math.Min(corners.BottomRight, Math.Min(bounds.Width, bounds.Height) / 2));
        var bl = Math.Max(0, (float)Math.Min(corners.BottomLeft, Math.Min(bounds.Width, bounds.Height) / 2));

        if (tl <= 0 && tr <= 0 && br <= 0 && bl <= 0)
        {
            return CreateSquareGeometry(bounds, handDrawn, seed);
        }

        if (handDrawn)
        {
            var approx = new List<CoreVector>
            {
                new(bounds.Left + tl, bounds.Top),
                new(bounds.Right - tr, bounds.Top),
                new(bounds.Right, bounds.Top + tr),
                new(bounds.Right, bounds.Bottom - br),
                new(bounds.Right - br, bounds.Bottom),
                new(bounds.Left + bl, bounds.Bottom),
                new(bounds.Left, bounds.Bottom - bl),
                new(bounds.Left, bounds.Top + tl)
            };
            var sketched = Sketch.ClosedCurve(approx, Sketch.Seeded(seed));
            return ToPolygonGeometry(sketched, true);
        }

        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(new AvaloniaPoint(bounds.Left + tl, bounds.Top), true);
        
        ctx.LineTo(new AvaloniaPoint(bounds.Right - tr, bounds.Top));
        if (tr > 0)
            ctx.ArcTo(new AvaloniaPoint(bounds.Right, bounds.Top + tr), new Size(tr, tr), 0, false, SweepDirection.Clockwise);
            
        ctx.LineTo(new AvaloniaPoint(bounds.Right, bounds.Bottom - br));
        if (br > 0)
            ctx.ArcTo(new AvaloniaPoint(bounds.Right - br, bounds.Bottom), new Size(br, br), 0, false, SweepDirection.Clockwise);
            
        ctx.LineTo(new AvaloniaPoint(bounds.Left + bl, bounds.Bottom));
        if (bl > 0)
            ctx.ArcTo(new AvaloniaPoint(bounds.Left, bounds.Bottom - bl), new Size(bl, bl), 0, false, SweepDirection.Clockwise);
            
        ctx.LineTo(new AvaloniaPoint(bounds.Left, bounds.Top + tl));
        if (tl > 0)
            ctx.ArcTo(new AvaloniaPoint(bounds.Left + tl, bounds.Top), new Size(tl, tl), 0, false, SweepDirection.Clockwise);

        ctx.EndFigure(true);
        return geometry;
    }

    public static StreamGeometry CreateOctagonalGeometry(CoreRect bounds, CornerRadii corners, bool handDrawn = false, int seed = 0)
    {
        var defaultChamfer = Math.Min(bounds.Width, bounds.Height) * 0.25f;
        var tl = corners.TopLeft > 0 ? (float)Math.Min(corners.TopLeft, Math.Min(bounds.Width, bounds.Height) / 2) : defaultChamfer;
        var tr = corners.TopRight > 0 ? (float)Math.Min(corners.TopRight, Math.Min(bounds.Width, bounds.Height) / 2) : defaultChamfer;
        var br = corners.BottomRight > 0 ? (float)Math.Min(corners.BottomRight, Math.Min(bounds.Width, bounds.Height) / 2) : defaultChamfer;
        var bl = corners.BottomLeft > 0 ? (float)Math.Min(corners.BottomLeft, Math.Min(bounds.Width, bounds.Height) / 2) : defaultChamfer;

        var vertices = new[]
        {
            new CoreVector(bounds.Left + tl, bounds.Top),
            new CoreVector(bounds.Right - tr, bounds.Top),
            new CoreVector(bounds.Right, bounds.Top + tr),
            new CoreVector(bounds.Right, bounds.Bottom - br),
            new CoreVector(bounds.Right - br, bounds.Bottom),
            new CoreVector(bounds.Left + bl, bounds.Bottom),
            new CoreVector(bounds.Left, bounds.Bottom - bl),
            new CoreVector(bounds.Left, bounds.Top + tl)
        };

        if (handDrawn)
        {
            var sketched = Sketch.Polygon(vertices, Sketch.Seeded(seed));
            return ToPolygonGeometry(sketched, true);
        }

        return ToPolygonGeometry(vertices, true);
    }

    public static StreamGeometry CreateEllipseGeometry(CoreRect bounds, bool handDrawn = false, int seed = 0)
    {
        if (handDrawn)
        {
            var cx = bounds.Left + bounds.Width / 2f;
            var cy = bounds.Top + bounds.Height / 2f;
            var rx = bounds.Width / 2f;
            var ry = bounds.Height / 2f;
            const int sampleCount = 24;
            var samples = new List<CoreVector>(sampleCount);
            for (var i = 0; i < sampleCount; i++)
            {
                var theta = i * 2 * Math.PI / sampleCount;
                samples.Add(new CoreVector(cx + rx * (float)Math.Cos(theta), cy + ry * (float)Math.Sin(theta)));
            }
            var sketched = Sketch.ClosedCurve(samples, Sketch.Seeded(seed));
            return ToPolygonGeometry(sketched, true);
        }

        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        var center = new AvaloniaPoint(bounds.Left + bounds.Width / 2.0, bounds.Top + bounds.Height / 2.0);
        var rxD = bounds.Width / 2.0;
        var ryD = bounds.Height / 2.0;

        ctx.BeginFigure(new AvaloniaPoint(center.X, center.Y - ryD), true);
        ctx.ArcTo(new AvaloniaPoint(center.X, center.Y + ryD), new Size(rxD, ryD), 0, false, SweepDirection.Clockwise);
        ctx.ArcTo(new AvaloniaPoint(center.X, center.Y - ryD), new Size(rxD, ryD), 0, false, SweepDirection.Clockwise);
        ctx.EndFigure(true);
        return geometry;
    }

    public static void DrawChevron(
        AvaloniaDrawingContext context,
        AvaloniaPoint tip,
        double angleDeg,
        double arrowSize,
        IBrush brush,
        int seed = 0,
        bool handDrawn = false)
    {
        var rad = angleDeg * Math.PI / 180.0;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);

        var pLeft = new AvaloniaPoint(tip.X - arrowSize * (sin + cos * 0.5), tip.Y + arrowSize * (cos - sin * 0.5));
        var pRight = new AvaloniaPoint(tip.X - arrowSize * (sin - cos * 0.5), tip.Y + arrowSize * (cos + sin * 0.5));

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(tip, true);
            ctx.LineTo(pLeft);
            ctx.LineTo(pRight);
            ctx.EndFigure(true);
        }

        context.DrawGeometry(brush, null, geometry);
    }

    /// <summary>
    /// Constrains <see cref="FormattedText"/> to its natural width, including trailing
    /// whitespace, so per-line alignment works without wrapping a trailing space onto a new line.
    /// </summary>
    public static void ConstrainToNaturalWidth(this FormattedText text)
    {
        var width = text.WidthIncludingTrailingWhitespace;
        if (width > 0)
        {
            text.MaxTextWidth = width;
        }
    }
}
