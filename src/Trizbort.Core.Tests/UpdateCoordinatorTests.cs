using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.AppSettings;
using Trizbort.Updates;

namespace Trizbort.Core.Tests;

/// <summary>
///   Recast updating itself (Lee, 26 Nov 2026): "Notify me and let me choose when to download", "Download but let me
///   control when to update", "Update for me". Nothing is installed without the user saying so or Recast closing.
/// </summary>
[TestFixture]
public class UpdateCoordinatorTests
{
  private sealed class FakeUpdater : IAppUpdater
  {
    public bool CanUpdate { get; set; } = true;
    public string CurrentVersion { get; set; } = "0.15.0-beta";
    public AppUpdate Downloaded { get; set; }
    public AppUpdate Online { get; set; }
    public Exception CheckFails { get; set; }
    public Exception DownloadFails { get; set; }
    public List<AppUpdate> DownloadedUpdates { get; } = new();
    public List<(AppUpdate Update, bool Reopen)> Installed { get; } = new();

    /// <summary>The last full release, offered when going back.</summary>
    public AppUpdate LastFullRelease { get; set; }
    public List<(bool Betas, bool GoBack)> Checks { get; } = new();

    public Task<AppUpdate> CheckAsync(bool betas, bool goBack = false, CancellationToken cancel = default)
    {
      Checks.Add((betas, goBack));
      if (CheckFails != null) return Task.FromException<AppUpdate>(CheckFails);
      return Task.FromResult(goBack ? LastFullRelease : Online);
    }

    public Task DownloadAsync(AppUpdate update, Action<int> progress, CancellationToken cancel = default)
    {
      if (DownloadFails != null) return Task.FromException(DownloadFails);
      progress(50);
      progress(100);
      DownloadedUpdates.Add(update);
      Downloaded = update;
      return Task.CompletedTask;
    }

    public void InstallWhenClosed(AppUpdate update, bool reopen) => Installed.Add((update, reopen));
  }

  private FakeUpdater _updater;
  private UpdateMode _mode;
  private bool? _testVersions;
  private UpdateCoordinator _updates;

  [SetUp]
  public void SetUp()
  {
    UpdateCoordinator.JustUpdatedTo = null;
    _updater = new FakeUpdater { Online = new AppUpdate("0.16.0-beta") };
    _mode = UpdateMode.DownloadAndAsk;
    _testVersions = null;
    _updates = new UpdateCoordinator(_updater, () => _mode, () => _testVersions);
  }

  [TearDown]
  public void TearDown() => UpdateCoordinator.JustUpdatedTo = null;

  [Test]
  public async Task Notify_SaysANewVersionIsOut_AndDownloadsOnlyWhenAsked()
  {
    _mode = UpdateMode.Notify;
    await _updates.StartAsync();

    _updates.State.ShouldBe(UpdateState.Available);
    _updates.Update!.Version.ShouldBe("0.16.0-beta");
    _updater.DownloadedUpdates.ShouldBeEmpty();

    await _updates.DownloadAsync();
    _updates.State.ShouldBe(UpdateState.Ready);
    _updater.DownloadedUpdates.ShouldHaveSingleItem();
    _updates.OnClosing().ShouldBeFalse(); // ready, but only installed when asked
  }

  [Test]
  public async Task DownloadAndAsk_Downloads_ThenWaitsToBeAsked_InstallingOnlyAfterRestartToUpdate()
  {
    var progress = new List<int>();
    _updates.Changed += (_, _) => { if (_updates.State == UpdateState.Downloading) progress.Add(_updates.Progress); };
    await _updates.StartAsync();

    progress.ShouldContain(50);
    _updates.State.ShouldBe(UpdateState.Ready);
    _updates.InstallsOnClose.ShouldBeFalse();
    _updates.OnClosing().ShouldBeFalse();
    _updater.Installed.ShouldBeEmpty();

    _updates.RestartToUpdate();
    _updates.OnClosing().ShouldBeTrue();
    _updater.Installed.ShouldBe(new[] { (_updates.Update!, true) });
  }

  [Test]
  public async Task UpdateForMe_Downloads_AndInstallsWhenRecastCloses_WithoutReopening()
  {
    _mode = UpdateMode.Automatic;
    await _updates.StartAsync();

    _updates.State.ShouldBe(UpdateState.Ready);
    _updates.InstallsOnClose.ShouldBeTrue();
    _updates.OnClosing().ShouldBeTrue();
    _updater.Installed.ShouldHaveSingleItem().Reopen.ShouldBeFalse();
  }

  [Test]
  public async Task UpdateForMe_RestartNow_Reopens()
  {
    _mode = UpdateMode.Automatic;
    await _updates.StartAsync();
    _updates.RestartToUpdate();
    _updates.OnClosing();
    _updater.Installed.ShouldHaveSingleItem().Reopen.ShouldBeTrue();
  }

  [Test]
  public async Task NothingNew_SaysNothing_UnlessAsked()
  {
    _updater.Online = null;
    await _updates.StartAsync();
    _updates.State.ShouldBe(UpdateState.None);

    await _updates.CheckAsync(true);
    _updates.State.ShouldBe(UpdateState.UpToDate);
  }

