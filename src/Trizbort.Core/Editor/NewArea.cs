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
///   area beyond it can be mapped apart from the rest. Its joins to the rooms it leaves behind are split: each is
///   drawn as a stub out of either room, labelled with the room at the other end ("to Front Porch", "to Foyer"),
///   and stays one join between the same rooms, so automap goes along it as ever. Ctrl-click a stub to go across.
/// </summary>
public static class NewArea
{
  /// <summary>Clear space left between the map and the new area, in rooms.</summary>
  private const int RoomsApart = 3;

  /// <summary>Whether <paramref name="room" /> can start a new area: a room, not a copy, joined to another room.</summary>
  public static bool CanStart(Room room) =>
    room != null && !room.IsReference && Joins(room).Any();

  /// <summary>
  ///   Moves <paramref name="room" /> to the right of everything else on the map and splits its joins to other
  ///   rooms. Returns the joins split; none when the room can't start an area.
  /// </summary>
  public static IReadOnlyList<Connection> Start(Project project, Room room)
  {
    if (project == null || !CanStart(room) || !project.Elements.Contains(room)) return Array.Empty<Connection>();
    var joins = Joins(room).ToList();

    var old = room.Position;
    var others = project.Elements.OfType<Room>().Where(other => other != room).ToList();
    var right = others.Count == 0 ? old.X + room.Width : others.Max(other => other.Position.X + other.Width);
    var gap = RoomsApart * (Settings.PreferredDistanceBetweenRooms + room.Width);
    room.Position = Settings.Snap(new Vector(right + gap, old.Y));

    foreach (var join in joins)
    {
      join.Split = true;
      // bends drawn for the old route mean nothing now
      while (join.VertexList.Count > 2) join.VertexList.RemoveAt(1);
      join.ClearCurveWaypoints();
    }

    return joins;
  }

  /// <summary>Joins between <paramref name="room" /> and another room (not loops or stubs).</summary>
  private static IEnumerable<Connection> Joins(Room room) =>
    room.GetConnections().Where(join =>
    {
      var source = join.GetSourceRoom();
      var target = join.GetTargetRoom();
      return source != null && target != null && source != target && join.VertexList.Count >= 2;
    }).Distinct();
}
