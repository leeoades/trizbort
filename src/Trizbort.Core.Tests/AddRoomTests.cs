using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Domain.Enums;
using Trizbort.Editor;
using Trizbort.Editor.Commands;
using Trizbort.Editor.Input;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

/// <summary>Tickets 037 and 115: Add Room with defaults, clash nudging and insert-into-connection.</summary>
[TestFixture]
[Category("Unit")]
public class AddRoomTests : RoomTestBase
{
  private bool _previousApplyStyle;

  [SetUp]
  public void SetUp()
  {
    _previousApplyStyle = ApplicationSettingsController.AppSettings.ApplyStyleToNewRooms;
    ApplicationSettingsController.AppSettings.ApplyStyleToNewRooms = false;
  }

  [TearDown]
  public void TearDown()
  {
    ApplicationSettingsController.AppSettings.ApplyStyleToNewRooms = _previousApplyStyle;
  }

  private static int RoomCount()
  {
    return Project.Current.Elements.OfType<Room>().Count();
  }

  private Connection Connect(Room from, CompassPoint fromPoint, Room to, CompassPoint toPoint)
  {
    var connection = new Connection(Project.Current, new Vertex(from.PortAt(fromPoint)), new Vertex(to.PortAt(toPoint)));
    Project.Current.Elements.Add(connection);
    return connection;
  }

  [Test]
  public void AddRoom_IsCentredOnThePointAndSnapped()
  {
    var room = Session.AddRoom(new Vector(500, 300));

    room.Position.ShouldBe(Settings.Snap(new Vector(500, 300) - room.Size / 2));
    Session.Selection.Primary.ShouldBe(room);
  }

  [Test]
  public void AddRoom_WithoutPoint_UsesViewportCentre()
  {
    var room = Session.AddRoom(null);

    room.Position.ShouldBe(Settings.Snap(Session.Viewport.Origin - room.Size / 2));
  }

  [Test]
  public void AddRoom_AtTheSamePointTwice_NudgesTheSecondRoom()
  {
    var first = Session.AddRoom(new Vector(500, 300));
    var second = Session.AddRoom(new Vector(500, 300));

    second.Position.ShouldNotBe(first.Position);
    RoomCount().ShouldBe(2);
  }

  [Test]
  public void AddRoom_CopiesSizeAndDarknessFromTheSelectedRoom()
  {
    var template = NewRoom("T", 0, 0);
    template.Size = new Vector(160, 96);
    template.IsDark = true;
    Select(template);

    var room = Session.AddRoom(new Vector(600, 600));

    room.Size.ShouldBe(new Vector(160, 96));
    room.IsDark.ShouldBeTrue();
  }

  [Test]
  public void AddRoom_IsOneUndoStep()
  {
    Session.AddRoom(new Vector(500, 300));

    Session.Undo.UndoTitle.ShouldBe("Add room");
    Session.PerformUndo();
    RoomCount().ShouldBe(0);
    Session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void AddRoomAsync_OpensPropertiesAndIsOneUndoStep()
  {
    var prompts = new RecordingPrompts();
    Session.Prompts = prompts;

    var room = Session.AddRoomAsync(new Vector(500, 300)).GetAwaiter().GetResult();

    prompts.Edited.ShouldBe(room);
    RoomCount().ShouldBe(1);
    Session.PerformUndo();
    RoomCount().ShouldBe(0);
    Session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void AddLabelAsync_OpensPropertiesSelectsAndIsOneUndoStep()
  {
    var prompts = new RecordingPrompts();
    Session.Prompts = prompts;

    var label = Session.AddLabelAsync(new Vector(500, 300)).GetAwaiter().GetResult();

    prompts.Edited.ShouldBe(label);
    Session.Selection.Primary.ShouldBe(label);
    Session.Project.Elements.OfType<MapLabel>().Count().ShouldBe(1);
    Session.PerformUndo();
    Session.Project.Elements.OfType<MapLabel>().Count().ShouldBe(0);
    Session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void AddRoomAsync_CancelledDialog_KeepsTheRoom()
  {
    Session.Prompts = new RecordingPrompts();

    Session.AddRoomAsync(new Vector(500, 300)).GetAwaiter().GetResult();

    RoomCount().ShouldBe(1);
  }

  [Test]
  public void AddRoom_WithConnectionSelected_SplicesTheRoomIn()
  {
    var a = NewRoom("A", 0, 0);
    var b = NewRoom("B", 600, 0);
    a.Region = "Cave";
    b.Region = "Cave";
    var connection = Connect(a, CompassPoint.East, b, CompassPoint.West);
    Select(connection);

    var room = Session.AddRoom(new Vector(300, 0));

    Project.Current.Elements.ShouldNotContain(connection);
    room.Region.ShouldBe("Cave");
    var connections = Project.Current.Elements.OfType<Connection>().ToList();
    connections.Count.ShouldBe(2);
    var intoRoom = connections.Single(c => c.VertexList[1].Port.Owner == room);
    var outOfRoom = connections.Single(c => c.VertexList[0].Port.Owner == room);
    intoRoom.VertexList[0].Port.Owner.ShouldBe(a);
    outOfRoom.VertexList[1].Port.Owner.ShouldBe(b);
    Session.Selection.Primary.ShouldBe(room);

    Session.PerformUndo();
    RoomCount().ShouldBe(2);
    Project.Current.Elements.OfType<Connection>().Count().ShouldBe(1);
  }

  [Test]
  public void AddRoom_CommandKeyIsBareR()
  {
    var command = Catalogue.FindByGesture(
      new KeyInput(InputKey.R, ModifierKeys.None),
      CommandScope.Canvas);

    command.Id.ShouldBe("project.addRoom");
    command.ExecuteAsync(Session).GetAwaiter().GetResult();
    RoomCount().ShouldBe(1);
  }

  private sealed class RecordingPrompts : IEditorPrompts
  {
    public Element Edited { get; private set; }

    public Task<bool> EditElementAsync(Element element, CancellationToken token = default)
    {
      Edited = element;
      return Task.FromResult(false);
    }

    public Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(
      Room existing, string newRoomName, CancellationToken token = default)
    {
      return NonInteractiveEditorPrompts.Instance.ChooseSameDirectionAsync(existing, newRoomName, token);
    }

    public Task<RoomDisambiguation> DisambiguateRoomAsync(
      string transcriptLine,
      string roomDescription,
      IReadOnlyList<Room> candidates,
      CancellationToken token = default)
    {
      return NonInteractiveEditorPrompts.Instance.DisambiguateRoomAsync(
        transcriptLine, roomDescription, candidates, token);
    }
  }
}
