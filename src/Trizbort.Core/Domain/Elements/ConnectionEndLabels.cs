using System.Linq;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Domain.Elements;

/// <summary>
///   The text drawn at one end of a join: its own label, or, when it has none, the direction it leaves the room in
///   ("ne", "n"), if the map settings ask for diagonal (<see cref="Settings.LabelDiagonalExits" />) or straight
///   (<see cref="Settings.LabelCompassExits" />) exits to be labelled. Only drawn: the join's labels don't change.
/// </summary>
public static class ConnectionEndLabels
{
  /// <summary>
  ///   The direction the side an end leaves its room from gives it, when its label names no direction: a join on the
  ///   right of a room goes east. Null when the label names a direction ("down" on the south side is down, not south),
  ///   when the end isn't on one of the eight main sides, for a command's join or an end left by a command (the side
  ///   only faces the other room), for a loop, and for the far end of a one-way join (no way out of its room).
  /// </summary>
  public static MappableDirection? SideDirection(Connection connection, bool atStart)
  {
    var text = atStart ? connection.StartText : connection.EndText;
    if (DirectionWords.SplitLabel(text).Any(word => DirectionWords.TryParse(word, out _))) return null;
    if (!atStart && connection.Flow == ConnectionFlow.OneWay) return null;
    if (!string.IsNullOrEmpty(connection.AutomapCommand)) return null;
    var travelled = atStart ? connection.StartTravelled : connection.EndTravelled;
    if (!string.IsNullOrWhiteSpace(travelled) && travelled.Split(' ').Contains("command")) return null;
    var source = connection.GetSourceRoom(out var from);
    var target = connection.GetTargetRoom(out var to);
    var room = atStart ? source : target;
    if (room == null || (source == target && from != to)) return null;
    return (atStart ? from : to) switch
    {
      CompassPoint.North => MappableDirection.North,
      CompassPoint.NorthEast => MappableDirection.NorthEast,
      CompassPoint.East => MappableDirection.East,
      CompassPoint.SouthEast => MappableDirection.SouthEast,
      CompassPoint.South => MappableDirection.South,
      CompassPoint.SouthWest => MappableDirection.SouthWest,
      CompassPoint.West => MappableDirection.West,
      CompassPoint.NorthWest => MappableDirection.NorthWest,
      _ => null
    };
  }

  public static string Text(Connection connection, bool atStart)
  {
    var style = Settings.LabelDirectionWords;
    var full = style == DirectionWordStyle.Full;
    var text = atStart ? connection.StartText : connection.EndText;
    // the directions in it written one way, short or in full, if the map settings say so ("u" and "up" alike), and
    // up and down left off if the map settings say so
    if (!string.IsNullOrEmpty(text)) return Shaped(text, style, Settings.LabelUpDownExits);

    // only where the side is the way out (SideDirection): not the far end of a one-way join, and not a command's
    // join ("enter closet" leaving the foyer's corner isn't south-east)
    var side = SideDirection(connection, atStart);
    if (side is { } direction && Labelled(direction)) return DirectionWords.Styled(direction, full);
    return text;
  }

  /// <summary>
  ///   <paramref name="label" /> drawn with the map's direction words: as written, all short or all in full; with up
  ///   and down left off unless <paramref name="upDown" />. Anything that isn't a direction ("climb beanstalk") stays.
  /// </summary>
  public static string Shaped(string label, DirectionWordStyle style, bool upDown)
  {
    if (string.IsNullOrEmpty(label)) return label;
    if (!upDown)
    {
      var parts = DirectionWords.SplitLabel(label);
      var kept = parts.Where(part => !(DirectionWords.TryParse(part, out var way) &&
                                       way is MappableDirection.Up or MappableDirection.Down)).ToList();
      if (kept.Count != parts.Count) label = string.Join(" / ", kept);
    }
    return style == DirectionWordStyle.AsWritten ? label : DirectionWords.Restyle(label, style == DirectionWordStyle.Full);
  }

  /// <summary>Whether the map settings ask for exits that leave a room in <paramref name="direction" /> to be labelled.</summary>
  public static bool Labelled(MappableDirection direction) => direction switch
  {
    MappableDirection.North or MappableDirection.East or MappableDirection.South or MappableDirection.West =>
      Settings.LabelCompassExits,
    MappableDirection.NorthEast or MappableDirection.NorthWest or MappableDirection.SouthEast
      or MappableDirection.SouthWest => Settings.LabelDiagonalExits,
    MappableDirection.Up or MappableDirection.Down => Settings.LabelUpDownExits,
    _ => false
  };
}
