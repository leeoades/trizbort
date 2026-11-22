using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;

namespace Trizbort.Domain.Misc;

/// <summary>Text after an edit, with what's selected in it (the caret, when they're the same).</summary>
public sealed record MarkdownEdit(string Text, int SelectionStart, int SelectionEnd);

/// <summary>A room or object to link a label to: shown as <see cref="Name" /> (and where), written as <see cref="Markdown" />.</summary>
public sealed record MarkdownLinkChoice(string Name, string Where, string Markdown, Room Room);

/// <summary>
///   The label editor's toolbar, on the text being written (Lee, 22 Nov 2026): bold and italic round what's
///   selected (or taken off again), headings and lists at the start of each selected line, a table or rule on lines
///   of their own, and links to rooms and objects.
/// </summary>
public static class MarkdownEditing
{
  /// <summary>A table to start from: two headings and a few rows.</summary>
  public const string SampleTable =
    "| Item | Where |\n" +
    "|---|---|\n" +
    "| Brass lantern | Inside Building |\n" +
    "| Gold nugget | Nugget of Gold Room |\n" +
    "| Bird cage | Cobble Crawl |";

  public const string Rule = "---";

  /// <summary>
  ///   <paramref name="mark" /> (** for bold, * for italic, ` for code) round the selection, or taken off when it's
  ///   there already; with nothing selected, a pair with the caret between, to type into.
  /// </summary>
  public static MarkdownEdit Wrap(string text, int start, int end, string mark)
  {
    text ??= string.Empty;
    (start, end) = Order(text, start, end);
    var length = mark.Length;

    // a word picked by double-click can bring its space with it: keep that outside the marks
    while (end > start && char.IsWhiteSpace(text[end - 1])) end--;
    while (start < end && char.IsWhiteSpace(text[start])) start++;

    if (start == end)
      return new MarkdownEdit(text.Insert(start, mark + mark), start + length, start + length);

    // marked already, just outside the selection: take them off
    if (start >= length && end + length <= text.Length && text.Substring(start - length, length) == mark &&
        text.Substring(end, length) == mark && !LongerMark(text, start - length, end + length, mark))
    {
      var without = text.Remove(end, length).Remove(start - length, length);
      return new MarkdownEdit(without, start - length, end - length);
    }

    // or inside it
    var selected = text.Substring(start, end - start);
    if (selected.Length >= length * 2 && selected.StartsWith(mark, StringComparison.Ordinal) &&
        selected.EndsWith(mark, StringComparison.Ordinal) && !LongerMark(selected, 0, selected.Length, mark))
    {
      var inner = selected.Substring(length, selected.Length - length * 2);
      return new MarkdownEdit(text.Remove(start, end - start).Insert(start, inner), start, start + inner.Length);
    }

    var wrapped = text.Insert(end, mark).Insert(start, mark);
    return new MarkdownEdit(wrapped, start + length, end + length);
  }

  /// <summary>Whether the marks found are part of a longer one: * found inside ** is bold, not italic.</summary>
  private static bool LongerMark(string text, int open, int close, string mark)
  {
    if (mark != "*") return false;
    var before = open > 0 && text[open - 1] == '*';
    var after = close < text.Length && text[close] == '*';
    var inside = open + 1 < text.Length && text[open + 1] == '*' && close - 2 >= 0 && text[close - 2] == '*';
    return before && after || inside && close - open > 3;
  }

  /// <summary>The kinds of line the toolbar starts: a heading, a bullet, a numbered item.</summary>
  public enum LineKind
  {
    Heading,
    Bullet,
    Numbered
  }

  private static readonly Regex _heading = new(@"^#{1,6}\s+");
  private static readonly Regex _bullet = new(@"^(\s*)[-*+]\s+");
  private static readonly Regex _numbered = new(@"^(\s*)\d+[.)]\s+");

  /// <summary>
  ///   Each line the selection touches made a heading, a bullet or a numbered item (1., 2., 3.), or, when they all
  ///   are already, made plain again.
  /// </summary>
  public static MarkdownEdit Lines(string text, int start, int end, LineKind kind)
  {
    text ??= string.Empty;
    (start, end) = Order(text, start, end);
    var first = text.LastIndexOf('\n', Math.Max(0, start - 1)) + 1;
    if (start == 0) first = 0;
    var last = text.IndexOf('\n', end > start && end > 0 && text[end - 1] == '\n' ? end - 1 : end);
    if (last < 0) last = text.Length;
    var lines = text.Substring(first, last - first).Split('\n');
    var pattern = kind switch
    {
      LineKind.Heading => _heading,
      LineKind.Bullet => _bullet,
      _ => _numbered
    };

    var all = lines.Where(line => line.Trim().Length > 0).All(line => pattern.IsMatch(line)) &&
              lines.Any(line => line.Trim().Length > 0);
    var number = 0;
    var changed = lines.Select(line =>
    {
      if (all) return pattern.Replace(line, match => match.Groups.Count > 1 ? match.Groups[1].Value : string.Empty, 1);
      if (line.Trim().Length == 0 && lines.Length > 1) return line;
      // one kind of line at a time: a heading made a bullet loses its #
      var plain = _numbered.Replace(_bullet.Replace(_heading.Replace(line, string.Empty, 1), "$1", 1), "$1", 1);
      return kind switch
      {
        LineKind.Heading => "## " + plain.TrimStart(),
        LineKind.Bullet => "- " + plain,
        _ => $"{++number}. " + plain
      };
    }).ToList();

    var joined = string.Join("\n", changed);
    var result = text.Substring(0, first) + joined + text.Substring(last);
    // what was one empty line, with the caret on it: the caret goes after what was put there
    if (start == end && lines.Length == 1)
    {
      var caret = first + joined.Length;
      return new MarkdownEdit(result, caret, caret);
    }

    return new MarkdownEdit(result, first, first + joined.Length);
  }

