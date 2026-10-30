using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests;

[TestFixture]
[NonParallelizable]
public class ViewportPointerTests
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

  [Test]
  public void SetPointerClientPosition_AtViewportCentre_ReturnsOrigin()
  {
    // Default viewport is 800x600, so its centre is client (400, 300).
    _session.Viewport.SetView(new Vector(100, 50), 1f);

    _session.SetPointerClientPosition(400, 300);

    _session.PointerMapPosition.ShouldBe(new Vector(100, 50));
  }

  [Test]
  public void SetPointerClientPosition_AppliesZoom()
  {
    _session.Viewport.SetView(new Vector(100, 50), 2f);

    _session.SetPointerClientPosition(420, 310);

    _session.PointerMapPosition.ShouldBe(new Vector(110, 55));
  }

  [Test]
  public void SetPointerClientPosition_LeftAndAboveCentre_MapsToSmallerCoordinates()
  {
    _session.Viewport.SetView(new Vector(100, 50), 2f);

    _session.SetPointerClientPosition(300, 200);

    _session.PointerMapPosition.ShouldBe(new Vector(50, 0));
  }

  [Test]
  public void ClearPointerPosition_ClearsTheStoredPosition()
  {
    _session.SetPointerClientPosition(400, 300);

    _session.ClearPointerPosition();

    _session.PointerMapPosition.HasValue.ShouldBeFalse();
  }

  [Test]
  public void PointerPosition_RaisesAdornersOnlyWhenThePositionChanges()
  {
    var changes = new List<EditorChange>();
    _session.Changed += changes.Add;

    _session.SetPointerClientPosition(400, 300);
    _session.SetPointerClientPosition(400, 300);
    _session.ClearPointerPosition();
    _session.ClearPointerPosition();

    changes.ShouldBe(new[] { EditorChange.Pointer, EditorChange.Pointer });
  }
}
