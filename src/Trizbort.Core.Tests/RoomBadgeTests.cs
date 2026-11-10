using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests;

/// <summary>The little signs in a room's corner (start room, don't automap from here) and their tooltips.</summary>
[TestFixture]
[NonParallelizable]
public class RoomBadgeTests
{
  private Project _previous;
  private Project _project;

  [SetUp]
  public void SetUp()
  {
    _previous = Project.Current;
    _project = new Project();
    Project.Current = _project;
  }

  [TearDown]
  public void TearDown()
  {
    Project.Current = _previous;
    _project.Dispose();
  }

  private Room AddRoom(string name)
  {
    var room = new Room(_project) { Name = name, Position = new Vector(0, 0), Size = new Vector(96, 64) };
    _project.Elements.Add(room);
    return room;
  }

  [Test]
  public void AnOrdinaryRoom_HasNoSigns()
  {
    RoomBadges.Of(AddRoom("Hall")).ShouldBeEmpty();
  }

  [Test]
  public void TheStartRoomSign_ComesFirst_ThenDontAutomap_SideBySideInTheTopLeftCorner()
  {
    var room = AddRoom("Hedge Maze");
    room.IsStartRoom = true;
    room.AutomapPause = true;

    var badges = RoomBadges.Of(room);

    badges.Count.ShouldBe(2);
    badges[0].Kind.ShouldBe(RoomBadgeKind.StartRoom);
    badges[1].Kind.ShouldBe(RoomBadgeKind.NoAutomap);
    badges[1].Centre.X.ShouldBeGreaterThan(badges[0].Centre.X + badges[0].Radius * 2);
    badges[0].Centre.Y.ShouldBe(badges[1].Centre.Y);
    room.InnerBounds.Contains(badges[1].Centre).ShouldBeTrue();
  }

  [Test]
  public void HoveringOverASign_ExplainsIt_ElsewhereTheRoomsOwnTooltip()
  {
    var room = AddRoom("Hedge Maze");
    room.AutomapPause = true;
    var sign = RoomBadges.Of(room)[0];

    var onSign = TooltipModel.Create(room, sign.Centre);
    onSign.ShouldNotBeNull();
    onSign.Title.ShouldBe("Don't automap from here");
    onSign.Body.ShouldContain("Automap maps nothing");

    TooltipModel.Create(room, room.InnerBounds.Center)?.Title.ShouldBe("Hedge Maze");
  }
}
