using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Editor.Commands;
using Trizbort.Editor.Input;
using Trizbort.Domain.Enums;

namespace Trizbort.Core.Tests;

/// <summary>Tickets 061 and 126: run-to-completion and single-step keys while automapping.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class AutomapKeyCommandTests
{
  private sealed class FakeAutomap : IAutomapController
  {
    public bool IsRunning { get; set; }
    public bool SingleStep { get; set; }
    public string Status { get; set; } = "Automap is not running.";
    public int Steps { get; private set; }
    public int Runs { get; private set; }

    public event Action<string> StatusChanged;
    public event Action<Exception> AutomapFailed;

    public void Step()
    {
      Steps++;
    }

    public void RunToCompletion()
    {
      Runs++;
    }

    public bool MoveByMove { get; set; }
    public int MovesLeft { get; set; }

    public int MovesAhead() => MovesLeft;
    public int Moves { get; private set; }

    public void PlayNextMove()
    {
      MoveByMove = true;
      Moves++;
    }

    public int Backs { get; private set; }

    public void GoBack() => Backs++;

    public Room CurrentRoom { get; set; }

    public void SetCurrentRoom(Room room) => CurrentRoom = room;

    public void RaiseStatus(string status)
    {
      StatusChanged?.Invoke(status);
    }

    public void RaiseFailed(Exception exception)
    {
      AutomapFailed?.Invoke(exception);
    }
  }

  private sealed class ConfirmingPrompts : IEditorPrompts
  {
    public bool Answer { get; set; }
    public string Asked { get; private set; }

    public Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(Room existing, string newRoomName, CancellationToken token = default) =>
      Task.FromResult(AutomapSameDirectionResult.Cancel);

    public Task<RoomDisambiguation> DisambiguateRoomAsync(string transcriptLine, string roomDescription, IReadOnlyList<Room> candidates, CancellationToken token = default) =>
      Task.FromResult<RoomDisambiguation>(default);

    public Task<bool> ConfirmAsync(string message, string title, CancellationToken token = default)
    {
      Asked = message;
      return Task.FromResult(Answer);
    }
  }

  private Project _previousProject;
  private Project _project;
  private EditorSession _session;
  private CommandCatalogue _catalogue;
  private FakeAutomap _automap;

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _automap = new FakeAutomap();
    _session = new EditorSession { Automap = _automap };
    _catalogue = CommandCatalogue.CreateDefault();
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
  public void Run_DisabledWhenNotRunning_AndDoesNotCallAutomap()
  {
    var run = _catalogue.Find("automap.run");

    run.CanExecute(_session).ShouldBeFalse();
    _automap.Runs.ShouldBe(0);
  }

  [Test]
  public void Run_WhenRunning_CallsRunToCompletion()
  {
    _automap.IsRunning = true;
    var run = _catalogue.Find("automap.run");

    run.CanExecute(_session).ShouldBeTrue();
    run.ExecuteAsync(_session).Wait();

    _automap.Runs.ShouldBe(1);
  }

  [Test]
  public void PlayToTheEnd_WithManyMovesLeft_AsksFirst()
  {
    var prompts = new ConfirmingPrompts();
    _session.Prompts = prompts;
    _automap.IsRunning = true;
    _automap.MoveByMove = true;
    _automap.MovesLeft = 250;
    var run = _catalogue.Find("automap.run");

    run.ExecuteAsync(_session).Wait();
    prompts.Asked.ShouldContain("250 moves");
    _automap.Runs.ShouldBe(0); // said no

    prompts.Answer = true;
    run.ExecuteAsync(_session).Wait();
    _automap.Runs.ShouldBe(1);
  }

  [Test]
  public void PlayToTheEnd_WithFewMovesLeft_OrAlreadyPlaying_JustPlays()
  {
    var prompts = new ConfirmingPrompts();
    _session.Prompts = prompts;
    _automap.IsRunning = true;
    _automap.MoveByMove = true;
    _automap.MovesLeft = CommandCatalogue.ConfirmPlayToEndMoves - 1;
    var run = _catalogue.Find("automap.run");

    run.ExecuteAsync(_session).Wait();
    _automap.MoveByMove = false;
    _automap.MovesLeft = 1000;
    run.ExecuteAsync(_session).Wait();

    prompts.Asked.ShouldBeNull();
    _automap.Runs.ShouldBe(2);
  }

  [Test]
  public void F10_PlaysTheNextMove_WhileAutomapRuns()
  {
    var next = _catalogue.FindByGesture(new KeyGesture(InputKey.F10, ModifierKeys.None), CommandScope.Canvas);
    next.ShouldNotBeNull();
    next.Id.ShouldBe("automap.nextMove");
    next.CanExecute(_session).ShouldBeFalse();

    _automap.IsRunning = true;
    next.CanExecute(_session).ShouldBeTrue();
    next.ExecuteAsync(_session).Wait();

    _automap.Moves.ShouldBe(1);
    _automap.MoveByMove.ShouldBeTrue();
  }

  [Test]
  public void F9_GoesBackAMove_WhileAutomapRuns()
  {
    var back = _catalogue.FindByGesture(new KeyGesture(InputKey.F9, ModifierKeys.None), CommandScope.Canvas);
    back.ShouldNotBeNull();
    back.Id.ShouldBe("automap.back");
    back.CanExecute(_session).ShouldBeFalse();

    _automap.IsRunning = true;
    back.ExecuteAsync(_session).Wait();
    _automap.Backs.ShouldBe(1);
  }

  [Test]
  public void GoToCurrentRoom_SelectsTheRoomAutomapIsIn_AndCentresOnIt()
  {
    var command = _catalogue.Find("automap.currentRoom");
    var far = new Room(_project) { Name = "Far Away", Position = new Vector(5000, 4000) };
    _project.Elements.Add(far);
    command.CanExecute(_session).ShouldBeFalse(); // not running

    _automap.IsRunning = true;
    command.CanExecute(_session).ShouldBeFalse(); // doesn't know where the player is
    _automap.CurrentRoom = far;
    command.CanExecute(_session).ShouldBeTrue();
    command.ExecuteAsync(_session).Wait();

    _session.Selection.Items.ShouldBe(new Element[] { far });
    _session.Viewport.VisibleWorldBounds.Contains(far.InnerBounds.Center).ShouldBeTrue();
  }

  [Test]
  public void ImHere_TellsAutomapThePlayerIsInTheRoom()
  {
    var command = _catalogue.Find("automap.imHere");
    var cellar = new Room(_project) { Name = "Cellar" };
    _project.Elements.Add(cellar);
    _session.Selection.Set(new Element[] { cellar });
    command.CanExecute(_session).ShouldBeFalse(); // not running

    _automap.IsRunning = true;
    command.CanExecute(_session).ShouldBeTrue();
    command.ExecuteAsync(_session).Wait();

    _automap.CurrentRoom.ShouldBe(cellar);
  }

  [Test]
  public void Step_DisabledWhenNotRunning_OrNotSingleStepping_OrNotDebug()
  {
    var step = _catalogue.Find("automap.step");
    _session.DebugEnabled = true;
    _automap.SingleStep = true;

    step.CanExecute(_session).ShouldBeFalse(); // not running

    _automap.IsRunning = true;
    step.CanExecute(_session).ShouldBeTrue();

    _automap.SingleStep = false;
    step.CanExecute(_session).ShouldBeFalse();

    _automap.SingleStep = true;
    _session.DebugEnabled = false;
    step.CanExecute(_session).ShouldBeFalse();
  }

  [Test]
  public void Step_WhenAvailable_CallsStep()
  {
    _session.DebugEnabled = true;
    _automap.IsRunning = true;
    _automap.SingleStep = true;

    _catalogue.Find("automap.step").ExecuteAsync(_session).Wait();

    _automap.Steps.ShouldBe(1);
  }

  [Test]
  public void Gestures_UseExactModifiers_InCanvasScope()
  {
    var run = _catalogue.Find("automap.run");
    var step = _catalogue.Find("automap.step");
    run.Scope.ShouldBe(CommandScope.Canvas);
    step.Scope.ShouldBe(CommandScope.Canvas);

    _catalogue.FindByGesture(new KeyGesture(InputKey.F5, ModifierKeys.None), CommandScope.Canvas).ShouldBeSameAs(run);
    // Ctrl+F5 is Start Room, never run-to-completion
    _catalogue.FindByGesture(new KeyGesture(InputKey.F5, ModifierKeys.Primary), CommandScope.Canvas)?.Id.ShouldNotBe("automap.run");

    if (!OperatingSystem.IsMacOS())
      _catalogue.FindByGesture(new KeyGesture(InputKey.F11, ModifierKeys.None), CommandScope.Canvas).ShouldBeSameAs(step);
  }

  [Test]
  public void SessionForwardsAutomapEvents()
  {
    string status = null;
    Exception failure = null;
    _session.AutomapStatusChanged += s => status = s;
    _session.AutomapFailed += e => failure = e;

    var error = new InvalidOperationException("boom");
    _automap.RaiseStatus("Automapping has started.");
    _automap.RaiseFailed(error);

    status.ShouldBe("Automapping has started.");
    failure.ShouldBeSameAs(error);
  }

  [Test]
  public void Automap_DefaultsToTheSharedEngine()
  {
    using var fresh = new EditorSession();

    fresh.Automap.ShouldBeSameAs(global::Trizbort.Automap.Automap.Instance);
  }
}
