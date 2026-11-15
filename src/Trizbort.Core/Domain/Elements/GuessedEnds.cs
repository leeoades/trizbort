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
        if (FaceFloorEnd(list, connection, true)) moved++;
        if (FaceFloorEnd(list, connection, false)) moved++;
      }

      if (IsFreeCommandJoin(connection))
      {
        moved += FaceCommandJoin(list, connection, rooms);
        continue;
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
  ///   A dashed join automap made for a command ("pull first peg"), with no direction either way: no side of either
  ///   room means anything, so each end meets its room on the side facing the other room, and the line never runs
  ///   across either room.
  /// </summary>
  public static bool IsFreeCommandJoin(Connection connection) =>
    !string.IsNullOrEmpty(connection.AutomapCommand) && connection.VertexList.Count == 2;

  private static int FaceCommandJoin(IList<Element> elements, Connection connection, ICollection<Room> rooms)
  {
    var source = connection.GetSourceRoom(out var startPoint);
    var target = connection.GetTargetRoom(out var endPoint);
    if (source == null || target == null || source == target) return 0;
    if (rooms != null && !rooms.Contains(source) && !rooms.Contains(target)) return 0;
    var moved = 0;
    // an end already on a side facing the other room is left where it is
    if (!Faces(source, connection.VertexList[0].Position, target))
    {
      var start = FreeFacingPoint(elements, source, target, connection, target.InnerBounds.Center, startPoint);
      if (start != startPoint)
      {
        connection.VertexList[0].Port = source.PortAt(start);
        moved++;
      }
    }

    var last = connection.VertexList.Count - 1;
    if (!Faces(target, connection.VertexList[last].Position, source))
    {
      var end = FreeFacingPoint(elements, target, source, connection, connection.VertexList[0].Position, endPoint);
      if (end != endPoint)
      {
        connection.VertexList[last].Port = target.PortAt(end);
        moved++;
      }
    }

    return moved;
  }

  /// <summary>Whether <paramref name="point" /> on <paramref name="room" /> is on its side towards <paramref name="other" />.</summary>
  private static bool Faces(Room room, Vector point, Room other)
  {
    var centre = room.InnerBounds.Center;
    var toward = other.InnerBounds.Center - centre;
    var outward = point - centre;
    return toward.X * outward.X + toward.Y * outward.Y > 0;
  }

  /// <summary>
  ///   The side of <paramref name="from" /> facing <paramref name="to" /> (any of its sixteen points), or the nearest
  ///   one free on that side: a point a known exit uses is taken, and the points either side of it are avoided where
  ///   possible, so a guess doesn't look like that exit. A point another guessed arrow arrives at may be shared when
  ///   no clean point is near. When every point near the facing side is taken, the facing point is shared rather than
  ///   the end going round the room. <paramref name="self" /> is the
  ///   join being docked, if any.
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

    // points other joins meet the room at are taken; the points either side of a known exit are best avoided, so
    // a guess doesn't look like that exit, but they beat going round to another side of the room
    var taken = new HashSet<int>();
    var nearExit = new HashSet<int>();
    var arrivals = new HashSet<int>();
    foreach (var connection in elements.OfType<Connection>())
    {
      if (connection == self) continue;
      if (connection.GetSourceRoom(out var startPoint) == from) Mark(taken, nearExit, (int)startPoint);
      if (connection.GetTargetRoom(out var endPoint) != from) continue;
      // another arrow that only guesses where it arrives: sharing its point is better than going far round
      if (IsGuessedFarEnd(connection)) arrivals.Add((int)endPoint);
      else Mark(taken, nearExit, (int)endPoint);
    }

    var lines = otherEnd.HasValue ? Lines(elements, self) : null;
    CompassPoint? choice = null;
    var choiceScore = double.MaxValue;
    // stay on the side facing the other room: within two points of it, or four if those are all taken
    foreach (var reach in new[] { 2, 4 })
    {
      for (var step = 0; step <= reach; step++)
        foreach (var offset in step == 0 ? new[] { 0 } : new[] { step, -step })
        {
          var point = ((best + offset) % 16 + 16) % 16;
          if (taken.Contains(point)) continue;
          var score = step * 0.1 + (nearExit.Contains(point) ? 0.5 : 0) + (arrivals.Contains(point) ? 0.35 : 0);
          if (lines != null) score += Crossings(from.PortAt((CompassPoint)point).Position, otherEnd.Value, lines);
          // an end already here stays unless somewhere is clearly better
          if (current.HasValue && (int)current.Value == point) score -= 0.15;
          if (score < choiceScore)
          {
            choiceScore = score;
            choice = (CompassPoint)point;
          }
        }

      if (choice.HasValue) break;
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

  /// <summary>
  ///   An end of an automap join labelled only up or down ("d", "u / d") meets its room at one of the eight points
  ///   between the compass points (down starts at the bottom, left of centre). It moves to whichever of those, not used
  ///   by another join, has its line cross fewest other joins, then best faces the room at the other end; it stays
  ///   put unless somewhere is clearly better. An end docked on a compass point (by the user) is left alone. Returns
  ///   true if it moved.
  /// </summary>
  private static bool FaceFloorEnd(IList<Element> elements, Connection connection, bool atStart)
  {
    if (connection.VertexList.Count != 2) return false;
    // made by automap (drawn by hand, it stays where it was drawn)
    if (string.IsNullOrWhiteSpace(connection.StartTravelled) && string.IsNullOrWhiteSpace(connection.EndTravelled))
      return false;
    var label = atStart ? connection.StartText : connection.EndText;
    var directions = FloorWords(label);
    if (directions.Count == 0 ||
        directions.Any(direction => direction is not (MappableDirection.Up or MappableDirection.Down)))
      return false;

    CompassPoint current;
    var here = atStart ? connection.GetSourceRoom(out current) : connection.GetTargetRoom(out current);
    var there = atStart ? connection.GetTargetRoom() : connection.GetSourceRoom();
    if (here == null || there == null || here == there) return false;
    if (IsCompassPoint(current)) return false;

    var taken = new HashSet<CompassPoint>();
    foreach (var other in elements.OfType<Connection>())
    {
      if (other == connection) continue;
      if (other.GetSourceRoom(out var start) == here) taken.Add(start);
      if (other.GetTargetRoom(out var end) == here) taken.Add(end);
    }

    var centre = here.InnerBounds.Center;
    var towards = there.InnerBounds.Center - centre;
    if (towards.Length < 0.01f) return false;
    towards = Vector.Normalize(towards);
    var preferred = CompassPointHelper.GetCompassDirection(directions[0]);
    var otherEnd = (atStart ? connection.VertexList[connection.VertexList.Count - 1] : connection.VertexList[0]).Position;
    var lines = Lines(elements, connection);

    double Score(CompassPoint point)
    {
      var port = here.PortAt(point).Position;
      var outward = port - centre;
      var unit = outward.Length < 0.01f ? outward : Vector.Normalize(outward);
      var facing = unit.X * towards.X + unit.Y * towards.Y;
      return Crossings(port, otherEnd, lines) + (1 - facing) * 0.4 + (point == preferred ? -0.1 : 0);
    }

    var candidates = Enumerable.Range(0, 16).Select(i => (CompassPoint)i)
      .Where(point => !IsCompassPoint(point) && (!taken.Contains(point) || point == current)).ToList();
    if (candidates.Count == 0) return false;
    var best = candidates.OrderBy(Score).First();
    if (best == current || Score(best) > Score(current) - 0.05) return false;
    var vertex = atStart ? connection.VertexList[0] : connection.VertexList[connection.VertexList.Count - 1];
    vertex.Port = here.PortAt(best);
    return true;
  }

  /// <summary>
  ///   The directions a label names, if every word of it is a direction ("u / d"); otherwise none, as a label with
  ///   other words was written by someone.
  /// </summary>
  public static List<MappableDirection> FloorWords(string label)
  {
    var directions = new List<MappableDirection>();
    foreach (var word in DirectionWords.SplitLabel(label))
    {
      if (!DirectionWords.TryParse(word, out var direction)) return new List<MappableDirection>();
      directions.Add(direction);
    }

    return directions;
  }

  /// <summary>North, north-east, east...: the eight points a compass exit uses (the others lie between them).</summary>
  private static bool IsCompassPoint(CompassPoint point) => ((int)point & 1) == 0;

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

  private static void Mark(HashSet<int> taken, HashSet<int> nearExit, int point)
  {
    taken.Add(point);
    nearExit.Add((point + 1) % 16);
    nearExit.Add((point + 15) % 16);
  }
}
