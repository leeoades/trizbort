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
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests.Automap;

/// <summary>Playing a transcript a move at a time: automap pauses before each command until told to go on.</summary>
[TestFixture]
[Category("Unit")]
public class AutomapMoveByMoveTests : IsolatedProjectTests
{
  private static List<string> Rooms() =>
    Project.Current.Elements.OfType<Room>().Select(room => room.Name).OrderBy(name => name).ToList();

  private static async Task WaitFor(Func<bool> condition)
  {
    var clock = Stopwatch.StartNew();
    while (!condition())
    {
      if (clock.ElapsedMilliseconds > 5000) Assert.Fail("Timed out waiting for automap.");
      await Task.Delay(10);
    }
  }

  [Test]
  public async Task MoveByMove_PausesBeforeEachCommand_WithTheTranscriptSoFarOnScreen()
  {
    var path = Path.GetTempFileName();
    File.WriteAllLines(path, new[]
    {
      ">look", "Hall", "A grand hall. A door leads north.", "",
      ">north", "Kitchen", "A small kitchen. The hall is south.", "",
      ">south", "Hall", "",
      ">"
    });
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    settings.MoveByMove = true;
    try
    {
      var run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);

      await WaitFor(() => engine.NextCommand == "look");
      engine.MoveByMove.ShouldBeTrue();
      Rooms().ShouldBeEmpty();
      engine.ScreenLines().ShouldBe(new[] { ">look" });
      engine.MovesAhead().ShouldBe(3); // look, north, south: the empty prompt at the end isn't a move

      engine.PlayNextMove();
      await WaitFor(() => engine.NextCommand == "north");
      Rooms().ShouldBe(new[] { "Hall" });
      engine.ScreenLines().Last().ShouldBe(">north");
      engine.ScreenLines(1).First().ShouldBe("Hall");

      engine.PlayNextMove();
      await WaitFor(() => engine.NextCommand == "south");
      Rooms().ShouldBe(new[] { "Hall", "Kitchen" });
      engine.MovesAhead().ShouldBe(1);

      engine.PlayToEnd();
      await WaitFor(() => engine.ScreenLines().Count == 12 && engine.NextCommand == null);
      engine.MoveByMove.ShouldBeFalse();

      engine.Stop();
      await run;
    }
    finally
    {
      engine.Stop();
      File.Delete(path);
    }
  }

  [Test]
  public async Task MoveByMove_AtTheEnd_WhenTheGameGoesOn_SaysSo_AndPlayToEndFollowsItLive()
  {
    // a new game, move by move left on: the player types look in the game
    var path = Path.GetTempFileName();
    File.WriteAllText(path, ">look\nHall\nA grand hall. A door leads north.\n\n>");
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    settings.MoveByMove = true;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      await WaitFor(() => engine.NextCommand == "look");
      engine.GameWentOn.ShouldBeFalse();
      engine.PlayNextMove();
      await WaitFor(() => engine.Status == "Automapping is waiting for more text.");

      File.AppendAllText(path, "north\nKitchen\nA small kitchen.\n\n>");
      await WaitFor(() => engine.NextCommand == "north");
      engine.GameWentOn.ShouldBeTrue();
      engine.Status.ShouldContain("The game has gone on");
      Rooms().ShouldBe(new[] { "Hall" });

      engine.PlayToEnd();
      await WaitFor(() => Rooms().Count == 2);
      engine.GameWentOn.ShouldBeFalse();
      engine.MoveByMove.ShouldBeFalse();
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }

  private static List<string> Stubs() =>
    Project.Current.Elements.OfType<Connection>()
      .Select(connection => (source: connection.GetSourceRoom(out var from), target: connection.GetTargetRoom(out var to), from, to))
      .Where(x => x.source != null && x.source == x.target && x.from == x.to)
      .Select(x => x.source.Name + " " + x.from).OrderBy(name => name).ToList();

  [Test]
  public async Task AWayThatLeadsNowhere_LosesItsStub_AsSoonAsTheReplyIsIn()
  {
    // playing live: the game has answered "east" and waits at the prompt for the next command
    var path = Path.GetTempFileName();
    File.WriteAllLines(path, new[]
    {
      ">look", "Hall", "A grand hall. Doors lead north and east.", "",
      ">east", "You can't go that way.", "",
      ">"
    });
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    try
    {
      var run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);

      await WaitFor(() => engine.ScreenLines().Count == 8);
      await Task.Delay(200);
      TestContext.Out.WriteLine(string.Join(", ", Stubs()));
      Stubs().ShouldNotContain(stub => stub.EndsWith("East"));
      Stubs().ShouldContain("Hall North");

      engine.Stop();
      await run;
    }
    finally
    {
      engine.Stop();
      File.Delete(path);
    }
  }

  [Test]
  public async Task CarryingOnFromTheEnd_OfAGameStillRunning_ReadsTheNextCommandTyped()
  {
    // the map already has the room; the game waits at its prompt
    var path = Path.GetTempFileName();
    File.WriteAllText(path, ">look\nHall\nA grand hall. A door leads north.\n\n>");
    var hall = new Room(Project.Current) { Name = "Hall" };
    Project.Current.Elements.Add(hall);
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    settings.ContinueTranscript = true;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      // the prompt is on the game monitor: automap waits for the command
      await WaitFor(() => engine.ScreenLines().Count == 1);
      await Task.Delay(100);

      // the player types look, then goes north
      File.AppendAllText(path, "look\nHall\nA grand hall. A door leads north.\n\n>north\nKitchen\nA small kitchen.\n\n>");
      await WaitFor(() => engine.ScreenLines().Count == 10);
      await Task.Delay(100);
      Rooms().ShouldBe(new[] { "Hall", "Kitchen" });
      var join = Project.Current.Elements.OfType<Connection>().Single(connection => connection.GetSourceRoom() != connection.GetTargetRoom());
      join.GetSourceRoom().Name.ShouldBe("Hall");
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }

  private static List<string> Joins() =>
    Project.Current.Elements.OfType<Connection>()
      .Where(connection => connection.GetSourceRoom() != null && connection.GetSourceRoom() != connection.GetTargetRoom())
      .Select(connection => connection.GetSourceRoom().Name + " -> " + connection.GetTargetRoom()?.Name +
                            (string.IsNullOrEmpty(connection.MidText) ? "" : " " + connection.MidText))
      .OrderBy(join => join).ToList();

  private static Room AddRoom(string name, bool automapPause = false, int column = 0)
  {
    var room = new Room(Project.Current) { Name = name, AutomapPause = automapPause, Position = new Vector(column * 200, 0) };
    Project.Current.Elements.Add(room);
    return room;
  }

  /// <summary>Runs automap over <paramref name="lines" /> to the end (the game waits at its prompt).</summary>
  private static async Task RunToEnd(Trizbort.Automap.Automap engine, string[] lines, int screenLines)
  {
    var path = Path.GetTempFileName();
    File.WriteAllText(path, string.Join("\n", lines));
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      await WaitFor(() => engine.ScreenLines().Count >= screenLines);
      await Task.Delay(200);
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }

  [Test]
  public async Task LeavingARoomMarkedDontAutomap_MapsWhereYouComeOut_WithADottedQuestionMarkJoin()
  {
    // Hollywood Hijinx: the cupboard off the foyer is a lift; pulling a peg takes it down to the cellar
    var foyer = AddRoom("Foyer");
    AddRoom("Cupboard", automapPause: true, column: 1);
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var lines = new[]
    {
      ">look", "Foyer", "A grand foyer. A cupboard is set into the east wall.", "",
      ">enter cupboard", "Cupboard", "A cramped cupboard with pegs on the wall.", "",
      ">pull first peg", "The cupboard lurches and sinks.", "",
      ">look", "Cupboard", "A cramped cupboard with pegs on the wall.", "",
      ">out", "Cellar", "A damp cellar. A passage leads west.", "",
      ">west", "Wine Store", "Racks of dusty bottles.", "",
      ">"
    };

    await RunToEnd(engine, lines, lines.Length);

    Rooms().ShouldBe(new[] { "Cellar", "Cupboard", "Foyer", "Wine Store" });
    Joins().ShouldContain("Foyer -> Cupboard enter cupboard");
    Joins().ShouldContain("Cupboard -> Cellar " + Trizbort.Automap.Automap.UnmappedWay);
    Joins().ShouldContain("Cellar -> Wine Store");
    // the cupboard's way out isn't taken to lead back to the foyer, or anywhere else
    Joins().Count.ShouldBe(3);
    foyer.Name.ShouldBe("Foyer");
  }

  [Test]
  public async Task InAMazeMarkedDontAutomap_MovesStayInTheMaze_AndLeavingToAKnownRoomJumpsThere()
  {
    AddRoom("Hall");
    AddRoom("Maze", automapPause: true, column: 1);
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var lines = new[]
    {
      ">look", "Hall", "A grand hall.", "",
      ">east", "Maze", "You are in a maze of twisty little passages, all alike.", "",
      ">north", "Maze", "You are in a maze of twisty little passages, all different.", "",
      ">up", "Hall", "A grand hall.", "",
      ">south", "Kitchen", "A small kitchen.", "",
      ">"
    };

    await RunToEnd(engine, lines, lines.Length);

    Rooms().ShouldBe(new[] { "Hall", "Kitchen", "Maze" });
    // into the maze is mapped; nothing in it or out of it is; mapping goes on from the hall
    Joins().ShouldBe(new[] { "Hall -> Kitchen", "Hall -> Maze" }, ignoreOrder: true);
  }

  [TestCase("pull first peg")]
  [TestCase("pull peg", "first")]
  public async Task AJoinTheUserDrew_LabelledWithTheCommand_TakesThePlayerAlongIt(params string[] commands)
  {
    // Hollywood Hijinx: the user drew the lift's way down themselves
    var cupboard = AddRoom("Cupboard", automapPause: true);
    var below = AddRoom("Cupboard (-1)", column: 1);
    var lift = new Connection(Project.Current, new Vertex(cupboard.PortAt(CompassPoint.South)), new Vertex(below.PortAt(CompassPoint.North)))
    {
      Flow = ConnectionFlow.OneWay,
      MidText = "Pull First Peg"
    };
    Project.Current.Elements.Add(lift);
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var lines = new List<string> { ">look", "Cupboard", "A cramped cupboard with pegs on the wall.", "" };
    if (commands.Length == 2)
      lines.AddRange(new[] { ">" + commands[0], "Which peg do you mean, the first peg or the second peg?", "" });
    lines.AddRange(new[]
    {
      ">" + commands[^1], "The cupboard lurches and sinks.", "",
      ">look", "Cupboard", "A cramped cupboard with pegs on the wall.", "",
      ">out", "Cellar", "A damp cellar.", "",
      ">"
    });

    await RunToEnd(engine, lines.ToArray(), lines.Count);

    Rooms().ShouldBe(new[] { "Cellar", "Cupboard", "Cupboard (-1)" });
    // the lift's way out leads to the cellar; nothing joins the cellar to the cupboard upstairs
    Joins().ShouldBe(new[] { "Cupboard -> Cupboard (-1) Pull First Peg", "Cupboard (-1) -> Cellar" });
  }

  [Test]
  public async Task TheWayBackFromTheCloset_DoesntTakeTheFoyersSouthSide_WhenTheFrontPorchIsThere()
  {
    // Hollywood Hijinx: south from the foyer is the front porch; "in" to the closet, "north" back out
    var porch = new Room(Project.Current) { Name = "Front Porch", Position = new Vector(0, 300), Size = new Vector(96, 64) };
    var foyer = new Room(Project.Current) { Name = "Foyer", Position = new Vector(0, 0), Size = new Vector(96, 64) };
    Project.Current.Elements.Add(porch);
    Project.Current.Elements.Add(foyer);
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    new AutomapCanvas(Project.Current, engine).Connect(porch, MappableDirection.North, foyer, true);
    var lines = new[]
    {
      ">look", "Foyer", "An elegant foyer.", "",
      ">in", "Closet", "A small closet.", "",
      ">out", "Foyer", "",
      ">enter closet", "Closet", "",
      ">north", "Foyer", "",
      ">"
    };

    await RunToEnd(engine, lines, lines.Length);

    var closet = Project.Current.Elements.OfType<Room>().Single(room => room.Name == "Closet");
    var join = Project.Current.Elements.OfType<Connection>().Single(connection =>
      connection.GetSourceRoom() == foyer && connection.GetTargetRoom() == closet);
    join.GetSourceRoom(out var side);
    side.ShouldNotBe(CompassPoint.South);
    join.StartText.ShouldBe("in / enter closet");
    join.MidText.ShouldBeEmpty();
  }

  [Test]
  public async Task AQuestionInBrackets_AndTheAnswer_FollowTheJoinLabelledWithBoth()
  {
    // Hollywood Hijinx: "pull peg", "[Which peg do you mean, ...?]", "second" follows the join end labelled
    // "Pull second peg" back up
    var cupboard = AddRoom("Cupboard");
    var below = AddRoom("Cupboard (-1)", column: 1);
    var lift = new Connection(Project.Current, new Vertex(cupboard.PortAt(CompassPoint.East)), new Vertex(below.PortAt(CompassPoint.West)))
    {
      StartText = "Pull first peg",
      EndText = "Pull second peg"
    };
    Project.Current.Elements.Add(lift);
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var lines = new[]
    {
      ">look", "Cupboard", "A cramped cupboard with pegs on the wall.", "",
      ">pull first peg", "The cupboard moves down, then stops.", "",
      ">pull peg", "[Which peg do you mean, the first coat peg, the second coat peg, the third coat",
      "peg, or the sawed-off peg?]", "",
      ">second", "The cupboard moves up, then stops.", "",
      ">north", "Hall", "A grand hall.", "",
      ">"
    };

    await RunToEnd(engine, lines, lines.Length);

    Joins().ShouldContain("Cupboard -> Hall");
    Joins().ShouldNotContain(join => join.StartsWith("Cupboard (-1) -> Hall"));
  }

  private sealed class SplitPrompts : IEditorPrompts
  {
    public string AskedAbout;
    public AutomapSameDirectionResult Answer = AutomapSameDirectionResult.Split;

    public Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(Room existing, string newRoomName, System.Threading.CancellationToken token = default) =>
      Task.FromResult(AutomapSameDirectionResult.KeepRoom1);

    public Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(Room from, Room existing, string newRoomName, System.Threading.CancellationToken token = default)
    {
      AskedAbout = from?.Name;
      return Task.FromResult(Answer);
    }

    public Task<RoomDisambiguation> DisambiguateRoomAsync(string transcriptLine, string roomDescription, IReadOnlyList<Room> candidates, System.Threading.CancellationToken token = default) =>
      Task.FromResult(RoomDisambiguation.Choose(candidates[0]));
  }

  [Test]
  public async Task ADifferentRoomYouJustCameInto_TakesTheWayIn_AndTheNewRoomJoinsIt()
  {
    // two maze rooms alike: east from the hall looked like the maze north of it, but its south leads to a pit
    var prompts = new SplitPrompts { Answer = AutomapSameDirectionResult.DifferentRoom };
    var engine = new Trizbort.Automap.Automap((_, _) => { }, prompts);
    var lines = new[]
    {
      ">look", "Hall", "A grand hall.", "",
      ">north", "Maze", "Twisty little passages.", "",
      ">south", "Hall", "",
      ">east", "Maze", "Twisty little passages.", "",
      ">south", "Pit", "A deep pit.", "",
      ">"
    };

    await RunToEnd(engine, lines, lines.Length);

    prompts.AskedAbout.ShouldBe("Maze");
    Rooms().ShouldBe(new[] { "Hall", "Maze", "Maze (2)", "Pit" });
    Joins().ShouldContain("Hall -> Maze");
    Joins().ShouldContain("Hall -> Maze (2)");
    Joins().ShouldContain("Maze (2) -> Pit");
    Joins().ShouldNotContain(join => join.StartsWith("Maze -> Pit") || join.StartsWith("Maze -> Maze (2)"));
    var hallToMaze = Project.Current.Elements.OfType<Connection>().Single(c =>
      c.GetSourceRoom()?.Name == "Hall" && c.GetTargetRoom()?.Name == "Maze");
    JoinWays.Of(hallToMaze).ShouldNotContain(way => way.IsDirection && way.Direction == MappableDirection.East);
  }

  [Test]
  public async Task TheSamePlaceAtDifferentTimes_KeepsBothJoins_WithTheRoomsSideBySide()
  {
    // Hollywood Hijinx: south from the cellar is the shaft bottom, or the closet once it has come down
    var prompts = new SplitPrompts { Answer = AutomapSameDirectionResult.SamePlace };
    var engine = new Trizbort.Automap.Automap((_, _) => { }, prompts);
    var lines = new[]
    {
      ">look", "Cellar", "A musty cellar.", "",
      ">south", "Shaft Bottom", "The bottom of a shaft.", "",
      ">north", "Cellar", "",
      ">south", "Closet", "A small closet.", "",
      ">"
    };

    await RunToEnd(engine, lines, lines.Length);

    prompts.AskedAbout.ShouldBe("Cellar");
    Joins().ShouldContain("Cellar -> Shaft Bottom");
    Joins().ShouldContain("Cellar -> Closet");
    var rooms = Project.Current.Elements.OfType<Room>().ToList();
    var shaft = rooms.Single(room => room.Name == "Shaft Bottom");
    var closet = rooms.Single(room => room.Name == "Closet");
    closet.Position.Y.ShouldBe(shaft.Position.Y);
    closet.Position.X.ShouldBeGreaterThan(shaft.Position.X);
    (closet.Position.X - shaft.Position.X).ShouldBeLessThan(shaft.Width * 2);
  }

  [Test]
  public async Task ThisIsADifferentRoom_AfterAWayTakenForALoop_PutsTheNewRoomThatWay()
  {
    // Hollywood Hijinx: up the chimney to another "In the Chimney", which automap took for the same one (a loop)
    var path = Path.GetTempFileName();
    File.WriteAllLines(path, new[]
    {
      ">look", "Fireplace", "The fireplace opens to the east.", "",
      ">up", "In the Chimney", "You're in the chimney, above the fireplace.", "",
      ">up", "In the Chimney", "",
      ">"
    });
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      await WaitFor(() => engine.ScreenLines().Count >= 11);
      await Task.Delay(200);
      var chimney = Project.Current.Elements.OfType<Room>().Single(room => room.Name == "In the Chimney");
      Project.Current.Elements.OfType<Connection>()
        .ShouldContain(c => c.GetSourceRoom() == chimney && c.GetTargetRoom() == chimney);
      engine.CanSplitCurrentRoom.ShouldBeTrue();

      (await engine.SplitCurrentRoomAsync()).ShouldBeTrue();

      var upper = engine.CurrentRoom;
      upper.Name.ShouldBe("In the Chimney (2)");
      upper.Position.Y.ShouldBeLessThan(chimney.Position.Y); // up, above it
      Joins().ShouldContain("In the Chimney -> In the Chimney (2)");
      Project.Current.Elements.OfType<Connection>()
        .ShouldNotContain(c => c.GetSourceRoom() == chimney && c.GetTargetRoom() == chimney);
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }

  [Test]
  public async Task ThisIsADifferentRoom_SplitsTheRoomYouAreIn_OffTheWayYouCameIn()
  {
    var path = Path.GetTempFileName();
    File.WriteAllLines(path, new[]
    {
      ">look", "Hall", "A grand hall.", "",
      ">north", "Maze", "Twisty little passages.", "",
      ">south", "Hall", "",
      ">east", "Maze", "Twisty little passages.", "",
      ">"
    });
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      await WaitFor(() => engine.ScreenLines().Count >= 13);
      await Task.Delay(200);
      engine.CurrentRoom.Name.ShouldBe("Maze");
      engine.CanSplitCurrentRoom.ShouldBeTrue();

      (await engine.SplitCurrentRoomAsync()).ShouldBeTrue();

      engine.CurrentRoom.Name.ShouldBe("Maze (2)");
      Rooms().ShouldBe(new[] { "Hall", "Maze", "Maze (2)" });
      Joins().ShouldContain("Hall -> Maze");
      Joins().ShouldContain("Hall -> Maze (2)");
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }

  [Test]
  public async Task SplittingTheRoomYouLeft_MakesItTwo_JoinedByWhatYouDidThere()
  {
    // the cupboard's way out led to the foyer; after pulling a peg it leads to a cellar
    var prompts = new SplitPrompts();
    var engine = new Trizbort.Automap.Automap((_, _) => { }, prompts);
    var lines = new[]
    {
      ">look", "Foyer", "A grand foyer.", "",
      ">enter cupboard", "Cupboard", "A cramped cupboard with pegs on the wall.", "",
      ">out", "Foyer", "A grand foyer.", "",
      ">enter cupboard", "Cupboard", "A cramped cupboard with pegs on the wall.", "",
      ">pull peg", "Which peg do you mean, the first peg or the second peg?", "",
      ">first", "The cupboard lurches and sinks.", "",
      ">out", "Cellar", "A damp cellar.", "",
      ">"
    };

    await RunToEnd(engine, lines, lines.Length);

    prompts.AskedAbout.ShouldBe("Cupboard");
    Rooms().ShouldBe(new[] { "Cellar", "Cupboard", "Cupboard (2)", "Foyer" });
    Joins().ShouldContain("Cupboard -> Cupboard (2) pull peg, first");
    Joins().ShouldContain("Cupboard (2) -> Cellar");
    Joins().ShouldNotContain(join => join.StartsWith("Cupboard -> Cellar"));
  }

  [Test]
  public async Task OutOfTheClosetToARoomAlreadyMapped_AfterOutLedToTheFoyer_AsksAndSplitsTheCloset()
  {
    // Hollywood Hijinx: out of the closet led to the foyer; after pulling a peg, the closet sinks, and out leads to
    // the cellar, which is already on the map
    var prompts = new SplitPrompts();
    var engine = new Trizbort.Automap.Automap((_, _) => { }, prompts);
    var lines = new[]
    {
      ">look", "Cellar", "A dank cellar.", "",
      ">up", "Foyer", "An elegant foyer.", "",
      ">enter closet", "Closet", "A coat closet with pegs.", "",
      ">out", "Foyer", "",
      ">enter closet", "Closet", "",
      ">pull peg", "The closet lurches and sinks.", "",
      ">out", "Cellar", "",
      ">"
    };

    await RunToEnd(engine, lines, lines.Length);

    prompts.AskedAbout.ShouldBe("Closet");
    Rooms().ShouldBe(new[] { "Cellar", "Closet", "Closet (2)", "Foyer" });
    Joins().ShouldContain("Closet -> Closet (2) pull peg");
    Joins().ShouldContain("Closet (2) -> Cellar");
    Joins().ShouldNotContain(join => join.StartsWith("Closet -> Cellar"));
  }

  [Test]
  public async Task Dying_AndBeingReincarnated_JoinsNothing()
  {
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var lines = new[]
    {
      ">look", "Hall", "A grand hall.", "",
      ">north", "Ledge", "A narrow ledge over a pit.", "",
      ">jump", "You fall into the pit and break every bone. You are dead.", "",
      "Do you want me to try to reincarnate you?", "",
      ">yes", "All right. You find yourself back where you started.", "",
      "Garden", "A walled garden.", "",
      ">east", "You stumble and fall. You have died.", "", "*** You have died ***", "",
      "Lawn", "A wide lawn.", "",
      ">"
    };

    await RunToEnd(engine, lines, lines.Length);

    Rooms().ShouldBe(new[] { "Garden", "Hall", "Lawn", "Ledge" });
    // "yes" isn't a magic word from the ledge, and dying on the way east doesn't lead to the lawn
    Joins().ShouldBe(new[] { "Hall -> Ledge" });
  }

  [TestCase("You are dead.", true)]
  [TestCase("You have died.", true)]
  [TestCase("    *** You have died ***", true)]
  [TestCase("*** You have won ***", true)]
  [TestCase("You've been killed by the troll.", true)]
  [TestCase("The dead tree creaks.", false)]
  [TestCase("You are standing by a dead end.", false)]
  public void DeathAndTheEnd_AreRecognised(string line, bool recognised)
  {
    Trizbort.Automap.Automap.IsDeathOrEnd(line).ShouldBe(recognised);
  }

  [Test]
  public async Task TrackingOnly_LeavesTheMapAlone_ObjectsAndStubsToo()
  {
    var path = Path.GetTempFileName();
    File.WriteAllText(path, ">look\nHall\nA grand hall. A door leads north.\n\n>");
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      await WaitFor(() => engine.ScreenLines().Count >= 5);
      await Task.Delay(100);
      var hall = Project.Current.Elements.OfType<Room>().Single();
      Stubs().ShouldBe(new[] { "Hall North" });

      // tracking only: "see" commands from the transcript and ways that lead nowhere change nothing
      engine.Suspend();
      File.AppendAllText(path, $"{settings.AddObjectCommand} brass lamp\nOk.\n\n>north\nThe door is locked.\n\n>");
      await WaitFor(() => engine.ScreenLines().Any(line => line.Contains("locked")));
      await Task.Delay(200);

      string.IsNullOrEmpty(hall.Objects).ShouldBeTrue();
      Stubs().ShouldBe(new[] { "Hall North" });
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }

  [Test]
  public async Task SuspendedThenResumed_MapsFromTheNextRoom_JoiningItToNothing()
  {
    var path = Path.GetTempFileName();
    File.WriteAllText(path, ">look\nHall\nA grand hall.\n\n>");
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      await WaitFor(() => engine.ScreenLines().Count == 5);
      await Task.Delay(100);

      // suspended: the player wanders somewhere new; nothing is mapped
      engine.Suspend();
      File.AppendAllText(path, "north\nKitchen\nA small kitchen.\n\n>");
      await WaitFor(() => engine.ScreenLines().Count == 10);
      await Task.Delay(100);
      Rooms().ShouldBe(new[] { "Hall" });
      engine.CurrentRoom.ShouldBeNull();

      // resumed: automap waits to see where the player is, and maps on from there
      engine.Resume();
      File.AppendAllText(path, "east\nGarden\nA walled garden.\n\n>south\nShed\nA potting shed.\n\n>");
      await WaitFor(() => engine.ScreenLines().Count == 19);
      await Task.Delay(100);
      Rooms().ShouldBe(new[] { "Garden", "Hall", "Shed" });
      Joins().ShouldBe(new[] { "Garden -> Shed" });
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }

  [Test]
  public async Task GoingBack_ReadsTheTranscriptAgain_AndPutsBackARoomDeletedByMistake()
  {
    var path = Path.GetTempFileName();
    File.WriteAllLines(path, new[]
    {
      ">look", "Hall", "A grand hall. A door leads north.", "",
      ">north", "Kitchen", "A small kitchen. The hall is south.", "",
      ">south", "Hall", "",
      ">"
    });
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    settings.MoveByMove = true;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      await WaitFor(() => engine.NextCommand == "look");
      engine.PlayNextMove();
      await WaitFor(() => engine.NextCommand == "north");
      engine.PlayNextMove();
      await WaitFor(() => engine.NextCommand == "south");
      Rooms().ShouldBe(new[] { "Hall", "Kitchen" });

      // the Kitchen is deleted by mistake
      var kitchen = Project.Current.Elements.OfType<Room>().Single(room => room.Name == "Kitchen");
      foreach (var join in Project.Current.Elements.OfType<Connection>().ToList()) Project.Current.Elements.Remove(join);
      Project.Current.Elements.Remove(kitchen);

      engine.GoBack();
      await WaitFor(() => engine.NextCommand == "north");
      engine.ScreenLines().Last().ShouldBe(">north");
      Rooms().ShouldBe(new[] { "Hall" });

      engine.PlayNextMove();
      await WaitFor(() => engine.NextCommand == "south");
      Rooms().ShouldBe(new[] { "Hall", "Kitchen" });
      // (exits not yet explored are stubs from a room to itself)
      var joined = Project.Current.Elements.OfType<Connection>()
        .Where(join => join.GetSourceRoom() != join.GetTargetRoom()).ShouldHaveSingleItem();
      new[] { joined.GetSourceRoom()?.Name, joined.GetTargetRoom()?.Name }.ShouldBe(new[] { "Hall", "Kitchen" }, true);

      // and back two moves at once
      engine.GoBack();
      engine.GoBack();
      await WaitFor(() => engine.NextCommand == "look");
      engine.ScreenLines().ShouldBe(new[] { ">look" });
      Rooms().ShouldBe(new[] { "Hall", "Kitchen" });

      engine.Stop();
      await run;
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }

  [Test]
  public async Task GoingBack_AfterPlayingToTheEnd_PausesBeforeTheLastMove_WithTheScreenEndingThere()
  {
    var path = Path.GetTempFileName();
    File.WriteAllLines(path, new[]
    {
      ">look", "Hall", "A grand hall. A door leads north.", "",
      ">north", "Kitchen", "A small kitchen. The hall is south.", "",
      ">south", "Hall", "",
      ">"
    });
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    Task run = null;
    try
    {
      // played to the end: waiting at the last prompt for the game to go on
      run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      await WaitFor(() => engine.ScreenLines().Count == 12);
      await Task.Delay(100);
      engine.MoveByMove.ShouldBeFalse();

      engine.GoBack();
      await WaitFor(() => engine.NextCommand == "south");
      engine.MoveByMove.ShouldBeTrue();
      engine.ScreenLines().Last().ShouldBe(">south");
      engine.ScreenLines().Count.ShouldBe(9);
      engine.CurrentRoom?.Name.ShouldBe("Kitchen");

      engine.GoBack();
      await WaitFor(() => engine.NextCommand == "north");
      engine.ScreenLines().Last().ShouldBe(">north");

      engine.PlayToEnd();
      await WaitFor(() => engine.ScreenLines().Count == 12 && engine.NextCommand == null);
      Rooms().ShouldBe(new[] { "Hall", "Kitchen" });

      engine.Stop();
      await run;
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }

  [Test]
  public async Task TheMoveCount_SaysWhereAutomapIsInTheTranscript()
  {
    var path = Path.GetTempFileName();
    File.WriteAllLines(path, new[]
    {
      ">look", "Hall", "A grand hall.", "",
      ">north", "Kitchen", "A small kitchen.", "",
      ">south", "Hall", "",
      ">"
    });
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      await WaitFor(() => engine.ScreenLines().Count >= 11);
      await Task.Delay(200);

      // at the end: nothing to play
      engine.MoveCount().ShouldBe((3, 3));
      engine.HasNextMove.ShouldBeFalse();

      engine.GoBack();
      await WaitFor(() => engine.NextCommand == "south");
      engine.MoveCount().ShouldBe((2, 3));
      engine.HasNextMove.ShouldBeTrue();
      engine.CanGoBack.ShouldBeTrue();

      // the game goes on: the count grows
      File.WriteAllLines(path, new[]
      {
        ">look", "Hall", "A grand hall.", "",
        ">north", "Kitchen", "A small kitchen.", "",
        ">south", "Hall", "",
        ">west", "Pantry", "A pantry.", "",
        ">"
      });
      engine.MoveCount().Total.ShouldBe(4);
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }

  [Test]
  public async Task GoingBack_WhileWaitingForTheGamesNextLine_GoesBackAMove()
  {
    // a live game: the last move's reply is in, but no prompt yet; going back then crashed (no line read to show)
    var path = Path.GetTempFileName();
    File.WriteAllLines(path, new[]
    {
      ">look", "Hall", "A grand hall. A door leads north.", "",
      ">north", "Kitchen", "A small kitchen. The hall is south.", "",
      ">south", "Hall", ""
    });
    string error = null;
    var engine = new Trizbort.Automap.Automap((message, _) => error = message, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      await WaitFor(() => engine.ScreenLines().Count == 11);
      await Task.Delay(100);

      engine.GoBack();
      await WaitFor(() => engine.NextCommand == "south");
      // and on again
      engine.PlayNextMove();
      await WaitFor(() => engine.NextCommand == null || run.IsCompleted);
      await Task.Delay(200);
      error.ShouldBeNull();
      engine.IsRunning.ShouldBeTrue();
      Rooms().ShouldBe(new[] { "Hall", "Kitchen" });
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }

  [Test]
  public async Task TwoRoomsWithTheSameName_TheJoinOnTheMap_SaysWhichOneYouGoTo()
  {
    // the chimney goes on up into a second room also called "In the Chimney"; the user has drawn it, with a join
    // labelled "n / up" on the north side (not the side automap gives up)
    var lower = new Room(Project.Current) { Name = "In the Chimney", Position = new Vector(0, 300), Size = new Vector(96, 64) };
    var upper = new Room(Project.Current) { Name = "In the Chimney", Position = new Vector(0, 0), Size = new Vector(96, 64) };
    Project.Current.Elements.Add(lower);
    Project.Current.Elements.Add(upper);
    new AutomapCanvas(Project.Current).Connect(lower, MappableDirection.North, upper, true);
    var drawn = Project.Current.Elements.OfType<Connection>().ShouldHaveSingleItem();
    drawn.StartText = "n / up";

    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var lines = new[]
    {
      ">look", "In the Chimney", "A sooty chimney. Light shows far above.", "",
      ">up", "In the Chimney", "Higher up the chimney, the walls are rough.", "",
      ">"
    };

    await RunToEnd(engine, lines, lines.Length);

    // no loop back into the lower room: the join leads up to the other one
    Project.Current.Elements.OfType<Connection>().ShouldHaveSingleItem().ShouldBeSameAs(drawn);
    upper.Descriptions.ShouldContain(d => d.StartsWith("Higher up"));
    lower.Descriptions.ShouldNotContain(d => d.StartsWith("Higher up"));
  }

  private sealed class NewRoomPrompts : IEditorPrompts
  {
    public int Asked;

    public Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(Room existing, string newRoomName, System.Threading.CancellationToken token = default) =>
      Task.FromResult(AutomapSameDirectionResult.KeepRoom1);

    public Task<RoomDisambiguation> DisambiguateRoomAsync(string transcriptLine, string roomDescription, IReadOnlyList<Room> candidates, System.Threading.CancellationToken token = default)
    {
      Asked++;
      return Task.FromResult(RoomDisambiguation.CreateNew());
    }
  }

  [Test]
  public async Task TheSameNameWithADifferentDescription_AsksWhetherItIsANewRoom_Once()
  {
    var prompts = new NewRoomPrompts();
    var engine = new Trizbort.Automap.Automap((_, _) => { }, prompts);
    var lines = new[]
    {
      ">look", "In the Chimney", "A sooty chimney. Light shows far above.", "",
      ">up", "In the Chimney", "Higher up the chimney, the walls are rough.", "",
      ">down", "In the Chimney", "A sooty chimney. Light shows far above.", "",
      ">up", "In the Chimney", "Higher up the chimney, the walls are rough.", "",
      ">"
    };

    await RunToEnd(engine, lines, lines.Length);

    prompts.Asked.ShouldBe(1);
    Rooms().ShouldBe(new[] { "In the Chimney", "In the Chimney" });
    Joins().ShouldHaveSingleItem().ShouldStartWith("In the Chimney -> In the Chimney");
  }

  [Test]
  public async Task AWayBackIntoTheSameRoom_WithTheSameDescription_AsksNothing()
  {
    var prompts = new NewRoomPrompts();
    var engine = new Trizbort.Automap.Automap((_, _) => { }, prompts);
    var lines = new[]
    {
      ">look", "Forest", "Trees all around. Paths lead every way.", "",
      ">north", "Forest", "Trees all around. Paths lead every way.", "",
      ">"
    };

    await RunToEnd(engine, lines, lines.Length);

    prompts.Asked.ShouldBe(0);
    Rooms().ShouldBe(new[] { "Forest" });
  }

  private sealed class SameDirectionPrompts : IEditorPrompts
  {
    public string AskedAbout;

    public Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(Room existing, string newRoomName, System.Threading.CancellationToken token = default)
    {
      AskedAbout = existing?.Name;
      return Task.FromResult(AutomapSameDirectionResult.KeepBoth);
    }

    public Task<RoomDisambiguation> DisambiguateRoomAsync(string transcriptLine, string roomDescription, IReadOnlyList<Room> candidates, System.Threading.CancellationToken token = default) =>
      Task.FromResult(RoomDisambiguation.Choose(candidates[0]));
  }

  [Test]
  public async Task AWayUpDrawnOnTheNorthSide_CountsAsTheWayUp_WhenUpLeadsSomewhereNew()
  {
    // the user drew the chimney going up from the fireplace on its north side, labelled "n / up"
    var fireplace = new Room(Project.Current) { Name = "Fireplace", Position = new Vector(0, 300), Size = new Vector(96, 64) };
    var chimney = new Room(Project.Current) { Name = "In the Chimney", Position = new Vector(0, 0), Size = new Vector(96, 64) };
    Project.Current.Elements.Add(fireplace);
    Project.Current.Elements.Add(chimney);
    var drawn = new Connection(Project.Current, new Vertex(fireplace.PortAt(CompassPoint.North)), new Vertex(chimney.PortAt(CompassPoint.South)))
      { StartText = "n / up" };
    Project.Current.Elements.Add(drawn);

    var prompts = new SameDirectionPrompts();
    var engine = new Trizbort.Automap.Automap((_, _) => { }, prompts);
    var lines = new[]
    {
      ">look", "Fireplace", "A big fireplace.", "",
      ">up", "Roof", "On the roof.", "",
      ">"
    };

    await RunToEnd(engine, lines, lines.Length);

    // up already leads to the chimney: automap asks rather than quietly adding a second way up
    prompts.AskedAbout.ShouldBe("In the Chimney");
  }

  [Test]
  public async Task Pause_StopsBeforeTheNextMove()
  {
    var path = Path.GetTempFileName();
    File.WriteAllLines(path, new[] { ">look", "Hall", "A grand hall.", "" });
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    try
    {
      engine.Pause();
      engine.MoveByMove.ShouldBeTrue();
      var run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      // a run starts as its settings say
      await WaitFor(() => engine.ScreenLines().Count == 4);
      engine.MoveByMove.ShouldBeFalse();

      engine.Pause();
      File.AppendAllLines(path, new[] { ">north", "Kitchen", "A small kitchen.", "", ">" });
      await WaitFor(() => engine.NextCommand == "north");
      Rooms().ShouldBe(new[] { "Hall" });

      engine.Stop();
      await run;
    }
    finally
    {
      engine.Stop();
      File.Delete(path);
    }
  }
}
