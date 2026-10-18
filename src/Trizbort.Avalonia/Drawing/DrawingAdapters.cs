#pragma warning disable CA1416

using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Media;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using CoreRect = Trizbort.Domain.Misc.Rect;
using CoreVector = Trizbort.Domain.Misc.Vector;
using CoreColor = System.Drawing.Color;
using AvaloniaRect = Avalonia.Rect;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaColor = Avalonia.Media.Color;
using AvaloniaDrawingContext = Avalonia.Media.DrawingContext;

namespace Trizbort.Avalonia.Drawing;

public static class DrawingAdapters
{
    private static readonly ConcurrentDictionary<uint, IBrush> _brushCache = new();
    private static readonly ConcurrentDictionary<(uint, int, int), IPen> _penCache = new();
    private static readonly ConcurrentDictionary<(string, int), Typeface> _typefaceCache = new();

    public static AvaloniaPoint ToAvaloniaPoint(this CoreVector v) => new(v.X, v.Y);

    public static CoreVector ToVector(this AvaloniaPoint p) => new((float)p.X, (float)p.Y);

    public static AvaloniaRect ToRect(this CoreRect r) => new(r.Left, r.Top, Math.Max(0, r.Width), Math.Max(0, r.Height));

    public static CoreRect ToCoreRect(this AvaloniaRect r) => new((float)r.X, (float)r.Y, (float)r.Width, (float)r.Height);

    public static AvaloniaColor ToColor(this CoreColor c) => AvaloniaColor.FromArgb(c.A, c.R, c.G, c.B);

    public static IBrush ToBrush(this CoreColor c)
    {
        var argb = (uint)c.ToArgb();
        return _brushCache.GetOrAdd(argb, _ => new SolidColorBrush(c.ToColor()));
    }

    public static IBrush ToBrush(int colorKey) => Settings.Color[colorKey].ToBrush();

    private static readonly DashStyle _dashStyle = new(new double[] { 4, 2 }, 0);
    private static readonly DashStyle _dotStyle = new(new double[] { 1, 2 }, 0);
    private static readonly DashStyle _dashDotStyle = new(new double[] { 4, 2, 1, 2 }, 0);
    private static readonly DashStyle _dashDotDotStyle = new(new double[] { 4, 2, 1, 2, 1, 2 }, 0);
    private static readonly DashStyle _connectionDashedStyle = new(new double[] { 2, 2 }, 0);

    public static DashStyle? ToDashStyle(this BorderDashStyle style) => style switch
    {
        BorderDashStyle.Dash => _dashStyle,
        BorderDashStyle.Dot => _dotStyle,
        BorderDashStyle.DashDot => _dashDotStyle,
        BorderDashStyle.DashDotDot => _dashDotDotStyle,
        _ => null
    };

    public static DashStyle? ToDashStyle(this ConnectionStyle style) => style switch
    {
        ConnectionStyle.Dashed => _connectionDashedStyle,
        _ => null
    };

    private static int GetDashStyleKey(DashStyle? dashStyle)
    {
        if (dashStyle == null || dashStyle.Dashes == null || dashStyle.Dashes.Count == 0) return 0;
        var hash = new HashCode();
        foreach (var d in dashStyle.Dashes)
        {
            hash.Add(d);
        }
        hash.Add(dashStyle.Offset);
        return hash.ToHashCode();
    }

    public static IPen ToPen(this CoreColor c, double thickness = 1.0, DashStyle? dashStyle = null)
    {
        var clampedThickness = Math.Max(0.5, thickness);
        var thicknessKey = (int)(clampedThickness * 100);
        var dashKey = GetDashStyleKey(dashStyle);
        var key = ((uint)c.ToArgb(), thicknessKey, dashKey);

        return _penCache.GetOrAdd(key, _ => new Pen(c.ToBrush(), clampedThickness, dashStyle, PenLineCap.Round, PenLineJoin.Round));
    }

