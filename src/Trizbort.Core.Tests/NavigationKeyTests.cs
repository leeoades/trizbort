using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Editor.Commands;
using Trizbort.Editor.Input;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

/// <summary>Tickets 045 (F3 cycling), 047 (Shift navigation), 048 (Primary add-or-connect), 062 (Ctrl-click reference).</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class NavigationKeyTests
{
  private Project _previousProject;
  private Project _project;
  private EditorSession _session;
  private CommandCatalogue _catalogue;
  private CanvasInteraction _interaction;
  private ModifierKeys _previousCreation;
  private ModifierKeys _previousUnexplored;

  private sealed class RecordingPrompts : IEditorPrompts
  {
    public List<Element> Edited { get; } = new();

    public Task<Trizbort.Automap.AutomapSameDirectionResult> ChooseSameDirectionAsync(
      Room existing, string newRoomName, CancellationToken token = default)
    {
      return NonInteractiveEditorPrompts.Instance.ChooseSameDirectionAsync(existing, newRoomName, token);
    }

    public Task<RoomDisambiguation> DisambiguateRoomAsync(
      string transcriptLine, string roomDescription, IReadOnlyList<Room> candidates, CancellationToken token = default)
    {
      return NonInteractiveEditorPrompts.Instance.DisambiguateRoomAsync(transcriptLine, roomDescription, candidates, token);
    }

    public Task<bool> EditElementAsync(Element element, CancellationToken token = default)
    {
      Edited.Add(element);
      return Task.FromResult(true);
    }
  }

  [SetUp]
  public void SetUp()
  {
    _previousCreation = Settings.KeypadNavigationCreationModifier;
    _previousUnexplored = Settings.KeypadNavigationUnexploredModifier;
    Settings.KeypadNavigationCreationModifier = ModifierKeys.Control;
    Settings.KeypadNavigationUnexploredModifier = ModifierKeys.Alt;

    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _session = new EditorSession();
    _catalogue = CommandCatalogue.CreateDefault();
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
    Settings.KeypadNavigationCreationModifier = _previousCreation;
    Settings.KeypadNavigationUnexploredModifier = _previousUnexplored;
  }

  private Room AddRoomAt(float x, float y, string name = "Room")
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y), Size = new Vector(96, 64) };
    Project.Current.Elements.Add(room);
    return room;
  }

  private static Connection Link(Room from, CompassPoint fromPoint, Room to, CompassPoint toPoint)
  {
    var connection = new Connection(Project.Current,
      new Vertex(from.PortAt(fromPoint)),
      new Vertex(to.PortAt(toPoint)));
    Project.Current.Elements.Add(connection);
    return connection;
  }

  private void Select(params Element[] elements)
  {
    _session.Selection.Set(elements);
  }

  private async Task<bool> Press(InputKey key, ModifierKeys modifiers = ModifierKeys.None)
  {
    var command = _catalogue.FindByGesture(new KeyGesture(key, modifiers), CommandScope.Canvas);
    if (command == null || !command.CanExecute(_session)) return false;

    await command.ExecuteAsync(_session);
    return true;
  }

  private int ConnectionCount => _session.Project.Elements.OfType<Connection>().Count();

  private int RoomCount => _session.Project.Elements.OfType<Room>().Count();

  // Centre room with a neighbour on every side, each joined by a connection.
  private sealed class Hub
  {
    public Room Centre { get; init; }
    public Room North { get; init; }
    public Room South { get; init; }
    public Room East { get; init; }
    public Room West { get; init; }
    public Room NorthWest { get; init; }
    public Room SouthWest { get; init; }
    public Room NorthEast { get; init; }
    public Room SouthEast { get; init; }
    public Connection ToNorth { get; set; }
    public Connection ToNorthWest { get; set; }
  }

  private Hub BuildHub()
  {
    var hub = new Hub
    {
      Centre = AddRoomAt(0, 0, "Centre"),
      North = AddRoomAt(0, -300, "N"),
      South = AddRoomAt(0, 300, "S"),
      East = AddRoomAt(300, 0, "E"),
      West = AddRoomAt(-300, 0, "W"),
      NorthWest = AddRoomAt(-300, -300, "NW"),
      SouthWest = AddRoomAt(-300, 300, "SW"),
      NorthEast = AddRoomAt(300, -300, "NE"),
      SouthEast = AddRoomAt(300, 300, "SE")
    };
    hub.ToNorth = Link(hub.Centre, CompassPoint.North, hub.North, CompassPoint.South);
    Link(hub.Centre, CompassPoint.South, hub.South, CompassPoint.North);
    Link(hub.Centre, CompassPoint.East, hub.East, CompassPoint.West);
    Link(hub.Centre, CompassPoint.West, hub.West, CompassPoint.East);
    hub.ToNorthWest = Link(hub.Centre, CompassPoint.NorthWest, hub.NorthWest, CompassPoint.SouthEast);
    Link(hub.Centre, CompassPoint.SouthWest, hub.SouthWest, CompassPoint.NorthEast);
    Link(hub.Centre, CompassPoint.NorthEast, hub.NorthEast, CompassPoint.SouthWest);
    Link(hub.Centre, CompassPoint.SouthEast, hub.SouthEast, CompassPoint.NorthWest);
    return hub;
  }

  // ---- catalogue ----

  [Test]
  public void Catalogue_HasNoGestureClashes()
  {
    CatalogueValidator.Validate(_catalogue).ShouldBeEmpty();
  }

  [Test]
  public void KeypadGestures_CoverEightDirectionsAndSkipFive()
  {
    var keys = new[]
    {
      InputKey.NumPad1, InputKey.NumPad2, InputKey.NumPad3, InputKey.NumPad4,
      InputKey.NumPad6, InputKey.NumPad7, InputKey.NumPad8, InputKey.NumPad9
    };
    foreach (var key in keys)
    {
      foreach (var modifiers in new[] { ModifierKeys.None, ModifierKeys.Shift, ModifierKeys.Primary })
      {
        _catalogue.FindByGesture(new KeyGesture(key, modifiers), CommandScope.Canvas).ShouldNotBeNull($"{key} {modifiers}");
      }
    }

    _catalogue.FindByGesture(new KeyGesture(InputKey.NumPad5, ModifierKeys.None), CommandScope.Canvas).ShouldBeNull();
  }

  [Test]
  public void KeypadCreationModifier_FollowsTheSetting()
  {
    Settings.KeypadNavigationCreationModifier = ModifierKeys.Alt;
    Settings.KeypadNavigationUnexploredModifier = ModifierKeys.Control;
    var catalogue = CommandCatalogue.CreateDefault();

    var command = catalogue.FindByGesture(new KeyGesture(InputKey.NumPad2, ModifierKeys.Alt), CommandScope.Canvas);
    command.ShouldNotBeNull();
    command.Id.ShouldBe("nav.addOrConnect.south");
    CatalogueValidator.Validate(catalogue).ShouldBeEmpty();
  }

  // ---- 047 ----

  [Test]
  public async Task PlainKeypad_SelectsRoomInEachDirection()
  {
    var hub = BuildHub();
    var cases = new[]
    {
      (InputKey.NumPad8, hub.North), (InputKey.NumPad2, hub.South), (InputKey.NumPad6, hub.East),
      (InputKey.NumPad4, hub.West), (InputKey.NumPad7, hub.NorthWest), (InputKey.NumPad1, hub.SouthWest),
      (InputKey.NumPad9, hub.NorthEast), (InputKey.NumPad3, hub.SouthEast)
    };
    foreach (var (key, expected) in cases)
    {
      Select(hub.Centre);
      (await Press(key)).ShouldBeTrue(key.ToString());
      _session.Selection.Items.ShouldBe(new Element[] { expected }, key.ToString());
    }
  }

  [Test]
  public async Task PlainKeypad_CentresTheSelectedRoom()
  {
    var hub = BuildHub();
    Select(hub.Centre);

    await Press(InputKey.NumPad6);

    _session.Viewport.Origin.ShouldBe(hub.East.InnerBounds.Center);
  }

  [Test]
  public async Task PlainKeypad_AtTheEdgeDoesNothing()
  {
    var hub = BuildHub();
    Select(hub.North);

    await Press(InputKey.NumPad8);

    _session.Selection.Items.ShouldBe(new Element[] { hub.North });
  }

  [Test]
  public async Task ShiftArrow_SelectsConnectionFirstThenRoom()
  {
    var hub = BuildHub();
    Select(hub.Centre);

    await Press(InputKey.Up, ModifierKeys.Shift);
    _session.Selection.Items.ShouldBe(new Element[] { hub.ToNorth });

    await Press(InputKey.Up, ModifierKeys.Shift);
    _session.Selection.Items.ShouldBe(new Element[] { hub.North });
  }

  [Test]
  public async Task ShiftHome_SelectsNorthWestConnection()
  {
    var hub = BuildHub();
    Select(hub.Centre);

    await Press(InputKey.Home, ModifierKeys.Shift);
    _session.Selection.Items.ShouldBe(new Element[] { hub.ToNorthWest });

    await Press(InputKey.Home, ModifierKeys.Shift);
    _session.Selection.Items.ShouldBe(new Element[] { hub.NorthWest });
  }

  [TestCase(InputKey.Up, InputKey.NumPad8)]
  [TestCase(InputKey.Down, InputKey.NumPad2)]
  [TestCase(InputKey.Left, InputKey.NumPad4)]
  [TestCase(InputKey.Right, InputKey.NumPad6)]
  [TestCase(InputKey.Home, InputKey.NumPad7)]
  [TestCase(InputKey.End, InputKey.NumPad1)]
  [TestCase(InputKey.PageUp, InputKey.NumPad9)]
  [TestCase(InputKey.PageDown, InputKey.NumPad3)]
  public async Task ShiftKeypad_BehavesLikeShiftArrow(InputKey arrow, InputKey keypad)
  {
    var hub = BuildHub();
    Select(hub.Centre);
    await Press(arrow, ModifierKeys.Shift);
    var viaArrow = _session.Selection.Primary;

    Select(hub.Centre);
    await Press(keypad, ModifierKeys.Shift);

    viaArrow.ShouldBeOfType<Connection>();
    _session.Selection.Primary.ShouldBeSameAs(viaArrow);
  }

  [Test]
  public async Task ShiftArrow_WithoutConnectionFallsBackToRoom()
  {
    var centre = AddRoomAt(0, 0);
    Select(centre);

    (await Press(InputKey.Right, ModifierKeys.Shift)).ShouldBeTrue();

    _session.Selection.Items.ShouldBe(new Element[] { centre });
  }

  [Test]
  public async Task ShiftArrow_DoesNothingWithMultipleSelection()
  {
    var hub = BuildHub();
    Select(hub.Centre, hub.North);

    (await Press(InputKey.Right, ModifierKeys.Shift)).ShouldBeFalse();

    _session.Selection.Count.ShouldBe(2);
  }

  [Test]
  public void ShiftArrow_DoesNotNudgeTheSelection()
  {
    var room = AddRoomAt(0, 0);
    Select(room, AddRoomAt(500, 500));
    var before = room.Position;

    _interaction.OnKey(new KeyInput(InputKey.Right, ModifierKeys.Shift)).ShouldBeFalse();

    room.Position.ShouldBe(before);
  }

  // ---- 048 ----

  [Test]
  public async Task PrimaryArrow_SelectsExistingRoomInDirection()
  {
    var hub = BuildHub();
    Select(hub.Centre);
    var rooms = RoomCount;

    await Press(InputKey.Right, ModifierKeys.Primary);

    _session.Selection.Items.ShouldBe(new Element[] { hub.East });
    RoomCount.ShouldBe(rooms);
    _session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public async Task PrimaryArrow_ConnectsToRoomAtTargetPoint()
  {
    var a = AddRoomAt(0, 0, "A");
    var rect = a.InnerBounds;
    rect.Inflate(Settings.PreferredDistanceBetweenRooms + a.Width / 2, Settings.PreferredDistanceBetweenRooms + a.Height / 2);
    var target = rect.GetCorner(CompassPoint.East);
    AddRoomAt(target.X - 48, target.Y - 32, "B");
    Select(a);

    await Press(InputKey.Right, ModifierKeys.Primary);

    RoomCount.ShouldBe(2);
    ConnectionCount.ShouldBe(1);
    _session.Selection.Primary.ShouldBeOfType<Room>().Name.ShouldBe("B");
    a.GetConnections(CompassPoint.East).Count.ShouldBe(1);

    _session.PerformUndo();
    ConnectionCount.ShouldBe(0);
    _session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public async Task PrimaryArrow_CreatesNewRoomWithCopiedTraits()
  {
    var prompts = new RecordingPrompts();
    _session.Prompts = prompts;
    var a = AddRoomAt(0, 0, "A");
    a.Size = new Vector(128, 96);
    a.IsDark = true;
    a.Region = "Cellar";
    Select(a);

    await Press(InputKey.Down, ModifierKeys.Primary);

    RoomCount.ShouldBe(2);
    ConnectionCount.ShouldBe(1);
    var created = _session.Selection.Primary.ShouldBeOfType<Room>();
    created.ShouldNotBeSameAs(a);
    created.Size.ShouldBe(new Vector(128, 96));
    created.IsDark.ShouldBeTrue();
    created.Region.ShouldBe("Cellar");
    created.InnerBounds.Center.Y.ShouldBeGreaterThan(a.InnerBounds.Center.Y);
    prompts.Edited.ShouldBe(new Element[] { created });
    a.GetConnections(CompassPoint.South).Count.ShouldBe(1);
    _session.Viewport.Origin.ShouldBe(created.InnerBounds.Center);
  }

  [Test]
  public async Task PrimaryArrow_NewRoomAndConnectionAreOneUndoStep()
  {
    var a = AddRoomAt(0, 0, "A");
    Select(a);

    await Press(InputKey.Left, ModifierKeys.Primary);
    RoomCount.ShouldBe(2);

    _session.PerformUndo();

    RoomCount.ShouldBe(1);
    ConnectionCount.ShouldBe(0);
    _session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public async Task PrimaryKeypad_AddsRoomDiagonally()
  {
    var a = AddRoomAt(0, 0, "A");
    Select(a);

    await Press(InputKey.NumPad9, ModifierKeys.Primary);

    RoomCount.ShouldBe(2);
    var created = _session.Selection.Primary.ShouldBeOfType<Room>();
    created.InnerBounds.Center.X.ShouldBeGreaterThan(a.InnerBounds.Center.X);
    created.InnerBounds.Center.Y.ShouldBeLessThan(a.InnerBounds.Center.Y);
    a.GetConnections(CompassPoint.NorthEast).Count.ShouldBe(1);
  }

  [Test]
  public async Task PrimaryArrow_FromConnectionSelectsRoomAtItsEnd()
  {
    var hub = BuildHub();
    Select(hub.ToNorth);

    await Press(InputKey.Up, ModifierKeys.Primary);

    _session.Selection.Items.ShouldBe(new Element[] { hub.North });
    RoomCount.ShouldBe(9);
  }

  // ---- 045 ----

  [Test]
  public async Task F3_CyclesForwardWithWraparound()
  {
    var a = AddRoomAt(0, 0);
    var b = AddRoomAt(400, 0);
    var c = AddRoomAt(800, 0);
    Select(a, b, c);
    _session.Project.ActiveSelectedElement.ShouldBeSameAs(c);

    await Press(InputKey.F3);
    _session.Project.ActiveSelectedElement.ShouldBeSameAs(a);
    _session.Viewport.Origin.ShouldBe(a.InnerBounds.Center);

    await Press(InputKey.F3);
    _session.Project.ActiveSelectedElement.ShouldBeSameAs(b);

    await Press(InputKey.F3);
    _session.Project.ActiveSelectedElement.ShouldBeSameAs(c);
    _session.Selection.Count.ShouldBe(3);
  }

  [Test]
  public async Task ShiftF3_CyclesBackwardWithWraparound()
  {
    var a = AddRoomAt(0, 0);
    var b = AddRoomAt(400, 0);
    var c = AddRoomAt(800, 0);
    Select(a, b, c);

    await Press(InputKey.F3, ModifierKeys.Shift);
    _session.Project.ActiveSelectedElement.ShouldBeSameAs(b);

    await Press(InputKey.F3, ModifierKeys.Shift);
    _session.Project.ActiveSelectedElement.ShouldBeSameAs(a);

    await Press(InputKey.F3, ModifierKeys.Shift);
    _session.Project.ActiveSelectedElement.ShouldBeSameAs(c);
  }

  [Test]
  public async Task F3_SingleSelectionIsANoOp()
  {
    var a = AddRoomAt(0, 0);
    Select(a);
    var origin = _session.Viewport.Origin;

    (await Press(InputKey.F3)).ShouldBeFalse();

    _session.Project.ActiveSelectedElement.ShouldBeSameAs(a);
    _session.Viewport.Origin.ShouldBe(origin);
  }

  [Test]
  public void F3_WithOtherModifiersIsNotBound()
  {
    _catalogue.FindByGesture(new KeyGesture(InputKey.F3, ModifierKeys.Alt), CommandScope.Canvas).ShouldBeNull();
  }

  // ---- 062 ----

  private (Room original, Room reference) BuildReferencePair()
  {
    var original = AddRoomAt(1000, 1000, "Original");
    var reference = AddRoomAt(0, 0, "Ref");
    reference.AddReferenceRoom(original.Id);
    return (original, reference);
  }

  private Vector Client(Vector world)
  {
    return _session.Viewport.CanvasToClient(world);
  }

  private void ClickAt(Vector world, ModifierKeys modifiers)
  {
    _interaction.OnPointerMoved(new PointerInput(PointerButton.None, modifiers, Client(world), 0));
    _interaction.OnPointerPressed(new PointerInput(PointerButton.Left, modifiers, Client(world), 1));
    _interaction.OnPointerReleased(new PointerInput(PointerButton.Left, modifiers, Client(world), 0));
  }

  [Test]
  public void CtrlClick_OnSelectedReferenceRoomJumpsToOriginal()
  {
    var (original, reference) = BuildReferencePair();
    Select(reference);

    ClickAt(reference.InnerBounds.Center, ModifierKeys.Control);

    _session.Selection.Items.ShouldBe(new Element[] { original });
    _session.Viewport.Origin.ShouldBe(original.InnerBounds.Center);
  }

  [Test]
  public void Click_WithoutCtrlDoesNotJump()
  {
    var (original, reference) = BuildReferencePair();
    Select(reference);

    ClickAt(reference.InnerBounds.Center, ModifierKeys.None);

    _session.Selection.Items.ShouldBe(new Element[] { reference });
    _session.Selection.Contains(original).ShouldBeFalse();
  }

  [Test]
  public void CtrlClick_OnOrdinaryRoomDoesNotJump()
  {
    var original = AddRoomAt(1000, 1000, "Original");
    var plain = AddRoomAt(0, 0, "Plain");
    Select(plain);

    ClickAt(plain.InnerBounds.Center, ModifierKeys.Control);

    _session.Selection.Contains(original).ShouldBeFalse();
  }

  [Test]
  public void Cursor_IsHandOverReferenceRoomWithCtrl()
  {
    var (_, reference) = BuildReferencePair();
    var centre = reference.InnerBounds.Center;

    _interaction.OnPointerMoved(new PointerInput(PointerButton.None, ModifierKeys.Control, Client(centre), 0));
    _interaction.Adorners.Cursor.ShouldBe(CursorKind.Hand);

    _interaction.OnPointerMoved(new PointerInput(PointerButton.None, ModifierKeys.None, Client(centre), 0));
    _interaction.Adorners.Cursor.ShouldNotBe(CursorKind.Hand);
  }

  [Test]
  public void Cursor_IsNotHandOverOrdinaryRoomWithCtrl()
  {
    var plain = AddRoomAt(0, 0, "Plain");

    _interaction.OnPointerMoved(
      new PointerInput(PointerButton.None, ModifierKeys.Control, Client(plain.InnerBounds.Center), 0));

    _interaction.Adorners.Cursor.ShouldNotBe(CursorKind.Hand);
  }
}
