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
///   Split Room: a room on the map turns out to be two places with one name (its exits went elsewhere). A new room,
///   with the same name and description (the game calls them both "Maze"), is added beside it, and the joins picked
///   move over to it, each at the same side.
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
      .Select(way => way.IsDirection ? DirectionWords.Word(way.Direction) : way.Text)
      .Where(text => !string.IsNullOrWhiteSpace(text)).Distinct(StringComparer.OrdinalIgnoreCase));
    var outward = Words(atStart);
    var back = Words(!atStart);
    var name = source == target ? room.Name + " itself" : other?.Name ?? "nowhere";
    var text = string.IsNullOrEmpty(outward) ? "to " + name : outward + " to " + name;
    if (string.IsNullOrEmpty(outward) && !string.IsNullOrEmpty(back)) text = "from " + name;
    if (!string.IsNullOrEmpty(back)) text += $" ({back} back)";
    return text;
  }

  /// <summary>
  ///   The name for the second room: the same name, as the game calls it, so automap knows it by its name. (It was
  ///   "Maze (2)", which isn't the name of the location.)
  /// </summary>
  public static string SplitName(Project project, string name) => name;

  /// <summary>
  ///   The side of <paramref name="room" /> a join leaves it from (a loop: the side it leaves from, not the side it
  ///   comes back to).
  /// </summary>
  public static CompassPoint SideOf(Connection join, Room room)
  {
    var source = join.GetSourceRoom(out var start);
    var target = join.GetTargetRoom(out var end);
    return source == room ? start : target == room ? end : start;
  }

  /// <summary>Whether a join leaves <paramref name="room" /> and comes back into it.</summary>
  public static bool IsLoop(Connection join, Room room) =>
    join.GetSourceRoom() == room && join.GetTargetRoom() == room;

  /// <summary>
  ///   How a join reads in Split Room, from <paramref name="room" />: "east to Pit", "east, back here" (a loop),
  ///   "in from Hall (its south)" (one way into the room), "north to Pit, one way".
  /// </summary>
  public static string Exit(Connection join, Room room)
  {
    var source = join.GetSourceRoom();
    var target = join.GetTargetRoom();
    var atStart = source == room;
    var other = atStart ? target : source;
    var ways = JoinWays.Of(join);
    string Words(bool fromStart) => string.Join(" / ", ways.Where(way => way.AtStart == fromStart)
      .Select(way => way.IsDirection ? DirectionWords.Word(way.Direction) : way.Text)
      .Where(text => !string.IsNullOrWhiteSpace(text)).Distinct(StringComparer.OrdinalIgnoreCase));
    var otherName = other?.Name ?? "nowhere";
    if (IsLoop(join, room))
    {
      var round = Words(true);
      return (string.IsNullOrEmpty(round) ? "a way" : round) + ", back here";
    }

    var oneWay = join.Flow == ConnectionFlow.OneWay;
    // one way into this room: only the way in from the other room is known
    if (oneWay && !atStart)
    {
      var theirs = Words(true);
      return "in from " + otherName + (string.IsNullOrEmpty(theirs) ? string.Empty : $" (its {theirs})");
    }

    var outward = Words(atStart);
    var text = (string.IsNullOrEmpty(outward) ? "to " : outward + " to ") + otherName;
    if (oneWay) return text + ", one way";
    var back = Words(!atStart);
    return string.IsNullOrEmpty(back) ? text : $"{text} ({back} back)";
  }

  /// <summary>
  ///   The ways in and out of <paramref name="room" />, newest join first, for Split Room: each read from the room
  ///   ("west → Maze (rod)", "from Maze (rod), going east", "east → back here"), the other room told apart by its
  ///   objects when rooms share its name.
  /// </summary>
  public static IReadOnlyList<SplitExit> ExitsOf(Room room)
  {
    var exits = new List<SplitExit>();
    foreach (var join in JoinsOf(room))
    {
      var side = SideOf(join, room);
      var atStart = join.GetSourceRoom() == room;
      var other = atStart ? join.GetTargetRoom() : join.GetSourceRoom();
      var ways = JoinWays.Of(join);
      string Words(bool fromStart) => string.Join(" / ", ways.Where(way => way.AtStart == fromStart)
        .Select(way => way.IsDirection ? DirectionWords.Word(way.Direction) : way.Text)
        .Where(text => !string.IsNullOrWhiteSpace(text)).Distinct(StringComparer.OrdinalIgnoreCase));
      if (IsLoop(join, room))
      {
        var round = Words(true);
        exits.Add(new SplitExit(join, true, side, (string.IsNullOrEmpty(round) ? "a way" : round) + " → back here", true));
        continue;
      }

      var there = Called(room.Project, other);
      var outward = Words(atStart);
      var inward = Words(!atStart);
      var outText = (string.IsNullOrEmpty(outward) ? "→ " : outward + " → ") + there;
      var inText = "from " + there + (string.IsNullOrEmpty(inward) ? string.Empty : ", going " + inward);
      if (join.Flow == ConnectionFlow.OneWay)
        exits.Add(atStart ? new SplitExit(join, false, side, outText, true) : new SplitExit(join, true, side, inText, true));
      else
      {
        exits.Add(new SplitExit(join, false, side, outText, false));
        exits.Add(new SplitExit(join, true, side, inText, false));
      }
    }

    return exits;
  }

  /// <summary>
  ///   The exit to suggest moving: the way into the room by its newest join (the way the player just came in, which
  ///   automap took to lead here), or that join whole when it only goes one way.
  /// </summary>
  public static SplitExit SuggestedExit(IReadOnlyList<SplitExit> exits)
  {
    var newest = exits.FirstOrDefault()?.Join;
    if (newest == null) return null;
    var halves = exits.Where(exit => exit.Join == newest).ToList();
    return halves.FirstOrDefault(exit => exit.Inward) ?? halves.First();
  }

  /// <summary>A room's name, with its objects when other rooms share the name ("Maze (rod)").</summary>
  private static string Called(Project project, Room room)
  {
    if (room == null) return "nowhere";
    var shared = project != null && project.Elements.OfType<Room>().Count(other => other.Name == room.Name) > 1;
    var objects = (room.Objects ?? string.Empty).Replace("\r", string.Empty).Split('\n')
      .Select(line => line.Trim()).Where(line => line.Length > 0).ToList();
    return shared && objects.Count > 0 ? $"{room.Name} ({string.Join(", ", objects)})" : room.Name;
  }

  /// <summary>
  ///   Split Room with the ways chosen in the dialog: a whole join moves as <see cref="Split" /> moves it; half of a
  ///   two-way join splits it in two one-way joins, the half that stays on the original room, and the half that moves,
  ///   now to or from the new room. (West from the gold room to the rod room stays; east from the rod room goes into
  ///   the new room, one way, as nothing yet says the way back.)
  /// </summary>
  public static Room Split(Project project, Room room, IReadOnlyList<SplitExit> exits, string description = null)
  {
    exits ??= Array.Empty<SplitExit>();
    var all = ExitsOf(room);
    var whole = new List<Connection>();
    var halves = new List<SplitExit>();
    foreach (var group in exits.GroupBy(exit => exit.Join))
    {
      var join = group.Key;
      var both = all.Count(exit => exit.Join == join);
      if (group.Any(exit => exit.Whole) || group.Select(exit => exit.Inward).Distinct().Count() >= both)
        whole.Add(join);
      else
        halves.Add(group.First());
    }

    var second = Split(project, room, whole, description);
    if (second == null) return null;
    foreach (var half in halves) SplitHalf(project, room, second, half);
    return second;
  }

  /// <summary>
  ///   Splits a two-way join between <paramref name="room" /> and another in two: the half that stays, one way between
  ///   them, and the half that moves, one way between the other room and <paramref name="second" />.
  /// </summary>
  private static void SplitHalf(Project project, Room room, Room second, SplitExit half)
  {
    var join = half.Join;
    var source = join.GetSourceRoom(out var start);
    var target = join.GetTargetRoom(out var end);
    var roomAtStart = source == room;
    var other = roomAtStart ? target : source;
    var roomSide = roomAtStart ? start : end;
    var otherSide = roomAtStart ? end : start;
    var ways = JoinWays.Of(join);
    var outWays = ways.Where(way => way.AtStart == roomAtStart).ToList();
    var inWays = ways.Where(way => way.AtStart != roomAtStart).ToList();

    // the half that stays: the way out of the room (the way in moved), or the way in (the way out moved)
    if (half.Inward) OneWay(join, room, outWays, roomAtStart);
    else OneWay(join, other, inWays, !roomAtStart);

    var moved = half.Inward
      ? new Connection(project, new Vertex(other.PortAt(otherSide)), new Vertex(second.PortAt(roomSide)))
      : new Connection(project, new Vertex(second.PortAt(roomSide)), new Vertex(other.PortAt(otherSide)));
    moved.Style = join.Style;
    moved.ConnectionColor = join.ConnectionColor;
    moved.Unreliable = join.Unreliable;
    moved.Flow = ConnectionFlow.OneWay;
    project.Elements.Add(moved);
    JoinWays.Write(moved, (half.Inward ? inWays : outWays).Select(way => way with { AtStart = true }),
      DirectionWords.Word);
  }

  /// <summary>
  ///   Makes <paramref name="join" /> one way from <paramref name="from" />, along <paramref name="ways" /> (which
  ///   were from its start end when <paramref name="wereAtStart" />).
  /// </summary>
  private static void OneWay(Connection join, Room from, IEnumerable<JoinWay> ways, bool wereAtStart)
  {
    var list = ways.ToList();
    if (join.GetSourceRoom() != from)
    {
      join.Reverse();
      (join.StartText, join.EndText) = (join.EndText, join.StartText);
    }

    join.Flow = ConnectionFlow.OneWay;
    JoinWays.Write(join, list.Select(way => way with { AtStart = true }), DirectionWords.Word);
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
    second.IsDark = room.IsDark;
    // the same place to look at: the game describes them alike. (Not the objects: they mark the room they're in.)
    foreach (var known in room.Descriptions) second.AddDescription(known);
    second.Position = Settings.Snap(new Vector(room.Position.X + room.Width + Settings.PreferredDistanceBetweenRooms,
      room.Position.Y));
    if (!string.IsNullOrWhiteSpace(description)) second.AddDescription(description);
    project.Elements.Add(second);

    foreach (var join in moving)
    {
      if (join.VertexList.Count == 0) continue;
      var source = join.GetSourceRoom(out var start);
      var target = join.GetTargetRoom(out var end);
      // a way round back into the room (east, taken for the same maze room): it arrives in the new one, one way, as
      // nothing yet says the way back
      if (source == room && target == room)
      {
        join.VertexList[join.VertexList.Count - 1].Port = second.PortAt(end);
        join.Flow = ConnectionFlow.OneWay;
        continue;
      }

      if (source == room) join.VertexList[0].Port = second.PortAt(start);
      if (target == room) join.VertexList[join.VertexList.Count - 1].Port = second.PortAt(end);
    }

    return second;
  }
}

/// <summary>
///   What the user chose in Split Room: the joins that go to the new room, and where the player is; or, from the
///   dialog, the ways in and out that go (<see cref="Exits" />), each half of a two-way join on its own.
/// </summary>
public sealed record RoomSplitChoice(IReadOnlyList<Connection> Moving, RoomSplitWhere Where,
  IReadOnlyList<SplitExit> Exits = null);

/// <summary>
///   One way in or out of a room being split. A two-way join is two of them: the way out (<see cref="Inward" />
///   false) and the way back in (true), which can go to different rooms: west from the gold room to the rod room, but
///   east from the rod room into another maze room. A one-way join, or a loop, is one.
/// </summary>
public sealed record SplitExit(Connection Join, bool Inward, CompassPoint Side, string Text, bool Whole);

/// <summary>Which of the two rooms the player is in, after a split.</summary>
public enum RoomSplitWhere
{
  Neither,
  Original,
  New
}
