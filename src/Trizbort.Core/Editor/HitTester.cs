using System;
using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Editor;

/// <summary>
///   The result of <see cref="HitTester.HitAll" />: whatever lies under a point, by kind.
/// </summary>
public sealed record HitResult(Element Element, ResizeHandle Handle, Port Port, CurveWaypoint? Waypoint);

/// <summary>
///   Pure "what is under this world point" queries over an <see cref="EditorSession" />: elements (ZOrder-aware),
///   resize handles, ports and curve waypoints. No UI types; all coordinates are world coordinates.
/// </summary>
public sealed class HitTester
{
  private static readonly CompassPoint[] _handlePoints =
  {
    CompassPoint.NorthWest, CompassPoint.North, CompassPoint.NorthEast,
    CompassPoint.East, CompassPoint.SouthEast, CompassPoint.South,
    CompassPoint.SouthWest, CompassPoint.West
  };

  private static readonly CurveWaypoint[] _allWaypoints =
  {
    CurveWaypoint.Quarter, CurveWaypoint.ThreeQuarter, CurveWaypoint.Middle
  };

  private readonly EditorSession _session;

  public HitTester(EditorSession session)
  {
    _session = session;
  }

  /// <summary>
  ///   Topmost element within <c>Settings.SnapToElementSize</c> of the point. Among several candidates the one
  ///   that sorts last by <see cref="Element.CompareTo" /> (depth, ZOrder, id) wins, with later list position
  ///   breaking ties; being nearer does not beat being on top (legacy behaviour). <paramref name="exclude" />
  ///   is skipped.
  /// </summary>
  public Element HitElement(Vector world, bool includeMargins = false, Element exclude = null)
  {
    // Matches the reference (legacy Canvas.HitTestElement): the nearest element within snap
    // distance wins; only elements at the same distance are decided by drawing order (topmost).
    Element best = null;
    var bestDistance = float.MaxValue;
    var snap = Settings.SnapToElementSize;
    foreach (var element in _session.Project.Elements)
    {
      if (element == exclude) continue;
      var distance = element.Distance(world, includeMargins);
      if (distance > snap) continue;
      if (best != null && Math.Abs(distance - bestDistance) < 0.001f)
      {
        if (element.CompareTo(best) >= 0) best = element;
      }
      else if (distance < bestDistance)
      {
        best = element;
        bestDistance = distance;
      }
    }

    return best;
  }

  /// <summary>Elements intersecting the world rectangle (marquee selection).</summary>
  public IReadOnlyList<Element> HitRect(Rect world)
  {
    return _session.Project.Elements.Where(e => e.Intersects(world)).ToList();
  }

  /// <summary>The eight resize handles when exactly one <see cref="ISizeable" /> is selected, else none.</summary>
  public IReadOnlyList<ResizeHandle> ActiveHandles()
  {
    var result = new List<ResizeHandle>();
    if (_session.Selection.Count == 1 && _session.Selection.Items[0] is ISizeable sizeable)
    {
      foreach (var point in _handlePoints) result.Add(new ResizeHandle(point, sizeable));
    }

    return result;
  }

  public ResizeHandle HitHandle(Vector world)
  {
    return HitHandle(world, ActiveHandles());
  }

  /// <summary>Hit-test the given handles (last first).</summary>
  public ResizeHandle HitHandle(Vector world, IReadOnlyList<ResizeHandle> handles)
  {
    for (var i = handles.Count - 1; i >= 0; i--)
    {
      if (handles[i].HitTest(world)) return handles[i];
    }

    return null;
  }

  /// <summary>
  ///   Ports to show: those of the hovered room or label (when nothing is selected, or while a port is being
  ///   moved) and the movable ports of the single selected connection.
  /// </summary>
  public IReadOnlyList<Port> ActivePorts(Element hoverRoom, bool movingPort = false)
  {
    var result = new List<Port>();
    var selection = _session.Selection;
    var hoverIsRoomOrLabel = hoverRoom is Room || hoverRoom is MapLabel;

    if (movingPort)
    {
      if (hoverIsRoomOrLabel) result.AddRange(hoverRoom.PortList);
    }
    else
    {
      if (hoverIsRoomOrLabel && !selection.Contains(hoverRoom) && selection.Count == 0)
        result.AddRange(hoverRoom.PortList);

      if (selection.Count == 1 && selection.Items[0] is Connection connection)
        result.AddRange(connection.PortList.OfType<MoveablePort>());
    }

    return result;
  }

  public Port HitPort(Vector world, Element hoverRoom = null)
  {
    return HitPort(world, ActivePorts(hoverRoom));
  }

  /// <summary>
  ///   Nearest of the given ports within snap distance. Inside the owner's bounds only an exact hit counts, or
  ///   anything when <paramref name="movingPort" /> is set. <paramref name="exclude" /> is skipped.
  /// </summary>
  public Port HitPort(Vector world, IEnumerable<Port> ports, bool movingPort = false, Port exclude = null)
  {
    Port closest = null;
    var closestDistance = float.MaxValue;
    var snap = Settings.SnapToElementSize;

    foreach (var port in ports)
    {
      if (port == exclude) continue;

      var distance = port.Distance(world);
      var curSnap = snap;
      var bounds = port.Owner.UnionBoundsWith(Rect.Empty, true);
      if (bounds.Contains(world)) curSnap = movingPort ? float.MaxValue : 0;

      if (distance <= curSnap && distance < closestDistance)
      {
        closest = port;
        closestDistance = distance;
      }
    }

    return closest;
  }

  /// <summary>Waypoints of the connection that are set or can be added.</summary>
  public IEnumerable<CurveWaypoint> VisibleWaypoints(Connection connection)
  {
    foreach (var waypoint in _allWaypoints)
    {
      if (connection.GetCurveWaypoint(waypoint).HasValue || connection.CanAddCurveWaypoint(waypoint))
        yield return waypoint;
    }
  }

  public Rect WaypointBounds(Connection connection, CurveWaypoint waypoint)
  {
    var isSet = connection.GetCurveWaypoint(waypoint).HasValue;
    var size = Settings.HandleSize * (isSet ? 2.0f : 1.5f);
    var pos = connection.GetCurveWaypointHandlePosition(waypoint);
    return new Rect(pos.X - size / 2, pos.Y - size / 2, size, size);
  }

  /// <summary>Waypoint handle under the point; only when a single connection is selected.</summary>
  public CurveWaypoint? HitWaypoint(Vector world)
  {
    var selection = _session.Selection;
    if (selection.Count == 1 && selection.Items[0] is Connection connection)
    {
      foreach (var waypoint in VisibleWaypoints(connection))
      {
        var bounds = WaypointBounds(connection, waypoint);
        bounds.Inflate(Settings.HandleSize / 2f);
        if (bounds.Contains(world)) return waypoint;
      }
    }

    return null;
  }

  /// <summary>
  ///   Everything under the point. Press priority (legacy order) is waypoint, then handle, then port, then
  ///   element; callers should act on the first non-null in that order. The previous Avalonia order was
  ///   handle, port, waypoint, element.
  /// </summary>
  public HitResult HitAll(Vector world, Element hoverRoom = null)
  {
    var waypoint = HitWaypoint(world);
    var handle = HitHandle(world);
    var port = HitPort(world, hoverRoom);
    var element = HitElement(world);
    return new HitResult(element, handle, port, waypoint);
  }
}
