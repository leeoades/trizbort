using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Editor;

// Navigation, properties, connection label, join and direction commands (tickets 020, 022, 023, 028, 031, 045, 047, 048, 062).
public sealed partial class EditorSession
{
  // ---- Viewport helpers (020) ----

  /// <summary>
  ///   The room whose inner-bounds centre is nearest the viewport centre, or null when the map has no rooms. The
  ///   first room in map order wins a tie.
  /// </summary>
  public Room FindRoomNearestViewportCentre()
  {
    var centre = Viewport.Origin;
    Room closest = null;
    var closestDistance = float.MaxValue;
    foreach (var room in Project.Elements.OfType<Room>())
    {
      var distance = room.InnerBounds.Center.Distance(centre);
      if (!(distance < closestDistance)) continue;
      closest = room;
      closestDistance = distance;
    }
    return closest;
  }

  /// <summary>Centres the viewport on the element without changing the zoom.</summary>
  public void CenterOnElement(Element element)
  {
    if (element == null) return;

    var bounds = element is Room room ? room.InnerBounds : element.UnionBoundsWith(Rect.Empty, false);
    if (bounds == Rect.Empty) return;
    Viewport.CenterOn(bounds);
  }

  // ---- Selection (022) ----

  /// <summary>Selects every room in the regions of the selected rooms.</summary>
  public void SelectRegions()
  {
    var regions = Selection.Rooms.Select(room => room.Region).Distinct().ToList();
    if (regions.Count == 0) return;

    Selection.Set(Project.Elements.OfType<Room>().Where(room => regions.Contains(room.Region)).Cast<Element>().ToList());
  }

  public bool HasStartRoom => Project.Elements.OfType<Room>().Any(room => room.IsStartRoom);

  /// <summary>Selects the start room and centres the viewport on it.</summary>
  public void SelectStartRoom()
  {
    var startRoom = Project.Elements.OfType<Room>().FirstOrDefault(room => room.IsStartRoom);
    if (startRoom == null) return;

    SelectAndCentre(startRoom);
  }

  /// <summary>Selects the room nearest the viewport centre and centres the viewport on it.</summary>
  public void SelectRoomNearestViewportCentre()
  {
    var room = FindRoomNearestViewportCentre();
    if (room == null) return;

    SelectAndCentre(room);
  }

  private void SelectAndCentre(Element element)
  {
    Selection.Set(new Element[] { element });
    Project.ActiveSelectedElement = element;
    CenterOnElement(element);
  }

  // ---- Properties (023) ----

  /// <summary>True for the element kinds that have a properties dialog.</summary>
  public static bool HasPropertiesDialog(Element element)
  {
    return element is Room || element is Connection || element is MapLabel;
  }

  /// <summary>True when exactly one element is selected and it has a properties dialog.</summary>
  public bool CanShowProperties => Selection.Count == 1 && HasPropertiesDialog(Selection.Primary);

  /// <summary>
  ///   Asks <see cref="Prompts" /> to edit the element. Anything the dialog changes is one undo step; when the
  ///   dialog changes nothing (cancel) no step is recorded. Returns true when the user accepted.
  /// </summary>
  public async Task<bool> ShowPropertiesAsync(Element element)
  {
    if (!HasPropertiesDialog(element)) return false;

    var transaction = BeginTransaction("Properties", new[] { element });
    bool accepted;
    try
    {
      accepted = await Prompts.EditElementAsync(element);
    }
    catch
    {
      transaction.Cancel();
      transaction.Dispose();
      throw;
    }
    transaction.Dispose();
    RaiseChanged(EditorChange.Model);
    return accepted;
  }

  /// <summary>Properties of the single selected element.</summary>
  public Task<bool> ShowSelectedPropertiesAsync()
  {
    return CanShowProperties ? ShowPropertiesAsync(Selection.Primary) : Task.FromResult(false);
  }

  /// <summary>True when Enter does something: select the nearest room (nothing selected) or open properties.</summary>
  public bool CanEnter => Selection.Count == 0 ? Project.Elements.OfType<Room>().Any() : CanShowProperties;

  /// <summary>Enter: with nothing selected select the room nearest the viewport centre, else open properties.</summary>
  public Task<bool> EnterAsync()
  {
    if (Selection.Count == 0)
    {
      SelectRoomNearestViewportCentre();
      return Task.FromResult(false);
    }
    return ShowSelectedPropertiesAsync();
  }

