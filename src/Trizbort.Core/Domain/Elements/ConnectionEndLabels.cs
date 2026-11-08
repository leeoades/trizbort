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
  public static string Text(Connection connection, bool atStart)
  {
    var text = atStart ? connection.StartText : connection.EndText;
    if (!string.IsNullOrEmpty(text)) return text;

    // the far end of a one-way join isn't a way out of its room
    if (!atStart && connection.Flow == ConnectionFlow.OneWay) return text;

    CompassPoint point;
    var room = atStart ? connection.GetSourceRoom(out point) : connection.GetTargetRoom(out point);
    if (room == null) return text;

    return point switch
    {
      CompassPoint.North when Settings.LabelCompassExits => "n",
      CompassPoint.East when Settings.LabelCompassExits => "e",
      CompassPoint.South when Settings.LabelCompassExits => "s",
      CompassPoint.West when Settings.LabelCompassExits => "w",
      CompassPoint.NorthEast when Settings.LabelDiagonalExits => "ne",
      CompassPoint.NorthWest when Settings.LabelDiagonalExits => "nw",
      CompassPoint.SouthEast when Settings.LabelDiagonalExits => "se",
      CompassPoint.SouthWest when Settings.LabelDiagonalExits => "sw",
      _ => text
    };
  }
}
