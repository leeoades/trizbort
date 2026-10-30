using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests;

// Tickets 007 and 034: new-connection defaults and redock.
[TestFixture]
public class ConnectionGestureTests : CanvasGestureTestBase
{
  [Test]
  public void DrawConnection_UsesNewDefaults_StyleFlowLabel()
  {
    var a = AddRoom(0, 0);
    Session.NewConnection.Style = ConnectionStyle.Dashed;
    Session.NewConnection.Flow = ConnectionFlow.OneWay;
    Session.NewConnection.Label = ConnectionLabel.Up;
    Connection.GetText(ConnectionLabel.Up, out var start, out var end);

    var connection = DrawConnection(a, new Vector(300, 200));

    connection.Style.ShouldBe(ConnectionStyle.Dashed);
    connection.Flow.ShouldBe(ConnectionFlow.OneWay);
    connection.StartText.ShouldBe(start);
    connection.EndText.ShouldBe(end);
  }

  [Test]
  public void DrawConnection_NoSelection_UsesAppDefaults_SolidTwoWay()
  {
    var a = AddRoom(0, 0);

    var connection = DrawConnection(a, new Vector(300, 200));

    connection.Style.ShouldBe(ConnectionStyle.Solid);
    connection.Flow.ShouldBe(ConnectionFlow.TwoWay);
    connection.StartText.ShouldBeNullOrEmpty();
    connection.EndText.ShouldBeNullOrEmpty();
  }

  [Test]
  public void SelectingConnection_CopiesItsStyleFlowIntoDefaults()
  {
    var a = AddRoom(0, 0);
    var b = AddRoom(300, 0);
    var connection = AddConnection(PortOf(a, CompassPoint.East), PortOf(b, CompassPoint.West));
    connection.Style = ConnectionStyle.Dashed;
    connection.Flow = ConnectionFlow.OneWay;

    Select(connection);

    Session.NewConnection.Style.ShouldBe(ConnectionStyle.Dashed);
    Session.NewConnection.Flow.ShouldBe(ConnectionFlow.OneWay);
  }

  [Test]
  public void SelectingTwoConnections_DoesNotChangeDefaults()
  {
    var a = AddRoom(0, 0);
    var b = AddRoom(300, 0);
    var first = AddConnection(PortOf(a, CompassPoint.East), PortOf(b, CompassPoint.West));
    var second = AddConnection(PortOf(a, CompassPoint.South), PortOf(b, CompassPoint.South));
    first.Style = ConnectionStyle.Dashed;
    second.Flow = ConnectionFlow.OneWay;

    Select(first, second);

    Session.NewConnection.Style.ShouldBe(ConnectionStyle.Solid);
    Session.NewConnection.Flow.ShouldBe(ConnectionFlow.TwoWay);
  }

  [Test]
  public void SelectingRoom_DoesNotChangeDefaults()
  {
    var a = AddRoom(0, 0);
    Session.NewConnection.Style = ConnectionStyle.Dashed;
    Session.NewConnection.Flow = ConnectionFlow.OneWay;

    Select(a);

    Session.NewConnection.Style.ShouldBe(ConnectionStyle.Dashed);
    Session.NewConnection.Flow.ShouldBe(ConnectionFlow.OneWay);
  }

  [Test]
  public void Redock_HoverFixedPort_Docks()
  {
    var a = AddRoom(0, 0);
    var b = AddRoom(300, 0);
    var c = AddRoom(300, 300);
    var connection = AddConnection(PortOf(a, CompassPoint.East), PortOf(b, CompassPoint.West));
    Select(connection);
    var end = connection.VertexList[1].Position;
    var target = PortOf(c, CompassPoint.West).Position;

    Move(end);
    Press(end);
    Move(target);
    Release(target);

    var moved = Session.Project.Elements.OfType<Connection>().Single();
    moved.VertexList[1].Port.ShouldNotBeNull();
    moved.VertexList[1].Port.Owner.Id.ShouldBe(c.Id);
  }

