using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Controllers;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using Trizbort.UI.Controls;

namespace Trizbort.Tests;

internal class TestCanvas : Canvas {
  public void MoveMouse(Vector world)
  {
    var point = Point.Round(CanvasToClient(world));
    OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 0));
  }

  public void PressMouse(Vector world)
  {
    var point = Point.Round(CanvasToClient(world));
    OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, point.X, point.Y, 0));
  }

  public void ReleaseMouse()
  {
    OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
  }

  public void Key(Keys key)
  {
    OnKeyDown(new KeyEventArgs(key));
  }

  public void Wheel(Point point)
  {
    OnMouseWheel(new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 120));
  }

  public void LeaveMouse()
  {
    OnMouseLeave(EventArgs.Empty);
  }
}

[TestFixture]
[Category("Unit")]
public class EditingRegressionTests : IsolatedProjectTests {
  private static IEnumerable<TestCaseData> ResizeCases()
  {
    foreach (var label in new[] { false, true })
      foreach (var point in new[] {
                 CompassPoint.North, CompassPoint.NorthEast, CompassPoint.East, CompassPoint.SouthEast,
                 CompassPoint.South, CompassPoint.SouthWest, CompassPoint.West, CompassPoint.NorthWest
               })
        yield return new TestCaseData(label, point);
  }

  [TestCaseSource(nameof(ResizeCases))]
  public void Resize_AllHandlesChangeOnlyTheirEdges(bool label, CompassPoint point)
  {
    var node = label ? (ISizeable)new MapLabel(Project.Current) : new Room(Project.Current);
    node.Position = new Vector(10, 20);
    node.Size = new Vector(100, 80);
    var handle = new ResizeHandle(point, node);
    var old = handle.OwnerPosition;
    handle.HitTest(handle.Position + new Vector(Settings.HandleSize / 2)).ShouldBeTrue();
    handle.HitTest(handle.Position - new Vector(100)).ShouldBeFalse();
    handle.OwnerPosition = old + new Vector(10, 10);
    var movesLeft = point == CompassPoint.West || point == CompassPoint.NorthWest || point == CompassPoint.SouthWest;
    var movesRight = point == CompassPoint.East || point == CompassPoint.NorthEast || point == CompassPoint.SouthEast;
    var movesTop = point == CompassPoint.North || point == CompassPoint.NorthWest || point == CompassPoint.NorthEast;
    var movesBottom = point == CompassPoint.South || point == CompassPoint.SouthWest || point == CompassPoint.SouthEast;
    node.InnerBounds.Left.ShouldBe(movesLeft ? 20 : 10);
    node.InnerBounds.Right.ShouldBe(movesRight ? 120 : 110);
    node.InnerBounds.Top.ShouldBe(movesTop ? 30 : 20);
    node.InnerBounds.Bottom.ShouldBe(movesBottom ? 110 : 100);
  }

  [TestCaseSource(nameof(ResizeCases))]
  public void Resize_RejectsInversionAndAllowsOneUnitMinimum(bool label, CompassPoint point)
  {
    var node = label ? (ISizeable)new MapLabel(Project.Current) : new Room(Project.Current);
    node.Position = Vector.Zero;
    node.Size = new Vector(10, 10);
    var handle = new ResizeHandle(point, node);
    var center = new Vector(5, 5);
    var corner = handle.OwnerPosition;
    var opposite = center * 2 - corner;
    handle.OwnerPosition = opposite;
    node.Size.ShouldBe(new Vector(10, 10));
    var minimum = new Vector(
      corner.X < 5 ? 9 : corner.X > 5 ? 1 : 5,
      corner.Y < 5 ? 9 : corner.Y > 5 ? 1 : 5);
    handle.OwnerPosition = minimum;
    node.Width.ShouldBe(point == CompassPoint.North || point == CompassPoint.South ? 10 : 1);
    node.Height.ShouldBe(point == CompassPoint.East || point == CompassPoint.West ? 10 : 1);
  }

