using System.Collections.Generic;

namespace Trizbort.Editor.Commands;

public static class CatalogueValidator
{
  /// <summary>
  ///   Returns a description of each problem found. A gesture may be used by only one command, whatever the scope:
  ///   two commands in the same scope would collide, and a Window gesture would shadow a Canvas one.
  /// </summary>
  public static IReadOnlyList<string> Validate(CommandCatalogue catalogue)
  {
    var problems = new List<string>();
    var owners = new Dictionary<KeyGesture, EditorCommand>();

    foreach (var command in catalogue.All)
    {
      if (string.IsNullOrWhiteSpace(command.Title))
        problems.Add($"Command '{command.Id}' has no title.");
      if (string.IsNullOrWhiteSpace(command.MenuPath) && !command.Hidden)
        problems.Add($"Command '{command.Id}' has no menu path and is not hidden.");
      if (command.Hidden && command.ShowInToolbar)
        problems.Add($"Command '{command.Id}' is hidden but shown in the toolbar.");

      foreach (var gesture in command.Gestures)
      {
        if (owners.TryGetValue(gesture, out var other))
        {
          if (other.Id == command.Id)
            problems.Add($"Command '{command.Id}' lists gesture {gesture} twice.");
          else if (other.Scope == command.Scope)
            problems.Add($"Gesture {gesture} is used by both '{other.Id}' and '{command.Id}' in scope {command.Scope}.");
          else
            problems.Add($"Gesture {gesture} is used by '{other.Id}' ({other.Scope}) and '{command.Id}' ({command.Scope}); a Window binding would shadow the Canvas one.");
        }
        else
        {
          owners[gesture] = command;
        }
      }
    }

    return problems;
  }
}
