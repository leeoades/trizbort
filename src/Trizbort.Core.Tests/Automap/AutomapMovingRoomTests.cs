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

    /// <summary>What "what moved it?" listed each time, and what it suggested.</summary>
    public readonly List<(List<string> Done, int Suggested)> WhatMoved = new();

    /// <summary>The answer to "what moved it?"; null takes the suggestion.</summary>
    public WhatMovedItChoice Answer;

    public Task<WhatMovedItChoice> ChooseWhatMovedItAsync(Room room, string comeOutIn, IReadOnlyList<DoneInRoom> done,
      int suggested, CancellationToken token = default)
    {
      WhatMoved.Add((done.Select(item => item.Display).ToList(), suggested));
      return Task.FromResult(Answer ?? (suggested >= 0 ? new WhatMovedItChoice(done[suggested].Label) : WhatMovedItChoice.Itself));
    }
  }

  [Test]
  public async Task NothingThePlayerDid_MovedTheBus_TheJoinSaysSo_AndReadingAgainDoesntMoveYou()
  {
    // reading on the bus doesn't move it: the user says nothing they did moved it
    var prompts = new MovesPrompts { Answer = WhatMovedItChoice.Itself };
    var engine = new Trizbort.Automap.Automap((_, _) => { }, prompts);
    var lines = new[]
    {
      ">look", "High Street", "A busy street.", "",
      ">enter bus", "Bus", "A rattling bus.", "",
      ">out", "High Street", "",
      ">enter bus", "Bus", "",
      ">read book", "You read a chapter.", "",
      ">out", "Market", "A noisy market.", "",
      ">enter bus", "Bus", "",
      ">read book", "You read another chapter.", "",
      ">"
    };

    await RunToEnd(engine, lines);

    prompts.WhatMoved.Count.ShouldBe(1);
    prompts.WhatMoved[0].Done.ShouldBe(new[] { "read book" });
    var bus = AllRooms().Single(room => room.Name == "Bus" && !room.IsReference);
    var stop = AllRooms().Single(room => room.IsStop);
    Project.Current.Elements.OfType<Connection>()
      .Single(connection => connection.GetSourceRoom() == bus && connection.GetTargetRoom() == stop)
      .MidText.ShouldBe(Trizbort.Automap.Automap.ByItself);
    engine.CurrentRoom.ShouldBe(stop);
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

  [TestCase(false)]
  [TestCase(true)]
  public async Task WhatMovedTheCloset_IsThePegPulled_NotTheDoorOpenedAfter_AndPullingItAgainMovesYouToo(bool backUp)
  {
    // Hollywood Hijinx: the door swings shut as the closet moves, so it's opened before going out. The join to the
    // stop is labelled with the peg pulled (down, and a join back up), and pulling it later moves the location.
    var prompts = new MovesPrompts();
    var engine = new Trizbort.Automap.Automap((_, _) => { }, prompts);
    var lines = new List<string>
    {
      ">look", "Cellar", "A dank cellar.", "",
      ">up", "Foyer", "An elegant foyer.", "",
      ">enter closet", "Closet", "A coat closet with pegs.", "",
      ">out", "Foyer", "",
      ">enter closet", "Closet", "",
      ">pull first peg", "The closet moves down, then stops.", "",
      ">out", "The closet door is closed.", "",
      ">open closet door", "The closet door squeals as it opens.", "",
      ">out", "Cellar", "",
      ">enter closet", "Closet", "",
      ">look", "Closet", "A coat closet with pegs.", "",
      ">pull second peg", "The closet moves up, then stops.", "",
      ">out", "The closet door is closed.", "",
      ">open door", "The closet door whinnies as it opens.", "",
      ">out", "Foyer", "",
      ">in", "Closet", "",
      ">see skis", "They're a large pair of downhill snow skis.", "",
      ">pull first peg", "The closet moves down, then stops.", ""
    };
    if (backUp)
      lines.AddRange(new[]
      {
        ">pull peg", "[Which peg do you mean, the first coat peg, the second coat peg, the third coat",
        "peg, or the sawed-off peg?]", "",
        ">second", "The closet moves up, then stops.", ""
      });
    lines.Add(">");

    await RunToEnd(engine, lines.ToArray());

    prompts.Asked.ShouldBe(1);
    // asked once going down, and once coming back up; never again
    prompts.WhatMoved.Count.ShouldBe(2);
    prompts.WhatMoved[0].Done.ShouldBe(new[] { "pull first peg", "open closet door" });
    prompts.WhatMoved[0].Suggested.ShouldBe(0);
    prompts.WhatMoved[1].Done.ShouldBe(new[] { "pull second peg", "open door" });
    prompts.WhatMoved[1].Suggested.ShouldBe(0);
    var closet = AllRooms().Single(room => room.Name == "Closet" && !room.IsReference);
    var stop = AllRooms().Single(room => room.IsStop);
    Connection From(Room a, Room b) => Project.Current.Elements.OfType<Connection>()
      .Single(connection => connection.GetSourceRoom() == a && connection.GetTargetRoom() == b);
    From(closet, stop).MidText.ShouldBe("pull first peg");
    From(stop, closet).MidText.ShouldBe("pull second peg");
    engine.CurrentRoom.ShouldBe(backUp ? closet : stop);
  }

  [Test]
  public async Task AfterANewArea_AutomapGoesAlongTheSplitJoin_AndAddsNothing()
  {
    // the house mapped apart from the garden: the foyer moved to an area of its own, its join to the porch split
    var porch = new Room(Project.Current) { Name = "Front Porch", Position = new Vector(0, 0) };
    var foyer = new Room(Project.Current) { Name = "Foyer", Position = new Vector(0, -200) };
    Project.Current.Elements.Add(porch);
    Project.Current.Elements.Add(foyer);
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var canvas = new AutomapCanvas(Project.Current, engine);
    canvas.Connect(porch, MappableDirection.North, foyer, true);
    NewArea.Start(Project.Current, foyer).Count.ShouldBe(1);
    var connections = Project.Current.Elements.OfType<Connection>().Count();

    var lines = new[]
    {
      ">look", "Foyer", "An elegant foyer.", "",
      ">south", "Front Porch", "A creaky porch.", "",
      ">north", "Foyer", "",
      ">"
    };
    await RunToEnd(engine, lines);

    AllRooms().Count.ShouldBe(2);
    Project.Current.Elements.OfType<Connection>().Count().ShouldBe(connections);
    Project.Current.Elements.OfType<Connection>().Single().Split.ShouldBeTrue();
  }

  [Test]
  public async Task ThroughAReferenceRoomMadeByHand_AutomapGoesToTheRoomItStandsFor()
  {
    // "To Front Porch" drawn south of the foyer, the real porch far away
    var porch = Add("Front Porch", 0, 0, "A creaky porch.");
    var foyer = Add("Foyer", 2000, -200, "An elegant foyer.");
    var portal = Add("x", 2000, 0);
    portal.AddReferenceRoom(porch.Id);
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    new AutomapCanvas(Project.Current, engine).Connect(foyer, MappableDirection.South, portal, true);
    var lines = new[]
    {
      ">look", "Foyer", "An elegant foyer.", "",
      ">south", "Front Porch", "A creaky porch.", "",
      ">east", "Garden", "A walled garden.", "",
      ">"
    };

    await RunToEnd(engine, lines);

    var garden = AllRooms().Single(room => room.Name == "Garden");
    Joined(porch, garden).ShouldBeTrue();
    Joined(porch, foyer).ShouldBeFalse();
  }

  private static Room Add(string name, float x, float y, string description = null)
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y) };
    if (description != null) room.AddDescription(description);
    Project.Current.Elements.Add(room);
    return room;
  }

  private static void Join(Room from, CompassPoint side, Room to, string command = null)
  {
    var join = new Connection(Project.Current, new Vertex(from.PortAt(side)), new Vertex(to.PortAt(CompassPoint.South)))
    {
      Flow = ConnectionFlow.OneWay,
      MidText = command ?? string.Empty
    };
    Project.Current.Elements.Add(join);
  }

  [Test]
  public async Task OneCommand_LeadingToTwoRooms_TheHeadingSaysWhich()
  {
    // Hollywood Hijinx: from the cellar, "enter closet" leads to the shaft, or, once the closet has come down, into it
    var cellar = Add("Cellar", 0, 0, "A dank cellar.");
    var shaft = Add("Shaft", -200, -200, "A dark shaft under the closet.");
    var closet = Add("Closet", 200, -200, "A coat closet with pegs.");
    Join(cellar, CompassPoint.NorthWest, shaft, "enter closet");
    Join(cellar, CompassPoint.NorthEast, closet, "enter closet");
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var lines = new[]
    {
      ">look", "Cellar", "A dank cellar.", "",
      ">enter closet", "Closet", "A coat closet with pegs.", "",
      ">north", "Coat Rack", "A rack of coats.", "",
      ">"
    };

    await RunToEnd(engine, lines);

    var rack = AllRooms().Single(room => room.Name == "Coat Rack");
    Joined(closet, rack).ShouldBeTrue();
    Joined(shaft, rack).ShouldBeFalse();
    Joined(cellar, rack).ShouldBeFalse();
  }

  [Test]
  public async Task OneDirection_LeadingToTwoRoomsWithOneName_TheDescriptionSaysWhich()
  {
    var cellar = Add("Cellar", 0, 0, "A dank cellar.");
    var first = Add("Shaft", -200, -200, "A dark shaft under the closet.");
    var second = Add("Shaft", 200, -200, "A shaft, the closet's floor just above you.");
    Join(cellar, CompassPoint.North, first);
    Join(cellar, CompassPoint.North, second);
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var lines = new[]
    {
      ">look", "Cellar", "A dank cellar.", "",
      ">north", "Shaft", "A shaft, the closet's floor just above you.", "",
      ">east", "Ledge", "A narrow ledge.", "",
      ">"
    };

    await RunToEnd(engine, lines);

    var ledge = AllRooms().Single(room => room.Name == "Ledge");
    Joined(second, ledge).ShouldBeTrue();
    Joined(first, ledge).ShouldBeFalse();
  }

  [Test]
  public async Task OneCommand_LeadingToTwoRooms_WithNoHeading_JoinsNothingUntilAHeadingSays()
  {
    var cellar = Add("Cellar", 0, 0, "A dank cellar.");
    var shaft = Add("Shaft", -200, -200, "A dark shaft under the closet.");
    var closet = Add("Closet", 200, -200, "A coat closet with pegs.");
    Join(cellar, CompassPoint.NorthWest, shaft, "enter closet");
    Join(cellar, CompassPoint.NorthEast, closet, "enter closet");
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var lines = new[]
    {
      ">look", "Cellar", "A dank cellar.", "",
      ">enter closet", "You squeeze in.", "",
      ">north", "Coat Rack", "A rack of coats.", "",
      ">"
    };

    await RunToEnd(engine, lines);

    var rack = AllRooms().Single(room => room.Name == "Coat Rack");
    rack.GetConnections().ShouldBeEmpty();
    rack.IsStartRoom.ShouldBeFalse();
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
