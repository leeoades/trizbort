using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 131: trackpad scroll / pinch and multi-touch navigation (GitHub issue #21).</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class TrackpadNavigationTests
{
  private static readonly Vector _centre = new(400, 300);

  private Project _previousProject;
  private Project _project;
  private EditorSession _session;
  private CanvasInteraction _interaction;
  private bool _savedInvert;
  private ScrollWheelMode _savedMode;

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _session = new EditorSession();
    _interaction = new CanvasInteraction(_session, new HitTester(_session));
    var app = ApplicationSettingsController.AppSettings;
    _savedInvert = app.InvertMouseWheel;
    _savedMode = app.ScrollWheelBehavior;
    app.InvertMouseWheel = false;
    app.ScrollWheelBehavior = ScrollWheelMode.Automatic;
  }

  [TearDown]
  public void TearDown()
  {
    var app = ApplicationSettingsController.AppSettings;
    app.InvertMouseWheel = _savedInvert;
    app.ScrollWheelBehavior = _savedMode;
    _interaction.Dispose();
    _session.Dispose();
    var current = Project.Current;
    Project.Current = _previousProject;
    if (current != null && current != _previousProject) current.Dispose();
    if (_project != current) _project.Dispose();
  }

  private void Wheel(float dy, float dx = 0, ModifierKeys modifiers = ModifierKeys.None)
  {
    _interaction.OnWheel(new WheelInput(dy, modifiers, _centre, dx));
  }

  // ---- wheel heuristic ---------------------------------------------------------------------

  [Test]
  public void WholeNotch_Zooms_AndDoesNotPan()
  {
    var origin = _session.Viewport.Origin;
    Wheel(1);
    _session.Viewport.ZoomFactor.ShouldBe(1.25f, 0.001f);
    _session.Viewport.Origin.ShouldBe(origin);
  }

  [Test]
  public void FractionalDelta_Pans_AndDoesNotZoom()
  {
    var origin = _session.Viewport.Origin;
    Wheel(0.5f);
    _session.Viewport.ZoomFactor.ShouldBe(1f, 0.0001f);
    // Content follows the fingers: a positive delta moves the view up (origin Y decreases).
    _session.Viewport.Origin.Y.ShouldBe(origin.Y - 0.5f * Viewport.ScrollPanPixelsPerUnit, 0.01f);
    _session.Viewport.Origin.X.ShouldBe(origin.X, 0.01f);
  }

  [Test]
  public void HorizontalDelta_PansBothAxes()
  {
    var origin = _session.Viewport.Origin;
    Wheel(1, 2);
    _session.Viewport.ZoomFactor.ShouldBe(1f, 0.0001f);
    _session.Viewport.Origin.X.ShouldBe(origin.X - 2 * Viewport.ScrollPanPixelsPerUnit, 0.01f);
    _session.Viewport.Origin.Y.ShouldBe(origin.Y - 1 * Viewport.ScrollPanPixelsPerUnit, 0.01f);
  }

  [Test]
  public void AlwaysZoom_ZoomsEvenForTrackpadLikeEvents()
  {
    ApplicationSettingsController.AppSettings.ScrollWheelBehavior = ScrollWheelMode.AlwaysZoom;
    var origin = _session.Viewport.Origin;
    Wheel(0.5f);
    _session.Viewport.ZoomFactor.ShouldBeGreaterThan(1f);
    _session.Viewport.ZoomFactor.ShouldBeLessThan(1.25f);
    // Zoomed about the viewport centre, so nothing panned.
    _session.Viewport.Origin.X.ShouldBe(origin.X, 0.01f);
    _session.Viewport.Origin.Y.ShouldBe(origin.Y, 0.01f);
  }

  [Test]
  public void AlwaysPan_PansEvenForWholeNotches()
  {
    ApplicationSettingsController.AppSettings.ScrollWheelBehavior = ScrollWheelMode.AlwaysPan;
    var origin = _session.Viewport.Origin;
    Wheel(1);
    _session.Viewport.ZoomFactor.ShouldBe(1f, 0.0001f);
    _session.Viewport.Origin.Y.ShouldBe(origin.Y - Viewport.ScrollPanPixelsPerUnit, 0.01f);
  }

  [Test]
  public void AlwaysPan_CtrlStillZooms()
  {
    ApplicationSettingsController.AppSettings.ScrollWheelBehavior = ScrollWheelMode.AlwaysPan;
    Wheel(1, 0, ModifierKeys.Control);
    _session.Viewport.ZoomFactor.ShouldBe(1.01f, 0.0005f);
  }

  [Test]
  public void CtrlFractionalDelta_SmoothZooms()
  {
    Wheel(0.5f, 0, ModifierKeys.Control);
    _session.Viewport.ZoomFactor.ShouldBe((float)System.Math.Pow(1.25, 0.5), 0.001f);
  }

  [Test]
  public void InvertMouseWheel_AppliesToSmoothZoomButNotPan()
  {
    ApplicationSettingsController.AppSettings.InvertMouseWheel = true;
    Wheel(0.5f, 0, ModifierKeys.Control);
    _session.Viewport.ZoomFactor.ShouldBeLessThan(1f);

    _session.Viewport.ResetZoom();
    var origin = _session.Viewport.Origin;
    Wheel(0.5f);
    _session.Viewport.Origin.Y.ShouldBe(origin.Y - 0.5f * Viewport.ScrollPanPixelsPerUnit, 0.01f);
  }

  [Test]
  public void IsTrackpadLike_Heuristic()
  {
    CanvasInteraction.IsTrackpadLike(new WheelInput(1, ModifierKeys.None, _centre)).ShouldBeFalse();
    CanvasInteraction.IsTrackpadLike(new WheelInput(-3, ModifierKeys.None, _centre)).ShouldBeFalse();
    CanvasInteraction.IsTrackpadLike(new WheelInput(1.0005f, ModifierKeys.None, _centre)).ShouldBeFalse();
    CanvasInteraction.IsTrackpadLike(new WheelInput(0.1f, ModifierKeys.None, _centre)).ShouldBeTrue();
    CanvasInteraction.IsTrackpadLike(new WheelInput(1, ModifierKeys.None, _centre, 0.2f)).ShouldBeTrue();
  }

  // ---- smooth zoom --------------------------------------------------------------------------

  [Test]
  public void ZoomSmooth_TenTenths_EqualOneNotch()
  {
    var smooth = new Viewport();
    for (var i = 0; i < 10; i++) smooth.ZoomSmooth(0.1, _centre);
    var notch = new Viewport();
    notch.ZoomSmooth(1.0, _centre);
    smooth.ZoomFactor.ShouldBe(notch.ZoomFactor, 0.001f);
    notch.ZoomFactor.ShouldBe(1.25f, 0.001f);
  }

  [Test]
  public void ZoomSmooth_ClampsPerEventDelta()
  {
    var viewport = new Viewport();
    viewport.ZoomSmooth(50, _centre);
    viewport.ZoomFactor.ShouldBe(1.25f, 0.001f);
    viewport.ZoomSmooth(-50, _centre);
    viewport.ZoomFactor.ShouldBe(1.0f, 0.001f);
  }

  [Test]
  public void ZoomSmooth_RespectsMinAndMax()
  {
    var viewport = new Viewport();
    for (var i = 0; i < 100; i++) viewport.ZoomSmooth(1, _centre);
    viewport.ZoomFactor.ShouldBe(10f, 0.001f);
    for (var i = 0; i < 100; i++) viewport.ZoomSmooth(-1, _centre);
    viewport.ZoomFactor.ShouldBe(0.1f, 0.001f);
  }

  [Test]
  public void ZoomSmooth_KeepsPointUnderPointer()
  {
    var client = new Vector(600, 200);
    var before = _session.Viewport.ClientToCanvas(client);
    _session.Viewport.ZoomSmooth(0.7, client);
    var after = _session.Viewport.ClientToCanvas(client);
    after.X.ShouldBe(before.X, 0.5f);
    after.Y.ShouldBe(before.Y, 0.5f);
  }

  [Test]
  public void Magnify_ZoomsProportionally()
  {
    _interaction.OnMagnify(0.1, _centre);
    _session.Viewport.ZoomFactor.ShouldBe(1.1f, 0.001f);
    _interaction.OnMagnify(-0.5, _centre);
    _session.Viewport.ZoomFactor.ShouldBe(0.55f, 0.001f);
  }

  // ---- MultiTouchTracker --------------------------------------------------------------------

  [Test]
  public void Tracker_SingleTouch_ReportsNothing()
  {
    var tracker = new MultiTouchTracker();
    tracker.Press(1, new Vector(10, 10));
    tracker.IsMultiTouch.ShouldBeFalse();
    tracker.Move(1, new Vector(50, 50), out _).ShouldBeFalse();
  }

  [Test]
  public void Tracker_TwoTouches_ReportCentroidAndSpread()
  {
    var tracker = new MultiTouchTracker();
    tracker.Press(1, new Vector(100, 100));
    tracker.Press(2, new Vector(200, 100));
    tracker.IsMultiTouch.ShouldBeTrue();
    tracker.Count.ShouldBe(2);

    // Pointer 2 moves right by 100: centroid shifts 50, spread goes from 50 to 100.
    tracker.Move(2, new Vector(300, 100), out var step).ShouldBeTrue();
    step.Centroid.ShouldBe(new Vector(200, 100));
    step.CentroidDelta.ShouldBe(new Vector(50, 0));
    step.Scale.ShouldBe(2f, 0.001f);
  }

  [Test]
  public void Tracker_ParallelMove_PansWithoutScaling()
  {
    var tracker = new MultiTouchTracker();
    tracker.Press(1, new Vector(100, 100));
    tracker.Press(2, new Vector(200, 100));

    tracker.Move(1, new Vector(100, 130), out var first).ShouldBeTrue();
    tracker.Move(2, new Vector(200, 130), out var second).ShouldBeTrue();

    (first.CentroidDelta.Y + second.CentroidDelta.Y).ShouldBe(30f, 0.001f);
    (first.Scale * second.Scale).ShouldBe(1f, 0.001f);
  }

  [Test]
  public void Tracker_AddingOrRemovingAPointer_DoesNotJump()
  {
    var tracker = new MultiTouchTracker();
    tracker.Press(1, new Vector(0, 0));
    tracker.Press(2, new Vector(100, 0));
    tracker.Press(3, new Vector(50, 300)); // third finger lands far away: baseline resets
    tracker.Move(3, new Vector(50, 300), out var step).ShouldBeTrue();
    step.CentroidDelta.ShouldBe(new Vector(0, 0));
    step.Scale.ShouldBe(1f, 0.001f);

    tracker.Release(3);
    tracker.Count.ShouldBe(2);
    tracker.Move(1, new Vector(0, 0), out step).ShouldBeTrue();
    step.CentroidDelta.ShouldBe(new Vector(0, 0));
    step.Scale.ShouldBe(1f, 0.001f);

    tracker.Release(2);
    tracker.IsMultiTouch.ShouldBeFalse();
    tracker.Move(2, new Vector(5, 5), out _).ShouldBeFalse(); // unknown pointer
  }

  [Test]
  public void Tracker_Clear_ForgetsAllTouches()
  {
    var tracker = new MultiTouchTracker();
    tracker.Press(1, new Vector(0, 0));
    tracker.Press(2, new Vector(10, 0));
    tracker.Clear();
    tracker.Count.ShouldBe(0);
  }

  // ---- touch gesture entry point -------------------------------------------------------------

  [Test]
  public void TouchGesture_PansAndZooms()
  {
    var origin = _session.Viewport.Origin;
    _interaction.OnTouchGesture(new Vector(30, 0), 1f, _centre);
    _session.Viewport.Origin.X.ShouldBe(origin.X - 30f, 0.01f);

    _interaction.OnTouchGesture(Vector.Zero, 1.5f, _centre);
    _session.Viewport.ZoomFactor.ShouldBe(1.5f, 0.001f);
  }

  [Test]
  public void TouchGesture_CancelsDragInProgress_WithNoUndoStep()
  {
    var room = new Room(_project) { Position = new Vector(0, 0), Size = new Vector(64, 64) };
    _project.Elements.Add(room);
    var id = room.Id;
    _session.Undo.Clear();

    var start = _session.Viewport.CanvasToClient(new Vector(32, 32));
    _interaction.OnPointerMoved(new PointerInput(PointerButton.None, ModifierKeys.None, start, 0));
    _interaction.OnPointerPressed(new PointerInput(PointerButton.Left, ModifierKeys.None, start, 1));
    _interaction.OnPointerMoved(new PointerInput(PointerButton.None, ModifierKeys.None, start + new Vector(100, 50), 0));
    _interaction.IsGestureActive.ShouldBeTrue();

    _interaction.OnTouchGesture(new Vector(10, 10), 1f, start);

    _interaction.IsGestureActive.ShouldBeFalse();
    _session.Undo.CanUndo.ShouldBeFalse();
    _session.Project.Elements.OfType<Room>().First(r => r.Id == id).Position.ShouldBe(new Vector(0, 0));
  }
}
