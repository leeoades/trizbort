using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests.Editor;

[TestFixture]
[Category("Unit")]
public class NewAreaTests : IsolatedProjectTests
{
  private static Room Add(string name, float x, float y)
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y) };
    Project.Current.Elements.Add(room);
    return room;
  }

  [Test]
  public void StartingANewAreaAtTheFoyer_MovesItClearOfTheMap_AndSplitsItsJoins()
  {
    var garden = Add("Garden", -400, 0);
    var porch = Add("Front Porch", 0, 0);
    var foyer = Add("Foyer", 0, -200);
    var canvas = new AutomapCanvas(Project.Current);
    canvas.Connect(garden, MappableDirection.East, porch, true);
    canvas.Connect(porch, MappableDirection.North, foyer, true);

    var split = NewArea.Start(Project.Current, foyer);

    split.Count.ShouldBe(1);
    var join = split[0];
    join.Split.ShouldBeTrue();
    join.IsDrawnSplit.ShouldBeTrue();
    join.GetSourceRoom().ShouldBe(porch);
    join.GetTargetRoom().ShouldBe(foyer);
    foyer.Position.X.ShouldBeGreaterThan(porch.Position.X + porch.Width * 2);
    Project.Current.Elements.OfType<Room>().Count().ShouldBe(3);
    // the garden's join is left alone
    Project.Current.Elements.OfType<Connection>().Single(c => c != join).Split.ShouldBeFalse();
  }

  [Test]
  public void ASplitJoin_IsDrawnAsAStubOutOfEachRoom()
  {
    var porch = Add("Front Porch", 0, 0);
    var foyer = Add("Foyer", 2000, -200);
    new AutomapCanvas(Project.Current).Connect(porch, MappableDirection.North, foyer, true);
    var join = Project.Current.Elements.OfType<Connection>().Single();
    join.Split = true;

    var segments = join.GetSegments();

    segments.Count.ShouldBe(2);
    segments[0].Start.ShouldBe(join.VertexList[0].Position);
    segments[0].Length.ShouldBe(Connection.SplitStubLength, 0.01f);
    segments[0].End.Y.ShouldBeLessThan(segments[0].Start.Y); // out of the porch's north side
    segments[1].End.ShouldBe(join.VertexList[^1].Position);
    segments[1].Length.ShouldBe(Connection.SplitStubLength, 0.01f);
    join.FarRoom(true).ShouldBe(foyer);
    join.FarRoom(false).ShouldBe(porch);
    // nowhere near the middle of the old line
    join.Distance(new Vector(1000, -100), false).ShouldBeGreaterThan(100);
  }

  /// <summary>Garden - Porch - Foyer, with the house beyond the foyer: Living Room - Dining Room, Living Room - Study.</summary>
  private static (Room Garden, Room Porch, Room Foyer, Room Living, Room Dining, Room Study) House()
  {
    var garden = Add("Garden", -400, 0);
    var porch = Add("Front Porch", 0, 0);
    var foyer = Add("Foyer", 0, -200);
    var living = Add("Living Room", -200, -200);
    var dining = Add("Dining Room", -200, -400);
    var study = Add("Study", -400, -200);
    var canvas = new AutomapCanvas(Project.Current);
    canvas.Connect(garden, MappableDirection.East, porch, true);
    canvas.Connect(porch, MappableDirection.North, foyer, true);
    canvas.Connect(foyer, MappableDirection.West, living, true);
    canvas.Connect(living, MappableDirection.North, dining, true);
    canvas.Connect(living, MappableDirection.West, study, true);
    garden.IsStartRoom = true;
    return (garden, porch, foyer, living, dining, study);
  }

  private static Connection JoinOf(Room a, Room b) =>
    Project.Current.Elements.OfType<Connection>().Single(join =>
      (join.GetSourceRoom() == a && join.GetTargetRoom() == b) || (join.GetSourceRoom() == b && join.GetTargetRoom() == a));

  [Test]
  public void TheHouseBeyondTheFoyer_IsABranch_TheGardenSideIsnt()
  {
    var (_, _, foyer, living, dining, study) = House();

    var branches = NewArea.Branches(Project.Current, new[] { foyer });

    var branch = branches.ShouldHaveSingleItem();
    branch.Via.ShouldBe(foyer);
    branch.First.ShouldBe(living);
    branch.Rooms.ShouldBe(new[] { living, dining, study }, ignoreOrder: true);
    branch.Description.ShouldBe("Living Room and 2 more rooms");
  }

  [Test]
  public void AJoinBackAnotherWay_MeansNoBranch()
  {
    var (garden, _, foyer, _, _, study) = House();
    new AutomapCanvas(Project.Current).Connect(study, MappableDirection.South, garden, true);

    NewArea.Branches(Project.Current, new[] { foyer }).ShouldBeEmpty();
  }

  [Test]
  public void MovingSeveralRooms_KeepsTheirLayout_AndSplitsOnlyTheJoinsToRoomsLeftBehind()
  {
    var (_, porch, foyer, living, dining, study) = House();
    var before = new[] { foyer, living, dining, study }.Select(room => room.Position).ToList();

    var split = NewArea.Start(Project.Current, new[] { foyer, living, dining, study });

    split.ShouldHaveSingleItem().ShouldBe(JoinOf(porch, foyer));
    var moved = new[] { foyer, living, dining, study }.Select(room => room.Position).ToList();
    var delta = moved[0] - before[0];
    delta.X.ShouldBeGreaterThan(0);
    for (var index = 0; index < moved.Count; index++) (moved[index] - before[index]).ShouldBe(delta);
    JoinOf(foyer, living).Split.ShouldBeFalse();
    JoinOf(living, dining).Split.ShouldBeFalse();
    study.Position.X.ShouldBeGreaterThan(porch.Position.X + porch.Width);
  }

  [Test]
  public void BringingARoomAcrossASplitJoin_PutsItBesideTheOtherEnd_AndJoinsThemWhole()
  {
    var (_, _, foyer, living, dining, study) = House();
    NewArea.Start(Project.Current, foyer);
    var join = JoinOf(foyer, living);
    join.Split.ShouldBeTrue();

    NewArea.Bring(join, living, new[] { study });

    join.Split.ShouldBeFalse();
    // the join leaves the foyer on its west side: the living room goes west of it
    (living.Position.X + living.Width).ShouldBeLessThan(foyer.Position.X);
    living.Position.Y.ShouldBe(foyer.Position.Y);
    // the study came too, still west of the living room; the dining room was left behind
    (study.Position.X + study.Width).ShouldBeLessThan(living.Position.X);
    JoinOf(living, study).Split.ShouldBeFalse();
    JoinOf(living, dining).Split.ShouldBeTrue();
  }

  [Test]
  public void ACopy_OrARoomWithNoJoins_CantStartAnArea()
  {
    var porch = Add("Front Porch", 0, 0);
    NewArea.CanStart(porch).ShouldBeFalse();
    var copy = Add("x", 300, 0);
    copy.AddReferenceRoom(porch.Id);
    NewArea.CanStart(copy).ShouldBeFalse();
  }
}
