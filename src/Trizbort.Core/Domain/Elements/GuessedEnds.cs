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
///   An end whose label lists several compass directions ("n / e") may meet its room on any of those sides, and
///   takes whichever best faces the room at the other end. Either way, a choice whose line crosses fewer other joins
///   wins.
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
      var source0 = connection.GetSourceRoom();
      var target0 = connection.GetTargetRoom();
      if (rooms == null || rooms.Contains(source0) || rooms.Contains(target0))
      {
        if (FaceListedSide(list, connection, true)) moved++;
        if (FaceListedSide(list, connection, false)) moved++;
      }

      if (!IsGuessedFarEnd(connection)) continue;
      var source = connection.GetSourceRoom();
      var target = connection.GetTargetRoom(out var current);
      if (source == null || target == null || source == target) continue;
      if (rooms != null && !rooms.Contains(source) && !rooms.Contains(target)) continue;
      var facing = FreeFacingPoint(list, target, source, connection, connection.VertexList[0].Position, current);
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
  /// <param name="otherEnd">Where the join's other end is, if known: then, of the free points near the facing side,
  ///   the one whose line crosses fewest other joins wins, and <paramref name="current" /> keeps its place in a tie.</param>
  public static CompassPoint FreeFacingPoint(
    IEnumerable<Element> elements, Room from, Room to, Connection self, Vector? otherEnd = null,
    CompassPoint? current = null)
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

    var lines = otherEnd.HasValue ? Lines(elements, self) : null;
    CompassPoint? choice = null;
    var choiceScore = double.MaxValue;
    for (var step = 0; step <= 8; step++)
    {
      // once a free point is found, only look a little further round for one that crosses fewer joins
      if (choice.HasValue && (lines == null || step > 3)) break;
      foreach (var offset in step == 0 ? new[] { 0 } : new[] { step, -step })
      {
        var point = ((best + offset) % 16 + 16) % 16;
        if (used.Contains(point)) continue;
        var score = step * 0.1;
        if (lines != null)
        {
          score += Crossings(from.PortAt((CompassPoint)point).Position, otherEnd.Value, lines);
          if (current.HasValue && (int)current.Value == point) score -= 0.05;
        }

        if (score < choiceScore)
        {
          choiceScore = score;
          choice = (CompassPoint)point;
        }
      }
    }

    return choice ?? (CompassPoint)best;
  }

  /// <summary>
  ///   An end labelled with several compass directions ("n / e") and meeting its room on one of their sides moves to
  ///   the listed side that best faces the room at the other end. Returns true if it moved.
  /// </summary>
  private static bool FaceListedSide(IList<Element> elements, Connection connection, bool atStart)
  {
    if (connection.VertexList.Count != 2) return false;
    var directions = DirectionWords.ListedDirections(atStart ? connection.StartText : connection.EndText)
      .Where(DirectionWords.IsCompass).ToList();
    if (directions.Count < 2) return false;

    CompassPoint current;
    var here = atStart ? connection.GetSourceRoom(out current) : connection.GetTargetRoom(out current);
    var there = atStart ? connection.GetTargetRoom() : connection.GetSourceRoom();
    if (here == null || there == null || here == there) return false;
    var sides = directions.Select(CompassPointHelper.GetCompassDirection).Distinct().ToList();
    // docked somewhere else on purpose: leave it
    if (!sides.Contains(current)) return false;

    var a = here.InnerBounds.Center;
    var b = there.InnerBounds.Center;
    var towards = new Vector(b.X - a.X, b.Y - a.Y);
    if (towards.Length < 0.01f) return false;
    towards = Vector.Normalize(towards);

    double Facing(CompassPoint side)
    {
      var v = CompassPointHelper.GetAutomapDirectionVector(side);
      var unit = Vector.Normalize(new Vector(v.X, v.Y));
      return unit.X * towards.X + unit.Y * towards.Y;
    }

    // fewer crossings first, then facing the other room; a little stickiness, so an end doesn't flicker between two
    // sides that do about as well
    var otherEnd = (atStart ? connection.VertexList[connection.VertexList.Count - 1] : connection.VertexList[0]).Position;
    var lines = Lines(elements, connection);
    double Score(CompassPoint side) => Crossings(here.PortAt(side).Position, otherEnd, lines) + (1 - Facing(side)) * 0.4;

    var best = sides.OrderBy(Score).First();
    if (best == current || Score(best) > Score(current) - 0.05) return false;
    var vertex = atStart ? connection.VertexList[0] : connection.VertexList[connection.VertexList.Count - 1];
    vertex.Port = here.PortAt(best);
    return true;
  }

  /// <summary>The straight pieces of every join but <paramref name="self" />, as drawn between their points.</summary>
  private static List<(Vector A, Vector B)> Lines(IEnumerable<Element> elements, Connection self)
  {
    var lines = new List<(Vector, Vector)>();
    foreach (var connection in elements.OfType<Connection>())
    {
      if (connection == self) continue;
      var vertices = connection.VertexList;
      for (var i = 0; i + 1 < vertices.Count; i++)
      {
        var a = vertices[i].Position;
        var b = vertices[i + 1].Position;
        if ((a - b).Length > 0.5f) lines.Add((a, b));
      }
    }

    return lines;
  }

  /// <summary>How many of <paramref name="lines" /> the line from <paramref name="p" /> to <paramref name="q" /> crosses.</summary>
  private static int Crossings(Vector p, Vector q, List<(Vector A, Vector B)> lines)
  {
    var count = 0;
    foreach (var (a, b) in lines)
      if (Cross(p, q, a, b))
        count++;
    return count;
  }

  // a proper crossing: lines that only meet at an end (two joins at one point of a room) don't count
  private static bool Cross(Vector p, Vector q, Vector a, Vector b)
  {
    static double Side(Vector o, Vector x, Vector y) => (x.X - o.X) * (y.Y - o.Y) - (x.Y - o.Y) * (y.X - o.X);
    var d1 = Side(a, b, p);
    var d2 = Side(a, b, q);
    var d3 = Side(p, q, a);
    var d4 = Side(p, q, b);
    return ((d1 > 0.5 && d2 < -0.5) || (d1 < -0.5 && d2 > 0.5)) && ((d3 > 0.5 && d4 < -0.5) || (d3 < -0.5 && d4 > 0.5));
  }

  private static void Block(HashSet<int> used, int point, bool withNeighbours)
  {
    used.Add(point);
    if (!withNeighbours) return;
    used.Add((point + 1) % 16);
    used.Add((point + 15) % 16);
  }
}
