using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;

namespace Trizbort.Editor;

/// <summary>
///   Merge Rooms: makes several rooms one, as when automap has mapped the same place twice ("Darkness" and the room
///   it turned out to be, or a room reached a different way).
/// </summary>
public static class RoomMerge
{
  /// <summary>
  ///   Moves every join of <paramref name="others" /> onto <paramref name="keep" /> (at the same point of the room),
  ///   adds their descriptions and objects to it, and removes them. Joins between the merged rooms go, as do joins
  ///   and unexplored-exit stubs left doubled. Returns how many rooms were merged into <paramref name="keep" />.
  /// </summary>
  public static int Merge(IList<Element> elements, Room keep, IEnumerable<Room> others)
  {
    if (elements == null || keep == null || others == null) return 0;
    var gone = others.Where(room => room != null && room != keep && elements.Contains(room)).Distinct().ToList();
    if (gone.Count == 0) return 0;
    var merged = new HashSet<Room>(gone) { keep };

    // joins between the rooms being merged would join the room to itself
    foreach (var connection in elements.OfType<Connection>().ToList())
    {
      var source = connection.GetSourceRoom();
      var target = connection.GetTargetRoom();
      if (source != null && target != null && source != target && merged.Contains(source) && merged.Contains(target))
        elements.Remove(connection);
    }

    foreach (var connection in elements.OfType<Connection>().ToList())
    {
      if (connection.VertexList.Count == 0) continue;
      if (connection.GetSourceRoom(out var start) is { } source && gone.Contains(source))
        connection.VertexList[0].Port = keep.PortAt(start);
      if (connection.GetTargetRoom(out var end) is { } target && gone.Contains(target))
        connection.VertexList[connection.VertexList.Count - 1].Port = keep.PortAt(end);
    }

    foreach (var room in gone)
    {
      foreach (var description in room.Descriptions.ToList()) keep.AddDescription(description);
      if (!string.IsNullOrWhiteSpace(room.Objects) &&
          (keep.Objects == null || !keep.Objects.Contains(room.Objects.Trim())))
        keep.Objects = string.IsNullOrWhiteSpace(keep.Objects) ? room.Objects : keep.Objects.TrimEnd() + "\n" + room.Objects;
      if (room.IsDark) keep.IsDark = true;
      elements.Remove(room);
    }

    RemoveDoubles(elements, keep);
    return gone.Count;
  }

  /// <summary>Joins of <paramref name="room" /> left doubled by the merge, and stubs on a point a join uses.</summary>
  private static void RemoveDoubles(IList<Element> elements, Room room)
  {
    var seen = new HashSet<string>();
    var used = new HashSet<CompassPoint>();
    var connections = elements.OfType<Connection>().ToList();
    foreach (var connection in connections)
    {
      var source = connection.GetSourceRoom(out var start);
      var target = connection.GetTargetRoom(out var end);
      if (source != room && target != room) continue;
      if (source == target && start == end) continue; // a stub: below
      // the same line drawn either way round is the same join
      var from = string.Join(":", source?.Id, start);
      var to = string.Join(":", target?.Id, end);
      var key = string.CompareOrdinal(from, to) <= 0 ? from + "|" + to : to + "|" + from;
      if (!seen.Add(key))
      {
        elements.Remove(connection);
        continue;
      }

      if (source == room) used.Add(start);
      if (target == room) used.Add(end);
    }

    var stubs = new HashSet<CompassPoint>();
    foreach (var connection in connections.Where(elements.Contains))
    {
      var source = connection.GetSourceRoom(out var start);
      var target = connection.GetTargetRoom(out var end);
      if (source != room || target != room || start != end) continue;
      if (used.Contains(start) || !stubs.Add(start)) elements.Remove(connection);
    }
  }
}
