using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Controllers;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;

namespace Trizbort.Editor;

// Room commands (tickets 025, 026, 027, 029, 037, 115). Every operation is one undo step.
public sealed partial class EditorSession
{
  private IEditorPrompts _prompts = NonInteractiveEditorPrompts.Instance;

  /// <summary>Asks the user questions such as "edit this new room". Never null.</summary>
  public IEditorPrompts Prompts
  {
    get => _prompts;
    set => _prompts = value ?? NonInteractiveEditorPrompts.Instance;
  }

  /// <summary>The selected rooms in selection order (the last one is the primary element).</summary>
  public IReadOnlyList<Room> SelectedRooms => Selection.Rooms.ToList();

  /// <summary>The primary selected element when it is a room, otherwise null.</summary>
  public Room PrimaryRoom => Selection.Primary as Room;

  // ---- Lighting (025) ----

  public void ToggleDarkness()
  {
    var rooms = SelectedRooms.ToList();
    if (rooms.Count == 0) return;
    Execute("Toggle darkness", () => new RoomController().ToggleDarkness(rooms), rooms);
    RaiseChanged(EditorChange.Model);
  }

  public void ForceDarkness()
  {
    var rooms = SelectedRooms.ToList();
    if (rooms.Count == 0) return;
    Execute("Force darkness", () => new RoomController().ForceDarkness(rooms), rooms);
    RaiseChanged(EditorChange.Model);
  }

  public void ForceLighted()
  {
    var rooms = SelectedRooms.ToList();
    if (rooms.Count == 0) return;
    Execute("Force lighted", () => new RoomController().ForceLighted(rooms), rooms);
    RaiseChanged(EditorChange.Model);
  }

  // ---- Start / end room (026) ----

  /// <summary>
  ///   With exactly one room selected: clear the start flag if it is the start room, otherwise make it the only
  ///   start room (legacy toggle behaviour).
  /// </summary>
  public void SetStartRoom()
  {
    var rooms = SelectedRooms;
    if (rooms.Count != 1) return;

    var room = rooms[0];
    var touched = new List<Element> { room };
    Execute("Set start room", () =>
    {
      if (room.IsStartRoom)
      {
        room.IsStartRoom = false;
        return;
      }

      foreach (var other in Project.Elements.OfType<Room>().Where(r => r.IsStartRoom).ToList())
      {
        other.IsStartRoom = false;
        touched.Add(other);
      }
      room.IsStartRoom = true;
    }, touched);
    RaiseChanged(EditorChange.Model);
  }

  /// <summary>Toggle the end-room flag of every selected room.</summary>
  public void ToggleEndRoom()
  {
    var rooms = SelectedRooms.ToList();
    if (rooms.Count == 0) return;
    Execute("Set end room", () =>
    {
      foreach (var room in rooms) room.IsEndRoom = !room.IsEndRoom;
    }, rooms);
    RaiseChanged(EditorChange.Model);
  }

  // ---- Shapes (027) ----

  public void SetRoomShape(RoomShape shape)
  {
    var rooms = SelectedRooms.ToList();
    if (rooms.Count == 0) return;
    Execute("Room shape", () => new RoomController().SetRoomShape(rooms, shape), rooms);
    RaiseChanged(EditorChange.Model);
  }

  // ---- Swaps (029) ----

  public void SwapRoomObjects()
  {
    SwapSelectedRooms("Swap objects", (a, b) =>
    {
      var objects = a.Objects;
      a.Objects = b.Objects;
      b.Objects = objects;
    });
  }

  public void SwapRoomNames()
  {
    SwapSelectedRooms("Swap names", (a, b) =>
    {
      var name = a.Name;
      a.Name = b.Name;
      b.Name = name;
    });
  }

  /// <summary>Swap border style, border colour, fill, second fill colour and location, and shape.</summary>
  public void SwapRoomFormats()
  {
    SwapSelectedRooms("Swap formats", (a, b) =>
    {
      var borderStyle = a.BorderStyle;
      var borderColor = a.RoomBorderColor;
      var fill = a.RoomFillColor;
      var secondFill = a.SecondFillColor;
      var secondLocation = a.SecondFillLocation;
      var shape = a.Shape;

      a.BorderStyle = b.BorderStyle;
      a.RoomBorderColor = b.RoomBorderColor;
      a.RoomFillColor = b.RoomFillColor;
      a.SecondFillLocation = b.SecondFillLocation;
      a.SecondFillColor = b.SecondFillColor;
      a.Shape = b.Shape;

      b.BorderStyle = borderStyle;
      b.RoomBorderColor = borderColor;
      b.RoomFillColor = fill;
      b.SecondFillLocation = secondLocation;
      b.SecondFillColor = secondFill;
      b.Shape = shape;
    });
  }

