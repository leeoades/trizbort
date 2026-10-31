using System.Collections.Generic;
using Trizbort.Domain.AppSettings;
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

/// <summary>The kind of element a tooltip describes; hosts colour the tooltip by it (rooms light blue, connections pale yellow).</summary>
public enum TooltipElementKind
{
  Other,
  Room,
  Connection
}

/// <summary>What the canvas tooltip shows: title, body and footer are styled separately by the host.</summary>
public sealed record TooltipModel(string Title, string Body, string Footer, TooltipElementKind Kind, Element Element)
{
  /// <summary>
  ///   The tooltip for <paramref name="element" />, or null when tooltips are off, the element has none, or it has
  ///   neither a title nor a body.
  /// </summary>
  public static TooltipModel Create(Element element)
  {
    if (element == null || !ApplicationSettingsController.AppSettings.ShowTooltips || !element.HasTooltip()) return null;

    if (element is Room room) room.CheckValidation();

    var title = element.GetToolTipHeader() ?? string.Empty;
    var body = element.GetToolTipText() ?? string.Empty;
    if (title.Length == 0 && body.Length == 0) return null;

    var kind = element is Room ? TooltipElementKind.Room
      : element is Connection ? TooltipElementKind.Connection
      : TooltipElementKind.Other;
    return new TooltipModel(title, body, element.GetToolTipFooter() ?? string.Empty, kind, element);
  }
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
  TooltipModel Tooltip)
{
  /// <summary>The element the tooltip describes, or null when no tooltip is shown.</summary>
  public Element TooltipTarget => Tooltip?.Element;

  public static AdornerState Empty { get; } = new(
    null, null, null, null, null,
    new List<ResizeHandle>(), new List<Port>(),
    null, null, CursorKind.Default, null);
}
