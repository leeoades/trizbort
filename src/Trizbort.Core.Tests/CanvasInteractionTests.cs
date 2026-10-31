using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Editor.Input;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

[TestFixture]
[NonParallelizable]
public class CanvasInteractionTests
{
  private Project _previousProject;
  private Project _project;
  private EditorSession _session;
  private CanvasInteraction _interaction;
  private bool _previousSnap;
  private float _previousGrid;

  [SetUp]
  public void SetUp()
  {
    _previousSnap = Settings.SnapToGrid;
    _previousGrid = Settings.GridSize;
    Settings.SnapToGrid = false;
    Settings.GridSize = 32;

    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _session = new EditorSession();
    _interaction = new CanvasInteraction(_session, new HitTester(_session));
  }

  [TearDown]
  public void TearDown()
  {
    _interaction.Dispose();
    _session.Dispose();
    var current = Project.Current;
    Project.Current = _previousProject;
    if (current != null && current != _previousProject) current.Dispose();
    if (_project != current) _project.Dispose();
    Settings.SnapToGrid = _previousSnap;
    Settings.GridSize = _previousGrid;
  }

  private Room AddRoom(float x, float y, float size = 64)
  {
    var room = new Room(_project) { Position = new Vector(x, y), Size = new Vector(size, size) };
    _project.Elements.Add(room);
    return room;
  }

  private static Room.CompassPort Port(Room room, CompassPoint point)
  {
    return room.PortList.OfType<Room.CompassPort>().First(p => p.CompassPoint == point);
  }

  private Vector Client(Vector world)
  {
    return _session.Viewport.CanvasToClient(world);
  }

  private bool Press(Vector world, PointerButton button = PointerButton.Left, ModifierKeys modifiers = ModifierKeys.None,
    int clickCount = 1)
  {
    return _interaction.OnPointerPressed(new PointerInput(button, modifiers, Client(world), clickCount));
  }

  private void Move(Vector world, ModifierKeys modifiers = ModifierKeys.None)
  {
    _interaction.OnPointerMoved(new PointerInput(PointerButton.None, modifiers, Client(world), 0));
  }

  private void Release(Vector world, PointerButton button = PointerButton.Left, ModifierKeys modifiers = ModifierKeys.None)
  {
    _interaction.OnPointerReleased(new PointerInput(button, modifiers, Client(world), 0));
  }

  private void Click(Vector world, ModifierKeys modifiers = ModifierKeys.None)
  {
    Move(world, modifiers);
    Press(world, PointerButton.Left, modifiers);
    Release(world, PointerButton.Left, modifiers);
  }

  // Cancelling or undoing replaces the project, so tests look elements up again by id.
  private Room FindRoom(int id)
  {
    return _session.Project.Elements.OfType<Room>().Single(r => r.Id == id);
  }

  private Connection FindConnection(int id)
  {
    return _session.Project.Elements.OfType<Connection>().Single(c => c.Id == id);
  }

  private void Select(params Element[] elements)
  {
    _session.Selection.Set(elements);
  }

  // ---- selection ---------------------------------------------------------------------------

  [Test]
  public void ClickRoom_SelectsOnlyThatRoom()
  {
    var a = AddRoom(0, 0);
    var b = AddRoom(200, 0);
    Select(b);

    Click(new Vector(32, 32));

    _session.Selection.Items.ShouldBe(new Element[] { a });
  }

  [Test]
  public void CtrlClick_TogglesSelection()
  {
    var a = AddRoom(0, 0);
    var b = AddRoom(200, 0);
    Select(a);

    Click(new Vector(232, 32), ModifierKeys.Control);
    _session.Selection.Items.ShouldBe(new Element[] { a, b }, true);

    Click(new Vector(32, 32), ModifierKeys.Control);
    _session.Selection.Items.ShouldBe(new Element[] { b });
  }

  [Test]
  public void ShiftClick_AddsToSelection()
  {
    var a = AddRoom(0, 0);
    var b = AddRoom(200, 0);
    Select(a);

    Click(new Vector(232, 32), ModifierKeys.Shift);
    _session.Selection.Items.ShouldBe(new Element[] { a, b }, true);

    // Shift never removes: clicking an already selected room keeps it selected.
    Click(new Vector(32, 32), ModifierKeys.Shift);
    _session.Selection.Count.ShouldBe(2);
    _session.Selection.Contains(a).ShouldBeTrue();
  }

