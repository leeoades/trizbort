using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trizbort.Automap;
using Trizbort.Domain.Elements;

namespace Trizbort.Editor;

/// <summary>
///   Questions the editor core may need to ask the user. UI hosts provide an interactive implementation;
///   headless callers use <see cref="NonInteractiveEditorPrompts" />.
/// </summary>
public interface IEditorPrompts
{
  Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(
    Room existing,
    string newRoomName,
    CancellationToken token = default);

  Task<RoomDisambiguation> DisambiguateRoomAsync(
    string transcriptLine,
    string roomDescription,
    IReadOnlyList<Room> candidates,
    CancellationToken token = default);

  /// <summary>
  ///   Shows an informational or error message and completes when the user dismisses it. Default implementation
  ///   shows nothing so existing implementers keep compiling.
  /// </summary>
  Task ShowMessageAsync(string title, string message, CancellationToken token = default)
  {
    return Task.CompletedTask;
  }

  /// <summary>
  ///   Asks a yes/no question. Default implementation answers "no" so existing implementers keep compiling.
  /// </summary>
  Task<bool> ConfirmAsync(string message, string title, CancellationToken token = default)
  {
    return Task.FromResult(false);
  }

  /// <summary>
  ///   Opens the properties dialog for a freshly added element. Returns true when the user accepted. Default
  ///   implementation does nothing so existing implementers keep compiling.
  /// </summary>
  Task<bool> EditElementAsync(Element element, CancellationToken token = default)
  {
    return Task.FromResult(false);
  }

  /// <summary>
  ///   Asks the user to pick a region. Returns the chosen region name, or null when cancelled. Default
  ///   implementation cancels so existing implementers keep compiling.
  /// </summary>
  Task<string> ChooseRegionAsync(IReadOnlyList<string> regions, string current, CancellationToken token = default)
  {
    return Task.FromResult<string>(null);
  }

  /// <summary>
  ///   Quick Find: asks for the text to search for, offering <paramref name="suggestions" /> for completion. Returns
  ///   the entered text, or null when cancelled. Default implementation cancels.
  /// </summary>
  Task<string> QuickFindAsync(IReadOnlyList<string> suggestions, CancellationToken token = default)
  {
    return Task.FromResult<string>(null);
  }

  /// <summary>
  ///   Asks for a web address (Open Map from Web). The dialog validates that it is an http or https URL. Returns the
  ///   entered text, or null when cancelled. Default implementation cancels.
  /// </summary>
  Task<string> PromptUrlAsync(string title, string label, CancellationToken token = default)
  {
    return Task.FromResult<string>(null);
  }

  /// <summary>Shows a read-only text report in a window. Default implementation does nothing.</summary>
  Task ShowTextReportAsync(string title, string text, CancellationToken token = default)
  {
    return Task.CompletedTask;
  }

  /// <summary>
  ///   Asks where to save a file. <paramref name="suggestedPath" /> is a full path whose folder and file name are
  ///   offered. Returns the chosen path, or null when cancelled. Default implementation cancels.
  /// </summary>
  Task<string> PickSaveFileAsync(string title, string suggestedPath, string extension, CancellationToken token = default)
  {
    return Task.FromResult<string>(null);
  }

  /// <summary>Shows an informational message. Default implementation does nothing.</summary>
  Task MessageAsync(string message, string title, CancellationToken token = default)
  {
    return Task.CompletedTask;
  }

  /// <summary>
  ///   Opens a link in the default browser. Returns false when it could not be opened. Default implementation
  ///   returns false.
  /// </summary>
  Task<bool> OpenUriAsync(string uri, CancellationToken token = default)
  {
    return Task.FromResult(false);
  }
}

public readonly record struct RoomDisambiguation(Room Chosen, bool NewRoom, bool AutoChooseRest)
{
  public static RoomDisambiguation Choose(Room r) => new(r, false, false);
  public static RoomDisambiguation CreateNew() => new(null, true, false);
  public static RoomDisambiguation ChooseFirstAndStopAsking(Room first) => new(first, false, true);
}
