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

    second.Name.ShouldBe("Maze (2)");
    wrong.GetSourceRoom(out var side).ShouldBe(second);
    side.ShouldBe(CompassPoint.East);
    wrong.GetTargetRoom().ShouldBe(pit);
    Project.Current.Elements.OfType<Connection>().Single(c => c != wrong).GetTargetRoom().ShouldBe(maze);
    second.Position.X.ShouldBeGreaterThan(maze.Position.X);
  }

  [Test]
  public void ANameAlreadyTaken_GetsTheNextNumber()
  {
    Add("Maze", 0, 0);
    Add("Maze (2)", 300, 0);
    RoomSplit.SplitName(Project.Current, "Maze").ShouldBe("Maze (3)");
  }
}
