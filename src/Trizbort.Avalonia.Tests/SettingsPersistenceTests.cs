using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.AppSettings;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class SettingsPersistenceTests
{
    private string _previousLast = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _previousLast = ApplicationSettingsController.AppSettings.LastProjectFileName;
    }

    [TearDown]
    public void TearDown()
    {
        ApplicationSettingsController.AppSettings.LastProjectFileName = _previousLast;
    }

    [Test]
    public void RecordProjectOpened_PersistsLastProjectAndRecentList()
    {
        var path = Path.Combine(Path.GetTempPath(), "persist-test-" + Guid.NewGuid().ToString("N") + ".trizbort");

        StorageService.RecordProjectOpened(path);

        ApplicationSettingsController.AppSettings.LastProjectFileName.ShouldBe(path);
        ApplicationSettingsController.AppSettings.RecentProjects[0].ShouldBe(path);
        File.ReadAllText(AppPaths.SettingsFilePath).ShouldContain("LastProjectFileName");
        File.ReadAllText(AppPaths.SettingsFilePath).ShouldContain(Path.GetFileName(path));
    }

    [Test]
    public void SaveSettings_WritesShowMiniMapToDisk()
    {
        var settings = ApplicationSettingsController.AppSettings;
        var original = settings.ShowMiniMap;
        try
        {
            settings.ShowMiniMap = !original;
            ApplicationSettingsController.SaveSettings();
            File.ReadAllText(AppPaths.SettingsFilePath).ShouldContain($"\"ShowMiniMap\": {(!original).ToString().ToLowerInvariant()}");
        }
        finally
        {
            settings.ShowMiniMap = original;
            ApplicationSettingsController.SaveSettings();
        }
    }
}
