using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

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
