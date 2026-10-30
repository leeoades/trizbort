using System;
using System.Collections.Generic;

namespace Trizbort.Domain.Misc;

/// <summary>
///   Centripetal Catmull-Rom spline helpers used to bend connections through waypoints.
/// </summary>
public static class CurveGeometry
{
  /// <summary>
  ///   Evaluate the spline span running from <paramref name="p1" /> to <paramref name="p2" />
  ///   at parameter <paramref name="t" /> (0..1), using <paramref name="p0" /> and
  ///   <paramref name="p3" /> as the neighbouring control points.
  /// </summary>
  public static Vector Evaluate(Vector p0, Vector p1, Vector p2, Vector p3, float t)
  {
    const float minKnotGap = 1e-3f;
    var t0 = 0f;
    var t1 = t0 + Math.Max(KnotGap(p0, p1), minKnotGap);
    var t2 = t1 + Math.Max(KnotGap(p1, p2), minKnotGap);
    var t3 = t2 + Math.Max(KnotGap(p2, p3), minKnotGap);
    var u = t1 + (t2 - t1) * t;

    var a1 = Lerp(p0, p1, t0, t1, u);
    var a2 = Lerp(p1, p2, t1, t2, u);
    var a3 = Lerp(p2, p3, t2, t3, u);
    var b1 = Lerp(a1, a2, t0, t2, u);
    var b2 = Lerp(a2, a3, t1, t3, u);
    return Lerp(b1, b2, t1, t2, u);
  }

  /// <summary>
  ///   Flatten a spline passing through every point in <paramref name="points" /> into one
  ///   polyline per span. <paramref name="before" /> and <paramref name="after" /> are phantom
  ///   control points that shape the tangent at the first and last points.
  /// </summary>
  public static List<List<Vector>> Flatten(IList<Vector> points, Vector before, Vector after, int subdivisions)
  {
    var spans = new List<List<Vector>>();
    for (var i = 0; i < points.Count - 1; ++i)
    {
      var p0 = i == 0 ? before : points[i - 1];
      var p3 = i + 2 < points.Count ? points[i + 2] : after;
      var span = new List<Vector>(subdivisions + 1);
      for (var step = 0; step <= subdivisions; ++step)
        span.Add(Evaluate(p0, points[i], points[i + 1], p3, step / (float)subdivisions));
      spans.Add(span);
    }

    return spans;
  }

  /// <summary>
  ///   Find the point half way along a polyline, and the direction of travel at that point.
  /// </summary>
  public static Vector PolylineMidpoint(IList<Vector> polyline, out Vector direction)
  {
    direction = Vector.Zero;
    if (polyline.Count == 0) return Vector.Zero;
    if (polyline.Count == 1) return polyline[0];

    var total = 0f;
    for (var i = 1; i < polyline.Count; ++i) total += polyline[i].Distance(polyline[i - 1]);

    var remaining = total / 2;
    for (var i = 1; i < polyline.Count; ++i)
    {
      var delta = polyline[i] - polyline[i - 1];
      var length = delta.Length;
      if (length <= 0) continue;
      direction = delta;
      if (remaining <= length) return polyline[i - 1] + delta * (remaining / length);
      remaining -= length;
    }

    return polyline[polyline.Count - 1];
  }

  private static float KnotGap(Vector a, Vector b)
  {
    return (float)Math.Sqrt(a.Distance(b));
  }

  private static Vector Lerp(Vector a, Vector b, float ta, float tb, float u)
  {
    return a * ((tb - u) / (tb - ta)) + b * ((u - ta) / (tb - ta));
  }
}