using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.Elements;

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

  /// <summary>Copies of <paramref name="room" /> on the map.</summary>
  public static IEnumerable<Room> Of(IEnumerable<Element> elements, Room room) =>
    elements.OfType<Room>().Where(other => other != room && other.ReferenceRoomId == room.Id && other.IsReference);
}
