using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 129: Restore Default Map Settings confirms, then resets as one undo step.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class RestoreDefaultMapSettingsTests
{
  private Project _previousProject;
  private Project _project;
  private EditorSession _session;
  private AnswerPrompts _prompts;

  private sealed class AnswerPrompts : IEditorPrompts
  {
    public bool Answer { get; set; }

    public string LastMessage { get; private set; }

    public Task<Trizbort.Automap.AutomapSameDirectionResult> ChooseSameDirectionAsync(
      Trizbort.Domain.Elements.Room existing, string newRoomName, CancellationToken token = default)
    {
      return NonInteractiveEditorPrompts.Instance.ChooseSameDirectionAsync(existing, newRoomName, token);
    }

    public Task<RoomDisambiguation> DisambiguateRoomAsync(
      string transcriptLine,
      string roomDescription,
      System.Collections.Generic.IReadOnlyList<Trizbort.Domain.Elements.Room> candidates,
      CancellationToken token = default)
    {
      return NonInteractiveEditorPrompts.Instance.DisambiguateRoomAsync(transcriptLine, roomDescription, candidates, token);
    }

    public Task<bool> ConfirmAsync(string message, string title, CancellationToken token = default)
    {
      LastMessage = message;
      return Task.FromResult(Answer);
    }
  }

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    Settings.Reset();
    _session = new EditorSession();
    _prompts = new AnswerPrompts();
    _session.Prompts = _prompts;
  }

  [TearDown]
  public void TearDown()
  {
    _session.Dispose();
    var current = Project.Current;
    Settings.Reset();
    Project.Current = _previousProject;
    if (current != null && current != _previousProject) current.Dispose();
    if (_project != current) _project.Dispose();
  }

  [Test]
  public async Task ConfirmNo_ChangesNothing()
  {
    Settings.Color[Colors.Canvas] = Color.Red;

    var done = await _session.RestoreDefaultMapSettingsAsync();

    done.ShouldBeFalse();
    Settings.Color[Colors.Canvas].ToArgb().ShouldBe(Color.Red.ToArgb());
    _session.Undo.CanUndo.ShouldBeFalse();
    _prompts.LastMessage.ShouldBe("Restore default settings?\n\nThis will revert any changes to settings in this project.");
  }

  [Test]
  public async Task ConfirmYes_RestoresDefaults_AsOneUndoStep()
  {
    _prompts.Answer = true;
    Settings.Color[Colors.Canvas] = Color.Red;
    Settings.LineWidth = 5.0f;

    var done = await _session.RestoreDefaultMapSettingsAsync();

    done.ShouldBeTrue();
    Settings.Color[Colors.Canvas].ToArgb().ShouldBe(Color.White.ToArgb());
    Settings.LineWidth.ShouldBe(2.0f);
    _session.Undo.UndoTitle.ShouldBe("Restore default map settings");

    _session.PerformUndo();

    Settings.Color[Colors.Canvas].ToArgb().ShouldBe(Color.Red.ToArgb());
    Settings.LineWidth.ShouldBe(5.0f);
  }
}
