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

/// <summary>Rooms that move (a lift, a car) and portals to another area of the map, as automap sees them.</summary>
[TestFixture]
[Category("Unit")]
public class AutomapMovingRoomTests : IsolatedProjectTests
{
  private static List<Room> AllRooms() => Project.Current.Elements.OfType<Room>().ToList();

  private static bool Joined(Room a, Room b) =>
    Project.Current.Elements.OfType<Connection>().Any(connection =>
      (connection.GetSourceRoom() == a && connection.GetTargetRoom() == b) ||
      (connection.GetSourceRoom() == b && connection.GetTargetRoom() == a));

  private static async Task WaitFor(Func<bool> condition)
  {
    var clock = Stopwatch.StartNew();
    while (!condition())
    {
      if (clock.ElapsedMilliseconds > 5000) Assert.Fail("Timed out waiting for automap.");
      await Task.Delay(10);
    }
  }

  private static async Task RunToEnd(Trizbort.Automap.Automap engine, string[] lines)
  {
    var path = Path.GetTempFileName();
    File.WriteAllText(path, string.Join("\n", lines));
    var settings = AutomapSettings.Default;
    settings.FileName = path;
    Task run = null;
    try
    {
      run = engine.Start(new AutomapCanvas(Project.Current, engine), settings);
      await WaitFor(() => engine.ScreenLines().Count >= lines.Length);
      await Task.Delay(200);
    }
    finally
    {
      engine.Stop();
      if (run != null) await run;
      File.Delete(path);
    }
  }

  private sealed class MovesPrompts : IEditorPrompts
  {
    public int Asked;

    public Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(Room existing, string newRoomName, CancellationToken token = default) =>
      Task.FromResult(AutomapSameDirectionResult.KeepRoom1);

    public Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(Room from, Room existing, string newRoomName, CancellationToken token = default)
    {
      Asked++;
      return Task.FromResult(AutomapSameDirectionResult.Moves);
    }

    public Task<RoomDisambiguation> DisambiguateRoomAsync(string transcriptLine, string roomDescription, IReadOnlyList<Room> candidates, CancellationToken token = default) =>
      Task.FromResult(RoomDisambiguation.Choose(candidates[0]));
  }

  [Test]
  public async Task TheClosetMoves_ItsStopDownstairsIsACopy_AndAutomapKnowsWhichStopYouAreAt()
  {
    // Hollywood Hijinx: the closet is a lift. Out of it led to the foyer; after pulling the peg it leads to the
    // cellar. Told the closet moves, automap shows it down there too, and asks nothing more as it goes up and down.
    var prompts = new MovesPrompts();
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
      ">enter closet", "Closet", "",
      ">pull peg", "The closet lurches and rises.", "",
      ">out", "Foyer", "",
      ">enter closet", "Closet", "",
      ">pull peg", "The closet lurches and sinks.", "",
      ">out", "Cellar", "",
      ">"
    };

    await RunToEnd(engine, lines);

    prompts.Asked.ShouldBe(1);
    var rooms = AllRooms();
    rooms.Select(room => room.Name).OrderBy(name => name).ShouldBe(new[] { "Cellar", "Closet", "Closet", "Foyer" });
    var closet = rooms.Single(room => room.Name == "Closet" && !room.IsReference);
    var stop = rooms.Single(room => room.IsStop);
    var cellar = rooms.Single(room => room.Name == "Cellar");
    var foyer = rooms.Single(room => room.Name == "Foyer");
    closet.Moves.ShouldBeTrue();
    stop.ReferenceRoom.ShouldBe(closet);
    Joined(closet, stop).ShouldBeTrue();
    Joined(stop, cellar).ShouldBeTrue();
    Joined(closet, foyer).ShouldBeTrue();
    Joined(closet, cellar).ShouldBeFalse();
    Joined(stop, foyer).ShouldBeFalse();
  }

  [Test]
  public async Task ThroughAPortal_AutomapGoesToTheRoomItStandsFor_AndJoinsNothingAcrossTheMap()
  {
    // the house mapped apart from the garden: the foyer moved to an area of its own, with portals
    var porch = new Room(Project.Current) { Name = "Front Porch", Position = new Vector(0, 0) };
    var foyer = new Room(Project.Current) { Name = "Foyer", Position = new Vector(0, -200) };
    Project.Current.Elements.Add(porch);
    Project.Current.Elements.Add(foyer);
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var canvas = new AutomapCanvas(Project.Current, engine);
    canvas.Connect(porch, MappableDirection.North, foyer, true);
    NewArea.Start(Project.Current, foyer).Count.ShouldBe(2);
    var connections = Project.Current.Elements.OfType<Connection>().Count();

    var lines = new[]
    {
      ">look", "Foyer", "An elegant foyer.", "",
      ">south", "Front Porch", "A creaky porch.", "",
      ">north", "Foyer", "",
      ">"
    };
    await RunToEnd(engine, lines);

    AllRooms().Count.ShouldBe(4);
    Joined(porch, foyer).ShouldBeFalse();
    Project.Current.Elements.OfType<Connection>().Count().ShouldBe(connections);
  }

  [Test]
  public async Task ANewRoomIsNeverOneOfItsCopies_ByName()
  {
    // a portal "Front Porch (ref)" isn't the front porch's variant, found by name
    var porch = new Room(Project.Current) { Name = "Front Porch", Position = new Vector(0, 0) };
    Project.Current.Elements.Add(porch);
    var portal = new Room(Project.Current) { Position = new Vector(600, 0) };
    Project.Current.Elements.Add(portal);
    portal.AddReferenceRoom(porch.Id);
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var lines = new[] { ">look", "Front Porch", "A creaky porch.", "", ">north", "Hall", "A hall.", "", ">" };

    await RunToEnd(engine, lines);

    var hall = AllRooms().Single(room => room.Name == "Hall");
    Joined(porch, hall).ShouldBeTrue();
    Joined(portal, hall).ShouldBeFalse();
  }
}
