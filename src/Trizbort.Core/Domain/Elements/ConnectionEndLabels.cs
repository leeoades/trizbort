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
    var text = atStart ? connection.StartText : connection.EndText;
    if (!string.IsNullOrEmpty(text)) return text;

    // only where the side is the way out (SideDirection): not the far end of a one-way join, and not a command's
    // join ("enter closet" leaving the foyer's corner isn't south-east)
    return SideDirection(connection, atStart) switch
    {
      MappableDirection.North when Settings.LabelCompassExits => "n",
      MappableDirection.East when Settings.LabelCompassExits => "e",
      MappableDirection.South when Settings.LabelCompassExits => "s",
      MappableDirection.West when Settings.LabelCompassExits => "w",
      MappableDirection.NorthEast when Settings.LabelDiagonalExits => "ne",
      MappableDirection.NorthWest when Settings.LabelDiagonalExits => "nw",
      MappableDirection.SouthEast when Settings.LabelDiagonalExits => "se",
      MappableDirection.SouthWest when Settings.LabelDiagonalExits => "sw",
      _ => text
    };
  }
}