    public static Typeface ToTypeface(this ThemeFont font)
    {
        var familyName = string.IsNullOrWhiteSpace(font.Name) ? "Segoe UI, Inter, Sans-Serif" : font.Name;
        var styleInt = (int)font.Style;
        return _typefaceCache.GetOrAdd((familyName, styleInt), _ =>
        {
            var style = (styleInt & (int)System.Drawing.FontStyle.Italic) != 0 ? global::Avalonia.Media.FontStyle.Italic : global::Avalonia.Media.FontStyle.Normal;
            var weight = (styleInt & (int)System.Drawing.FontStyle.Bold) != 0 ? FontWeight.Bold : FontWeight.Normal;
            return new Typeface(new FontFamily(familyName), style, weight);
        });
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
            var outline = Sketch.RoundedRectangle(bounds, tl, tr, br, bl);
            var sketched = Sketch.ClosedCurve(outline, Sketch.Seeded(seed));
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

    public static StreamGeometry CreateOctagonalGeometry(CoreRect bounds, CornerRadii? corners = null, bool handDrawn = false, int seed = 0)
    {
        var qw = bounds.Width * 0.25f;
        var qh = bounds.Height * 0.25f;

        var vertices = new[]
        {
            new CoreVector(bounds.Left + qw, bounds.Top),
            new CoreVector(bounds.Right - qw, bounds.Top),
            new CoreVector(bounds.Right, bounds.Top + qh),
            new CoreVector(bounds.Right, bounds.Bottom - qh),
            new CoreVector(bounds.Right - qw, bounds.Bottom),
            new CoreVector(bounds.Left + qw, bounds.Bottom),
            new CoreVector(bounds.Left, bounds.Bottom - qh),
            new CoreVector(bounds.Left, bounds.Top + qh)
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
            var outline = Sketch.Ellipse(bounds);
            var sketched = Sketch.ClosedCurve(outline, Sketch.Seeded(seed));
            return ToPolygonGeometry(sketched, true);
        }

        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        var cx = bounds.Left + bounds.Width / 2.0;
        var cy = bounds.Top + bounds.Height / 2.0;
        var rx = bounds.Width / 2.0;
        var ry = bounds.Height / 2.0;
        const double kappa = 0.5522847498307935;
        var ox = rx * kappa;
        var oy = ry * kappa;

        ctx.BeginFigure(new AvaloniaPoint(cx, bounds.Top), true);
        ctx.CubicBezierTo(new AvaloniaPoint(cx + ox, bounds.Top), new AvaloniaPoint(bounds.Right, cy - oy), new AvaloniaPoint(bounds.Right, cy));
        ctx.CubicBezierTo(new AvaloniaPoint(bounds.Right, cy + oy), new AvaloniaPoint(cx + ox, bounds.Bottom), new AvaloniaPoint(cx, bounds.Bottom));
        ctx.CubicBezierTo(new AvaloniaPoint(cx - ox, bounds.Bottom), new AvaloniaPoint(bounds.Left, cy + oy), new AvaloniaPoint(bounds.Left, cy));
        ctx.CubicBezierTo(new AvaloniaPoint(bounds.Left, cy - oy), new AvaloniaPoint(cx - ox, bounds.Top), new AvaloniaPoint(cx, bounds.Top));
        ctx.EndFigure(true);
        return geometry;
    }

    public static StreamGeometry CreateChevronGeometry(
        AvaloniaPoint pos,
        double angleDeg,
        double size,
        int seed = 0,
        bool handDrawn = false)
    {
        var rad = angleDeg * Math.PI / 180.0;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);

        AvaloniaPoint Transform(double lx, double ly) =>
            new(pos.X + (lx * cos - ly * sin) * size,
                pos.Y + (lx * sin + ly * cos) * size);

        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();

        if (handDrawn)
        {
            var sketch = Sketch.Seeded(seed);
            double Jitter() => (sketch.NextDouble() - 0.5) * 0.14;

            var p1 = Transform(0.5 + Jitter(), Jitter());
            var p2 = Transform(-0.5 + Jitter(), -0.5 + Jitter());
            var p3 = Transform(-0.38 + Jitter(), Jitter() * 0.5);
            var p4 = Transform(-0.5 + Jitter(), 0.5 + Jitter());

            ctx.BeginFigure(p1, true);
            ctx.LineTo(p2);
            ctx.LineTo(p3);
            ctx.LineTo(p4);
            ctx.EndFigure(true);
        }
        else
        {
            var apex = Transform(0.5, 0.0);
            var rightCorner = Transform(-0.5, -0.5);
            var leftCorner = Transform(-0.5, 0.5);

            ctx.BeginFigure(apex, true);
            ctx.LineTo(rightCorner);
            ctx.LineTo(leftCorner);
            ctx.EndFigure(true);
        }

        return geometry;
    }

    public static void DrawChevron(
        AvaloniaDrawingContext context,
        AvaloniaPoint pos,
        double angleDeg,
        double arrowSize,
        IBrush brush,
        int seed = 0,
        bool handDrawn = false)
    {
        var geometry = CreateChevronGeometry(pos, angleDeg, arrowSize, seed, handDrawn);
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
