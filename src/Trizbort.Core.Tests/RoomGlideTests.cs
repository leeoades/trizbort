using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;

namespace Trizbort.Core.Tests;

/// <summary>Rooms a tidy moves slide to their new places on screen instead of jumping.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class RoomGlideTests
{
  private Project _previousProject;
  private Func<double> _previousClock;
  private double _now;

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    Project.Current = new Project();
    _previousClock = RoomGlide.Clock;
    _now = 1000;
    RoomGlide.Clock = () => _now;
    RoomGlide.Stop();
  }

  [TearDown]
  public void TearDown()
  {
    RoomGlide.Stop();
    RoomGlide.Clock = _previousClock;
    Project.Current = _previousProject;
  }

  private static Room RoomAt(float x, float y)
  {
    var room = new Room(Project.Current) { Position = new Vector(x, y) };
    Project.Current.Elements.Add(room);
    return room;
  }

  [Test]
  public void A_moved_room_appears_where_it_was_then_slides_to_where_it_is()
  {
    var room = RoomAt(0, 0);
    var before = RoomGlide.Positions(Project.Current.Elements);
    room.Position = new Vector(100, 0);
    RoomGlide.Start(before);

    RoomGlide.IsActive.ShouldBeTrue();
    RoomGlide.Appears(room).X.ShouldBe(0f);

    _now += RoomGlide.DurationMs / 2;
    RoomGlide.Appears(room).X.ShouldBeInRange(50f, 99f); // more than half way: it slows as it arrives

    _now += RoomGlide.DurationMs;
    RoomGlide.IsActive.ShouldBeFalse();
    RoomGlide.Appears(room).X.ShouldBe(100f);
  }

  [Test]
  public void Showing_puts_rooms_where_they_appear_and_back_without_telling_anyone()
  {
    var room = RoomAt(0, 0);
    var before = RoomGlide.Positions(Project.Current.Elements);
    room.Position = new Vector(0, 64);
    RoomGlide.Start(before);
    var told = 0;
    room.Changed += (_, _) => told++;

    using (RoomGlide.Show())
    {
      room.Position.Y.ShouldBe(0f);
    }

    room.Position.Y.ShouldBe(64f);
    told.ShouldBe(0);
  }

  [Test]
  public void Rooms_that_did_not_move_do_not_slide()
  {
    RoomAt(0, 0);
    RoomGlide.Start(RoomGlide.Positions(Project.Current.Elements));
    RoomGlide.IsActive.ShouldBeFalse();
  }

  [Test]
  public void A_room_moved_again_while_sliding_carries_on_from_where_it_appears()
  {
    var room = RoomAt(0, 0);
    var before = RoomGlide.Positions(Project.Current.Elements);
    room.Position = new Vector(100, 0);
    RoomGlide.Start(before);
    _now += RoomGlide.DurationMs / 2;
    var halfway = RoomGlide.Appears(room).X;

    before = RoomGlide.Positions(Project.Current.Elements);
    room.Position = new Vector(200, 0);
    RoomGlide.Start(before);

    RoomGlide.Appears(room).X.ShouldBe(halfway);
  }

  [Test]
  public void Stopping_shows_rooms_where_they_are()
  {
    var room = RoomAt(0, 0);
    var before = RoomGlide.Positions(Project.Current.Elements);
    room.Position = new Vector(100, 0);
    RoomGlide.Start(before);

    RoomGlide.Stop();

    RoomGlide.IsActive.ShouldBeFalse();
    RoomGlide.Appears(room).X.ShouldBe(100f);
  }
}
