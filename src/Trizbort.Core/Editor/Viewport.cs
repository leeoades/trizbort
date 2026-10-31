using System;
using System.Collections.Generic;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;

namespace Trizbort.Editor;

/// <summary>
///   View state of the map canvas: origin (world point at the viewport centre), zoom and size.
/// </summary>
public sealed class Viewport
{
  private const float MinZoom = 0.1f;
  private const float MaxZoom = 10.0f;
  private const float ZoomFactorStep = 1.25f;
  private const float MicroZoomStep = 0.01f;

  /// <summary>Client pixels a trackpad scroll delta of 1.0 pans the view.</summary>
  public const float ScrollPanPixelsPerUnit = 40f;

  private readonly CanvasTransform _transform = new()
  {
    ViewportWidth = 800,
    ViewportHeight = 600
  };

  public Vector Origin => _transform.Origin;

  public float ZoomFactor => _transform.ZoomFactor;

  public double Width => _transform.ViewportWidth;

  public double Height => _transform.ViewportHeight;

  public Rect VisibleWorldBounds => _transform.GetVisibleWorldBounds();

  public event EventHandler Changed;

  private Func<IEnumerable<Element>> _fitSource;
  private bool _fitting;

  /// <summary>
  ///   Fit the map to the view now, and again whenever the viewport is resized until the view is changed by anything
  ///   else (pan, zoom, centre). A map opened at start up is loaded before the window has its real size.
  /// </summary>
  public void FitToMapAndKeepFitting(Func<IEnumerable<Element>> elements)
  {
    if (elements == null) throw new ArgumentNullException(nameof(elements));
    ApplyFit(elements);
    _fitSource = elements;
  }

  private void ApplyFit(Func<IEnumerable<Element>> elements)
  {
    _fitting = true;
    try
    {
      FitToMap(elements());
    }
    finally
    {
      _fitting = false;
    }
  }

  public Vector CanvasToClient(Vector world)
  {
    return _transform.CanvasToClient(world);
  }

  public Vector ClientToCanvas(Vector client)
  {
    return _transform.ClientToCanvas(client);
  }

  public void SetSize(double width, double height)
  {
    if (width <= 0 || height <= 0) return;
    if (width == _transform.ViewportWidth && height == _transform.ViewportHeight) return;

    _transform.ViewportWidth = width;
    _transform.ViewportHeight = height;
    RaiseChanged();
    if (_fitSource != null) ApplyFit(_fitSource);
  }

  public void SetView(Vector origin, float zoom)
  {
    if (!_fitting) _fitSource = null;
    var clamped = Math.Clamp(zoom, MinZoom, MaxZoom);
    if (_transform.Origin == origin && Math.Abs(_transform.ZoomFactor - clamped) <= 0.0001f) return;

    _transform.Origin = origin;
    _transform.ZoomFactor = clamped;
    RaiseChanged();
  }

  public void Pan(Vector worldDelta)
  {
    SetView(_transform.Origin + worldDelta, _transform.ZoomFactor);
  }

  public void PanClient(Vector clientDelta)
  {
    Pan(clientDelta / _transform.ZoomFactor);
  }

  public void ZoomIn()
  {
    SetView(_transform.Origin, (float)Math.Round(_transform.ZoomFactor * ZoomFactorStep, 2));
  }

  public void ZoomOut()
  {
    SetView(_transform.Origin, (float)Math.Round(_transform.ZoomFactor / ZoomFactorStep, 2));
  }

  /// <summary>Set the zoom factor (clamped to 0.1-10), keeping the viewport centre (<see cref="Origin" />) fixed.</summary>
  public void SetZoom(double zoom)
  {
    SetView(_transform.Origin, (float)zoom);
  }

  /// <summary>Zoom in by 0.01 (the reference "+1%" menu item).</summary>
  public void ZoomMicroIn()
  {
    SetView(_transform.Origin, (float)Math.Round(_transform.ZoomFactor + MicroZoomStep, 2));
  }

