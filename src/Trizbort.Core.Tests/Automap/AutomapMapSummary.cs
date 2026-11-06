using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;

namespace Trizbort.Core.Tests.Automap;

/// <summary>
///   A plain-text description of a map for the automap scenario tests: the rooms, every connection with the compass
///   ports it uses, and the room automap thinks the player is in. Positions are left out on purpose: a scenario
///   says what is connected to what, not where rooms land on the canvas.
/// </summary>
/// <remarks>
///   Format (see tests/automap/README.md):
///   <code>
///   current: Garden, North
///
///   rooms:
///   Garden, East (start)
///   Garden, North
///
///   connections:
///   Garden, East [north] &lt;-&gt; [south] Garden, North
///   Cellar [north "up"] &lt;-&gt; [south "down"] Hall
///   Pantry [east] -&gt; [west] Kitchen
///   Hall [west] stub
///
///   overlaps:
///   Patio is on the join South Junction - Southeast Junction
///   </code>
///   The overlaps section only appears when there are overlaps.
///   Rooms with the same name are numbered in the order they were created ("Maze #1", "Maze #2").
/// </remarks>
public static class AutomapMapSummary
{
  public static string Describe(Project project)
  {
    var rooms = project.Elements.OfType<Room>().ToList();
    var names = NameRooms(rooms);

    var builder = new StringBuilder();
    var current = project.ActiveSelectedElement as Room;
    builder.Append("current: ").Append(current != null && names.TryGetValue(current, out var currentName) ? currentName : "(none)").Append('\n');
    builder.Append('\n');

    builder.Append("rooms:\n");
    foreach (var room in rooms.OrderBy(r => names[r], StringComparer.Ordinal))
      builder.Append(names[room]).Append(room.IsStartRoom ? " (start)" : string.Empty).Append('\n');
    builder.Append('\n');

    builder.Append("connections:\n");
    var lines = project.Elements.OfType<Connection>().Select(c => DescribeConnection(c, names)).ToList();
    lines.Sort(StringComparer.Ordinal);
    foreach (var line in lines) builder.Append(line).Append('\n');

    // how readable the layout is: joins pointing the way their directions say (see LayoutTidy)
    var (natural, total) = Trizbort.Automap.LayoutTidy.NaturalJoins(project.Elements.ToList());
    builder.Append('\n');
    builder.Append($"natural joins: {natural} of {total}\n");

    var overlaps = Overlaps(project, rooms, names);
    if (overlaps.Count > 0)
    {
      builder.Append('\n');
      builder.Append("overlaps:\n");
      foreach (var line in overlaps) builder.Append(line).Append('\n');
    }

    return builder.ToString();
  }

  /// <summary>
  ///   Layout problems a player would notice: rooms on top of each other, and rooms sitting on a join that isn't
  ///   theirs. Exit stubs are left out.
  /// </summary>
  private static List<string> Overlaps(Project project, List<Room> rooms, Dictionary<Room, string> names)
  {
    var lines = new List<string>();
    for (var i = 0; i < rooms.Count; i++)
      for (var j = i + 1; j < rooms.Count; j++)
        if (rooms[i].InnerBounds.IntersectsWith(rooms[j].InnerBounds))
        {
          var pair = new[] { names[rooms[i]], names[rooms[j]] }.OrderBy(n => n, StringComparer.Ordinal).ToArray();
          lines.Add($"{pair[0]} overlaps {pair[1]}");
        }

    foreach (var connection in project.Elements.OfType<Connection>())
    {
      var a = connection.GetSourceRoom();
      var b = connection.GetTargetRoom();
      if (a == null || b == null || a == b) continue;
      foreach (var room in rooms)
        if (room != a && room != b && connection.Intersects(room.InnerBounds))
          lines.Add($"{names[room]} is on the join {names[a]} - {names[b]}");
    }

    lines.Sort(StringComparer.Ordinal);
    return lines;
  }

  /// <summary>Lines of a summary with trailing spaces and blank lines at the end removed, for comparing.</summary>
  public static IReadOnlyList<string> Normalise(string summary)
  {
    var lines = summary.Replace("\r\n", "\n").Split('\n').Select(l => l.TrimEnd()).ToList();
    while (lines.Count > 0 && lines[^1].Length == 0) lines.RemoveAt(lines.Count - 1);
    return lines;
  }

  private static Dictionary<Room, string> NameRooms(List<Room> rooms)
  {
    var names = new Dictionary<Room, string>();
    foreach (var group in rooms.GroupBy(r => r.Name ?? string.Empty))
    {
      var list = group.ToList();
      for (var i = 0; i < list.Count; i++)
        names[list[i]] = list.Count == 1 ? group.Key : group.Key + " #" + (i + 1);
    }

    return names;
  }

  private static string DescribeConnection(Connection connection, Dictionary<Room, string> names)
  {
    var source = connection.GetSourceRoom(out var sourcePoint);
    var target = connection.GetTargetRoom(out var targetPoint);
    var sourceName = source != null && names.TryGetValue(source, out var s) ? s : "?";
    var targetName = target != null && names.TryGetValue(target, out var t) ? t : "?";
    var sourcePort = Port(sourcePoint, connection.StartText);
    var targetPort = Port(targetPoint, connection.EndText);

    // An unexplored exit is a connection from a room's port back to the same port.
    if (source != null && source == target && sourcePoint == targetPoint) return $"{sourceName} [{Direction(sourcePoint)}] stub";

    var arrow = connection.Flow == ConnectionFlow.OneWay ? "->" : "<->";
    var dotted = connection.Style == ConnectionStyle.Dashed ? " (dotted)" : string.Empty;
    var label = string.IsNullOrEmpty(connection.MidText) ? string.Empty : $" \"{connection.MidText}\"";
    return $"{sourceName} [{sourcePort}] {arrow} [{targetPort}] {targetName}{label}{dotted}";
  }

  private static string Port(CompassPoint point, string label)
    => string.IsNullOrEmpty(label) ? Direction(point) : $"{Direction(point)} \"{label}\"";

  private static string Direction(CompassPoint point) => point.ToString().ToLowerInvariant();
}
