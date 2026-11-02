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

/// <summary>Ticket 136: automap records one undo step per move and the user can undo while it runs (GitHub #24).</summary>
[TestFixture]
[Category("Integration")]
public class AutomapUndoTests : IsolatedProjectTests
{
  private const string WaitingStatus = "Automapping is waiting for more text.";

  private static Parser CreateParser()
  {
    return new Parser(
      (message, title) => throw new AssertionException(title + ": " + message),
      new DelegatePrompts());
  }

  private sealed class DelegatePrompts : IEditorPrompts
  {
    public Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(
      Room existing,
      string newRoomName,
      CancellationToken token = default)
    {
      throw new AssertionException("Unexpected same-direction prompt");
    }

    public Task<RoomDisambiguation> DisambiguateRoomAsync(
      string transcriptLine,
      string roomDescription,
      IReadOnlyList<Room> candidates,
      CancellationToken token = default)
    {
      throw new AssertionException("Unexpected disambiguation");
    }
  }

  private AutomapSettings SettingsFor(string text)
  {
    var settings = AutomapSettings.Default;
    settings.FileName = Files.File("transcript.txt");
    File.WriteAllText(settings.FileName, text);
    settings.AssumeRoomsWithSameNameAreSameRoom = true;
    settings.AssumeTwoWayConnections = true;
    settings.GuessExits = false;
    return settings;
  }

  private static async Task WaitUntil(Func<bool> condition, Task run, CancellationToken token)
  {
    while (!condition())
    {
      token.ThrowIfCancellationRequested();
      run.IsCompleted.ShouldBeFalse();
      await Task.Delay(10, token);
    }
  }

  private static List<Room> Rooms()
  {
    return Project.Current.Elements.OfType<Room>().ToList();
  }

  [Test]
  public async Task Run_RecordsOneUndoStepPerMove()
  {
    using var session = new EditorSession();
    var canvas = new AutomapCanvas(null, null, session);
    var settings = SettingsFor(
      "Example Game\n\nFirst Room\nA small room.\n\n>e\n\nSecond Room\nAnother room.\n\n>e\n\nThird Room\nA third room.\n");

    await CreateParser().StartCl(canvas, settings);

    Rooms().Count.ShouldBe(3);
    var titles = new List<string>();
    while (session.Undo.CanUndo)
    {
      titles.Add(session.Undo.UndoTitle);
      session.PerformUndo();
    }

    titles.ShouldBe(new[] { "Automap: Third Room", "Automap: Second Room", "Automap: First Room" });
    Rooms().Count.ShouldBe(0);
  }

  [Test]
  public async Task UndoingAMove_BringsBackTheExitStubItUsed()
  {
    // PR #26 review: the stub went in the command's step and the new room in the response's, so one Undo left the
    // stub missing
    using var session = new EditorSession();
    var canvas = new AutomapCanvas(null, null, session);
    var settings = SettingsFor(
      "Example Game\n\nFirst Room\nA small room. A door leads east.\n\n>e\n\nSecond Room\nAnother room.\n");
    settings.GuessExits = true;

    await CreateParser().StartCl(canvas, settings);

    Rooms().Count.ShouldBe(2);
    session.Undo.UndoTitle.ShouldBe("Automap: Second Room");
    session.PerformUndo();

    Rooms().Select(room => room.Name).ShouldBe(new[] { "First Room" });
    var stub = Project.Current.Elements.OfType<Connection>().ShouldHaveSingleItem();
    stub.GetSourceRoom().ShouldBeSameAs(stub.GetTargetRoom());
  }

