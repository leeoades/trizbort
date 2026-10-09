using System;
using System.Collections.Generic;
using System.Drawing;

namespace Trizbort.Domain.Misc {
  /// <summary>
  ///   Geometry for the "hand-drawn" style. Strokes get a smooth, gentle bow perpendicular to their
  ///   direction whose size scales with the stroke length, so short edges stay tidy and long lines
  ///   wander naturally. All results are deterministic for a given <see cref="Random" /> seed.
  /// </summary>
  public static class Sketch {
    private const float SAMPLE_SPACING = 3f;

    public static Random Seeded(int seed) {
      return new Random(unchecked(seed * 7919 + 17));
    }

    /// <summary>A hand-drawn straight stroke. The end points are exact so strokes join up.</summary>
    public static PointF[] Line(PointF start, PointF end, Random random) {
      var dx = end.X - start.X;
      var dy = end.Y - start.Y;
      var length = (float) Math.Sqrt(dx * dx + dy * dy);
      if (length < 2) return new[] { start, end };

      var nx = -dy / length;
      var ny = dx / length;
      var amplitude = clamp(length * 0.02f, 0.35f, 2.2f);
      var bow = amplitude * (0.5f + 0.5f * (float) random.NextDouble()) * (random.Next(2) == 0 ? -1 : 1);
      var wave = amplitude * 0.35f * signed(random);
      var ripple = amplitude * 0.15f * signed(random);

      var count = Math.Max(4, (int) (length / SAMPLE_SPACING));
      var points = new PointF[count + 1];
      for (var i = 0; i <= count; ++i) {
        var t = i / (float) count;
        var offset = bow * sin(Math.PI * t) + wave * sin(2 * Math.PI * t) + ripple * sin(3 * Math.PI * t);
        points[i] = new PointF(start.X + dx * t + nx * offset, start.Y + dy * t + ny * offset);
      }
      points[0] = start;
      points[count] = end;
      return points;
    }

    /// <summary>A hand-drawn closed polygon: each edge is drawn as its own stroke between exact corners.</summary>
    public static PointF[] Polygon(IReadOnlyList<PointF> vertices, Random random) {
      var result = new List<PointF>();
      for (var i = 0; i < vertices.Count; ++i) {
        var edge = Line(vertices[i], vertices[(i + 1) % vertices.Count], random);
        for (var j = 0; j < edge.Length - 1; ++j) result.Add(edge[j]);
      }
      return result.ToArray();
    }

    /// <summary>A hand-drawn version of a smooth closed outline (ellipse, rounded rectangle).</summary>
    public static PointF[] ClosedCurve(IReadOnlyList<PointF> outline, Random random) {
      var points = densify(outline, true);
      var perimeter = length(points, true);
      if (points.Count < 3 || perimeter < 2) return points.ToArray();

      var amplitude = clamp(perimeter * 0.005f, 0.6f, 2.2f);
      var harmonics = new[] { 2, 3, 5 };
      var weights = new[] { 0.6f, 0.3f, 0.15f };
      var amplitudes = new float[harmonics.Length];
      var phases = new double[harmonics.Length];
      for (var k = 0; k < harmonics.Length; ++k) {
        amplitudes[k] = amplitude * weights[k] * (0.5f + 0.5f * (float) random.NextDouble());
        phases[k] = random.NextDouble() * Math.PI * 2;
      }

      var result = new PointF[points.Count];
      var distance = 0f;
      for (var i = 0; i < points.Count; ++i) {
        if (i > 0) distance += Sketch.distance(points[i - 1], points[i]);
        var s = distance / perimeter;
        var offset = 0f;
        for (var k = 0; k < harmonics.Length; ++k)
          offset += amplitudes[k] * sin(2 * Math.PI * harmonics[k] * s + phases[k]);
        result[i] = Sketch.offset(points, i, true, offset);
      }
      return result;
    }

    /// <summary>
    ///   A hand-drawn open polyline such as a connection. End points are exact. Longer strokes get a
    ///   larger bow plus a few gentle waves, so a long connector reads as drawn freehand.
    /// </summary>
    public static PointF[] Polyline(IReadOnlyList<PointF> polyline, Random random) {
      var points = densify(polyline, false);
      var length = Sketch.length(points, false);
      if (points.Count < 2 || length < 2) return points.ToArray();

      var amplitude = clamp(length * 0.015f, 0.5f, 4f);
      var bow = amplitude * (0.5f + 0.5f * (float) random.NextDouble()) * (random.Next(2) == 0 ? -1 : 1);
      var waves = Math.Max(2, (int) Math.Round(length / 110));
      var wave = clamp(length * 0.006f, 0.3f, 1.6f) * (0.6f + 0.4f * (float) random.NextDouble());
      var phase = random.NextDouble() * Math.PI * 2;

      var result = new PointF[points.Count];
      var distance = 0f;
      for (var i = 0; i < points.Count; ++i) {
        if (i > 0) distance += Sketch.distance(points[i - 1], points[i]);
        var t = distance / length;
        var envelope = sin(Math.PI * t);
        var offset = bow * envelope + wave * envelope * sin(Math.PI * waves * t + phase);
        result[i] = Sketch.offset(points, i, false, offset);
      }
      result[0] = points[0];
      result[result.Length - 1] = points[points.Count - 1];
      return result;
    }

