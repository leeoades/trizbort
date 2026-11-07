using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests;

/// <summary>
///   I-053: several directions leading to the same room are listed on one join, compass directions decide where the
///   join and the room go, and new rooms go somewhere clear instead of on top of a join.
/// </summary>
[TestFixture]
[Category("Unit")]
public class AutomapJoinDirectionsTests : IsolatedProjectTests
{
  private static Room AddRoomAt(string name, float x, float y)
  {
    var room = ProjectRegressionTests.AddRoom(name);
    room.Position = new Vector(x, y);
    return room;
  }

  private static Connection TheJoin() =>
    Project.Current.Elements.OfType<Connection>().ShouldHaveSingleItem();

  private static CompassPoint StartSide(Connection connection)
  {
    connection.GetSourceRoom(out var point);
    return point;
  }

  private static CompassPoint EndSide(Connection connection)
  {
    connection.GetTargetRoom(out var point);
    return point;
  }

  [Test]
  public void DownThenNorth_MovesTheRoomNorthAndLabelsBothDirections()
  {
    var canvas = new AutomapCanvas();
    var cliff = AddRoomAt("Cliff Top", 0, 0);
    var beach = canvas.CreateRoom(cliff, MappableDirection.Down, "Beach", "Beach");
    canvas.Connect(cliff, MappableDirection.Down, beach, true);
    beach.InnerBounds.Center.Y.ShouldBeGreaterThan(cliff.InnerBounds.Center.Y);

    canvas.Connect(cliff, MappableDirection.North, beach, true);

    var join = TheJoin();
    join.StartText.ShouldBe("north / down");
    join.EndText.ShouldBe("south / up");
    StartSide(join).ShouldBe(CompassPoint.North);
    EndSide(join).ShouldBe(CompassPoint.South);
    beach.InnerBounds.Center.Y.ShouldBeLessThan(cliff.InnerBounds.Center.Y);
    beach.InnerBounds.Center.X.ShouldBe(cliff.InnerBounds.Center.X);
  }

  [Test]
  public void NorthThenDown_KeepsTheRoomAndLabelsBothDirections()
  {
    var canvas = new AutomapCanvas();
    var cliff = AddRoomAt("Cliff Top", 0, 0);
    var beach = canvas.CreateRoom(cliff, MappableDirection.North, "Beach", "Beach");
    canvas.Connect(cliff, MappableDirection.North, beach, true);
    var position = beach.Position;

    canvas.Connect(cliff, MappableDirection.Down, beach, true);

    var join = TheJoin();
    join.StartText.ShouldBe("north / down");
    join.EndText.ShouldBe("south / up");
    StartSide(join).ShouldBe(CompassPoint.North);
    beach.Position.ShouldBe(position);
  }

  [Test]
  public void AnotherWayBackFromAKnownEnd_IsAddedAtBothEnds()
  {
    var canvas = new AutomapCanvas();
    var hall = AddRoomAt("Hall", 0, 0);
    var garden = canvas.CreateRoom(hall, MappableDirection.East, "Garden", "Garden");
    canvas.Connect(hall, MappableDirection.East, garden, true);
    canvas.Connect(garden, MappableDirection.West, hall, true);

    canvas.Connect(garden, MappableDirection.Out, hall, true);

    var join = TheJoin();
    join.StartText.ShouldBe("east / in");
    join.EndText.ShouldBe("west / out");
  }

  [Test]
  public void OneWay_LabelsOnlyTheEndTravelledFrom()
  {
    var canvas = new AutomapCanvas();
    var cliff = AddRoomAt("Cliff Top", 0, 0);
    var beach = canvas.CreateRoom(cliff, MappableDirection.Down, "Beach", "Beach");
    canvas.Connect(cliff, MappableDirection.Down, beach, false);

    canvas.Connect(cliff, MappableDirection.North, beach, false);

    var join = TheJoin();
    join.Flow.ShouldBe(ConnectionFlow.OneWay);
    join.StartText.ShouldBe("north / down");
    join.EndText.ShouldBeNullOrEmpty();
    beach.InnerBounds.Center.Y.ShouldBeLessThan(cliff.InnerBounds.Center.Y);
  }

