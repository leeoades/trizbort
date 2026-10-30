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
}

public readonly record struct RoomDisambiguation(Room Chosen, bool NewRoom, bool AutoChooseRest)
{
  public static RoomDisambiguation Choose(Room r) => new(r, false, false);
  public static RoomDisambiguation CreateNew() => new(null, true, false);
  public static RoomDisambiguation ChooseFirstAndStopAsking(Room first) => new(first, false, true);
}
