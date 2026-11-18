using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Editor;

namespace Trizbort.Core.Tests.Automap;

/// <summary>
///   Lee, 18 Nov 2026: "enter closet" from the Foyer had led to the Top of Closet; typed again, it led to the Closet,
///   and automap made a new room without asking. A command typed here before asks, as a direction does.
/// </summary>
[TestFixture]
[Category("Unit")]
public class CommandSameWayTests : IsolatedProjectTests
{
  private static readonly string[] _game =
  {
    ">look", "Foyer", "An elegant foyer.", "",
    ">enter closet", "Top of Closet", "A shaft on top of the closet.", "",
    ">out", "Foyer", "An elegant foyer.", "",
    ">pull peg", "The closet moves.", "",
    ">enter closet", "Closet", "A small closet.", "",
    ">"
  };

  private sealed class Asked : IEditorPrompts
  {
    public AutomapSameDirectionResult Answer { get; init; } = AutomapSameDirectionResult.SamePlace;
    public List<(string From, string Way, string Before, string Now)> Questions { get; } = new();

    public Task<AutomapSameDirectionResult> ChooseSameWayAsync(Room from, string way, Room existing,
      string newRoomName, CancellationToken token = default)
    {
      Questions.Add((from?.Name, way, existing?.Name, newRoomName));
      return Task.FromResult(Answer);
    }

    public Task<RoomDisambiguation> DisambiguateRoomAsync(string transcriptLine, string roomDescription,
      IReadOnlyList<Room> candidates, CancellationToken token = default) =>
      NonInteractiveEditorPrompts.Instance.DisambiguateRoomAsync(transcriptLine, roomDescription, candidates, token);

    public Task<bool> ConfirmAsync(string message, string title, CancellationToken token = default) =>
      NonInteractiveEditorPrompts.Instance.ConfirmAsync(message, title, token);
  }

  private async Task Play(IEditorPrompts prompts)
  {
    var path = Files.File("game.txt");
    File.WriteAllLines(path, _game);
    var settings = AutomapSettings.Default;
    settings.GuessExits = false;
    settings.AutoTidy = false;
    await HeadlessAutomap.RunAsync(Project.Current, path, settings, prompts);
  }

  private static Room Only(string name) =>
    Project.Current.Elements.OfType<Room>().Single(room => room.Name == name);

  private static IEnumerable<string> JoinedTo(Room room) =>
    room.GetConnections().Select(join => join.GetSourceRoom() == room ? join.GetTargetRoom() : join.GetSourceRoom())
      .Where(other => other != null && other != room).Select(other => other.Name).OrderBy(name => name);

  [Test]
  public async Task ACommandThatLedSomewhereElseBefore_Asks()
  {
    var asked = new Asked();

    await Play(asked);

    asked.Questions.ShouldHaveSingleItem().ShouldBe(("Foyer", "enter closet", "Top of Closet", "Closet"));
    // both right: both joined from the foyer, the Closet beside the Top of Closet
    JoinedTo(Only("Foyer")).ShouldBe(new[] { "Closet", "Top of Closet" });
    var closet = Only("Closet");
    var top = Only("Top of Closet");
    closet.InnerBounds.IntersectsWith(top.InnerBounds).ShouldBeFalse();
  }

  [Test]
  public async Task TheRoomMoves_TheCommandLeadsFromAnotherStop()
  {
    var asked = new Asked { Answer = AutomapSameDirectionResult.Moves };

    await Play(asked);

    asked.Questions.Count.ShouldBe(1);
    JoinedTo(Only("Closet")).ShouldNotContain("Top of Closet");
    Project.Current.Elements.OfType<Room>().Count(room => room.Name == "Foyer").ShouldBe(2);
  }
}
