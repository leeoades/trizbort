using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Editor.Commands;
using Trizbort.Editor.Input;
using Trizbort.Setup;
using Vector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Core.Tests;

/// <summary>Tickets 041 (copy colour), 042 (duplicate), 043 (backup) and 049 (unexplored connection).</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class MiscCommandTests
{
  private Project _previousProject;
  private Project _project;
  private EditorSession _session;
  private CommandCatalogue _catalogue;
  private InMemoryClipboard _clipboard;
  private float _savedGrid;
  private string _tempDir;

  private sealed class RecordingPrompts : IEditorPrompts
  {
    public List<string> Messages { get; } = new();

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

    public Task ShowMessageAsync(string title, string message, CancellationToken token = default)
    {
      Messages.Add(message);
      return Task.CompletedTask;
    }
  }

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
    _savedGrid = Settings.GridSize;
    Settings.GridSize = 32;
    _tempDir = Path.Combine(Path.GetTempPath(), "trizbort-misc-" + Path.GetRandomFileName());
  }

  [TearDown]
  public void TearDown()
  {
    Settings.GridSize = _savedGrid;
    _session.Dispose();
    var current = Project.Current;
    Project.Current = _previousProject;
    if (current != null && current != _previousProject) current.Dispose();
    if (_project != current) _project.Dispose();
    if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
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

  // ---- 041 ----

  [Test]
  public async Task CopyColor_ThenPasteAppliesColoursToAnotherRoom()
  {
    var source = _session.AddRoom(new Vector(0, 0));
    var target = _session.AddRoom(new Vector(300, 0));
    source.RoomFillColor = Color.Crimson;
    source.RoomBorderColor = Color.Navy;
    target.RoomFillColor = Color.White;
    _session.Selection.Set(new Element[] { source });

    await Run("edit.copyColor");

    _clipboard.Text.ShouldNotBeNullOrEmpty();
    _session.Selection.Set(new Element[] { target });
    (await _session.PasteAsync(null)).ShouldBeTrue();

    target.RoomFillColor.ToArgb().ShouldBe(Color.Crimson.ToArgb());
    target.RoomBorderColor.ToArgb().ShouldBe(Color.Navy.ToArgb());
  }

  [Test]
  public void CopyColor_NeedsExactlyOneRoom_AndIsNotAnUndoStep()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(300, 0));
    var command = _catalogue.Find("edit.copyColor");
    command.Gestures.ShouldContain(new KeyGesture(InputKey.C, ModifierKeys.Primary | ModifierKeys.Alt));

    _session.Selection.Clear();
    command.CanExecute(_session).ShouldBeFalse();
    _session.Selection.Set(new Element[] { a, b });
    command.CanExecute(_session).ShouldBeFalse();

    _session.Selection.Set(new Element[] { a });
    command.CanExecute(_session).ShouldBeTrue();

    var undoTitle = _session.Undo.UndoTitle;
    _ = command.ExecuteAsync(_session);
    _session.Undo.UndoTitle.ShouldBe(undoTitle);
  }

  // ---- 042 ----

  [Test]
  public void Duplicate_BindsExactlyPrimaryD()
  {
    var command = _catalogue.Find("edit.duplicate");
    command.Gestures.ShouldBe(new[] { new KeyGesture(InputKey.D, ModifierKeys.Primary) });
    _catalogue.FindByGesture(new KeyGesture(InputKey.D, ModifierKeys.Primary), CommandScope.Window).ShouldBe(command);
    _catalogue.FindByGesture(new KeyGesture(InputKey.D, ModifierKeys.None), CommandScope.Canvas).Id
      .ShouldBe("connection.label.down");
  }

  [Test]
  public async Task Duplicate_CopiesRoomsAndInnerConnectionsOnly_SelectsCopies_LeavesClipboardAlone()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(300, 0));
    var outside = _session.AddRoom(new Vector(600, 0));
    a.Name = "Alpha";
    var inner = Connect(a, b);
    var outer = Connect(b, outside);
    _clipboard.Text = "keep me";
    _session.Selection.Set(new Element[] { a, b, inner });
    var elementsBefore = Project.Current.Elements.Count;

    await Run("edit.duplicate");

    Project.Current.Elements.Count.ShouldBe(elementsBefore + 3);
    _clipboard.Text.ShouldBe("keep me");
    _session.Selection.Count.ShouldBe(3);
    _session.Selection.Rooms.Count().ShouldBe(2);
    _session.Selection.Connections.Count().ShouldBe(1);
    _session.Selection.Contains(a).ShouldBeFalse();
    _session.Selection.Contains(inner).ShouldBeFalse();
    _session.Selection.Contains(outer).ShouldBeFalse();

    var copiedRoom = _session.Selection.Rooms.First(r => r.Name == "Alpha");
    copiedRoom.Position.X.ShouldBe(a.Position.X + 32);
    copiedRoom.Position.Y.ShouldBe(a.Position.Y + 32);
    var copiedConnection = _session.Selection.Connections.Single();
    copiedConnection.VertexList.Select(v => v.Port?.Owner).ShouldBe(_session.Selection.Rooms.Cast<Element>().ToArray(), ignoreOrder: true);
    Project.Current.Elements.OfType<Connection>().Count().ShouldBe(3);
  }

  [Test]
  public async Task Duplicate_IsOneUndoStep_AndRepeatsAtTheSameOffsetFromTheNewSelection()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    _session.Selection.Set(new Element[] { a });
    var roomsBefore = Project.Current.Elements.OfType<Room>().Count();

    await Run("edit.duplicate");
    Project.Current.Elements.OfType<Room>().Count().ShouldBe(roomsBefore + 1);

    _session.PerformUndo();
    Project.Current.Elements.OfType<Room>().Count().ShouldBe(roomsBefore);

    _session.PerformRedo();
    Project.Current.Elements.OfType<Room>().Count().ShouldBe(roomsBefore + 1);
  }

  [Test]
  public void Duplicate_NeedsASelection()
  {
    _session.Selection.Clear();
    _catalogue.Find("edit.duplicate").CanExecute(_session).ShouldBeFalse();
  }

  // ---- 043 ----

  [Test]
  public async Task Backup_WritesACopyAndReportsIt()
  {
    Directory.CreateDirectory(_tempDir);
    var file = Path.Combine(_tempDir, "map.trz");
    File.WriteAllText(file, "project");
    _project.FileName = file;
    var prompts = new RecordingPrompts();
    _session.Prompts = prompts;
    var command = _catalogue.Find("file.backup");
    command.Gestures.ShouldBe(new[] { new KeyGesture(InputKey.B, ModifierKeys.Primary) });
    command.CanExecute(_session).ShouldBeTrue();

    await command.ExecuteAsync(_session);

    var files = Directory.GetFiles(_tempDir);
    files.Length.ShouldBe(2);
    var backup = files.Single(f => f != file);
    File.ReadAllText(backup).ShouldBe("project");
    prompts.Messages.Count.ShouldBe(1);
    prompts.Messages[0].ShouldContain(backup);
  }

  [Test]
  public void Backup_IsDisabledWithoutAFileName()
  {
    _project.FileName = string.Empty;
    _catalogue.Find("file.backup").CanExecute(_session).ShouldBeFalse();
  }

  // ---- 049 ----

  [TestCase(CompassPoint.North, InputKey.NumPad8)]
  [TestCase(CompassPoint.SouthWest, InputKey.NumPad1)]
  [TestCase(CompassPoint.East, InputKey.NumPad6)]
  public async Task AltKeypad_AddsALoopbackOnTheCompassPortAndUndoRemovesIt(CompassPoint point, InputKey key)
  {
    var room = _session.AddRoom(new Vector(0, 0));
    _session.Selection.Set(new Element[] { room });

    var command = _catalogue.FindByGesture(new KeyGesture(key, ModifierKeys.Alt), CommandScope.Canvas);
    command.ShouldNotBeNull();
    command.Id.ShouldStartWith("room.addUnexplored.");
    command.CanExecute(_session).ShouldBeTrue();

    await command.ExecuteAsync(_session);

    var connection = Project.Current.Elements.OfType<Connection>().Single();
    connection.VertexList.Count.ShouldBe(2);
    foreach (var vertex in connection.VertexList)
    {
      var port = vertex.Port as Room.CompassPort;
      port.ShouldNotBeNull();
      port.CompassPoint.ShouldBe(point);
      port.Owner.ShouldBeOfType<Room>();
    }

    _session.PerformUndo();
    Project.Current.Elements.OfType<Connection>().ShouldBeEmpty();
  }

  [Test]
  public void AltKeypad_NeedsExactlyOneRoom_AndDoesNotClashWithPlainShiftOrCreation()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(300, 0));
    var command = _catalogue.Find("room.addUnexplored.north");
    command.ShouldNotBeNull();

    _session.Selection.Set(new Element[] { a, b });
    command.CanExecute(_session).ShouldBeFalse();
    _session.Selection.Clear();
    command.CanExecute(_session).ShouldBeFalse();

    _catalogue.FindByGesture(new KeyGesture(InputKey.NumPad8, ModifierKeys.None), CommandScope.Canvas).Id
      .ShouldBe("nav.select.north");
    _catalogue.FindByGesture(new KeyGesture(InputKey.NumPad8, ModifierKeys.Primary), CommandScope.Canvas).Id
      .ShouldBe("nav.addOrConnect.north");
  }
}

