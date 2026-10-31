using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 133: I-029 (keyboard connections carry no up/down text) and I-030 (zoom to fit after a load).</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class ChecklistCanvasFixesTests
{
  private Project _previousProject;
  private Project _project;
  private EditorSession _session;

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _session = new EditorSession();
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

  private Room AddRoomAt(float x, float y)
  {
    var room = new Room(Project.Current) { Position = new Vector(x, y), Size = new Vector(96, 64) };
    Project.Current.Elements.Add(room);
    return room;
  }

  [TestCase(CompassPoint.East)]
  [TestCase(CompassPoint.North)]
  [TestCase(CompassPoint.South)]
  public async Task CtrlArrowConnectToExistingRoom_HasNoEndTextEvenWithALabelDefault(CompassPoint direction)
  {
    var a = AddRoomAt(0, 0);
    var inner = a.InnerBounds;
    inner.Inflate(Settings.PreferredDistanceBetweenRooms + a.Width / 2, Settings.PreferredDistanceBetweenRooms + a.Height / 2);
    var target = inner.GetCorner(direction);
    var b = AddRoomAt(target.X - 48, target.Y - 32);
    _session.NewConnection.Label = ConnectionLabel.Up;
    _session.Selection.Set(new Element[] { a });

    var handled = await _session.NavigateOrAddRoomAsync(direction);

    handled.ShouldBeTrue();
    var connection = Project.Current.Elements.OfType<Connection>().Single();
    connection.StartText.ShouldBeEmpty();
    connection.EndText.ShouldBeEmpty();
  }

  [Test]
  public async Task CtrlArrowNewRoom_HasNoEndTextEvenWithALabelDefault()
  {
    var a = AddRoomAt(0, 0);
    _session.NewConnection.Label = ConnectionLabel.Down;
    _session.Selection.Set(new Element[] { a });

    await _session.NavigateOrAddRoomAsync(CompassPoint.East);

    var connection = Project.Current.Elements.OfType<Connection>().Single();
    connection.StartText.ShouldBeEmpty();
    connection.EndText.ShouldBeEmpty();
  }

  [Test]
  public void NewProject_ResetsNewConnectionDefaults()
  {
    _session.NewConnection.Label = ConnectionLabel.Up;
    _session.NewConnection.Style = ConnectionStyle.Dashed;

    _session.NewProject();

    _session.NewConnection.Label.ShouldBe(ConnectionLabel.None);
    _session.NewConnection.Style.ShouldBe(ConnectionStyle.Solid);
  }

  [Test]
  public void OnMapLoaded_ZoomsToFitAndResetsDefaults()
  {
    AddRoomAt(0, 0);
    AddRoomAt(3000, 2000);
    _session.Viewport.SetSize(800, 600);
    _session.NewConnection.Label = ConnectionLabel.In;

    _session.OnMapLoaded();

    _session.Viewport.ZoomFactor.ShouldBeLessThan(0.5f);
    _session.Viewport.Origin.X.ShouldBe(1500f, 100f);
    _session.NewConnection.Label.ShouldBe(ConnectionLabel.None);
    _session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void OnMapLoaded_RefitsWhenTheViewportIsResized_UntilTheUserMovesTheView()
  {
    AddRoomAt(0, 0);
    AddRoomAt(3000, 2000);
    _session.OnMapLoaded();
    var small = _session.Viewport.ZoomFactor;

    _session.Viewport.SetSize(1600, 1200);
    _session.Viewport.ZoomFactor.ShouldBeGreaterThan(small);

    _session.Viewport.SetView(new Vector(10, 10), 1f);
    _session.Viewport.SetSize(400, 300);
    _session.Viewport.ZoomFactor.ShouldBe(1f);
  }
}