  [TestCase(false)]
  [TestCase(true)]
  public void Resize_RepeatedSmallDragsTrackAppliedDeltaWithoutDrift(bool snap)
  {
    Settings.SnapToGrid = snap;
    Settings.GridSize = 10;
    var room = new Room(Project.Current) { Position = Vector.Zero, Size = new Vector(100, 80) };
    var handle = new ResizeHandle(CompassPoint.SouthEast, room);
    var last = handle.OwnerPosition;
    for (var i = 1; i <= 100; i++) last = MapEditing.Resize(handle, last, new Vector(100 + i, 80 + i));
    room.Size.ShouldBe(new Vector(200, 180));
    last.ShouldBe(new Vector(200, 180));
    last = MapEditing.Resize(handle, last, new Vector(-50, -50));
    room.Size.ShouldBe(new Vector(200, 180));
    MapEditing.Resize(handle, last, new Vector(210, 190)).ShouldBe(new Vector(210, 190));
  }

  [TestCase(false, false)]
  [TestCase(true, false)]
  [TestCase(true, true)]
  public void MovingNodesAndConnections_MovesWaypointsExactlyOnce(bool bothNodes, bool selectedLine)
  {
    var first = ProjectRegressionTests.AddRoom("First");
    var second = ProjectRegressionTests.AddRoom("Second");
    var connection = ProjectRegressionTests.Connect(first, second);
    connection.SetCurveWaypoint(CurveWaypoint.Middle, new Vector(50, 60));
    var selected = new List<Element> { first };
    if (bothNodes) selected.Add(second);
    if (selectedLine) selected.Add(connection);
    MapEditing.Move(Project.Current.Elements, selected, new Vector(20, -10));
    connection.GetCurveWaypoint(CurveWaypoint.Middle).ShouldBe(new Vector(bothNodes ? 70 : 50, bothNodes ? 50 : 60));
    connection.VertexList[0].Port.Owner.ShouldBeSameAs(first);
    connection.VertexList[1].Port.Owner.ShouldBeSameAs(second);
  }

  [Test]
  public void MovingFreeConnection_TranslatesAllVerticesAndWaypoints()
  {
    var connection = new Connection(Project.Current, new Vertex(Vector.Zero), new Vertex(new Vector(100, 0)));
    connection.SetCurveWaypoint(CurveWaypoint.Middle, new Vector(50, 30));
    MapEditing.Move(new[] { connection }, new Element[] { connection }, new Vector(-10, 20));
    connection.VertexList.Select(vertex => vertex.Position).ShouldBe(new[] { new Vector(-10, 20), new Vector(90, 20) });
    connection.GetCurveWaypoint(CurveWaypoint.Middle).ShouldBe(new Vector(40, 50));
  }

  [Test]
  public void ClipboardReconstruction_RemapsCopiedDocks_AndDetachesOmittedEndpoints()
  {
    var first = ProjectRegressionTests.AddRoom("First");
    var second = ProjectRegressionTests.AddRoom("Second");
    var connection = ProjectRegressionTests.Connect(first, second);
    connection.Door = new Door { Locked = true };
    connection.SetCurveWaypoint(CurveWaypoint.Middle, new Vector(100, 60));
    var controller = new CopyController();
    var copy = JsonConvert.DeserializeObject<CopyController.CopyObject>(
      JsonConvert.SerializeObject(controller.CreateCopyObject(new[] { connection })));
    var copied = ProjectRegressionTests.AddRoom("Copy");
    var pasted = controller.PasteConnections(
      Project.Current,
      copy.Connections,
      new Dictionary<int, Element> { { first.Id, copied } },
      new Vector(20, 10)).Single();
    pasted.Id.ShouldNotBe(connection.Id);
    pasted.GetSourceRoom().ShouldBeSameAs(copied);
    pasted.VertexList[1].Port.ShouldBeNull();
    pasted.VertexList[1].Position.ShouldBe(connection.VertexList[1].Position - new Vector(20, 10));
    pasted.GetCurveWaypoint(CurveWaypoint.Middle).ShouldBe(new Vector(80, 50));
    pasted.Door.Locked.ShouldBeTrue();
    pasted.Door.ShouldNotBeSameAs(copy.Connections.Single().Door);
    pasted.Door.Locked = false;
    copy.Connections.Single().Door.Locked.ShouldBeTrue();
  }
}

