using System.Linq;
using Trizbort.Domain.Elements;

namespace Trizbort.Editor;

// Quick Find (tickets 044, 086). Selection and view only, so there is no undo step.
public sealed partial class EditorSession
{
  /// <summary>
  ///   Selects every room matching <paramref name="text" /> and centres the view on the first. With no match the
  ///   selection is cleared and the status message says so. Empty text does nothing. Returns the number of matches.
  /// </summary>
  public int FindRooms(string text)
  {
    if (string.IsNullOrWhiteSpace(text)) return 0;

    var matches = QuickFind.Search(Project, text);
    if (matches.Count == 0)
    {
      Selection.Clear();
      PostStatusMessage($"No rooms match '{text}'");
      return 0;
    }

    Selection.Set(matches.Cast<Element>().ToList());
    Project.ActiveSelectedElement = matches[0];
    CenterOnElement(matches[0]);
    return matches.Count;
  }
}
