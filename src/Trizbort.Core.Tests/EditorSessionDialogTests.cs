using System.Drawing;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Editor;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 076: the Label dialog's OK is one undo step, whatever the number of properties it changes.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class EditorSessionDialogTests
{
  private const string LabelEditTitle = "Edit label properties";

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
  public void LabelEdit_ChangingAllSevenProperties_IsOneUndoStep()
  {
    var label = new MapLabel(_project)
    {
      Text = "Before",
      TextColor = Color.Black,
      Shape = RoomShape.SquareCorners,
      BorderStyle = BorderDashStyle.None,
      BorderColor = Color.Black,
      HasBackground = false,
      BackgroundColor = Color.White
    };
    _project.Elements.Add(label);
    var labelId = label.Id;

    _session.Apply(LabelEditTitle, label, () =>
    {
      label.Text = "After";
      label.TextColor = Color.Red;
      label.Shape = RoomShape.RoundedCorners;
      label.BorderStyle = BorderDashStyle.Dash;
      label.BorderColor = Color.Blue;
      label.HasBackground = true;
      label.BackgroundColor = Color.Yellow;
    });

    _session.Undo.UndoTitle.ShouldBe(LabelEditTitle);

    _session.PerformUndo();

    // Undo restores a new project object, so look the label up again instead of reusing the old reference.
    var restored = _session.Project.Elements.OfType<MapLabel>().Single(l => l.Id == labelId);
    restored.Text.ShouldBe("Before");
    restored.TextColor.ShouldBe(Color.Black);
    restored.Shape.ShouldBe(RoomShape.SquareCorners);
    restored.BorderStyle.ShouldBe(BorderDashStyle.None);
    restored.BorderColor.ShouldBe(Color.Black);
    restored.HasBackground.ShouldBeFalse();
    restored.BackgroundColor.ShouldBe(Color.White);

    // The single step is gone: nothing further to undo, and redo brings back the whole edit.
    _session.Undo.UndoTitle.ShouldNotBe(LabelEditTitle);
    _session.Undo.RedoTitle.ShouldBe(LabelEditTitle);
  }

  [Test]
  public void LabelEdit_NoChanges_RecordsNothing()
  {
    var label = new MapLabel(_project) { Text = "Same" };
    _project.Elements.Add(label);
    var undoBefore = _session.Undo.UndoTitle;

    _session.Apply(LabelEditTitle, label, () => { });

    _session.Undo.UndoTitle.ShouldBe(undoBefore);
  }
}