[TestFixture]
[Category("Integration")]
public class CanvasInteractionTests : IsolatedProjectTests {
  [Test]
  public void TooltipHover_RegistersNativeDelayedCursorPositionedTooltipWithoutShowingImmediately()
  {
    ApplicationSettingsController.AppSettings.ShowObjectsInTooltips = true;
    using var canvas = new TestCanvas { Size = new Size(600, 400) };
    var room = ProjectRegressionTests.AddRoom("Room");
    room.Objects = "lamp\nkey\nbag";
    canvas.MoveMouse(room.InnerBounds.Center);
    var tooltip = GetTooltip(canvas);
    tooltip.GetToolTip(canvas).ShouldBe(room.GetToolTipHeader());
    tooltip.FooterText.ShouldBe(room.GetToolTipFooter());
    tooltip.HoverElement.ShouldBeSameAs(room);
    tooltip.InitialDelay.ShouldBe(500);
    tooltip.ReshowDelay.ShouldBe(500);
    tooltip.AutoPopDelay.ShouldBe(5000);
    tooltip.IsShown.ShouldBeFalse();
    tooltip.LastOwner.ShouldBeNull();
    tooltip.LastPosition.ShouldBe(Point.Empty);
    canvas.MoveMouse(room.InnerBounds.Center + new Vector(1, 1));
    tooltip.GetToolTip(canvas).ShouldBe(room.GetToolTipHeader());
  }

  [TestCase("leave")]
  [TestCase("click")]
  [TestCase("empty")]
  [TestCase("disabled")]
  public void TooltipHover_DismissesVisibleAndPendingTooltip(string action)
  {
    ApplicationSettingsController.AppSettings.ShowObjectsInTooltips = true;
    using var canvas = new TestCanvas { Size = new Size(600, 400) };
    var room = ProjectRegressionTests.AddRoom("Room");
    canvas.MoveMouse(room.InnerBounds.Center);
    var tooltip = GetTooltip(canvas);
    tooltip.GetToolTip(canvas).ShouldNotBeNullOrEmpty();
    tooltip.IsShown = true;
    tooltip.LastOwner = canvas;
    if (action == "leave") {
      canvas.LeaveMouse();
    }
    else if (action == "click") {
      canvas.PressMouse(room.InnerBounds.Center);
      canvas.SelectedElement.ShouldBeSameAs(room);
    }
    else if (action == "disabled") {
      ApplicationSettingsController.AppSettings.ShowObjectsInTooltips = false;
      canvas.MoveMouse(room.InnerBounds.Center + new Vector(1, 1));
    }
    else {
      canvas.MoveMouse(new Vector(250, 150));
    }

    tooltip.GetToolTip(canvas).ShouldBeNullOrEmpty();
    tooltip.IsShown.ShouldBeFalse();
    tooltip.LastOwner.ShouldBeNull();
    tooltip.HoverElement.ShouldBeNull();
  }

  [Test]
  public void TooltipHover_SwitchesRoomsAndDoesNotChangeConnectionText()
  {
    ApplicationSettingsController.AppSettings.ShowObjectsInTooltips = true;
    using var canvas = new TestCanvas { Size = new Size(600, 400) };
    var first = ProjectRegressionTests.AddRoom("First");
    var second = ProjectRegressionTests.AddRoom("Second");
    second.Position = new Vector(100, 50);
    canvas.MoveMouse(first.InnerBounds.Center);
    canvas.MoveMouse(second.InnerBounds.Center);
    GetTooltip(canvas).GetToolTip(canvas).ShouldBe(second.GetToolTipHeader());

    var connection = new Connection(
      Project.Current,
      new Vertex(new Vector(-200, -150)),
      new Vertex(new Vector(200, -150))) { Name = "Connection", MidText = "path" };
    Project.Current.Elements.Add(connection);
    Project.Current.IsDirty = false;
    canvas.MoveMouse(new Vector(0, -150));
    GetTooltip(canvas).HoverElement.ShouldBeSameAs(connection);
    connection.MidText.ShouldBe("path");
    Project.Current.IsDirty.ShouldBeFalse();
  }