  [Test]
  public void RoomJoinedElsewhere_StaysAndTheJoinKeepsItsSide()
  {
    var canvas = new AutomapCanvas();
    var cliff = AddRoomAt("Cliff Top", 0, 0);
    var beach = canvas.CreateRoom(cliff, MappableDirection.Down, "Beach", "Beach");
    canvas.Connect(cliff, MappableDirection.Down, beach, true);
    var cave = canvas.CreateRoom(beach, MappableDirection.East, "Cave", "Cave");
    canvas.Connect(beach, MappableDirection.East, cave, true);
    var field = canvas.CreateRoom(cliff, MappableDirection.West, "Field", "Field");
    canvas.Connect(cliff, MappableDirection.West, field, true);
    var position = beach.Position;
    var cliffPosition = cliff.Position;

    canvas.Connect(cliff, MappableDirection.North, beach, true);

    beach.Position.ShouldBe(position);
    cliff.Position.ShouldBe(cliffPosition);
    var join = Project.Current.Elements.OfType<Connection>().Single(c => c.GetTargetRoom() == beach);
    join.StartText.ShouldBe("north / down");
    StartSide(join).ShouldBe(CompassPoint.SouthSouthWest);
  }

  [Test]
  public void AssumedWayBack_IsReplacedByTheWayActuallyTravelled()
  {
    // a curved path: east from the junction, but south (not the assumed west) leads back
    var canvas = new AutomapCanvas();
    var junction = AddRoomAt("South Junction", 0, 0);
    var east = canvas.CreateRoom(junction, MappableDirection.East, "Southeast Junction", "Southeast Junction");
    canvas.Connect(junction, MappableDirection.East, east, true);

    canvas.Connect(east, MappableDirection.South, junction, true);

    var join = TheJoin();
    EndSide(join).ShouldBe(CompassPoint.South);
    join.StartText.ShouldBeNullOrEmpty();
    join.EndText.ShouldBeNullOrEmpty();
  }

  [Test]
  public void AssumedWayBack_IsStillReplacedAfterUndoingAnotherEdit()
  {
    // PR #26 review: undo rebuilds the map as a new Project, which used to forget which ends were travelled
    using var session = new Trizbort.Editor.EditorSession();
    var canvas = new AutomapCanvas(null, null, session);
    var junction = AddRoomAt("South Junction", 0, 0);
    var east = canvas.CreateRoom(junction, MappableDirection.East, "Southeast Junction", "Southeast Junction");
    session.Execute("Automap", () => canvas.Connect(junction, MappableDirection.East, east, true));
    session.Execute("Add room", () => AddRoomAt("Elsewhere", 0, 600));
    session.PerformUndo();

    canvas.Connect(canvas.ResolveRoom(east), MappableDirection.South, canvas.ResolveRoom(junction), true);

    var join = TheJoin();
    EndSide(join).ShouldBe(CompassPoint.South);
    join.StartText.ShouldBeNullOrEmpty();
    join.EndText.ShouldBeNullOrEmpty();
  }

  [Test]
  public void DottedLabelledConnection_IsStillAnOrdinaryExit()
  {
    // PR #26 review: a user's dotted connection labelled "requires key" was mistaken for a teleport join
    var canvas = new AutomapCanvas();
    var hall = AddRoomAt("Hall", 0, 0);
    var vault = canvas.CreateRoom(hall, MappableDirection.East, "Vault", "Vault");
    canvas.Connect(hall, MappableDirection.East, vault, true);
    TheJoin().Style = ConnectionStyle.Dashed;
    TheJoin().MidText = "requires key";

    canvas.Connect(hall, MappableDirection.East, vault, true);

    TheJoin().MidText.ShouldBe("requires key");
  }

  [Test]
  public void AbbreviatedDirections_LabelJoinsInShort()
  {
    var canvas = new AutomapCanvas { AbbreviateDirections = true };
    var cliff = AddRoomAt("Cliff Top", 0, 0);
    var beach = canvas.CreateRoom(cliff, MappableDirection.Down, "Beach", "Beach");
    canvas.Connect(cliff, MappableDirection.Down, beach, true);
    canvas.Connect(cliff, MappableDirection.North, beach, true);
    canvas.Connect(cliff, MappableDirection.In, beach, true);

    var join = TheJoin();
    join.StartText.ShouldBe("n / d / in");
    join.EndText.ShouldBe("s / u / out");
  }