  // ---- Connection labels (031) ----

  /// <summary>Apply a label to the selected connections and make it the default for new ones.</summary>
  public void SetConnectionLabel(ConnectionLabel label)
  {
    var connections = Selection.Connections.ToList();
    if (connections.Count > 0)
    {
      Execute("Line label", () =>
      {
        foreach (var connection in connections) connection.SetText(label);
      }, connections);
      RaiseChanged(EditorChange.Model);
    }
    NewConnection.Label = label;
  }

  /// <summary>The label a connection currently shows (None when its end texts match no label).</summary>
  public static ConnectionLabel LabelOf(Connection connection)
  {
    foreach (var label in new[] { ConnectionLabel.Up, ConnectionLabel.Down, ConnectionLabel.In, ConnectionLabel.Out })
    {
      Connection.GetText(label, out var start, out var end);
      if (connection.StartText == start && connection.EndText == end) return label;
    }
    return ConnectionLabel.None;
  }

  /// <summary>The label the check marks show: the primary selected connection's, else the new-connection default.</summary>
  public ConnectionLabel CurrentConnectionLabel
  {
    get { return Selection.Primary is Connection connection ? LabelOf(connection) : NewConnection.Label; }
  }

  // ---- Join rooms (028) ----

  /// <summary>True when exactly two rooms are selected and nothing connects them yet.</summary>
  public bool CanJoinRooms
  {
    get
    {
      var rooms = SelectedRooms;
      return rooms.Count == 2 && !Project.AreRoomsConnected(rooms.ToList());
    }
  }

  /// <summary>Connects the two selected rooms with facing compass ports, as one undo step.</summary>
  public Connection JoinSelectedRooms()
  {
    if (!CanJoinRooms) return null;

    var rooms = SelectedRooms;
    return JoinRooms(rooms[0], rooms[1]);
  }

  public Connection JoinRooms(Room room1, Room room2)
  {
    var rect1 = room1.InnerBounds;
    var rect2 = room2.InnerBounds;
    var dx = rect1.X - rect2.X;
    var dy = rect1.Y - rect2.Y;

    CompassPoint first;
    CompassPoint second;
    var vertical = !(dy == 0 && dx != 0) && (dx == 0 || System.Math.Abs(dy) >= System.Math.Abs(dx));
    if (vertical)
    {
      first = dy > 0 ? CompassPoint.North : CompassPoint.South;
      second = dy > 0 ? CompassPoint.South : CompassPoint.North;
    }
    else
    {
      first = dx > 0 ? CompassPoint.West : CompassPoint.East;
      second = dx > 0 ? CompassPoint.East : CompassPoint.West;
    }

    Connection connection = null;
    Execute("Join rooms", () =>
    {
      connection = new Connection(Project, new Vertex(room1.PortAt(first)), new Vertex(room2.PortAt(second)))
      {
        Style = NewConnection.Style,
        Flow = NewConnection.Flow
      };
      connection.SetText(NewConnection.Label);
      Project.Elements.Add(connection);
    }, new Element[] { room1, room2 });
    RaiseChanged(EditorChange.Model);
    return connection;
  }

  // ---- Direction navigation (047, 048) ----

  /// <summary>
  ///   The direction a room's connection port on this side is facing away from the room: the rough opposite of a
  ///   port compass point. Null in, null out.
  /// </summary>
  public static CompassPoint? RoughOpposite(CompassPoint? point)
  {
    if (point == null) return null;

    switch (point.Value)
    {
      case CompassPoint.North:
      case CompassPoint.NorthNorthEast:
      case CompassPoint.NorthNorthWest:
        return CompassPoint.South;
      case CompassPoint.South:
      case CompassPoint.SouthSouthEast:
      case CompassPoint.SouthSouthWest:
        return CompassPoint.North;
      case CompassPoint.East:
      case CompassPoint.EastSouthEast:
      case CompassPoint.EastNorthEast:
        return CompassPoint.West;
      case CompassPoint.West:
      case CompassPoint.WestSouthWest:
      case CompassPoint.WestNorthWest:
        return CompassPoint.East;
      case CompassPoint.SouthWest:
        return CompassPoint.NorthEast;
      case CompassPoint.SouthEast:
        return CompassPoint.NorthWest;
      case CompassPoint.NorthWest:
        return CompassPoint.SouthEast;
      case CompassPoint.NorthEast:
        return CompassPoint.SouthWest;
    }

    return CompassPoint.North;
  }

