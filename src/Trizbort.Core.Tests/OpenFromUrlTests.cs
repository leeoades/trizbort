using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Editor;

namespace Trizbort.Core.Tests;

/// <summary>Tickets 060 and 089: Open Map from Web, with a fake downloader (no network).</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class OpenFromUrlTests
{
  private const string MapUrlText = "https://example.com/maps/castle.trizbort";

  private Project _previousProject;
  private Project _project;
  private EditorSession _session;
  private UrlPrompts _prompts;
  private FakeDownloader _downloader;
  private readonly List<string> _recorded = new();

  private sealed class UrlPrompts : IEditorPrompts
  {
    public string UrlAnswer { get; set; }

    public int UrlAsked { get; private set; }

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

    public Task<string> PromptUrlAsync(string title, string label, CancellationToken token = default)
    {
      UrlAsked++;
      return Task.FromResult(UrlAnswer);
    }

    public Task ShowMessageAsync(string title, string message, CancellationToken token = default)
    {
      Messages.Add(message);
      return Task.CompletedTask;
    }
  }

  private sealed class FakeDownloader : IMapDownloader
  {
    public byte[] Data { get; set; }

    public Exception Failure { get; set; }

    public List<Uri> Requested { get; } = new();

    public Task<byte[]> DownloadAsync(Uri uri, CancellationToken token = default)
    {
      Requested.Add(uri);
      if (Failure != null) throw Failure;
      return Task.FromResult(Data);
    }
  }

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _session = new EditorSession();
    _prompts = new UrlPrompts();
    _session.Prompts = _prompts;
    _downloader = new FakeDownloader { Data = MapBytes("Cellar") };
    _recorded.Clear();
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

  private static byte[] MapBytes(string roomName)
  {
    using var source = new Project();
    source.Elements.Add(new Room(source) { Name = roomName });
    using var stream = new MemoryStream();
    new LegacyMapFileEngine(source).Save(stream);
    return stream.ToArray();
  }

  private static Task<bool> Allow() => Task.FromResult(true);

  private static Task<bool> Refuse() => Task.FromResult(false);

  [TestCase("")]
  [TestCase("   ")]
  [TestCase("not a url")]
  [TestCase("ftp://example.com/map.trizbort")]
  [TestCase("file:///tmp/map.trizbort")]
  [TestCase("/tmp/map.trizbort")]
  public void TryParse_RejectsInvalidAndNonHttpAddresses(string text)
  {
    MapUrl.TryParse(text, out var uri, out var error).ShouldBeFalse();
    uri.ShouldBeNull();
    error.ShouldNotBeNullOrEmpty();
  }

  [Test]
  public void TryParse_AcceptsHttpAndHttpsAndTrimsWhiteSpace()
  {
    MapUrl.TryParse("  http://example.com/a.trizbort ", out var http, out _).ShouldBeTrue();
    http.Host.ShouldBe("example.com");
    MapUrl.TryParse(MapUrlText, out var https, out _).ShouldBeTrue();
    MapUrl.FileName(https).ShouldBe("castle.trizbort");
  }

  [Test]
  public async Task Run_InvalidUrl_ShowsMessageAndKeepsTheCurrentMap()
  {
    _prompts.UrlAnswer = "ftp://example.com/x";

    var opened = await OpenFromUrl.RunAsync(_session, _downloader, Allow, _recorded.Add);

    opened.ShouldBeFalse();
    _downloader.Requested.ShouldBeEmpty();
    _prompts.Messages.Count.ShouldBe(1);
    _prompts.Messages[0].ShouldStartWith("Could not open ftp://example.com/x: ");
    Project.Current.ShouldBeSameAs(_project);
  }

  [Test]
  public async Task Run_Success_LoadsTheMapNamedByTheUrlAndRecordsIt()
  {
    _prompts.UrlAnswer = MapUrlText;

    var opened = await OpenFromUrl.RunAsync(_session, _downloader, Allow, _recorded.Add);

    opened.ShouldBeTrue();
    _downloader.Requested.Count.ShouldBe(1);
    _downloader.Requested[0].AbsoluteUri.ShouldBe(MapUrlText);
    var project = _session.Project;
    project.ShouldNotBeSameAs(_project);
    project.FileName.ShouldBe(MapUrlText);
    project.IsDirty.ShouldBeFalse();
    project.Elements.OfType<Room>().Select(r => r.Name).ShouldBe(new[] { "Cellar" });
    _recorded.ShouldBe(new[] { MapUrlText });
    _prompts.Messages.ShouldBeEmpty();
  }

  [Test]
  public async Task Run_DirtyCheckCancelled_AbortsWithoutAsking()
  {
    _prompts.UrlAnswer = MapUrlText;

    var opened = await OpenFromUrl.RunAsync(_session, _downloader, Refuse, _recorded.Add);

    opened.ShouldBeFalse();
    _prompts.UrlAsked.ShouldBe(0);
    _downloader.Requested.ShouldBeEmpty();
    Project.Current.ShouldBeSameAs(_project);
  }

  [Test]
  public async Task Run_PromptCancelled_DoesNothing()
  {
    _prompts.UrlAnswer = null;

    var opened = await OpenFromUrl.RunAsync(_session, _downloader, Allow, _recorded.Add);

    opened.ShouldBeFalse();
    _prompts.Messages.ShouldBeEmpty();
    Project.Current.ShouldBeSameAs(_project);
  }

  [Test]
  public async Task Open_DownloadFails_ShowsCouldNotOpenWithTheReason()
  {
    _downloader.Failure = new InvalidOperationException("the server answered 404 Not Found");

    var opened = await OpenFromUrl.OpenAsync(_session, _downloader, MapUrlText, _recorded.Add);

    opened.ShouldBeFalse();
    _prompts.Messages.ShouldBe(new[] { $"Could not open {MapUrlText}: the server answered 404 Not Found" });
    _recorded.ShouldBeEmpty();
    Project.Current.ShouldBeSameAs(_project);
  }

  [Test]
  public async Task Open_NotAMap_ShowsCouldNotOpen()
  {
    _downloader.Data = System.Text.Encoding.UTF8.GetBytes("<html>nope</html>");

    var opened = await OpenFromUrl.OpenAsync(_session, _downloader, MapUrlText, _recorded.Add);

    opened.ShouldBeFalse();
    _prompts.Messages.Count.ShouldBe(1);
    _prompts.Messages[0].ShouldStartWith($"Could not open {MapUrlText}: ");
    Project.Current.ShouldBeSameAs(_project);
  }
}
