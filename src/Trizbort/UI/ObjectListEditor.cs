using System;
using System.Collections.Generic;
using System.Text;
using Trizbort.Domain.Misc;

namespace Trizbort.UI {
  /// <summary>
  ///   Text editing helpers for the room object list, which uses "-" bullets to show containment
  ///   (see <see cref="ObjectList" />). Kept free of WinForms so it can be unit tested.
  /// </summary>
  public static class ObjectListEditor {
    public struct EditResult {
      public string Text;
      public int SelectionStart;
      public int SelectionLength;
    }

    /// <summary>
    ///   Indent (or outdent) every line touched by the selection by one level.
    /// </summary>
    public static EditResult ChangeIndent(string text, int selectionStart, int selectionLength, bool outdent) {
      text = text ?? string.Empty;
      var lines = getLines(text);
      var first = lineIndexAt(lines, selectionStart);
      var selectionEnd = selectionStart + selectionLength;
      var last = lineIndexAt(lines, selectionLength > 0 ? selectionEnd - 1 : selectionEnd);

      var builder = new StringBuilder(text);
      var newStart = selectionStart;
      var newEnd = selectionEnd;
      var offset = 0;
      for (var index = first; index <= last; ++index) {
        var lineStart = lines[index].Start + offset;
        var lineText = lines[index].Text;
        var change = outdent ? outdentLine(builder, lineStart, lineText) : indentLine(builder, lineStart, lineText);
        if (change.Length == 0) continue;

        offset += change.Length;
        newStart = adjust(newStart, change, selectionLength > 0);
        newEnd = adjust(newEnd, change, false);
      }

      if (selectionLength == 0) newEnd = newStart;
      return new EditResult {Text = builder.ToString(), SelectionStart = newStart, SelectionLength = Math.Max(0, newEnd - newStart)};
    }

    private static int adjust(int position, Change change, bool keepAtInsertionPoint) {
      if (change.Length > 0)
        return position > change.Position || (position == change.Position && !keepAtInsertionPoint) ? position + change.Length : position;
      return position <= change.Position ? position : Math.Max(change.Position, position + change.Length);
    }

    /// <summary>
    ///   Handle Enter: start a new line at the same nesting level as the current one. Pressing Enter
    ///   on a line holding only a bullet outdents it instead. Returns null when there's no indentation
    ///   to continue, so the default behaviour applies.
    /// </summary>
    public static EditResult? NewLine(string text, int selectionStart, int selectionLength) {
      text = text ?? string.Empty;
      var lines = getLines(text);
      var line = lines[lineIndexAt(lines, selectionStart)];
      var prefixLength = ObjectList.LeadingIndentLength(line.Text);
      if (prefixLength == 0) return null;

      if (selectionLength == 0 && line.Text.Substring(prefixLength).Trim().Length == 0 && selectionStart >= line.Start + prefixLength)
        return ChangeIndent(text, selectionStart, 0, true);

      // don't continue indentation when the caret is within the line's indentation
      if (selectionStart < line.Start + prefixLength) return null;

      var insert = "\r\n" + line.Text.Substring(0, prefixLength);
      var newText = text.Substring(0, selectionStart) + insert + text.Substring(selectionStart + selectionLength);
      return new EditResult {Text = newText, SelectionStart = selectionStart + insert.Length, SelectionLength = 0};
    }

    private struct Change {
      public int Position;
      public int Length;
    }

    private static Change indentLine(StringBuilder builder, int lineStart, string lineText) {
      var whitespace = 0;
      while (whitespace < lineText.Length && (lineText[whitespace] == ' ' || lineText[whitespace] == '\t')) ++whitespace;

      if (ObjectList.LeadingIndentLength(lineText) > whitespace && lineText[whitespace] != ' ' && lineText[whitespace] != '\t') {
        // already bulleted: add another bullet to the run, e.g. "- key" -> "-- key"
        builder.Insert(lineStart + whitespace, '-');
        return new Change {Position = lineStart + whitespace, Length = 1};
      }

      builder.Insert(lineStart + whitespace, "- ");
      return new Change {Position = lineStart + whitespace, Length = 2};
    }

    private static Change outdentLine(StringBuilder builder, int lineStart, string lineText) {
      var whitespace = 0;
      while (whitespace < lineText.Length && (lineText[whitespace] == ' ' || lineText[whitespace] == '\t')) ++whitespace;

      var prefix = ObjectList.LeadingIndentLength(lineText);
      if (prefix > whitespace) {
        // remove one bullet; if it was the last, remove the space following it too
        var runEnd = whitespace;
        while (runEnd < prefix && lineText[runEnd] != ' ' && lineText[runEnd] != '\t') ++runEnd;
        var remove = 1;
        if (runEnd - whitespace == 1 && runEnd < lineText.Length && (lineText[runEnd] == ' ' || lineText[runEnd] == '\t')) remove = 2;
        builder.Remove(lineStart + whitespace, remove);
        return new Change {Position = lineStart + whitespace, Length = -remove};
      }

      if (whitespace == 0) return new Change();
      if (lineText[0] == '\t') {
        builder.Remove(lineStart, 1);
        return new Change {Position = lineStart, Length = -1};
      }

      var spaces = 0;
      while (spaces < Math.Min(ObjectList.TAB_INDENT, whitespace) && lineText[spaces] == ' ') ++spaces;
      builder.Remove(lineStart, spaces);
      return new Change {Position = lineStart, Length = -spaces};
    }

    private struct Line {
      public int Start;
      public string Text;
    }

    private static List<Line> getLines(string text) {
      var lines = new List<Line>();
      var start = 0;
      while (true) {
        var end = text.IndexOf('\n', start);
        if (end < 0) {
          lines.Add(new Line {Start = start, Text = text.Substring(start)});
          return lines;
        }

        var length = end - start;
        if (length > 0 && text[end - 1] == '\r') --length;
        lines.Add(new Line {Start = start, Text = text.Substring(start, length)});
        start = end + 1;
      }
    }

    private static int lineIndexAt(List<Line> lines, int position) {
      for (var index = lines.Count - 1; index >= 0; --index)
        if (position >= lines[index].Start)
          return index;
      return 0;
    }
  }
}
