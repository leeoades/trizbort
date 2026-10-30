using System.Collections.Generic;
using System.Linq;
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

/// <summary>Tickets 021, 030, 032, 038, 039 and 040: selection, line-style, delete and clipboard commands.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class ConnectionSelectionCommandTests
{
  private Project _previousProject;
  private Project _project;
  private EditorSession _session;
  private CommandCatalogue _catalogue;
  private InMemoryClipboard _clipboard;

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _session = new EditorSession();
    _clipboard = new InMemoryClipboard();
    _session.Clipboard = _clipboard;
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

  private static Connection Connect(Room from, Room to, CompassPoint fromPoint = CompassPoint.East, CompassPoint toPoint = CompassPoint.West)
  {
    var connection = new Connection(Project.Current,
      new Vertex(from.PortAt(fromPoint)),
      new Vertex(to.PortAt(toPoint)));
    Project.Current.Elements.Add(connection);
    return connection;
  }

  private static List<Connection> Connections()
  {
    return Project.Current.Elements.OfType<Connection>().ToList();
  }

  private Task Run(string id)
  {
    var command = _catalogue.Find(id);
    command.ShouldNotBeNull(id);
    return command.ExecuteAsync(_session);
  }

  private static EditorCommand FindByKey(CommandCatalogue catalogue, InputKey key, ModifierKeys modifiers, CommandScope scope)
  {
    return catalogue.FindByGesture(new KeyGesture(key, modifiers), scope);
  }

  // ---- 021 ----

  [Test]
  public async Task SelectSpecial_SelectsTheRightSets()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(300, 0));
    var c = _session.AddRoom(new Vector(600, 0));
    var d = _session.AddRoom(new Vector(900, 0));
    b.Objects = "lamp";
    var ab = Connect(a, b);
    var loop = Connect(c, c, CompassPoint.North, CompassPoint.South);
    var dangling = new Connection(Project.Current, new Vertex(a.PortAt(CompassPoint.North)), new Vertex(new Vector(0, -300)));
    Project.Current.Elements.Add(dangling);

    await Run("select.special.rooms");
    _session.Selection.Items.ShouldBe(new Element[] { a, b, c, d });

    await Run("select.special.unconnectedRooms");
    _session.Selection.Items.ShouldBe(new Element[] { d });

    await Run("select.special.roomsWithObjects");
    _session.Selection.Items.ShouldBe(new Element[] { b });

    await Run("select.special.roomsWithoutObjects");
    _session.Selection.Items.ShouldBe(new Element[] { a, c, d });

    await Run("select.special.connections");
    _session.Selection.Items.ShouldBe(new Element[] { ab, loop, dangling });

    await Run("select.special.danglingConnections");
    _session.Selection.Items.ShouldBe(new Element[] { dangling });

    await Run("select.special.selfLoopingConnections");
    _session.Selection.Items.ShouldBe(new Element[] { loop });
  }

  [Test]
  public void SelectSpecial_DisabledWhenNoCandidates()
  {
    _session.AddRoom(new Vector(0, 0));

    _catalogue.Find("select.special.connections").CanExecute(_session).ShouldBeFalse();
    _catalogue.Find("select.special.danglingConnections").CanExecute(_session).ShouldBeFalse();
    _catalogue.Find("select.special.rooms").CanExecute(_session).ShouldBeTrue();
  }

  [Test]
  public async Task SelectAllAndNone_CanExecuteFollowsSelection()
  {
    var selectAll = _catalogue.Find("edit.selectAll");
    var selectNone = _catalogue.Find("edit.selectNone");
    _session.AddRoom(new Vector(0, 0));
    _session.AddRoom(new Vector(300, 0));
    _session.ClearSelection();

    selectAll.CanExecute(_session).ShouldBeTrue();
    selectNone.CanExecute(_session).ShouldBeFalse();

    await selectAll.ExecuteAsync(_session);
    _session.Selection.Items.ShouldBe(Project.Current.Elements.ToList());
    selectAll.CanExecute(_session).ShouldBeFalse();
    selectNone.CanExecute(_session).ShouldBeTrue();

    await selectNone.ExecuteAsync(_session);
    _session.Selection.Count.ShouldBe(0);
  }

  [Test]
  public void SelectNone_IsEscapeOnCanvas()
  {
    FindByKey(_catalogue, InputKey.Escape, ModifierKeys.None, CommandScope.Canvas).Id.ShouldBe("edit.selectNone");
    FindByKey(_catalogue, InputKey.A, ModifierKeys.Primary, CommandScope.Window).Id.ShouldBe("edit.selectAll");
  }

  // ---- 030 ----

  [Test]
  public async Task ToggleDotted_AppliesToSelectionAndDefaults_AndToggles()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(300, 0));
    var connection = Connect(a, b);
    _session.Select(connection, false, false);

    await Run("connection.toggleDotted");
    connection.Style.ShouldBe(ConnectionStyle.Dashed);
    _session.NewConnection.Style.ShouldBe(ConnectionStyle.Dashed);
    _catalogue.Find("connection.toggleDotted").IsChecked(_session).ShouldBe(true);

    await Run("connection.toggleDotted");
    connection.Style.ShouldBe(ConnectionStyle.Solid);
    _session.NewConnection.Style.ShouldBe(ConnectionStyle.Solid);
    _catalogue.Find("connection.toggleDotted").IsChecked(_session).ShouldBe(false);
  }

  [Test]
  public async Task ToggleOneWay_WithNothingSelected_ChangesOnlyDefaults()
  {
    await Run("connection.toggleOneWay");

    _session.NewConnection.Flow.ShouldBe(ConnectionFlow.OneWay);
    _catalogue.Find("connection.toggleOneWay").IsChecked(_session).ShouldBe(true);
    await Run("connection.toggleOneWay");
    _session.NewConnection.Flow.ShouldBe(ConnectionFlow.TwoWay);
  }

  [Test]
  public async Task Plain_ResetsEverythingOnSelectionAndDefaults()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(300, 0));
    var connection = Connect(a, b);
    connection.Style = ConnectionStyle.Dashed;
    connection.Flow = ConnectionFlow.OneWay;
    connection.MidText = "mid";
    connection.SetText(ConnectionLabel.Up);
    connection.ConnectionColor = System.Drawing.Color.Red;
    _session.NewConnection.Style = ConnectionStyle.Dashed;
    _session.NewConnection.Flow = ConnectionFlow.OneWay;
    _session.NewConnection.Label = ConnectionLabel.Up;
    _session.Select(connection, false, false);

    await Run("connection.plain");

    connection.Style.ShouldBe(ConnectionStyle.Solid);
    connection.Flow.ShouldBe(ConnectionFlow.TwoWay);
    connection.MidText.ShouldBe(string.Empty);
    connection.StartText.ShouldBe(string.Empty);
    connection.EndText.ShouldBe(string.Empty);
    connection.ConnectionColor.ShouldBe(System.Drawing.Color.Transparent);
    _session.NewConnection.Style.ShouldBe(ConnectionStyle.Solid);
    _session.NewConnection.Flow.ShouldBe(ConnectionFlow.TwoWay);
    _session.NewConnection.Label.ShouldBe(ConnectionLabel.None);
  }

  [Test]
  public async Task ToggleDotted_Undo_RestoresConnectionButNotDefaults()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(300, 0));
    Connect(a, b);
    _session.Select(Connections().Single(), false, false);

    await Run("connection.toggleDotted");
    _session.PerformUndo();

    Connections().Single().Style.ShouldBe(ConnectionStyle.Solid);
    // Undo re-selects the restored connection, and selecting a single connection copies its style into the
    // new-connection defaults (reference rule, ticket 007), so the defaults follow it back to Solid.
    _session.NewConnection.Style.ShouldBe(ConnectionStyle.Solid);
  }

  [Test]
  public void LineStyleGestures_AreBareCanvasKeys()
  {
    FindByKey(_catalogue, InputKey.P, ModifierKeys.None, CommandScope.Canvas).Id.ShouldBe("connection.plain");
    FindByKey(_catalogue, InputKey.T, ModifierKeys.None, CommandScope.Canvas).Id.ShouldBe("connection.toggleDotted");
    FindByKey(_catalogue, InputKey.A, ModifierKeys.None, CommandScope.Canvas).Id.ShouldBe("connection.toggleOneWay");
    FindByKey(_catalogue, InputKey.P, ModifierKeys.Primary, CommandScope.Canvas).ShouldBeNull();
    _catalogue.Find("connection.toggleDotted").ShowInToolbar.ShouldBeTrue();
    _catalogue.Find("connection.toggleOneWay").ShowInToolbar.ShouldBeTrue();
  }

  // ---- 032 ----

  [Test]
  public async Task Reverse_SwapsSourceAndTarget_AndUndoes()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(300, 0));
    var connection = Connect(a, b);
    _session.Select(connection, false, false);

    await Run("connection.reverse");
    Connections().Single().GetSourceRoom().Id.ShouldBe(b.Id);
    Connections().Single().GetTargetRoom().Id.ShouldBe(a.Id);

    _session.PerformUndo();
    Connections().Single().GetSourceRoom().Id.ShouldBe(a.Id);
  }

  [Test]
  public void Reverse_NeedsAConnection_AndVIsNotPaste()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    _catalogue.Find("connection.reverse").CanExecute(_session).ShouldBeFalse();
    _session.Select(a, false, false);
    _catalogue.Find("connection.reverse").CanExecute(_session).ShouldBeFalse();

    FindByKey(_catalogue, InputKey.V, ModifierKeys.None, CommandScope.Canvas).Id.ShouldBe("connection.reverse");
    FindByKey(_catalogue, InputKey.V, ModifierKeys.Primary, CommandScope.Window).Id.ShouldBe("edit.paste");
  }

  [Test]
  public async Task Rotate_MovesTheConnectorOnePointPerPress()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(300, 0));
    var connection = Connect(a, b);
    _session.Select(connection, false, false);
    var start = (int)((Room.CompassPort)connection.VertexList[0].Port).CompassPoint;

    await Run("connection.rotate.cw");
    CompassAt(Connections().Single(), 0).ShouldBe((start + 1) % 16);

    await Run("connection.rotate.ccw");
    CompassAt(Connections().Single(), 0).ShouldBe(start);

    // The Primary variants turn the other end.
    var other = CompassAt(Connections().Single(), 1);
    await Run("connection.rotate.ctrlCw");
    CompassAt(Connections().Single(), 1).ShouldBe((other + 1) % 16);
    CompassAt(Connections().Single(), 0).ShouldBe(start);

    _session.PerformUndo();
    CompassAt(Connections().Single(), 1).ShouldBe(other);
  }

  private static int CompassAt(Connection connection, int vertex)
  {
    return (int)((Room.CompassPort)connection.VertexList[vertex].Port).CompassPoint;
  }

  [Test]
  public void Rotate_NeedsExactlyOneConnection_AndHasBracketGestures()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(300, 0));
    var first = Connect(a, b);
    var second = Connect(a, b, CompassPoint.North, CompassPoint.North);
    var rotate = _catalogue.Find("connection.rotate.cw");

    rotate.CanExecute(_session).ShouldBeFalse();
    _session.Select(first, false, false);
    rotate.CanExecute(_session).ShouldBeTrue();
    _session.Select(second, false, true);
    rotate.CanExecute(_session).ShouldBeFalse();

    FindByKey(_catalogue, InputKey.OemOpenBrackets, ModifierKeys.None, CommandScope.Canvas).Id.ShouldBe("connection.rotate.cw");
    FindByKey(_catalogue, InputKey.OemCloseBrackets, ModifierKeys.None, CommandScope.Canvas).Id.ShouldBe("connection.rotate.ccw");
    FindByKey(_catalogue, InputKey.OemOpenBrackets, ModifierKeys.Primary, CommandScope.Canvas).Id.ShouldBe("connection.rotate.ctrlCw");
    FindByKey(_catalogue, InputKey.OemCloseBrackets, ModifierKeys.Primary, CommandScope.Canvas).Id.ShouldBe("connection.rotate.ctrlCcw");
    FindByKey(_catalogue, InputKey.OemOpenBrackets, ModifierKeys.Shift, CommandScope.Canvas).ShouldBeNull();
  }

  // ---- 038 ----

  [Test]
  public void AddLabel_CentresOnPoint_SelectsAndUndoes()
  {
    var label = _session.AddLabel(new Vector(300, 300));

    (label.Position.X + label.Size.X / 2).ShouldBe(300, Settings.GridSize);
    (label.Position.Y + label.Size.Y / 2).ShouldBe(300, Settings.GridSize);
    _session.Selection.Primary.ShouldBeSameAs(label);

    _session.PerformUndo();
    Project.Current.Elements.OfType<MapLabel>().Count().ShouldBe(0);
  }

  [Test]
  public async Task AddLabelCommand_IsLOnCanvas_AndAddsALabel()
  {
    FindByKey(_catalogue, InputKey.L, ModifierKeys.None, CommandScope.Canvas).Id.ShouldBe("project.addLabel");

    await Run("project.addLabel");

    Project.Current.Elements.OfType<MapLabel>().Count().ShouldBe(1);
  }

  // ---- 039 ----

  [Test]
  public async Task Delete_RoomCascadesToConnections_AndUndoRestoresBoth()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(300, 0));
    Connect(a, b);
    _session.Select(a, false, false);

    await Run("edit.delete");

    Project.Current.Elements.OfType<Room>().Count().ShouldBe(1);
    Connections().Count.ShouldBe(0);

    _session.PerformUndo();
    Project.Current.Elements.OfType<Room>().Count().ShouldBe(2);
    Connections().Count.ShouldBe(1);
  }

  [Test]
  public async Task Backspace_DeletesLikeDelete_AndIsRegisteredOnce()
  {
    var all = _catalogue.All.SelectMany(c => c.Gestures.Select(g => (Command: c, Gesture: g))).ToList();
    all.Count(x => x.Gesture == new KeyGesture(InputKey.Back, ModifierKeys.None)).ShouldBe(1);
    all.Count(x => x.Gesture == new KeyGesture(InputKey.Delete, ModifierKeys.None)).ShouldBe(1);
    FindByKey(_catalogue, InputKey.Back, ModifierKeys.None, CommandScope.Canvas).ShouldNotBeNull();
    FindByKey(_catalogue, InputKey.Delete, ModifierKeys.None, CommandScope.Window).Id.ShouldBe("edit.delete");

    var room = _session.AddRoom(new Vector(0, 0));
    _session.Select(room, false, false);
    await FindByKey(_catalogue, InputKey.Back, ModifierKeys.None, CommandScope.Canvas).ExecuteAsync(_session);

    Project.Current.Elements.OfType<Room>().Count().ShouldBe(0);
  }

  // ---- 040 ----

  [Test]
  public async Task CopyPaste_RoundTripsRoomsAndConnections()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(300, 0));
    Connect(a, b);
    _session.SelectAll();

    await Run("edit.copy");
    _clipboard.Text.ShouldNotBeNullOrEmpty();
    await Run("edit.paste");

    Project.Current.Elements.OfType<Room>().Count().ShouldBe(4);
    Connections().Count.ShouldBe(2);
    _session.Selection.Count.ShouldBe(3);
    var pasted = _session.Selection.Rooms.ToList();
    pasted.Select(r => r.Id).ShouldNotContain(a.Id);
    pasted.Select(r => r.Id).ShouldNotContain(b.Id);
    pasted.Min(r => r.Position.X).ShouldBeGreaterThan(System.Math.Min(a.Position.X, b.Position.X));
    pasted.ShouldContain(_session.Selection.Connections.Single().GetSourceRoom());
  }

  [Test]
  public async Task Paste_RepeatedPastesStepFurther_AndIsOneUndoStep()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    _session.Select(a, false, false);
    await Run("edit.copy");

    await Run("edit.paste");
    var first = _session.Selection.Rooms.Single().Position;
    await Run("edit.paste");
    var second = _session.Selection.Rooms.Single().Position;

    second.X.ShouldBeGreaterThan(first.X);
    Project.Current.Elements.OfType<Room>().Count().ShouldBe(3);

    _session.PerformUndo();
    Project.Current.Elements.OfType<Room>().Count().ShouldBe(2);
  }

  [Test]
  public async Task Cut_RemovesSelection_AndPasteBringsItBack()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    _session.Select(a, false, false);

    await Run("edit.cut");
    Project.Current.Elements.OfType<Room>().Count().ShouldBe(0);

    await Run("edit.paste");
    Project.Current.Elements.OfType<Room>().Count().ShouldBe(1);

    _session.PerformUndo();
    _session.PerformUndo();
    Project.Current.Elements.OfType<Room>().Count().ShouldBe(1);
  }

  [Test]
  public async Task Paste_IsDisabledUnlessClipboardHoldsTrizbortData()
  {
    var paste = _catalogue.Find("edit.paste");
    _clipboard.Text = "hello";
    await _session.RefreshClipboardStateAsync();
    paste.CanExecute(_session).ShouldBeFalse();

    var room = _session.AddRoom(new Vector(0, 0));
    _session.Select(room, false, false);
    await Run("edit.copy");
    await _session.RefreshClipboardStateAsync();
    paste.CanExecute(_session).ShouldBeTrue();
  }

  [Test]
  public async Task Paste_OfGarbageDoesNothing()
  {
    _clipboard.Text = "not json";

    (await _session.PasteAsync(null)).ShouldBeFalse();

    Project.Current.Elements.Count.ShouldBe(0);
    _session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void ClipboardGestures_ArePrimaryCXV()
  {
    FindByKey(_catalogue, InputKey.C, ModifierKeys.Primary, CommandScope.Window).Id.ShouldBe("edit.copy");
    FindByKey(_catalogue, InputKey.X, ModifierKeys.Primary, CommandScope.Window).Id.ShouldBe("edit.cut");
    FindByKey(_catalogue, InputKey.V, ModifierKeys.Primary, CommandScope.Window).Id.ShouldBe("edit.paste");
  }

  [Test]
  public void Catalogue_HasNoGestureProblems()
  {
    CatalogueValidator.Validate(_catalogue).ShouldBeEmpty();
  }
}