  [Test]
  public async Task NotInstalled_NeverChecks_AndSaysWhyWhenAsked()
  {
    _updater.CanUpdate = false;
    await _updates.StartAsync();
    _updates.State.ShouldBe(UpdateState.None);

    await _updates.CheckAsync(true);
    _updates.State.ShouldBe(UpdateState.NotInstalled);
  }

  [Test]
  public async Task AFailedCheck_IsQuietAtStartUp_ButSaidWhenAsked()
  {
    _updater.CheckFails = new InvalidOperationException("no internet");
    await _updates.StartAsync();
    _updates.State.ShouldBe(UpdateState.None);

    await _updates.CheckAsync(true);
    _updates.State.ShouldBe(UpdateState.Failed);
    _updates.Error.ShouldContain("no internet");
  }

  [Test]
  public async Task AFailedDownload_SaysSo_AndInstallsNothing()
  {
    _updater.DownloadFails = new InvalidOperationException("disk full");
    await _updates.StartAsync();
    _updates.State.ShouldBe(UpdateState.Failed);
    _updates.Error.ShouldContain("0.16.0-beta");
    _updates.OnClosing().ShouldBeFalse();
  }

  [Test]
  public async Task AnUpdateDownloadedLastTime_IsReadyStraightAway()
  {
    _updater.Downloaded = new AppUpdate("0.16.0-beta");
    _updater.Online = new AppUpdate("0.16.0-beta");
    await _updates.StartAsync();

    _updates.State.ShouldBe(UpdateState.Ready);
    _updater.DownloadedUpdates.ShouldBeEmpty(); // not downloaded again
  }

  [Test]
  public async Task ANewerOneThanTheOneDownloaded_IsDownloaded()
  {
    _updater.Downloaded = new AppUpdate("0.16.0-beta");
    _updater.Online = new AppUpdate("0.17.0-beta");
    await _updates.StartAsync();

    _updates.State.ShouldBe(UpdateState.Ready);
    _updates.Update!.Version.ShouldBe("0.17.0-beta");
  }

  [Test]
  public async Task JustUpdated_SaysSo()
  {
    UpdateCoordinator.JustUpdatedTo = "0.16.0-beta";
    _updater.Online = null;
    await _updates.StartAsync();

    _updates.State.ShouldBe(UpdateState.Updated);
    _updates.Update!.Version.ShouldBe("0.16.0-beta");
  }

  [Test]
  public async Task Later_PutsTheBarAway_TheUpdateStaysDownloaded()
  {
    await _updates.StartAsync();
    _updates.Dismiss();
    _updates.Dismissed.ShouldBeTrue();
    _updates.State.ShouldBe(UpdateState.Ready);
    _updater.Downloaded.ShouldNotBeNull();
  }

  [Test]
  public void TheDefault_IsDownloadButLetMeChoose()
  {
    new ApplicationSettings().Updates.ShouldBe(UpdateMode.DownloadAndAsk);
  }

  // Get test versions (Lee, 26 Nov 2026)

  [TestCase("0.16.0-beta.2", true)]
  [TestCase("1.0.0", false)]
  public async Task TestVersions_NotChosen_GoByTheVersionRunning(string running, bool betas)
  {
    // a tester on a beta gets the full release when it's out, and then only full releases
    _updater.CurrentVersion = running;
    await _updates.StartAsync();
    _updater.Checks.ShouldBe(new[] { (betas, false) });
  }

  [TestCase(true, "1.0.0", true)]
  [TestCase(false, "0.16.0-beta.2", false)]
  public async Task TestVersions_Chosen_AreWhatsAskedFor(bool chosen, string running, bool betas)
  {
    _testVersions = chosen;
    _updater.CurrentVersion = running;
    await _updates.StartAsync();
    _updater.Checks.ShouldBe(new[] { (betas, false) });
  }

  [Test]
  public void TestVersion_IsOneWithAPreReleasePart()
  {
    UpdateCoordinator.IsTestVersion("0.16.0-beta").ShouldBeTrue();
    UpdateCoordinator.IsTestVersion("1.0.0").ShouldBeFalse();
    UpdateCoordinator.IsTestVersion(null).ShouldBeFalse();
  }

  [Test]
  public async Task GoingBack_FromATestVersion_DownloadsTheLastFullRelease_ForRestart()
  {
    _testVersions = false;
    _updater.CurrentVersion = "1.1.0-beta.2";
    _updater.LastFullRelease = new AppUpdate("1.0.0", null, true);

    (await _updates.GoBackAsync()).ShouldBeTrue();
    _updater.Checks.ShouldBe(new[] { (false, true) });
    _updates.State.ShouldBe(UpdateState.Ready);
    _updates.Update!.GoesBack.ShouldBeTrue();

    _updates.RestartToUpdate();
    _updates.OnClosing().ShouldBeTrue();
    _updater.Installed.ShouldBe(new[] { (_updater.LastFullRelease, true) });
  }

  [Test]
  public async Task GoingBack_WithNoFullReleaseToGoBackTo_SaysSo()
  {
    _updater.CurrentVersion = "0.16.0-beta";
    _updater.LastFullRelease = null;
    (await _updates.GoBackAsync()).ShouldBeFalse();
    _updates.State.ShouldBe(UpdateState.None);
    _updater.DownloadedUpdates.ShouldBeEmpty();
  }
}