  /// <summary>Zoom out by 0.01 (the reference "-1%" menu item).</summary>
  public void ZoomMicroOut()
  {
    SetView(_transform.Origin, (float)Math.Round(_transform.ZoomFactor - MicroZoomStep, 2));
  }

  /// <summary>
  ///   Pan by a fraction of the viewport size in each axis (<paramref name="unit" /> is e.g. (1, 0) for right). Uses the
  ///   viewport width for horizontal and its height for vertical moves.
  /// </summary>
  public void PanByViewportFraction(Vector unit, double fraction)
  {
    PanClient(new Vector((float)(unit.X * Width * fraction), (float)(unit.Y * Height * fraction)));
  }

  public void ZoomBy(float factor, Vector clientPoint)
  {
    ZoomTo(_transform.ZoomFactor * factor, clientPoint);
  }

  /// <summary>
  ///   Smooth zoom about <paramref name="clientPoint" />: the zoom changes by 1.25 to the power of the delta, which is
  ///   clamped to +/-1 per event (a whole wheel notch is one step, a 0.1 trackpad tick a tenth of a step).
  /// </summary>
  public void ZoomSmooth(double wheelDelta, Vector clientPoint)
  {
    if (double.IsNaN(wheelDelta) || wheelDelta == 0) return;
    var delta = Math.Clamp(wheelDelta, -1.0, 1.0);
    ZoomBy((float)Math.Pow(ZoomFactorStep, delta), clientPoint);
  }

  /// <summary>Pan by a trackpad scroll delta; the content follows the fingers (the OS already applies natural direction).</summary>
  public void ScrollPan(double deltaX, double deltaY)
  {
    PanClient(new Vector((float)(-deltaX * ScrollPanPixelsPerUnit), (float)(-deltaY * ScrollPanPixelsPerUnit)));
  }

  public void ZoomWheel(int direction, Vector clientPoint, bool micro, bool invertWheel)
  {
    var sign = Math.Sign(direction);
    if (sign == 0) return;
    if (invertWheel) sign = -sign;

    if (micro)
    {
      ZoomTo(_transform.ZoomFactor + sign * MicroZoomStep, clientPoint);
    }
    else
    {
      ZoomBy(sign > 0 ? ZoomFactorStep : 1.0f / ZoomFactorStep, clientPoint);
    }
  }

  public void ResetZoom()
  {
    SetView(Vector.Zero, 1.0f);
  }

  public void FitToMap(IEnumerable<Element> elements)
  {
    var fit = new CanvasTransform
    {
      ViewportWidth = _transform.ViewportWidth,
      ViewportHeight = _transform.ViewportHeight
    };
    fit.FitToMap(elements);
    SetView(fit.Origin, fit.ZoomFactor);
  }

  public void CenterOn(Rect worldBounds)
  {
    SetView(worldBounds.Center, _transform.ZoomFactor);
  }

  public static Rect ComputeCanvasBounds(IEnumerable<Element> elements, float padding = 0)
  {
    if (elements == null) return Rect.Empty;

    var bounds = Rect.Empty;
    var hasElements = false;
    foreach (var element in elements)
    {
      bounds = element.UnionBoundsWith(bounds, true);
      hasElements = true;
    }

    if (!hasElements) return Rect.Empty;

    if (padding > 0)
    {
      bounds.Inflate(padding);
    }
    return bounds;
  }

  private void ZoomTo(float newZoom, Vector clientPoint)
  {
    var before = _transform.Origin;
    var beforeZoom = _transform.ZoomFactor;
    _transform.ZoomAt(clientPoint, newZoom);
    if (_transform.Origin == before && Math.Abs(_transform.ZoomFactor - beforeZoom) <= 0.0001f)
    {
      _transform.ZoomFactor = beforeZoom;
      return;
    }
    RaiseChanged();
  }

  private void RaiseChanged()
  {
    var handler = Changed;
    handler?.Invoke(this, EventArgs.Empty);
  }
}
