using Trizbort.Updates;
using Velopack;
using Velopack.Sources;

namespace Trizbort.Avalonia.Services;

/// <summary>
///   Updates Recast from its GitHub releases with Velopack (Lee, 26 Nov 2026). Each release carries Velopack's
///   packages beside the zips (release.yml); an installed copy finds the newest for its own platform (its channel),
///   pre-releases included while Recast is in beta.
/// </summary>
public sealed class VelopackUpdater : IAppUpdater
{
    public const string Repository = "https://github.com/leeoades/trizbort";

    private readonly UpdateManager? _manager;

    public VelopackUpdater()
    {
        try
        {
            _manager = new UpdateManager(new GithubSource(Repository, null, true, null));
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

    public async Task<AppUpdate?> CheckAsync(CancellationToken cancel = default)
    {
        if (!CanUpdate) return null;
        var info = await _manager!.CheckForUpdatesAsync().ConfigureAwait(false);
        cancel.ThrowIfCancellationRequested();
        return info == null ? null : new AppUpdate(info.TargetFullRelease.Version.ToString(), info);
    }

    public Task DownloadAsync(AppUpdate update, Action<int> progress, CancellationToken cancel = default)
    {
        if (!CanUpdate || update.Handle is not UpdateInfo info) return Task.CompletedTask;
        return _manager!.DownloadUpdatesAsync(info, progress, cancel);
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
