using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.AppSettings;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class RecentFilesServiceTests
{
    private string _folder = string.Empty;
    private string? _previousOverride;
    private string _previousSettings = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "trizbort-recent-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _previousOverride = AppPaths.OverrideConfigFolder;
        _previousSettings = JsonConvert.SerializeObject(ApplicationSettingsController.AppSettings);
        AppPaths.OverrideConfigFolder = Path.Combine(_folder, "config");
    }

    [TearDown]
    public void TearDown()
    {
        AppPaths.OverrideConfigFolder = _previousOverride;
        JsonConvert.PopulateObject(
            _previousSettings,
            ApplicationSettingsController.AppSettings,
            new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
        try
        {
            Directory.Delete(_folder, true);
        }
        catch (IOException)
        {
            // best effort
        }
    }

    [Test]
    public async Task MissingFile_IsRemovedAndMessageShown()
    {
        var missing = Path.Combine(_folder, "gone.trizbort");
        ApplicationSettingsController.AppSettings.RecentProjects.Clear();
        ApplicationSettingsController.AppSettings.RecentProjects.Add(missing);
        var storage = DispatchProxy.Create<IStorageService, StartupTests.RecordingProxy>();
        var dialogs = DispatchProxy.Create<IDialogService, StartupTests.RecordingProxy>();
        var service = new RecentFilesService(storage, dialogs);

        var opened = await service.OpenRecentAsync(missing, null);

        opened.ShouldBeFalse();
        ApplicationSettingsController.AppSettings.RecentProjects.ShouldNotContain(missing);
        ((StartupTests.RecordingProxy)(object)dialogs).Calls.ShouldContain(nameof(IDialogService.ShowMessageAsync));
        ((StartupTests.RecordingProxy)(object)storage).Calls.ShouldBeEmpty();
    }

    [Test]
    public async Task ExistingFile_OpensThroughStorage()
    {
        var file = Path.Combine(_folder, "a.trizbort");
        File.WriteAllText(file, "x");
        var storage = DispatchProxy.Create<IStorageService, StartupTests.RecordingProxy>();
        var dialogs = DispatchProxy.Create<IDialogService, StartupTests.RecordingProxy>();
        var service = new RecentFilesService(storage, dialogs);

        var opened = await service.OpenRecentAsync(file, null, () => Task.FromResult(true));

        opened.ShouldBeTrue();
        ((StartupTests.RecordingProxy)(object)storage).Calls.ShouldContain(nameof(IStorageService.OpenMapFromPathAsync));
    }

    [Test]
    public async Task DeclinedUnsavedPrompt_DoesNotOpen()
    {
        var file = Path.Combine(_folder, "a.trizbort");
        File.WriteAllText(file, "x");
        var storage = DispatchProxy.Create<IStorageService, StartupTests.RecordingProxy>();
        var dialogs = DispatchProxy.Create<IDialogService, StartupTests.RecordingProxy>();
        var service = new RecentFilesService(storage, dialogs);

        var opened = await service.OpenRecentAsync(file, null, () => Task.FromResult(false));

        opened.ShouldBeFalse();
        ((StartupTests.RecordingProxy)(object)storage).Calls.ShouldBeEmpty();
    }
}
