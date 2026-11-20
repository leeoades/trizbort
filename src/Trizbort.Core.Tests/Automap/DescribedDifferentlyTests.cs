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
using Trizbort.Setup;

namespace Trizbort.Core.Tests.Automap;

/// <summary>
///   Lee, 20 Nov 2026, Colossal Cave: the Twopit Room keeps its name but is described differently as the beanstalk
///   grows, and automap asks which room it is. Picking the room says it's the same room described differently now:
///   the new description is kept with it, so it isn't asked again, even while automap is only tracking.
/// </summary>
[TestFixture]
[Category("Unit")]
public class DescribedDifferentlyTests : IsolatedProjectTests
{
  private const string Before = "You are at the bottom of the western pit. A tiny plant murmurs for water.";
  private const string After = "A twelve-foot beanstalk rises out of the western pit. You are at its foot.";

  private sealed class PickTheRoom : IEditorPrompts
  {
    public int Asked { get; private set; }

    public Task<RoomDisambiguation> DisambiguateRoomAsync(string transcriptLine, string roomDescription,
      IReadOnlyList<Room> candidates, CancellationToken token = default)
    {
      Asked++;
      return Task.FromResult(RoomDisambiguation.Choose(candidates[0]));
    }

    public Task<bool> ConfirmAsync(string message, string title, CancellationToken token = default) =>
      Task.FromResult(true);
  }

  private static (Room Hall, Room Pit) Map()
  {
    var project = Project.Current;
    var hall = new Room(project) { Name = "Hall", Position = new Vector(0, 0) };
    hall.AddDescription("A grand hall.");
    var pit = new Room(project) { Name = "In West Pit", Position = new Vector(160, 0) };
    pit.AddDescription(Before);
    project.Elements.Add(hall);
    project.Elements.Add(pit);
    project.Elements.Add(new Connection(project, new Vertex(hall.PortAt(CompassPoint.East)),
      new Vertex(pit.PortAt(CompassPoint.West))));
    return (hall, pit);
  }

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
  public async Task Mapping_PickingTheRoom_KeepsItsNewDescription_SoItIsntAskedAgain()
  {
    var (_, pit) = Map();
    var path = Files.File("game.txt");
    File.WriteAllText(path,
      ">look\n\nHall\nA grand hall.\n\n" +
      $">e\n\nIn West Pit\n{After}\n\n" +
      ">w\n\nHall\nA grand hall.\n\n" +
      $">e\n\nIn West Pit\n{After}\n\n>");
    var settings = AutomapSettings.Default;
    settings.AutoTidy = false;
    settings.GuessExits = false;
    var prompts = new PickTheRoom();

    var result = await HeadlessAutomap.RunAsync(Project.Current, path, settings, prompts);

    result.Succeeded.ShouldBeTrue(result.Error);
    prompts.Asked.ShouldBe(1);
    pit.Descriptions.ShouldContain(After);
    Project.Current.Elements.OfType<Room>().Count().ShouldBe(2);
  }

  /// <summary>Lee: it didn't ask again until the map was saved and opened again; only the first description was saved.</summary>
  [Test]
  public async Task TheNewDescription_IsSaved_SoItIsntAskedAgainAfterTheMapIsOpenedAgain()
  {
    var (_, pit) = Map();
    var path = Files.File("game.txt");
    File.WriteAllText(path,
      ">look\n\nHall\nA grand hall.\n\n" +
      $">e\n\nIn West Pit\n{After}\n\n>");
    var settings = AutomapSettings.Default;
    settings.AutoTidy = false;
    settings.GuessExits = false;
    var prompts = new PickTheRoom();
    (await HeadlessAutomap.RunAsync(Project.Current, path, settings, prompts)).Succeeded.ShouldBeTrue();
    prompts.Asked.ShouldBe(1);

    new LegacyMapFileEngine(Project.Current).Save(Files.File("map.trizbort")).ShouldBeTrue();
    using var reopened = new Project();
    PersistenceRegressionTests.Load(reopened, Files.File("map.trizbort"));
    var reopenedPit = reopened.Elements.OfType<Room>().Single(room => room.Name == "In West Pit");
    reopenedPit.Descriptions.ShouldBe(new[] { Before, After });
    // and saved again, as it was
    new LegacyMapFileEngine(reopened).Save(Files.File("again.trizbort")).ShouldBeTrue();
    File.ReadAllText(Files.File("again.trizbort")).ShouldBe(File.ReadAllText(Files.File("map.trizbort")));

    var again = new PickTheRoom();
    (await HeadlessAutomap.RunAsync(reopened, path, settings, again)).Succeeded.ShouldBeTrue();
    again.Asked.ShouldBe(0);
    pit.ShouldNotBeNull();
  }

  [Test]
  public void TheRoomDialog_EditsTheFirstDescription_AndKeepsTheOthers()
  {
    var (_, pit) = Map();
    pit.AddDescription(After);

    pit.SetPrimaryDescription("You are at the bottom of the western pit.");

    pit.Descriptions.ShouldBe(new[] { "You are at the bottom of the western pit.", After });
    pit.SetPrimaryDescription(string.Empty);
    pit.Descriptions.ShouldBe(new[] { After });
  }

  [Test]
  public async Task TrackingOnly_PickingTheRoom_KeepsItsNewDescription_SoItIsntAskedAgain()
  {
    var (hall, pit) = Map();
    var path = Files.File("tracking.txt");
    File.WriteAllText(path, ">look\n\nHall\nA grand hall.\n\n>");
    var prompts = new PickTheRoom();
    var engine = new Trizbort.Automap.Automap((_, _) => { }, prompts);
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    settings.AutoTidy = false;
    settings.GuessExits = false;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      await WaitFor(() => engine.CurrentRoom == hall);
      engine.Suspend();

      foreach (var (move, room) in new[] { ("e", pit), ("w", hall), ("e", pit) })
      {
        var lines = engine.ScreenLines().Count;
        File.AppendAllText(path, room == pit
          ? $"{move}\n\nIn West Pit\n{After}\n\n>"
          : $"{move}\n\nHall\nA grand hall.\n\n>");
        await WaitFor(() => engine.ScreenLines().Count > lines + 3);
        await Task.Delay(100);
        engine.CurrentRoom.ShouldBe(room, $"after {move}");
      }

      prompts.Asked.ShouldBe(1);
      pit.Descriptions.ShouldContain(After);
      Project.Current.Elements.OfType<Room>().Count().ShouldBe(2);
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
    }
  }
}
