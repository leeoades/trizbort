using System.Reflection;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Export;
using NUnit.Framework;
using Shouldly;

namespace Trizbort.Avalonia.Tests;

/// <summary>Ticket 107: --automap, --quicksave, --smartsave and --exit run in order, isolated and without prompts.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class StartupCommandLineTests
{
    private const string TwoRooms =
        "Example Game\n\nFirst Room\nA small room.\n\n>e\n\nSecond Room\nAnother room.\n";

    private sealed class RecordingWriters : IMapImageWriter, IMapPdfWriter
    {
        private readonly List<string> _events;
        private readonly Func<bool> _probe;

        public RecordingWriters(List<string> events, Func<bool> probe)
        {
            _events = events;
            _probe = probe;
        }

        public bool FailPdf { get; set; }

        public Task WriteAsync(Project project, string path, ImageFormat format)
        {
            _events.Add("image" + (_probe() ? "-ready" : "-early"));
            return Task.CompletedTask;
        }

        public Task WriteAsync(Project project, string path, PdfMetadata metadata)
        {
            if (FailPdf) throw new InvalidOperationException("pdf locked");
            _events.Add("pdf" + (_probe() ? "-ready" : "-early"));
            return Task.CompletedTask;
        }
    }

    private Project? _previousProject;
    private IMapImageWriter? _previousImage;
    private IMapPdfWriter? _previousPdf;
    private Action? _previousExit;
    private string _dir = string.Empty;
    private List<string> _events = new();

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
        _previousImage = WebAndSmartSaveCommands.ImageWriter;
        _previousPdf = WebAndSmartSaveCommands.PdfWriter;
        _previousExit = StartupActions.ExitApplication;
        _dir = Path.Combine(Path.GetTempPath(), "Trizbort.Avalonia.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _events = new List<string>();
        App.ExitCode = 0;
    }

    [TearDown]
    public void TearDown()
    {
        App.ExitCode = 0;
        StartupActions.ExitApplication = _previousExit!;
        WebAndSmartSaveCommands.ImageWriter = _previousImage!;
        WebAndSmartSaveCommands.PdfWriter = _previousPdf!;
        Project.FileWatcher.StopWatcher();
        var created = Project.Current;
        Project.Current = _previousProject!;
        created?.Dispose();
        Directory.Delete(_dir, true);
    }

    private static MainWindowViewModel CreateViewModel()
    {
        var storage = DispatchProxy.Create<IStorageService, StartupTests.RecordingProxy>();
        var dialogs = DispatchProxy.Create<IDialogService, StartupTests.RecordingProxy>();
        return new MainWindowViewModel(new CommandService(), storage, dialogs, new AvaloniaAutomapCanvas());
    }

    private string WriteTranscript()
    {
        var path = Path.Combine(_dir, "transcript.txt");
        File.WriteAllText(path, TwoRooms);
        return path;
    }

    private static ApplicationSettings SmartSettings()
    {
        return new ApplicationSettings { SaveToPDF = true, SaveToImage = true };
    }

    [Test]
    public async Task AutomapExportQuickSaveSmartSaveExit_RunInOrder()
    {
        var quick = Path.Combine(_dir, "mapped.trizbort");
        var inform = Path.Combine(_dir, "mapped.inf");
        Func<bool> ready = () => File.Exists(quick) && File.Exists(inform);
        var writers = new RecordingWriters(_events, ready);
        WebAndSmartSaveCommands.ImageWriter = writers;
        WebAndSmartSaveCommands.PdfWriter = writers;
        StartupActions.ExitApplication = () => _events.Add("exit" + (ready() ? "-ready" : "-early"));
        var options = new CommandLineOptions
        {
            Transcript = WriteTranscript(),
            I6 = inform,
            QuickSave = quick,
            SmartSave = true,
            Exit = true
        };
        var output = new StringWriter();
        var error = new StringWriter();

        await StartupActions.RunCommandLineActionsAsync(CreateViewModel(), options, output, error, SmartSettings());

        error.ToString().ShouldBeEmpty();
        Project.Current.Elements.OfType<Room>().Count().ShouldBe(2);
        _events.ShouldBe(new[] { "pdf-ready", "image-ready", "exit-ready" });
        App.ExitCode.ShouldBe(0);
    }

    [Test]
    public async Task Exit_DoesNotPromptAboutUnsavedChanges()
    {
        var storage = DispatchProxy.Create<IStorageService, StartupTests.RecordingProxy>();
        var dialogs = DispatchProxy.Create<IDialogService, StartupTests.RecordingProxy>();
        var vm = new MainWindowViewModel(new CommandService(), storage, dialogs, new AvaloniaAutomapCanvas());
        Project.Current.IsDirty = true;
        var exited = false;
        StartupActions.ExitApplication = () =>
        {
            exited = true;
            Project.Current.IsDirty.ShouldBeFalse();
        };

        await StartupActions.RunCommandLineActionsAsync(
            vm, new CommandLineOptions { Exit = true }, new StringWriter(), new StringWriter(), SmartSettings());

        exited.ShouldBeTrue();
        ((StartupTests.RecordingProxy)(object)dialogs).Calls.ShouldBeEmpty();
        ((StartupTests.RecordingProxy)(object)storage).Calls.ShouldBeEmpty();
    }

    [Test]
    public async Task FailingSteps_ReportToStderrSetExitCodeAndStillExit()
    {
        var exited = false;
        StartupActions.ExitApplication = () => exited = true;
        var options = new CommandLineOptions
        {
            Transcript = Path.Combine(_dir, "missing.txt"),
            QuickSave = Path.Combine(_dir, "no-such-folder", "x.trizbort"),
            SmartSave = true,
            Exit = true
        };
        var error = new StringWriter();

        await StartupActions.RunCommandLineActionsAsync(CreateViewModel(), options, new StringWriter(), error, SmartSettings());

        var text = error.ToString();
        text.ShouldContain("Could not automap");
        text.ShouldContain("Could not quick save");
        text.ShouldContain("Could not smart save");
        App.ExitCode.ShouldBe(1);
        exited.ShouldBeTrue();
    }

    [Test]
    public async Task SmartSave_PdfFailure_StillWritesImageAndFailsExitCode()
    {
        var quick = Path.Combine(_dir, "mapped.trizbort");
        var writers = new RecordingWriters(_events, () => true) { FailPdf = true };
        WebAndSmartSaveCommands.ImageWriter = writers;
        WebAndSmartSaveCommands.PdfWriter = writers;
        var options = new CommandLineOptions { QuickSave = quick, SmartSave = true };
        var error = new StringWriter();

        await StartupActions.RunCommandLineActionsAsync(CreateViewModel(), options, new StringWriter(), error, SmartSettings());

        _events.ShouldBe(new[] { "image-ready" });
        error.ToString().ShouldContain("pdf locked");
        App.ExitCode.ShouldBe(1);
    }

    [Test]
    public async Task NameOption_IsIgnored()
    {
        var error = new StringWriter();

        await StartupActions.RunCommandLineActionsAsync(
            CreateViewModel(), new CommandLineOptions { Name = "Whatever" }, new StringWriter(), error, SmartSettings());

        error.ToString().ShouldBeEmpty();
        App.ExitCode.ShouldBe(0);
    }
}