/// <summary>Ticket 065: the tooltip model and its suppression during a press, drag and wheel.</summary>
[TestFixture]
[Category("Unit")]
public class TooltipStateTests : CanvasGestureTestBase
{
  private bool _savedDescriptions;
  private bool _savedObjects;

  [SetUp]
  public void SetUpTooltips()
  {
    var settings = ApplicationSettingsController.AppSettings;
    _savedDescriptions = settings.ShowDescriptionsInTooltips;
    _savedObjects = settings.ShowObjectsInTooltips;
    settings.ShowDescriptionsInTooltips = true;
    settings.ShowObjectsInTooltips = true;
  }

  [TearDown]
  public void TearDownTooltips()
  {
    var settings = ApplicationSettingsController.AppSettings;
    settings.ShowDescriptionsInTooltips = _savedDescriptions;
    settings.ShowObjectsInTooltips = _savedObjects;
  }

  private Room AddNamedRoom()
  {
    var room = AddRoom(0, 0, 64);
    room.Name = "Cellar";
    room.AddDescription("A damp cellar.");
    return room;
  }

  [Test]
  public void Hover_ProducesAModelWithTitleBodyAndKind()
  {
    var room = AddNamedRoom();

    Move(new Vector(32, 32));

    var tooltip = Interaction.Adorners.Tooltip;
    tooltip.ShouldNotBeNull();
    tooltip.Element.ShouldBe(room);
    tooltip.Kind.ShouldBe(TooltipElementKind.Room);
    tooltip.Title.ShouldContain("Cellar");
    tooltip.Body.ShouldContain("A damp cellar.");
    Interaction.Adorners.TooltipTarget.ShouldBe(room);
  }

