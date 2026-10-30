using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Editor.Commands;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 052 view commands and the status messages of issue I-015.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class ResetViewAndMiniMapTests
{
  private Project _previousProject;
  private Project _project;
  private EditorSession _session;
  private CommandCatalogue _catalogue;
  private bool _savedMiniMap;

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _session = new EditorSession();
    _catalogue = CommandCatalogue.CreateDefault();
    _savedMiniMap = ApplicationSettingsController.AppSettings.ShowMiniMap;
  }

  [TearDown]
  public void TearDown()
  {
    ApplicationSettingsController.AppSettings.ShowMiniMap = _savedMiniMap;
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

  [Test]
  public async Task ResetOrigin_KeepsZoom()
  {
    _session.Viewport.SetView(new Vector(500, 400), 2.0f);
    await Run("view.resetOrigin");
    _session.Viewport.Origin.ShouldBe(Vector.Zero);
    _session.Viewport.ZoomFactor.ShouldBe(2.0f, 0.001f);
  }

  [Test]
  public async Task FitToMap_NeverZoomsIn()
  {
    var room = new Room(_project) { Position = new Vector(0, 0) };
    _project.Elements.Add(room);
    _session.Viewport.SetZoom(5.0);
    await Run("view.fitToMap");
    _session.Viewport.ZoomFactor.ShouldBeLessThanOrEqualTo(1.0f);
  }

  [Test]
  public async Task Toggles_UpdateIsChecked()
  {
    var grid = _catalogue.Find("view.toggleGrid");
    var before = grid.IsChecked(_session);
    await Run("view.toggleGrid");
    grid.IsChecked(_session).ShouldBe(!before);

    var map = _catalogue.Find("view.toggleMiniMap");
    ApplicationSettingsController.AppSettings.ShowMiniMap = true;
    map.IsChecked(_session).ShouldBe(true);
    await Run("view.toggleMiniMap");
    map.IsChecked(_session).ShouldBe(false);
    ApplicationSettingsController.AppSettings.ShowMiniMap.ShouldBeFalse();
  }

  [Test]
  public async Task SelectAllAndDelete_PostStatusMessages()
  {
    _project.Elements.Add(new Room(_project));
    _project.Elements.Add(new Room(_project));

    await Run("edit.selectAll");
    _session.StatusMessage.ShouldBe("Selected all 2 elements");

    await Run("edit.delete");
    _session.StatusMessage.ShouldBe("Deleted 2 element(s)");
  }

  [Test]
  public async Task Copy_PostsStatusMessage()
  {
    var room = new Room(_project);
    _project.Elements.Add(room);
    _session.Selection.Set(new Element[] { room });

    string posted = null;
    _session.StatusMessagePosted += message => posted = message;
    await Run("edit.copy");
    posted.ShouldBe("Copied 1 element(s)");
  }
}