  private static TrizbortToolTip GetTooltip(Canvas canvas)
  {
    return canvas.ElementToolTip;
  }

  private static IEnumerable<TestCaseData> TooltipMovementCases()
  {
    foreach (var kind in new[] { "room", "label", "connection" })
      foreach (var key in new[] { Keys.Left, Keys.Right, Keys.Up, Keys.Down, Keys.None })
        yield return new TestCaseData(kind, key);
  }

  [TestCaseSource(nameof(TooltipMovementCases))]
  public void MovingSelection_DismissesExistingTooltip(string kind, Keys key)
  {
    Settings.SnapToGrid = false;
    using var canvas = new TestCanvas { Size = new Size(600, 400) };
    canvas.ZoomFactor = 1;
    Element element;
    if (kind == "room") {
      element = ProjectRegressionTests.AddRoom("Room");
    }
    else if (kind == "label") {
      element = new MapLabel(Project.Current);
      Project.Current.Elements.Add(element);
    }
    else {
      element = new Connection(Project.Current, new Vertex(new Vector(-100, 0)), new Vertex(new Vector(100, 0)));
      Project.Current.Elements.Add(element);
    }

    canvas.SelectedElement = element;
    var center = element is Room room ? room.InnerBounds.Center :
      element is MapLabel label ? label.InnerBounds.Center : new Vector(-50, 0);
    if (key == Keys.None) {
      canvas.MoveMouse(center);
      canvas.PressMouse(center);
    }

    // Seed the tooltip's public lifecycle state without displaying a native popup.
    var tooltip = canvas.ElementToolTip;
    tooltip.LastOwner = canvas;
    tooltip.HoverElement = element;
    tooltip.IsShown = true;
    Project.Current.IsDirty = false;
    if (key == Keys.None) {
      canvas.MoveMouse(center + new Vector(30, 20));
      canvas.ReleaseMouse();
    }
    else {
      canvas.Key(key);
    }

    Project.Current.IsDirty.ShouldBeTrue();
    tooltip.IsShown.ShouldBeFalse();
    tooltip.LastOwner.ShouldBeNull();
    tooltip.HoverElement.ShouldBeNull();
  }

  [TestCase(Keys.Left)]
  [TestCase(Keys.Right)]
  [TestCase(Keys.Up)]
  [TestCase(Keys.Down)]
  public void KeyboardPanning_DismissesExistingTooltip(Keys key)
  {
    using var canvas = new TestCanvas { Size = new Size(600, 400) };
    var tooltip = canvas.ElementToolTip;
    tooltip.LastOwner = canvas;
    tooltip.HoverElement = ProjectRegressionTests.AddRoom("Room");
    tooltip.IsShown = true;
    var origin = canvas.ClientToCanvas(Point.Empty);
    canvas.Key(key);
    canvas.ClientToCanvas(Point.Empty).ShouldNotBe(origin);
    tooltip.IsShown.ShouldBeFalse();
    tooltip.LastOwner.ShouldBeNull();
    tooltip.HoverElement.ShouldBeNull();
  }

