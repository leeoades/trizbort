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
