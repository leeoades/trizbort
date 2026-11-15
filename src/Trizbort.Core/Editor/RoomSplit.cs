using System;
using System.Collections.Generic;
using System.Linq;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Editor;

/// <summary>
///   Split Room: a room on the map turns out to be two places with one name (its exits went elsewhere). A new room
///   ("Maze (2)") is added beside it, and the joins picked move over to it, each at the same side.
/// </summary>
public static class RoomSplit
{
  /// <summary>The joins of <paramref name="room" /> to other rooms, newest first, and its stubs.</summary>
  public static IReadOnlyList<Connection> JoinsOf(Room room) =>
    room?.GetConnections().Distinct().OrderByDescending(join => join.Id).ToList() ?? new List<Connection>();

  /// <summary>
  ///   The join to suggest moving: the one made last (automap's way in, just now), when there are several.
  /// </summary>
  public static Connection Suggested(Room room) => JoinsOf(room).FirstOrDefault();

  /// <summary>
  ///   How a join reads from <paramref name="room" />'s side: its ways out to the room at the other end, and back
  ///   ("east to Pit (west back)", "xyzzy to Building").
  /// </summary>
  public static string Describe(Connection join, Room room)
  {
    var source = join.GetSourceRoom();
    var target = join.GetTargetRoom();
    var atStart = source == room;
    var other = atStart ? target : source;
    var ways = JoinWays.Of(join);
    string Words(bool fromStart) => string.Join(" / ", ways.Where(way => way.AtStart == fromStart)
      .Select(way => way.IsDirection ? way.Text ?? DirectionWords.Word(way.Direction) : way.Text)
      .Where(text => !string.IsNullOrWhiteSpace(text)).Distinct(StringComparer.OrdinalIgnoreCase));
    var outward = Words(atStart);
    var back = Words(!atStart);
    var name = source == target ? room.Name + " itself" : other?.Name ?? "nowhere";
    var text = string.IsNullOrEmpty(outward) ? "to " + name : outward + " to " + name;
    if (string.IsNullOrEmpty(outward) && !string.IsNullOrEmpty(back)) text = "from " + name;
    if (!string.IsNullOrEmpty(back)) text += $" ({back} back)";
    return text;
  }

  /// <summary>A name for the second room: "Maze (2)", or the next number free.</summary>
  public static string SplitName(Project project, string name)
  {
    var names = new HashSet<string>(project.Elements.OfType<Room>().Select(room => room.Name),
      StringComparer.OrdinalIgnoreCase);
    for (var number = 2;; number++)
    {
      var candidate = $"{name} ({number})";
      if (!names.Contains(candidate)) return candidate;
    }
  }

  /// <summary>
  ///   Adds the second room beside <paramref name="room" />, in its style, and moves <paramref name="joins" /> over
  ///   to it, each end on <paramref name="room" /> going to the same side of the new room. Returns the new room.
  /// </summary>
  public static Room Split(Project project, Room room, IEnumerable<Connection> joins, string description = null)
  {
    if (project == null || room == null || !project.Elements.Contains(room)) return null;
    var moving = (joins ?? Enumerable.Empty<Connection>()).Where(join => join != null).Distinct().ToList();

    var second = new Room(project) { Name = SplitName(project, room.Name) };
    second.CopyStyleFrom(room);
    second.Size = room.Size;
    second.Position = Settings.Snap(new Vector(room.Position.X + room.Width + Settings.PreferredDistanceBetweenRooms,
      room.Position.Y));
    if (!string.IsNullOrWhiteSpace(description)) second.AddDescription(description);
    project.Elements.Add(second);

    foreach (var join in moving)
    {
      if (join.VertexList.Count == 0) continue;
      var source = join.GetSourceRoom(out var start);
      var target = join.GetTargetRoom(out var end);
      // a way round back into the room (up "In the Chimney" taken for the same chimney): it arrives in the new one
      if (source == room && target != room) join.VertexList[0].Port = second.PortAt(start);
      if (target == room) join.VertexList[join.VertexList.Count - 1].Port = second.PortAt(end);
    }

    return second;
  }
}

/// <summary>What the user chose in Split Room: the joins that go to the new room, and where the player is.</summary>
public sealed record RoomSplitChoice(IReadOnlyList<Connection> Moving, RoomSplitWhere Where);

/// <summary>Which of the two rooms the player is in, after a split.</summary>
public enum RoomSplitWhere
{
  Neither,
  Original,
  New
}