  [Test]
  public void ClickEmpty_ClearsSelection()
  {
    var a = AddRoom(0, 0);
    Select(a);

    Click(new Vector(500, 500));

    _session.Selection.Count.ShouldBe(0);
  }

  [Test]
  public void ClickEmpty_WithShift_KeepsSelection()
  {
    var a = AddRoom(0, 0);
    Select(a);

    Click(new Vector(500, 500), ModifierKeys.Shift);

    _session.Selection.Contains(a).ShouldBeTrue();
  }

  [Test]
  public void Marquee_SelectsIntersectingElements()
  {
    var a = AddRoom(0, 0);
    var b = AddRoom(200, 0);
    var c = AddRoom(500, 500);

    Move(new Vector(-50, -50));
    Press(new Vector(-50, -50));
    _interaction.Mode.ShouldBe(CanvasDragMode.Marquee);
    Move(new Vector(100, 100));
    _interaction.Adorners.Marquee.ShouldNotBeNull();
    Release(new Vector(100, 100));

    _interaction.Mode.ShouldBe(CanvasDragMode.None);
    _interaction.Adorners.Marquee.ShouldBeNull();
    _session.Selection.Items.ShouldBe(new Element[] { a });
    _session.Selection.Contains(b).ShouldBeFalse();
    _session.Selection.Contains(c).ShouldBeFalse();
  }

  [Test]
  public void Marquee_ShiftAdds()
  {
    var a = AddRoom(0, 0);
    AddRoom(200, 0);
    var c = AddRoom(500, 500);
    Select(c);

    Move(new Vector(-50, -50), ModifierKeys.Shift);
    Press(new Vector(-50, -50), PointerButton.Left, ModifierKeys.Shift);
    Move(new Vector(100, 100), ModifierKeys.Shift);
    Release(new Vector(100, 100), PointerButton.Left, ModifierKeys.Shift);

    _session.Selection.Count.ShouldBe(2);
    _session.Selection.Contains(a).ShouldBeTrue();
    _session.Selection.Contains(c).ShouldBeTrue();
  }

  // ---- move / resize -----------------------------------------------------------------------

  [Test]
  public void DragRoom_MovesSnappedByGrid()
  {
    Settings.SnapToGrid = true;
    var room = AddRoom(0, 0);

    Move(new Vector(32, 32));
    Press(new Vector(32, 32));
    _interaction.Mode.ShouldBe(CanvasDragMode.MoveElement);
    Move(new Vector(82, 52));
    Release(new Vector(82, 52));

    room.Position.X.ShouldNotBe(0f);
    (room.Position.X % 32).ShouldBe(0f, 0.01f);
    (room.Position.Y % 32).ShouldBe(0f, 0.01f);
  }

