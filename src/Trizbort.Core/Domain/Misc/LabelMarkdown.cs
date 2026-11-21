using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;

namespace Trizbort.Domain.Misc;

/// <summary>A run of a label's text in one style: bold, italic, code, or a link.</summary>
public sealed record MarkdownSpan(string Text, bool Bold = false, bool Italic = false, bool Code = false,
  string Link = null);

public enum MarkdownBlockKind
{
  Paragraph,
  Heading,
  Bullet,
  Numbered,
  Table,
  Rule,
  Blank
}

/// <summary>
///   One block of a label's Markdown: a line of text, a heading (level 1 to 3), a list item (with its indent level and,
///   numbered, its number), a horizontal rule, a blank line, or a table (its rows of cells, the first the header when
///   <see cref="HasHeader" />).
/// </summary>
public sealed record MarkdownBlock(
  MarkdownBlockKind Kind,
  IReadOnlyList<MarkdownSpan> Spans,
  int Level = 0,
  int Number = 0,
  IReadOnlyList<IReadOnlyList<IReadOnlyList<MarkdownSpan>>> Rows = null,
  bool HasHeader = false);

/// <summary>
///   The Markdown a map label can be written in (Lee, 21 Nov 2026: formatted text on the map, with tables and links to
///   rooms). A small, forgiving subset: # headings, **bold**, *italic*, `code`, - and 1. lists, --- rules, | tables |,
///   [text](https://...) links, and room links, [[Room Name]] or [[Room Name|text shown]]; a room that shares its name
///   ("Maze") is linked by its id, [[Maze#42]]. Each line of text stays a line, as in a plain label.
/// </summary>
public static class LabelMarkdown
{
  /// <summary>The prefix of a link to a room: "room:Name", or "room:Name#42" for the room with id 42.</summary>
  public const string RoomLinkPrefix = "room:";

  private static readonly Regex _heading = new(@"^(#{1,6})\s+(.*)$");
  private static readonly Regex _bullet = new(@"^(\s*)[-*+]\s+(.*)$");
  private static readonly Regex _numbered = new(@"^(\s*)(\d+)[.)]\s+(.*)$");
  private static readonly Regex _rule = new(@"^\s*([-*_])(\s*\1){2,}\s*$");
  private static readonly Regex _tableSeparator = new(@"^\s*\|?\s*:?-{1,}:?\s*(\|\s*:?-{1,}:?\s*)*\|?\s*$");

  /// <summary>The blocks of <paramref name="text" />.</summary>
  public static IReadOnlyList<MarkdownBlock> Parse(string text)
  {
    var blocks = new List<MarkdownBlock>();
    if (string.IsNullOrEmpty(text)) return blocks;
    var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
    for (var i = 0; i < lines.Length; i++)
    {
      var line = lines[i];
      if (string.IsNullOrWhiteSpace(line))
      {
        blocks.Add(new MarkdownBlock(MarkdownBlockKind.Blank, Array.Empty<MarkdownSpan>()));
        continue;
      }

      if (IsTableRow(line))
      {
        var rows = new List<IReadOnlyList<IReadOnlyList<MarkdownSpan>>>();
        var hasHeader = false;
        while (i < lines.Length && IsTableRow(lines[i]))
        {
          if (_tableSeparator.IsMatch(lines[i]) && lines[i].Contains('-'))
          {
            // the line under the header
            if (rows.Count == 1) hasHeader = true;
          }
          else
            rows.Add(Cells(lines[i]).Select(Inline).ToList());

          i++;
        }

        i--;
        blocks.Add(new MarkdownBlock(MarkdownBlockKind.Table, Array.Empty<MarkdownSpan>(), Rows: rows,
          HasHeader: hasHeader));
        continue;
      }

      if (_rule.IsMatch(line))
      {
        blocks.Add(new MarkdownBlock(MarkdownBlockKind.Rule, Array.Empty<MarkdownSpan>()));
        continue;
      }

      var match = _heading.Match(line);
      if (match.Success)
      {
        blocks.Add(new MarkdownBlock(MarkdownBlockKind.Heading, Inline(match.Groups[2].Value.Trim()),
          Math.Min(3, match.Groups[1].Value.Length)));
        continue;
      }

      match = _numbered.Match(line);
      if (match.Success)
      {
        blocks.Add(new MarkdownBlock(MarkdownBlockKind.Numbered, Inline(match.Groups[3].Value),
          Indent(match.Groups[1].Value), int.Parse(match.Groups[2].Value)));
        continue;
      }

      match = _bullet.Match(line);
      if (match.Success)
      {
        blocks.Add(new MarkdownBlock(MarkdownBlockKind.Bullet, Inline(match.Groups[2].Value),
          Indent(match.Groups[1].Value)));
        continue;
      }

      blocks.Add(new MarkdownBlock(MarkdownBlockKind.Paragraph, Inline(line.Trim())));
    }

    return blocks;
  }

