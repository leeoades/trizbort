using System;
using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

namespace Trizbort.Automap;

/// <summary>What a <see cref="JoinWay" /> is: a direction, a command the player typed, or words the user wrote.</summary>
public enum JoinWayKind
{
  Direction,
  Command,
  Label
}

/// <summary>
///   One way along a join: a direction ("north"), a command ("climb up chimney") or words written on its label ("through
///   the arch"), from its start end or its end end. <see cref="Seen" /> when the player was seen going that way, not
///   just assumed. <see cref="Text" /> is the word as written, for a direction taken from a label ("n"); otherwise the
///   label shows the usual word.
/// </summary>
public sealed record JoinWay(bool AtStart, JoinWayKind Kind, MappableDirection Direction, string Text, bool Seen)
{
  public bool IsDirection => Kind == JoinWayKind.Direction;
  public bool IsCommand => Kind == JoinWayKind.Command;

  public static JoinWay Go(bool atStart, MappableDirection direction, bool seen, string text = null) =>
    new(atStart, JoinWayKind.Direction, direction, text, seen);

  public static JoinWay Typed(bool atStart, string command) =>
    new(atStart, JoinWayKind.Command, MappableDirection.None, command, true);

  public static JoinWay Written(bool atStart, string text) =>
    new(atStart, JoinWayKind.Label, MappableDirection.None, text, false);
}

/// <summary>
///   The ways along a join, kept as data (<see cref="Connection.AutomapWays" />) rather than worked out from its labels
///   every time. The labels are drawn from the ways: all the ways from one end on that end's label ("north / up"),
///   compass directions first and left off when the side the join leaves from says it; commands in the middle, or on
///   their end's label when they are the only way from that end and a direction leads back the other way ("enter
///   closet" one way, "north" back). A join automap never recorded, or whose labels or side the user has changed since,
///   has its ways read from its labels and sides instead, as before.
/// </summary>
public static class JoinWays
{
  /// <summary>
  ///   In <see cref="Connection.StartTravelled" />: the player left this end by a command written on its label, so the
  ///   side it leaves from isn't a direction.
  /// </summary>
  public const string ByCommand = "command";

  /// <summary>The ways along <paramref name="connection" />, in the order they were learnt.</summary>
  public static List<JoinWay> Of(Connection connection)
  {
    var stored = Parse(connection.AutomapWays);
    if (stored != null && connection.AutomapWaysFor == Stamp(connection, stored)) return stored;
    return FromLabels(connection);
  }

  /// <summary>
  ///   Records <paramref name="ways" /> on <paramref name="connection" /> and draws its labels from them, writing
  ///   directions with <paramref name="word" /> ("n" or "north"). The older attributes (<see cref="Connection.AutomapCommand" />,
  ///   <see cref="Connection.StartTravelled" />...) are kept in step for code, and Trizbort versions, that read them.
  /// </summary>
  public static void Write(Connection connection, IEnumerable<JoinWay> ways, Func<MappableDirection, string> word)
  {
    // a direction keeps the word it was first written with ("north" stays "north" when abbreviations are turned on)
    var list = ways.Select(way => way.IsDirection && way.Text == null ? way with { Text = word(way.Direction) } : way)
      .ToList();
    var start = EndLabel(connection, list, true, word);
    var end = EndLabel(connection, list, false, word);
    var middle = string.Join(" / ", Distinct(list.Where(way => way.IsCommand && !OnEndLabel(list, way.AtStart))
      .Select(way => way.Text)));
    if (connection.StartText != start) connection.StartText = start;
    if (connection.EndText != end) connection.EndText = end;
    if (connection.MidText != middle) connection.MidText = middle;

    var commandOnly = list.Any(way => way.IsCommand) && !list.Any(way => way.IsDirection);
    var automapCommand = commandOnly
      ? string.Join(" / ", Distinct(list.Where(way => way.IsCommand).Select(way => way.Text)))
      : string.Empty;
    if (connection.AutomapCommand != automapCommand) connection.AutomapCommand = automapCommand;
    var startTravelled = Travelled(list, true);
    var endTravelled = Travelled(list, false);
    if (connection.StartTravelled != startTravelled) connection.StartTravelled = startTravelled;
    if (connection.EndTravelled != endTravelled) connection.EndTravelled = endTravelled;

    connection.AutomapWays = Format(list);
    connection.AutomapWaysFor = Stamp(connection, list);
  }