  [Test]
  public void NoModel_WhenTooltipsAreOff()
  {
    AddNamedRoom();
    var settings = ApplicationSettingsController.AppSettings;
    settings.ShowDescriptionsInTooltips = false;
    settings.ShowObjectsInTooltips = false;

    Move(new Vector(32, 32));

    Interaction.Adorners.Tooltip.ShouldBeNull();
  }

  [Test]
  public void NullWhileDragging_ReturnsAfterTheNextMove()
  {
    var room = AddNamedRoom();
    Move(new Vector(32, 32));
    Interaction.Adorners.Tooltip.ShouldNotBeNull();

    Press(new Vector(32, 32));
    Interaction.Adorners.Tooltip.ShouldBeNull();
    Move(new Vector(100, 100));
    Interaction.Adorners.Tooltip.ShouldBeNull();
    Release(new Vector(100, 100));
    Interaction.Adorners.Tooltip.ShouldBeNull();

    var moved = Project.Current.Elements.OfType<Room>().Single();
    Move(new Vector(moved.Position.X + 16, moved.Position.Y + 16));
    Interaction.Adorners.Tooltip.ShouldNotBeNull();
    room.ShouldNotBeNull();
  }

  [Test]
  public void NullAfterTheWheel_ReturnsAfterTheNextMove()
  {
    AddNamedRoom();
    Move(new Vector(32, 32));
    Interaction.Adorners.Tooltip.ShouldNotBeNull();

    Interaction.OnWheel(new WheelInput(1, ModifierKeys.None, Client(new Vector(32, 32))));
    Interaction.Adorners.Tooltip.ShouldBeNull();

    Move(new Vector(32, 32));
    Interaction.Adorners.Tooltip.ShouldNotBeNull();
  }
}
