using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Elements;
using Trizbort.Editor;

namespace Trizbort.Core.Tests.Editor;

/// <summary>Ticket 059: Smart Save saves, then writes the outputs the settings ask for.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class SmartSaveTests
{
  private Project _previousProject;
  private Project _project;
  private EditorSession _session;
  private SmartPrompts _prompts;
  private List<string> _calls;

  private sealed class SmartPrompts : IEditorPrompts
  {
    public bool ConfirmAnswer { get; set; } = true;

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

    public Task ShowMessageAsync(string title, string message, CancellationToken token = default)
    {
      Messages.Add(message);
      return Task.CompletedTask;
    }

    public Task<bool> ConfirmAsync(string message, string title, CancellationToken token = default)
    {
      return Task.FromResult(ConfirmAnswer);
    }
  }

  private sealed class FakeExporter : ISmartSaveExporter
  {
    private readonly List<string> _calls;

    public FakeExporter(SmartSaveKind kind, List<string> calls)
    {
      Kind = kind;
      _calls = calls;
    }

    public SmartSaveKind Kind { get; }

    public bool Fail { get; set; }

    public Task<string> ExportAsync(EditorSession session, string mapFileName, CancellationToken token = default)
    {
      _calls.Add("export " + Kind);
      if (Fail) throw new InvalidOperationException("disk full");
      return Task.FromResult(mapFileName + "." + Kind);
    }
  }

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project { FileName = "/maps/castle.trizbort" };
    Project.Current = _project;
    _session = new EditorSession();
    _prompts = new SmartPrompts();
    _session.Prompts = _prompts;
    _calls = new List<string>();
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

  private Func<bool, Task> Saver(bool succeed)
  {
    return saveAs =>
    {
      _calls.Add(saveAs ? "saveAs" : "save");
      if (succeed)
      {
        Project.Current.FileName = "/maps/castle.trizbort";
        Project.Current.IsDirty = false;
      }
      return Task.CompletedTask;
    };
  }

  private static ApplicationSettings Settings(bool pdf, bool image)
  {
    return new ApplicationSettings { SaveToPDF = pdf, SaveToImage = image };
  }

  [Test]
  public async Task BothOutputsOff_ExplainsAndDoesNothing()
  {
    _project.IsDirty = true;
    var exporters = new[] { new FakeExporter(SmartSaveKind.Image, _calls) };

    var done = await SmartSave.RunAsync(_session, Settings(false, false), exporters, Saver(true));

    done.ShouldBeFalse();
    _calls.ShouldBeEmpty();
    _prompts.Messages.Count.ShouldBe(1);
    _prompts.Messages[0].ShouldStartWith("Your settings are set to not save anything.");
  }

  [Test]
  public async Task DirtyMap_SavesThenExportsInOrder()
  {
    _project.IsDirty = true;
    var exporters = new[]
    {
      new FakeExporter(SmartSaveKind.Image, _calls),
      new FakeExporter(SmartSaveKind.Pdf, _calls)
    };

    var done = await SmartSave.RunAsync(_session, Settings(true, true), exporters, Saver(true));

    done.ShouldBeTrue();
    _calls.ShouldBe(new[] { "save", "export Pdf", "export Image" });
    _prompts.Messages.Count.ShouldBe(1);
    _prompts.Messages[0].ShouldContain("PDF file has been saved to /maps/castle.trizbort.Pdf");
    _prompts.Messages[0].ShouldContain("Image file has been saved to /maps/castle.trizbort.Image");
  }

  [Test]
  public async Task CleanNamedMap_ExportsWithoutSaving()
  {
    var exporters = new[] { new FakeExporter(SmartSaveKind.Image, _calls) };

    var done = await SmartSave.RunAsync(_session, Settings(false, true), exporters, Saver(true));

    done.ShouldBeTrue();
    _calls.ShouldBe(new[] { "export Image" });
  }

  [Test]
  public async Task UrlMap_SavesAsFirst()
  {
    _project.FileName = "https://example.com/castle.trizbort";
    var exporters = new[] { new FakeExporter(SmartSaveKind.Image, _calls) };

    var done = await SmartSave.RunAsync(_session, Settings(false, true), exporters, Saver(true));

    done.ShouldBeTrue();
    _calls.ShouldBe(new[] { "saveAs", "export Image" });
  }

  [Test]
  public async Task UserDeclinesSave_NothingIsSavedOrExported()
  {
    _project.IsDirty = true;
    _prompts.ConfirmAnswer = false;
    var exporters = new[] { new FakeExporter(SmartSaveKind.Image, _calls) };

    var done = await SmartSave.RunAsync(_session, Settings(false, true), exporters, Saver(true));

    done.ShouldBeFalse();
    _calls.ShouldBeEmpty();
    _prompts.Messages.ShouldBe(new[] { "No files have been saved during the SmartSave." });
  }

  [Test]
  public async Task SaveCancelled_DoesNotExport()
  {
    _project.IsDirty = true;
    var exporters = new[] { new FakeExporter(SmartSaveKind.Image, _calls) };

    var done = await SmartSave.RunAsync(_session, Settings(false, true), exporters, Saver(false));

    done.ShouldBeFalse();
    _calls.ShouldBe(new[] { "save" });
  }

  [Test]
  public async Task MissingPdfExporter_ReportsItAndStillWritesTheImage()
  {
    var exporters = new[] { new FakeExporter(SmartSaveKind.Image, _calls) };

    var done = await SmartSave.RunAsync(_session, Settings(true, true), exporters, Saver(true));

    done.ShouldBeFalse();
    _calls.ShouldBe(new[] { "export Image" });
    _prompts.Messages.Count.ShouldBe(1);
    _prompts.Messages[0].ShouldContain("PDF export is not available yet");
  }

  [Test]
  public async Task ExporterFailure_ShowsErrorWithReason()
  {
    var exporters = new[] { new FakeExporter(SmartSaveKind.Image, _calls) { Fail = true } };

    var done = await SmartSave.RunAsync(_session, Settings(false, true), exporters, Saver(true));

    done.ShouldBeFalse();
    _prompts.Messages.Count.ShouldBe(1);
    _prompts.Messages[0].ShouldContain("There was an error saving the Image file");
    _prompts.Messages[0].ShouldContain("disk full");
  }
}
