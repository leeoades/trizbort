using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trizbort.Automap;
using Trizbort.Domain.Elements;

namespace Trizbort.Editor;

/// <summary>
///   Never asks anything; always picks the legacy default answer.
/// </summary>
public sealed class NonInteractiveEditorPrompts : IEditorPrompts
{
  public static readonly NonInteractiveEditorPrompts Instance = new();

  private NonInteractiveEditorPrompts()
  {
  }

  public Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(
    Room existing,
    string newRoomName,
    CancellationToken token = default)
  {
    return Task.FromResult(AutomapSameDirectionResult.KeepRoom1);
  }

  public Task<RoomDisambiguation> DisambiguateRoomAsync(
    string transcriptLine,
    string roomDescription,
    IReadOnlyList<Room> candidates,
    CancellationToken token = default)
  {
    return Task.FromResult(RoomDisambiguation.Choose(candidates[0]));
  }
}