  /// <summary>Whether a way from one end lists <paramref name="direction" />.</summary>
  public static bool Lists(IEnumerable<JoinWay> ways, bool atStart, MappableDirection direction) =>
    ways.Any(way => way.AtStart == atStart && way.IsDirection && way.Direction == direction);

  /// <summary>Whether the player was seen leaving from one end (by a direction or a command).</summary>
  public static bool SeenFrom(IEnumerable<JoinWay> ways, bool atStart) =>
    ways.Any(way => way.AtStart == atStart && way.Seen && way.Kind != JoinWayKind.Label);

  /// <summary>The first compass direction from one end, if any: the side the join should leave from.</summary>
  public static MappableDirection? FirstCompass(IEnumerable<JoinWay> ways, bool atStart)
  {
    foreach (var way in ways)
      if (way.AtStart == atStart && way.IsDirection && DirectionWords.IsCompass(way.Direction))
        return way.Direction;
    return null;
  }

  /// <summary>The commands that lead along the join, from either end.</summary>
  public static IEnumerable<string> Commands(IEnumerable<JoinWay> ways) =>
    ways.Where(way => way.IsCommand).Select(way => way.Text);

  /// <summary>
  ///   Adds <paramref name="direction" /> from one end, or marks it seen if it's there already and
  ///   <paramref name="seen" />. Returns true if it wasn't there.
  /// </summary>
  public static bool AddDirection(List<JoinWay> ways, bool atStart, MappableDirection direction, bool seen)
  {
    var index = ways.FindIndex(way => way.AtStart == atStart && way.IsDirection && way.Direction == direction);
    if (index >= 0)
    {
      if (seen && !ways[index].Seen) ways[index] = ways[index] with { Seen = true };
      return false;
    }

    ways.Add(JoinWay.Go(atStart, direction, seen));
    return true;
  }

  /// <summary>Adds a command from one end, unless it's there already. Returns true if it wasn't.</summary>
  public static bool AddCommand(List<JoinWay> ways, bool atStart, string command)
  {
    if (ways.Any(way => way.IsCommand && way.AtStart == atStart &&
                        string.Equals(way.Text, command, StringComparison.OrdinalIgnoreCase)))
      return false;
    ways.Add(JoinWay.Typed(atStart, command));
    return true;
  }

  // ---- drawing the labels ----

  /// <summary>
  ///   A command goes on its end's label when the player was seen going back from the other end by a direction ("in /
  ///   enter closet" at the foyer, "north" seen back), or by another command ("pull first peg" down, "pull second peg"
  ///   back up): it works from that end. Otherwise (a command join one way, or a way back only assumed) it goes in the
  ///   middle.
  /// </summary>
  private static bool OnEndLabel(List<JoinWay> ways, bool atStart) =>
    ways.Any(way => way.AtStart != atStart && (way.IsDirection && way.Seen || way.IsCommand));

  private static string EndLabel(Connection connection, List<JoinWay> ways, bool atStart,
    Func<MappableDirection, string> word)
  {
    var mine = ways.Where(way => way.AtStart == atStart).ToList();
    var commandsHere = OnEndLabel(ways, atStart);
    var parts = new List<(string Text, JoinWay Way)>();
    // compass directions first, then everything else in the order learnt (a loop keeps the order learnt)
    var ordered = IsLoop(connection)
      ? mine
      : mine.Where(way => way.IsDirection && DirectionWords.IsCompass(way.Direction))
        .Concat(mine.Where(way => !(way.IsDirection && DirectionWords.IsCompass(way.Direction))));
    foreach (var way in ordered)
    {
      if (way.IsCommand && !commandsHere) continue;
      var text = way.IsDirection ? way.Text ?? word(way.Direction) : way.Text;
      if (string.IsNullOrWhiteSpace(text)) continue;
      if (parts.Any(part => SameWay(part.Way, way) || string.Equals(part.Text, text, StringComparison.OrdinalIgnoreCase)))
        continue;
      parts.Add((text, way));
    }

    // a single compass direction the side already says is left off (but not on a loop, which leaves on one side and
    // comes back on another)
    var point = EndPoint(connection, atStart);
    if (parts.Count == 1 && parts[0].Way.IsDirection && IsMainPoint(point) &&
        parts[0].Way.Direction == DirectionOfMainPoint(point) && !IsLoop(connection))
      return string.Empty;
    return string.Join(" / ", parts.Select(part => part.Text));
  }

