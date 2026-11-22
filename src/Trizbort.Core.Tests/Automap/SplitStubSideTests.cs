using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

namespace Trizbort.Core.Tests.Automap;

/// <summary>
///   Lee, 20 Nov 2026: the Brink of Pit's split join "d", with its label dragged below and right of the room, kept
///   leaving the room from the top left (facing the far room, the Orange River Chamber, up and left), its line crossing
///   the room to its label. A split join's stub leaves its room on its label's side.
/// </summary>
[TestFixture]
[Category("Unit")]
public class SplitStubSideTests : IsolatedProjectTests
{
  private static (Room Brink, Room River, Connection Join) Map(CompassPoint dockedAt)
  {
    var project = Project.Current;
    var brink = new Room(project) { Name = "At Brink of Pit", Position = new Vector(1888, 2720) };
    var river = new Room(project) { Name = "Orange River Chamber", Position = new Vector(64, 1216) };
    project.Elements.Add(brink);
    project.Elements.Add(river);
    // automap's: "d" from the brink, one way, nobody back up
    var join = new Connection(project, new Vertex(brink.PortAt(dockedAt)), new Vertex(river.PortAt(CompassPoint.SouthEast)))
    {
      Flow = ConnectionFlow.OneWay,
      StartText = "d",
      StartTravelled = "down",
      Split = true
    };
    project.Elements.Add(join);
    return (brink, river, join);
  }

  [Test]
  public void AStubFacingAwayFromItsLabel_MovesToItsLabelsSide_AndTheLabelStaysPut()
  {
    var (brink, _, join) = Map(CompassPoint.NorthNorthWest);
    join.GetSplitStub(true, out _, out var tip);
    // the label dragged below and right of the room
    var label = new Vector(brink.InnerBounds.Right + 60, brink.InnerBounds.Bottom + 60);
    join.SetSplitLabelOffset(true, label - tip);

    GuessedEnds.Face(Project.Current.Elements.ToList());

    join.GetSourceRoom(out var side);
    var port = brink.PortAt(side).Position;
    port.X.ShouldBeGreaterThan(brink.InnerBounds.Center.X);
    port.Y.ShouldBeGreaterThan(brink.InnerBounds.Center.Y);
    var stubEnd = join.GetSplitStubEnd(true);
    stubEnd.X.ShouldBe(label.X, 0.01f);
    stubEnd.Y.ShouldBe(label.Y, 0.01f);
  }

  [Test]
  public void AStubOnItsLabelsSide_StaysThere_WhereverTheFarRoomIs()
  {
    var (brink, _, join) = Map(CompassPoint.SouthEast);
    join.SetSplitLabelOffset(true, new Vector(98, 119));

    for (var i = 0; i < 3; i++) GuessedEnds.Face(Project.Current.Elements.ToList());

    join.GetSourceRoom(out var side);
    side.ShouldBe(CompassPoint.SouthEast);
    join.GetSplitLabelOffset(true).ShouldBe(new Vector(98, 119));
    brink.ShouldNotBeNull();
  }

  [Test]
  public void AGuessedStubEnd_GivesWayToALoopOnItsSide()
  {
    // Lee, 22 Nov 2026: south from the west end of the Hall of Mists into the maze; automap took north to lead back,
    // but north looped back into the maze room. The split join, now one way into the maze room, still met it on the
    // north side, on top of the loop
    var project = Project.Current;
    var hall = new Room(project) { Name = "At West End of Hall of Mists", Position = new Vector(0, -2000) };
    var maze = new Room(project) { Name = "Maze", Position = new Vector(0, 0) };
    project.Elements.Add(hall);
    project.Elements.Add(maze);
    var join = new Connection(project, new Vertex(hall.PortAt(CompassPoint.South)), new Vertex(maze.PortAt(CompassPoint.North)))
    {
      Flow = ConnectionFlow.OneWay,
      StartText = "s",
      StartTravelled = "south",
      Split = true
    };
    var loop = new Connection(project, new Vertex(maze.PortAt(CompassPoint.North)), new Vertex(maze.PortAt(CompassPoint.North)))
    {
      StartText = "n",
      StartTravelled = "north"
    };
    project.Elements.Add(join);
    project.Elements.Add(loop);

    GuessedEnds.Face(project.Elements.ToList());

    join.GetTargetRoom(out var side);
    side.ShouldNotBe(CompassPoint.North);
    // still coming in from above, but clear of the loop's point and those either side of it
    side.ShouldBeOneOf(CompassPoint.NorthEast, CompassPoint.NorthWest);
    join.GetSplitLabelOffset(false).ShouldBe(Vector.Zero);
    // the way seen, south out of the hall, stays where it is
    join.GetSourceRoom(out var start);
    start.ShouldBe(CompassPoint.South);
  }
}
