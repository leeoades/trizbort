using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests.Editor;

[TestFixture]
[Category("Unit")]
public class RoomCopiesTests : IsolatedProjectTests
{
  private static Room Add(string name, float x, float y)
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y) };
    Project.Current.Elements.Add(room);
    return room;
  }

  private static Connection Join(Room a, CompassPoint from, Room b, CompassPoint to)
  {
    var join = new Connection(Project.Current, new Vertex(a.PortAt(from)), new Vertex(b.PortAt(to)));
    Project.Current.Elements.Add(join);
    return join;
  }

  [Test]
  public void MovingACopysJoinsToTheRoom_PutsThemOnTheRoomAtTheSameSide_AndRemovesTheCopy()
  {
    var porch = Add("Front Porch", 0, 0);
    porch.AddDescription("A creaky porch.");
    var hall = Add("Hall", 2000, -200);
    var copy = Add("x", 2000, 0);
    copy.AddReferenceRoom(porch.Id);
    var join = Join(hall, CompassPoint.South, copy, CompassPoint.North);

    RoomCopies.MoveJoinsToRoom(Project.Current.Elements, copy).ShouldBeTrue();

    Project.Current.Elements.ShouldNotContain(copy);
    join.GetTargetRoom(out var side).ShouldBe(porch);
    side.ShouldBe(CompassPoint.North);
    porch.Descriptions.ShouldBe(new[] { "A creaky porch." });
  }

  [Test]
  public void MakingACopyARoomOfItsOwn_KeepsItsJoins_AndTakesTheRoomsDetails()
  {
    var closet = Add("Closet", 0, 0);
    closet.AddDescription("A coat closet.");
    closet.IsDark = true;
    var cellar = Add("Cellar", 0, 300);
    var copy = Add("whatever", 0, 200);
    copy.AddReferenceRoom(closet.Id);
    var join = Join(copy, CompassPoint.South, cellar, CompassPoint.North);

    RoomCopies.MakeRoomOfItsOwn(copy).ShouldBeTrue();

    copy.IsReference.ShouldBeFalse();
    copy.Name.ShouldBe("Closet");
    copy.IsDark.ShouldBeTrue();
    copy.Descriptions.ShouldBe(new[] { "A coat closet." });
    join.GetSourceRoom().ShouldBe(copy);
  }
}