  /// <summary>True when exactly one room is selected.</summary>
  public bool HasSingleSelectedRoom => Selection.Count == 1 && Selection.Primary is Room;

  /// <summary>True when exactly one room or connection is selected (the start of direction navigation).</summary>
  public bool CanNavigateFromSelection =>
    Selection.Count == 1 && (Selection.Primary is Room || Selection.Primary is Connection);

  /// <summary>
  ///   The room reached from <paramref name="room" /> through a connection leaving on <paramref name="direction" />,
  ///   trying the direction itself, then anticlockwise, then clockwise. Null when there is none.
  /// </summary>
  public static Room FindRoomInDirection(Room room, CompassPoint direction)
  {
    return RoomAcrossConnections(room, direction)
           ?? RoomAcrossConnections(room, CompassPointHelper.RotateAntiClockwise(direction))
           ?? RoomAcrossConnections(room, CompassPointHelper.RotateClockwise(direction));
  }

  private static Room RoomAcrossConnections(Room room, CompassPoint direction)
  {
    foreach (var connection in room.GetConnections(direction))
    {
      foreach (var vertex in connection.VertexList)
      {
        var port = vertex.Port;
        if (port != null && port.Owner != room && port.Owner is Room other) return other;
      }
    }

    return null;
  }

  /// <summary>
  ///   Selects the room next to the single selected room in the given direction and centres it. Returns false (and
  ///   changes nothing) when no room is selected or none is found.
  /// </summary>
  public bool SelectRoomInDirection(CompassPoint direction)
  {
    if (!HasSingleSelectedRoom) return false;

    var next = FindRoomInDirection((Room)Selection.Primary, direction);
    if (next == null) return false;

    SelectAndCentre(next);
    return true;
  }

  /// <summary>
  ///   With a connection selected: selects the room at the end of the connection that points in the given direction
  ///   and centres it. Returns false when the selection is not one connection or no end points that way.
  /// </summary>
  public bool SelectRoomRelativeToSelectedConnection(CompassPoint direction)
  {
    if (Selection.Count != 1 || Selection.Primary is not Connection connection) return false;
    if (connection.VertexList.Count < 2) return false;

    for (var index = 0; index < 2; index++)
    {
      var port = connection.VertexList[index].Port;
      var compass = (port as Room.CompassPort)?.CompassPoint;
      var outDirection = RoughOpposite(compass);
      if (!PointsToward(outDirection, direction)) continue;

      var owner = port?.Owner;
      if (owner == null) return false;

      SelectAndCentre(owner);
      return true;
    }

    return false;
  }

  private static bool PointsToward(CompassPoint? outDirection, CompassPoint direction)
  {
    if (outDirection == null) return false;

    var diagonal = outDirection.Value;
    var overrideDirection =
      diagonal == CompassPoint.NorthEast && (direction == CompassPoint.North || direction == CompassPoint.East) ||
      diagonal == CompassPoint.NorthWest && (direction == CompassPoint.North || direction == CompassPoint.West) ||
      diagonal == CompassPoint.SouthEast && (direction == CompassPoint.South || direction == CompassPoint.East) ||
      diagonal == CompassPoint.SouthWest && (direction == CompassPoint.South || direction == CompassPoint.West);
    return overrideDirection || CompassPointHelper.IsSameApproximateDirection(direction, diagonal);
  }

  /// <summary>
  ///   Shift+arrow: with a room selected, selects a connection leaving it in the direction (else the one on the
  ///   clockwise side, else the anticlockwise side) and centres it; when there is no connection, selects the room
  ///   in that direction. With a connection selected, selects the room at its end pointing that way. Returns false
  ///   when nothing is found.
  /// </summary>
  public bool SelectConnectionOrRoomInDirection(CompassPoint direction)
  {
    if (Selection.Count != 1) return false;
    if (Selection.Primary is Connection) return SelectRoomRelativeToSelectedConnection(direction);
    if (Selection.Primary is not Room room) return false;

    var sides = new[]
    {
      direction, CompassPointHelper.RotateClockwise(direction), CompassPointHelper.RotateAntiClockwise(direction)
    };
    foreach (var side in sides)
    {
      var connection = room.GetConnections(side).FirstOrDefault();
      if (connection == null) continue;

      SelectAndCentre(connection);
      return true;
    }

    return SelectRoomInDirection(direction);
  }

