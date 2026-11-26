using Trizbort.Avalonia.Services;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.AppSettings;
using Trizbort.Updates;

namespace Trizbort.Avalonia.Tests;

/// <summary>The bar about new versions, and the setting for how Recast updates itself. Lee, 26 Nov 2026.</summary>
[TestFixture]
[NonParallelizable]
public class UpdateBarTests
{
    private sealed class FakeUpdater : IAppUpdater
    {
        public bool CanUpdate { get; set; } = true;
        public string? CurrentVersion => "0.15.0-beta";
        public AppUpdate? Downloaded { get; set; }
        public AppUpdate? Online { get; set; } = new("0.16.0-beta");
        public bool? Reopened { get; private set; }

        public AppUpdate? LastFullRelease { get; set; }

        public Task<AppUpdate?> CheckAsync(bool betas, bool goBack = false, CancellationToken cancel = default) =>
            Task.FromResult(goBack ? LastFullRelease : Online);

        public Task DownloadAsync(AppUpdate update, Action<int> progress, CancellationToken cancel = default)
        {
            Downloaded = update;
            return Task.CompletedTask;
        }

        public void InstallWhenClosed(AppUpdate update, bool reopen) => Reopened = reopen;
    }

    private UpdateMode _mode;
    private FakeUpdater _updater = null!;
    private readonly List<string> _opened = new();
    private int _closed;

    private UpdateBarViewModel Bar()
    {
        _updater = new FakeUpdater();
        var updates = new UpdateCoordinator(_updater, () => _mode);
        return new UpdateBarViewModel(updates, uri => { _opened.Add(uri); return Task.CompletedTask; }, () => _closed++);
    }

    [SetUp]
    public void SetUp()
    {
        _opened.Clear();
        _closed = 0;
        UpdateCoordinator.JustUpdatedTo = null;
    }

    [AvaloniaTest]
    public async Task Notify_OffersDownload_AndWhatsNew()
    {
        _mode = UpdateMode.Notify;
        var bar = Bar();
        bar.IsVisible.ShouldBeFalse();
        await bar.Updates.StartAsync();

        bar.IsVisible.ShouldBeTrue();
        bar.Message.ShouldBe("Recast 0.16.0-beta is out.");
        bar.ShowDownload.ShouldBeTrue();
        bar.ShowRestart.ShouldBeFalse();
        bar.DismissText.ShouldBe("Not Now");

        await bar.WhatsNewCommand.ExecuteAsync(null);
        _opened.ShouldBe(new[] { "https://github.com/leeoades/trizbort/releases/tag/recast-v0.16.0-beta" });

        await bar.DownloadCommand.ExecuteAsync(null);
        bar.ShowRestart.ShouldBeTrue();
        bar.RestartText.ShouldBe("Restart to Update");
    }

    [AvaloniaTest]
    public async Task DownloadAndAsk_RestartToUpdate_ClosesTheWindow_AndReopensAfterInstalling()
    {
        _mode = UpdateMode.DownloadAndAsk;
        var bar = Bar();
        await bar.Updates.StartAsync();

        bar.Message.ShouldBe("Recast 0.16.0-beta is ready to install.");
        bar.DismissText.ShouldBe("Later");
        bar.RestartCommand.Execute(null);
        _closed.ShouldBe(1);
        bar.Updates.OnClosing().ShouldBeTrue();
        _updater.Reopened.ShouldBe(true);
    }

    [AvaloniaTest]
    public async Task UpdateForMe_SaysItsInstalledOnClose()
    {
        _mode = UpdateMode.Automatic;
        var bar = Bar();
        await bar.Updates.StartAsync();

        bar.Message.ShouldContain("installed when you close Recast");
        bar.RestartText.ShouldBe("Restart Now");
        bar.DismissCommand.Execute(null);
        bar.IsVisible.ShouldBeFalse();
    }

    [AvaloniaTest]
    public async Task CheckingByHand_WhenNotInstalled_PointsToTheReleases()
    {
        var bar = Bar();
        _updater.CanUpdate = false;
        await bar.CheckNowAsync();

        bar.IsVisible.ShouldBeTrue();
        bar.Message.ShouldContain("can't update itself");
        bar.Message.ShouldContain(UpdateDownload.HowToInstall());
        bar.ShowReleases.ShouldBeTrue();
    }

