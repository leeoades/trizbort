using System.Drawing;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 090: applying a theme and inferring the default room style are single undo steps.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class EditorSessionThemeTests
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
  public void ApplyTheme_IsOneUndoStep()
  {
    _project.Elements.Add(new Room(_project) { Name = "A" });
    var theme = MapTheme.BuiltInThemes().First();
    var before = _session.Undo.CanUndo;

    _session.ApplyTheme(theme, true);

    _session.Undo.UndoTitle.ShouldBe("Apply theme");
    _session.PerformUndo();
    _session.Undo.CanUndo.ShouldBe(before);
  }

  [Test]
  public void Inference_PromotesSharedFillAndIsOneUndoStep()
  {
    var fill = Color.FromArgb(255, 1, 2, 3);
    for (var i = 0; i < 4; i++)
    {
      _project.Elements.Add(new Room(_project) { Name = "R" + i, Position = new Vector(i * 200, 0), RoomFillColor = fill });
    }

    var inference = RoomStyleInference.Analyze(_project);
    inference.HasChanges.ShouldBeTrue();
    inference.Changes.Any(c => c.StartsWith("Fill colour")).ShouldBeTrue();

    _session.ApplyRoomStyleInference(inference);

    _session.Undo.UndoTitle.ShouldBe("Infer default room style");
    _session.Project.Elements.OfType<Room>().All(r => r.RoomFillColor == Color.Transparent).ShouldBeTrue();

    _session.PerformUndo();
    _session.Project.Elements.OfType<Room>().All(r => r.RoomFillColor.ToArgb() == fill.ToArgb()).ShouldBeTrue();
  }

  [Test]
  public void Inference_WithNoChanges_DoesNotRecordAStep()
  {
    _project.Elements.Add(new Room(_project) { Name = "A" });
    var inference = RoomStyleInference.Analyze(_project);
    inference.HasChanges.ShouldBeFalse();

    _session.ApplyRoomStyleInference(inference);

    _session.Undo.CanUndo.ShouldBeFalse();
  }
}
