using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Editor;
using Parser = Trizbort.Automap.Automap;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 127: automap errors stop the run cleanly and are reported once through IEditorPrompts.</summary>
[TestFixture]
[Category("Integration")]
public class AutomapErrorTests : IsolatedProjectTests
{
  private const string TwoRooms =
    "Example Game\n\nFirst Room\nA small room.\n\n>e\n\nSecond Room\nAnother room.\n";

  private sealed class RecordingPrompts : IEditorPrompts
  {
    public readonly List<(string Title, string Message)> Messages = new();

    public Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(
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
      Messages.Add((title, message));
      return Task.CompletedTask;
    }
  }

  /// <summary>Creates the first room, then throws when asked for a second.</summary>
  private sealed class ThrowingCanvas : IAutomapCanvas
  {
    private readonly AutomapCanvas _inner;
    private int _created;

    public ThrowingCanvas(Parser parser)
    {
      _inner = new AutomapCanvas(null, parser);
    }

    private void CountCreation()
    {
      _created++;
      if (_created > 1) throw new InvalidOperationException("canvas exploded");
    }

    public RoomSearchResult FindRooms(string roomName, string roomDescription, RoomMatcher matcher)
    {
      return _inner.FindRooms(roomName, roomDescription, matcher);
    }

    public Room CreateRoom(Room existing, string name)
    {
      CountCreation();
      return _inner.CreateRoom(existing, name);
    }

    public Room CreateRoom(Room existing, MappableDirection directionFromExisting, string roomName, string line)
    {
      CountCreation();
      return _inner.CreateRoom(existing, directionFromExisting, roomName, line);
    }

    public void Connect(Room source, MappableDirection directionFromSource, Room target, bool assumeTwoWayConnections)
    {
      _inner.Connect(source, directionFromSource, target, assumeTwoWayConnections);
    }

    public void AddExitStub(Room room, MappableDirection direction)
    {
      _inner.AddExitStub(room, direction);
    }

    public void RemoveExitStub(Room room, MappableDirection direction)
    {
      _inner.RemoveExitStub(room, direction);
    }

    public void SelectRoom(Room room)
    {
      _inner.SelectRoom(room);
    }

    public void RemoveRoom(Room otherRoom)
    {
      _inner.RemoveRoom(otherRoom);
    }
  }

  private AutomapSettings SettingsFor(string fileName)
  {
    var settings = AutomapSettings.Default;
    settings.FileName = fileName;
    settings.VerboseTranscript = true;
    settings.AssumeTwoWayConnections = true;
    settings.GuessExits = false;
    return settings;
  }

  private string WriteTranscript(string text)
  {
    var path = Files.File("transcript.txt");
    File.WriteAllText(path, text);
    return path;
  }

  [Test]
  public async Task MissingTranscript_StopsWithOneMessageAndRaisesFailed()
  {
    var prompts = new RecordingPrompts();
    var parser = new Parser(null, prompts);
    Exception failure = null;
    parser.AutomapFailed += e => failure = e;

    await parser.Start(new AutomapCanvas(null, parser), SettingsFor(Files.File("missing.txt")));

    prompts.Messages.Count.ShouldBe(1);
    prompts.Messages[0].Title.ShouldBe("File Error");
    prompts.Messages[0].Message.ShouldContain("Error opening transcript file");
    parser.Status.ShouldBe("Automapping halted.");
    parser.Running.ShouldBeFalse();
    failure.ShouldBeAssignableTo<IOException>();
  }

  [Test]
  public async Task UnreadableTranscript_StopsWithOneMessage()
  {
    // a directory cannot be opened as a transcript file
    var prompts = new RecordingPrompts();
    var parser = new Parser(null, prompts);
    var failed = 0;
    parser.AutomapFailed += _ => failed++;

    await parser.Start(new AutomapCanvas(null, parser), SettingsFor(Files.Path));

    prompts.Messages.Count.ShouldBe(1);
    parser.Status.ShouldBe("Automapping halted.");
    parser.Running.ShouldBeFalse();
    failed.ShouldBe(1);
  }

  [Test]
  public async Task ExceptionFromCanvas_StopsRunAndShowsOneMessage()
  {
    var prompts = new RecordingPrompts();
    var parser = new Parser(null, prompts);
    Exception failure = null;
    parser.AutomapFailed += e => failure = e;

    await parser.StartCl(new ThrowingCanvas(parser), SettingsFor(WriteTranscript(TwoRooms)));

    prompts.Messages.Count.ShouldBe(1);
    prompts.Messages[0].Title.ShouldBe("Automap Error");
    prompts.Messages[0].Message.ShouldContain("canvas exploded");
    parser.Status.ShouldBe("Automapping halted.");
    parser.Running.ShouldBeFalse();
    failure.ShouldBeOfType<InvalidOperationException>();
  }

  [Test]
  public async Task ExceptionMidRun_KeepsEarlierRoomsAsOneUndoStep()
  {
    var prompts = new RecordingPrompts();
    var parser = new Parser(null, prompts);
    using var session = new EditorSession();

    using (session.BeginTransaction("Automap"))
    {
      await parser.StartCl(new ThrowingCanvas(parser), SettingsFor(WriteTranscript(TwoRooms)));
    }

    Project.Current.Elements.OfType<Room>().Count().ShouldBe(1);
    session.Undo.CanUndo.ShouldBeTrue();
    session.Undo.UndoTitle.ShouldBe("Automap");

    session.PerformUndo();

    Project.Current.Elements.OfType<Room>().Count().ShouldBe(0);
    session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public async Task LegacyStaticReporter_StillReceivesTheMessage()
  {
    var previous = Parser.ErrorReporter;
    var calls = new List<(string Message, string Title)>();
    try
    {
      Parser.ErrorReporter = (message, title) => calls.Add((message, title));
      var parser = new Parser();

      await parser.Start(new AutomapCanvas(null, parser), SettingsFor(Files.File("missing.txt")));

      calls.Count.ShouldBe(1);
      calls[0].Title.ShouldBe("File Error");
    }
    finally
    {
      Parser.ErrorReporter = previous;
    }
  }

  [Test]
  public async Task NormalCompletion_ReportsNothing()
  {
    var prompts = new RecordingPrompts();
    var parser = new Parser(null, prompts);
    var failed = 0;
    parser.AutomapFailed += _ => failed++;

    await parser.StartCl(new AutomapCanvas(null, parser), SettingsFor(WriteTranscript(TwoRooms)));

    prompts.Messages.ShouldBeEmpty();
    failed.ShouldBe(0);
    parser.Status.ShouldBe("Automapping has completed.");
  }
}
