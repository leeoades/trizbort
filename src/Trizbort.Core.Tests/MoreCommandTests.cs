using System.Collections.Generic;
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

namespace Trizbort.Core.Tests;

/// <summary>Tickets 024, 033, 035, 036 and 054.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class MoreCommandTests
{
  private Project _previousProject;
  private Project _project;
  private EditorSession _session;
  private CommandCatalogue _catalogue;
  private List<Region> _savedRegions;
  private int _savedDetail;
  private bool _savedTextFlag;

  private sealed class FixedRegionPrompts : IEditorPrompts
  {
    public string Choice { get; set; }

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

    public Task<string> ChooseRegionAsync(IReadOnlyList<string> regions, string current, CancellationToken token = default)
    {
      return Task.FromResult(Choice);
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
    _savedRegions = Settings.Regions;
    Settings.Regions = new List<Region>
    {
      new Region { RegionName = Region.DefaultRegion },
      new Region { RegionName = "Zebra" },
      new Region { RegionName = "Alpha" }
    };
    _savedDetail = ApplicationSettingsController.AppSettings.PortAdjustDetail;
    _savedTextFlag = ApplicationSettingsController.AppSettings.DebugDisableTextRendering;
  }

  [TearDown]
  public void TearDown()
  {
    Settings.Regions = _savedRegions;
    ApplicationSettingsController.AppSettings.PortAdjustDetail = _savedDetail;
    ApplicationSettingsController.AppSettings.DebugDisableTextRendering = _savedTextFlag;
    _session.Dispose();
    var current = Project.Current;
    Project.Current = _previousProject;
    if (current != null && current != _previousProject) current.Dispose();
    if (_project != current) _project.Dispose();
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

  // ---- 024 ----

  [Test]
  public void RegionNames_DefaultFirstThenAlphabetical()
  {
    _session.RegionNames().ShouldBe(new[] { Region.DefaultRegion, "Alpha", "Zebra" });
  }

  [Test]
  public async Task ChangeRegion_SetsAllSelectedRoomsAndUndoRestores()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(300, 0));
    _session.Selection.Set(new Element[] { a, b });
    _session.Prompts = new FixedRegionPrompts { Choice = "Alpha" };

    await Run("room.changeRegion");

    a.Region.ShouldBe("Alpha");
    b.Region.ShouldBe("Alpha");

    _session.PerformUndo();
    var rooms = Project.Current.Elements.OfType<Room>().ToList();
    rooms.ShouldAllBe(r => r.Region != "Alpha");
  }

  [Test]
  public async Task ChangeRegion_CancelledPickerChangesNothing()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    _session.Selection.Set(new Element[] { a });
    _session.Prompts = new FixedRegionPrompts { Choice = null };

    var regionBefore = a.Region;
    var stepBefore = _session.Undo.UndoTitle;
    await Run("room.changeRegion");

    _session.Undo.UndoTitle.ShouldBe(stepBefore);
    a.Region.ShouldBe(regionBefore);
  }

  [Test]
  public void ChangeRegion_DisabledWithSingleRegion()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    _session.Selection.Set(new Element[] { a });
    Settings.Regions = new List<Region> { new Region { RegionName = Region.DefaultRegion } };

    _catalogue.Find("room.changeRegion").CanExecute(_session).ShouldBeFalse();
  }

  [Test]
  public void ChangeRegion_GestureIsShiftF2()
  {
    var command = _catalogue.FindByGesture(new KeyGesture(InputKey.F2, ModifierKeys.Shift), CommandScope.Canvas);
    command.ShouldNotBeNull();
    command.Id.ShouldBe("room.changeRegion");
  }

  // ---- 033 ----

  [Test]
  public void CyclePortDetail_WrapsAndPostsMessage()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    _session.Selection.Set(new Element[] { a });
    ApplicationSettingsController.AppSettings.PortAdjustDetail = 1;

    _session.CyclePortDetail();
    ApplicationSettingsController.AppSettings.PortAdjustDetail.ShouldBe(2);
    _session.StatusMessage.ShouldBe("Port adjust detail: 16 ports");

    _session.CyclePortDetail();
    ApplicationSettingsController.AppSettings.PortAdjustDetail.ShouldBe(0);
    _session.StatusMessage.ShouldBe("Port adjust detail: 4 ports");
  }

  [Test]
  public async Task AdjustPorts_MovesPortsTowardsTheOtherRoom()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(400, 0));
    var connection = new Connection(Project.Current,
      new Vertex(a.PortAt(CompassPoint.North)),
      new Vertex(b.PortAt(CompassPoint.South)));
    Project.Current.Elements.Add(connection);
    _session.Selection.Set(new Element[] { a });
    ApplicationSettingsController.AppSettings.PortAdjustDetail = 0;

    await Run("room.adjustPorts");

    connection.GetSourceRoom().ShouldBe(a);
    ((Room.CompassPort)connection.VertexList[0].Port).CompassPoint.ShouldBe(CompassPoint.East);
    ((Room.CompassPort)connection.VertexList[1].Port).CompassPoint.ShouldBe(CompassPoint.West);
  }

  [Test]
  public async Task DeleteAllConnections_RemovesThemAndUndoRestores()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(400, 0));
    var c = _session.AddRoom(new Vector(800, 0));
    Connect(a, b);
    Connect(b, c);
    _session.Selection.Set(new Element[] { b });

    await Run("room.deleteAllConnections");
    Project.Current.Elements.OfType<Connection>().Count().ShouldBe(0);

    _session.PerformUndo();
    Project.Current.Elements.OfType<Connection>().Count().ShouldBe(2);
  }

  [Test]
  public void PortCommands_RequireExactlyOneRoom()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(400, 0));
    _session.Selection.Set(new Element[] { a, b });

    _catalogue.Find("room.adjustPorts").CanExecute(_session).ShouldBeFalse();
    _catalogue.Find("room.cyclePortDetail").CanExecute(_session).ShouldBeFalse();
    _catalogue.Find("room.deleteAllConnections").CanExecute(_session).ShouldBeFalse();

    _session.Selection.Set(new Element[] { a });
    _catalogue.Find("room.adjustPorts").CanExecute(_session).ShouldBeTrue();
  }

  // ---- 035 ----

  [Test]
  public async Task ZOrder_FrontAndBackChangeValuesHitTestAndUndo()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(0, 0));
    // AddRoom avoids placing on top of an existing room; stack them exactly so only z-order decides the hit.
    b.Position = a.Position;
    var tester = new HitTester(_session);
    var point = new Vector(a.Position.X + 1, a.Position.Y + 1);

    _session.Selection.Set(new Element[] { a });
    await Run("element.bringToFront");
    a.ZOrder.ShouldBeGreaterThan(b.ZOrder);
    tester.HitElement(point).ShouldBe(a);

    _session.Selection.Set(new Element[] { a });
    await Run("element.sendToBack");
    a.ZOrder.ShouldBeLessThan(b.ZOrder);
    tester.HitElement(point).ShouldBe(b);

    _session.PerformUndo();
    var restored = Project.Current.Elements.OfType<Room>().First(r => r.Id == a.Id);
    var other = Project.Current.Elements.OfType<Room>().First(r => r.Id == b.Id);
    restored.ZOrder.ShouldBeGreaterThan(other.ZOrder);
  }

  [Test]
  public void ZOrder_RequiresRoomOrLabelSelection()
  {
    _catalogue.Find("element.bringToFront").CanExecute(_session).ShouldBeFalse();
    var a = _session.AddRoom(new Vector(0, 0));
    _session.Selection.Set(new Element[] { a });
    _catalogue.Find("element.bringToFront").CanExecute(_session).ShouldBeTrue();
    _catalogue.Find("element.sendToBack").ContextTargets.ShouldBe(ContextTargets.Room | ContextTargets.Label);
  }

  // ---- 036 ----

  [Test]
  public async Task ValidationToggles_FlipTheProjectFlags()
  {
    var pairs = new (string Id, System.Func<Project, bool> Get)[]
    {
      ("project.validate.uniqueNames", p => p.MustHaveUniqueNames),
      ("project.validate.description", p => p.MustHaveDescription),
      ("project.validate.subtitle", p => p.MustHaveSubtitle),
      ("project.validate.noDangling", p => p.MustHaveNoDanglingConnectors)
    };

    foreach (var pair in pairs)
    {
      var command = _catalogue.Find(pair.Id);
      command.IsChecked(_session).ShouldBe(false);
      await command.ExecuteAsync(_session);
      pair.Get(Project.Current).ShouldBeTrue(pair.Id);
      command.IsChecked(_session).ShouldBe(true);
      await command.ExecuteAsync(_session);
      pair.Get(Project.Current).ShouldBeFalse(pair.Id);
    }
  }

  // ---- 054 ----

  [Test]
  public async Task ToggleText_FlipsTheSetting()
  {
    var settings = ApplicationSettingsController.AppSettings;
    settings.DebugDisableTextRendering = false;
    var command = _catalogue.Find("view.toggleText");

    await command.ExecuteAsync(_session);
    settings.DebugDisableTextRendering.ShouldBeTrue();
    command.IsChecked(_session).ShouldBe(true);

    await command.ExecuteAsync(_session);
    settings.DebugDisableTextRendering.ShouldBeFalse();
    _catalogue.FindByGesture(new KeyGesture(InputKey.F4, ModifierKeys.Primary), CommandScope.Window).ShouldBe(command);
  }
}
