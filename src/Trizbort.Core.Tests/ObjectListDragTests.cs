using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

namespace Trizbort.Core.Tests;

/// <summary>Dragging a room's object list on the map places it by hand, by any amount; undo puts it back.</summary>
[TestFixture]
[Category("Unit")]
public class ObjectListDragTests : CanvasGestureTestBase
{
  [Test]
  public void DraggingTheList_PlacesItByHand_WhereItWasDrawn_NudgedByTheDrag()
  {
    var room = AddRoom(0, 0);
    room.Objects = "skis";
    // as drawn: automatic, moved out of the way to the east
    room.ObjectListBounds = new Rect(70, 10, 30, 14);
    room.ObjectsDrawnAt = CompassPoint.East;

    Press(new Vector(80, 15)).ShouldBeTrue();
    Move(new Vector(90, 20));
    Move(new Vector(103, 8));
    Release(new Vector(103, 8));

    var moved = Session.Project.Elements.OfType<Room>().Single();
    moved.ObjectsPlaced.ShouldBeTrue();
    moved.ObjectsPosition.ShouldBe(CompassPoint.East);
    moved.ObjectsCustomPosition.ShouldBeTrue();
    moved.ObjectsCustomPositionRight.ShouldBe(23);
    moved.ObjectsCustomPositionDown.ShouldBe(-7);
    moved.Position.ShouldBe(new Vector(0, 0));

    Session.Undo.Undo(Session);
    Session.Project.Elements.OfType<Room>().Single().ObjectsPlaced.ShouldBeFalse();
  }

  [Test]
  public void AListInsideTheRoom_IsOnlyDraggedOnceTheRoomIsSelected()
  {
    var room = AddRoom(0, 0);
    room.Objects = "skis";
    room.ObjectListBounds = new Rect(4, 40, 30, 14);
    room.ObjectsDrawnInside = true;

    // not selected: the room itself moves
    Press(new Vector(10, 45));
    Move(new Vector(40, 45));
    Release(new Vector(40, 45));
    var first = Session.Project.Elements.OfType<Room>().Single();
    first.ObjectsPlaced.ShouldBeFalse();
    first.Position.X.ShouldBeGreaterThan(0);
  }
}
