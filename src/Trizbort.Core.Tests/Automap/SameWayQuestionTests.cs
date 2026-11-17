using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using AutomapEngine = Trizbort.Automap.Automap;

namespace Trizbort.Core.Tests.Automap;

/// <summary>
///   Lee, 16-17 Nov 2026: a way already seen leading to one room, now leading to another. Both are real, so both are
///   kept, the new room beside the old, unless the user says the room left moves (a lift) or was another room with its
///   name (a maze). The app asks at that move, in a dialog, and automap waits for the answer (a wrong one leads to
///   more mistakes); tests and headless runs keep both.
/// </summary>
[TestFixture]
[Category("Unit")]
public class SameWayQuestionTests : IsolatedProjectTests
{
  // Hall, east to the Pantry and back; a lever pulled; east again, to the Kitchen; then a look
  private static readonly string[] _game =
  {
    ">look", "Hall", "A grand hall.", "",
    ">east", "Pantry", "A pantry.", "",
    ">west", "Hall", "A grand hall.", "",
    ">pull lever", "The room shakes.", "",
    ">east", "Kitchen", "A kitchen.", "",
    ">look", "Kitchen", "A kitchen.", "",
    ">"
  };

  private static async Task WaitFor(Func<bool> condition, string what)
  {
    var clock = Stopwatch.StartNew();
    while (!Holds(condition))
    {
      if (clock.ElapsedMilliseconds > 8000) Assert.Fail("Timed out waiting for " + what);
      await Task.Delay(10);
    }
  }

  // automap changes the map on its own thread: a look at it mid-change is tried again
  private static bool Holds(Func<bool> condition)
  {
    try
    {
      return condition();
    }
    catch (InvalidOperationException)
    {
      return false;
    }
  }

  private static List<Room> Rooms(string name) =>
    Project.Current.Elements.OfType<Room>().Where(room => room.Name == name).ToList();

  private static IEnumerable<Room> JoinedTo(Room room) =>
    room.GetConnections().Select(join => join.GetSourceRoom() == room ? join.GetTargetRoom() : join.GetSourceRoom())
      .Where(other => other != null && other != room);

  /// <summary>Answers the question as told, once <see cref="Answer" /> is set; records what it was asked.</summary>
  private sealed class Asked : IEditorPrompts
  {
    public TaskCompletionSource<AutomapSameDirectionResult> Answer { get; } =
      new(TaskCreationOptions.RunContinuationsAsynchronously);

    public List<(string From, string Way, string Before, string Now)> Questions { get; } = new();

    public Task<AutomapSameDirectionResult> ChooseSameWayAsync(Room from, string way, Room existing,
      string newRoomName, CancellationToken token = default)
    {
      lock (Questions) Questions.Add((from?.Name, way, existing?.Name, newRoomName));
      return Answer.Task;
    }

    public Task<RoomDisambiguation> DisambiguateRoomAsync(string transcriptLine, string roomDescription,
      IReadOnlyList<Room> candidates, CancellationToken token = default) =>
      NonInteractiveEditorPrompts.Instance.DisambiguateRoomAsync(transcriptLine, roomDescription, candidates, token);

    public Task<bool> ConfirmAsync(string message, string title, CancellationToken token = default) =>
      NonInteractiveEditorPrompts.Instance.ConfirmAsync(message, title, token);
  }

  /// <summary>Automap playing the whole game without pausing (as Play to the End, or live).</summary>
  private sealed class Played : IAsyncDisposable
  {
    public AutomapEngine Engine { get; init; }
    public EditorSession Session { get; init; }
    public Task Run { get; init; }

    public async ValueTask DisposeAsync()
    {
      Engine.Stop();
      if (Run != null) await Run;
      Session.Dispose();
    }
  }

  private Played Play(IEditorPrompts prompts)
  {
    var path = Files.File("game.txt");
    File.WriteAllLines(path, _game);
    var session = new EditorSession();
    var engine = new AutomapEngine((_, _) => { }, prompts);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    settings.MoveByMove = false;
    settings.GuessExits = false;
    settings.AutoTidy = false;
    return new Played
    {
      Engine = engine, Session = session, Run = engine.Start(new AutomapCanvas(null, engine, session), settings)
    };
  }

