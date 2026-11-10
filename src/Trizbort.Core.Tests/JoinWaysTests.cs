using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

namespace Trizbort.Core.Tests;

/// <summary>The ways along a join kept as data, and the labels drawn from them.</summary>
[TestFixture]
[Category("Unit")]
public class JoinWaysTests : IsolatedProjectTests
{
  private static Room AddRoomAt(string name, float x, float y)
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y) };
    Project.Current.Elements.Add(room);
    return room;
  }

  private static Connection Join(Room a, CompassPoint from, Room b, CompassPoint to)
  {
    var connection = new Connection(Project.Current, new Vertex(a.PortAt(from)), new Vertex(b.PortAt(to)));
    Project.Current.Elements.Add(connection);
    return connection;
  }

  private static string Word(MappableDirection direction) => DirectionWords.Word(direction);

  [Test]
  public void Ways_RoundTripThroughTheirSavedForm_EvenWithCommasAndSemicolonsInCommands()
  {
    var ways = new[]
    {
      JoinWay.Go(true, MappableDirection.North, true),
      JoinWay.Go(false, MappableDirection.Up, false, "u"),
      JoinWay.Typed(true, "say \"hello, world\"; wave"),
      JoinWay.Written(false, "through the arch")
    };

    JoinWays.Parse(JoinWays.Format(ways)).ShouldBe(ways);
  }

  [Test]
  public void Labels_AreDrawnFromTheWays_TheSideSayingASingleCompassDirection()
  {
    var hall = AddRoomAt("Hall", 0, 0);
    var attic = AddRoomAt("Attic", 0, -300);
    var join = Join(hall, CompassPoint.North, attic, CompassPoint.South);

    JoinWays.Write(join, new[]
    {
      JoinWay.Go(true, MappableDirection.North, true),
      JoinWay.Go(true, MappableDirection.Up, true),
      JoinWay.Go(false, MappableDirection.South, false),
      JoinWay.Typed(true, "climb ladder")
    }, Word);

    join.StartText.ShouldBe("north / up");
    join.EndText.ShouldBeEmpty(); // the south side says it
    join.MidText.ShouldBe("climb ladder");
    join.AutomapCommand.ShouldBeEmpty();
    join.StartTravelled.ShouldBe("north up");
    join.EndTravelled.ShouldBeEmpty();
  }

  [Test]
  public void ACommandOneWay_AndADirectionBack_PutTheCommandOnItsEnd()
  {
    var foyer = AddRoomAt("Foyer", 0, 0);
    var closet = AddRoomAt("Closet", -300, -300);
    var join = Join(foyer, CompassPoint.NorthWest, closet, CompassPoint.North);

    JoinWays.Write(join, new[] { JoinWay.Typed(true, "enter closet"), JoinWay.Go(false, MappableDirection.North, true) }, Word);

    join.StartText.ShouldBe("enter closet"); // not "northwest": the corner only faces the closet
    join.MidText.ShouldBeEmpty();
    join.StartTravelled.ShouldBe(JoinWays.ByCommand);
    JoinWays.Of(join).ShouldNotContain(way => way.AtStart && way.IsDirection);
  }

  [Test]
  public void ACommandAlone_IsWrittenInTheMiddle_AndMarksACommandJoin()
  {
    var building = AddRoomAt("Building", 0, 0);
    var debris = AddRoomAt("Debris", 300, 0);
    var join = Join(building, CompassPoint.East, debris, CompassPoint.West);

    JoinWays.Write(join, new[] { JoinWay.Typed(true, "xyzzy") }, Word);

    join.MidText.ShouldBe("xyzzy");
    join.AutomapCommand.ShouldBe("xyzzy");
    join.StartText.ShouldBeEmpty();
    // its sides aren't directions
    JoinWays.Of(join).ShouldHaveSingleItem().IsCommand.ShouldBeTrue();
  }

  [Test]
  public void WhenTheUserChangesALabel_TheLabelsCount()
  {
    var hall = AddRoomAt("Hall", 0, 0);
    var garden = AddRoomAt("Garden", 300, 0);
    var join = Join(hall, CompassPoint.East, garden, CompassPoint.West);
    JoinWays.Write(join, new[] { JoinWay.Go(true, MappableDirection.East, true) }, Word);
    JoinWays.Of(join).ShouldHaveSingleItem().Seen.ShouldBeTrue();

    join.StartText = "east / through the arch";

    var ways = JoinWays.Of(join);
    ways.ShouldContain(way => way.AtStart && way.IsDirection && way.Direction == MappableDirection.East && way.Seen);
    ways.ShouldContain(way => way.AtStart && way.Kind == JoinWayKind.Label && way.Text == "through the arch");
  }

  [Test]
  public void WhenTheUserMovesAnEndWhoseSideSaidTheDirection_TheSideCounts()
  {
    var hall = AddRoomAt("Hall", 0, 0);
    var garden = AddRoomAt("Garden", 300, 0);
    var join = Join(hall, CompassPoint.East, garden, CompassPoint.West);
    JoinWays.Write(join, new[] { JoinWay.Go(true, MappableDirection.East, true) }, Word);

    join.VertexList[0].Port = hall.PortAt(CompassPoint.NorthEast);

    JoinWays.Lists(JoinWays.Of(join), true, MappableDirection.NorthEast).ShouldBeTrue();
  }

  [Test]
  public void AJoinAutomapNeverRecorded_IsReadFromItsLabelsAndSides()
  {
    var hall = AddRoomAt("Hall", 0, 0);
    var attic = AddRoomAt("Attic", 0, -300);
    var join = Join(hall, CompassPoint.North, attic, CompassPoint.South);
    join.StartText = "u";
    join.MidText = "climb ladder";

    var ways = JoinWays.Of(join);

    JoinWays.Lists(ways, true, MappableDirection.Up).ShouldBeTrue();
    JoinWays.Lists(ways, true, MappableDirection.North).ShouldBeFalse(); // the label says up: the side doesn't add north
    JoinWays.Lists(ways, false, MappableDirection.South).ShouldBeTrue(); // no label: the side says south
    JoinWays.Commands(ways).ShouldBe(new[] { "climb ladder" });
  }

  [Test]
  public void AJoinLabelledDown_OnTheSouthSide_IsDown_NotSouth()
  {
    // drawn by hand from the bottom of the roof to the top of the chimney, labelled down and up
    var roof = AddRoomAt("East End of Roof", 0, 0);
    var chimney = AddRoomAt("In the Chimney", 0, 300);
    var join = Join(roof, CompassPoint.South, chimney, CompassPoint.North);
    join.StartText = "down";
    join.EndText = "up";

    var ways = JoinWays.Of(join);

    JoinWays.Lists(ways, true, MappableDirection.Down).ShouldBeTrue();
    JoinWays.Lists(ways, true, MappableDirection.South).ShouldBeFalse();
    JoinWays.Lists(ways, false, MappableDirection.Up).ShouldBeTrue();
    JoinWays.Lists(ways, false, MappableDirection.North).ShouldBeFalse();
    ConnectionEndLabels.SideDirection(join, true).ShouldBeNull();

    // with no label, the side says it: south
    join.StartText = string.Empty;
    ConnectionEndLabels.SideDirection(join, true).ShouldBe(MappableDirection.South);
    JoinWays.Lists(JoinWays.Of(join), true, MappableDirection.South).ShouldBeTrue();
  }

  [Test]
  public void Ways_AreSavedWithTheMap_AndStillCountAfterLoading()
  {
    var hall = AddRoomAt("Hall", 0, 0);
    var attic = AddRoomAt("Attic", 0, -300);
    var join = Join(hall, CompassPoint.NorthWest, attic, CompassPoint.South);
    JoinWays.Write(join, new[] { JoinWay.Typed(true, "climb ladder"), JoinWay.Go(false, MappableDirection.Down, true) }, Word);

    new LegacyMapFileEngine(Project.Current).Save(Files.File("ways.trizbort")).ShouldBeTrue();
    using var loaded = new Project();
    PersistenceRegressionTests.Load(loaded, Files.File("ways.trizbort"));

    var again = loaded.Elements.OfType<Connection>().ShouldHaveSingleItem();
    again.AutomapWays.ShouldBe(join.AutomapWays);
    JoinWays.Of(again).ShouldBe(JoinWays.Of(join));
  }
}
