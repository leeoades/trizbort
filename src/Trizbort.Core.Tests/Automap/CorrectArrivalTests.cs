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
      engine.ArrivalAssumption.ShouldBe("You came to Maze, already on the map.");

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
}
