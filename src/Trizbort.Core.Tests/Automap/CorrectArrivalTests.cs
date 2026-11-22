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
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests.Automap;

/// <summary>
///   Lee, 21 Nov 2026: "a button for 'the last assumption was wrong'": automap took the maze room he came into for the
///   gold room already on the map; Not This Room asks which it is, or a new one, and moves the way in there.
/// </summary>
[TestFixture]
[Category("Unit")]
public class CorrectArrivalTests : IsolatedProjectTests
{
  private sealed class Answers : IEditorPrompts
  {
    public RoomDisambiguation Answer { get; set; } = RoomDisambiguation.CreateNew();
    public IReadOnlyList<Room> Offered { get; private set; }

    public Task<RoomDisambiguation> DisambiguateRoomAsync(string transcriptLine, string roomDescription,
      IReadOnlyList<Room> candidates, CancellationToken token = default)
    {
      Offered = candidates;
      return Task.FromResult(Answer);
    }

    public Task<bool> ConfirmAsync(string message, string title, CancellationToken token = default) =>
      Task.FromResult(true);
  }

  private static async Task WaitFor(Func<bool> condition, string what)
  {
    var clock = Stopwatch.StartNew();
    while (!condition())
    {
      if (clock.ElapsedMilliseconds > 10000) Assert.Fail("Timed out waiting for automap: " + what);
      await Task.Delay(10);
    }
  }

  [Test]
  public async Task NotThisRoom_ANewRoom_TakesTheWayIn()
  {
    const string Alike = "You are in a maze of twisty little passages, all alike.";
    var project = Project.Current;
    var hall = new Room(project) { Name = "Hall", Position = new Vector(0, 300) };
    hall.AddDescription("A grand hall.");
    var gold = new Room(project) { Name = "Maze", Position = new Vector(300, 0), Objects = "gold" };
    gold.AddDescription(Alike);
    project.Elements.Add(hall);
    project.Elements.Add(gold);

    var path = Path.GetTempFileName();
    File.WriteAllText(path, $">look\n\nHall\nA grand hall.\n\n>north\n\nMaze\n{Alike}\n\n>");
    var answers = new Answers();
    var engine = new Trizbort.Automap.Automap((_, _) => { }, answers);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    settings.AutoTidy = false;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(project, engine), settings);
      await WaitFor(() => engine.CurrentRoom == gold, "in the gold room");
      // the only Maze on the map: taken for it without asking
      engine.CanCorrectArrival.ShouldBeTrue();
      engine.ArrivalAssumption.ShouldBe("North from Hall isn't on the map yet. Automap took you to be in Maze, " +
                                        "already on the map, as it has the same name and description.");

      (await engine.CorrectArrivalAsync()).ShouldBeTrue();

