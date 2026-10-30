using System;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
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

    public void RaiseStatus(string status)
    {
      StatusChanged?.Invoke(status);
    }

    public void RaiseFailed(Exception exception)
    {
      AutomapFailed?.Invoke(exception);
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
