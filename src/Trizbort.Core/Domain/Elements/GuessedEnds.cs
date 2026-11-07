using System;
using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

namespace Trizbort.Domain.Elements;

/// <summary>
///   The far end of a one-way join automap made, which nobody has come back along: where it meets its room is only a
///   guess, so it meets it on the side facing the room it came from, at a point no known exit uses, and follows as
///   rooms move (automap's tidy, or the user dragging). Joins drawn by hand, and ends with a label, are left alone.
/// </summary>
public static class GuessedEnds
{
  /// <summary>Whether <paramref name="connection" />'s far end is such a guess.</summary>
  public static bool IsGuessedFarEnd(Connection connection) =>
    connection.Flow == ConnectionFlow.OneWay &&
    string.IsNullOrEmpty(connection.AutomapCommand) &&
    !string.IsNullOrWhiteSpace(connection.StartTravelled) &&
    string.IsNullOrWhiteSpace(connection.EndTravelled) &&
    string.IsNullOrWhiteSpace(connection.EndText) &&
    connection.VertexList.Count == 2;

  /// <summary>
  ///   Re-docks the guessed far ends touching <paramref name="rooms" /> (all of them when null). Returns how many
  ///   moved.
  /// </summary>
  public static int Face(IEnumerable<Element> elements, ICollection<Room> rooms = null)
  {
    var list = elements as IList<Element> ?? elements.ToList();
    var moved = 0;
    foreach (var connection in list.OfType<Connection>().ToList())
    {
      if (!IsGuessedFarEnd(connection)) continue;
      var source = connection.GetSourceRoom();
      var target = connection.GetTargetRoom(out var current);
      if (source == null || target == null || source == target) continue;
      if (rooms != null && !rooms.Contains(source) && !rooms.Contains(target)) continue;
      var facing = FreeFacingPoint(list, target, source, connection);
      if (facing == current) continue;
      connection.VertexList[connection.VertexList.Count - 1].Port = target.PortAt(facing);
      moved++;
    }

    return moved;
  }

  /// <summary>
  ///   The side of <paramref name="from" /> facing <paramref name="to" /> (any of its sixteen points), or the nearest
  ///   one free: a known exit keeps its point and the points either side of it, so a guess doesn't look like that
  ///   exit; another guess only keeps its own point. <paramref name="self" /> is the join being docked, if any.
  /// </summary>
  public static CompassPoint FreeFacingPoint(IEnumerable<Element> elements, Room from, Room to, Connection self)
  {
    var a = from.InnerBounds.Center;
    var b = to.InnerBounds.Center;
    var dx = b.X - a.X;
    var dy = b.Y - a.Y;
    // compass bearing (0 = north, clockwise) in sixteenths, as CompassPoint counts: North 0, NorthNorthEast 1...
    var bearing = dx == 0 && dy == 0 ? 90 : Math.Atan2(dx, -dy) * 180.0 / Math.PI;
    var best = ((int)Math.Round(bearing / 22.5) % 16 + 16) % 16;

    var used = new HashSet<int>();
    foreach (var connection in elements.OfType<Connection>())
    {
      if (connection == self) continue;
      var guessedFar = IsGuessedFarEnd(connection);
      if (connection.GetSourceRoom(out var startPoint) == from) Block(used, (int)startPoint, true);
      if (connection.GetTargetRoom(out var endPoint) == from) Block(used, (int)endPoint, !guessedFar);
    }

    for (var step = 0; step <= 8; step++)
      foreach (var offset in step == 0 ? new[] { 0 } : new[] { step, -step })
      {
        var point = ((best + offset) % 16 + 16) % 16;
        if (!used.Contains(point)) return (CompassPoint)point;
      }

    return (CompassPoint)best;
  }

  private static void Block(HashSet<int> used, int point, bool withNeighbours)
  {
    used.Add(point);
    if (!withNeighbours) return;
    used.Add((point + 1) % 16);
    used.Add((point + 15) % 16);
  }
}
