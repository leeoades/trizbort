using System;
using System.Collections.Generic;
using Trizbort.Domain.Misc;

namespace Trizbort.Editor;

/// <summary>Movement of a multi-touch gesture since the previous step.</summary>
/// <param name="CentroidDelta">How far the centroid of the active touches moved, in client pixels.</param>
/// <param name="Scale">Ratio of the new finger spread to the previous one (1 = unchanged, above 1 = fingers moved apart).</param>
/// <param name="Centroid">The current centroid in client pixels.</param>
public readonly record struct TouchGestureStep(Vector CentroidDelta, float Scale, Vector Centroid);

/// <summary>
///   Tracks active touch pointers (by id) and, while two or more are down, reports the centroid movement and the
///   change in finger spread between updates. Pure and UI-free.
/// </summary>
public sealed class MultiTouchTracker
{
  private const float MinSpread = 0.001f;

  private readonly Dictionary<int, Vector> _points = new();
  private Vector _lastCentroid;
  private float _lastSpread;

  /// <summary>Number of touches currently down.</summary>
  public int Count => _points.Count;

  /// <summary>True while two or more touches are down.</summary>
  public bool IsMultiTouch => _points.Count >= 2;

  public void Press(int id, Vector position)
  {
    _points[id] = position;
    ResetBaseline();
  }

  public void Release(int id)
  {
    if (_points.Remove(id)) ResetBaseline();
  }

  public void Clear()
  {
    _points.Clear();
    ResetBaseline();
  }

  /// <summary>
  ///   Record a touch move. Returns true (with the step since the last update) when two or more touches are down
  ///   and the pointer is known; otherwise the position is stored (if known) and false is returned.
  /// </summary>
  public bool Move(int id, Vector position, out TouchGestureStep step)
  {
    step = default;
    if (!_points.ContainsKey(id)) return false;

    _points[id] = position;
    if (_points.Count < 2) return false;

    var centroid = Centroid();
    var spread = Spread(centroid);
    var scale = _lastSpread > MinSpread ? spread / _lastSpread : 1f;
    step = new TouchGestureStep(centroid - _lastCentroid, scale, centroid);
    _lastCentroid = centroid;
    _lastSpread = spread;
    return true;
  }

  private void ResetBaseline()
  {
    if (_points.Count == 0)
    {
      _lastCentroid = Vector.Zero;
      _lastSpread = 0;
      return;
    }

    _lastCentroid = Centroid();
    _lastSpread = Spread(_lastCentroid);
  }

  private Vector Centroid()
  {
    float x = 0, y = 0;
    foreach (var p in _points.Values)
    {
      x += p.X;
      y += p.Y;
    }

    return new Vector(x / _points.Count, y / _points.Count);
  }

  private float Spread(Vector centroid)
  {
    float total = 0;
    foreach (var p in _points.Values)
    {
      var d = p - centroid;
      total += (float)Math.Sqrt(d.X * d.X + d.Y * d.Y);
    }

    return total / _points.Count;
  }
}