  [Test]
  public void AbbreviatedDirections_DontDoubleUpWordsAlreadyWrittenInFull()
  {
    var canvas = new AutomapCanvas();
    var cliff = AddRoomAt("Cliff Top", 0, 0);
    var beach = canvas.CreateRoom(cliff, MappableDirection.Down, "Beach", "Beach");
    canvas.Connect(cliff, MappableDirection.Down, beach, true);
    canvas.Connect(cliff, MappableDirection.North, beach, true);

    canvas.AbbreviateDirections = true;
    canvas.Connect(cliff, MappableDirection.North, beach, true);
    canvas.Connect(cliff, MappableDirection.Down, beach, true);

    TheJoin().StartText.ShouldBe("north / down");
  }

  [Test]
  public void OneWayArrivalsFromTheSameSide_DontShareAPoint()
  {
    // Colossal Cave: west from the Valley and west from the Streambed both drop into the Forest, with no way back
    // (the Forest's own way east is still unexplored)
    var canvas = new AutomapCanvas();
    var forest = AddRoomAt("In Forest", 0, 0);
    var valley = AddRoomAt("In A Valley", 400, -40);
    var streambed = AddRoomAt("At Slit In Streambed", 400, 40);
    canvas.AddExitStub(forest, MappableDirection.East);
    canvas.Connect(valley, MappableDirection.West, forest, false);
    canvas.Connect(streambed, MappableDirection.West, forest, false);

    var arrivals = Project.Current.Elements.OfType<Connection>()
      .Where(join => join.GetSourceRoom() != forest)
      .Select(join =>
      {
        join.GetTargetRoom(out var point);
        return point;
      })
      .ToList();
    arrivals.Count.ShouldBe(2);
    arrivals.Distinct().Count().ShouldBe(2);
    arrivals.ShouldNotContain(CompassPoint.East);
  }

  [Test]
  public void AWayBackIntoTheSameRoom_IsALoop()
  {
    // "north" in the forest returns to the forest; so does "west"
    var canvas = new AutomapCanvas { AbbreviateDirections = true };
    var forest = AddRoomAt("Forest", 0, 0);
    canvas.ConnectLoop(forest, MappableDirection.North);
    canvas.ConnectLoop(forest, MappableDirection.North);
    canvas.ConnectLoop(forest, MappableDirection.West);

    var loop = TheJoin();
    loop.GetSourceRoom().ShouldBe(forest);
    loop.GetTargetRoom().ShouldBe(forest);
    StartSide(loop).ShouldBe(CompassPoint.North);
    EndSide(loop).ShouldBe(CompassPoint.NorthEast);
    loop.Flow.ShouldBe(ConnectionFlow.OneWay);
    loop.StartText.ShouldBe("n / w");
  }

  [Test]
  public void TwoCommandsToTheSameRoom_ShareOneJoin()
  {
    // Colossal Cave: "light lamp" and "turn on lamp" both lead from the darkness to the Debris Room
    var canvas = new AutomapCanvas();
    var dark = AddRoomAt("Darkness", 0, 0);
    var debris = AddRoomAt("Debris Room", 400, 0);
    canvas.ConnectByCommand(dark, debris, "light lamp");
    canvas.ConnectByCommand(dark, debris, "turn on lamp");
    canvas.ConnectByCommand(dark, debris, "Light lamp");

    var join = TheJoin();
    join.MidText.ShouldBe("light lamp / turn on lamp");
    join.AutomapCommand.ShouldBe("light lamp / turn on lamp");
    join.Flow.ShouldBe(ConnectionFlow.OneWay);
  }

  [Test]
  public void ACommandAlongADirection_JoinsTheDirectionsLabel()
  {
    var canvas = new AutomapCanvas();
    var hall = AddRoomAt("Hall", 0, 0);
    var garden = AddRoomAt("Garden", 400, 0);
    canvas.Connect(hall, MappableDirection.East, garden, true);
    canvas.ConnectByCommand(hall, garden, "climb through window");

    var join = TheJoin();
    join.MidText.ShouldBe("climb through window");
    join.AutomapCommand.ShouldBeEmpty();
    join.Style.ShouldBe(ConnectionStyle.Solid);
  }

