using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests;

// Ticket 064: waypoint drag threshold, selected waypoint and Delete.
[TestFixture]
public class WaypointInteractionTests : CanvasGestureTestBase
{
  private Connection AddFreeConnection()
  {
    var connection = new Connection(CurrentProject, new Vertex(new Vector(-100, 0)), new Vertex(new Vector(100, 0)));
    CurrentProject.Elements.Add(connection);
    Select(connection);
    return connection;
  }

  private Connection Dragged(int id)
  {
    return FindConnection(id);
  }

  [Test]
  public void BareClick_OnGhostWaypoint_AddsNothing()
  {
    var connection = AddFreeConnection();
    var handle = connection.GetCurveWaypointHandlePosition(CurveWaypoint.Middle);
    Session.Undo.Clear();

    Move(handle);
    Press(handle);
    Interaction.Mode.ShouldBe(CanvasDragMode.MoveWaypoint);
    Move(handle + new Vector(5, 5));
    Release(handle + new Vector(5, 5));

    connection.GetCurveWaypoint(CurveWaypoint.Middle).ShouldBeNull();
    Session.SelectedWaypoint.ShouldBeNull();
    Session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void Drag_BeyondThreshold_AddsAndSelectsWaypoint_OneUndoStep()
  {
    var connection = AddFreeConnection();
    var id = connection.Id;
    var handle = connection.GetCurveWaypointHandlePosition(CurveWaypoint.Middle);
    Session.Undo.Clear();

    Move(handle);
    Press(handle);
    Move(handle + new Vector(0, -50));
    Release(handle + new Vector(0, -50));

    Dragged(id).GetCurveWaypoint(CurveWaypoint.Middle).ShouldNotBeNull();
    Session.SelectedWaypoint.ShouldBe(CurveWaypoint.Middle);

    Session.PerformUndo();

    Dragged(id).GetCurveWaypoint(CurveWaypoint.Middle).ShouldBeNull();
    Session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void Delete_RemovesSelectedWaypoint_NotConnection()
  {
    var connection = AddFreeConnection();
    var id = connection.Id;
    var handle = connection.GetCurveWaypointHandlePosition(CurveWaypoint.Middle);
    Move(handle);
    Press(handle);
    Move(handle + new Vector(0, -50));
    Release(handle + new Vector(0, -50));
    Session.Undo.Clear();

    Session.DeleteSelected();

    var remaining = Session.Project.Elements.OfType<Connection>().Single();
    remaining.Id.ShouldBe(id);
    remaining.GetCurveWaypoint(CurveWaypoint.Middle).ShouldBeNull();
    Session.SelectedWaypoint.ShouldBeNull();
    Session.Undo.UndoTitle.ShouldBe("Delete waypoint");

    // With no waypoint selected, the next Delete removes the connection.
    Session.DeleteSelected();
    Session.Project.Elements.OfType<Connection>().ShouldBeEmpty();
  }

  [Test]
  public void Delete_Undo_RestoresWaypoint()
  {
    var connection = AddFreeConnection();
    var id = connection.Id;
    var handle = connection.GetCurveWaypointHandlePosition(CurveWaypoint.Middle);
    Move(handle);
    Press(handle);
    Move(handle + new Vector(0, -50));
    Release(handle + new Vector(0, -50));

    Session.DeleteSelected();
    Session.PerformUndo();

    Dragged(id).GetCurveWaypoint(CurveWaypoint.Middle).ShouldNotBeNull();
  }

  [Test]
  public void SelectionChange_ClearsSelectedWaypoint()
  {
    var connection = AddFreeConnection();
    var handle = connection.GetCurveWaypointHandlePosition(CurveWaypoint.Middle);
    Move(handle);
    Press(handle);
    Move(handle + new Vector(0, -50));
    Release(handle + new Vector(0, -50));
    Session.SelectedWaypoint.ShouldNotBeNull();

    Session.Selection.Clear();

    Session.SelectedWaypoint.ShouldBeNull();
  }
}
