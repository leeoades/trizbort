using System;
using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Editor;

/// <summary>
///   A room, or several, that the rest of a branch of the map hangs off: the rooms joined to the rest of the map only
///   through <see cref="Via" /> (and the other rooms being moved). Moving to a new area offers to take them along.
/// </summary>
public sealed class AreaBranch
{
  public AreaBranch(Room via, Room first, IReadOnlyList<Room> rooms)
  {
    Via = via;
    First = first;
    Rooms = rooms;
  }

  /// <summary>The room being moved that the branch is joined to.</summary>
  public Room Via { get; }

  /// <summary>The room of the branch joined to <see cref="Via" /> (the one it's named by).</summary>
  public Room First { get; }

  public IReadOnlyList<Room> Rooms { get; }

  /// <summary>"Living Room and 9 more rooms", "Living Room", "Dining Room and 1 more room".</summary>
  public string Description =>
    Rooms.Count switch
    {
      1 => First.Name,
      2 => First.Name + " and 1 more room",
      _ => First.Name + " and " + (Rooms.Count - 1) + " more rooms"
    };
}

/// <summary>
///   Move to a New Area: moves rooms (the foyer, on going into a house) into clear space beside the map, so the area
///   beyond them can be mapped apart from the rest. Their joins to rooms left behind are split: each is drawn as a stub
///   out of either room, labelled with the room at the other end ("to Front Porch", "to Foyer"), and stays one join
///   between the same rooms, so automap goes along it as ever. Ctrl-click a stub to go across. A room at the other
///   end of a split join can be brought across to join the room it leads to again (<see cref="Bring" />).
/// </summary>
public static class NewArea
{
  /// <summary>Clear space left between the map and the new area, in rooms.</summary>
  private const int RoomsApart = 3;

  /// <summary>Whether <paramref name="room" /> can start a new area: a room, not a copy, joined to another room.</summary>
  public static bool CanStart(Room room) =>
    room != null && !room.IsReference && Joins(room).Any();

  /// <summary>
  ///   Whether <paramref name="rooms" /> can be moved to a new area: one of them can start one (the others, copies
  ///   included, go with it).
  /// </summary>
  public static bool CanMove(IReadOnlyCollection<Room> rooms) => rooms != null && rooms.Any(CanStart);

  /// <summary>Moves <paramref name="room" /> to a new area (<see cref="Start(Project, IReadOnlyCollection{Room})" />).</summary>
  public static IReadOnlyList<Connection> Start(Project project, Room room) =>
    CanStart(room) ? Start(project, new[] { room }) : Array.Empty<Connection>();

  /// <summary>
  ///   Moves <paramref name="rooms" /> together, keeping their layout, to the right of everything else on the map, and
  ///   splits their joins to the rooms left behind. Returns the joins split.
  /// </summary>
  public static IReadOnlyList<Connection> Start(Project project, IReadOnlyCollection<Room> rooms)
  {
    if (project == null || !CanMove(rooms)) return Array.Empty<Connection>();
    var moving = rooms.Where(project.Elements.Contains).Distinct().ToList();
    if (moving.Count == 0) return Array.Empty<Connection>();

    var set = new HashSet<Room>(moving);
    var others = project.Elements.OfType<Room>().Where(other => !set.Contains(other)).ToList();
    var left = moving.Min(room => room.Position.X);
    var right = others.Count == 0
      ? moving.Max(room => room.Position.X + room.Width)
      : others.Max(other => other.Position.X + other.Width);
    var gap = RoomsApart * (Settings.PreferredDistanceBetweenRooms + moving[0].Width);
    return Move(set, moving[0], new Vector(right + gap - left, 0), null);
  }

