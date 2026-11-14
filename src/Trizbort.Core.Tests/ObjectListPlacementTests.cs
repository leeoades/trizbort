using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

/// <summary>Where a room's object list goes: placed by hand, or automatic and out of the way.</summary>
[TestFixture]
[Category("Unit")]
public class ObjectListPlacementTests : IsolatedProjectTests
{
  private static Rect RoomBounds => new(0, 0, 96, 64);

  [Test]
  public void AClearDefault_IsKept()
  {
    var spot = ObjectListPlacement.Choose(RoomBounds, 40, 20, new ObjectListSpot(CompassPoint.South, false),
      new List<LineSegment>(), new List<Rect>());

    spot.ShouldBe(new ObjectListSpot(CompassPoint.South, false));
  }

  [Test]
  public void AJoinAcrossTheDefault_MovesTheListToTheFirstClearSide()
  {
    // a join out of the bottom of the room runs down through where the list would go
    var down = new LineSegment(new Vector(80, 64), new Vector(80, 300));

    var spot = ObjectListPlacement.Choose(RoomBounds, 120, 20, new ObjectListSpot(CompassPoint.South, false),
      new List<LineSegment> { down }, new List<Rect>());

    spot.ShouldBe(new ObjectListSpot(CompassPoint.North, false));
  }

  [Test]
  public void InsideTheRoom_SitsAtTheBottomLeft()
  {
    var bounds = ObjectListPlacement.Bounds(RoomBounds, new ObjectListSpot(CompassPoint.South, true), 40, 20);

    bounds.Left.ShouldBeGreaterThan(RoomBounds.Left);
    bounds.Bottom.ShouldBeLessThanOrEqualTo(RoomBounds.Bottom);
  }

  [Test]
  public void PlacedByHand_IsSaved_AnOldMapsOwnSpotCountsAsPlaced_AndResetGoesBackToAutomatic()
  {
    var room = new Room(Project.Current) { Name = "Closet", Objects = "skis" };
    Project.Current.Elements.Add(room);
    room.ObjectsPlaced = true;
    room.ObjectsInside = true;
    room.ObjectsCustomPosition = true;
    room.ObjectsCustomPositionRight = 12;
    room.ObjectsCustomPositionDown = -3;
    var plain = new Room(Project.Current) { Name = "Hall", Objects = "lamp" };
    Project.Current.Elements.Add(plain);

    new LegacyMapFileEngine(Project.Current).Save(Files.File("objects.trizbort")).ShouldBeTrue();
    using var loaded = new Project();
    PersistenceRegressionTests.Load(loaded, Files.File("objects.trizbort"));

    var again = loaded.Elements.OfType<Room>().Single(r => r.Name == "Closet");
    again.ObjectsPlaced.ShouldBeTrue();
    again.ObjectsInside.ShouldBeTrue();
    again.ObjectsCustomPositionRight.ShouldBe(12);
    again.ObjectsCustomPositionDown.ShouldBe(-3);
    loaded.Elements.OfType<Room>().Single(r => r.Name == "Hall").ObjectsPlaced.ShouldBeFalse();

    again.ResetObjectListPosition();
    again.ObjectsPlaced.ShouldBeFalse();
    again.ObjectsInside.ShouldBeFalse();
    again.ObjectsCustomPosition.ShouldBeFalse();
  }

  [Test]
  public void TheDefaultSpot_IsAMapSetting_SavedWithTheMap()
  {
    Settings.DefaultObjectsPosition = CompassPoint.East;
    Settings.DefaultObjectsInside = false;
    new LegacyMapFileEngine(Project.Current).Save(Files.File("default-objects.trizbort")).ShouldBeTrue();
    Settings.Reset();
    Settings.DefaultObjectsPosition.ShouldBe(CompassPoint.South);

    using var loaded = new Project();
    PersistenceRegressionTests.Load(loaded, Files.File("default-objects.trizbort"));

    Settings.DefaultObjectsPosition.ShouldBe(CompassPoint.East);
    Settings.Reset();
  }
}