  [Test]
  public void ADirectionFoundAfterACommand_TakesOverTheCommandsJoin()
  {
    var canvas = new AutomapCanvas();
    var dark = AddRoomAt("Darkness", 0, 0);
    var debris = AddRoomAt("Debris Room", 400, 0);
    var lobby = AddRoomAt("Lobby", 0, 400);
    canvas.ConnectByCommand(dark, debris, "light lamp");
    canvas.ConnectByCommand(lobby, debris, "xyzzy");

    canvas.Connect(dark, MappableDirection.East, debris, true);

    var joins = Project.Current.Elements.OfType<Connection>().ToList();
    joins.Count.ShouldBe(2);
    var east = joins.Single(join => join.GetSourceRoom() == dark);
    east.MidText.ShouldBe("light lamp");
    east.AutomapCommand.ShouldBeEmpty();
    east.StartText.ShouldBeEmpty();
    StartSide(east).ShouldBe(CompassPoint.East);
    joins.Single(join => join.GetSourceRoom() == lobby).AutomapCommand.ShouldBe("xyzzy");
  }

  [Test]
  public void TeleportJoin_IsMarkedAndTheMarkIsSaved()
  {
    var canvas = new AutomapCanvas();
    var building = AddRoomAt("Inside Building", 0, 0);
    var debris = AddRoomAt("Debris Room", 400, 0);
    canvas.ConnectByCommand(building, debris, "xyzzy");
    TheJoin().AutomapCommand.ShouldBe("xyzzy");

    var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "teleport-" + System.Guid.NewGuid().ToString("N") + ".trizbort");
    try
    {
      new MapSaver(Project.Current).SaveMap(path).ShouldBeTrue();
      using var reloaded = new Project();
      var previous = Project.Current;
      Project.Current = reloaded;
      try
      {
        new MapLoader(reloaded).LoadMap(path).ShouldBeTrue();
        reloaded.Elements.OfType<Connection>().ShouldHaveSingleItem().AutomapCommand.ShouldBe("xyzzy");
      }
      finally
      {
        Project.Current = previous;
      }
    }
    finally
    {
      System.IO.File.Delete(path);
    }
  }

  [Test]
  public void UpThenSouthBack_ReplacesTheAssumedDownInsteadOfAddingNorth()
  {
    // PR #26 review: the "down" automap wrote as the assumed way back counted as a direction someone had seen
    var canvas = new AutomapCanvas();
    var hall = AddRoomAt("Hall", 0, 0);
    var attic = canvas.CreateRoom(hall, MappableDirection.Up, "Attic", "Attic");
    canvas.Connect(hall, MappableDirection.Up, attic, true);

    canvas.Connect(attic, MappableDirection.South, hall, true);

    var join = TheJoin();
    join.StartText.ShouldBe("up");
    join.EndText.ShouldBeNullOrEmpty();
    EndSide(join).ShouldBe(CompassPoint.South);
  }

  [Test]
  public void UndoingAReturnJourney_UndoesKnowingThatEndToo()
  {
    // PR #26 review: undo put the guessed exit back but automap still thought that end had been travelled
    using var session = new Trizbort.Editor.EditorSession();
    var canvas = new AutomapCanvas(null, null, session);
    var a = AddRoomAt("A", 0, 0);
    var b = canvas.CreateRoom(a, MappableDirection.East, "B", "B");
    session.Execute("Automap: B", () => canvas.Connect(a, MappableDirection.East, b, true));
    session.Execute("Automap: A", () => canvas.Connect(b, MappableDirection.South, a, true));
    session.PerformUndo();

    canvas.Connect(canvas.ResolveRoom(b), MappableDirection.South, canvas.ResolveRoom(a), true);

    var join = TheJoin();
    EndSide(join).ShouldBe(CompassPoint.South);
    join.StartText.ShouldBeNullOrEmpty();
    join.EndText.ShouldBeNullOrEmpty();
  }

  [Test]
  public void TravelledDirections_AreKeptOnTheConnection()
  {
    var canvas = new AutomapCanvas();
    var cliff = AddRoomAt("Cliff Top", 0, 0);
    var beach = canvas.CreateRoom(cliff, MappableDirection.Down, "Beach", "Beach");
    canvas.Connect(cliff, MappableDirection.Down, beach, true);
    canvas.Connect(cliff, MappableDirection.North, beach, true);
    canvas.Connect(beach, MappableDirection.Up, cliff, true);

    var join = TheJoin();
    join.StartTravelled.ShouldBe("down north");
    join.EndTravelled.ShouldBe("up");
    join.Reverse();
    join.StartTravelled.ShouldBe("up");
    join.EndTravelled.ShouldBe("down north");
  }

  [Test]
  public void ResolveRoom_ANewRoomThatReusedADeletedRoomsId_IsNotThatRoom()
  {
    // PR #26 review: ids are reused, so a deleted room's id could lead automap to an unrelated room
    var canvas = new AutomapCanvas();
    var hall = AddRoomAt("Hall", 0, 0);
    var remembered = canvas.CreateRoom(hall, MappableDirection.East, "Cupboard", "Cupboard");
    Project.Current.Elements.Remove(remembered);
    var cupboard = AddRoomAt("Cupboard", 900, 0);
    cupboard.Id.ShouldBe(remembered.Id);
    cupboard.Name.ShouldBe(remembered.Name);

    canvas.ResolveRoom(remembered).ShouldBeNull();
    canvas.ResolveRoom(hall).ShouldBeSameAs(hall);
  }

  [Test]
  public void ResolveRoom_RestoredAfterUndoWithDifferentName_IsStillFound()
  {
    using var session = new EditorSession();
    var canvas = new AutomapCanvas(null, null, session);
    var hall = AddRoomAt("Hall", 0, 0);

    session.Execute("Rename room", () => hall.Name = "Great Hall");
    session.PerformUndo();

    var restored = Project.Current.Elements.OfType<Room>().ShouldHaveSingleItem();
    restored.ShouldNotBeSameAs(hall);
    restored.Id.ShouldBe(hall.Id);
    restored.Name.ShouldBe("Hall");
    canvas.ResolveRoom(hall).ShouldBeSameAs(restored);
  }

  [Test]
  public void ResolveRoom_ARenamedRoom_IsStillFound()
  {
    var canvas = new AutomapCanvas();
    var hall = AddRoomAt("Hall", 0, 0);
    hall.Name = "Great Hall";

    canvas.ResolveRoom(hall).ShouldBeSameAs(hall);
  }

  [Test]
  public void UpOnItsOwn_StillLabelledUpAndDown()
  {
    var canvas = new AutomapCanvas();
    var hall = AddRoomAt("Hall", 0, 0);
    var attic = canvas.CreateRoom(hall, MappableDirection.Up, "Attic", "Attic");
    canvas.Connect(hall, MappableDirection.Up, attic, true);

    var join = TheJoin();
    join.StartText.ShouldBe("up");
    join.EndText.ShouldBe("down");
  }

  [Test]
  public void TypedLabel_IsKept()
  {
    var canvas = new AutomapCanvas();
    var hall = AddRoomAt("Hall", 0, 0);
    var garden = canvas.CreateRoom(hall, MappableDirection.East, "Garden", "Garden");
    canvas.Connect(hall, MappableDirection.East, garden, true);
    TheJoin().StartText = "through the arch";

    canvas.Connect(hall, MappableDirection.In, garden, true);

    TheJoin().StartText.ShouldBe("east / through the arch / in");
  }

  [Test]
  public void NewRoomWhereARoomAlreadyIs_GoesSomewhereClearInsteadOfOnTheJoin()
  {
    // Hollywood Hijinx with two-way joins off: east from South Junction, then west somewhere new (the Patio)
    var canvas = new AutomapCanvas();
    var start = AddRoomAt("South Junction", 0, 0);
    var east = canvas.CreateRoom(start, MappableDirection.East, "Southeast Junction", "Southeast Junction");
    canvas.Connect(start, MappableDirection.East, east, false);

    var patio = canvas.CreateRoom(east, MappableDirection.West, "Patio", "Patio");

    patio.InnerBounds.IntersectsWith(start.InnerBounds).ShouldBeFalse();
    TheJoin().Intersects(patio.InnerBounds).ShouldBeFalse();
    patio.InnerBounds.Center.X.ShouldBeLessThan(east.InnerBounds.Center.X);
    start.Position.ShouldBe(new Vector(0, 0));
  }

  [Test]
  public void NewRoomInAClearSpot_GoesWhereItAlwaysDid()
  {
    var canvas = new AutomapCanvas();
    var hall = AddRoomAt("Hall", 0, 0);

    var garden = canvas.CreateRoom(hall, MappableDirection.East, "Garden", "Garden");

    garden.InnerBounds.Center.Y.ShouldBe(hall.InnerBounds.Center.Y);
    garden.InnerBounds.Left.ShouldBeGreaterThan(hall.InnerBounds.Right);
  }
}
