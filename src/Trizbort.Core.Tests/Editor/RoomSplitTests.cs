using System;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests.Editor;

[TestFixture]
[Category("Unit")]
public class RoomSplitTests : IsolatedProjectTests
{
  private static Room Add(string name, float x, float y)
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y) };
    Project.Current.Elements.Add(room);
    return room;
  }

  [Test]
  public void SplittingAMaze_MovesTheJoinsPicked_ToTheNewRoom_AtTheSameSides()
  {
    // the hall's east led to a "Maze" that turned out not to be the maze north of the hall
    var hall = Add("Hall", 0, 0);
    var maze = Add("Maze", 0, -300);
    var pit = Add("Pit", 300, -300);
    var canvas = new AutomapCanvas(Project.Current);
    canvas.Connect(hall, MappableDirection.North, maze, true);
    canvas.Connect(maze, MappableDirection.East, pit, true);
    var wrong = Project.Current.Elements.OfType<Connection>().Single(c => c.GetTargetRoom() == pit);

    RoomSplit.Suggested(maze).ShouldBe(wrong); // the newest
    RoomSplit.Describe(wrong, maze).ShouldBe("east to Pit (west back)");

    var second = RoomSplit.Split(Project.Current, maze, new[] { wrong });

    // Lee, 21 Nov 2026: the game calls them both "Maze", and so does the map, so automap knows it by its name
    second.Name.ShouldBe("Maze");
    wrong.GetSourceRoom(out var side).ShouldBe(second);
    side.ShouldBe(CompassPoint.East);
    wrong.GetTargetRoom().ShouldBe(pit);
    Project.Current.Elements.OfType<Connection>().Single(c => c != wrong).GetTargetRoom().ShouldBe(maze);
    second.Position.X.ShouldBeGreaterThan(maze.Position.X);
  }

  [Test]
  public void SplittingOffAWayRound_MovesOnlyTheEndItArrivesAt()
  {
    // up "In the Chimney", taken for a loop back into the same chimney
    var chimney = Add("In the Chimney", 0, 0);
    var loop = new Connection(Project.Current, new Vertex(chimney.PortAt(CompassPoint.North)),
      new Vertex(chimney.PortAt(CompassPoint.North)));
    Project.Current.Elements.Add(loop);
    JoinWays.Write(loop, new[] { JoinWay.Go(true, MappableDirection.Up, true, "up") }, DirectionWords.Word);
    RoomSplit.Describe(loop, chimney).ShouldBe("up to In the Chimney itself");

    RoomSplit.Exit(loop, chimney).ShouldBe("up, back here");

    var upper = RoomSplit.Split(Project.Current, chimney, new[] { loop });

    loop.GetSourceRoom().ShouldBe(chimney);
    loop.GetTargetRoom().ShouldBe(upper);
    // nothing says the way back yet
    loop.Flow.ShouldBe(ConnectionFlow.OneWay);
  }

  [Test]
  public void TheOtherRoom_HasTheSameNameAndDescription_ButNotTheObjects()
  {
    var maze = Add("Maze", 0, 0);
    maze.AddDescription("You are in a maze of twisty little passages, all alike.");
    maze.Objects = "food";
    Add("Maze", 300, 300);

    RoomSplit.SplitName(Project.Current, "Maze").ShouldBe("Maze");
    var other = RoomSplit.Split(Project.Current, maze, Array.Empty<Connection>());

    other.Name.ShouldBe("Maze");
    other.Descriptions.ShouldBe(maze.Descriptions);
    string.IsNullOrEmpty(other.Objects).ShouldBeTrue();
  }

  [Test]
  public void TheWaysOut_ReadFromTheRoom()
  {
    // Lee, 21 Nov 2026: "n to At West End of Hall" read as a way out, when it's a one-way way in
    var hall = Add("At West End of Hall of Mists", 0, 300);
    var maze = Add("Maze", 0, 0);
    var pit = Add("Pit", 300, 0);
    var inward = new Connection(Project.Current, new Vertex(hall.PortAt(CompassPoint.South)),
      new Vertex(maze.PortAt(CompassPoint.North))) { Flow = ConnectionFlow.OneWay, StartText = "s" };
    Project.Current.Elements.Add(inward);
    var canvas = new AutomapCanvas(Project.Current);
    canvas.Connect(maze, MappableDirection.East, pit, true);
    var east = Project.Current.Elements.OfType<Connection>().Single(c => c.GetTargetRoom() == pit);

    RoomSplit.Exit(inward, maze).ShouldStartWith("in from At West End of Hall of Mists");
    RoomSplit.SideOf(inward, maze).ShouldBe(CompassPoint.North);
    RoomSplit.Exit(east, maze).ShouldBe("east to Pit (west back)");
    RoomSplit.SideOf(east, maze).ShouldBe(CompassPoint.East);
  }
}