    /// <summary>The point on a polyline nearest to <paramref name="target" />, with the local direction there.</summary>
    public static PointF Nearest(IReadOnlyList<PointF> points, PointF target, out PointF direction) {
      var best = 0;
      var bestDistance = float.MaxValue;
      for (var i = 0; i < points.Count; ++i) {
        var d = distance(points[i], target);
        if (d < bestDistance) {
          bestDistance = d;
          best = i;
        }
      }
      var before = points[Math.Max(0, best - 2)];
      var after = points[Math.Min(points.Count - 1, best + 2)];
      direction = new PointF(after.X - before.X, after.Y - before.Y);
      return points[best];
    }

    public static PointF[] Ellipse(RectangleF rect) {
      var perimeter = Math.PI * (rect.Width + rect.Height) / 2;
      var count = Math.Max(16, (int) (perimeter / SAMPLE_SPACING));
      var points = new PointF[count];
      for (var i = 0; i < count; ++i) {
        var angle = 2 * Math.PI * i / count;
        points[i] = new PointF(rect.X + rect.Width / 2 * (1 + (float) Math.Cos(angle)),
                               rect.Y + rect.Height / 2 * (1 + (float) Math.Sin(angle)));
      }
      return points;
    }

    public static PointF[] RoundedRectangle(RectangleF rect, float topLeft, float topRight, float bottomRight, float bottomLeft) {
      var max = Math.Min(rect.Width, rect.Height) / 2;
      var points = new List<PointF>();
      addArc(points, rect.Left + Math.Min(topLeft, max), rect.Top + Math.Min(topLeft, max), Math.Min(topLeft, max), 180);
      addArc(points, rect.Right - Math.Min(topRight, max), rect.Top + Math.Min(topRight, max), Math.Min(topRight, max), 270);
      addArc(points, rect.Right - Math.Min(bottomRight, max), rect.Bottom - Math.Min(bottomRight, max), Math.Min(bottomRight, max), 0);
      addArc(points, rect.Left + Math.Min(bottomLeft, max), rect.Bottom - Math.Min(bottomLeft, max), Math.Min(bottomLeft, max), 90);
      return points.ToArray();
    }

    private static void addArc(List<PointF> points, float cx, float cy, float radius, float startDegrees) {
      var steps = Math.Max(2, (int) (radius * Math.PI / 2 / SAMPLE_SPACING));
      for (var i = 0; i <= steps; ++i) {
        var angle = (startDegrees + 90.0 * i / steps) * Math.PI / 180;
        points.Add(new PointF(cx + radius * (float) Math.Cos(angle), cy + radius * (float) Math.Sin(angle)));
      }
    }

    private static List<PointF> densify(IReadOnlyList<PointF> points, bool closed) {
      var result = new List<PointF>();
      var segments = closed ? points.Count : points.Count - 1;
      for (var i = 0; i < segments; ++i) {
        var a = points[i];
        var b = points[(i + 1) % points.Count];
        var steps = Math.Max(1, (int) Math.Ceiling(distance(a, b) / SAMPLE_SPACING));
        for (var j = 0; j < steps; ++j)
          result.Add(new PointF(a.X + (b.X - a.X) * j / steps, a.Y + (b.Y - a.Y) * j / steps));
      }
      if (!closed && points.Count > 0) result.Add(points[points.Count - 1]);
      return result;
    }

    private static PointF offset(IReadOnlyList<PointF> points, int index, bool closed, float offset) {
      var count = points.Count;
      var previous = closed ? points[(index - 1 + count) % count] : points[Math.Max(0, index - 1)];
      var next = closed ? points[(index + 1) % count] : points[Math.Min(count - 1, index + 1)];
      var tx = next.X - previous.X;
      var ty = next.Y - previous.Y;
      var length = (float) Math.Sqrt(tx * tx + ty * ty);
      if (length < 0.0001f) return points[index];
      return new PointF(points[index].X - ty / length * offset, points[index].Y + tx / length * offset);
    }

    private static float length(IReadOnlyList<PointF> points, bool closed) {
      var total = 0f;
      for (var i = 1; i < points.Count; ++i) total += distance(points[i - 1], points[i]);
      if (closed && points.Count > 1) total += distance(points[points.Count - 1], points[0]);
      return total;
    }

    private static float distance(PointF a, PointF b) {
      return (float) Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
    }

    private static float sin(double value) {
      return (float) Math.Sin(value);
    }

    private static float signed(Random random) {
      return (float) (random.NextDouble() * 2 - 1);
    }

    private static float clamp(float value, float min, float max) {
      return Math.Max(min, Math.Min(max, value));
    }
  }
}
