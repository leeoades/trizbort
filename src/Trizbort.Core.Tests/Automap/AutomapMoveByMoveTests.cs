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

      engine.PlayNextMove();
      await WaitFor(() => engine.NextCommand == "north");
      Rooms().ShouldBe(new[] { "Hall" });
      engine.ScreenLines().Last().ShouldBe(">north");
      engine.ScreenLines(1).First().ShouldBe("Hall");

      engine.PlayNextMove();
      await WaitFor(() => engine.NextCommand == "south");
      Rooms().ShouldBe(new[] { "Hall", "Kitchen" });

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