  [Test]
  public async Task WithNoOneToAsk_BothAreKept_SideBySide()
  {
    await using var played = Play(NonInteractiveEditorPrompts.Instance);
    var engine = played.Engine;
    await WaitFor(() => engine.CurrentRoom?.Name == "Kitchen" && Rooms("Kitchen").Count == 1 &&
                        JoinedTo(Rooms("Kitchen")[0]).Any(), "the Kitchen");

    var hall = Rooms("Hall").ShouldHaveSingleItem();
    JoinedTo(hall).Select(room => room.Name).OrderBy(name => name).ShouldBe(new[] { "Kitchen", "Pantry" });
    // the Kitchen beside the Pantry
    var pantry = Rooms("Pantry").Single();
    var kitchen = Rooms("Kitchen").Single();
    pantry.InnerBounds.IntersectsWith(kitchen.InnerBounds).ShouldBeFalse();
    Math.Abs(kitchen.InnerBounds.Center.X - pantry.InnerBounds.Center.X).ShouldBeLessThan(300);
  }

  [Test]
  public async Task AutomapAsks_AndWaitsForTheAnswer_ThenBothAreRight()
  {
    var prompts = new Asked();
    await using var played = Play(prompts);
    var engine = played.Engine;

    await WaitFor(() => prompts.Questions.Count == 1, "the question");
    prompts.Questions[0].ShouldBe(("Hall", "east", "Pantry", "Kitchen"));
    // nothing more is mapped until it's answered, even playing to the end
    await Task.Delay(300);
    Rooms("Kitchen").ShouldBeEmpty();

    prompts.Answer.SetResult(AutomapSameDirectionResult.SamePlace);
    await WaitFor(() => Rooms("Kitchen").Count == 1 && JoinedTo(Rooms("Kitchen")[0]).Any(), "the Kitchen");
    JoinedTo(Rooms("Hall").Single()).Select(room => room.Name).OrderBy(name => name)
      .ShouldBe(new[] { "Kitchen", "Pantry" });
    prompts.Questions.Count.ShouldBe(1);
  }

  [Test]
  public async Task ItMoves_MapsAStopOfTheRoom_JoinedByWhatMovedIt()
  {
    var prompts = new Asked();
    prompts.Answer.SetResult(AutomapSameDirectionResult.Moves);
    await using var played = Play(prompts);
    var engine = played.Engine;
    await WaitFor(() => Rooms("Hall").Count == 2 && Rooms("Kitchen").Count == 1 &&
                        JoinedTo(Rooms("Kitchen")[0]).Any(), "the move mapped");

    var hall = Rooms("Hall").Single(room => !room.IsStop);
    var stop = Rooms("Hall").Single(room => room.IsStop);
    hall.Moves.ShouldBeTrue();
    JoinedTo(hall).Select(room => room.Name).OrderBy(name => name).ShouldBe(new[] { "Hall", "Pantry" });
    JoinedTo(stop).Select(room => room.Name).OrderBy(name => name).ShouldBe(new[] { "Hall", "Kitchen" });
    var byLever = hall.GetConnections().Single(join => join.GetTargetRoom() == stop || join.GetSourceRoom() == stop);
    JoinWays.Commands(JoinWays.Of(byLever)).ShouldContain("pull lever");
  }

  [Test]
  public async Task ADifferentRoom_MapsTheMoveFromAnotherRoomWithItsName()
  {
    var prompts = new Asked();
    prompts.Answer.SetResult(AutomapSameDirectionResult.DifferentRoom);
    await using var played = Play(prompts);
    var engine = played.Engine;
    await WaitFor(() => Rooms("Hall (2)").Count == 1 && Rooms("Kitchen").Count == 1 &&
                        JoinedTo(Rooms("Kitchen")[0]).Any(), "the move mapped");

    var hall = Rooms("Hall").Single();
    var other = Rooms("Hall (2)").Single();
    JoinedTo(hall).Select(room => room.Name).ShouldBe(new[] { "Pantry" });
    JoinedTo(other).Select(room => room.Name).OrderBy(name => name).ShouldBe(new[] { "Kitchen", "Pantry" });
  }
}