  private static int Indent(string leading) => leading.Replace("\t", "  ").Length / 2;

  private static bool IsTableRow(string line)
  {
    var trimmed = line.Trim();
    return trimmed.Length > 1 && trimmed[0] == '|';
  }

  /// <summary>The cells of a table row, "| a | b |" (a | inside a room link, [[Maze|maze]], isn't a cell's end).</summary>
  private static List<string> Cells(string line)
  {
    var trimmed = line.Trim();
    if (trimmed.StartsWith("|")) trimmed = trimmed.Substring(1);
    if (trimmed.EndsWith("|") && !trimmed.EndsWith("\\|")) trimmed = trimmed.Substring(0, trimmed.Length - 1);
    var cells = new List<string>();
    var cell = new StringBuilder();
    var inRoomLink = 0;
    for (var i = 0; i < trimmed.Length; i++)
    {
      var c = trimmed[i];
      if (c == '\\' && i + 1 < trimmed.Length && trimmed[i + 1] == '|')
      {
        cell.Append('|');
        i++;
        continue;
      }

      if (c == '[' && i + 1 < trimmed.Length && trimmed[i + 1] == '[') inRoomLink++;
      if (c == ']' && i + 1 < trimmed.Length && trimmed[i + 1] == ']' && inRoomLink > 0) inRoomLink--;
      if (c == '|' && inRoomLink == 0)
      {
        cells.Add(cell.ToString().Trim());
        cell.Clear();
        continue;
      }

      cell.Append(c);
    }

    cells.Add(cell.ToString().Trim());
    return cells;
  }

  /// <summary>The runs of a line: bold, italic, code and links, the Markdown marks taken out.</summary>
  public static IReadOnlyList<MarkdownSpan> Inline(string text)
  {
    var spans = new List<MarkdownSpan>();
    if (string.IsNullOrEmpty(text)) return spans;
    var plain = new StringBuilder();
    var bold = false;
    var italic = false;

    void Flush()
    {
      if (plain.Length == 0) return;
      spans.Add(new MarkdownSpan(plain.ToString(), bold, italic));
      plain.Clear();
    }

    for (var i = 0; i < text.Length; i++)
    {
      var c = text[i];
      if (c == '\\' && i + 1 < text.Length && "\\`*_[]#|-".IndexOf(text[i + 1]) >= 0)
      {
        plain.Append(text[++i]);
        continue;
      }

      // [[Room Name]], [[Room Name|text]], [[Maze#42]]
      if (c == '[' && At(text, i, "[["))
      {
        var end = text.IndexOf("]]", i + 2, StringComparison.Ordinal);
        if (end > i + 2)
        {
          Flush();
          var inside = text.Substring(i + 2, end - i - 2);
          var bar = inside.IndexOf('|');
          var target = (bar < 0 ? inside : inside.Substring(0, bar)).Trim();
          var shown = bar < 0 ? WithoutId(target) : inside.Substring(bar + 1).Trim();
          spans.Add(new MarkdownSpan(shown, bold, italic, Link: RoomLinkPrefix + target));
          i = end + 1;
          continue;
        }
      }

      // [text](target)
      if (c == '[')
      {
        var close = text.IndexOf("](", i + 1, StringComparison.Ordinal);
        var end = close < 0 ? -1 : text.IndexOf(')', close + 2);
        if (close > i && end > close)
        {
          Flush();
          spans.Add(new MarkdownSpan(text.Substring(i + 1, close - i - 1), bold, italic,
            Link: text.Substring(close + 2, end - close - 2).Trim()));
          i = end;
          continue;
        }
      }

      if (c == '`')
      {
        var end = text.IndexOf('`', i + 1);
        if (end > i)
        {
          Flush();
          spans.Add(new MarkdownSpan(text.Substring(i + 1, end - i - 1), bold, italic, true));
          i = end;
          continue;
        }
      }

      if ((c == '*' || c == '_') && i + 1 < text.Length && text[i + 1] == c && (bold || Closes(text, i + 2, c, 2)))
      {
        Flush();
        bold = !bold;
        i++;
        continue;
      }

      // _ inside a word (snake_case) is just an underscore
      if ((c == '*' || c == '_') && (italic || Closes(text, i + 1, c, 1)) &&
          !(c == '_' && i > 0 && char.IsLetterOrDigit(text[i - 1]) && !italic))
      {
        Flush();
        italic = !italic;
        continue;
      }

      plain.Append(c);
    }

    Flush();
    return spans;
  }

