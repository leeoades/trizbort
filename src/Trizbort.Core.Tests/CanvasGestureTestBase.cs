using System.Linq;
using NUnit.Framework;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Editor.Input;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

/// <summary>Shared harness for tests that drive <see cref="CanvasInteraction" /> with pointer input.</summary>
[NonParallelizable]
public abstract class CanvasGestureTestBase
{
  private Project _previousProject;
  private Project _project;
  private bool _previousSnap;
  private float _previousGrid;

  protected EditorSession Session { get; private set; }

  protected CanvasInteraction Interaction { get; private set; }

  protected Project CurrentProject => _project;

  [SetUp]
  public void GestureSetUp()
  {
    _previousSnap = Settings.SnapToGrid;
    _previousGrid = Settings.GridSize;
    Settings.SnapToGrid = false;
    Settings.GridSize = 32;

    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    Session = new EditorSession();
    Interaction = new CanvasInteraction(Session, new HitTester(Session));
  }

  [TearDown]
  public void GestureTearDown()
  {
    Interaction.Dispose();
    Session.Dispose();
    var current = Project.Current;
    Project.Current = _previousProject;
    if (current != null && current != _previousProject) current.Dispose();
    if (_project != current) _project.Dispose();
    Settings.SnapToGrid = _previousSnap;
    Settings.GridSize = _previousGrid;
  }

  protected Room AddRoom(float x, float y, float size = 64)
  {
    var room = new Room(_project) { Position = new Vector(x, y), Size = new Vector(size, size) };
    _project.Elements.Add(room);
    return room;
  }

  protected static Room.CompassPort PortOf(Room room, CompassPoint point)
  {
    return room.PortList.OfType<Room.CompassPort>().First(p => p.CompassPoint == point);
  }

  protected Connection AddConnection(Port from, Port to)
  {
    var connection = new Connection(_project, new Vertex(from), new Vertex(to));
    _project.Elements.Add(connection);
    return connection;
  }

  protected Vector Client(Vector world)
  {
    return Session.Viewport.CanvasToClient(world);
  }

  protected bool Press(Vector world, ModifierKeys modifiers = ModifierKeys.None, int clickCount = 1)
  {
    return Interaction.OnPointerPressed(new PointerInput(PointerButton.Left, modifiers, Client(world), clickCount));
  }

  protected void Move(Vector world, ModifierKeys modifiers = ModifierKeys.None)
  {
    Interaction.OnPointerMoved(new PointerInput(PointerButton.None, modifiers, Client(world), 0));
  }

  protected void Release(Vector world, ModifierKeys modifiers = ModifierKeys.None)
  {
    Interaction.OnPointerReleased(new PointerInput(PointerButton.Left, modifiers, Client(world), 0));
  }

  protected void Select(params Element[] elements)
  {
    Session.Selection.Set(elements);
  }

  // Undo and cancel replace the project, so tests look elements up again by id.
  protected Connection FindConnection(int id)
  {
    return Session.Project.Elements.OfType<Connection>().Single(c => c.Id == id);
  }

  protected Connection DrawConnection(Room from, Vector end)
  {
    var start = PortOf(from, CompassPoint.East).Position;
    Move(start);
    Press(start);
    Move(start + new Vector(100, 0));
    Move(end);
    Release(end);
    return Session.Project.Elements.OfType<Connection>().Single();
  }
}
