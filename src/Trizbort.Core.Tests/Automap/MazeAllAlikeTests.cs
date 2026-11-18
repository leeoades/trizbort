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
///   Lee, 18 Nov 2026, in Adventure's maze of twisty little passages, all alike: every room is "Maze" with the same
///   description, so only the map can say which one the player is in. Tracking only didn't follow the map at all,
///   and a room the map said nothing about was taken to be the first of them.
/// </summary>
[TestFixture]
[Category("Unit")]
public class MazeAllAlikeTests : IsolatedProjectTests
{
  private const string Alike = "You are in a maze of twisty little passages, all alike.";

  private static Room AddRoom(string name, int x, int y, string description)
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y) };
    room.AddDescription(description);
    Project.Current.Elements.Add(room);
    return room;
  }

  private static void Join(Room a, CompassPoint from, Room b, CompassPoint to) =>
    Project.Current.Elements.Add(new Connection(Project.Current, new Vertex(a.PortAt(from)), new Vertex(b.PortAt(to))));

  private static async Task WaitFor(Func<bool> condition)
  {
    var clock = Stopwatch.StartNew();
    while (!condition())
    {
      if (clock.ElapsedMilliseconds > 5000) Assert.Fail("Timed out waiting for automap.");
      await Task.Delay(10);
    }
  }

  private static string Move(string command, string room, string description) =>
    $"{command}\n\n{room}\n{description}\n\n>";

  private sealed class Asked : IEditorPrompts
  {
    public Func<IReadOnlyList<Room>, Room> Choose { get; init; } = _ => null;
    public List<IReadOnlyList<Room>> Questions { get; } = new();

    public Task<RoomDisambiguation> DisambiguateRoomAsync(string transcriptLine, string roomDescription,
      IReadOnlyList<Room> candidates, CancellationToken token = default)
    {
      Questions.Add(candidates);
      var chosen = Choose(candidates);
      return Task.FromResult(chosen == null ? RoomDisambiguation.CreateNew() : RoomDisambiguation.Choose(chosen));
    }

    public Task<bool> ConfirmAsync(string message, string title, CancellationToken token = default) =>
      Task.FromResult(true);
  }

  [Test]
  public async Task TrackingOnly_FollowsTheMap_RoundAMazeOfRoomsAllAlike()
  {
    var hall = AddRoom("Hall", 0, 0, "A grand hall.");
    var a = AddRoom("Maze", 0, 160, Alike);
    var b = AddRoom("Maze", 160, 160, Alike);
    var c = AddRoom("Maze", 160, 320, Alike);
    Join(hall, CompassPoint.South, a, CompassPoint.North);
    Join(a, CompassPoint.East, b, CompassPoint.West);
    Join(b, CompassPoint.South, c, CompassPoint.North);
    var path = Path.GetTempFileName();
    File.WriteAllText(path, ">look\n\nHall\nA grand hall.\n\n>");
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      await WaitFor(() => engine.CurrentRoom == hall);
      engine.Suspend();

      var expected = new (string Command, Room Room)[] { ("s", a), ("e", b), ("s", c), ("n", b), ("w", a), ("n", null) };
      foreach (var (command, room) in expected)
      {
        var lines = engine.ScreenLines().Count;
        File.AppendAllText(path, room == null
          ? Move(command, "Hall", "A grand hall.")
          : Move(command, "Maze", Alike));
        await WaitFor(() => engine.ScreenLines().Count > lines + 3);
        await Task.Delay(100);
        engine.CurrentRoom.ShouldBe(room ?? hall, $"after {command}");
      }

      Project.Current.Elements.OfType<Room>().Count().ShouldBe(4);
      Project.Current.Elements.OfType<Connection>().Count().ShouldBe(3);
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }

  [Test]
  public async Task AWayTheMapDoesntShow_IntoOneOfTheMaze_Asks_RatherThanJoiningTheFirst()
  {
    var first = AddRoom("Maze", 0, 160, Alike);
    var second = AddRoom("Maze", 160, 160, Alike);
    Join(first, CompassPoint.East, second, CompassPoint.West);
    // a dead end drawn by hand, not joined to anything yet
    var deadEnd = AddRoom("Dead End", 320, 160, "You have reached a dead end.");
    var asked = new Asked { Choose = candidates => candidates.Contains(second) ? second : null };
    var path = Path.GetTempFileName();
    File.WriteAllText(path, ">look\n\nDead End\nYou have reached a dead end.\n\n>" + Move("w", "Maze", Alike));
    var settings = AutomapSettings.Default;
    settings.GuessExits = false;
    settings.AutoTidy = false;

    await HeadlessAutomap.RunAsync(Project.Current, path, settings, asked);

    asked.Questions.ShouldHaveSingleItem().ShouldBe(new[] { first, second }, ignoreOrder: true);
    var joined = deadEnd.GetConnections().Select(join => join.GetSourceRoom() == deadEnd ? join.GetTargetRoom() : join.GetSourceRoom());
    joined.ShouldBe(new[] { second });
  }

  [Test]
  public async Task MakingABestGuess_AWayTheMapDoesntShow_IntoOneOfTheMaze_IsANewRoom()
  {
    var first = AddRoom("Maze", 0, 160, Alike);
    var second = AddRoom("Maze", 160, 160, Alike);
    Join(first, CompassPoint.East, second, CompassPoint.West);
    var deadEnd = AddRoom("Dead End", 320, 160, "You have reached a dead end.");
    var path = Path.GetTempFileName();
    File.WriteAllText(path, ">look\n\nDead End\nYou have reached a dead end.\n\n>" + Move("w", "Maze", Alike));
    var settings = AutomapSettings.Default;
    settings.GuessExits = false;
    settings.AutoTidy = false;
    settings.BestGuessWhichRoom = true;

    await HeadlessAutomap.RunAsync(Project.Current, path, settings, new Asked());

    var joined = deadEnd.GetConnections().Select(join => join.GetSourceRoom() == deadEnd ? join.GetTargetRoom() : join.GetSourceRoom()).ToList();
    joined.ShouldHaveSingleItem();
    joined[0].ShouldNotBe(first);
    joined[0].ShouldNotBe(second);
    Project.Current.Elements.OfType<Room>().Count(room => room.Name == "Maze").ShouldBe(3);
  }
}
