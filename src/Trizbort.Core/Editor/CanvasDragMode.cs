namespace Trizbort.Editor;

/// <summary>What a pointer gesture on the canvas is currently doing.</summary>
public enum CanvasDragMode
{
  None,
  Pan,
  Marquee,
  MoveElement,
  MoveResizeHandle,
  MovePort,
  MoveWaypoint,
  MoveObjectList,
  /// <summary>Dragging a split join's label ("to Debris Room") to a new place beside its room.</summary>
  MoveSplitLabel
}
