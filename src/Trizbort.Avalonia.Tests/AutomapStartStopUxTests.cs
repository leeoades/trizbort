using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;
using Trizbort.Editor;
using Trizbort.Editor.Commands;

namespace Trizbort.Avalonia.Tests;

/// <summary>Tickets 126 and 061: event-driven status, Start/Stop command states and the debug-only single step.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class AutomapStartStopUxTests
{
    private sealed class FakeAutomap : IAutomapController
    {
        public bool IsRunning { get; set; }
        public bool SingleStep { get; set; }
        public string Status { get; set; } = "Automap is not running.";
        public int Steps { get; private set; }

        public event Action<string>? StatusChanged;
        public event Action<Exception>? AutomapFailed;

        public void Step() => Steps++;

        public void RunToCompletion()
        {
        }

        public void RaiseStatus(string status) => StatusChanged?.Invoke(status);

        public void RaiseFailed(Exception exception) => AutomapFailed?.Invoke(exception);
    }

    private Project? _previousProject;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
    }

    [TearDown]
    public void TearDown()
    {
        var created = Project.Current;
        Project.Current = _previousProject!;
        created.Dispose();
    }

    [AvaloniaTest]
    public void StatusText_FollowsEngineEvents_WithoutPolling()
    {
        var fake = new FakeAutomap();
        var service = new CommandService();
        service.Session.Automap = fake;
        var vm = new MainWindowViewModel(service, null!, null!, new AvaloniaAutomapCanvas());

        fake.RaiseStatus("Automapping is processing the transcript.");

        vm.AutomapStatusText.ShouldBe("Automapping is processing the transcript.");
    }

    [AvaloniaTest]
    public void FailureWhileIdle_LeavesVmNotActive()
    {
        var fake = new FakeAutomap();
        var service = new CommandService();
        service.Session.Automap = fake;
        var vm = new MainWindowViewModel(service, null!, null!, new AvaloniaAutomapCanvas());

        fake.RaiseFailed(new InvalidOperationException("boom"));

        vm.IsAutomapActive.ShouldBeFalse();
    }

    [AvaloniaTest]
    public void StartAndStopCommands_FollowRunningState()
    {
        var fake = new FakeAutomap();
        var session = new EditorSession { Automap = fake };
        var catalogue = AppCommands.CreateCatalogue(null);
        var start = catalogue.Find("project.startAutomap")!;
        var stop = catalogue.Find("project.stopAutomap")!;

        start.CanExecute(session).ShouldBeTrue();
        stop.CanExecute(session).ShouldBeFalse();

        fake.IsRunning = true;
        start.CanExecute(session).ShouldBeFalse();
        stop.CanExecute(session).ShouldBeTrue();
    }

    [AvaloniaTest]
    public void SingleStepKey_IsGatedByDebug()
    {
        var fake = new FakeAutomap { IsRunning = true, SingleStep = true };
        var session = new EditorSession { Automap = fake };
        var step = AppCommands.CreateCatalogue(null).Find("automap.step")!;

        step.CanExecute(session).ShouldBeFalse();

        session.DebugEnabled = true;
        step.CanExecute(session).ShouldBeTrue();
        step.ExecuteAsync(session).Wait();
        fake.Steps.ShouldBe(1);
    }

    [AvaloniaTest]
    public void SingleStepOption_IsOnlyExportedWithDebug()
    {
        var vm = new AutomapSettingsViewModel(null!) { IsDebugEnabled = false, SingleStep = true };
        vm.GetSettings().SingleStep.ShouldBeFalse();

        vm.IsDebugEnabled = true;
        vm.GetSettings().SingleStep.ShouldBeTrue();
    }

    [AvaloniaTest]
    public void SingleStepOption_IsIgnoredOnInitializeWithoutDebug()
    {
        var vm = new AutomapSettingsViewModel(null!) { IsDebugEnabled = false };
        var settings = AutomapSettings.Default;
        settings.SingleStep = true;

        vm.Initialize(settings);

        vm.SingleStep.ShouldBeFalse();
    }
}
