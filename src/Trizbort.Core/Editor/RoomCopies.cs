using System.Collections.Generic;
using System.Linq;
using Trizbort.Automap;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Editor;

/// <summary>What can be done with a copy of a room (a reference room: a portal, or a moving room's stop).</summary>
public static class RoomCopies
{
  /// <summary>
  ///   Puts every join of <paramref name="copy" /> onto the room it stands for, at the same side, and removes the
  ///   copy. Joins between the two go, as do joins left doubled. Returns false when it isn't a copy.
  /// </summary>
  public static bool MoveJoinsToRoom(IList<Element> elements, Room copy)
  {
    var room = copy?.ReferenceRoom;
    if (elements == null || room == null || !elements.Contains(copy)) return false;
    // only the joins move: what the copy says about the place is the room's already
    copy.ClearDescriptions();
    copy.Objects = string.Empty;
    copy.IsDark = false;
    return RoomMerge.Merge(elements, room, new[] { copy }) == 1;
  }

  /// <summary>
  ///   Makes <paramref name="copy" /> a room of its own, somewhere else after all: it keeps its joins and takes the
  ///   name, description, objects and look of the room it stood for. Returns false when it isn't a copy.
  /// </summary>
  public static bool MakeRoomOfItsOwn(Room copy)
  {
    var room = copy?.ReferenceRoom;
    if (room == null) return false;
    copy.ReferenceRoomId = -1;
    copy.Name = room.Name;
    copy.CopyStyleFrom(room);
    copy.ClearDescriptions();
    foreach (var description in room.Descriptions) copy.AddDescription(description);
    copy.Objects = room.Objects;
    return true;
  }

  /// <summary>
  ///   A room that moves (<see cref="Room.Moves" />), or a stop of one: true if another stop can be added for it.
  /// </summary>
  public static bool CanAddStop(Room room) => room != null && (room.Moves || room.IsStop);

  /// <summary>
  ///   Adds another stop for the moving room <paramref name="room" /> is (or is a stop of): a copy of it, with its
  ///   name, look and descriptions, beside <paramref name="room" /> where there's space, for the user to put where the
  ///   room goes (the closet down in the cellar). Joined from <paramref name="room" /> by a dashed one-way join, as
  ///   automap joins stops, labelled "?": a way there by a command nobody knows yet (not a direction: it isn't east
  ///   because the join meets the room's east side). The first time automap sees the room move there, it asks what
  ///   moved it and that takes the place of "?". Returns the new stop, or null.
  /// </summary>
  public static Room AddStop(IList<Element> elements, Room room)
  {
    if (elements == null || !CanAddStop(room) || !elements.Contains(room)) return null;
    var root = room.IsStop ? room.ReferenceRoom : room;
    var stop = new Room(root.Project) { Name = root.Name, Size = root.Size, ReferenceRoomId = root.Id };
    stop.CopyStyleFrom(root);
    foreach (var description in root.Descriptions) stop.AddDescription(description);
    stop.Position = FreeSpotBeside(elements, room, stop.Size);
    elements.Add(stop);
    var join = new Connection(root.Project,
      new Vertex(room.PortAt(GuessedEnds.FreeFacingPoint(elements, room, stop, null))),
      new Vertex(stop.PortAt(GuessedEnds.FreeFacingPoint(elements, stop, room, null))))
    {
      Style = ConnectionStyle.Dashed,
      Flow = ConnectionFlow.OneWay
    };
    JoinWays.Write(join, new[] { JoinWay.Typed(true, Trizbort.Automap.Automap.UnmappedWay) },
      direction => direction.ToString().ToLowerInvariant());
    elements.Add(join);
    return stop;
  }

  /// <summary>Where a room of <paramref name="size" /> fits beside <paramref name="room" />, clear of other rooms.</summary>
  private static Vector FreeSpotBeside(IList<Element> elements, Room room, Vector size)
  {
    var gap = Settings.PreferredDistanceBetweenRooms;
    var stepX = room.Size.X + gap;
    var stepY = room.Size.Y + gap;
    var others = elements.OfType<Room>().Select(other => other.InnerBounds).ToList();
    // east, then round the room, then further out
    var around = new[] { (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1), (0, -1), (1, -1) };
    for (var distance = 1; distance <= 6; distance++)
      foreach (var (dx, dy) in around)
      {
        var at = Settings.Snap(new Vector(room.Position.X + dx * stepX * distance, room.Position.Y + dy * stepY * distance));
        var bounds = new Rect(at, size);
        if (!others.Any(other => other.IntersectsWith(bounds))) return at;
      }

    return Settings.Snap(new Vector(room.Position.X + stepX, room.Position.Y));
  }

  /// <summary>Copies of <paramref name="room" /> on the map.</summary>
  public static IEnumerable<Room> Of(IEnumerable<Element> elements, Room room) =>
    elements.OfType<Room>().Where(other => other != room && other.ReferenceRoomId == room.Id && other.IsReference);
}
