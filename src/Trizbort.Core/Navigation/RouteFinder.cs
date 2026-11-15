using System;
using System.Collections.Generic;
using System.Linq;
using Trizbort.Automap;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

namespace Trizbort.Navigation;

/// <summary>
///   One step of a route: <see cref="Command" /> typed in <see cref="From" /> leads to <see cref="To" />, along
///   <see cref="Join" />. <see cref="Assumed" /> when automap only assumed the way (the way back of a join), and nobody
///   has gone it.
/// </summary>
public sealed record RouteStep(Room From, Room To, string Command, bool Assumed, Connection Join = null);

/// <summary>
///   Satnav for the map: the shortest list of commands that leads from one room to another along the map's joins
///   (their directions, and the commands written on them), as automap recorded them or the user drew them. It can only
///   follow the map: it knows nothing of the game's puzzles. Ways automap only assumed cost a little more than ways the
///   player has been seen to go, so a known route is preferred when there's a choice.
/// </summary>
public static class RouteFinder
{
  private const double SeenCost = 1.0;
  private const double AssumedCost = 1.5;

  // words written on a label that aren't commands: automap's "?" for a way it couldn't map, and a moving room that
  // moves by itself
  private static readonly HashSet<string> _notCommands = new(StringComparer.OrdinalIgnoreCase)
    { Automap.Automap.UnmappedWay, Automap.Automap.ByItself };

  /// <summary>
  ///   The place a room is: a portal ("To Front Porch") is the room it stands for. A moving room's stops are places of
  ///   their own (the lift at each floor).
  /// </summary>
  public static Room Place(Room room) => room is { IsPortal: true } ? room.ReferenceRoom ?? room : room;

  /// <summary>
  ///   The steps from <paramref name="from" /> to <paramref name="to" />: none when they're the same place, null when
  ///   the map shows no way.
  /// </summary>
  public static IReadOnlyList<RouteStep> Find(IEnumerable<Element> elements, Room from, Room to)
  {
    from = Place(from);
    to = Place(to);
    if (from == null || to == null) return null;
    if (from == to) return Array.Empty<RouteStep>();

    var ways = Ways(elements);
    var cost = new Dictionary<Room, double> { [from] = 0 };
    var cameBy = new Dictionary<Room, RouteStep>();
    var done = new HashSet<Room>();
    var queue = new PriorityQueue<Room, double>();
    queue.Enqueue(from, 0);
    while (queue.TryDequeue(out var room, out var sofar))
    {
      if (!done.Add(room)) continue;
      if (room == to) break;
      if (!ways.TryGetValue(room, out var leaving)) continue;
      foreach (var (step, stepCost) in leaving)
      {
        var next = sofar + stepCost;
        if (cost.TryGetValue(step.To, out var known) && known <= next) continue;
        cost[step.To] = next;
        cameBy[step.To] = step;
        queue.Enqueue(step.To, next);
      }
    }

    if (!cameBy.ContainsKey(to)) return null;
    var route = new List<RouteStep>();
    for (var room = to; room != from; room = cameBy[room].From) route.Add(cameBy[room]);
    route.Reverse();
    return route;
  }

  /// <summary>Every step the map shows, by the place it leaves, each with its cost.</summary>
  private static Dictionary<Room, List<(RouteStep Step, double Cost)>> Ways(IEnumerable<Element> elements)
  {
    var ways = new Dictionary<Room, List<(RouteStep, double)>>();
    foreach (var connection in elements.OfType<Connection>())
    {
      var start = Place(connection.GetSourceRoom());
      var end = Place(connection.GetTargetRoom());
      // a stub (an exit not yet explored) or a loop leads nowhere new
      if (start == null || end == null || start == end) continue;
      var recorded = !string.IsNullOrEmpty(connection.AutomapWays);
      foreach (var way in JoinWays.Of(connection))
      {
        // a one-way join leads only from its start
        if (!way.AtStart && connection.Flow == ConnectionFlow.OneWay) continue;
        var command = Command(way);
        if (command == null) continue;
        var (from, to) = way.AtStart ? (start, end) : (end, start);
        // automap's assumed way back; on a join the user drew, every way is as good as the user says
        var assumed = way.IsDirection && !way.Seen && recorded;
        // a direction beats a command, and a command a word written on a label, when they lead the same way
        var stepCost = (assumed ? AssumedCost : SeenCost) +
                       way.Kind switch { JoinWayKind.Direction => 0, JoinWayKind.Command => 0.01, _ => 0.02 };
        if (!ways.TryGetValue(from, out var leaving)) ways[from] = leaving = new List<(RouteStep, double)>();
        leaving.Add((new RouteStep(from, to, command, assumed, connection), stepCost));
      }
    }

    return ways;
  }

  /// <summary>What to type for a way: the direction's word ("north", "up", "in"), or the command or label as written.</summary>
  private static string Command(JoinWay way)
  {
    if (way.IsDirection)
      return way.Direction == MappableDirection.None ? null : DirectionWords.Word(way.Direction);
    var text = way.Text?.Trim();
    return string.IsNullOrEmpty(text) || _notCommands.Contains(text) ? null : text;
  }
}
