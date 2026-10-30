using System.Linq;
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

/// <summary>Undo/redo commands (008), arrow keys (046), zoom commands (050).</summary>
[TestFixture]
[NonParallelizable]
public class ViewCommandTests
{
  private Project _previousProject;
  private Project _project;
  private EditorSession _session;
  private CommandCatalogue _catalogue;

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

  private EditorCommand Command(string id)
  {
    var command = _catalogue.Find(id);
    command.ShouldNotBeNull(id);
    return command;
  }

  private int RoomCount()
  {
    return _session.Project.Elements.OfType<Room>().Count();
  }

  private Room FirstRoom()
  {
    return _session.Project.Elements.OfType<Room>().First();
  }

  // ---- 008 undo / redo ----

  [Test]
  public void Undo_CanExecute_FalseWhenEmpty()
  {
    Command("edit.undo").CanExecute(_session).ShouldBeFalse();
  }

  [Test]
  public void Undo_Execute_RevertsLastEdit()
  {
    _session.AddRoom(new Vector(0, 0));
    RoomCount().ShouldBe(1);

    Command("edit.undo").CanExecute(_session).ShouldBeTrue();
    Command("edit.undo").ExecuteAsync(_session).Wait();

    RoomCount().ShouldBe(0);
  }

  [Test]
  public void Redo_CanExecute_FalseUntilUndo()
  {
    _session.AddRoom(new Vector(0, 0));
    Command("edit.redo").CanExecute(_session).ShouldBeFalse();

    Command("edit.undo").ExecuteAsync(_session).Wait();

    Command("edit.redo").CanExecute(_session).ShouldBeTrue();
  }

  [Test]
  public void Redo_Execute_ReappliesEdit()
  {
    _session.AddRoom(new Vector(0, 0));
    Command("edit.undo").ExecuteAsync(_session).Wait();

    Command("edit.redo").ExecuteAsync(_session).Wait();

    RoomCount().ShouldBe(1);
  }

  [Test]
  public void UndoTitle_ShowsInDynamicTitle()
  {
    Command("edit.undo").TitleFor(_session).ShouldContain("Undo");
    _session.AddRoom(new Vector(0, 0));

    var title = Command("edit.undo").TitleFor(_session);

    title.ShouldContain(_session.Undo.UndoTitle);
  }

  [Test]
  public void Undo_Gesture_IsPrimaryZ()
  {
    Command("edit.undo").Gestures.ShouldBe(new[] { new KeyGesture(InputKey.Z, ModifierKeys.Primary) });
  }

  [Test]
  public void Redo_HasBothGestures_PrimaryY_And_PrimaryShiftZ()
  {
    var gestures = Command("edit.redo").Gestures;

    gestures.ShouldContain(new KeyGesture(InputKey.Y, ModifierKeys.Primary));
    gestures.ShouldContain(new KeyGesture(InputKey.Z, ModifierKeys.Primary | ModifierKeys.Shift));
  }

  [Test]
  public void Undo_DisabledDuringDrag()
  {
    _session.AddRoom(new Vector(0, 0));
    _session.InteractionActiveProbe = () => true;

    Command("edit.undo").CanExecute(_session).ShouldBeFalse();

    _session.InteractionActiveProbe = () => false;
    Command("edit.undo").CanExecute(_session).ShouldBeTrue();
  }

  [Test]
  public void UndoRedo_AreInToolbar()
  {
    Command("edit.undo").ShowInToolbar.ShouldBeTrue();
    Command("edit.redo").ShowInToolbar.ShouldBeTrue();
  }

  // ---- 046 arrows ----

  [Test]
  public void ArrowRight_WithSelection_MovesByArrowStep()
  {
    _session.AddRoom(new Vector(0, 0));
    var room = FirstRoom();
    _session.Select(room, false, false);
    var before = room.Position.X;

    Command("selection.move.right").ExecuteAsync(_session).Wait();

    FirstRoom().Position.X.ShouldBe(before + _session.ArrowStep, 0.01f);
  }

  [Test]
  public void ArrowMove_IsOneUndoStep()
  {
    _session.AddRoom(new Vector(0, 0));
    _session.Select(FirstRoom(), false, false);
    var before = FirstRoom().Position;

    Command("selection.move.down").ExecuteAsync(_session).Wait();
    _session.PerformUndo();

    FirstRoom().Position.ShouldBe(before);
  }

  [Test]
  public void ArrowRight_WithoutSelection_PansByTenthOfViewportWidth()
  {
    _session.Viewport.SetSize(1000, 500);
    _session.Viewport.SetView(Vector.Zero, 1.0f);
    _session.Selection.Clear();

    Command("selection.move.right").ExecuteAsync(_session).Wait();

    _session.Viewport.Origin.X.ShouldBe(100f, 0.01f);
    _session.Viewport.Origin.Y.ShouldBe(0f, 0.01f);
  }

  [Test]
  public void ArrowDown_WithoutSelection_UsesViewportHeight()
  {
    _session.Viewport.SetSize(1000, 500);
    _session.Viewport.SetView(Vector.Zero, 1.0f);
    _session.Selection.Clear();

    Command("selection.move.down").ExecuteAsync(_session).Wait();

    _session.Viewport.Origin.Y.ShouldBe(50f, 0.01f);
  }

  [Test]
  public void FastPan_UsesFifthOfViewport()
  {
    _session.Viewport.SetSize(1000, 500);
    _session.Viewport.SetView(Vector.Zero, 1.0f);
    _session.Selection.Clear();

    _session.ArrowMoveOrPan(new Vector(0, 1), true);

    _session.Viewport.Origin.Y.ShouldBe(100f, 0.01f);
  }

