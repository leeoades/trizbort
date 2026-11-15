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
  public void AfterTheRoomsMove_ACommandJoinFacesEachRoom_RatherThanRunningAcrossThem()
  {
    // the closet's stop was put east of the closet, then moved to the west, by the cellar
    var closet = AddRoomAt("Closet", 0, 0);
    var stop = AddRoomAt("Closet", 400, 0);
    new AutomapCanvas().ConnectByCommand(closet, stop, "pull first peg");
    stop.Position = new Vector(-400, -100);

    GuessedEnds.Face(Project.Current.Elements.ToList());

    var connection = Connections().ShouldHaveSingleItem();
    connection.GetSourceRoom(out var sourcePoint);
    connection.GetTargetRoom(out var targetPoint);
    sourcePoint.ShouldBeOneOf(CompassPoint.West, CompassPoint.WestNorthWest);
    targetPoint.ShouldBeOneOf(CompassPoint.East, CompassPoint.EastSouthEast);
  }

  [Test]
  public void BetweenStops_WhatMovesItBack_GoesOnTheSameJoin_LabelledAtEachEnd()
  {
    var closet = AddRoomAt("Closet", 0, 0);
    var stop = AddRoomAt("Closet", 400, 0);
    var canvas = new AutomapCanvas();

    canvas.ConnectByCommand(closet, stop, "pull first peg");
    canvas.ConnectStops(stop, closet, "pull second peg");

    var connection = Connections().ShouldHaveSingleItem();
    connection.Flow.ShouldBe(ConnectionFlow.TwoWay);
    connection.StartText.ShouldBe("pull first peg");
    connection.EndText.ShouldBe("pull second peg");
    connection.MidText.ShouldBeEmpty();
    connection.Style.ShouldBe(ConnectionStyle.Dashed);
  }

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
  public void ADirectionBack_JoinsTheCommandJoin_WithTheCommandAtItsStart()
  {
    // the foyer's closet: "enter closet" goes in, "north" comes back out
    var foyer = AddRoomAt("Foyer", 0, 0);
    var closet = AddRoomAt("Closet", 0, 400);
    var canvas = new AutomapCanvas();

    canvas.ConnectByCommand(foyer, closet, "enter closet");
    var join = Connections().ShouldHaveSingleItem();
    join.StartText.ShouldBeNullOrEmpty();
    join.EndText.ShouldBeNullOrEmpty();

    canvas.Connect(closet, MappableDirection.North, foyer, true);

    Connections().ShouldHaveSingleItem().ShouldBeSameAs(join);
    join.Flow.ShouldBe(ConnectionFlow.TwoWay);
    join.Style.ShouldBe(ConnectionStyle.Solid);
    join.AutomapCommand.ShouldBeNullOrEmpty();
    join.MidText.ShouldBeNullOrEmpty();
    join.GetSourceRoom().ShouldBeSameAs(foyer);
    join.StartText.ShouldBe("enter closet"); // not "south", and not the side it happens to leave from
    join.GetTargetRoom(out var closetSide).ShouldBeSameAs(closet);
    closetSide.ShouldBe(CompassPoint.North);
    join.EndText.ShouldBeNullOrEmpty(); // the north side says it
  }

  [Test]
  public void ACommandJoinOnACornerOfTheRoom_IsNotLabelledWithTheCorner()
  {
    var foyer = AddRoomAt("Foyer", 0, 0);
    var closet = AddRoomAt("Closet", -400, -400);
    var canvas = new AutomapCanvas();

    canvas.ConnectByCommand(foyer, closet, "enter closet");
    canvas.Connect(closet, MappableDirection.Out, foyer, true);

    var join = Connections().ShouldHaveSingleItem();
    join.StartText.ShouldBe("enter closet");
    join.EndText.ShouldBe("out");
  }

  [Test]
  public void AWayOutBackTheWayYouCame_IsAssumedToBeTheJoin_NotAStub()
  {
    // up from the cellar, one-way as far as automap knows; the kitchen mentions a way down
    var cellar = AddRoomAt("Cellar", 0, 400);
    var kitchen = AddRoomAt("Kitchen", 0, 0);
    var canvas = new AutomapCanvas();

    canvas.Connect(cellar, MappableDirection.Up, kitchen, false);
    canvas.AddExitStub(kitchen, MappableDirection.Down);
    canvas.AddExitStub(kitchen, MappableDirection.Down);

    var join = Connections().ShouldHaveSingleItem();
    join.Flow.ShouldBe(ConnectionFlow.OneWay);
    join.GetTargetRoom().ShouldBeSameAs(kitchen);
    join.EndText.ShouldStartWith("d", Case.Insensitive); // "down", or "d" when directions are abbreviated
  }

  [Test]
  public void ACompassWayOutBack_DocksTheJoinOnThatSide()
  {
    var hall = AddRoomAt("Hall", 0, 0);
    var garden = AddRoomAt("Garden", 400, 0);
    var canvas = new AutomapCanvas();

    canvas.Connect(hall, MappableDirection.East, garden, false);
    canvas.AddExitStub(garden, MappableDirection.West);
    canvas.AddExitStub(garden, MappableDirection.North);

    var joins = Connections();
    joins.Length.ShouldBe(2); // the join, and a stub north
    var join = joins.Single(c => c.GetSourceRoom() == hall);
    join.Flow.ShouldBe(ConnectionFlow.OneWay);
    join.GetTargetRoom(out var side).ShouldBeSameAs(garden);
    side.ShouldBe(CompassPoint.West);
    join.EndText.ShouldBeNullOrEmpty();
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
