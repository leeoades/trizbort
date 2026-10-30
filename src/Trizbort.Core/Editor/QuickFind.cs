using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;

namespace Trizbort.Editor;

/// <summary>Quick Find (tickets 044, 086): searches rooms by name, description, objects and subtitle.</summary>
public static class QuickFind
{
  /// <summary>
  ///   The rooms whose name, primary description, objects or subtitle contain <paramref name="text" /> (case
  ///   insensitive), each once, in map order. Empty or whitespace text finds nothing.
  /// </summary>
  public static IReadOnlyList<Room> Search(Project project, string text)
  {
    var result = new List<Room>();
    if (project == null || string.IsNullOrWhiteSpace(text)) return result;

    foreach (var room in project.Elements.OfType<Room>())
    {
      if (Contains(room.Name, text) || Contains(room.PrimaryDescription, text) || Contains(room.Objects, text) ||
          Contains(room.SubTitle, text))
      {
        result.Add(room);
      }
    }
    return result;
  }

  /// <summary>The distinct non-empty room texts (name, description, objects, subtitle), sorted, for auto-completion.</summary>
  public static IEnumerable<string> Suggestions(Project project)
  {
    var texts = new List<string>();
    if (project == null) return texts;

    foreach (var room in project.Elements.OfType<Room>())
    {
      Add(texts, room.Name);
      Add(texts, room.PrimaryDescription);
      Add(texts, room.Objects);
      Add(texts, room.SubTitle);
    }
    return texts.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();
  }

  /// <summary>Asks for the text through the session's prompts, then selects and centres on the matches.</summary>
  public static async Task RunAsync(EditorSession session)
  {
    var suggestions = Suggestions(session.Project).ToList();
    var text = await session.Prompts.QuickFindAsync(suggestions);
    if (string.IsNullOrWhiteSpace(text)) return;

    session.FindRooms(text);
  }

  private static bool Contains(string haystack, string needle)
  {
    return haystack != null && haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
  }

  private static void Add(List<string> texts, string text)
  {
    if (string.IsNullOrWhiteSpace(text)) return;
    texts.Add(text.Trim());
  }
}