  [Test]
  public void Redock_EmptySpace_UndocksAndSnaps()
  {
    Trizbort.Setup.Settings.SnapToGrid = true;
    var a = AddRoom(0, 0);
    var b = AddRoom(300, 0);
    var connection = AddConnection(PortOf(a, CompassPoint.East), PortOf(b, CompassPoint.West));
    Select(connection);
    var end = connection.VertexList[1].Position;

    Move(end);
    Press(end);
    Move(new Vector(517, 411));
    Release(new Vector(517, 411));

    var moved = Session.Project.Elements.OfType<Connection>().Single();
    moved.VertexList[1].Port.ShouldBeNull();
    (moved.VertexList[1].Position.X % 32).ShouldBe(0f, 0.01f);
    (moved.VertexList[1].Position.Y % 32).ShouldBe(0f, 0.01f);
  }

  [Test]
  public void Redock_StayingOverSamePort_DoesNotRedock()
  {
    var a = AddRoom(0, 0);
    var b = AddRoom(300, 0);
    var c = AddRoom(300, 300);
    var connection = AddConnection(PortOf(a, CompassPoint.East), PortOf(b, CompassPoint.West));
    Select(connection);
    var end = connection.VertexList[1].Position;
    var target = PortOf(c, CompassPoint.West).Position;

    Move(end);
    Press(end);
    Move(target);
    var live = Session.Project.Elements.OfType<Connection>().Single();
    var dockedAt = live.VertexList[1].Port;
    var changes = 0;
    live.Changed += (_, _) => changes++;

    Move(target + new Vector(1, 1));
    Move(target + new Vector(-1, 0));

    changes.ShouldBe(0);
    live.VertexList[1].Port.ShouldBeSameAs(dockedAt);
    Release(target);
  }

  [Test]
  public void Redock_KeepsTextOnEnds()
  {
    var a = AddRoom(0, 0);
    var b = AddRoom(300, 0);
    var connection = AddConnection(PortOf(a, CompassPoint.East), PortOf(b, CompassPoint.West));
    connection.StartText = "start";
    connection.EndText = "end";
    Select(connection);
    var end = connection.VertexList[1].Position;

    Move(end);
    Press(end);
    Move(new Vector(500, 400));
    Release(new Vector(500, 400));

    var moved = Session.Project.Elements.OfType<Connection>().Single();
    moved.StartText.ShouldBe("start");
    moved.EndText.ShouldBe("end");
  }

  [Test]
  public void Redock_IsOneUndoStep_AndCancelRestoresDock()
  {
    var a = AddRoom(0, 0);
    var b = AddRoom(300, 0);
    var connection = AddConnection(PortOf(a, CompassPoint.East), PortOf(b, CompassPoint.West));
    Select(connection);
    var id = connection.Id;
    var end = connection.VertexList[1].Position;
    Session.Undo.Clear();

    // Cancel restores the dock and records nothing.
    Move(end);
    Press(end);
    Move(new Vector(500, 400));
    Interaction.Cancel();
    FindConnection(id).VertexList[1].Port.ShouldNotBeNull();
    Session.Undo.CanUndo.ShouldBeFalse();

    // Release records exactly one step; undo restores the dock.
    var again = FindConnection(id).VertexList[1].Position;
    Move(again);
    Press(again);
    Move(new Vector(450, 350));
    Move(new Vector(500, 400));
    Release(new Vector(500, 400));
    Session.Undo.CanUndo.ShouldBeTrue();
    FindConnection(id).VertexList[1].Port.ShouldBeNull();

    Session.PerformUndo();

    FindConnection(id).VertexList[1].Port.ShouldNotBeNull();
    Session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void NewConnection_FromPort_CreatedConnection_IsOneUndoStep()
  {
    var a = AddRoom(0, 0);
    Session.Undo.Clear();

    DrawConnection(a, new Vector(300, 200));

    Session.Undo.UndoTitle.ShouldBe("Add connection");
    Session.PerformUndo();
    Session.Project.Elements.OfType<Connection>().ShouldBeEmpty();
    Session.Undo.CanUndo.ShouldBeFalse();
  }
}
