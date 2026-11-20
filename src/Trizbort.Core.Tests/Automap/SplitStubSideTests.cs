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
}
