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
