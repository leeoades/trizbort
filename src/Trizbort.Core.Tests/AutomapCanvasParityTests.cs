using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Parser = Trizbort.Automap.Automap;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 128: AutomapCanvas selects and scrolls to the room being worked on and dashes dotted exits.</summary>
[TestFixture]
[Category("Unit")]
public class AutomapCanvasParityTests : IsolatedProjectTests
{
  private static Room AddRoomAt(string name, float x, float y)
  {
    var room = ProjectRegressionTests.AddRoom(name);
    room.Position = new Vector(x, y);
    return room;
  }

  [Test]
  public void SelectRoom_SelectsRoomInSession()
  {
    using var session = new EditorSession();
    var room = AddRoomAt("Hall", 0, 0);
    var canvas = new AutomapCanvas(null, null, session);

    canvas.SelectRoom(room);

    session.Selection.Items.ShouldBe(new Element[] { room });
    Project.Current.ActiveSelectedElement.ShouldBeSameAs(room);
  }

  [Test]
  public void SelectRoom_OffScreenRoom_ScrollsItIntoViewWithoutChangingZoom()
  {
    using var session = new EditorSession();
    session.Viewport.SetView(Vector.Zero, 1.0f);
    var room = AddRoomAt("Far Away", 3000, 2000);
    session.Viewport.VisibleWorldBounds.Contains(room.InnerBounds).ShouldBeFalse();
    var canvas = new AutomapCanvas(null, null, session);

    canvas.SelectRoom(room);

    session.Viewport.VisibleWorldBounds.Contains(room.InnerBounds).ShouldBeTrue();
    session.Viewport.ZoomFactor.ShouldBe(1.0f);
  }

  [Test]
  public void SelectRoom_AlreadyVisibleRoom_DoesNotMoveTheView()
  {
    using var session = new EditorSession();
    session.Viewport.SetView(Vector.Zero, 1.0f);
    var room = AddRoomAt("Near", -20, -20);
    var origin = session.Viewport.Origin;
    var canvas = new AutomapCanvas(null, null, session);

    canvas.SelectRoom(room);

    session.Viewport.Origin.ShouldBe(origin);
  }

  [Test]
  public void EnsureVisible_RoomPartlyOffTheEdge_PansJustEnough()
  {
    using var session = new EditorSession();
    session.Viewport.SetView(Vector.Zero, 1.0f);
    var visible = session.Viewport.VisibleWorldBounds;
    var room = AddRoomAt("Edge", visible.Right - 10, 0);

    session.EnsureVisible(room);

    session.Viewport.VisibleWorldBounds.Contains(room.InnerBounds).ShouldBeTrue();
    session.Viewport.Origin.X.ShouldBeLessThan(room.InnerBounds.Center.X);
  }

  [Test]
  public void SelectRoom_WithoutSession_OnlySetsActiveElement()
  {
    var room = AddRoomAt("Hall", 0, 0);
    var canvas = new AutomapCanvas();

    canvas.SelectRoom(room);

    Project.Current.ActiveSelectedElement.ShouldBeSameAs(room);
  }

  [Test]
  public void Connect_DottedExit_CreatesDashedConnectionAndClearsFlag()
  {
    var parser = new Parser();
    var first = AddRoomAt("First", 0, 0);
    var second = AddRoomAt("Second", 300, 0);
    parser.UseDottedConnection = true;

    new AutomapCanvas(null, parser).Connect(first, MappableDirection.East, second, true);

    var connection = Project.Current.Elements.OfType<Connection>().Single();
    connection.Style.ShouldBe(ConnectionStyle.Dashed);
    parser.UseDottedConnection.ShouldBeFalse();
  }

  [Test]
  public void Connect_PlainExit_CreatesSolidConnection()
  {
    var parser = new Parser();
    var first = AddRoomAt("First", 0, 0);
    var second = AddRoomAt("Second", 300, 0);

    new AutomapCanvas(null, parser).Connect(first, MappableDirection.East, second, true);

    Project.Current.Elements.OfType<Connection>().Single().Style.ShouldBe(ConnectionStyle.Solid);
  }
}
