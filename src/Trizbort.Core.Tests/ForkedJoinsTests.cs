using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

namespace Trizbort.Core.Tests;

/// <summary>
///   Joins the same way out of the same side of a room, to different rooms, are drawn as one join out of the room
///   that forks near the rooms they lead to, with one set of labels. Still two joins in the map.
/// </summary>
[TestFixture]
[Category("Unit")]
public class ForkedJoinsTests : IsolatedProjectTests
{
  private static Room AddRoom(string name, int x, int y)
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y) };
    Project.Current.Elements.Add(room);
    return room;
  }

  private static Connection Join(Room from, CompassPoint fromSide, Room to, CompassPoint toSide, string startText = "")
  {
    var join = new Connection(Project.Current, new Vertex(from.PortAt(fromSide)), new Vertex(to.PortAt(toSide)))
    {
      StartText = startText
    };
    Project.Current.Elements.Add(join);
    return join;
  }

  private Room _porch = null!;
  private Room _junction = null!;
  private Room _garden = null!;

  [SetUp]
  public void Rooms()
  {
    _porch = AddRoom("Front Porch", 0, 0);
    _junction = AddRoom("South Junction", -160, 320);
    _garden = AddRoom("Garden", 160, 320);
  }

  [Test]
  public void TwoJoinsSouthOutOfTheSameSide_Fork_WithOneDrawingTheSharedPart()
  {
    var toJunction = Join(_porch, CompassPoint.South, _junction, CompassPoint.North, "s");
    var toGarden = Join(_porch, CompassPoint.South, _garden, CompassPoint.North, "s");

    var first = ForkedJoins.Find(toJunction);
    var second = ForkedJoins.Find(toGarden);

    first.ShouldNotBeNull();
    second.ShouldNotBeNull();
    first.AtStart.ShouldBeTrue();
    first.Point.ShouldBe(second.Point);
    new[] { first.DrawsTrunk, second.DrawsTrunk }.Count(draws => draws).ShouldBe(1);

    // nearer the rooms they lead to than the porch, between the two
    var foot = toJunction.VertexList[0].Position;
    var junctionFoot = toJunction.VertexList[1].Position;
    first.Point.Y.ShouldBeGreaterThan(foot.Y + (junctionFoot.Y - foot.Y) / 2);
    first.Point.X.ShouldBeGreaterThan(junctionFoot.X);
    first.Point.X.ShouldBeLessThan(toGarden.VertexList[1].Position.X);

    // drawn out of the porch, through the fork, to the junction
    var segments = toJunction.GetSegments();
    segments[0].Start.ShouldBe(foot);
    segments.Any(segment => segment.End == first.Point).ShouldBeTrue();
    segments[^1].End.ShouldBe(junctionFoot);
    segments.Count(segment => ForkedJoins.OnTrunk(first, segment)).ShouldBeGreaterThan(0);
  }

  [Test]
  public void AJoinIntoTheSharedRoom_ForksAtItsEnd()
  {
    var toJunction = Join(_porch, CompassPoint.South, _junction, CompassPoint.North);
    var fromGarden = Join(_garden, CompassPoint.North, _porch, CompassPoint.South);

    var fork = ForkedJoins.Find(fromGarden);

    fork.ShouldNotBeNull();
    fork.AtStart.ShouldBeFalse();
    var segments = fromGarden.GetSegments();
    segments[0].Start.ShouldBe(fromGarden.VertexList[0].Position);
    segments[^1].End.ShouldBe(fromGarden.VertexList[1].Position);
    ForkedJoins.Find(toJunction)!.Point.ShouldBe(fork.Point);
  }

  [Test]
  public void JoinsThatSayDifferentThingsAtTheRoom_AreDrawnAsTheyAre()
  {
    var toJunction = Join(_porch, CompassPoint.South, _junction, CompassPoint.North, "s");
    Join(_porch, CompassPoint.South, _garden, CompassPoint.North, "down");

    ForkedJoins.Find(toJunction).ShouldBeNull();
    toJunction.GetSegments()[^1].End.ShouldBe(toJunction.VertexList[1].Position);
  }

  [Test]
  public void JoinsOutOfDifferentSides_OrToTheSameRoom_AreDrawnAsTheyAre()
  {
    var south = Join(_porch, CompassPoint.South, _junction, CompassPoint.North);
    Join(_porch, CompassPoint.SouthEast, _garden, CompassPoint.North);
    ForkedJoins.Find(south).ShouldBeNull();

    Join(_porch, CompassPoint.South, _junction, CompassPoint.East);
    ForkedJoins.Find(south).ShouldBeNull();
  }

  [Test]
  public void AOneWayJoinAndATwoWayJoin_AreDrawnAsTheyAre()
  {
    var toJunction = Join(_porch, CompassPoint.South, _junction, CompassPoint.North);
    var toGarden = Join(_porch, CompassPoint.South, _garden, CompassPoint.North);
    toGarden.Flow = ConnectionFlow.OneWay;

    ForkedJoins.Find(toJunction).ShouldBeNull();
  }
}
