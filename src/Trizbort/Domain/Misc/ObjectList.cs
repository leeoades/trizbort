using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Trizbort.Domain.Misc;

/// <summary>
///   One parsed line of a room's free-text object list.
/// </summary>
public class ObjectListItem
{
  /// <summary>The object name, without indentation, bullets or [property] flags.</summary>
  public string Name { get; set; }

  /// <summary>The contents of the [property] flags, e.g. "cs" for "[cs]"; empty if none.</summary>
  public string PropString { get; set; }

  /// <summary>
  ///   Raw indentation weight of the line (spaces count 1, tabs and bullets count 4).
  ///   Only meaningful relative to other lines in the same list.
  /// </summary>
  public int Indent { get; set; }

  /// <summary>Nesting depth: 0 for top-level objects, 1 for objects inside those, and so on.</summary>
  public int Depth { get; set; }

  /// <summary>Index of the containing item in the parsed list, or -1 for top-level objects.</summary>
  public int ParentIndex { get; set; } = -1;
}

/// <summary>
///   Parses a room's object list. Each non-blank line is an object. A line which is indented
///   (with spaces or tabs) and/or prefixed with one or more bullets ("-", "*" or "•") further than
///   the line before it is contained by that earlier object, e.g.
///   <code>
///   Chest
///   - Gold coin
///   - Pouch
///   -- Gem
///   </code>
/// </summary>
public static class ObjectList
{
  public const int TabIndent = 4;
  public const int BulletIndent = 4;
  public const string DisplayBullet = "\u2022 ";

  private static readonly Regex _propertiesRegex = new(@"\[[^\]\[]*\]");
  private static readonly char[] _bullets = { '-', '*', '\u2022' };

  public static List<ObjectListItem> Parse(string text)
  {
    var items = new List<ObjectListItem>();
    if (string.IsNullOrEmpty(text)) return items;

    foreach (var line in text.Replace("\r", string.Empty).Split(
               new[] { '\n' },
               StringSplitOptions.RemoveEmptyEntries))
    {
      var item = ParseLine(line);
      if (item == null) continue;

      for (var index = items.Count - 1; index >= 0; --index)
        if (item.Indent > items[index].Indent)
        {
          item.ParentIndex = index;
          item.Depth = items[index].Depth + 1;
          break;
        }

      items.Add(item);
    }

    return items;
  }

  /// <summary>
  ///   Parse a single line; returns null if the line has no object name.
  /// </summary>
  public static ObjectListItem ParseLine(string line)
  {
    var indent = 0;
    var position = LeadingIndentLength(line, ref indent);

    var content = line.Substring(position).Trim();
    var propString = string.Empty;
    if (_propertiesRegex.IsMatch(content))
    {
      propString = Regex.Replace(content, @".*\[", string.Empty);
      propString = Regex.Replace(propString, @"\].*", string.Empty);
    }

    var name = _propertiesRegex.Replace(content, string.Empty).Trim();
    if (string.IsNullOrEmpty(name)) return null;

    return new ObjectListItem { Name = name, PropString = propString, Indent = indent };
  }

  /// <summary>
  ///   Returns the length of the line's leading indentation (whitespace and bullets) and
  ///   accumulates its indentation weight into <paramref name="indent" />.
  /// </summary>
  public static int LeadingIndentLength(string line, ref int indent)
  {
    var position = 0;
    while (position < line.Length)
    {
      var c = line[position];
      if (c == ' ')
      {
        indent += 1;
        ++position;
      }
      else if (c == '\t')
      {
        indent += TabIndent;
        ++position;
      }
      else if (Array.IndexOf(_bullets, c) >= 0)
      {
        // a run of bullet characters only counts as indentation if followed by whitespace,
        // so that names such as "-shaped key" or "*star*" are left alone
        var end = position;
        while (end < line.Length && Array.IndexOf(_bullets, line[end]) >= 0) ++end;
        if (end >= line.Length || !char.IsWhiteSpace(line[end])) break;
        indent += (end - position) * BulletIndent;
        position = end;
      }
      else
      {
        break;
      }
    }

    return position;
  }

  public static int LeadingIndentLength(string line)
  {
    var indent = 0;
    return LeadingIndentLength(line, ref indent);
  }

  /// <summary>
  ///   Returns true if any object in the list is contained within another.
  /// </summary>
  public static bool HasNesting(IEnumerable<ObjectListItem> items)
  {
    return items.Any(item => item.Depth > 0);
  }

  /// <summary>
  ///   Format an object list for display on the map: [property] flags are removed and
  ///   contained objects are shown indented under their container with a bullet.
  /// </summary>
  public static string FormatForDisplay(string text)
  {
    if (string.IsNullOrEmpty(text)) return string.Empty;

    var items = Parse(text);
    if (!HasNesting(items))
      return _propertiesRegex.Replace(text, string.Empty);

    var builder = new StringBuilder();
    foreach (var item in items)
    {
      if (builder.Length > 0) builder.Append("\r\n");
      if (item.Depth > 0)
      {
        builder.Append(' ', (item.Depth - 1) * 4 + 2);
        builder.Append(DisplayBullet);
      }

      builder.Append(item.Name);
    }

    return builder.ToString();
  }
}