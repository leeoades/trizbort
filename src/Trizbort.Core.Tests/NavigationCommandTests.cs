using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Editor.Commands;
using Trizbort.Editor.Input;

namespace Trizbort.Core.Tests;

/// <summary>Tickets 020, 022, 023, 028 and 031.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class NavigationCommandTests
{
  private Project _previousProject;
  private Project _project;
  private EditorSession _session;
  private CommandCatalogue _catalogue;

  private sealed class RecordingPrompts : IEditorPrompts
  {
    public List<Element> Edited { get; } = new();

    public bool Accept { get; set; }

    public System.Action<Element> OnEdit { get; set; }

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
      OnEdit?.Invoke(element);
      return Task.FromResult(Accept);
    }
  }

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _session = new EditorSession();
    _catalogue = CommandCatalogue.CreateDefault();
  }

  [TearDown]
  public void TearDown()
  {
    _session.Dispose();
    var current = Project.Current;
    Project.Current = _previousProject;
    if (current != null && current != _previousProject) current.Dispose();
    if (_project != current) _project.Dispose();
  }

  private Room AddRoomAt(float x, float y, string name = "Room")
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y), Size = new Vector(96, 64) };
    Project.Current.Elements.Add(room);
    return room;
  }

  private Task Run(string id)
  {
    var command = _catalogue.Find(id);
    command.ShouldNotBeNull(id);
    return command.ExecuteAsync(_session);
  }

  private static Connection Connect(Room from, Room to)
  {
    var connection = new Connection(Project.Current,
      new Vertex(from.PortAt(CompassPoint.East)),
      new Vertex(to.PortAt(CompassPoint.West)));
    Project.Current.Elements.Add(connection);
    return connection;
  }

  // ---- 020 ----

  [TestCase(0.5f, 800, 600)]
  [TestCase(1f, 800, 600)]
  [TestCase(2f, 1200, 300)]
  public void CenterOnElement_MovesOriginToRoomCentreKeepingZoom(float zoom, double width, double height)
  {
    var room = AddRoomAt(300, -200);
    _session.Viewport.SetSize(width, height);
    _session.Viewport.SetView(Vector.Zero, zoom);

    _session.CenterOnElement(room);

    _session.Viewport.Origin.ShouldBe(room.InnerBounds.Center);
    _session.Viewport.ZoomFactor.ShouldBe(zoom, 0.001f);
  }

  [Test]
  public void FindRoomNearestViewportCentre_PicksClosestRoom()
  {
    AddRoomAt(0, 0, "Far");
    var near = AddRoomAt(1000, 1000, "Near");
    _session.Viewport.SetView(new Vector(1048, 1032), 1f);

    _session.FindRoomNearestViewportCentre().ShouldBeSameAs(near);
  }

  [Test]
  public void FindRoomNearestViewportCentre_TieKeepsFirstRoom()
  {
    var left = AddRoomAt(-200, 0, "Left");
    AddRoomAt(104, 0, "Right");
    _session.Viewport.SetView(new Vector(left.InnerBounds.Center.X + 152, left.InnerBounds.Center.Y), 1f);

    _session.FindRoomNearestViewportCentre().ShouldBeSameAs(left);
  }

  [Test]
  public void FindRoomNearestViewportCentre_EmptyMapReturnsNull()
  {
    _session.FindRoomNearestViewportCentre().ShouldBeNull();
  }

  // ---- 022 ----

  [Test]
  public async Task SelectRegions_SelectsRoomsInRegionsOfSelectedRooms()
  {
    var a1 = AddRoomAt(0, 0, "A1");
    var a2 = AddRoomAt(200, 0, "A2");
    var b1 = AddRoomAt(400, 0, "B1");
    a1.Region = "North";
    a2.Region = "North";
    b1.Region = "South";
    _session.Selection.Set(new Element[] { a1 });

    await Run("select.regions");

    _session.Selection.Items.ShouldContain(a1);
    _session.Selection.Items.ShouldContain(a2);
    _session.Selection.Items.ShouldNotContain(b1);
  }

  [Test]
  public void SelectRegions_NeedsASelectedRoom_AndGestureIsCtrlShiftA()
  {
    AddRoomAt(0, 0);
    var command = _catalogue.Find("select.regions");
    command.CanExecute(_session).ShouldBeFalse();

    var found = _catalogue.FindByGesture(
      new KeyGesture(InputKey.A, Trizbort.Domain.Enums.ModifierKeys.Primary | Trizbort.Domain.Enums.ModifierKeys.Shift),
      CommandScope.Canvas);
    found.ShouldBeSameAs(command);
  }

  [Test]
  public async Task SelectStartRoom_SelectsAndCentres()
  {
    AddRoomAt(0, 0, "Other");
    var start = AddRoomAt(900, 500, "Start");
    start.IsStartRoom = true;
    _session.Viewport.SetView(Vector.Zero, 1f);

    await Run("select.startRoom");

    _session.Selection.Items.ShouldBe(new Element[] { start });
    _session.Viewport.Origin.ShouldBe(start.InnerBounds.Center);
  }

  [Test]
  public void SelectStartRoom_DisabledWithoutStartRoom()
  {
    AddRoomAt(0, 0);
    _catalogue.Find("select.startRoom").CanExecute(_session).ShouldBeFalse();
    _catalogue.FindByGesture(new KeyGesture(InputKey.NumPad0, Trizbort.Domain.Enums.ModifierKeys.None), CommandScope.Canvas)
      .Id.ShouldBe("select.startRoom");
  }

  [Test]
  public async Task SelectNearestToCentre_SelectsAndCentresNearestRoom()
  {
    AddRoomAt(0, 0, "A");
    var b = AddRoomAt(2000, 0, "B");
    _session.Viewport.SetView(new Vector(1900, 0), 1f);

    await Run("select.nearestToCentre");

    _session.Selection.Items.ShouldBe(new Element[] { b });
    _session.Viewport.Origin.ShouldBe(b.InnerBounds.Center);
  }

  // ---- 023 ----

  [Test]
  public async Task Enter_WithNothingSelected_SelectsNearestRoom()
  {
    var a = AddRoomAt(0, 0);
    _session.Viewport.SetView(Vector.Zero, 1f);

    await Run("edit.enter");

    _session.Selection.Items.ShouldBe(new Element[] { a });
  }

  [Test]
  public async Task Enter_WithOneSelected_OpensPropertiesOfThatElement()
  {
    var a = AddRoomAt(0, 0);
    var prompts = new RecordingPrompts();
    _session.Prompts = prompts;
    _session.Selection.Set(new Element[] { a });

    await Run("edit.enter");

    prompts.Edited.ShouldBe(new Element[] { a });
  }

  [Test]
  public void Properties_CanExecuteOnlyForSingleElementWithDialog()
  {
    var a = AddRoomAt(0, 0);
    var b = AddRoomAt(200, 0);
    var command = _catalogue.Find("edit.properties");

    command.CanExecute(_session).ShouldBeFalse();
    _session.Selection.Set(new Element[] { a });
    command.CanExecute(_session).ShouldBeTrue();
    _session.Selection.Set(new Element[] { a, b });
    command.CanExecute(_session).ShouldBeFalse();
  }

  [Test]
  public async Task Rename_OpensRoomPropertiesAndAcceptedEditIsOneUndoStep()
  {
    var a = AddRoomAt(0, 0, "Before");
    var prompts = new RecordingPrompts { Accept = true, OnEdit = element => ((Room)element).Name = "After" };
    _session.Prompts = prompts;
    _session.Selection.Set(new Element[] { a });
    _session.Undo.Clear();

    await Run("room.rename");

    prompts.Edited.ShouldBe(new Element[] { a });
    _session.Undo.CanUndo.ShouldBeTrue();
    _session.PerformUndo();
    Project.Current.Elements.OfType<Room>().Single().Name.ShouldBe("Before");
  }

  [Test]
  public async Task Properties_CancelLeavesUndoStackUnchanged()
  {
    var a = AddRoomAt(0, 0);
    _session.Prompts = new RecordingPrompts { Accept = false };
    _session.Selection.Set(new Element[] { a });
    _session.Undo.Clear();

    await Run("edit.properties");

    _session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void Rename_OnlyForRooms()
  {
    var a = AddRoomAt(0, 0);
    var b = AddRoomAt(200, 0);
    var connection = Connect(a, b);
    var command = _catalogue.Find("room.rename");

    _session.Selection.Set(new Element[] { connection });
    command.CanExecute(_session).ShouldBeFalse();
    _session.Selection.Set(new Element[] { a });
    command.CanExecute(_session).ShouldBeTrue();
  }

  // ---- 031 ----

  [TestCase("connection.label.up", ConnectionLabel.Up)]
  [TestCase("connection.label.down", ConnectionLabel.Down)]
  [TestCase("connection.label.in", ConnectionLabel.In)]
  [TestCase("connection.label.out", ConnectionLabel.Out)]
  public async Task LabelCommands_SetLabelDefaultAndCheckMark(string id, ConnectionLabel label)
  {
    var a = AddRoomAt(0, 0);
    var b = AddRoomAt(200, 0);
    var connection = Connect(a, b);
    _session.Selection.Set(new Element[] { connection });
    _session.Undo.Clear();
    var command = _catalogue.Find(id);

    command.IsChecked(_session).ShouldBe(false);
    await command.ExecuteAsync(_session);

    Connection.GetText(label, out var start, out var end);
    connection.StartText.ShouldBe(start);
    connection.EndText.ShouldBe(end);
    _session.NewConnection.Label.ShouldBe(label);
    command.IsChecked(_session).ShouldBe(true);
  }

  [Test]
  public async Task LabelCommand_UndoRestoresLabel()
  {
    var a = AddRoomAt(0, 0);
    var b = AddRoomAt(200, 0);
    var connection = Connect(a, b);
    _session.Selection.Set(new Element[] { connection });
    _session.Undo.Clear();

    await Run("connection.label.up");
    _session.Undo.CanUndo.ShouldBeTrue();
    _session.PerformUndo();

    var restored = Project.Current.Elements.OfType<Connection>().Single();
    EditorSession.LabelOf(restored).ShouldBe(ConnectionLabel.None);
  }

  [Test]
  public void LabelCommands_NeedAConnectionAndBareKeys()
  {
    AddRoomAt(0, 0);
    _catalogue.Find("connection.label.down").CanExecute(_session).ShouldBeFalse();
    var gesture = new KeyGesture(InputKey.D, Trizbort.Domain.Enums.ModifierKeys.None);
    _catalogue.FindByGesture(gesture, CommandScope.Canvas).Id.ShouldBe("connection.label.down");
    _catalogue.FindByGesture(new KeyGesture(InputKey.D, Trizbort.Domain.Enums.ModifierKeys.Primary), CommandScope.Canvas)
      .ShouldBeNull();
  }

  // ---- 028 ----

  [TestCase(300f, 0f, CompassPoint.East, CompassPoint.West)]
  [TestCase(-300f, 0f, CompassPoint.West, CompassPoint.East)]
  [TestCase(0f, 300f, CompassPoint.South, CompassPoint.North)]
  [TestCase(0f, -300f, CompassPoint.North, CompassPoint.South)]
  [TestCase(300f, 100f, CompassPoint.East, CompassPoint.West)]
  [TestCase(100f, 300f, CompassPoint.South, CompassPoint.North)]
  public async Task Join_ChoosesFacingPorts(float dx, float dy, CompassPoint first, CompassPoint second)
  {
    var a = AddRoomAt(0, 0, "A");
    var b = AddRoomAt(dx, dy, "B");
    _session.Selection.Set(new Element[] { a, b });

    await Run("room.join");

    var connection = Project.Current.Elements.OfType<Connection>().Single();
    var source = (Room.CompassPort)connection.VertexList[0].Port;
    var target = (Room.CompassPort)connection.VertexList[1].Port;
    connection.GetSourceRoom().ShouldBeSameAs(a);
    connection.GetTargetRoom().ShouldBeSameAs(b);
    source.CompassPoint.ShouldBe(first);
    target.CompassPoint.ShouldBe(second);
  }

  [Test]
  public void Join_DisabledWhenAlreadyConnectedOrNotTwoRooms()
  {
    var a = AddRoomAt(0, 0);
    var b = AddRoomAt(300, 0);
    var c = AddRoomAt(600, 0);
    var command = _catalogue.Find("room.join");

    _session.Selection.Set(new Element[] { a });
    command.CanExecute(_session).ShouldBeFalse();
    _session.Selection.Set(new Element[] { a, b, c });
    command.CanExecute(_session).ShouldBeFalse();
    _session.Selection.Set(new Element[] { a, b });
    command.CanExecute(_session).ShouldBeTrue();

    Connect(a, b);
    command.CanExecute(_session).ShouldBeFalse();
  }

  [Test]
  public async Task Join_AppliesNewConnectionDefaults_AndUndoRemovesOnlyTheConnection()
  {
    var a = AddRoomAt(0, 0);
    var b = AddRoomAt(300, 0);
    _session.NewConnection.Style = ConnectionStyle.Dashed;
    _session.NewConnection.Flow = ConnectionFlow.OneWay;
    _session.NewConnection.Label = ConnectionLabel.Up;
    _session.Selection.Set(new Element[] { a, b });
    _session.Undo.Clear();

    await Run("room.join");

    var connection = Project.Current.Elements.OfType<Connection>().Single();
    connection.Style.ShouldBe(ConnectionStyle.Dashed);
    connection.Flow.ShouldBe(ConnectionFlow.OneWay);
    EditorSession.LabelOf(connection).ShouldBe(ConnectionLabel.Up);

    _session.PerformUndo();
    Project.Current.Elements.OfType<Connection>().Count().ShouldBe(0);
    Project.Current.Elements.OfType<Room>().Count().ShouldBe(2);
  }

  [Test]
  public void Catalogue_HasNoGestureProblems()
  {
    CatalogueValidator.Validate(_catalogue).ShouldBeEmpty();
  }
}