      var other = engine.CurrentRoom;
      other.ShouldNotBe(gold);
      other.Name.ShouldBe("Maze");
      engine.CanCorrectArrival.ShouldBeFalse();
      project.Elements.OfType<Connection>().ShouldContain(join =>
        join.GetSourceRoom() == hall && join.GetTargetRoom() == other);
      project.Elements.OfType<Connection>().ShouldNotContain(join =>
        (join.GetSourceRoom() == hall && join.GetTargetRoom() == gold) ||
        (join.GetSourceRoom() == gold && join.GetTargetRoom() == hall));
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }

  [Test]
  public async Task NotThisRoom_ANewRoomThatWasTheSameRoomDescribedDifferently()
  {
    // Lee, 22 Nov 2026: automap made a new room, not asking (best guess), but it was the room on the map described
    // differently now; Not This Room asks again
    const string Before = "You are at the bottom of the western pit. A tiny plant murmurs for water.";
    const string After = "A twelve-foot beanstalk rises out of the western pit.";
    var project = Project.Current;
    var hall = new Room(project) { Name = "Hall", Position = new Vector(0, 0) };
    hall.AddDescription("A grand hall.");
    var pit = new Room(project) { Name = "In West Pit", Position = new Vector(0, 300) };
    pit.AddDescription(Before);
    project.Elements.Add(hall);
    project.Elements.Add(pit);

    var path = Path.GetTempFileName();
    File.WriteAllText(path, $">look\n\nHall\nA grand hall.\n\n>down\n\nIn West Pit\n{After}\n\n>");
    var answers = new Answers();
    var engine = new Trizbort.Automap.Automap((_, _) => { }, answers);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    settings.AutoTidy = false;
    settings.BestGuessWhichRoom = true;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(project, engine), settings);
      await WaitFor(() => engine.CurrentRoom != null && engine.CurrentRoom.Name == "In West Pit", "in the pit");
      var made = engine.CurrentRoom;
      made.ShouldNotBe(pit);
      engine.CanCorrectArrival.ShouldBeTrue();
      engine.ArrivalAssumption.ShouldBe("Down from Hall led somewhere new to the map, so automap added In West Pit. " +
                                        "Another room on the map has its name: it may be that one, described differently.");
      answers.Answer = RoomDisambiguation.Choose(pit);

      (await engine.CorrectArrivalAsync()).ShouldBeTrue();

      answers.Offered.ShouldBe(new[] { pit });
      engine.CurrentRoom.ShouldBe(pit);
      project.Elements.ShouldNotContain(made);
      pit.Descriptions.ShouldContain(After);
      project.Elements.OfType<Connection>().ShouldContain(join =>
        join.GetSourceRoom() == hall && join.GetTargetRoom() == pit);
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }

  [Test]
  public async Task FollowingTheMapsJoinInAMaze_IsAGuess_SaidWhy_AndPutRight()
  {
    // Lee, 22 Nov 2026: west from the coins room led to the food room, but the map had west lead back to the keys
    // room (east from keys came to coins, so west from coins was taken to go back), and automap followed it
    const string Alike = "You are in a maze of twisty little passages, all alike.";
    var project = Project.Current;
    var food = new Room(project) { Name = "Maze", Position = new Vector(0, 0), Objects = "food" };
    var keys = new Room(project) { Name = "Maze", Position = new Vector(300, 0), Objects = "keys" };
    var coins = new Room(project) { Name = "Maze", Position = new Vector(600, 0), Objects = "coins" };
    foreach (var room in new[] { food, keys, coins })
    {
      room.AddDescription(Alike);
      project.Elements.Add(room);
    }
    new AutomapCanvas(project).Connect(keys, MappableDirection.East, coins, true);

    // in the coins room (asked, and answered), then west
    var path = Path.GetTempFileName();
    File.WriteAllText(path, $">look\n\nMaze\n{Alike}\n\n>west\n\nMaze\n{Alike}\n\n>");
    var answers = new Answers { Answer = RoomDisambiguation.Choose(coins) };
    var engine = new Trizbort.Automap.Automap((_, _) => { }, answers);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    settings.AutoTidy = false;
    settings.VerboseTranscript = true;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(project, engine), settings);
      await WaitFor(() => engine.CurrentRoom == keys, "taken back to the keys room");

      engine.CanCorrectArrival.ShouldBeTrue();
      engine.ArrivalAssumption.ShouldBe("West from Maze (coins) leads to Maze (keys) on the map, so automap took you " +
                                        "there. Rooms called Maze look alike, so it may be another.");

      answers.Answer = RoomDisambiguation.Choose(food);
      (await engine.CorrectArrivalAsync()).ShouldBeTrue();
      engine.CurrentRoom.ShouldBe(food);
      project.Elements.OfType<Connection>().ShouldContain(join =>
        (join.GetSourceRoom() == coins && join.GetTargetRoom() == food) ||
        (join.GetSourceRoom() == food && join.GetTargetRoom() == coins));
      engine.CanCorrectArrival.ShouldBeFalse();
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }

  [Test]
  public async Task FollowingTheMapsJoinToARoomNoOtherIsCalled_IsNoGuess()
  {
    var project = Project.Current;
    var hall = new Room(project) { Name = "Hall", Position = new Vector(0, 0) };
    hall.AddDescription("A grand hall.");
    var cellar = new Room(project) { Name = "Cellar", Position = new Vector(0, 300) };
    cellar.AddDescription("A damp cellar.");
    project.Elements.Add(hall);
    project.Elements.Add(cellar);
    new AutomapCanvas(project).Connect(hall, MappableDirection.Down, cellar, true);

    var path = Path.GetTempFileName();
    File.WriteAllText(path, ">look\n\nHall\nA grand hall.\n\n>down\n\nCellar\nA damp cellar.\n\n>");
    var engine = new Trizbort.Automap.Automap((_, _) => { }, new Answers());
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    settings.AutoTidy = false;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(project, engine), settings);
      await WaitFor(() => engine.CurrentRoom == cellar, "in the cellar");
      engine.CanCorrectArrival.ShouldBeFalse();
      engine.ArrivalAssumption.ShouldBeNull();
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }
}