  private static bool At(string text, int index, string what) =>
    string.CompareOrdinal(text, index, what, 0, what.Length) == 0;

  /// <summary>Whether a run of <paramref name="count" /> <paramref name="mark" />s closes what opens here.</summary>
  private static bool Closes(string text, int from, char mark, int count)
  {
    if (from >= text.Length || char.IsWhiteSpace(text[from])) return false;
    var closing = new string(mark, count);
    var at = text.IndexOf(closing, from, StringComparison.Ordinal);
    return at > from;
  }

  private static string WithoutId(string target)
  {
    var hash = target.LastIndexOf('#');
    return hash > 0 && int.TryParse(target.Substring(hash + 1), out _) ? target.Substring(0, hash) : target;
  }

  /// <summary>The text of <paramref name="label" /> without its Markdown, as a plain label would show it.</summary>
  public static string PlainText(string label) =>
    string.Join("\n", Parse(label).Select(block => block.Kind == MarkdownBlockKind.Table
      ? string.Join("\n", block.Rows.Select(row => string.Join("  ", row.Select(cell => string.Concat(cell.Select(s => s.Text))))))
      : string.Concat(block.Spans.Select(span => span.Text))));

  /// <summary>
  ///   The room a room link goes to ("room:Hall", "room:Maze#42"): by id when it has one, else the first room with the
  ///   name. Null for another kind of link, or a room that isn't on the map.
  /// </summary>
  public static Room LinkedRoom(Project project, string link)
  {
    if (project == null || string.IsNullOrEmpty(link) ||
        !link.StartsWith(RoomLinkPrefix, StringComparison.OrdinalIgnoreCase))
      return null;
    var target = link.Substring(RoomLinkPrefix.Length).Trim();
    var rooms = project.Elements.OfType<Room>().ToList();
    var hash = target.LastIndexOf('#');
    if (hash >= 0 && int.TryParse(target.Substring(hash + 1), out var id))
    {
      var byId = rooms.FirstOrDefault(room => room.Id == id);
      if (byId != null) return byId;
      target = target.Substring(0, hash);
    }

    return rooms.FirstOrDefault(room => string.Equals(room.Name, target, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  ///   A link to <paramref name="room" /> to write in a label: [[Giant Room]], or, when other rooms share its name,
  ///   [[Maze#42]] (its id), so it goes to this one.
  /// </summary>
  public static string RoomLink(Project project, Room room)
  {
    if (room == null) return string.Empty;
    var shared = project != null && project.Elements.OfType<Room>().Count(other =>
      string.Equals(other.Name, room.Name, StringComparison.OrdinalIgnoreCase)) > 1;
    return shared ? $"[[{room.Name}#{room.Id}]]" : $"[[{room.Name}]]";
  }

  /// <summary>Whether a link opens a web page (http, https or mailto).</summary>
  public static bool IsWebLink(string link) =>
    !string.IsNullOrEmpty(link) &&
    (link.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
     link.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
     link.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase));
}
