using System;
using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.Application;
using Trizbort.Domain.Misc;

namespace Trizbort.Domain.Elements;

/// <summary>
///   Joins that go the same way out of the same side of a room to different rooms (south from the Front Porch to
///   South Junction, and south to the Garden) are drawn as one join out of that room, with one set of labels, that
///   forks near the rooms it leads to. They are still separate joins in the map; only the drawing changes.
/// </summary>
public static class ForkedJoins
{
  /// <summary>How far along, from the shared room towards the rooms the joins lead to, they part.</summary>
  public const float ForkAt = 0.6f;

  /// <summary>
  ///   Where a join forks: at its start or its end (the shared room), the point where it parts from the others,
  ///   whether this join draws the shared part and its label (one of them does), and the shared part, from the room out.
  /// </summary>
  public sealed record Fork(bool AtStart, Vector Point, bool DrawsTrunk, IReadOnlyList<LineSegment> Trunk);

  /// <summary>The fork <paramref name="connection" /> is part of, if it's drawn as one.</summary>
  public static Fork Find(Connection connection)
  {
    if (!Plain(connection)) return null;
    return FindAt(connection, true) ?? FindAt(connection, false);
  }

  /// <summary>A straight join between two different rooms, drawn whole (not split, not curved).</summary>
  private static bool Plain(Connection connection) =>
    connection.VertexList.Count == 2 && !connection.Split && !connection.HasCurveWaypoints &&
    connection.VertexList[0].Port?.Owner is Room start && connection.VertexList[1].Port?.Owner is Room end &&
    start != end;

  private static Fork FindAt(Connection connection, bool atStart)
  {
    var port = connection.VertexList[atStart ? 0 : 1].Port;
    var elements = connection.Project?.Elements ?? Project.Current?.Elements;
    if (elements == null) return null;

    // most joins have their side of the room to themselves: find that out without making anything
    var crowded = false;
    foreach (var element in elements)
      if (element is Connection other && other != connection && other.VertexList.Count == 2 &&
          (other.VertexList[0].Port == port || other.VertexList[1].Port == port))
      {
        crowded = true;
        break;
      }

    if (!crowded) return null;

    var way = Way(connection, atStart);

    var members = new List<(Connection Join, bool AtStart)>();
    foreach (var element in elements)
    {
      if (element is not Connection other || !Plain(other)) continue;
      bool otherAtStart;
      if (other.VertexList[0].Port == port) otherAtStart = true;
      else if (other.VertexList[1].Port == port) otherAtStart = false;
      else continue;
      if (other.Style != connection.Style || other.ConnectionColor != connection.ConnectionColor) continue;
      if (Way(other, otherAtStart) != way) continue;
      members.Add((other, otherAtStart));
    }

    if (members.Count < 2) return null;
    // each to a different room: two joins to the same room are drawn as they are
    var farRooms = members.Select(member => member.Join.VertexList[member.AtStart ? 1 : 0].Port.Owner).ToList();
    if (farRooms.Distinct().Count() != farRooms.Count) return null;

    var shared = connection.VertexList[atStart ? 0 : 1];
    var foot = shared.Position;
    var stalk = StalkOf(shared);
    var towards = Vector.Zero;
    foreach (var member in members) towards += StalkOf(member.Join.VertexList[member.AtStart ? 1 : 0]);
    towards /= members.Count;
    if ((towards - stalk).Length < 1) return null;

    var point = stalk + (towards - stalk) * ForkAt;
    var trunk = new List<LineSegment>();
    if ((stalk - foot).Length > 0.01f) trunk.Add(new LineSegment(foot, stalk));
    trunk.Add(new LineSegment(stalk, point));
    var drawsTrunk = members.Min(member => member.Join.Id) == connection.Id;
    return new Fork(atStart, point, drawsTrunk, trunk);
  }

  private static Vector StalkOf(Vertex vertex) =>
    vertex.Port is { HasStalk: true } ? vertex.Port.StalkPosition : vertex.Position;

  /// <summary>
  ///   What the join says at that end, and which way it goes from there: joins fork only when they read the same out
  ///   of the shared room ("s", or nothing) and their arrows, if any, point the same way.
  /// </summary>
  private static (string Text, int Flow) Way(Connection connection, bool atStart)
  {
    var text = (ConnectionEndLabels.Text(connection, atStart) ?? string.Empty).Trim().ToLowerInvariant();
    var flow = connection.Flow == ConnectionFlow.OneWay ? atStart ? 1 : -1 : 0;
    return (text, flow);
  }

  /// <summary>
  ///   The join's segments as drawn in a fork, out of (or into) the shared room: along the shared part to the fork,
  ///   then on to the room it leads to.
  /// </summary>
  public static List<LineSegment> Segments(Connection connection, Fork fork)
  {
    var far = connection.VertexList[fork.AtStart ? 1 : 0];
    var farStalk = StalkOf(far);
    var outward = new List<LineSegment>(fork.Trunk) { new(fork.Point, farStalk) };
    if ((far.Position - farStalk).Length > 0.01f) outward.Add(new LineSegment(farStalk, far.Position));
    if (fork.AtStart) return outward;

    var inward = new List<LineSegment>(outward.Count);
    for (var index = outward.Count - 1; index >= 0; index--)
      inward.Add(new LineSegment(outward[index].End, outward[index].Start));
    return inward;
  }

  /// <summary>Whether a segment of a join in a fork lies along the shared part (which only one of them draws).</summary>
  public static bool OnTrunk(Fork fork, LineSegment segment)
  {
    var middle = (segment.Start + segment.End) * 0.5f;
    return fork.Trunk.Any(part => middle.DistanceFromLineSegment(part) < 0.5f);
  }
}