  [TestCase(.1f)]
  [TestCase(1f)]
  [TestCase(4f)]
  public void CoordinateTransforms_RoundTripWithNegativeOrigin(float zoom)
  {
    using var canvas = new TestCanvas { Size = new Size(601, 401), Origin = new Vector(-100, 70) };
    canvas.ZoomFactor = zoom;
    var point = new Vector(-245.25f, 180.5f);
    canvas.ClientToCanvas(canvas.CanvasToClient(point)).Distance(point).ShouldBeLessThan(.001f);
    var size = canvas.ClientToCanvas(canvas.CanvasToClient(new SizeF(31, 17)));
    size.Width.ShouldBe(31, .001f);
    size.Height.ShouldBe(17, .001f);
  }

  [Test]
  public void MouseAndKeyboardMovement_UpdateModelSelectionAndDirtyState()
  {
    Settings.SnapToGrid = false;
    using var canvas = new TestCanvas { Size = new Size(600, 400) };
    var room = canvas.AddRoom(false, false, false);
    room.Position = new Vector(-50, -30);
    var center = room.InnerBounds.Center;
    Project.Current.IsDirty = false;
    canvas.MoveMouse(center);
    canvas.PressMouse(center);
    canvas.MoveMouse(center + new Vector(30, 20));
    canvas.ReleaseMouse();
    room.Position.ShouldBe(new Vector(-20, -10));
    canvas.SelectedElement.ShouldBeSameAs(room);
    Project.Current.IsDirty.ShouldBeTrue();
    canvas.Key(Keys.Right);
    room.Position.ShouldBe(new Vector(-18, -10));
    canvas.DeleteSelection();
    Project.Current.Elements.ShouldBeEmpty();
    canvas.SelectedElements.ShouldBeEmpty();
  }

  [Test]
  public void MouseResize_UsesHandleAndSnappingThroughActualEvents()
  {
    Settings.SnapToGrid = true;
    Settings.GridSize = 10;
    using var canvas = new TestCanvas { Size = new Size(600, 400) };
    var room = canvas.AddRoom(false, false, false);
    room.Position = new Vector(-50, -40);
    room.Size = new Vector(100, 80);
    var corner = room.InnerBounds.GetCorner(CompassPoint.SouthEast);
    canvas.MoveMouse(corner);
    canvas.PressMouse(corner);
    canvas.MoveMouse(corner + new Vector(20, 30));
    canvas.ReleaseMouse();
    room.Position.ShouldBe(new Vector(-50, -40));
    room.Size.ShouldBe(new Vector(120, 110));
  }

  [TestCase(3, false)]
  [TestCase(4, false)]
  [TestCase(5, true)]
  public void InsertWaypoint_RequiresMovementStrictlyPastThreshold(int distance, bool creates)
  {
    Settings.SnapToGrid = false;
    Settings.DragDistanceToInitiateNewConnection = 4;
    using var canvas = new TestCanvas { Size = new Size(600, 400) };
    canvas.ZoomFactor = 1;
    var connection = new Connection(Project.Current, new Vertex(new Vector(-100, 0)), new Vertex(new Vector(100, 0)));
    Project.Current.Elements.Add(connection);
    canvas.SelectedElement = connection;
    canvas.MoveMouse(Vector.Zero);
    canvas.PressMouse(Vector.Zero);
    canvas.MoveMouse(new Vector(0, distance));
    canvas.ReleaseMouse();
    connection.HasCurveWaypoints.ShouldBe(creates);
    if (creates) {
      connection.GetCurveWaypoint(CurveWaypoint.Middle).ShouldBe(new Vector(0, distance));
      canvas.DeleteSelection();
      connection.HasCurveWaypoints.ShouldBeFalse();
      Project.Current.Elements.ShouldContain(connection);
    }
  }