    [AvaloniaTest]
    public async Task GoingBack_OffersRestartToGoBack()
    {
        _mode = UpdateMode.DownloadAndAsk;
        var bar = Bar();
        _updater.LastFullRelease = new AppUpdate("0.15.0", null, true);
        (await bar.Updates.GoBackAsync()).ShouldBeTrue();

        bar.Message.ShouldBe("Recast 0.15.0, the last full release, is ready to go back to.");
        bar.RestartText.ShouldBe("Restart to Go Back");
        bar.ShowWhatsNew.ShouldBeFalse();
    }

    [AvaloniaTest]
    public async Task CheckingByHand_NothingNew_SaysSo()
    {
        var bar = Bar();
        _updater.Online = null;
        await bar.CheckNowAsync();
        bar.Message.ShouldBe("You have the newest version of Recast (0.15.0-beta).");
    }

    [TestCase(UpdateDownload.Platform.Windows, "TrizbortRecast-win-x64-Setup.exe")]
    [TestCase(UpdateDownload.Platform.MacAppleSilicon, "TrizbortRecast-osx-arm64-Portable.zip")]
    [TestCase(UpdateDownload.Platform.MacIntel, "TrizbortRecast-osx-x64-Portable.zip")]
    [TestCase(UpdateDownload.Platform.Linux, ".AppImage")]
    public void APlainCopy_IsToldTheOneDownloadForItsComputer(UpdateDownload.Platform platform, string download)
    {
        var how = UpdateDownload.HowToInstall(platform);
        how.ShouldContain(download);
        foreach (var other in new[] { "Setup.exe", "Portable.zip", ".AppImage" }.Where(name => !download.Contains(name)))
            how.ShouldNotContain(other);
    }

    [AvaloniaTest]
    public void AppSettings_TestVersions_SavedOnlyOnceChanged()
    {
        var settings = ApplicationSettingsController.AppSettings;
        var (wasChoice, wasCurrent) = (settings.TestVersions, UpdateCoordinator.Current);
        try
        {
            // on a beta, not chosen: shown ticked, and left unchosen if not changed
            var updater = new FakeUpdater();
            UpdateCoordinator.Current = new UpdateCoordinator(updater, () => UpdateMode.DownloadAndAsk);
            settings.TestVersions = null;
            var vm = new AppSettingsViewModel();
            vm.Initialize();
            vm.TestVersions.ShouldBeTrue();
            vm.ApplyToSettings();
            settings.TestVersions.ShouldBeNull();
            vm.TurnedOffTestVersions.ShouldBeFalse();

            // turned off: saved, and a tester on a beta is offered going back
            vm.Initialize();
            vm.TestVersions = false;
            vm.ApplyToSettings();
            settings.TestVersions.ShouldBe(false);
            vm.TurnedOffTestVersions.ShouldBeTrue();
        }
        finally
        {
            settings.TestVersions = wasChoice;
            UpdateCoordinator.Current = wasCurrent;
        }
    }

    [AvaloniaTest]
    public void AppSettings_SaysWhenThisCopyCantUpdateItself()
    {
        var was = UpdateCoordinator.Current;
        try
        {
            var updater = new FakeUpdater { CanUpdate = false };
            UpdateCoordinator.Current = new UpdateCoordinator(updater, () => UpdateMode.DownloadAndAsk);
            var vm = new AppSettingsViewModel();
            vm.Initialize();
            vm.CantUpdateItself.ShouldBeTrue();
            vm.ReleasesPage.ToString().ShouldBe("https://github.com/leeoades/trizbort/releases");

            updater.CanUpdate = true;
            vm.Initialize();
            vm.CantUpdateItself.ShouldBeFalse();
        }
        finally
        {
            UpdateCoordinator.Current = was;
        }
    }

    [AvaloniaTest]
    public void AppSettings_TheThreeChoices_RoundTrip()
    {
        var settings = ApplicationSettingsController.AppSettings;
        var was = settings.Updates;
        try
        {
            foreach (var mode in Enum.GetValues<UpdateMode>())
            {
                settings.Updates = mode;
                var vm = new AppSettingsViewModel();
                vm.Initialize();
                vm.UpdateChoice.ShouldBe(mode);
                (vm.UpdatesNotify ? 1 : 0).Plus(vm.UpdatesDownload).Plus(vm.UpdatesAutomatic).ShouldBe(1);
            }

            var chosen = new AppSettingsViewModel();
            chosen.Initialize();
            chosen.UpdatesDownload = false;
            chosen.UpdatesAutomatic = true;
            chosen.ApplyToSettings();
            settings.Updates.ShouldBe(UpdateMode.Automatic);
        }
        finally
        {
            settings.Updates = was;
        }
    }
}

internal static class CountExtensions
{
    public static int Plus(this int count, bool another) => count + (another ? 1 : 0);
}
