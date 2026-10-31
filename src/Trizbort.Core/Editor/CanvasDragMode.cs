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
  MoveWaypoint
}
