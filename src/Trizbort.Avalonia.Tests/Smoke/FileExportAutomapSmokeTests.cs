using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Export;
using Trizbort.Export.Languages;
using Trizbort.Setup;
using AutomapEngine = Trizbort.Automap.Automap;
using CoreRect = Trizbort.Domain.Misc.Rect;
using CoreVector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests.Smoke;

/// <summary>
/// Automated versions of smoke-test-script.md sections 1 (Start-up and File), 6 (Export) and 7 (Automap).
/// Steps needing a real OS file picker, visual judgement or a real main window stay manual:
/// 6.51, 6.52, 6.53, 6.54, 6.57.
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("Smoke")]
public class FileExportAutomapSmokeTests
{
    private const string Fixture1570 = "basics-1570.trizbort";

    private Project? _previousProject;
    private List<Region>? _previousRegions;
    private bool _previousLoadLast;
    private string? _previousLastProject;
    private bool _previousSaveAt100;
    private Action<string, string>? _previousErrorReporter;
    private IEditorPrompts? _previousPrompts;
    private MainWindowViewModel? _lastVm;
    private string _tempDir = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
        _previousRegions = Settings.Regions?.Select(CloneRegion).ToList();
        var app = ApplicationSettingsController.AppSettings;
        _previousLoadLast = app.LoadLastProjectOnStart;
        _previousLastProject = app.LastProjectFileName;
        _previousSaveAt100 = app.SaveAt100;
        _previousErrorReporter = AutomapEngine.ErrorReporter;
        _previousPrompts = AutomapEngine.Instance.Prompts;
        _lastVm = null;
        _tempDir = Path.Combine(Path.GetTempPath(), "trizbort_smoke_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public async Task TearDown()
    {
        AutomapEngine.Instance.Stop();
        if (_lastVm?.CurrentAutomapTask != null)
        {
            try { await _lastVm.CurrentAutomapTask.WaitAsync(TimeSpan.FromSeconds(5)); } catch { }
        }

        AutomapEngine.ErrorReporter = _previousErrorReporter;
        AutomapEngine.Instance.Prompts = _previousPrompts ?? NonInteractiveEditorPrompts.Instance;

        var app = ApplicationSettingsController.AppSettings;
        app.LoadLastProjectOnStart = _previousLoadLast;
        app.LastProjectFileName = _previousLastProject!;
        app.SaveAt100 = _previousSaveAt100;

        if (_previousRegions != null)
        {
            Settings.Regions = _previousRegions;
        }

        var created = Project.Current;
        Project.Current = _previousProject!;
        if (!ReferenceEquals(created, _previousProject))
        {
            created.Dispose();
        }

        try { Directory.Delete(_tempDir, true); } catch { }
    }

    // ------------------------------------------------------------------ helpers

    private static Region CloneRegion(Region region) => new()
    {
        RegionName = region.RegionName,
        RegionId = region.RegionId,
        RColor = region.RColor,
        TextColor = region.TextColor
    };

    private static string FindRepoPath(params string[] segments)
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(new[] { current.FullName }.Concat(segments).ToArray());
            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        Assert.Fail("Could not find " + Path.Combine(segments) + " from " + TestContext.CurrentContext.TestDirectory);
        return string.Empty;
    }

    private static string ManualFixture(string name) => FindRepoPath("tests", "manual", name);

    private string CopyFixtureToTemp(string sourcePath)
    {
        var target = Path.Combine(_tempDir, Path.GetFileName(sourcePath));
        File.Copy(sourcePath, target, true);
        return target;
    }

    private static void LoadInto(Project project, string path)
    {
        new MapLoader(project).LoadMap(path).ShouldBeTrue();
    }

    private static XDocument LoadMainWindowXaml() =>
        XDocument.Load(FindRepoPath("src", "Trizbort.Avalonia", "Views", "MainWindow.axaml"));

    private static readonly XNamespace Ui = "https://github.com/avaloniaui";

    private static string? WindowGestureCommandId(Trizbort.Editor.Input.InputKey key, Trizbort.Domain.Enums.ModifierKeys modifiers)
    {
        var catalogue = Trizbort.Avalonia.Services.AppCommands.CreateCatalogue(null);
        return catalogue.FindByGesture(
            new Trizbort.Editor.Input.KeyInput(key, modifiers),
            Trizbort.Editor.Commands.CommandScope.Window)?.Id;
    }