  /// <summary>
  ///   Primary+arrow: selects the room in the direction; else, with a connection selected, the room at its end
  ///   pointing that way; else (room selected) connects to the room found at the target point or creates a new one
  ///   there. A new room copies the selected room's size, region, shape and style, is joined by a connection, is
  ///   selected and centred, and its properties open through <see cref="Prompts" />. Everything is one undo step.
  /// </summary>
  public async Task<bool> NavigateOrAddRoomAsync(CompassPoint direction)
  {
    if (SelectRoomInDirection(direction)) return true;
    if (SelectRoomRelativeToSelectedConnection(direction)) return true;
    if (!HasSingleSelectedRoom) return false;

    var room = (Room)Selection.Primary;
    var rect = room.InnerBounds;
    rect.Inflate(
      Settings.PreferredDistanceBetweenRooms + room.Width / 2,
      Settings.PreferredDistanceBetweenRooms + room.Height / 2);
    var centreOfNewRoom = rect.GetCorner(direction);
    var opposite = CompassPointHelper.GetOpposite(direction);

    var existing = new HitTester(this).HitElement(centreOfNewRoom) as Room;
    if (existing != null && existing != room)
    {
      Execute("Connect rooms", () => AddRoomConnection(room, direction, existing, opposite),
        new Element[] { room, existing });
      SelectAndCentre(existing);
      RaiseChanged(EditorChange.Model);
      return true;
    }

    var newRoom = RoomFactory.CreateNewRoom(Project, centreOfNewRoom, room, true);
    var transaction = BeginTransaction("Add room", new Element[] { room, newRoom });
    try
    {
      Project.Elements.Add(newRoom);
      AddRoomConnection(room, direction, newRoom, opposite);
      SelectAndCentre(newRoom);
      RaiseChanged(EditorChange.Model);
      await Prompts.EditElementAsync(newRoom);
    }
    catch
    {
      transaction.Cancel();
      transaction.Dispose();
      throw;
    }
    transaction.Dispose();
    RaiseChanged(EditorChange.Model);
    return true;
  }

  private Connection AddRoomConnection(Room from, CompassPoint fromPoint, Room to, CompassPoint toPoint)
  {
    var connection = new Connection(Project, new Vertex(from.PortAt(fromPoint)), new Vertex(to.PortAt(toPoint)))
    {
      Style = NewConnection.Style,
      Flow = NewConnection.Flow
    };
    // A keypad / Ctrl+arrow connection runs between compass points (never up or down), so it carries no end text
    // even when an Up / Down / In / Out label was chosen earlier as the default for connections drawn by hand (I-029).
    Project.Elements.Add(connection);
    return connection;
  }

  // ---- Active element cycling (045) ----

  /// <summary>True when more than one element is selected, so there is another one to make active.</summary>
  public bool CanCycleActiveElement => Selection.Count > 1;

  /// <summary>
  ///   F3 / Shift+F3: makes the next (or previous) selected element the active one, wrapping round, and centres
  ///   it. The selection itself does not change. Does nothing unless several elements are selected.
  /// </summary>
  public void CycleActiveElement(bool forward)
  {
    var items = Selection.Items;
    if (items.Count < 2) return;

    var index = -1;
    for (var i = 0; i < items.Count; i++)
    {
      if (ReferenceEquals(items[i], Project.ActiveSelectedElement)) index = i;
    }
    if (index < 0) index = items.Count - 1;

    var next = forward ? (index + 1) % items.Count : (index - 1 + items.Count) % items.Count;
    Project.ActiveSelectedElement = items[next];
    CenterOnElement(items[next]);
    RaiseChanged(EditorChange.Selection);
  }

  // ---- Reference rooms (062) ----

  /// <summary>
  ///   Ctrl+click on a selected reference room: selects the room it refers to and centres it. Returns false when
  ///   <paramref name="room" /> is not a reference room.
  /// </summary>
  public bool JumpToReferencedRoom(Room room)
  {
    var original = room?.ReferenceRoom;
    if (original == null) return false;

    SelectAndCentre(original);
    return true;
  }
}