  /// <summary>
  ///   The branches of the map hanging off <paramref name="moving" />: taking those rooms out of the map, the pieces
  ///   left that were joined to them but aren't joined to <paramref name="stay" />'s piece (the start room's, else the
  ///   biggest). A piece joined to the rest some other way isn't a branch: there's no line to draw.
  /// </summary>
  public static IReadOnlyList<AreaBranch> Branches(Project project, IReadOnlyCollection<Room> moving, Room stay = null)
  {
    if (project == null || moving == null || moving.Count == 0) return Array.Empty<AreaBranch>();
    var set = new HashSet<Room>(moving);
    var rooms = project.Elements.OfType<Room>().ToList();
    var neighbours = rooms.ToDictionary(room => room, _ => new HashSet<Room>());
    foreach (var join in project.Elements.OfType<Connection>())
    {
      var source = join.GetSourceRoom();
      var target = join.GetTargetRoom();
      if (source == null || target == null || source == target) continue;
      if (!neighbours.ContainsKey(source) || !neighbours.ContainsKey(target)) continue;
      neighbours[source].Add(target);
      neighbours[target].Add(source);
    }

    // the pieces of the map left when the moving rooms are taken out
    var piece = new Dictionary<Room, int>();
    var pieces = new List<List<Room>>();
    foreach (var room in rooms)
    {
      if (set.Contains(room) || piece.ContainsKey(room)) continue;
      var members = new List<Room>();
      var queue = new Queue<Room>();
      queue.Enqueue(room);
      piece[room] = pieces.Count;
      while (queue.Count > 0)
      {
        var next = queue.Dequeue();
        members.Add(next);
        foreach (var neighbour in neighbours[next])
        {
          if (set.Contains(neighbour) || piece.ContainsKey(neighbour)) continue;
          piece[neighbour] = pieces.Count;
          queue.Enqueue(neighbour);
        }
      }

      pieces.Add(members);
    }

    if (pieces.Count == 0) return Array.Empty<AreaBranch>();
    stay ??= rooms.FirstOrDefault(room => room.IsStartRoom && !set.Contains(room));
    var main = stay != null && piece.TryGetValue(stay, out var stayPiece)
      ? stayPiece
      : pieces.Select((members, index) => (members.Count, index)).Max().index;

    var branches = new List<AreaBranch>();
    var offered = new HashSet<int> { main };
    foreach (var via in moving)
    {
      if (!neighbours.ContainsKey(via)) continue;
      foreach (var first in neighbours[via].Where(room => !set.Contains(room)).OrderBy(room => room.Name))
      {
        var index = piece[first];
        if (!offered.Add(index)) continue;
        branches.Add(new AreaBranch(via, first, pieces[index]));
      }
    }

    return branches;
  }

  /// <summary>
  ///   Brings <paramref name="room" /> (with <paramref name="with" />, keeping their layout) beside the room at the
  ///   other end of <paramref name="join" />, on the side the join leaves that room, and joins them whole again; their
  ///   joins to rooms left behind are split.
  /// </summary>
  public static void Bring(Connection join, Room room, IReadOnlyCollection<Room> with)
  {
    var source = join.GetSourceRoom();
    var target = join.GetTargetRoom();
    if (source == null || target == null || source == target || (room != source && room != target)) return;
    var to = room == source ? target : source;

    // which way the join leaves the room it goes to
    var at = to == source ? join.VertexList[0].Position : join.VertexList[^1].Position;
    var centre = to.InnerBounds.Center;
    var dx = Math.Abs(at.X - centre.X) > to.Width / 4 ? Math.Sign(at.X - centre.X) : 0;
    var dy = Math.Abs(at.Y - centre.Y) > to.Height / 4 ? Math.Sign(at.Y - centre.Y) : 0;
    if (dx == 0 && dy == 0) dx = 1;

    var gap = Settings.PreferredDistanceBetweenRooms;
    var x = dx > 0 ? to.Position.X + to.Width + gap
      : dx < 0 ? to.Position.X - gap - room.Width
      : to.Position.X + (to.Width - room.Width) / 2;
    var y = dy > 0 ? to.Position.Y + to.Height + gap
      : dy < 0 ? to.Position.Y - gap - room.Height
      : to.Position.Y + (to.Height - room.Height) / 2;

    var set = new HashSet<Room>(with ?? Array.Empty<Room>()) { room };
    set.Remove(to);
    Move(set, room, new Vector(x, y) - room.Position, to);
  }

  /// <summary>
  ///   Moves <paramref name="set" /> by <paramref name="delta" />, bends and all, <paramref name="anchor" /> landing on
  ///   the grid. Their joins to rooms left behind are split, except those to <paramref name="rejoin" />, which are
  ///   made whole. Returns the joins split.
  /// </summary>
  private static IReadOnlyList<Connection> Move(HashSet<Room> set, Room anchor, Vector delta, Room rejoin)
  {
    // the rooms stay in the same places relative to each other
    delta = Settings.Snap(anchor.Position + delta) - anchor.Position;
    var joins = set.SelectMany(Joins).Distinct().ToList();
    foreach (var room in set) room.Position += delta;

    var split = new List<Connection>();
    foreach (var join in joins)
    {
      var inStart = set.Contains(join.GetSourceRoom());
      var inEnd = set.Contains(join.GetTargetRoom());
      if (inStart && inEnd)
      {
        foreach (var vertex in join.VertexList.Where(vertex => vertex.Port == null)) vertex.Position += delta;
        join.MoveCurveWaypointsBy(delta);
        continue;
      }

      var other = inStart ? join.GetTargetRoom() : join.GetSourceRoom();
      join.Split = other != rejoin;
      // bends drawn for the old route mean nothing now
      while (join.VertexList.Count > 2) join.VertexList.RemoveAt(1);
      join.ClearCurveWaypoints();
      if (join.Split) split.Add(join);
    }

    return split;
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
