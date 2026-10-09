using System.Collections.Generic;

namespace Trizbort.Domain.Misc;

/// <summary>
///   The context in which an object's drawing is taking place.
/// </summary>
public class DrawingContext {
  public DrawingContext(float zoomFactor)
  {
    ZoomFactor = zoomFactor;
  }

  /// <summary>
  ///   Get/set whether the object should be drawn as if hovered over.
  /// </summary>
  public bool Hover { get; set; }

  /// <summary>
  ///   Get the list of lines drawn so far.
  /// </summary>
  public List<LineSegment> LinesDrawn { get; } = new();

  /// <summary>
  ///   Get/set whether the object should be drawn as if selected.
  /// </summary>
  public bool Selected { get; set; }

  /// <summary>
  ///   Get/set whether to draw smart line segments, as opposed to simple ones.
  /// </summary>
  public bool UseSmartLineSegments { get; set; }

  /// <summary>
  ///   Get/set the zoom factor at which drawing is taking place.
  /// </summary>
  public float ZoomFactor { get; set; }
}