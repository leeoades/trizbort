using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 051: wheel zoom, InvertMouseWheel and Ctrl micro-steps.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class WheelZoomTests
{
  private Project _previousProject;
  private Project _project;
  private EditorSession _session;
  private CanvasInteraction _interaction;
  private bool _savedInvert;

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _session = new EditorSession();
    _interaction = new CanvasInteraction(_session, new HitTester(_session));
    _savedInvert = ApplicationSettingsController.AppSettings.InvertMouseWheel;
    ApplicationSettingsController.AppSettings.InvertMouseWheel = false;
  }

  [TearDown]
  public void TearDown()
  {
    ApplicationSettingsController.AppSettings.InvertMouseWheel = _savedInvert;
    _interaction.Dispose();
    _session.Dispose();
    var current = Project.Current;
    Project.Current = _previousProject;
    if (current != null && current != _previousProject) current.Dispose();
    if (_project != current) _project.Dispose();
  }

  [Test]
  public void WheelUp_ZoomsIn_AndWheelDown_ZoomsOut()
  {
    _interaction.OnWheel(new WheelInput(1, ModifierKeys.None, new Vector(400, 300)));
    _session.Viewport.ZoomFactor.ShouldBeGreaterThan(1f);

    _session.Viewport.ResetZoom();
    _interaction.OnWheel(new WheelInput(-1, ModifierKeys.None, new Vector(400, 300)));
    _session.Viewport.ZoomFactor.ShouldBeLessThan(1f);
  }

  [Test]
  public void InvertedWheel_ReversesDirection()
  {
    ApplicationSettingsController.AppSettings.InvertMouseWheel = true;
    _interaction.OnWheel(new WheelInput(1, ModifierKeys.None, new Vector(400, 300)));
    _session.Viewport.ZoomFactor.ShouldBeLessThan(1f);
  }

  [Test]
  public void CtrlWheel_UsesMicroStep()
  {
    _interaction.OnWheel(new WheelInput(1, ModifierKeys.Control, new Vector(400, 300)));
    _session.Viewport.ZoomFactor.ShouldBe(1.01f, 0.0005f);

    _interaction.OnWheel(new WheelInput(-1, ModifierKeys.Primary, new Vector(400, 300)));
    _session.Viewport.ZoomFactor.ShouldBe(1.0f, 0.0005f);
  }

  [Test]
  public void PointUnderCursor_StaysPut()
  {
    var client = new Vector(600, 200);
    var before = _session.Viewport.ClientToCanvas(client);
    _interaction.OnWheel(new WheelInput(1, ModifierKeys.None, client));
    var after = _session.Viewport.ClientToCanvas(client);
    after.X.ShouldBe(before.X, 0.5f);
    after.Y.ShouldBe(before.Y, 0.5f);
  }
}
