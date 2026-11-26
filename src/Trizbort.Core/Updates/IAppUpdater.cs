using System;
using System.Threading;
using System.Threading.Tasks;

namespace Trizbort.Updates;

/// <summary>A newer version of Recast, found online or already downloaded.</summary>
/// <param name="Version">"0.16.0-beta"</param>
/// <param name="Handle">What the updater needs to download or install it.</param>
/// <param name="GoesBack">An older version: going back from a test version to the last full release.</param>
public sealed record AppUpdate(string Version, object Handle = null, bool GoesBack = false);

/// <summary>
///   Finds, downloads and installs new versions of Recast (Velopack in the app, a fake in tests). Only an installed
///   copy can update itself; one run from an unzipped folder, or from a build, can't.
/// </summary>
public interface IAppUpdater
{
  /// <summary>True when this copy of Recast was installed, so it can update itself.</summary>
  bool CanUpdate { get; }

  /// <summary>The version running, as installed ("0.15.0-beta"), or null when it isn't installed.</summary>
  string CurrentVersion { get; }

  /// <summary>An update downloaded already (last time, say), waiting to be installed; or null.</summary>
  AppUpdate Downloaded { get; }

  /// <summary>
  ///   The newest version online, or null when this is the newest. <paramref name="betas" />: test versions too, or
  ///   full releases only. <paramref name="goBack" />: the newest even when it's older than this one (going back from
  ///   a test version to the last full release).
  /// </summary>
  Task<AppUpdate> CheckAsync(bool betas, bool goBack = false, CancellationToken cancel = default);

  /// <summary>Downloads it, reporting 0 to 100 as it goes.</summary>
  Task DownloadAsync(AppUpdate update, Action<int> progress, CancellationToken cancel = default);

  /// <summary>
  ///   Installs it once Recast has closed (call as Recast closes), opening Recast again afterwards if
  ///   <paramref name="reopen" />.
  /// </summary>
  void InstallWhenClosed(AppUpdate update, bool reopen);
}