  [Test]
  public void Paste_ClonesStylesAndReferenceRooms_AndSelectsNewGraph()
  {
    using var canvas = new TestCanvas { Size = new Size(600, 400) };
    var first = canvas.AddRoom(false, false, false);
    first.Name = "First";
    var alias = canvas.AddRoom(false, false, false);
    alias.ReferenceRoomId = first.Id;
    var line = ProjectRegressionTests.Connect(first, alias);
    var controller = new CopyController();
    var copy = controller.CreateCopyObject(new Element[] { first, alias, line });
    canvas.PasteRooms(false, copy, controller);
    var pasted = canvas.SelectedRooms;
    pasted.Count.ShouldBe(2);
    pasted.Single(room => room.ReferenceRoomId != -1).ReferenceRoom
          .ShouldBeSameAs(pasted.Single(room => room.Name == "First"));
    pasted.Select(room => room.Id).Intersect(new[] { first.Id, alias.Id }).ShouldBeEmpty();
    var pastedLine = canvas.SelectedConnections.Single();
    pasted.ShouldContain(pastedLine.GetSourceRoom());
    pasted.ShouldContain(pastedLine.GetTargetRoom());
    pasted[0].Corners.ShouldNotBeSameAs(first.Corners);
    pasted[0].Corners.TopLeft = 25;
    first.Corners.TopLeft.ShouldBe(15);
  }

  [TestCase(false, false, false)]
  [TestCase(false, false, true)]
  [TestCase(false, true, false)]
  [TestCase(false, true, true)]
  [TestCase(true, false, false)]
  [TestCase(true, false, true)]
  [TestCase(true, true, false)]
  [TestCase(true, true, true)]
  public void PasteReferences_ResolveOnlyTargetsIncludedInClipboard(bool crossMap, bool includeTarget, bool aliasFirst)
  {
    var source = Project.Current;
    var target = ProjectRegressionTests.AddRoom("Source target");
    var alias = ProjectRegressionTests.AddRoom("Alias");
    alias.ReferenceRoomId = target.Id;
    var controller = new CopyController();
    var selected = new List<Element> { alias };
    if (includeTarget) selected.Insert(aliasFirst ? 1 : 0, target);
    var copy = JsonConvert.DeserializeObject<CopyController.CopyObject>(
      JsonConvert.SerializeObject(controller.CreateCopyObject(selected)));
    try {
      if (crossMap) {
        Project.Current = new Project();
        ProjectRegressionTests.AddRoom("Unrelated target").Id.ShouldBe(target.Id);
      }

      using var canvas = new TestCanvas();
      canvas.PasteRooms(false, copy, controller);
      var pastedAlias = canvas.SelectedRooms.Single(room => room.Name == "Alias");
      if (includeTarget) {
        var pastedTarget = canvas.SelectedRooms.Single(room => room.Name == "Source target");
        pastedAlias.ReferenceRoom.ShouldBeSameAs(pastedTarget);
        pastedTarget.ShouldNotBeSameAs(target);
      }
      else {
        pastedAlias.ReferenceRoomId.ShouldBe(-1);
        pastedAlias.ReferenceRoom.ShouldBeNull();
        pastedAlias.IsReference.ShouldBeFalse();
      }

      alias.ReferenceRoomId.ShouldBe(target.Id);
    }
    finally {
      if (crossMap) {
        Project.Current.Dispose();
        Project.Current = source;
      }
    }
  }

  [Test]
  public void PasteReferences_DanglingIdCannotBindToNewRoomOrCopiedLabel()
  {
    var alias = ProjectRegressionTests.AddRoom("Alias");
    var label = new MapLabel(Project.Current);
    Project.Current.Elements.Add(label);
    alias.ReferenceRoomId = label.Id;
    var controller = new CopyController();
    var copy = controller.CreateCopyObject(new Element[] { alias, label });
    using var canvas = new TestCanvas();
    canvas.PasteRooms(false, copy, controller);
    canvas.SelectedRooms.Single().ReferenceRoomId.ShouldBe(-1);
    // The next pasted room receives this ID; it must not become its own target.
    copy.Rooms.Single().ReferenceRoomId = Project.Current.Elements.Max(element => element.Id) + 1;
    canvas.PasteRooms(false, copy, controller);
    canvas.SelectedRooms.Single().ReferenceRoomId.ShouldBe(-1);
    canvas.SelectedRooms.Single().ReferenceRoom.ShouldBeNull();
  }

