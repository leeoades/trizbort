using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
///   Lee, 16 Nov 2026: a way already seen leading to one room, now leading to another, keeps both, without asking:
///   both are real. The new room goes beside the old one, and the automap panel says so, offering to map that move
///   again if the room left moves (a lift) or was another room with its name (a maze). Lee, 17 Nov: with the user
///   there, automap waits for the answer before mapping on (a wrong answer leads to more mistakes).
/// </summary>
[TestFixture]
[Category("Unit")]
public class SameWayNoteTests : IsolatedProjectTests
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
    while (!condition())
    {
      if (clock.ElapsedMilliseconds > 8000) Assert.Fail("Timed out waiting for " + what);
      await Task.Delay(10);
    }
  }

  private static List<Room> Rooms(string name) =>
    Project.Current.Elements.OfType<Room>().Where(room => room.Name == name).ToList();

  private static IEnumerable<Room> JoinedTo(Room room) =>
    room.GetConnections().Select(join => join.GetSourceRoom() == room ? join.GetTargetRoom() : join.GetSourceRoom())
      .Where(other => other != null && other != room);

  /// <summary>Automap, part-way through the game: move by move, with an undo history, paused before the look.</summary>
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

  /// <summary>Plays the game move by move up to the look at the end.</summary>
  private async Task<Played> PlayToTheLook()
  {
    var path = Files.File("game.txt");
    File.WriteAllLines(path, _game);
    var session = new EditorSession();
    var engine = new AutomapEngine((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    settings.MoveByMove = true;
    settings.GuessExits = false;
    settings.AutoTidy = false;
    var played = new Played
    {
      Engine = engine, Session = session, Run = engine.Start(new AutomapCanvas(null, engine, session), settings)
    };
    for (var move = 0; move < 5; move++)
    {
      await WaitFor(() => engine.NextCommand != null, "the next move");
      var before = engine.MoveCount().Played;
      engine.PlayNextMove();
      await WaitFor(() => engine.MoveCount().Played > before && engine.NextCommand != null, "the move");
    }

    engine.NextCommand.ShouldBe("look");
    return played;
  }

  [Test]
  public async Task AWayThatNowLeadsSomewhereNew_KeepsBoth_WithoutAsking_AndSaysSo()
  {
    await using var played = await PlayToTheLook();
    var engine = played.Engine;
    {
      var hall = Rooms("Hall").ShouldHaveSingleItem();
      JoinedTo(hall).Select(room => room.Name).OrderBy(name => name).ShouldBe(new[] { "Kitchen", "Pantry" });
      engine.CurrentRoom.Name.ShouldBe("Kitchen");
      // the Kitchen beside the Pantry
      var pantry = Rooms("Pantry").Single();
      var kitchen = Rooms("Kitchen").Single();
      pantry.InnerBounds.IntersectsWith(kitchen.InnerBounds).ShouldBeFalse();
      Math.Abs(kitchen.InnerBounds.Center.X - pantry.InnerBounds.Center.X).ShouldBeLessThan(300);

      var note = engine.SameWay.ShouldNotBeNull();
      (note.From, note.Way, note.Before, note.Now).ShouldBe(("Hall", "east", "Pantry", "Kitchen"));
      engine.CanCorrectSameWay.ShouldBeTrue();

      // the next move: the note goes
      engine.PlayNextMove();
      await WaitFor(() => engine.NextCommand == null || engine.MoveCount().Played == engine.MoveCount().Total,
        "the look");
      await WaitFor(() => engine.SameWay == null, "the note to go");
    }
  }

  [Test]
  public async Task ItMoves_MapsThatMoveAgain_WithAStopOfTheRoom_JoinedByWhatMovedIt()
  {
    await using var played = await PlayToTheLook();
    var engine = played.Engine;
    {
      engine.CorrectSameWay(AutomapSameDirectionResult.Moves);
      await WaitFor(() => Rooms("Hall").Count == 2 && engine.NextCommand == "look", "the move mapped again");

      var hall = Rooms("Hall").Single(room => !room.IsStop);
      var stop = Rooms("Hall").Single(room => room.IsStop);
      hall.Moves.ShouldBeTrue();
      JoinedTo(hall).Select(room => room.Name).OrderBy(name => name).ShouldBe(new[] { "Hall", "Pantry" });
      JoinedTo(stop).Select(room => room.Name).OrderBy(name => name).ShouldBe(new[] { "Hall", "Kitchen" });
      var byLever = hall.GetConnections().Single(join => join.GetTargetRoom() == stop || join.GetSourceRoom() == stop);
      JoinWays.Commands(JoinWays.Of(byLever)).ShouldContain("pull lever");
      engine.CurrentRoom.Name.ShouldBe("Kitchen");
      engine.SameWay.ShouldBeNull();
      // paused again before the look, as before
      engine.MoveByMove.ShouldBeTrue();
    }
  }

  [Test]
  public async Task ADifferentRoom_MapsThatMoveAgain_FromAnotherRoomWithItsName()
  {
    await using var played = await PlayToTheLook();
    var engine = played.Engine;
    {
      engine.CorrectSameWay(AutomapSameDirectionResult.DifferentRoom);
      await WaitFor(() => Rooms("Hall (2)").Count == 1 && engine.NextCommand == "look", "the move mapped again");

      var hall = Rooms("Hall").Single();
      var other = Rooms("Hall (2)").Single();
      JoinedTo(hall).Select(room => room.Name).ShouldBe(new[] { "Pantry" });
      JoinedTo(other).Select(room => room.Name).OrderBy(name => name).ShouldBe(new[] { "Kitchen", "Pantry" });
      engine.CurrentRoom.Name.ShouldBe("Kitchen");
    }
  }

  /// <summary>The app's prompts: the user is there to answer the automap panel's note, so automap waits for it.</summary>
  private sealed class UserThere : IEditorPrompts
  {
    public bool AnswersInTheAutomapPanel => true;

    public Task<RoomDisambiguation> DisambiguateRoomAsync(string transcriptLine, string roomDescription,
      IReadOnlyList<Room> candidates, System.Threading.CancellationToken token = default) =>
      NonInteractiveEditorPrompts.Instance.DisambiguateRoomAsync(transcriptLine, roomDescription, candidates, token);

    public Task<bool> ConfirmAsync(string message, string title, System.Threading.CancellationToken token = default) =>
      NonInteractiveEditorPrompts.Instance.ConfirmAsync(message, title, token);
  }

  /// <summary>Plays the whole game without pausing (as Play to the End, or live), the user there to answer.</summary>
  private async Task<Played> PlayToTheEndWithTheUserThere()
  {
    var path = Files.File("game.txt");
    File.WriteAllLines(path, _game);
    var session = new EditorSession();
    var engine = new AutomapEngine((_, _) => { }, new UserThere());
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    settings.MoveByMove = false;
    settings.GuessExits = false;
    settings.AutoTidy = false;
    var played = new Played
    {
      Engine = engine, Session = session, Run = engine.Start(new AutomapCanvas(null, engine, session), settings)
    };
    await WaitFor(() => engine.WaitingForSameWay, "automap to wait for the answer");
    return played;
  }

  [Test]
  public async Task WithTheUserThere_AutomapWaitsForTheAnswer_EvenPlayingToTheEnd_AndNextMoveCantSkipIt()
  {
    await using var played = await PlayToTheEndWithTheUserThere();
    var engine = played.Engine;

    // the move is mapped, both kept, and the note says so; the look after it isn't played
    Rooms("Kitchen").Count.ShouldBe(1);
    engine.SameWay.ShouldNotBeNull();
    var waitingAt = engine.MoveCount().Played;
    engine.Status.ShouldContain("waiting");
    engine.PlayNextMove();
    await Task.Delay(300);
    engine.WaitingForSameWay.ShouldBeTrue();
    engine.MoveCount().Played.ShouldBe(waitingAt);

    // both are right: automap carries on to the end
    engine.DismissSameWay();
    await WaitFor(() => !engine.WaitingForSameWay && engine.MoveCount().Played == engine.MoveCount().Total,
      "the rest of the game");
    engine.MoveByMove.ShouldBeFalse();
    Rooms("Hall").Count.ShouldBe(1);
  }

  [Test]
  public async Task WithTheUserThere_ItMoves_MapsThatMoveAgain_ThenCarriesOnToTheEnd()
  {
    await using var played = await PlayToTheEndWithTheUserThere();
    var engine = played.Engine;

    engine.CorrectSameWay(AutomapSameDirectionResult.Moves);
    await WaitFor(() => Rooms("Hall").Count == 2 && !engine.WaitingForSameWay &&
                        engine.MoveCount().Played == engine.MoveCount().Total, "the move mapped again, and the rest");

    Rooms("Hall").Single(room => room.IsStop).ShouldNotBeNull();
    engine.CurrentRoom.Name.ShouldBe("Kitchen");
    engine.MoveByMove.ShouldBeFalse();
    // the move mapped the way the user said doesn't ask again
    engine.SameWay.ShouldBeNull();
  }

  [Test]
  public async Task AfterAChangeOfTheUsers_TheMoveIsNotMappedAgain()
  {
    await using var played = await PlayToTheLook();
    var engine = played.Engine;
    var session = played.Session;
    {
      using (session.BeginTransaction("Rename"))
        Rooms("Kitchen").Single().Name = "Larder";

      engine.CorrectSameWay(AutomapSameDirectionResult.Moves);
      await Task.Delay(300);

      Rooms("Larder").Count.ShouldBe(1);
      Rooms("Hall").Count.ShouldBe(1);
      engine.Status.ShouldContain("can't map it again");
      engine.SameWay.ShouldBeNull();
    }
  }
}