  private static bool SameWay(JoinWay one, JoinWay two) =>
    one.IsDirection && two.IsDirection && one.Direction == two.Direction;

  private static IEnumerable<string> Distinct(IEnumerable<string> texts)
  {
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var text in texts)
      if (!string.IsNullOrWhiteSpace(text) && seen.Add(text))
        yield return text;
  }

  private static string Travelled(List<JoinWay> ways, bool atStart)
  {
    var words = ways.Where(way => way.AtStart == atStart && way.IsDirection && way.Seen)
      .Select(way => DirectionWords.Word(way.Direction)).Distinct().ToList();
    if (ways.Any(way => way.AtStart == atStart && way.IsCommand) && OnEndLabel(ways, atStart)) words.Add(ByCommand);
    return string.Join(" ", words);
  }

  /// <summary>
  ///   What the labels were drawn as: each label, and the side of an end whose label was left off because the side
  ///   says the direction. If any of it changes, the user has changed the join, and its labels count.
  /// </summary>
  private static string Stamp(Connection connection, List<JoinWay> ways)
  {
    string Side(bool atStart, string label) =>
      label.Length == 0 && ways.Any(way => way.AtStart == atStart && way.IsDirection)
        ? EndPoint(connection, atStart).ToString()
        : string.Empty;

    return string.Join("|", connection.StartText, connection.MidText, connection.EndText,
      Side(true, connection.StartText), Side(false, connection.EndText));
  }

  // ---- reading the labels, for joins automap never recorded ----

  /// <summary>
  ///   The ways a join's labels and sides show: the directions written at each end, plus the compass direction of the
  ///   side an end leaves from when none is written (unless it's a command's join, or left by a command); other words
  ///   on an end's label as written; commands from the middle label.
  /// </summary>
  public static List<JoinWay> FromLabels(Connection connection)
  {
    var ways = new List<JoinWay>();
    var commandJoin = !string.IsNullOrEmpty(connection.AutomapCommand);
    // a loop leaves on one side and comes back on another: its sides say nothing, its label says it all
    var loop = IsLoop(connection);
    foreach (var atStart in new[] { true, false })
    {
      var words = DirectionWords.SplitLabel(atStart ? connection.StartText : connection.EndText);
      var travelled = TravelledWords(connection, atStart);
      var byCommand = travelled.Contains(ByCommand);
      var point = EndPoint(connection, atStart);
      // the side gives the direction only when the label names none: "down" drawn on the south side is down
      if (IsMainPoint(point) && !commandJoin && !byCommand && !loop &&
          !words.Any(w => DirectionWords.TryParse(w, out _)))
      {
        var side = DirectionOfMainPoint(point);
        ways.Add(JoinWay.Go(atStart, side, travelled.Contains(DirectionWords.Word(side))));
      }

      foreach (var w in words)
        if (DirectionWords.TryParse(w, out var direction))
          ways.Add(JoinWay.Go(atStart, direction, travelled.Contains(DirectionWords.Word(direction)), w));
        else if (byCommand)
          ways.Add(JoinWay.Typed(atStart, w));
        else
          ways.Add(JoinWay.Written(atStart, w));
    }

    // (a command join's commands written on its end labels are there already). A command join's command counts only
    // while its label still says it: the user clearing the label (or changing it) is the user saying it's not so
    var labels = new[] { connection.MidText, connection.StartText, connection.EndText }
      .SelectMany(DirectionWords.SplitLabel).ToList();
    var commands = commandJoin
      ? DirectionWords.SplitLabel(connection.AutomapCommand)
        .Where(command => labels.Contains(command, StringComparer.OrdinalIgnoreCase))
        .Concat(DirectionWords.SplitLabel(connection.MidText))
      : DirectionWords.SplitLabel(connection.MidText);
    foreach (var command in commands)
      if (!ways.Any(way => way.IsCommand && string.Equals(way.Text, command, StringComparison.OrdinalIgnoreCase)))
        ways.Add(JoinWay.Typed(true, command));
    return ways;
  }

  private static List<string> TravelledWords(Connection connection, bool atStart)
  {
    var text = atStart ? connection.StartTravelled : connection.EndTravelled;
    return string.IsNullOrWhiteSpace(text)
      ? new List<string>()
      : text.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
  }

  // ---- saving ----

  /// <summary>"s,d,north,1," — end, kind, direction, seen, text; ways separated by ";", text escaped.</summary>
  internal static string Format(IEnumerable<JoinWay> ways) =>
    string.Join(";", ways.Select(way => string.Join(",",
      way.AtStart ? "s" : "e",
      way.Kind switch { JoinWayKind.Direction => "d", JoinWayKind.Command => "c", _ => "l" },
      way.IsDirection ? DirectionWords.Word(way.Direction) : string.Empty,
      way.Seen ? "1" : "0",
      Uri.EscapeDataString(way.Text ?? string.Empty))));

  /// <summary>The ways <see cref="Format" /> wrote, or null if there are none or they can't be read.</summary>
  internal static List<JoinWay> Parse(string text)
  {
    if (string.IsNullOrWhiteSpace(text)) return null;
    var ways = new List<JoinWay>();
    foreach (var item in text.Split(';', StringSplitOptions.RemoveEmptyEntries))
    {
      var fields = item.Split(',');
      if (fields.Length != 5) return null;
      var atStart = fields[0] == "s";
      var seen = fields[3] == "1";
      var words = Uri.UnescapeDataString(fields[4]);
      switch (fields[1])
      {
        case "d":
          if (!DirectionWords.TryParse(fields[2], out var direction)) return null;
          ways.Add(JoinWay.Go(atStart, direction, seen, words.Length == 0 ? null : words));
          break;
        case "c":
          ways.Add(new JoinWay(atStart, JoinWayKind.Command, MappableDirection.None, words, seen));
          break;
        case "l":
          ways.Add(JoinWay.Written(atStart, words));
          break;
        default:
          return null;
      }
    }

    return ways;
  }

  // ---- sides ----

  private static CompassPoint EndPoint(Connection connection, bool atStart)
  {
    CompassPoint point;
    if (atStart) connection.GetSourceRoom(out point);
    else connection.GetTargetRoom(out point);
    return point;
  }

  private static bool IsLoop(Connection connection)
  {
    var source = connection.GetSourceRoom(out var from);
    var target = connection.GetTargetRoom(out var to);
    return source != null && source == target && from != to;
  }

  internal static bool IsMainPoint(CompassPoint point) => point is CompassPoint.North or CompassPoint.NorthEast
    or CompassPoint.East or CompassPoint.SouthEast or CompassPoint.South or CompassPoint.SouthWest
    or CompassPoint.West or CompassPoint.NorthWest;

  internal static MappableDirection DirectionOfMainPoint(CompassPoint point) => point switch
  {
    CompassPoint.North => MappableDirection.North,
    CompassPoint.NorthEast => MappableDirection.NorthEast,
    CompassPoint.East => MappableDirection.East,
    CompassPoint.SouthEast => MappableDirection.SouthEast,
    CompassPoint.South => MappableDirection.South,
    CompassPoint.SouthWest => MappableDirection.SouthWest,
    CompassPoint.West => MappableDirection.West,
    _ => MappableDirection.NorthWest
  };
}