  [Test]
  public void MouseConnectionDrag_CreatesDockedConnectionThroughActualEvents()
  {
    Settings.SnapToGrid = false;
    Settings.DragDistanceToInitiateNewConnection = 4;
    using var canvas = new TestCanvas { Size = new Size(600, 400) };
    canvas.ZoomFactor = 1;
    var first = ProjectRegressionTests.AddRoom("First");
    first.Position = new Vector(-200, -40);
    first.Size = new Vector(80, 80);
    var second = ProjectRegressionTests.AddRoom("Second");
    second.Position = new Vector(120, -40);
    second.Size = new Vector(80, 80);
    var start = first.PortAt(CompassPoint.East).Position;
    var end = second.PortAt(CompassPoint.West).Position;
    Project.Current.IsDirty = false;
    canvas.MoveMouse(first.InnerBounds.Center);
    canvas.MoveMouse(start);
    canvas.PressMouse(start);
    canvas.MoveMouse(start + new Vector(30, 0));
    canvas.MoveMouse(end);
    canvas.HoverElement.ShouldBeSameAs(second);
    canvas.ReleaseMouse();
    var connection = Project.Current.Elements.OfType<Connection>().Single();
    connection.GetSourceRoom().ShouldBeSameAs(first);
    connection.GetTargetRoom().ShouldBeSameAs(second);
    connection.IsDangling.ShouldBeFalse();
    Project.Current.IsDirty.ShouldBeTrue();
  }

  [Test]
  public void ChangingSelection_ClearsWaypointDeletionState()
  {
    Settings.SnapToGrid = false;
    using var canvas = new TestCanvas { Size = new Size(600, 400) };
    canvas.ZoomFactor = 1;
    var line = new Connection(Project.Current, new Vertex(new Vector(-100, 0)), new Vertex(new Vector(100, 0)));
    Project.Current.Elements.Add(line);
    line.SetCurveWaypoint(CurveWaypoint.Middle, new Vector(0, 50));
    canvas.SelectedElement = line;
    canvas.MoveMouse(new Vector(0, 50));
    canvas.PressMouse(new Vector(0, 50));
    canvas.ReleaseMouse();
    canvas.SelectedElement = ProjectRegressionTests.AddRoom("Other");
    canvas.SelectedElement = line;
    canvas.DeleteSelection();
    Project.Current.Elements.ShouldNotContain(line);
  }

  [Test]
  public void KeyboardMovement_TranslatesFreeConnectionAndItsWaypoints()
  {
    Settings.SnapToGrid = false;
    using var canvas = new TestCanvas();
    var connection = new Connection(Project.Current, new Vertex(Vector.Zero), new Vertex(new Vector(100, 0)));
    Project.Current.Elements.Add(connection);
    connection.SetCurveWaypoint(CurveWaypoint.Middle, new Vector(50, 60));
    canvas.SelectedElement = connection;
    canvas.Key(Keys.Right);
    connection.VertexList.Select(vertex => vertex.Position).ShouldBe(new[] { new Vector(2, 0), new Vector(102, 0) });
    connection.GetCurveWaypoint(CurveWaypoint.Middle).ShouldBe(new Vector(52, 60));
  }

  [Test]
  public void WheelZoom_AcceptsTallViewport_AndKeepsWorldPointUnderCursor()
  {
    ApplicationSettingsController.AppSettings.InvertMouseWheel = true;
    using var canvas = new TestCanvas { Size = new Size(200, 600) };
    canvas.ZoomFactor = 1;
    var point = new Point(100, 450);
    var world = canvas.ClientToCanvas(point);
    canvas.Wheel(point);
    canvas.ZoomFactor.ShouldBeGreaterThan(1);
    canvas.ClientToCanvas(point).Distance(world).ShouldBeLessThan(.001f);
    var zoom = canvas.ZoomFactor;
    canvas.Wheel(new Point(100, 650));
    canvas.ZoomFactor.ShouldBe(zoom);
  }
}