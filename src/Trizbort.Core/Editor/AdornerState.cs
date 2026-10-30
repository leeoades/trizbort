using System.Collections.Generic;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;

namespace Trizbort.Editor;

/// <summary>
///   Cursor shapes the canvas asks for. Mapped by the UI host; the Avalonia mapping (StandardCursorType) is:
///   Default = Arrow, Hand = Hand, SizeAll = SizeAll, Pan = Hand, Cross = Cross, ResizeNWSE = TopLeftCorner,
///   ResizeNESW = TopRightCorner, ResizeNS = SizeNorthSouth, ResizeEW = SizeWestEast, DrawLine = Cross,
///   NoMove = No.
/// </summary>
public enum CursorKind
{
  Default,
  Hand,
  SizeAll,
  Pan,
  Cross,
  ResizeNWSE,
  ResizeNESW,
  ResizeNS,
  ResizeEW,
  DrawLine,
  NoMove
}

/// <summary>A connection being dragged out of <see cref="From" />, currently ending at world point <see cref="End" />.</summary>
public readonly record struct PendingLine(Port From, Vector End);

/// <summary>
///   Immutable snapshot of everything the canvas draws on top of the map for interaction: hover state, handles,
///   ports, marquee, pending connection, cursor and tooltip target. Rebuilt by <see cref="CanvasInteraction" />.
/// </summary>
public sealed record AdornerState(
  Element HoverElement,
  Port HoverPort,
  ResizeHandle HoverHandle,
  CurveWaypoint? HoverWaypoint,
  CurveWaypoint? DragWaypoint,
  IReadOnlyList<ResizeHandle> Handles,
  IReadOnlyList<Port> Ports,
  Rect? Marquee,
  PendingLine? PendingConnection,
  CursorKind Cursor,
  Element TooltipTarget)
{
  public static AdornerState Empty { get; } = new(
    null, null, null, null, null,
    new List<ResizeHandle>(), new List<Port>(),
    null, null, CursorKind.Default, null);
}