  [Test]
  public void ResizeRight_GrowsSelectedRoom_AndLeftNeverBelowOneGridCell()
  {
    _session.AddRoom(new Vector(0, 0));
    _session.Select(FirstRoom(), false, false);
    var width = FirstRoom().Size.X;

    Command("selection.resize.right").ExecuteAsync(_session).Wait();
    FirstRoom().Size.X.ShouldBe(width + _session.ArrowStep, 0.01f);

    for (var i = 0; i < 500; i++) Command("selection.resize.left").ExecuteAsync(_session).Wait();
    FirstRoom().Size.X.ShouldBeGreaterThanOrEqualTo(Settings.GridSize - 0.01f);
  }

  [Test]
  public void Resize_CannotExecute_WithoutSelection()
  {
    _session.Selection.Clear();

    Command("selection.resize.left").CanExecute(_session).ShouldBeFalse();
  }

  [Test]
  public void ArrowGestures_AreCanvasScope_AndCatalogueIsValid()
  {
    _catalogue.FindByGesture(new KeyGesture(InputKey.Left, ModifierKeys.None), CommandScope.Canvas).Id.ShouldBe("selection.move.left");
    _catalogue.FindByGesture(new KeyGesture(InputKey.Up, ModifierKeys.Primary | ModifierKeys.Alt), CommandScope.Canvas).Id.ShouldBe("selection.resize.up");
    CatalogueValidator.Validate(_catalogue).ShouldBeEmpty();
  }

  // ---- 050 zoom ----

  [Test]
  public void ZoomPresets_SetExactZoom_KeepingCentre()
  {
    _session.Viewport.SetView(new Vector(30, 40), 1.0f);

    Command("view.zoom50").ExecuteAsync(_session).Wait();
    _session.Viewport.ZoomFactor.ShouldBe(0.5f, 0.001f);
    _session.Viewport.Origin.ShouldBe(new Vector(30, 40));

    Command("view.zoom200").ExecuteAsync(_session).Wait();
    _session.Viewport.ZoomFactor.ShouldBe(2.0f, 0.001f);
    _session.Viewport.Origin.ShouldBe(new Vector(30, 40));

    Command("view.resetZoom").ExecuteAsync(_session).Wait();
    _session.Viewport.ZoomFactor.ShouldBe(1.0f, 0.001f);
  }

  [Test]
  public void ZoomMicro_StepsByOnePercent()
  {
    _session.Viewport.SetView(Vector.Zero, 1.0f);

    Command("view.zoomMicroIn").ExecuteAsync(_session).Wait();
    _session.Viewport.ZoomFactor.ShouldBe(1.01f, 0.0005f);

    Command("view.zoomMicroOut").ExecuteAsync(_session).Wait();
    Command("view.zoomMicroOut").ExecuteAsync(_session).Wait();
    _session.Viewport.ZoomFactor.ShouldBe(0.99f, 0.0005f);
  }

  [Test]
  public void ZoomStep_Multiplies_By_1_25()
  {
    _session.Viewport.SetView(Vector.Zero, 1.0f);

    Command("view.zoomIn").ExecuteAsync(_session).Wait();
    _session.Viewport.ZoomFactor.ShouldBe(1.25f, 0.01f);

    Command("view.zoomOut").ExecuteAsync(_session).Wait();
    _session.Viewport.ZoomFactor.ShouldBe(1.0f, 0.02f);
  }

  [Test]
  public void Zoom_ClampsToCoreRange()
  {
    _session.Viewport.SetZoom(100);
    _session.Viewport.ZoomFactor.ShouldBe(10.0f, 0.001f);

    _session.Viewport.SetZoom(0.001);
    _session.Viewport.ZoomFactor.ShouldBe(0.1f, 0.001f);

    _session.Viewport.ZoomMicroOut();
    _session.Viewport.ZoomFactor.ShouldBe(0.1f, 0.001f);
  }

  [Test]
  public void ZoomKeys_BareAndPrimary_AreBound()
  {
    var plus = new KeyGesture(InputKey.OemPlus, ModifierKeys.None);
    var numAdd = new KeyGesture(InputKey.Add, ModifierKeys.None);
    var minus = new KeyGesture(InputKey.OemMinus, ModifierKeys.None);
    var numSubtract = new KeyGesture(InputKey.Subtract, ModifierKeys.None);

    _catalogue.FindByGesture(plus, CommandScope.Canvas).ShouldNotBeNull();
    _catalogue.FindByGesture(numAdd, CommandScope.Canvas).ShouldNotBeNull();
    _catalogue.FindByGesture(minus, CommandScope.Canvas).ShouldNotBeNull();
    _catalogue.FindByGesture(numSubtract, CommandScope.Canvas).ShouldNotBeNull();
    _catalogue.FindByGesture(new KeyGesture(InputKey.OemPlus, ModifierKeys.Primary), CommandScope.Window).Id.ShouldBe("view.zoomIn");
    _catalogue.FindByGesture(new KeyGesture(InputKey.OemMinus, ModifierKeys.Primary), CommandScope.Window).Id.ShouldBe("view.zoomOut");
    _catalogue.FindByGesture(new KeyGesture(InputKey.D0, ModifierKeys.Primary), CommandScope.Window).Id.ShouldBe("view.resetZoom");
  }

  [Test]
  public void ZoomPresets_AreInViewMenu()
  {
    var ids = _catalogue.Menu("View").Select(c => c.Id).ToList();

    ids.ShouldContain("view.zoom50");
    ids.ShouldContain("view.zoom200");
    ids.ShouldContain("view.zoomMicroIn");
    ids.ShouldContain("view.zoomMicroOut");
  }
}
