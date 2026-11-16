using System;
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
using Parser = Trizbort.Automap.Automap;

namespace Trizbort.Core.Tests;

/// <summary>
///   Cancelling "that way led somewhere else before" for a new room: nothing is mapped, but automap knows the player
///   is in that room, which isn't on the map, and Add Current Room puts it there (Lee, 15 Nov 2026).
/// </summary>
[TestFixture]
[Category("Integration")]
public class AutomapNotOnTheMapTests : IsolatedProjectTests
{
  private const string WaitingStatus = "Automapping is waiting for more text.";

  private sealed class CancelPrompts : IEditorPrompts
  {
    public Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(
      Room existing, string newRoomName, CancellationToken token = default) =>
      Task.FromResult(AutomapSameDirectionResult.Cancel);

    public Task<RoomDisambiguation> DisambiguateRoomAsync(
      string transcriptLine, string roomDescription, IReadOnlyList<Room> candidates,
      CancellationToken token = default) =>
      throw new AssertionException("Unexpected disambiguation");
  }

  // polls until it's so (no fixed waits, so a slow machine only takes longer)
  private static async Task WaitUntil(Func<bool> condition, Task run, CancellationToken token)
  {
    while (!condition())
    {
      token.ThrowIfCancellationRequested();
      run.IsCompleted.ShouldBeFalse();
      await Task.Delay(10, token);
    }
  }

  private static List<string> Rooms() =>
    Project.Current.Elements.OfType<Room>().Select(room => room.Name).OrderBy(name => name).ToList();

  private static List<string> Joins() =>
    Project.Current.Elements.OfType<Connection>()
      .Where(connection => connection.GetSourceRoom() != connection.GetTargetRoom())
      .Select(connection => connection.GetSourceRoom().Name + " - " + connection.GetTargetRoom().Name)
      .OrderBy(join => join).ToList();

  [Test]
  public async Task CancellingANewRoom_LeavesItOffTheMap_AndAddCurrentRoomPutsItThere()
  {
    using var session = new EditorSession();
    var canvas = new AutomapCanvas(null, null, session);
    var parser = new Parser((message, title) => throw new AssertionException(title + ": " + message),
      new CancelPrompts());
    var settings = AutomapSettings.Default;
    settings.FileName = Files.File("transcript.txt");
    settings.GuessExits = false;
    settings.AutoTidy = false;
    File.WriteAllText(settings.FileName,
      "Example Game\n\nRoom A\nA plain room called A.\n\n>e\n\nRoom B\nA plain room called B.\n\n" +
      ">w\n\nRoom A\nA plain room called A.\n\n>e\n\nRoom C\nA plain room called C.\n\n>");
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    var run = parser.Start(canvas, settings);
    try
    {
      await WaitUntil(() => parser.RoomNotOnMap == "Room C" && parser.Status == WaitingStatus, run, deadline.Token);
      Rooms().ShouldBe(new[] { "Room A", "Room B" });
      parser.CurrentRoom.ShouldBeNull();
      parser.CanAddCurrentRoom.ShouldBeTrue();

      (await parser.AddCurrentRoomAsync()).ShouldBeTrue();
      Rooms().ShouldBe(new[] { "Room A", "Room B", "Room C" });
      parser.CurrentRoom.Name.ShouldBe("Room C");
      parser.RoomNotOnMap.ShouldBeNull();
      parser.CanAddCurrentRoom.ShouldBeFalse();
      session.Undo.UndoTitle.ShouldBe("Automap: add Room C");
      // not joined: the question about the way was cancelled
      Joins().ShouldBe(new[] { "Room A - Room B" });

      // and automap maps on from there
      File.AppendAllText(settings.FileName, "s\n\nRoom D\nA plain room called D.\n\n>");
      await WaitUntil(() => Rooms().Count == 4 && parser.Status == WaitingStatus, run, deadline.Token);
      Joins().ShouldBe(new[] { "Room A - Room B", "Room C - Room D" });
      parser.CurrentRoom.Name.ShouldBe("Room D");
    }
    finally
    {
      parser.Stop();
      parser.RunToCompletion();
      await run.WaitAsync(TimeSpan.FromSeconds(5));
    }
  }
}
