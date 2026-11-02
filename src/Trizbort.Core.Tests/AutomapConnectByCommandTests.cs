using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

namespace Trizbort.Core.Tests;

/// <summary>I-052: rooms reached by a command that isn't a direction ("xyzzy") are joined, labelled with the command.</summary>
[TestFixture]
[Category("Unit")]
public class AutomapConnectByCommandTests : IsolatedProjectTests
{
  private static Room AddRoomAt(string name, float x, float y)
  {
    var room = ProjectRegressionTests.AddRoom(name);
    room.Position = new Vector(x, y);
    return room;
  }

  private static Connection[] Connections() => Project.Current.Elements.OfType<Connection>().ToArray();

  [Test]
  public void NewJoin_IsOneWayDottedAndLabelled()
  {
    var building = AddRoomAt("Inside Building", 0, 0);
    var debris = AddRoomAt("Debris Room", 400, 0);

    new AutomapCanvas().ConnectByCommand(building, debris, "xyzzy");

    var connection = Connections().ShouldHaveSingleItem();
    connection.MidText.ShouldBe("xyzzy");
    connection.Flow.ShouldBe(ConnectionFlow.OneWay);
    connection.Style.ShouldBe(ConnectionStyle.Dashed);
    connection.GetSourceRoom().ShouldBeSameAs(building);
    connection.GetTargetRoom().ShouldBeSameAs(debris);
  }

  [TestCase(400, 0, CompassPoint.East, CompassPoint.West)]
  [TestCase(-400, 0, CompassPoint.West, CompassPoint.East)]
  [TestCase(0, -400, CompassPoint.North, CompassPoint.South)]
  [TestCase(0, 400, CompassPoint.South, CompassPoint.North)]
  [TestCase(400, 400, CompassPoint.SouthEast, CompassPoint.NorthWest)]
  [TestCase(-400, -400, CompassPoint.NorthWest, CompassPoint.SouthEast)]
  public void Join_LeavesEachRoomOnTheSideFacingTheOther(
    float x, float y, CompassPoint fromSide, CompassPoint toSide)
  {
    var here = AddRoomAt("Here", 0, 0);
    var there = AddRoomAt("There", x, y);

    new AutomapCanvas().ConnectByCommand(here, there, "plugh");

    var connection = Connections().ShouldHaveSingleItem();
    connection.GetSourceRoom(out var sourcePoint).ShouldBeSameAs(here);
    connection.GetTargetRoom(out var targetPoint).ShouldBeSameAs(there);
    sourcePoint.ShouldBe(fromSide);
    targetPoint.ShouldBe(toSide);
  }

  [Test]
  public void SameCommandBack_MakesTheJoinTwoWay()
  {
    var building = AddRoomAt("Inside Building", 0, 0);
    var debris = AddRoomAt("Debris Room", 400, 0);
    var canvas = new AutomapCanvas();

    canvas.ConnectByCommand(building, debris, "xyzzy");
    canvas.ConnectByCommand(debris, building, "XYZZY");

    Connections().ShouldHaveSingleItem().Flow.ShouldBe(ConnectionFlow.TwoWay);
  }

  [Test]
  public void SameCommandSameWayAgain_AddsNothing()
  {
    var building = AddRoomAt("Inside Building", 0, 0);
    var debris = AddRoomAt("Debris Room", 400, 0);
    var canvas = new AutomapCanvas();

    canvas.ConnectByCommand(building, debris, "xyzzy");
    canvas.ConnectByCommand(building, debris, "xyzzy");

    Connections().ShouldHaveSingleItem().Flow.ShouldBe(ConnectionFlow.OneWay);
  }

  [Test]
  public void DifferentCommandBack_AddsASecondJoin()
  {
    var building = AddRoomAt("Inside Building", 0, 0);
    var y2 = AddRoomAt("Y2", 400, 0);
    var canvas = new AutomapCanvas();

    canvas.ConnectByCommand(building, y2, "plugh");
    canvas.ConnectByCommand(y2, building, "xyzzy");

    Connections().Select(c => c.MidText).ShouldBe(new[] { "plugh", "xyzzy" }, ignoreOrder: true);
  }

  [Test]
  public void SameRoomOrBlankCommand_AddsNothing()
  {
    var building = AddRoomAt("Inside Building", 0, 0);
    var debris = AddRoomAt("Debris Room", 400, 0);
    var canvas = new AutomapCanvas();

    canvas.ConnectByCommand(building, building, "xyzzy");
    canvas.ConnectByCommand(building, debris, "  ");

    Connections().ShouldBeEmpty();
  }
}
