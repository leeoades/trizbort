using System;
using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

namespace Trizbort.Automap;

/// <summary>
///   What tells rooms with the same name apart, for "which room does it mean?": in a maze of rooms all alike, the name
///   says nothing, so each room is shown with what's different about it: its subtitle, its description, the objects
///   in it, or failing those the ways out of it and where they lead.
/// </summary>
public static class RoomTells
{
  /// <summary>
  ///   For each room, what sets it apart from the others ("objects: rod · ways: e to Maze, s to Dead End"), as little
  ///   as tells them all apart; empty for a room with nothing different about it.
  /// </summary>
  public static IReadOnlyDictionary<Room, string> Apart(IReadOnlyList<Room> rooms)
  {
    var parts = rooms.Distinct().ToDictionary(room => room, _ => new List<string>());
    if (parts.Count < 2) return parts.ToDictionary(pair => pair.Key, _ => string.Empty);

    var features = new Func<Room, IReadOnlyList<Room>, string>[]
    {
      (room, _) => string.IsNullOrWhiteSpace(room.SubTitle) ? string.Empty : room.SubTitle.Trim(),
      Description,
      (room, _) => Objects(room) is { Length: > 0 } objects ? "objects: " + objects : "no objects",
      (room, _) => Ways(room) is { Length: > 0 } ways ? "ways: " + ways : "no ways out"
    };

    var all = parts.Keys.ToList();
    foreach (var feature in features)
    {
      var values = all.ToDictionary(room => room, room => feature(room, all));
      // a feature every room shares says nothing
      if (values.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count() < 2) continue;
      foreach (var room in all)
        if (values[room].Length > 0)
          parts[room].Add(values[room]);
      if (all.Select(room => string.Join(" · ", parts[room])).Distinct(StringComparer.OrdinalIgnoreCase).Count() ==
          all.Count)
        break;
    }

    return parts.ToDictionary(pair => pair.Key, pair => string.Join(" · ", pair.Value));
  }

  /// <summary>
  ///   For each room whose subtitle and objects some other room shares, the ways out of it and where they lead ("ways: e
  ///   to Maze, s to Dead End"), or "no ways out"; empty for a room its subtitle and objects already tell apart. For "which
  ///   room does it mean?", which shows the subtitle and the objects on lines of their own, and the description beside
  ///   the list rather than in it.
  /// </summary>
  public static IReadOnlyDictionary<Room, string> WaysApart(IReadOnlyList<Room> rooms)
  {
    var all = rooms.Distinct().ToList();
    string Shown(Room room) => (room.SubTitle ?? string.Empty).Trim() + "\n" + Objects(room);
    return all.ToDictionary(room => room, room =>
    {
      var alike = all.Where(other => other != room &&
                                     string.Equals(Shown(other), Shown(room), StringComparison.OrdinalIgnoreCase)).ToList();
      if (alike.Count == 0) return string.Empty;
      var ways = Ways(room);
      // ways the others share too say nothing
      if (alike.All(other => string.Equals(Ways(other), ways, StringComparison.OrdinalIgnoreCase))) return string.Empty;
      return ways.Length > 0 ? "ways: " + ways : "no ways out";
    });
  }

  /// <summary>The objects in the room, listed ("black rod, bird"); empty when there are none.</summary>
  public static string ObjectList(Room room) => Objects(room);

  /// <summary>The objects in the room, one by one.</summary>
  public static IReadOnlyList<string> ObjectItems(Room room) =>
    (room.Objects ?? string.Empty)
    .Split(new[] { '\n', '\r', '|' }, StringSplitOptions.RemoveEmptyEntries)
    .Select(item => item.Trim().TrimStart('-').Trim()).Where(item => item.Length > 0).ToList();

  /// <summary>The first sentence of the room's description that the other rooms' descriptions don't have.</summary>
  private static string Description(Room room, IReadOnlyList<Room> rooms)
  {
    var mine = Sentences(room.PrimaryDescription);
    var others = rooms.Where(other => other != room).Select(other => Sentences(other.PrimaryDescription)).ToList();
    var different = mine.FirstOrDefault(sentence =>
      others.Any(other => !other.Contains(sentence, StringComparer.OrdinalIgnoreCase)));
    if (different == null) return mine.Count == 0 && others.Any(other => other.Count > 0) ? "no description" : string.Empty;
    return different.Length > 90 ? different[..87].TrimEnd() + "…" : different;
  }

  private static List<string> Sentences(string text)
  {
    var sentences = new List<string>();
    if (string.IsNullOrWhiteSpace(text)) return sentences;
    var flat = string.Join(" ", text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries));
    var start = 0;
    for (var index = 0; index < flat.Length; index++)
    {
      if (flat[index] is not ('.' or '!' or '?') || (index + 1 < flat.Length && flat[index + 1] != ' ')) continue;
      sentences.Add(flat[start..(index + 1)].Trim());
      start = index + 1;
    }

    if (start < flat.Length && flat[start..].Trim().Length > 0) sentences.Add(flat[start..].Trim());
    return sentences;
  }

  private static string Objects(Room room) => string.Join(", ", ObjectItems(room));

  /// <summary>
  ///   The ways out of the room and where they lead ("east to Maze, south to Dead End"), directions in full words as a
  ///   dialog shows them (the map may abbreviate them; a dialog has room to spell them out).
  /// </summary>
  private static string Ways(Room room)
  {
    var ways = new List<(string Way, string To)>();
    foreach (var connection in room.GetConnections())
    {
      var source = connection.GetSourceRoom(out var sourcePoint);
      var target = connection.GetTargetRoom(out var targetPoint);
      var atStart = source == room;
      if (!atStart && connection.Flow == ConnectionFlow.OneWay) continue;
      var other = atStart ? target : source;
      var to = other == null ? "nowhere" : other == room ? "here" : other.Name;
      var named = JoinWays.Of(connection)
        .Where(way => way.AtStart == atStart && (way.IsDirection || way.IsCommand))
        .Select(way => way.IsDirection ? Short(way.Direction) : way.Text)
        .Where(word => !string.IsNullOrWhiteSpace(word))
        .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
      if (named.Count == 0) named.Add(Short(atStart ? sourcePoint : targetPoint));
      foreach (var way in named.Where(word => word.Length > 0)) ways.Add((way, to));
    }

    return string.Join(", ", ways.OrderBy(way => Order(way.Way)).ThenBy(way => way.Way)
      .Select(way => $"{way.Way} to {way.To}"));
  }

  private static readonly string[] _compass =
  {
    "north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest", "up", "down", "in", "out"
  };

  private static int Order(string way)
  {
    var index = Array.IndexOf(_compass, way.ToLowerInvariant());
    return index < 0 ? _compass.Length : index;
  }

  private static string Short(MappableDirection direction) => DirectionWords.Word(direction);

  private static string Short(CompassPoint point) => point switch
  {
    CompassPoint.North => "north",
    CompassPoint.NorthEast => "northeast",
    CompassPoint.East => "east",
    CompassPoint.SouthEast => "southeast",
    CompassPoint.South => "south",
    CompassPoint.SouthWest => "southwest",
    CompassPoint.West => "west",
    CompassPoint.NorthWest => "northwest",
    _ => string.Empty
  };
}
