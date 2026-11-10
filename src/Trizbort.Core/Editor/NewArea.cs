using System;
using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Editor;

/// <summary>
///   Start a New Area Here: moves a room (the foyer, on going into a house) into clear space beside the map, so the
///   area beyond it can be mapped apart from the rest, and leaves portals behind to keep the joins. Where the room
///   was, a portal to it ("To Foyer") takes the old joins; beside it in its new place, a portal to each room it was
///   joined to ("To Front Porch"), in the same places round it, joined the same way. Ctrl-click a portal to go to the
///   room; automap goes through portals as if they were the rooms.
/// </summary>
public static class NewArea
{
  /// <summary>Clear space left between the map and the new area, in rooms.</summary>
  private const int RoomsApart = 3;

  /// <summary>Whether <paramref name="room" /> can start a new area: a room, not a portal, joined to another room.</summary>
  public static bool CanStart(Room room) =>
    room != null && !room.IsReference && Joins(room).Any();

  /// <summary>
  ///   Moves <paramref name="room" /> to the right of everything else on the map and makes the portals. Returns the
  ///   portals made, the one where the room was first; none when the room can't start an area.
  /// </summary>
  public static IReadOnlyList<Room> Start(Project project, Room room)
  {
    var made = new List<Room>();
    if (project == null || !CanStart(room) || !project.Elements.Contains(room)) return made;
    var joins = Joins(room).ToList();
    var neighbours = joins.Select(join => OtherEnd(join, room)).Distinct().ToList();

    var old = room.Position;
    var others = project.Elements.OfType<Room>().Where(other => other != room).ToList();
    var right = others.Count == 0 ? old.X + room.Width : others.Max(other => other.Position.X + other.Width);
    // the neighbours' portals sit round the room as the neighbours did: all of them clear of the map
    var reachLeft = Math.Max(0, neighbours.Max(neighbour => old.X - neighbour.Position.X));
    var gap = RoomsApart * (Settings.PreferredDistanceBetweenRooms + room.Width);
    var moved = Settings.Snap(new Vector(right + gap + reachLeft, old.Y));
    var delta = moved - old;
    room.Position = moved;

    var back = Portal(project, room, old);
    made.Add(back);
    var portals = new Dictionary<Room, Room>();
    foreach (var neighbour in neighbours)
    {
      var portal = Portal(project, neighbour, neighbour.Position + delta);
      portals[neighbour] = portal;
      made.Add(portal);
    }

    foreach (var join in joins)
    {
      var source = join.GetSourceRoom(out var start);
      var target = join.GetTargetRoom(out var end);
      var roomIsStart = source == room;
      var neighbour = roomIsStart ? target : source;
      var portal = portals[neighbour];

      // the new area's copy of the join, room to portal, the same way round
      var copy = new Connection(
        project,
        new Vertex(roomIsStart ? room.PortAt(start) : portal.PortAt(start)),
        new Vertex(roomIsStart ? portal.PortAt(end) : room.PortAt(end)));
      CopyJoin(join, copy);
      project.Elements.Add(copy);

      // the old join now ends at the portal where the room was; its bends stay where they are
      if (roomIsStart) join.VertexList[0].Port = back.PortAt(start);
      else join.VertexList[join.VertexList.Count - 1].Port = back.PortAt(end);
    }

    return made;
  }

  /// <summary>Joins between <paramref name="room" /> and another room (not loops or stubs).</summary>
  private static IEnumerable<Connection> Joins(Room room) =>
    room.GetConnections().Where(join =>
    {
      var source = join.GetSourceRoom();
      var target = join.GetTargetRoom();
      return source != null && target != null && source != target && join.VertexList.Count >= 2;
    }).Distinct();

  private static Room OtherEnd(Connection join, Room room)
  {
    var source = join.GetSourceRoom();
    return source == room ? join.GetTargetRoom() : source;
  }

  /// <summary>A portal to <paramref name="room" /> at <paramref name="position" />, the room's size and style.</summary>
  private static Room Portal(Project project, Room room, Vector position)
  {
    var portal = new Room(project);
    project.Elements.Add(portal);
    portal.AddReferenceRoom(room.Id);
    portal.Size = room.Size;
    portal.Position = position;
    return portal;
  }

  private static void CopyJoin(Connection from, Connection to)
  {
    to.Style = from.Style;
    to.Flow = from.Flow;
    to.ConnectionColor = from.ConnectionColor;
    to.Door = from.Door;
    to.Description = from.Description;
    to.StartText = from.StartText;
    to.MidText = from.MidText;
    to.EndText = from.EndText;
    to.AutomapCommand = from.AutomapCommand;
    to.StartTravelled = from.StartTravelled;
    to.EndTravelled = from.EndTravelled;
    to.AutomapWays = from.AutomapWays;
    to.AutomapWaysFor = from.AutomapWaysFor;
  }
}