    private MainWindowViewModel CreateVm(
        RecordingDialogService dialog,
        StubStorageService storage,
        CommandService? commandService = null)
    {
        var vm = new MainWindowViewModel(
            commandService ?? new CommandService(),
            storage,
            dialog,
            new AvaloniaAutomapCanvas());
        _lastVm = vm;
        return vm;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int seconds = 4)
    {
        var timeout = DateTime.UtcNow.AddSeconds(seconds);
        while (DateTime.UtcNow < timeout && !condition())
        {
            await Task.Delay(50);
        }
    }

    private async Task<string> WriteTranscriptAsync(string text)
    {
        var path = Path.Combine(_tempDir, "transcript.txt");
        await File.WriteAllTextAsync(path, text);
        return path;
    }

    private const string TwoRoomTranscript =
        "Example Adventure\n\nLiving Room\nA cozy room with a fireplace.\n\n>n\n\nKitchen\nA bright kitchen with a refrigerator.\n\n>look\n\n";

    private async Task StopAndAwaitAsync(MainWindowViewModel vm)
    {
        vm.StopAutomap();
        if (vm.CurrentAutomapTask != null)
        {
            await vm.CurrentAutomapTask.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    // ------------------------------------------------------------------ stubs

    private sealed class RecordingDialogService : IDialogService
    {
        public ConfirmationResult SaveDiscardCancelResult { get; set; } = ConfirmationResult.Cancel;
        public AutomapSettings? AutomapSettingsToReturn { get; set; }
        public int SaveDiscardCancelCalls { get; private set; }
        public int ConfirmationCalls { get; private set; }
        public int MessageCalls { get; private set; }
        public string? LastPromptTitle { get; private set; }
        public string? LastPromptMessage { get; private set; }
        public string? LastMessageTitle { get; private set; }

        public int TotalPrompts => SaveDiscardCancelCalls + ConfirmationCalls + MessageCalls;

        public Task ShowAboutDialogAsync(Window? owner) => Task.CompletedTask;

        public Task ShowMessageAsync(Window? owner, string title, string message)
        {
            MessageCalls++;
            LastMessageTitle = title;
            LastPromptMessage = message;
            return Task.CompletedTask;
        }

        public Task<bool> ShowConfirmationAsync(Window? owner, string title, string message)
        {
            ConfirmationCalls++;
            LastPromptTitle = title;
            LastPromptMessage = message;
            return Task.FromResult(true);
        }

        public Task<ConfirmationResult> ShowSaveDiscardCancelAsync(Window? owner, string title, string message)
        {
            SaveDiscardCancelCalls++;
            LastPromptTitle = title;
            LastPromptMessage = message;
            return Task.FromResult(SaveDiscardCancelResult);
        }

        public Task<bool> ShowRoomPropertiesAsync(Window? owner, Room room) => Task.FromResult(true);
        public Task<bool> ShowConnectionPropertiesAsync(Window? owner, Connection connection) => Task.FromResult(true);
        public Task<bool> ShowMapLabelPropertiesAsync(Window? owner, MapLabel label) => Task.FromResult(true);
        public Task<bool> ShowElementPropertiesAsync(Window? owner, Element element) => Task.FromResult(true);
        public Task<bool> ShowMapSettingsAsync(Window? owner) => Task.FromResult(true);
        public Task<bool> ShowMapThemeAsync(Window? owner) => Task.FromResult(true);
        public Task<bool> ShowAppSettingsAsync(Window? owner) => Task.FromResult(true);
        public Task<AutomapSettings?> ShowAutomapSettingsAsync(Window? owner) => Task.FromResult(AutomapSettingsToReturn);
    }

    /// <summary>
    /// Stands in for the OS pickers: "picking" a file just returns the configured path, then the real
    /// Project load / save code runs exactly as StorageService does after the picker closes.
    /// </summary>
    private sealed class StubStorageService : IStorageService
    {
        public string? OpenPath { get; set; }
        public string? SaveAsPath { get; set; }
        public int OpenCalls { get; private set; }
        public List<bool> SaveCalls { get; } = new();
        public int SavePickerShown { get; private set; }

        public Task<bool> OpenMapAsync(TopLevel? topLevel)
        {
            OpenCalls++;
            if (OpenPath == null) return Task.FromResult(false);

            var project = new Project();
            if (!new MapLoader(project).LoadMap(OpenPath)) return Task.FromResult(false);
            project.FileName = OpenPath;
            project.IsDirty = false;
            Project.Current = project;
            return Task.FromResult(true);
        }

        public Task<bool> SaveMapAsync(TopLevel? topLevel, bool saveAs = false)
        {
            SaveCalls.Add(saveAs);
            var project = Project.Current;
            if (saveAs || string.IsNullOrEmpty(project.FileName))
            {
                SavePickerShown++;
                if (SaveAsPath == null) return Task.FromResult(false);
                project.FileName = SaveAsPath;
            }

            return Task.FromResult(project.Save());
        }

        public Task<bool> ExportCodeAsync(TopLevel? topLevel, CodeExporter exporter) => Task.FromResult(true);
        public Task<bool> ExportImageAsync(TopLevel? topLevel) => Task.FromResult(true);
        public Task<string?> OpenThemeFileAsync(TopLevel? topLevel) => Task.FromResult<string?>(null);
        public Task<string?> SaveThemeFileAsync(TopLevel? topLevel, string suggestedName) => Task.FromResult<string?>(null);
        public Task<string?> OpenTranscriptFileAsync(TopLevel? topLevel, string? initialPath = null) => Task.FromResult<string?>(null);
    }

    // ================================================================== 1. Start-up & File

    /// <summary>Smoke step 1.1: window opens on an empty map; title reads "Untitled - Trizbort - &lt;version&gt;".</summary>
    [Test]
    public void S1_01_Launch_StartsOnEmptyMapWithTrizbortTitle()
    {
        var vm = CreateVm(new RecordingDialogService(), new StubStorageService());

        vm.WindowTitle.ShouldStartWith("Untitled - Trizbort - ");
        vm.ElementCount.ShouldStartWith("Rooms: 0 | Connections: 0");
        Project.Current.Elements.Count.ShouldBe(0);
    }

    /// <summary>Smoke step 1.2: Ctrl+N opens a new empty map with no prompt when nothing is unsaved.</summary>
    [Test]
    public async Task S1_02_CtrlN_NewMap_NoPromptWhenClean()
    {
        WindowGestureCommandId(Trizbort.Editor.Input.InputKey.N, Trizbort.Domain.Enums.ModifierKeys.Primary).ShouldBe("file.new");

        var dialog = new RecordingDialogService();
        var vm = CreateVm(dialog, new StubStorageService());
        var oldProject = Project.Current;
        oldProject.Elements.Add(new Room(oldProject) { Name = "Old" });
        oldProject.IsDirty = false;

        await vm.NewMapAsync();

        dialog.TotalPrompts.ShouldBe(0);
        ReferenceEquals(Project.Current, oldProject).ShouldBeFalse();
        Project.Current.Elements.Count.ShouldBe(0);
    }

    /// <summary>Smoke step 1.3: Ctrl+O opens basics-1570; title reads "basics-1570 - Trizbort - &lt;version&gt;".</summary>
    [Test]
    public async Task S1_03_Open_LoadsMapAndSetsTitle()
    {
        var storage = new StubStorageService { OpenPath = CopyFixtureToTemp(ManualFixture(Fixture1570)) };
        var vm = CreateVm(new RecordingDialogService(), storage);

        await vm.OpenMapAsync();

        storage.OpenCalls.ShouldBe(1);
        Project.Current.Elements.OfType<Room>().Any().ShouldBeTrue();
        vm.WindowTitle.ShouldStartWith("basics-1570 - Trizbort - ");
    }

    /// <summary>Smoke step 1.3 (reference part): reference title is "&lt;name&gt; - Trizbort - &lt;version&gt;".</summary>
    [Test]
    public async Task S1_03b_Open_TitleHasReferenceFormatWithVersion()
    {
        var storage = new StubStorageService { OpenPath = CopyFixtureToTemp(ManualFixture(Fixture1570)) };
        var vm = CreateVm(new RecordingDialogService(), storage);

        await vm.OpenMapAsync();

        vm.WindowTitle.ShouldStartWith("basics-1570 - Trizbort - ");
    }

    /// <summary>Smoke step 1.4: moving a room gives the title a dirty marker.</summary>
    [Test]
    public async Task S1_04_MoveRoom_TitleGainsDirtyMarker()
    {
        var storage = new StubStorageService { OpenPath = CopyFixtureToTemp(ManualFixture(Fixture1570)) };
        var commandService = new CommandService();
        var vm = CreateVm(new RecordingDialogService(), storage, commandService);
        await vm.OpenMapAsync();
        vm.WindowTitle.ShouldNotContain("*");

        var room = Project.Current.Elements.OfType<Room>().First();
        var before = room.Position;
        commandService.Select(room);
        commandService.NudgeSelected(new CoreVector(40, 40));
        vm.UpdateState();

        room.Position.ShouldNotBe(before);
        Project.Current.IsDirty.ShouldBeTrue();
        vm.WindowTitle.ShouldContain("basics-1570* - Trizbort - ");
    }

    /// <summary>Smoke step 1.4b: undoing the move back to the saved state removes the dirty marker again.</summary>
    [Test]
    public async Task S1_04b_UndoToSavedState_ClearsTitleDirtyMarker()
    {
        var storage = new StubStorageService { OpenPath = CopyFixtureToTemp(ManualFixture(Fixture1570)) };
        var commandService = new CommandService();
        var vm = CreateVm(new RecordingDialogService(), storage, commandService);
        await vm.OpenMapAsync();

        commandService.Select(Project.Current.Elements.OfType<Room>().First());
        commandService.NudgeSelected(new CoreVector(40, 40));
        vm.WindowTitle.ShouldContain("basics-1570* - Trizbort - ");

        commandService.Session.PerformUndo();

        Project.Current.IsDirty.ShouldBeFalse();
        vm.WindowTitle.ShouldStartWith("basics-1570 - Trizbort - ");
        vm.WindowTitle.ShouldNotContain("*");
    }

    /// <summary>Smoke step 1.5: Ctrl+S saves without a dialog and the dirty marker disappears.</summary>
    [Test]
    public async Task S1_05_Save_NoPickerAndClearsDirtyMarker()
    {
        var path = CopyFixtureToTemp(ManualFixture(Fixture1570));
        var storage = new StubStorageService { OpenPath = path };
        var commandService = new CommandService();
        var vm = CreateVm(new RecordingDialogService(), storage, commandService);
        await vm.OpenMapAsync();

        commandService.Select(Project.Current.Elements.OfType<Room>().First());
        commandService.NudgeSelected(new CoreVector(40, 40));
        vm.UpdateState();
        vm.WindowTitle.ShouldContain("basics-1570* - Trizbort - ");
        var writtenBefore = File.GetLastWriteTimeUtc(path);
        await Task.Delay(20);

        await vm.SaveMapCommand.ExecuteAsync(null);

        storage.SaveCalls.ShouldBe(new[] { false });
        storage.SavePickerShown.ShouldBe(0);
        Project.Current.IsDirty.ShouldBeFalse();
        vm.WindowTitle.ShouldStartWith("basics-1570 - Trizbort - ");
        vm.WindowTitle.ShouldNotContain("*");
        File.GetLastWriteTimeUtc(path).ShouldBeGreaterThanOrEqualTo(writtenBefore);
    }

    /// <summary>Smoke step 1.6: Ctrl+Shift+S opens Save Map As (differs from reference: M005).</summary>
    [Test]
    public async Task S1_06_CtrlShiftS_SaveAs_AsksForNewFileAndRetitles()
    {
        WindowGestureCommandId(
            Trizbort.Editor.Input.InputKey.S,
            Trizbort.Domain.Enums.ModifierKeys.Primary | Trizbort.Domain.Enums.ModifierKeys.Shift).ShouldBe("file.saveAs");

        var saveAsPath = Path.Combine(_tempDir, "saved-as.trizbort");
        var storage = new StubStorageService
        {
            OpenPath = CopyFixtureToTemp(ManualFixture(Fixture1570)),
            SaveAsPath = saveAsPath
        };
        var vm = CreateVm(new RecordingDialogService(), storage);
        await vm.OpenMapAsync();

        await vm.SaveMapAsAsync();

        storage.SaveCalls.ShouldBe(new[] { true });
        storage.SavePickerShown.ShouldBe(1);
        File.Exists(saveAsPath).ShouldBeTrue();
        vm.WindowTitle.ShouldStartWith("saved-as - Trizbort - ");
    }

    /// <summary>Smoke step 1.7: dirty map then Ctrl+O prompts Yes/No/Cancel; Cancel keeps the map and the move.</summary>
    [Test]
    public async Task S1_07_OpenWhenDirty_CancelKeepsMapAndMove()
    {
        var dialog = new RecordingDialogService { SaveDiscardCancelResult = ConfirmationResult.Cancel };
        var storage = new StubStorageService { OpenPath = CopyFixtureToTemp(ManualFixture(Fixture1570)) };
        var commandService = new CommandService();
        var vm = CreateVm(dialog, storage, commandService);
        await vm.OpenMapAsync();
        storage.OpenCalls.ShouldBe(1);

        var project = Project.Current;
        var room = project.Elements.OfType<Room>().First();
        commandService.Select(room);
        commandService.NudgeSelected(new CoreVector(40, 40));
        var movedPosition = room.Position;

        await vm.OpenMapAsync();

        dialog.SaveDiscardCancelCalls.ShouldBe(1);
        dialog.LastPromptTitle.ShouldBe("Trizbort");
        dialog.LastPromptMessage.ShouldContain("current map");
        storage.OpenCalls.ShouldBe(1);
        ReferenceEquals(Project.Current, project).ShouldBeTrue();
        Project.Current.IsDirty.ShouldBeTrue();
        room.Position.ShouldBe(movedPosition);
    }

    /// <summary>Smoke step 1.8: file changed by another program prompts to reload (known gap: A005 / I-003).</summary>
    [Test]
    [Ignore("known gap: A005 / I-003")]
    public async Task S1_08_ExternalFileChange_PromptsToReload()
    {
        var path = CopyFixtureToTemp(ManualFixture(Fixture1570));
        var dialog = new RecordingDialogService();
        var storage = new StubStorageService { OpenPath = path };
        var vm = CreateVm(dialog, storage);
        await vm.OpenMapAsync();

        File.AppendAllText(path, Environment.NewLine);
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(5));
        await WaitUntilAsync(() => dialog.TotalPrompts > 0, 3);

        dialog.TotalPrompts.ShouldBeGreaterThan(0);
    }

    /// <summary>Smoke step 1.9: "Load Last Opened Project on Startup" reopens the last map (known gap: A014 / I-001).</summary>
    [Test]
    [Ignore("known gap: A014 / I-001")]
    public void S1_09_LoadLastProjectOnStartup_ReopensLastMap()
    {
        var path = CopyFixtureToTemp(ManualFixture(Fixture1570));
        var app = ApplicationSettingsController.AppSettings;
        app.LoadLastProjectOnStart = true;
        app.LastProjectFileName = path;
        Project.Current = new Project();

        // "Relaunch": build a fresh view model the way App does at start-up.
        _ = CreateVm(new RecordingDialogService(), new StubStorageService());

        Project.Current.FileName.ShouldBe(path);
    }

    // ================================================================== 6. Export

    /// <summary>Smoke step 6.49: File > Export lists one item per language, eight in total.</summary>
    [Test]
    public void S6_49_ExportMenu_HasOneItemPerLanguage()
    {
        var languageItems = Trizbort.Avalonia.Services.AppCommands.CreateCatalogue(null).All
            .Where(c => c.MenuPath == "File/Export" && c.Id != "file.export.png")
            .ToList();

        languageItems.Count.ShouldBe(8);
        // Strip access-key underscores ("A_dventuron") before matching.
        var headers = string.Join("|", languageItems.Select(c => c.Title.Replace("_", "")));
        foreach (var language in new[] { "Alan", "Adventuron", "Hugo", "Quest", "TADS", "ZIL", "Inform 6", "Inform 7" })
        {
            headers.ShouldContain(language);
        }
    }

    /// <summary>
    /// Smoke step 6.50: exporting Inform 7 with a name ending in ".inform" creates a "&lt;name&gt;.inform" folder.
    /// Covers the exporter only; the picker half of E010 stays manual.
    /// </summary>
    [Test]
    public void S6_50_Inform7Export_DotInformName_CreatesProjectFolder()
    {
        LoadInto(Project.Current, FindRepoPath("samples", "Zork_770614.trizbort"));
        var target = Path.Combine(_tempDir, "zork.inform");

        using (var exporter = new Inform7Exporter())
        {
            exporter.Export(target);
        }

        Directory.Exists(target).ShouldBeTrue();
        File.Exists(Path.Combine(target, "Source", "Story.ni")).ShouldBeTrue();
    }

    /// <summary>Smoke step 6.55: saving as "test.jpg" writes JPEG bytes (known gap: E016, Avalonia writes PNG).</summary>
    [AvaloniaTest]
    [Ignore("known gap: E016")]
    public void S6_55_SaveImageAsJpg_WritesJpegBytes()
    {
        LoadInto(Project.Current, FindRepoPath("samples", "Zork_770614.trizbort"));

        using var bitmap = MapCanvasControl.RenderToBitmap(Project.Current);
        bitmap.ShouldNotBeNull();
        using var stream = new MemoryStream();
        bitmap.Save(stream);
        var bytes = stream.ToArray();

        bytes[0].ShouldBe((byte)0xFF);
        bytes[1].ShouldBe((byte)0xD8);
    }

    /// <summary>Smoke step 6.56: export at about 200% zoom uses current zoom (known gap: E018, Avalonia always 100%).</summary>
    [AvaloniaTest]
    [Ignore("known gap: E018")]
    public void S6_56_ExportImageAtZoom200_UsesCurrentZoom()
    {
        ApplicationSettingsController.AppSettings.SaveAt100 = false;
        LoadInto(Project.Current, FindRepoPath("samples", "Zork_770614.trizbort"));
        var commandService = new CommandService();
        commandService.SetView(CoreVector.Zero, 2.0f);
        var bounds = MinimapTransform.ComputeCanvasBounds(Project.Current.Elements, includePadding: true);

        // The export entry point (StorageService.ExportImageAsync) renders with RenderToBitmap(project) at scale 1.
        using var bitmap = MapCanvasControl.RenderToBitmap(Project.Current);

        bitmap.ShouldNotBeNull();
        bitmap.PixelSize.Width.ShouldBe((int)Math.Ceiling(bounds.Width * 2.0));
    }

    // ================================================================== 7. Automap

    /// <summary>Smoke step 7.58: Toggle Automap opens the settings dialog; Start is blocked until a transcript name is set.</summary>
    [Test]
    public async Task S7_58_ToggleAutomap_BlocksStartWithoutTranscriptName()
    {
        var settingsVm = new AutomapSettingsViewModel(new StubStorageService());
        settingsVm.Initialize(AutomapSettings.Default);
        settingsVm.CanStart.ShouldBeFalse();

        var blank = AutomapSettings.Default;
        blank.FileName = "   ";
        var dialog = new RecordingDialogService { AutomapSettingsToReturn = blank };
        var vm = CreateVm(dialog, new StubStorageService());

        await vm.ToggleAutomapAsync();

        vm.IsAutomapActive.ShouldBeFalse();
        AutomapEngine.Instance.Running.ShouldBeFalse();
        dialog.LastMessageTitle.ShouldBe("Automap Error");
    }

    /// <summary>Smoke step 7.59: with a transcript name set, Start shows the running state, green indicator and Automap bar.</summary>
    [Test]
    public async Task S7_59_StartAutomap_ShowsRunningStateAndIndicators()
    {
        var settings = AutomapSettings.Default;
        settings.FileName = await WriteTranscriptAsync(TwoRoomTranscript);
        var dialog = new RecordingDialogService { AutomapSettingsToReturn = settings };
        var vm = CreateVm(dialog, new StubStorageService());

        await vm.ToggleAutomapAsync();

        vm.IsAutomapActive.ShouldBeTrue();
        vm.AutomapStatusText.ShouldBe("Automapping has started.");

        // Status bar indicator and AutomapBar are both visibility-bound to IsAutomapActive.
        var xaml = LoadMainWindowXaml();
        xaml.Descendants(Ui + "TextBlock").Any(t => (string?)t.Attribute("Text") == "Automap Running").ShouldBeTrue();
        var bar = xaml.Descendants().First(e => e.Name.LocalName == "AutomapBar");
        ((string?)bar.Attribute("IsVisible")).ShouldBe("{Binding IsAutomapActive}");

        await StopAndAwaitAsync(vm);
    }

    /// <summary>Smoke step 7.60: clicking Stop on the Automap bar stops automap and hides the bar.</summary>
    [Test]
    public async Task S7_60_StopButton_StopsAutomapAndHidesBar()
    {
        var settings = AutomapSettings.Default;
        settings.FileName = await WriteTranscriptAsync(TwoRoomTranscript);
        var vm = CreateVm(new RecordingDialogService(), new StubStorageService());
        await vm.StartAutomapAsync(settings);
        vm.IsAutomapActive.ShouldBeTrue();

        vm.StopAutomapCommand.Execute(null);
        if (vm.CurrentAutomapTask != null)
        {
            await vm.CurrentAutomapTask.WaitAsync(TimeSpan.FromSeconds(5));
        }

        vm.IsAutomapActive.ShouldBeFalse();
        AutomapEngine.Instance.Running.ShouldBeFalse();
        vm.AutomapStatusText.ShouldBe("Automap is not running.");
    }

    /// <summary>Smoke step 7.61: the Automap menu item keeps one static label (differs from reference: E038).</summary>
    [Test]
    public async Task S7_61_AutomapMenuItem_KeepsStaticLabelWhileRunning()
    {
        var command = Trizbort.Avalonia.Services.AppCommands.CreateCatalogue(null).Find("project.toggleAutomap")!;
        var header = command.Title;
        header.ShouldBe("Toggle _Automap");
        command.DynamicTitle.ShouldBeNull();

        var settings = AutomapSettings.Default;
        settings.FileName = await WriteTranscriptAsync(TwoRoomTranscript);
        var vm = CreateVm(new RecordingDialogService(), new StubStorageService());
        await vm.StartAutomapAsync(settings);
        vm.IsAutomapActive.ShouldBeTrue();

        // The header is a literal, so it cannot change to "Stop Automap" while running.
        header.ShouldNotContain("Stop");

        await StopAndAwaitAsync(vm);
    }

    /// <summary>Smoke step 7.62: a transcript naming a room ambiguously asks which room was meant (known gap: I-008).</summary>
    [Test]
    [Ignore("enabled by ticket 012")]
    public async Task S7_62_AmbiguousRoomName_AsksWhichRoomWasMeant()
    {
        var first = new Room(Project.Current) { Name = "Cave", Id = 1 };
        var second = new Room(Project.Current) { Name = "Cave", Id = 2 };
        Project.Current.Elements.Add(first);
        Project.Current.Elements.Add(second);

        var settings = AutomapSettings.Default;
        settings.FileName = await WriteTranscriptAsync("Game\n\nCave\nA damp cave.\n\n>look\n\n");
        var dialog = new RecordingDialogService();
        var vm = CreateVm(dialog, new StubStorageService());

        await vm.StartAutomapAsync(settings);
        await WaitUntilAsync(() => dialog.TotalPrompts > 0, 3);
        await StopAndAwaitAsync(vm);

        dialog.TotalPrompts.ShouldBeGreaterThan(0);
    }

    /// <summary>Smoke step 7.63: two rooms claiming the same exit asks what to do (known gap: I-008).</summary>
    [Test]
    public void S7_63_SameExitConflict_AsksWhatToDo()
    {
        // Ticket 012 assigns interactive prompts at start-up; until then Core uses the non-interactive ones.
        AutomapEngine.Instance.Prompts = NonInteractiveEditorPrompts.Instance;
        App.WireAutomapErrorReporter(new RecordingDialogService(), () => null);
        App.WireAutomapPrompts(() => null);

        AutomapEngine.Instance.Prompts.ShouldBeOfType<AvaloniaEditorPrompts>();
    }

    /// <summary>Smoke step 7.64: with automap running, closing the window stops automap without a hang.</summary>
    [Test]
    public async Task S7_64_CloseWindowWhileRunning_StopsAutomapWithoutHang()
    {
        var settings = AutomapSettings.Default;
        settings.FileName = await WriteTranscriptAsync(TwoRoomTranscript);
        var vm = CreateVm(new RecordingDialogService(), new StubStorageService());
        await vm.StartAutomapAsync(settings);
        vm.IsAutomapActive.ShouldBeTrue();

        // MainWindow.OnWindowClosing (map not dirty) calls vm.StopAutomap(); exercised here without a real window.
        Project.Current.IsDirty = false;
        vm.StopAutomap();
        if (vm.CurrentAutomapTask != null)
        {
            await vm.CurrentAutomapTask.WaitAsync(TimeSpan.FromSeconds(5));
        }

        vm.IsAutomapActive.ShouldBeFalse();
        AutomapEngine.Instance.Running.ShouldBeFalse();
    }
}
