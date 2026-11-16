using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Editor.Commands;

namespace Trizbort.Core.Tests.Editor;

/// <summary>
///   Lee, 16 Nov 2026 (Hollywood Hijinx): in the closet he pulled the first peg (it went down to the cellar) and walked
///   out, so automap never saw the closet move. A moving room can be given another stop by hand.
/// </summary>
[TestFixture]
[Category("Unit")]
public class AddStopTests : IsolatedProjectTests
{
  private static Room Add(string name, float x, float y)
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y) };
    Project.Current.Elements.Add(room);
    return room;
  }

  [Test]
  public void AnotherStop_IsACopyOfTheMovingRoom_BesideIt_ClearOfOtherRooms()
  {
    var closet = Add("Closet", 0, 0);
    closet.Moves = true;
    closet.AddDescription("A small closet.");
    closet.IsDark = true;
    // something already to the east
    var foyer = Add("Foyer", 160, 0);

    var stop = RoomCopies.AddStop(Project.Current.Elements, closet);

    stop.ShouldNotBeNull();
    Project.Current.Elements.ShouldContain(stop);
    stop.IsStop.ShouldBeTrue();
    stop.ReferenceRoom.ShouldBe(closet);
    stop.Name.ShouldBe("Closet");
    stop.Descriptions.ShouldBe(new[] { "A small closet." });
    stop.IsDark.ShouldBeTrue();
    stop.InnerBounds.IntersectsWith(closet.InnerBounds).ShouldBeFalse();
    stop.InnerBounds.IntersectsWith(foyer.InnerBounds).ShouldBeFalse();
    // not joined: automap asks what moved it the first time it sees it
    stop.GetConnections().ShouldBeEmpty();
  }

  [Test]
  public void AnotherStop_FromAStop_IsAStopOfTheRoomItself()
  {
    var closet = Add("Closet", 0, 0);
    closet.Moves = true;
    var first = RoomCopies.AddStop(Project.Current.Elements, closet);

    var second = RoomCopies.AddStop(Project.Current.Elements, first);

    second.ReferenceRoom.ShouldBe(closet);
    RoomCopies.Of(Project.Current.Elements, closet).Count().ShouldBe(2);
  }

  [Test]
  public void ARoomThatDoesntMove_HasNoStops()
  {
    var hall = Add("Hall", 0, 0);
    RoomCopies.CanAddStop(hall).ShouldBeFalse();
    RoomCopies.AddStop(Project.Current.Elements, hall).ShouldBeNull();
  }

  [Test]
  public void TheMenu_SaysWhatChoosingItDoes_AndAddsAStopAsOneUndoStep()
  {
    using var session = new EditorSession();
    var catalogue = CommandCatalogue.CreateDefault();
    var closet = Add("Closet", 0, 0);
    session.Selection.Set(new Element[] { closet });
    var moves = catalogue.Find("room.toggleMoves");
    var addStop = catalogue.Find("room.addStop");

    moves.TitleFor(session).ShouldBe("This Room M_oves");
    addStop.CanExecute(session).ShouldBeFalse();

    moves.ExecuteAsync(session).Wait();
    closet.Moves.ShouldBeTrue();
    moves.TitleFor(session).ShouldBe("This Room Doesn't M_ove");
    addStop.CanExecute(session).ShouldBeTrue();

    addStop.ExecuteAsync(session).Wait();
    var stop = session.Selection.Items.ShouldHaveSingleItem().ShouldBeOfType<Room>();
    stop.ReferenceRoom.ShouldBe(closet);
    session.Undo.UndoTitle.ShouldBe("Add another stop for Closet");
  }
}
