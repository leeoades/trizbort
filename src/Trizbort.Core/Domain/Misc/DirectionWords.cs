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

  /// <summary>The short word for a direction: "n", "ne", "u", "in".</summary>
  public static string Short(MappableDirection direction) => direction switch
  {
    MappableDirection.North => "n",
    MappableDirection.NorthEast => "ne",
    MappableDirection.East => "e",
    MappableDirection.SouthEast => "se",
    MappableDirection.South => "s",
    MappableDirection.SouthWest => "sw",
    MappableDirection.West => "w",
    MappableDirection.NorthWest => "nw",
    MappableDirection.Up => "u",
    MappableDirection.Down => "d",
    MappableDirection.In => "in",
    MappableDirection.Out => "out",
    _ => string.Empty
  };

  /// <summary>The direction as a short word ("n") or in full ("north").</summary>
  public static string Styled(MappableDirection direction, bool full) => full ? Word(direction) : Short(direction);

  /// <summary>
  ///   <paramref name="label" /> with each direction in it written the same way, short ("n / u") or in full
  ///   ("north / up"); anything else in it ("climb beanstalk") as it was. A label with no directions comes back as it
  ///   was.
  /// </summary>
  public static string Restyle(string label, bool full)
  {
    var parts = SplitLabel(label);
    if (!parts.Any(part => TryParse(part, out _))) return label;
    return string.Join(" / ", parts.Select(part => TryParse(part, out var direction) ? Styled(direction, full) : part));
  }

  /// <summary>The directions a label can be picked from: the compass round from north, then up and down.</summary>
  public static readonly IReadOnlyList<MappableDirection> Pickable = new[]
  {
    MappableDirection.North, MappableDirection.NorthEast, MappableDirection.East, MappableDirection.SouthEast,
    MappableDirection.South, MappableDirection.SouthWest, MappableDirection.West, MappableDirection.NorthWest,
    MappableDirection.Up, MappableDirection.Down
  };

  /// <summary>
  ///   A label taken apart for picking: the <see cref="Pickable" /> directions in it, and everything else ("in",
  ///   "climb beanstalk") as the custom text, joined with " / ".
  /// </summary>
  public static (List<MappableDirection> Directions, string Custom, bool Full) Pick(string label)
  {
    var directions = new List<MappableDirection>();
    var custom = new List<string>();
    bool? full = null;
    foreach (var part in SplitLabel(label))
      if (TryParse(part, out var direction) && Pickable.Contains(direction))
      {
        if (!directions.Contains(direction)) directions.Add(direction);
        full ??= !string.Equals(part, Short(direction), StringComparison.OrdinalIgnoreCase);
      }
      else
        custom.Add(part);
    return (directions, string.Join(" / ", custom), full ?? false);
  }

  /// <summary>
  ///   The label for picked <paramref name="directions" />, in compass order then up and down, then the
  ///   <paramref name="custom" /> text, joined with " / "; directions short ("n") or in full ("north").
  /// </summary>
  public static string Compose(IEnumerable<MappableDirection> directions, string custom, bool full)
  {
    var picked = directions.ToHashSet();
    var parts = Pickable.Where(picked.Contains).Select(direction => Styled(direction, full)).ToList();
    parts.AddRange(SplitLabel(custom));
    return string.Join(" / ", parts);
  }

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
