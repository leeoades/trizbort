using Trizbort.Updates;
using Velopack;
using Velopack.Sources;

namespace Trizbort.Avalonia.Services;

/// <summary>
///   Updates Recast from its GitHub releases with Velopack (Lee, 26 Nov 2026). Each release carries Velopack's
///   packages beside the zips (release.yml); an installed copy finds the newest for its own platform (its channel):
///   betas too, or full releases only, as Get test versions says; or, going back, the last full release.
/// </summary>
public sealed class VelopackUpdater : IAppUpdater
{
    public const string Repository = "https://github.com/leeoades/trizbort";

    /// <summary>For testing: a folder of vpk's output to update from instead of GitHub.</summary>
    public const string TestFolderVariable = "TRIZBORT_UPDATES_FROM";

    private readonly UpdateManager? _manager;

    // the manager that found the update last checked for: it downloads it too
    private UpdateManager? _checked;

    public VelopackUpdater()
    {
        try
        {
            _manager = new UpdateManager(Source(true));
        }
        catch (Exception)
        {
            // a build, or a copy run from a folder: it can't update itself
            _manager = null;
        }
    }

    public bool CanUpdate => _manager?.IsInstalled == true;

    public string? CurrentVersion => CanUpdate ? _manager!.CurrentVersion?.ToString() : null;

    public AppUpdate? Downloaded =>
        CanUpdate && _manager!.UpdatePendingRestart is { } asset ? new AppUpdate(asset.Version.ToString(), asset) : null;

    /// <summary>GitHub's releases (betas too, or not), or a folder of vpk's output when testing.</summary>
    private static IUpdateSource Source(bool betas)
    {
        var folder = Environment.GetEnvironmentVariable(TestFolderVariable);
        return string.IsNullOrWhiteSpace(folder)
            ? new GithubSource(Repository, null, betas, null)
            : new SimpleFileSource(new DirectoryInfo(folder));
    }

    public async Task<AppUpdate?> CheckAsync(bool betas, bool goBack = false, CancellationToken cancel = default)
    {
        if (!CanUpdate) return null;
        var manager = new UpdateManager(Source(betas), new UpdateOptions { AllowVersionDowngrade = goBack });
        var info = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
        cancel.ThrowIfCancellationRequested();
        if (info == null) return null;
        _checked = manager;
        return new AppUpdate(info.TargetFullRelease.Version.ToString(), info, info.IsDowngrade);
    }

    public Task DownloadAsync(AppUpdate update, Action<int> progress, CancellationToken cancel = default)
    {
        if (!CanUpdate || update.Handle is not UpdateInfo info) return Task.CompletedTask;
        return (_checked ?? _manager!).DownloadUpdatesAsync(info, progress, cancel);
    }

    public void InstallWhenClosed(AppUpdate update, bool reopen)
    {
        if (!CanUpdate) return;
        var asset = update.Handle switch
        {
            UpdateInfo info => info.TargetFullRelease,
            VelopackAsset downloaded => downloaded,
            _ => null
        };
        // Velopack waits for Recast to close, installs it quietly, and opens it again after Restart to Update
        _manager!.WaitExitThenApplyUpdates(asset, true, reopen, Array.Empty<string>());
    }
}
