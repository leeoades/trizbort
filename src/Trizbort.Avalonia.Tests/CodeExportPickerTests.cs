using Avalonia.Controls;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Services;
using Trizbort.Automap;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Export;
using Trizbort.Export.Languages;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class CodeExportPickerTests
{
    private Project? _previousProject;
    private readonly Dictionary<ExportLanguage, string> _saved = new();
    private string _folder = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
        _saved.Clear();
        foreach (var language in CodeExportService.AllLanguages)
        {
            _saved[language] = ExportPathMemory.GetPath(ApplicationSettingsController.AppSettings, language);
            ExportPathMemory.SetPath(ApplicationSettingsController.AppSettings, language, string.Empty);
        }
        _folder = Path.Combine(Path.GetTempPath(), "trizbort-export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TearDown]
    public void TearDown()
    {
        var created = Project.Current;
        Project.Current = _previousProject!;
        created.Dispose();
        foreach (var pair in _saved)
        {
            ExportPathMemory.SetPath(ApplicationSettingsController.AppSettings, pair.Key, pair.Value);
        }
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    [Test]
    public async Task DeclinedOverwrite_LeavesFileUntouched()
    {
        var path = Path.Combine(_folder, "map.t");
        File.WriteAllText(path, "original");
        var dialog = new FakeDialogService { ConfirmResult = false };
        var storage = new StorageService(dialog);

        using var exporter = new TadsExporter();
        var result = await storage.CompleteCodeExportAsync(null, exporter, ExportLanguage.Tads, path, ".t");

        result.ShouldBeFalse();
        dialog.ConfirmCount.ShouldBe(1);
        File.ReadAllText(path).ShouldBe("original");
    }

    [Test]
    public async Task AcceptedOverwrite_ReplacesFileAndRemembersPath()
    {
        var path = Path.Combine(_folder, "map.t");
        File.WriteAllText(path, "original");
        var storage = new StorageService(new FakeDialogService { ConfirmResult = true });

        using var exporter = new TadsExporter();
        var result = await storage.CompleteCodeExportAsync(null, exporter, ExportLanguage.Tads, path, ".t");

        result.ShouldBeTrue();
        File.ReadAllText(path).ShouldNotBe("original");
        ExportPathMemory.Get(ExportLanguage.Tads).FileName.ShouldBe("map");
    }

    [Test]
    public async Task MissingExtension_IsAppended()
    {
        var dialog = new FakeDialogService { ConfirmResult = true };
        var storage = new StorageService(dialog);

        using var exporter = new TadsExporter();
        var result = await storage.CompleteCodeExportAsync(
            null, exporter, ExportLanguage.Tads, Path.Combine(_folder, "noext"), ".t");

        result.ShouldBeTrue();
        File.Exists(Path.Combine(_folder, "noext.t")).ShouldBeTrue();
        dialog.ConfirmCount.ShouldBe(0);
    }

    [Test]
    public async Task EmptyInformPlaceholder_IsReplacedByBundleWithoutPrompt()
    {
        var path = Path.Combine(_folder, "game.inform");
        File.WriteAllBytes(path, []);
        var dialog = new FakeDialogService { ConfirmResult = false };
        var storage = new StorageService(dialog);

        using var exporter = new Inform7Exporter();
        var result = await storage.CompleteCodeExportAsync(null, exporter, ExportLanguage.Inform7, path, ".inform");

        result.ShouldBeTrue();
        dialog.ConfirmCount.ShouldBe(0);
        File.Exists(Path.Combine(path, "Source", "Story.ni")).ShouldBeTrue();
    }

    private sealed class FakeDialogService : IDialogService
    {
        public bool ConfirmResult { get; set; }
        public int ConfirmCount { get; private set; }

        public Task ShowAboutDialogAsync(Window? owner) => Task.CompletedTask;
        public Task ShowMessageAsync(Window? owner, string title, string message) => Task.CompletedTask;

        public Task<bool> ShowConfirmationAsync(Window? owner, string title, string message)
        {
            ConfirmCount++;
            return Task.FromResult(ConfirmResult);
        }

        public Task<ConfirmationResult> ShowSaveDiscardCancelAsync(Window? owner, string title, string message) => Task.FromResult(ConfirmationResult.Discard);
        public Task<bool> ShowRoomPropertiesAsync(Window? owner, Room room) => Task.FromResult(false);
        public Task<bool> ShowConnectionPropertiesAsync(Window? owner, Connection connection) => Task.FromResult(false);
        public Task<bool> ShowMapLabelPropertiesAsync(Window? owner, MapLabel label) => Task.FromResult(false);
        public Task<bool> ShowElementPropertiesAsync(Window? owner, Element element) => Task.FromResult(false);
        public Task<bool> ShowMapSettingsAsync(Window? owner) => Task.FromResult(false);
        public Task<bool> ShowMapThemeAsync(Window? owner) => Task.FromResult(false);
        public Task<bool> ShowAppSettingsAsync(Window? owner) => Task.FromResult(false);
        public Task<AutomapSettings?> ShowAutomapSettingsAsync(Window? owner) => Task.FromResult<AutomapSettings?>(null);
    }
}
