using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;

namespace Trizbort.Editor;

// Navigation, properties, connection label and join commands (tickets 020, 022, 023, 028, 031).
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

  private void SelectAndCentre(Room room)
  {
    Selection.Set(new Element[] { room });
    Project.ActiveSelectedElement = room;
    CenterOnElement(room);
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
}