  [Test]
  public void DragRoom_ReleaseRecordsOneUndoStep()
  {
    var room = AddRoom(0, 0);
    var id = room.Id;
    _session.Undo.Clear();

    Move(new Vector(32, 32));
    Press(new Vector(32, 32));
    Move(new Vector(62, 32));
    Move(new Vector(92, 62));
    Move(new Vector(132, 82));
    Release(new Vector(132, 82));

    _session.Undo.CanUndo.ShouldBeTrue();
    _session.Undo.UndoTitle.ShouldBe("Move");
    FindRoom(id).Position.X.ShouldBe(100f, 0.5f);

    _session.PerformUndo();

    FindRoom(id).Position.ShouldBe(new Vector(0, 0));
    _session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void ClickWithoutMoving_RecordsNoUndoStep()
  {
    AddRoom(0, 0);
    _session.Undo.Clear();

    Click(new Vector(32, 32));

    _session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void DragRoom_Cancel_RestoresPosition_NoUndoStep()
  {
    var room = AddRoom(0, 0);
    var id = room.Id;
    _session.Undo.Clear();

    Move(new Vector(32, 32));
    Press(new Vector(32, 32));
    Move(new Vector(132, 82));
    room.Position.X.ShouldNotBe(0f);

    _interaction.Cancel();

    _interaction.Mode.ShouldBe(CanvasDragMode.None);
    FindRoom(id).Position.ShouldBe(new Vector(0, 0));
    _session.Undo.CanUndo.ShouldBeFalse();

    // A late release after the cancel does nothing.
    Release(new Vector(132, 82));
    FindRoom(id).Position.ShouldBe(new Vector(0, 0));
    _session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void DragRoom_MovesAllSelected()
  {
    var a = AddRoom(0, 0);
    var b = AddRoom(200, 0);
    Select(a, b);

    Move(new Vector(32, 32));
    Press(new Vector(32, 32));
    Move(new Vector(82, 32));
    Release(new Vector(82, 32));

    a.Position.X.ShouldBe(50f, 0.5f);
    b.Position.X.ShouldBe(250f, 0.5f);
  }

  [Test]
  public void DragHandle_Resizes_AndCancelRestores()
  {
    var room = AddRoom(0, 0);
    var id = room.Id;
    Select(room);
    var corner = room.InnerBounds.GetCorner(CompassPoint.SouthEast);

    Move(corner);
    Press(corner);
    _interaction.Mode.ShouldBe(CanvasDragMode.MoveResizeHandle);
    Move(corner + new Vector(32, 32));
    room.Size.X.ShouldBeGreaterThan(64f);

    _interaction.Cancel();

    _interaction.Mode.ShouldBe(CanvasDragMode.None);
    FindRoom(id).Size.ShouldBe(new Vector(64, 64));
    _session.Selection.Contains(FindRoom(id)).ShouldBeTrue();
  }

  // ---- ports and connections ---------------------------------------------------------------

  [Test]
  public void DragPort_MovesEnd_CancelRestoresDock()
  {
    var a = AddRoom(0, 0);
    var b = AddRoom(200, 0);
    var connection = new Connection(_project, new Vertex(Port(a, CompassPoint.East)), new Vertex(Port(b, CompassPoint.West)));
    _project.Elements.Add(connection);
    Select(connection);
    var id = connection.Id;
    var end = connection.VertexList[1].Position;

    Move(end);
    _interaction.Adorners.HoverPort.ShouldBeAssignableTo<MoveablePort>();
    Press(end);
    _interaction.Mode.ShouldBe(CanvasDragMode.MovePort);
    Move(new Vector(300, 300));
    connection.VertexList[1].Port.ShouldBeNull();

    _interaction.Cancel();

    var restored = FindConnection(id);
    restored.VertexList[1].Port.ShouldNotBeNull();
    restored.VertexList[1].Port.Owner.Id.ShouldBe(b.Id);
    _session.Selection.Contains(restored).ShouldBeTrue();
    _session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void DragFromPort_BelowThreshold_CreatesNothing()
  {
    var a = AddRoom(0, 0);
    var start = Port(a, CompassPoint.East).Position;

    Move(start);
    Press(start);
    _interaction.IsGestureActive.ShouldBeTrue();
    Move(start + new Vector(4, 0));
    _interaction.Mode.ShouldBe(CanvasDragMode.None);
    Release(start + new Vector(4, 0));

    _interaction.IsGestureActive.ShouldBeFalse();
    _project.Elements.OfType<Connection>().ShouldBeEmpty();
  }

  [Test]
  public void DragNewConnection_Cancel_RemovesIt()
  {
    var a = AddRoom(0, 0);
    var start = Port(a, CompassPoint.East).Position;
    _session.Undo.Clear();

    Move(start);
    Press(start);
    Move(start + new Vector(100, 100));
    _interaction.Mode.ShouldBe(CanvasDragMode.MovePort);
    _interaction.Adorners.PendingConnection.ShouldNotBeNull();
    _project.Elements.OfType<Connection>().Count().ShouldBe(1);

    _interaction.Cancel();

    _interaction.Mode.ShouldBe(CanvasDragMode.None);
    _session.Project.Elements.OfType<Connection>().ShouldBeEmpty();
    _session.Selection.Count.ShouldBe(0);
    _session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void DragNewConnection_DegenerateOnRelease_Discarded()
  {
    var a = AddRoom(0, 0);
    var start = Port(a, CompassPoint.East).Position;
    _session.Undo.Clear();

    Move(start);
    Press(start);
    Move(start + new Vector(100, 100));
    _project.Elements.OfType<Connection>().Count().ShouldBe(1);
    // Back onto the source port: both ends on one port.
    Move(start);
    Release(start);

    _interaction.Mode.ShouldBe(CanvasDragMode.None);
    _session.Project.Elements.OfType<Connection>().ShouldBeEmpty();
    _session.Selection.Count.ShouldBe(0);
    _session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void DragNewConnection_ReleaseOnOtherPort_RecordsOneStep()
  {
    var a = AddRoom(0, 0);
    var b = AddRoom(300, 0);
    var start = Port(a, CompassPoint.East).Position;
    var target = Port(b, CompassPoint.West).Position;
    _session.Undo.Clear();

    Move(start);
    Press(start);
    Move(start + new Vector(100, 0));
    Move(target);
    Release(target);

    var connection = _session.Project.Elements.OfType<Connection>().Single();
    connection.VertexList[1].Port.ShouldNotBeNull();
    _session.Selection.Contains(connection).ShouldBeTrue();
    _session.Undo.UndoTitle.ShouldBe("Add connection");

    _session.PerformUndo();
    _session.Project.Elements.OfType<Connection>().ShouldBeEmpty();
    _session.Undo.CanUndo.ShouldBeFalse();
  }

  // ---- pan / context menu ------------------------------------------------------------------

  [Test]
  public void RightDrag_Pans()
  {
    Press(new Vector(0, 0), PointerButton.Right);
    _interaction.Mode.ShouldBe(CanvasDragMode.Pan);
    Move(new Vector(100, 0));
    Release(new Vector(100, 0), PointerButton.Right);

    _interaction.Mode.ShouldBe(CanvasDragMode.None);
    _session.Viewport.Origin.X.ShouldBe(-100f, 0.5f);
    _session.Viewport.Origin.Y.ShouldBe(0f, 0.5f);
  }

  [Test]
  public void RightClickWithin2px_RaisesContextMenuRequested()
  {
    var raised = 0;
    var where = Vector.Zero;
    _interaction.ContextMenuRequested += world =>
    {
      raised++;
      where = world;
    };

    Press(new Vector(40, 50), PointerButton.Right);
    Move(new Vector(41, 51));
    Release(new Vector(41, 51), PointerButton.Right);

    raised.ShouldBe(1);
    where.X.ShouldBe(40f, 0.5f);
    where.Y.ShouldBe(50f, 0.5f);
  }

  [Test]
  public void RightDrag_MoreThan2px_DoesNotRaiseContextMenuRequested()
  {
    var raised = 0;
    _interaction.ContextMenuRequested += _ => raised++;

    Press(new Vector(0, 0), PointerButton.Right);
    Move(new Vector(10, 0));
    Release(new Vector(10, 0), PointerButton.Right);

    raised.ShouldBe(0);
  }

  [Test]
  public void MiddleDrag_Pans()
  {
    Press(new Vector(0, 0), PointerButton.Middle);
    Move(new Vector(0, 60));
    Release(new Vector(0, 60), PointerButton.Middle);

    _session.Viewport.Origin.Y.ShouldBe(-60f, 0.5f);
  }

  [Test]
  public void SpaceLeftDrag_Pans()
  {
    AddRoom(0, 0);
    _interaction.SetSpaceHeld(true);

    Press(new Vector(32, 32));
    _interaction.Mode.ShouldBe(CanvasDragMode.Pan);
    Move(new Vector(32, 82));
    Release(new Vector(32, 82));
    _interaction.SetSpaceHeld(false);

    _session.Viewport.Origin.Y.ShouldBe(-50f, 0.5f);
    _session.Selection.Count.ShouldBe(0);
  }

  [Test]
  public void SecondaryButtonRelease_DoesNotEndTheGesture()
  {
    AddRoom(0, 0);

    Move(new Vector(32, 32));
    Press(new Vector(32, 32));
    Release(new Vector(32, 32), PointerButton.Right);

    _interaction.Mode.ShouldBe(CanvasDragMode.MoveElement);
    Release(new Vector(32, 32));
    _interaction.Mode.ShouldBe(CanvasDragMode.None);
  }

  // ---- hover, wheel, keys ------------------------------------------------------------------

  [Test]
  public void Hover_SetsCursorAndAdorners()
  {
    var a = AddRoom(0, 0);

    var port = Port(a, CompassPoint.East).Position;
    Move(port);
    _interaction.Adorners.HoverPort.ShouldNotBeNull();
    _interaction.Adorners.Ports.Count.ShouldBeGreaterThan(0);
    _interaction.Adorners.Cursor.ShouldBe(CursorKind.DrawLine);

    Select(a);
    var corner = a.InnerBounds.GetCorner(CompassPoint.SouthEast);
    Move(corner);
    _interaction.Adorners.HoverHandle.ShouldNotBeNull();
    _interaction.Adorners.Handles.Count.ShouldBe(8);
    _interaction.Adorners.Cursor.ShouldBe(CursorKind.ResizeNWSE);

    Move(a.InnerBounds.GetCorner(CompassPoint.NorthEast));
    _interaction.Adorners.Cursor.ShouldBe(CursorKind.ResizeNESW);

    Move(new Vector(32, 32));
    _interaction.Adorners.HoverElement.ShouldBe(a);
    _interaction.Adorners.Cursor.ShouldBe(CursorKind.SizeAll);

    Press(new Vector(500, 500), PointerButton.Middle);
    _interaction.Adorners.Cursor.ShouldBe(CursorKind.Pan);
    Release(new Vector(500, 500), PointerButton.Middle);
  }

  [Test]
  public void Wheel_Zooms_AboutCursor_AndHonoursInvertSetting()
  {
    var settings = ApplicationSettingsController.AppSettings;
    var previous = settings.InvertMouseWheel;
    try
    {
      var client = new Vector(600, 200);
      var before = _session.Viewport.ClientToCanvas(client);

      settings.InvertMouseWheel = false;
      _interaction.OnWheel(new WheelInput(1, ModifierKeys.None, client));
      _session.Viewport.ZoomFactor.ShouldBeGreaterThan(1f);
      var after = _session.Viewport.ClientToCanvas(client);
      after.X.ShouldBe(before.X, 0.5f);
      after.Y.ShouldBe(before.Y, 0.5f);

      _session.Viewport.ResetZoom();
      settings.InvertMouseWheel = true;
      _interaction.OnWheel(new WheelInput(1, ModifierKeys.None, client));
      _session.Viewport.ZoomFactor.ShouldBeLessThan(1f);
    }
    finally
    {
      settings.InvertMouseWheel = previous;
    }
  }

  [Test]
  public void ModelReplaced_DuringDrag_ClearsHoverAndDragState()
  {
    AddRoom(0, 0);

    Move(new Vector(32, 32));
    _interaction.Adorners.HoverElement.ShouldNotBeNull();
    Press(new Vector(32, 32));
    Move(new Vector(82, 32));
    _interaction.Mode.ShouldBe(CanvasDragMode.MoveElement);

    _session.ReplaceProject(new Project());

    _interaction.Mode.ShouldBe(CanvasDragMode.None);
    _interaction.IsGestureActive.ShouldBeFalse();
    _interaction.Adorners.HoverElement.ShouldBeNull();
    _interaction.Adorners.Handles.Count.ShouldBe(0);
  }

  [Test]
  public void Escape_Idle_ClearsSelection()
  {
    var a = AddRoom(0, 0);
    Select(a);

    var handled = _interaction.OnKey(new KeyInput(InputKey.Escape, ModifierKeys.None));

    handled.ShouldBeTrue();
    _session.Selection.Count.ShouldBe(0);
  }

  [Test]
  public void Escape_DuringDrag_CancelsAndKeepsSelection()
  {
    var a = AddRoom(0, 0);
    var id = a.Id;
    Select(a);

    Move(new Vector(32, 32));
    Press(new Vector(32, 32));
    Move(new Vector(132, 32));
    _interaction.OnKey(new KeyInput(InputKey.Escape, ModifierKeys.None));

    _interaction.Mode.ShouldBe(CanvasDragMode.None);
    FindRoom(id).Position.ShouldBe(new Vector(0, 0));
    _session.Selection.Contains(FindRoom(id)).ShouldBeTrue();
  }

  [Test]
  public void ArrowKey_NudgesSelection()
  {
    var a = AddRoom(0, 0);
    Select(a);

    var handled = _interaction.OnKey(new KeyInput(InputKey.Right, ModifierKeys.None));

    handled.ShouldBeTrue();
    a.Position.X.ShouldBe(2f, 0.1f);
  }

  [Test]
  public void DoubleClickRoom_RaisesEditRequested()
  {
    var a = AddRoom(0, 0);
    Element requested = null;
    _interaction.EditRequested += element => requested = element;

    Move(new Vector(32, 32));
    Press(new Vector(32, 32), PointerButton.Left, ModifierKeys.None, 2);

    requested.ShouldBe(a);
    _session.Selection.Contains(a).ShouldBeTrue();
    _interaction.IsGestureActive.ShouldBeFalse();
  }
}
