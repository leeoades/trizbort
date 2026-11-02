using System;
using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.Enums;

namespace Trizbort.Domain.Misc;

/// <summary>
///   Direction words as they appear in connection labels: "north", "ne", "up", "out"... Automap writes them, and code
///   export reads them, so a connection labelled "north / down" means both directions.
/// </summary>
public static class DirectionWords
{
  private static readonly Dictionary<string, MappableDirection> _words = new(StringComparer.OrdinalIgnoreCase)
  {
    ["north"] = MappableDirection.North, ["n"] = MappableDirection.North,
    ["northeast"] = MappableDirection.NorthEast, ["ne"] = MappableDirection.NorthEast,
    ["east"] = MappableDirection.East, ["e"] = MappableDirection.East,
    ["southeast"] = MappableDirection.SouthEast, ["se"] = MappableDirection.SouthEast,
    ["south"] = MappableDirection.South, ["s"] = MappableDirection.South,
    ["southwest"] = MappableDirection.SouthWest, ["sw"] = MappableDirection.SouthWest,
    ["west"] = MappableDirection.West, ["w"] = MappableDirection.West,
    ["northwest"] = MappableDirection.NorthWest, ["nw"] = MappableDirection.NorthWest,
    ["up"] = MappableDirection.Up, ["u"] = MappableDirection.Up,
    ["down"] = MappableDirection.Down, ["d"] = MappableDirection.Down,
    ["in"] = MappableDirection.In, ["out"] = MappableDirection.Out
  };

  /// <summary>The direction a single word means, if it is one.</summary>
  public static bool TryParse(string word, out MappableDirection direction)
  {
    direction = MappableDirection.None;
    return word != null && _words.TryGetValue(word.Trim(), out direction);
  }

  /// <summary>The word automap writes for a direction ("north", "up").</summary>
  public static string Word(MappableDirection direction) => direction.ToString().ToLowerInvariant();

  /// <summary>Up, down, in and out aren't compass directions.</summary>
  public static bool IsCompass(MappableDirection direction) =>
    direction is not (MappableDirection.Up or MappableDirection.Down or MappableDirection.In or MappableDirection.Out
      or MappableDirection.None);

  /// <summary>The parts of a label separated by "/", trimmed: "north / down" is "north" and "down".</summary>
  public static List<string> SplitLabel(string label) =>
    string.IsNullOrWhiteSpace(label)
      ? new List<string>()
      : label.Split('/').Select(part => part.Trim()).Where(part => part.Length > 0).ToList();

  /// <summary>
  ///   The directions a label lists when it lists more than one ("north / down"); empty for anything else, including
  ///   a single word such as "up", which keeps its usual meaning.
  /// </summary>
  public static List<MappableDirection> ListedDirections(string label)
  {
    var directions = new List<MappableDirection>();
    foreach (var part in SplitLabel(label))
      if (TryParse(part, out var direction) && !directions.Contains(direction))
        directions.Add(direction);
    return directions.Count >= 2 ? directions : new List<MappableDirection>();
  }
}
