using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Editor;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 088: Help links open through the prompts; a failed launch shows a message.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class HelpCommandTests
{
  private Project _previousProject;
  private Project _project;
  private EditorSession _session;
  private LinkPrompts _prompts;

  private sealed class LinkPrompts : IEditorPrompts
  {
    public bool Succeed { get; set; } = true;

    public List<string> Opened { get; } = new();

    public List<string> Messages { get; } = new();

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

    public Task<bool> OpenUriAsync(string uri, CancellationToken token = default)
    {
      Opened.Add(uri);
      return Task.FromResult(Succeed);
    }

    public Task MessageAsync(string message, string title, CancellationToken token = default)
    {
      Messages.Add(title + "|" + message);
      return Task.CompletedTask;
    }
  }

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _session = new EditorSession();
    _prompts = new LinkPrompts();
    _session.Prompts = _prompts;
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
  public async Task UserGuide_OpensTheUserGuideUri()
  {
    await HelpLinks.OpenAsync(_session, HelpLinks.UserGuide);

    _prompts.Opened.ShouldBe(new[] { "https://github.com/leeoades/trizbort/blob/master/Docs/index.md" });
    _prompts.Messages.ShouldBeEmpty();
  }

  [Test]
  public async Task OriginalHelp_OpensTheOriginalHelpUri()
  {
    await HelpLinks.OpenAsync(_session, HelpLinks.OriginalHelp);

    _prompts.Opened.ShouldBe(new[] { "https://trizbort.genstein.net/help/" });
  }

  [Test]
  public async Task FailedLaunch_ShowsAnErrorMessage()
  {
    _prompts.Succeed = false;

    await HelpLinks.OpenAsync(_session, HelpLinks.OriginalHelp);

    _prompts.Messages.ShouldBe(new[] { "Online Help|Unable to open the link:\n\nhttps://trizbort.genstein.net/help/" });
  }

  [Test]
  public async Task NonInteractivePrompts_ReportFailure()
  {
    _session.Prompts = NonInteractiveEditorPrompts.Instance;

    (await _session.Prompts.OpenUriAsync(HelpLinks.UserGuide)).ShouldBeFalse();
  }
}
