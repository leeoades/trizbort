using Avalonia.Controls;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Editor;
using Trizbort.Export;
using AutomapEngine = Trizbort.Automap.Automap;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class AutomapIntegrationTests
{
    private Project? _previousProject;
    private Action<string, string>? _previousErrorReporter;
    private IEditorPrompts? _previousPrompts;
    private string? _tempTranscriptPath;
    private MainWindowViewModel? _lastVm;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
        _previousErrorReporter = AutomapEngine.ErrorReporter;
        _previousPrompts = AutomapEngine.Instance.Prompts;
        _lastVm = null;
    }

    [TearDown]
    public async Task TearDown()
    {
        AutomapEngine.Instance.Stop();
        if (_lastVm?.CurrentAutomapTask != null)
        {
            try
            {
                await _lastVm.CurrentAutomapTask;
            }
            catch { }
        }

        AutomapEngine.ErrorReporter = _previousErrorReporter;
        AutomapEngine.Instance.Prompts = _previousPrompts ?? NonInteractiveEditorPrompts.Instance;

        if (_tempTranscriptPath != null && File.Exists(_tempTranscriptPath))
        {
            try { File.Delete(_tempTranscriptPath); } catch { }
            _tempTranscriptPath = null;
        }

        var created = Project.Current;
        Project.Current = _previousProject!;
        created?.Dispose();
    }

    private class MockStorageService(string? pickedTranscript = null) : IStorageService
    {
        public Task<bool> OpenMapAsync(TopLevel? topLevel) => Task.FromResult(true);
        public Task<bool> SaveMapAsync(TopLevel? topLevel, bool saveAs = false) => Task.FromResult(true);
        public Task<CodeExporter?> PickExporterAsync(TopLevel? topLevel) => Task.FromResult<CodeExporter?>(null);
        public Task<bool> ExportCodeAsync(TopLevel? topLevel, CodeExporter exporter) => Task.FromResult(true);
        public Task<bool> ExportImageAsync(TopLevel? topLevel) => Task.FromResult(true);
        public Task<string?> OpenThemeFileAsync(TopLevel? topLevel) => Task.FromResult<string?>(null);
        public Task<string?> SaveThemeFileAsync(TopLevel? topLevel, string suggestedName) => Task.FromResult<string?>(null);
        public Task<string?> OpenTranscriptFileAsync(TopLevel? topLevel, string? initialPath = null) => Task.FromResult(pickedTranscript);
    }

    private class MockDialogService : IDialogService
    {
        public AutomapSettings? AutomapSettingsToReturn { get; set; }
        public string? LastErrorTitle { get; private set; }
        public string? LastErrorMessage { get; private set; }

        public Task ShowAboutDialogAsync(Window? owner) => Task.CompletedTask;
        public Task ShowMessageAsync(Window? owner, string title, string message)
        {
            LastErrorTitle = title;
            LastErrorMessage = message;
            return Task.CompletedTask;
        }
        public Task<bool> ShowConfirmationAsync(Window? owner, string title, string message) => Task.FromResult(true);
        public Task<ConfirmationResult> ShowSaveDiscardCancelAsync(Window? owner, string title, string message) => Task.FromResult(ConfirmationResult.Discard);
        public Task<bool> ShowRoomPropertiesAsync(Window? owner, Room room) => Task.FromResult(true);
        public Task<bool> ShowConnectionPropertiesAsync(Window? owner, Connection connection) => Task.FromResult(true);
        public Task<bool> ShowMapLabelPropertiesAsync(Window? owner, MapLabel label) => Task.FromResult(true);
        public Task<bool> ShowElementPropertiesAsync(Window? owner, Element element) => Task.FromResult(true);
        public Task<bool> ShowMapSettingsAsync(Window? owner) => Task.FromResult(true);
        public Task<bool> ShowMapThemeAsync(Window? owner) => Task.FromResult(true);
        public Task<bool> ShowAppSettingsAsync(Window? owner) => Task.FromResult(true);
        public Task<AutomapSettings?> ShowAutomapSettingsAsync(Window? owner) => Task.FromResult(AutomapSettingsToReturn);
    }

    [Test]
    public void AutomapSettingsViewModel_InitializeAndGetSettings_RoundTripsCorrectly()
    {
        var storage = new MockStorageService();
        var vm = new AutomapSettingsViewModel(storage);

        var original = AutomapSettings.Default;
        original.FileName = "/tmp/transcript.txt";
        original.VerboseTranscript = true;
        original.AssumeRoomsWithSameNameAreSameRoom = true;
        original.GuessExits = false;
        original.AddObjectCommand = "take item";
        original.AddRegionCommand = "make zone";
        original.ContinueTranscript = true;
        original.AssumeTwoWayConnections = false;

        vm.Initialize(original);

        vm.FileName.ShouldBe("/tmp/transcript.txt");
        vm.CanStart.ShouldBeTrue();
        vm.VerboseTranscript.ShouldBeTrue();
        vm.AssumeRoomsWithSameNameAreSameRoom.ShouldBeTrue();
        vm.IsAssumeRoomsWithSameNameEnabled.ShouldBeTrue();
        vm.GuessExits.ShouldBeFalse();
        vm.AddObjectCommand.ShouldBe("take item");
        vm.AddRegionCommand.ShouldBe("make zone");
        vm.ContinueTranscript.ShouldBeTrue();
        vm.AssumeTwoWayConnections.ShouldBeFalse();

        var exported = vm.GetSettings();
        exported.FileName.ShouldBe("/tmp/transcript.txt");
        exported.SingleStep.ShouldBeFalse();
        exported.VerboseTranscript.ShouldBeTrue();
        exported.AssumeRoomsWithSameNameAreSameRoom.ShouldBeTrue();
        exported.GuessExits.ShouldBeFalse();
        exported.AddObjectCommand.ShouldBe("take item");
        exported.AddRegionCommand.ShouldBe("make zone");
        exported.ContinueTranscript.ShouldBeTrue();
        exported.AssumeTwoWayConnections.ShouldBeFalse();
    }

    [Test]
    public void AutomapSettingsViewModel_VerboseTurnedBackOn_ResetsSameNameRooms()
    {
        var vm = new AutomapSettingsViewModel(new MockStorageService());
        vm.Initialize(AutomapSettings.Default);

        vm.VerboseTranscript = false;
        vm.AssumeRoomsWithSameNameAreSameRoom.ShouldBeTrue();

        vm.VerboseTranscript = true;
        vm.IsAssumeRoomsWithSameNameEnabled.ShouldBeTrue();
        vm.AssumeRoomsWithSameNameAreSameRoom.ShouldBeFalse();
    }

    [Test]
    public void AutomapSettingsViewModel_BlankFileName_ShowsReasonAndDisablesStart()
    {
        var vm = new AutomapSettingsViewModel(new MockStorageService());
        vm.Initialize(AutomapSettings.Default);

        vm.CanStart.ShouldBeFalse();
        vm.IsFileNameMissing.ShouldBeTrue();

        vm.FileName = "transcript.txt";
        vm.CanStart.ShouldBeTrue();
        vm.IsFileNameMissing.ShouldBeFalse();
    }

    [Test]
    public void AutomapSettingsViewModel_NonVerboseTranscript_ForcesSameNameRooms()
    {
        var storage = new MockStorageService();
        var vm = new AutomapSettingsViewModel(storage);

        vm.Initialize(AutomapSettings.Default);
        vm.VerboseTranscript.ShouldBeTrue();
        vm.IsAssumeRoomsWithSameNameEnabled.ShouldBeTrue();

        vm.VerboseTranscript = false;
        vm.IsAssumeRoomsWithSameNameEnabled.ShouldBeFalse();
        vm.AssumeRoomsWithSameNameAreSameRoom.ShouldBeTrue();

        var settings = vm.GetSettings();
        settings.VerboseTranscript.ShouldBeFalse();
        settings.AssumeRoomsWithSameNameAreSameRoom.ShouldBeTrue();
    }

    [Test]
    public async Task AutomapSettingsViewModel_BrowseTranscript_UpdatesFileName()
    {
        var storage = new MockStorageService("/path/to/picked.log");
        var vm = new AutomapSettingsViewModel(storage);
        vm.FileName = "/old/path.txt";

        await vm.BrowseTranscriptAsync(null);
        vm.FileName.ShouldBe("/path/to/picked.log");
    }

    [Test]
    public async Task AutomapSettingsViewModel_BrowseTranscript_Cancelled_KeepsPreviousFileName()
    {
        var storage = new MockStorageService(null);
        var vm = new AutomapSettingsViewModel(storage);
        vm.FileName = "/old/path.txt";

        await vm.BrowseTranscriptAsync(null);
        vm.FileName.ShouldBe("/old/path.txt");
    }

    [Test]
    public async Task MainWindowViewModel_ToggleAutomap_CancelledDialog_LeavesAutomapStopped()
    {
        var commandService = new CommandService();
        var storage = new MockStorageService();
        var dialog = new MockDialogService { AutomapSettingsToReturn = null };
        var canvas = new AvaloniaAutomapCanvas();

        var vm = new MainWindowViewModel(commandService, storage, dialog, canvas);
        _lastVm = vm;
        vm.IsAutomapActive.ShouldBeFalse();

        await vm.ToggleAutomapAsync();

        vm.IsAutomapActive.ShouldBeFalse();
        AutomapEngine.Instance.Running.ShouldBeFalse();
    }

    [Test]
    public async Task MainWindowViewModel_ToggleAutomap_EmptyFileName_ShowsErrorAndLeavesStopped()
    {
        var commandService = new CommandService();
        var storage = new MockStorageService();
        var emptySettings = AutomapSettings.Default;
        emptySettings.FileName = "   ";
        var dialog = new MockDialogService
        {
            AutomapSettingsToReturn = emptySettings
        };
        var canvas = new AvaloniaAutomapCanvas();

        var vm = new MainWindowViewModel(commandService, storage, dialog, canvas);
        _lastVm = vm;
        await vm.ToggleAutomapAsync();

        vm.IsAutomapActive.ShouldBeFalse();
        AutomapEngine.Instance.Running.ShouldBeFalse();
        dialog.LastErrorTitle.ShouldBe("Automap Error");
    }

    [Test]
    public async Task Automap_LiveTranscript_CreatesRoomsAndExplicitStopWorks()
    {
        _tempTranscriptPath = Path.Combine(Path.GetTempPath(), $"trizbort_test_{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(_tempTranscriptPath,
            "Example Adventure\n\nLiving Room\nA cozy room with a fireplace.\n\n>n\n\nKitchen\nA bright kitchen with a refrigerator.\n\n>look\n\n");

        var commandService = new CommandService();
        var storage = new MockStorageService();
        var dialog = new MockDialogService();
        var canvas = new AvaloniaAutomapCanvas();

        var vm = new MainWindowViewModel(commandService, storage, dialog, canvas);
        _lastVm = vm;

        var settings = AutomapSettings.Default;
        settings.FileName = _tempTranscriptPath;
        settings.VerboseTranscript = true;
        settings.AssumeTwoWayConnections = true;
        settings.GuessExits = true;

        await vm.StartAutomapAsync(settings);
        vm.IsAutomapActive.ShouldBeTrue();

        // Give the background automapper task time to read and process the lines
        var timeout = DateTime.UtcNow.AddSeconds(4);
        while (DateTime.UtcNow < timeout && Project.Current.Elements.OfType<Room>().Count() < 2)
        {
            await Task.Delay(50);
        }

        var rooms = Project.Current.Elements.OfType<Room>().ToList();
        rooms.Count.ShouldBeGreaterThanOrEqualTo(2);
        rooms.Any(r => r.Name == "Living Room").ShouldBeTrue();
        rooms.Any(r => r.Name == "Kitchen").ShouldBeTrue();

        // Explicit Stop
        vm.StopAutomap();
        if (vm.CurrentAutomapTask != null)
        {
            await vm.CurrentAutomapTask;
        }

        vm.IsAutomapActive.ShouldBeFalse();
        AutomapEngine.Instance.Running.ShouldBeFalse();
        vm.AutomapStatusText.ShouldBe("Automap is not running.");
    }

    [Test]
    public async Task Automap_DeterministicMissingFile_ReportsErrorAndAutonomousReset()
    {
        var commandService = new CommandService();
        var storage = new MockStorageService();
        var dialog = new MockDialogService();
        var canvas = new AvaloniaAutomapCanvas();

        // Errors reach the dialog service through the engine's IEditorPrompts
        AutomapEngine.Instance.Prompts = new AvaloniaEditorPrompts(() => null, dialog);

        var vm = new MainWindowViewModel(commandService, storage, dialog, canvas);
        _lastVm = vm;
        var missingPath = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.txt");

        var settings = AutomapSettings.Default;
        settings.FileName = missingPath;
        settings.VerboseTranscript = true;

        await vm.StartAutomapAsync(settings);

        if (vm.CurrentAutomapTask != null)
        {
            await vm.CurrentAutomapTask;
        }

        // Wait briefly for dispatcher timer / reset
        var timeout = DateTime.UtcNow.AddSeconds(2);
        while (DateTime.UtcNow < timeout && vm.IsAutomapActive)
        {
            await Task.Delay(50);
        }

        vm.IsAutomapActive.ShouldBeFalse();
        dialog.LastErrorTitle.ShouldNotBeNull();
        dialog.LastErrorMessage.ShouldNotBeNull();
        dialog.LastErrorMessage!.ShouldContain("Error opening transcript file");
    }

    [Test]
    public async Task MainWindowViewModel_NewMapAndOpenMap_StopsActiveAutomap()
    {
        _tempTranscriptPath = Path.Combine(Path.GetTempPath(), $"trizbort_test_{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(_tempTranscriptPath, "Game\nRoom 1\n>look\n");

        var commandService = new CommandService();
        var storage = new MockStorageService();
        var dialog = new MockDialogService();
        var canvas = new AvaloniaAutomapCanvas();

        var vm = new MainWindowViewModel(commandService, storage, dialog, canvas);
        _lastVm = vm;

        var settings = AutomapSettings.Default;
        settings.FileName = _tempTranscriptPath;

        // Start automap
        await vm.StartAutomapAsync(settings);
        vm.IsAutomapActive.ShouldBeTrue();

        // NewMap stops active automap
        await vm.NewMapAsync();
        vm.IsAutomapActive.ShouldBeFalse();

        // Start again
        await vm.StartAutomapAsync(settings);
        vm.IsAutomapActive.ShouldBeTrue();

        // OpenMap stops active automap
        await vm.OpenMapAsync();
        vm.IsAutomapActive.ShouldBeFalse();
    }
}