  public void SwapRoomRegions()
  {
    SwapSelectedRooms("Swap regions", (a, b) =>
    {
      var region = a.Region;
      a.Region = b.Region;
      b.Region = region;
    });
  }

  private void SwapSelectedRooms(string title, Action<Room, Room> swap)
  {
    var rooms = SelectedRooms;
    if (rooms.Count != 2) return;

    Execute(title, () => swap(rooms[0], rooms[1]), rooms);
    RaiseChanged(EditorChange.Model);
  }

  // ---- Add room (037, 115) ----

  /// <summary>
  ///   Add a room centred on <paramref name="position" /> (map space; the viewport centre when null), using the
  ///   new-room defaults (see <see cref="RoomFactory" />). When a single connection is selected the room is
  ///   spliced into it. The room becomes the selection. One undo step; no dialog.
  /// </summary>
  public Room AddRoom(Vector? position)
  {
    var room = CreateRoomForAdd(position);
    var touched = TouchedForAdd(room);
    Execute("Add room", () => InsertRoom(room), touched);
    FinishAdd(room);
    return room;
  }

  /// <summary>
  ///   As <see cref="AddRoom" />, then asks <see cref="Prompts" /> to open the room's properties. The room and any
  ///   edits made in the dialog are one undo step; cancelling the dialog keeps the room.
  /// </summary>
  public async Task<Room> AddRoomAsync(Vector? position, bool openProperties = true)
  {
    var room = CreateRoomForAdd(position);
    var transaction = BeginTransaction("Add room", TouchedForAdd(room));
    try
    {
      InsertRoom(room);
      FinishAdd(room);
      if (openProperties) await Prompts.EditElementAsync(room);
    }
    catch
    {
      transaction.Cancel();
      transaction.Dispose();
      throw;
    }
    transaction.Dispose();
    RaiseChanged(EditorChange.Model);
    PostStatusMessage("Added room");
    return room;
  }

  private Room CreateRoomForAdd(Vector? position)
  {
    var centre = position ?? Viewport.Origin;
    var applyStyle = ApplicationSettingsController.AppSettings.ApplyStyleToNewRooms;
    return RoomFactory.CreateNewRoom(Project, centre, PrimaryRoom, applyStyle);
  }

  private List<Element> TouchedForAdd(Room room)
  {
    var touched = new List<Element> { room };
    if (Selection.Count == 1 && Selection.Primary is Connection connection) touched.Add(connection);
    return touched;
  }

  private void FinishAdd(Room room)
  {
    Selection.Set(new Element[] { room });
    Project.ActiveSelectedElement = room;
    RaiseChanged(EditorChange.Model);
  }

  private void InsertRoom(Room room)
  {
    var project = Project;
    var connection = Selection.Count == 1 ? Selection.Primary as Connection : null;
    project.Elements.Add(room);
    if (connection == null) return;

    var vertices = connection.VertexList;
    if (vertices.Count < 2) return;

    var targetPort = vertices[vertices.Count - 1].Port as Room.CompassPort;
    var sourcePort = vertices[0].Port as Room.CompassPort;
    var target = targetPort?.Owner as Room;
    var source = sourcePort?.Owner as Room;
    var targetCompass = targetPort != null ? targetPort.CompassPoint : CompassPoint.North;
    var sourceCompass = sourcePort != null ? sourcePort.CompassPoint : CompassPoint.North;

    if (target == null && source == null)
    {
      vertices.Add(new Vertex(room.PortAt(CompassPointHelper.GetOpposite(sourceCompass))));
    }
    else if (source == null)
    {
      vertices.RemoveAt(0);
      vertices.Add(new Vertex(room.PortAt(CompassPointHelper.GetOpposite(targetCompass))));
    }
    else if (target == null)
    {
      vertices.RemoveAt(vertices.Count - 1);
      vertices.Add(new Vertex(room.PortAt(CompassPointHelper.GetOpposite(sourceCompass))));
    }
    else
    {
      if (target.Region == source.Region) room.Region = target.Region;

      var first = new Connection(project, new Vertex(source.PortAt(sourceCompass)), new Vertex(room.PortAt(targetCompass)))
      {
        Style = connection.Style,
        Flow = connection.Flow
      };
      var second = new Connection(project, new Vertex(room.PortAt(sourceCompass)), new Vertex(target.PortAt(targetCompass)))
      {
        Style = connection.Style,
        Flow = connection.Flow
      };
      project.Elements.Add(first);
      project.Elements.Add(second);
      project.Elements.Remove(connection);
    }
  }
}
