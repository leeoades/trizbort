using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Editor;
using Trizbort.Editor.Commands;

namespace Trizbort.Core.Tests;

/// <summary>Tickets 055 and 108: debug toggles exist only with --debug.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class DebugCommandTests
{
  private static readonly string[] _ids =
  {
    "debug.fps", "debug.mouseCoords", "debug.elementRendering", "debug.lineRendering", "debug.gridPolyline"
  };

  private Project _previousProject;
  private Project _project;
  private EditorSession _session;
  private CommandCatalogue _catalogue;
  private string _savedSettings;
  private bool _savedDebugOptions;

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _session = new EditorSession();
    _savedDebugOptions = DebugOptions.Enabled;
    var settings = ApplicationSettingsController.AppSettings;
    _savedSettings = $"{settings.DebugShowFps}|{settings.DebugShowMouseCoordinates}|{settings.DebugDisableElementRendering}|{settings.DebugDisableLineRendering}|{settings.DebugDisableGridPolyline}";
    settings.DebugShowFps = false;
    settings.DebugShowMouseCoordinates = false;
    settings.DebugDisableElementRendering = false;
    settings.DebugDisableLineRendering = false;
    settings.DebugDisableGridPolyline = false;
    DebugOptions.Enabled = false;
    _catalogue = CommandCatalogue.CreateDefault();
  }

  [TearDown]
  public void TearDown()
  {
    DebugOptions.Enabled = _savedDebugOptions;
    var parts = _savedSettings.Split('|');
    var settings = ApplicationSettingsController.AppSettings;
    settings.DebugShowFps = parts[0] == "True";
    settings.DebugShowMouseCoordinates = parts[1] == "True";
    settings.DebugDisableElementRendering = parts[2] == "True";
    settings.DebugDisableLineRendering = parts[3] == "True";
    settings.DebugDisableGridPolyline = parts[4] == "True";
    _session.Dispose();
    var current = Project.Current;
    Project.Current = _previousProject;
    if (current != null && current != _previousProject) current.Dispose();
    if (_project != current) _project.Dispose();
  }

  [Test]
  public void DebugEnabled_DefaultsToFalse()
  {
    _session.DebugEnabled.ShouldBeFalse();
  }

  [Test]
  public void Catalogue_HasAllDebugCommands_AndPassesValidation()
  {
    foreach (var id in _ids) _catalogue.Find(id).ShouldNotBeNull(id);
    CatalogueValidator.Validate(_catalogue).ShouldBeEmpty();
  }

  [Test]
  public void WithoutDebug_CommandsAreHiddenAndCannotExecute()
  {
    foreach (var id in _ids)
    {
      var command = _catalogue.Find(id);
      command.Hidden.ShouldBeTrue(id);
      command.CanExecute(_session).ShouldBeFalse(id);
    }
  }

  [Test]
  public void WithoutDebug_ExecutingDoesNotFlipSettings()
  {
    foreach (var id in _ids) _catalogue.Find(id).ExecuteAsync(_session).Wait();

    var settings = ApplicationSettingsController.AppSettings;
    settings.DebugShowFps.ShouldBeFalse();
    settings.DebugShowMouseCoordinates.ShouldBeFalse();
    settings.DebugDisableElementRendering.ShouldBeFalse();
    settings.DebugDisableLineRendering.ShouldBeFalse();
    settings.DebugDisableGridPolyline.ShouldBeFalse();
  }

  [Test]
  public void WithDebugFlag_CommandsAreVisibleInTheMenu()
  {
    DebugOptions.Enabled = true;
    var catalogue = CommandCatalogue.CreateDefault();

    foreach (var id in _ids) catalogue.Find(id).Hidden.ShouldBeFalse(id);
    catalogue.Menu(System.OperatingSystem.IsMacOS() ? "Developer" : "View").Select(c => c.Id).ShouldContain("debug.fps");
  }

  [Test]
  public void WithDebug_ToggleCommandsFlipSettings()
  {
    _session.DebugEnabled = true;
    var settings = ApplicationSettingsController.AppSettings;

    _catalogue.Find("debug.fps").CanExecute(_session).ShouldBeTrue();
    _catalogue.Find("debug.fps").ExecuteAsync(_session).Wait();
    settings.DebugShowFps.ShouldBeTrue();
    _catalogue.Find("debug.fps").IsChecked(_session).ShouldBe(true);

    _catalogue.Find("debug.mouseCoords").ExecuteAsync(_session).Wait();
    settings.DebugShowMouseCoordinates.ShouldBeTrue();

    _catalogue.Find("debug.elementRendering").ExecuteAsync(_session).Wait();
    settings.DebugDisableElementRendering.ShouldBeTrue();
    _catalogue.Find("debug.elementRendering").IsChecked(_session).ShouldBe(false);

    _catalogue.Find("debug.lineRendering").ExecuteAsync(_session).Wait();
    settings.DebugDisableLineRendering.ShouldBeTrue();

    _catalogue.Find("debug.gridPolyline").ExecuteAsync(_session).Wait();
    settings.DebugDisableGridPolyline.ShouldBeTrue();

    _catalogue.Find("debug.fps").ExecuteAsync(_session).Wait();
    settings.DebugShowFps.ShouldBeFalse();
  }

  [Test]
  public void WithDebug_RenderingFlagsFollowSettings()
  {
    var settings = ApplicationSettingsController.AppSettings;
    settings.DebugDisableElementRendering = true;
    settings.DebugDisableLineRendering = true;
    settings.DebugShowFps = true;
    settings.DebugShowMouseCoordinates = true;

    // the settings do nothing without the flag
    _session.ElementRenderingEnabled.ShouldBeTrue();
    _session.LineRenderingEnabled.ShouldBeTrue();
    _session.ShowFps.ShouldBeFalse();
    _session.ShowMouseCoordinates.ShouldBeFalse();

    _session.DebugEnabled = true;
    _session.ElementRenderingEnabled.ShouldBeFalse();
    _session.LineRenderingEnabled.ShouldBeFalse();
    _session.ShowFps.ShouldBeTrue();
    _session.ShowMouseCoordinates.ShouldBeTrue();
  }

  [Test]
  public void Gestures_AreTheReferenceKeys_ExceptGridPolyline()
  {
    if (System.OperatingSystem.IsMacOS()) Assert.Ignore("macOS uses the Developer menu without keys.");

    string Gesture(string id) => string.Join(",", _catalogue.Find(id).Gestures.Select(g => $"{g.Key}+{g.Modifiers}"));
    Gesture("debug.fps").ShouldBe("F1+Control");
    Gesture("debug.mouseCoords").ShouldBe("F1+Shift");
    Gesture("debug.elementRendering").ShouldBe("F2+Control");
    Gesture("debug.lineRendering").ShouldBe("F3+Control");
    _catalogue.Find("debug.gridPolyline").Gestures.ShouldBeEmpty();
  }

  [Test]
  public void ChangingDebugEnabled_RaisesViewportChange()
  {
    EditorChange raised = EditorChange.None;
    _session.Changed += change => raised |= change;

    _session.DebugEnabled = true;

    (raised & EditorChange.Viewport).ShouldBe(EditorChange.Viewport);
  }
}