  [Test]
  public async Task ABlockedMove_RemovesTheStubAsAStepOfItsOwn()
  {
    using var session = new EditorSession();
    var canvas = new AutomapCanvas(null, null, session);
    var settings = SettingsFor(
      "Example Game\n\nFirst Room\nA small room. A door leads east.\n\n>e\nThe door is locked.\n\n>wait\nTime passes.\n");
    settings.GuessExits = true;

    await CreateParser().StartCl(canvas, settings);

    // the blocked move removed the stub, as a step of its own that undo brings back
    Stubs().ShouldBe(0);
    session.Undo.UndoTitle.ShouldBe("Automap: First Room");
    session.PerformUndo();
    Stubs().ShouldBe(1);
    Rooms().Count.ShouldBe(1);
  }

  private static int Stubs() => Project.Current.Elements.OfType<Connection>().Count();

  [Test]
  public async Task AMoveDuringADrag_WaitsForTheDrag_AndCancellingTheDragKeepsIt()
  {
    // PR #26 review: automap's step joined the user's drag, and Escape threw it away
    using var session = new EditorSession();
    var canvas = new AutomapCanvas(null, null, session);
    var parser = CreateParser();
    var settings = SettingsFor("Example Game\n\nFirst Room\nA small room.\n\n>east\n");
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    var run = parser.Start(canvas, settings);
    try
    {
      await WaitUntil(() => parser.Status == WaitingStatus, run, deadline.Token);

      var drag = session.BeginTransaction("Move");
      File.AppendAllText(settings.FileName, "\nSecond Room\nAnother room.\n\n>");
      await Task.Delay(400, deadline.Token);
      Rooms().Count.ShouldBe(1); // waiting for the drag

      drag.Cancel();
      await WaitUntil(() => Rooms().Count == 2, run, deadline.Token);
      await Task.Delay(150, deadline.Token);

      session.Undo.UndoTitle.ShouldBe("Automap: Second Room");
      Rooms().Select(room => room.Name).ShouldBe(new[] { "First Room", "Second Room" }, ignoreOrder: true);
    }
    finally
    {
      parser.Stop();
      parser.RunToCompletion();
      await run.WaitAsync(TimeSpan.FromSeconds(5));
    }
  }

  [Test]
  public async Task UndoWhileRunning_DoesNotThrow_AndTheNextMoveDoesNotUseTheDeletedRoom()
  {
    using var session = new EditorSession();
    var canvas = new AutomapCanvas(null, null, session);
    var parser = CreateParser();
    var settings = SettingsFor("Example Game\n\nFirst Room\nA small room.\n\n>east\n");
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    var run = parser.Start(canvas, settings);
    try
    {
      await WaitUntil(() => parser.Status == WaitingStatus, run, deadline.Token);

      File.AppendAllText(settings.FileName, "\nSecond Room\nAnother room.\n\n>");
      await WaitUntil(() => Rooms().Count == 2, run, deadline.Token);
      // let the engine settle back into waiting for the command before the user acts
      await Task.Delay(150, deadline.Token);
      session.Undo.UndoTitle.ShouldBe("Automap: Second Room");

      Should.NotThrow(() => session.PerformUndo());

      Rooms().Select(room => room.Name).ShouldBe(new[] { "First Room" });
      run.IsCompleted.ShouldBeFalse();

      // the engine still remembers the deleted Second Room; the next move must not use it
      File.AppendAllText(settings.FileName, "west\n\nThird Room\nYet another room.\n\n>");
      await WaitUntil(() => Rooms().Count == 2, run, deadline.Token);
      await Task.Delay(150, deadline.Token);

      Rooms().Select(room => room.Name).OrderBy(name => name).ToArray()
        .ShouldBe(new[] { "First Room", "Third Room" });
      var rooms = Rooms();
      foreach (var connection in Project.Current.Elements.OfType<Connection>())
      {
        rooms.ShouldContain(connection.GetSourceRoom(out _));
        rooms.ShouldContain(connection.GetTargetRoom());
      }

      run.IsCompleted.ShouldBeFalse();
      session.Undo.UndoTitle.ShouldBe("Automap: Third Room");
    }
    finally
    {
      parser.Stop();
      await run.WaitAsync(TimeSpan.FromSeconds(5));
    }
  }
}
