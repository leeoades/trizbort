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
  private const float ZoomStep = 1.25f;
  private const float MicroZoomStep = 0.01f;

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
  }

  public void SetView(Vector origin, float zoom)
  {
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
    SetView(_transform.Origin, (float)Math.Round(_transform.ZoomFactor * ZoomStep, 2));
  }

  public void ZoomOut()
  {
    SetView(_transform.Origin, (float)Math.Round(_transform.ZoomFactor / ZoomStep, 2));
  }

  public void ZoomBy(float factor, Vector clientPoint)
  {
    ZoomTo(_transform.ZoomFactor * factor, clientPoint);
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
      ZoomBy(sign > 0 ? ZoomStep : 1.0f / ZoomStep, clientPoint);
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