  /// <summary>
  ///   <paramref name="block" /> (a table, a rule) in place of the selection, on lines of its own; the caret after it.
  ///   Returns the block selected when it's a table, to type over its sample.
  /// </summary>
  public static MarkdownEdit Block(string text, int start, int end, string block, bool selectIt = false)
  {
    text ??= string.Empty;
    (start, end) = Order(text, start, end);
    var before = start > 0 && text[start - 1] != '\n' ? "\n" : string.Empty;
    var after = end < text.Length && text[end] != '\n' ? "\n" : string.Empty;
    var put = before + block + after;
    var result = text.Remove(start, end - start).Insert(start, put);
    var blockStart = start + before.Length;
    var blockEnd = blockStart + block.Length;
    return selectIt ? new MarkdownEdit(result, blockStart, blockEnd) : new MarkdownEdit(result, blockEnd, blockEnd);
  }

  /// <summary><paramref name="link" /> in place of the selection; the caret after it.</summary>
  public static MarkdownEdit Insert(string text, int start, int end, string link)
  {
    text ??= string.Empty;
    (start, end) = Order(text, start, end);
    var result = text.Remove(start, end - start).Insert(start, link);
    return new MarkdownEdit(result, start + link.Length, start + link.Length);
  }

  /// <summary>
  ///   A link to <paramref name="room" />, showing <paramref name="shown" /> when it's other words (an object's
  ///   name, or what was selected): [[Room]], [[Room|shown]], or with the room's id when its name is shared.
  /// </summary>
  public static string RoomLink(Project project, Room room, string shown = null)
  {
    var link = LabelMarkdown.RoomLink(project, room);
    if (string.IsNullOrWhiteSpace(shown) || string.Equals(shown.Trim(), room.Name, StringComparison.OrdinalIgnoreCase))
      return link;
    return link.Substring(0, link.Length - 2) + "|" + shown.Trim().Replace("|", "/").Replace("]]", "]") + "]]";
  }

  private static readonly Regex _objectMarks = new(@"\s*\[[^\]]*\]\s*$");

  /// <summary>The rooms of <paramref name="project" />, by name; a name shared shows where each is.</summary>
  public static IReadOnlyList<MarkdownLinkChoice> Rooms(Project project)
  {
    var rooms = project?.Elements.OfType<Room>().Where(room => !string.IsNullOrWhiteSpace(room.Name)).ToList() ??
                new List<Room>();
    return rooms
      .Select(room =>
      {
        var shared = rooms.Count(other => string.Equals(other.Name, room.Name, StringComparison.OrdinalIgnoreCase)) > 1;
        var where = shared ? Where(room) : string.Empty;
        return new MarkdownLinkChoice(room.Name, where, RoomLink(project, room), room);
      })
      .OrderBy(choice => choice.Name, StringComparer.CurrentCultureIgnoreCase)
      .ThenBy(choice => choice.Room.Id)
      .ToList();
  }

  /// <summary>The objects in the rooms of <paramref name="project" />, by name, each with the room it's in.</summary>
  public static IReadOnlyList<MarkdownLinkChoice> Objects(Project project)
  {
    var rooms = project?.Elements.OfType<Room>().ToList() ?? new List<Room>();
    return rooms
      .SelectMany(room => room.ListOfObjects().Select(item => (Item: _objectMarks.Replace(item, string.Empty).Trim(), Room: room)))
      .Where(found => found.Item.Length > 0)
      .Select(found => new MarkdownLinkChoice(found.Item, "in " + found.Room.Name,
        RoomLink(project, found.Room, found.Item), found.Room))
      .OrderBy(choice => choice.Name, StringComparer.CurrentCultureIgnoreCase)
      .ThenBy(choice => choice.Where, StringComparer.CurrentCultureIgnoreCase)
      .ToList();
  }

  /// <summary><paramref name="choices" /> with <paramref name="search" /> in their name, or in where they are.</summary>
  public static IReadOnlyList<MarkdownLinkChoice> Matching(IEnumerable<MarkdownLinkChoice> choices, string search)
  {
    if (string.IsNullOrWhiteSpace(search)) return choices.ToList();
    var words = search.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    return choices.Where(choice => words.All(word =>
      choice.Name.Contains(word, StringComparison.CurrentCultureIgnoreCase) ||
      choice.Where.Contains(word, StringComparison.CurrentCultureIgnoreCase))).ToList();
  }

  /// <summary>How to tell rooms with the same name apart: their region, or where on the map they are.</summary>
  private static string Where(Room room)
  {
    var region = string.IsNullOrWhiteSpace(room.Region) || room.Region == Region.DefaultRegion ? string.Empty : room.Region + ", ";
    return $"{region}#{room.Id}";
  }

  private static (int Start, int End) Order(string text, int start, int end)
  {
    start = Math.Clamp(start, 0, text.Length);
    end = Math.Clamp(end, 0, text.Length);
    return start <= end ? (start, end) : (end, start);
  }
}
