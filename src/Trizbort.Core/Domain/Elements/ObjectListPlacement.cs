using System.Collections.Generic;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Domain.Elements;

/// <summary>A place for a room's object list: beside the room at a side or corner, or inside it.</summary>
public readonly record struct ObjectListSpot(CompassPoint Side, bool Inside)
{
  public static ObjectListSpot Default => new(Settings.DefaultObjectsPosition, Settings.DefaultObjectsInside);
}

/// <summary>
///   Where a room's object list is drawn. A list placed by hand is where it was put; an automatic one is at the map's
///   default spot, or, when that would cross a join, a room or another room's object list, the first other spot clear
///   of them all (below, above, right, left, then the corners). None clear: the default.
/// </summary>
public static class ObjectListPlacement
{
  /// <summary>The spots an automatic list may move to, in order of preference after the default.</summary>
  public static readonly IReadOnlyList<CompassPoint> Sides = new[]
  {
    CompassPoint.South, CompassPoint.North, CompassPoint.East, CompassPoint.West,
    CompassPoint.SouthEast, CompassPoint.SouthWest, CompassPoint.NorthEast, CompassPoint.NorthWest
  };

  /// <summary>
  ///   The bounds of a <paramref name="width" /> by <paramref name="height" /> list at <paramref name="spot" /> for a
  ///   room drawn at <paramref name="room" />, nudged <paramref name="right" /> and <paramref name="down" />.
  /// </summary>
  public static Rect Bounds(Rect room, ObjectListSpot spot, float width, float height, float right = 0, float down = 0)
  {
    var offset = Settings.ObjectListOffsetFromRoom;
    if (spot.Inside)
      return new Rect(room.Left + offset + right, room.Bottom - offset - height + down, width, height);

    var expanded = room;
    expanded.Inflate(offset, offset);
    var at = expanded.GetCorner(spot.Side);
    var x = at.X + right + (spot.Side is CompassPoint.North or CompassPoint.South ? offset : 0);
    var y = at.Y + down;
    if (spot.Side is CompassPoint.West or CompassPoint.NorthWest or CompassPoint.SouthWest) x -= width;
    if (spot.Side is CompassPoint.North or CompassPoint.NorthEast or CompassPoint.NorthWest) y -= height;
    return new Rect(x, y, width, height);
  }

  /// <summary>
  ///   Where an automatic list goes: <paramref name="preferred" /> if clear of <paramref name="segments" /> (joins) and
  ///   <paramref name="obstacles" /> (rooms, other lists), else the first clear side, else the one crossing least.
  /// </summary>
  public static ObjectListSpot Choose(Rect room, float width, float height, ObjectListSpot preferred,
    IReadOnlyList<LineSegment> segments, IReadOnlyList<Rect> obstacles)
  {
    var best = preferred;
    var bestCount = Crossings(Bounds(room, preferred, width, height), segments, obstacles);
    if (bestCount == 0) return preferred;
    foreach (var side in Sides)
    {
      var spot = new ObjectListSpot(side, false);
      if (spot == preferred) continue;
      var count = Crossings(Bounds(room, spot, width, height), segments, obstacles);
      if (count == 0) return spot;
      if (count < bestCount)
      {
        best = spot;
        bestCount = count;
      }
    }

    return best;
  }

  private static int Crossings(Rect bounds, IReadOnlyList<LineSegment> segments, IReadOnlyList<Rect> obstacles)
  {
    var count = 0;
    foreach (var segment in segments)
      if (segment.IntersectsWith(bounds))
        count++;
    foreach (var obstacle in obstacles)
      if (obstacle.IntersectsWith(bounds))
        count++;
    return count;
  }
}
