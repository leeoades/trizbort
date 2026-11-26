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

        public Task<AppUpdate?> CheckAsync(CancellationToken cancel = default) => Task.FromResult(Online);

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
        bar.ShowReleases.ShouldBeTrue();
    }

    [AvaloniaTest]
    public async Task CheckingByHand_NothingNew_SaysSo()
    {
        var bar = Bar();
        _updater.Online = null;
        await bar.CheckNowAsync();
        bar.Message.ShouldBe("You have the newest version of Recast (0.15.0-beta).");
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
