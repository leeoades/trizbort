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
///   Lee, 17 Nov 2026 (Hollywood Hijinx): "enter fireplace" from the Living Room into the Fireplace, and later, down
///   the chimney into what automap took for another Fireplace (it showed a different description), east back into the
///   Living Room. Merging the two Fireplaces left two joins, one each way. A way there and a way back between the same
///   two rooms are one join.
/// </summary>
[TestFixture]
[Category("Unit")]
public class RoomMergeThereAndBackTests : IsolatedProjectTests
{
  private static Room AddRoomAt(string name, float x, float y)
  {
    var room = ProjectRegressionTests.AddRoom(name);
    room.Position = new Vector(x, y);
    return room;
  }

  private static Connection[] Joins() => Project.Current.Elements.OfType<Connection>().ToArray();

  [Test]
  public void ACommandThere_AndADirectionBack_FromTwoRoomsMerged_AreOneJoin()
  {
    var living = AddRoomAt("Living Room", 640, -224);
    var fireplace = AddRoomAt("Fireplace", 128, -224);
    var chimneyFireplace = AddRoomAt("Fireplace", 128, 96);
    var canvas = new AutomapCanvas();
    canvas.ConnectByCommand(living, fireplace, "enter fireplace");
    canvas.Connect(chimneyFireplace, MappableDirection.East, living, true);
    Joins().Length.ShouldBe(2);

    RoomMerge.Merge(Project.Current.Elements, fireplace, new[] { chimneyFireplace });

    var join = Joins().ShouldHaveSingleItem();
    join.Flow.ShouldBe(ConnectionFlow.TwoWay);
    join.Style.ShouldBe(ConnectionStyle.Solid);
    var ways = JoinWays.Of(join);
    var livingIsStart = join.GetSourceRoom() == living;
    JoinWays.Commands(ways.Where(way => way.AtStart == livingIsStart).ToList()).ShouldContain("enter fireplace");
    ways.ShouldContain(way => way.AtStart != livingIsStart && way.IsDirection && way.Seen &&
                              way.Direction == MappableDirection.East);
  }

  [Test]
  public void ADirectionThere_AndOneBack_OneWayEach_AreOneJoin()
  {
    var hall = AddRoomAt("Hall", 0, 0);
    var kitchen = AddRoomAt("Kitchen", 320, 0);
    var otherKitchen = AddRoomAt("Kitchen", 320, 320);
    var canvas = new AutomapCanvas();
    canvas.Connect(hall, MappableDirection.East, kitchen, false);
    canvas.Connect(otherKitchen, MappableDirection.West, hall, false);
    Joins().Length.ShouldBe(2);

    RoomMerge.Merge(Project.Current.Elements, kitchen, new[] { otherKitchen });

    var join = Joins().ShouldHaveSingleItem();
    join.Flow.ShouldBe(ConnectionFlow.TwoWay);
    var ways = JoinWays.Of(join);
    var hallIsStart = join.GetSourceRoom() == hall;
    ways.ShouldContain(way => way.AtStart == hallIsStart && way.Seen && way.Direction == MappableDirection.East);
    ways.ShouldContain(way => way.AtStart != hallIsStart && way.Seen && way.Direction == MappableDirection.West);
  }

  [Test]
  public void TwoWaysThere_FromTheSameRoom_StayTwoJoins()
  {
    // east and north from the hall both into what was two kitchens: two ways out, not a way there and back
    var hall = AddRoomAt("Hall", 0, 0);
    var kitchen = AddRoomAt("Kitchen", 320, 0);
    var otherKitchen = AddRoomAt("Kitchen", 0, -320);
    var canvas = new AutomapCanvas();
    canvas.Connect(hall, MappableDirection.East, kitchen, false);
    canvas.Connect(hall, MappableDirection.North, otherKitchen, false);

    RoomMerge.Merge(Project.Current.Elements, kitchen, new[] { otherKitchen });

    Joins().Length.ShouldBe(2);
  }
}
