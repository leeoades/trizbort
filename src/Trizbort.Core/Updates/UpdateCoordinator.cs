using System;
using System.Threading;
using System.Threading.Tasks;
using Trizbort.Domain.AppSettings;

namespace Trizbort.Updates;

/// <summary>Where updating has got to, for the bar at the top of the window.</summary>
public enum UpdateState
{
  /// <summary>Nothing to show.</summary>
  None,

  /// <summary>Looking for a new version (only shown when asked to check).</summary>
  Checking,

  /// <summary>A new version is online: Download, or not now (Notify).</summary>
  Available,

  /// <summary>Downloading it.</summary>
  Downloading,

  /// <summary>Downloaded: Restart to Update, or later (installed when Recast closes, with Install updates automatically).</summary>
  Ready,

  /// <summary>This is the newest version (only shown when asked to check).</summary>
  UpToDate,

  /// <summary>This copy wasn't installed, so it can't update itself (only shown when asked to check).</summary>
  NotInstalled,

  /// <summary>Checking or downloading didn't work.</summary>
  Failed,

  /// <summary>Recast has just been updated to this version.</summary>
  Updated
}

/// <summary>
///   Recast updating itself, as the user chose (Lee, 26 Nov 2026): <see cref="UpdateMode.Notify" /> says a new version
///   is out and downloads it when asked; <see cref="UpdateMode.DownloadAndAsk" /> downloads it and asks when to
///   restart; <see cref="UpdateMode.Automatic" /> downloads it and installs it when Recast closes. Nothing is
///   installed without the user's say-so or Recast closing. No Avalonia or Velopack here, so it can be tested.
/// </summary>
public sealed class UpdateCoordinator
{
  private readonly IAppUpdater _updater;
  private readonly Func<UpdateMode> _mode;
  private bool _restartWhenClosed;

  public UpdateCoordinator(IAppUpdater updater, Func<UpdateMode> mode)
  {
    _updater = updater ?? throw new ArgumentNullException(nameof(updater));
    _mode = mode ?? throw new ArgumentNullException(nameof(mode));
  }

  /// <summary>Set by the app's start-up hook when Recast was just updated: the version it's now on.</summary>
  public static string JustUpdatedTo { get; set; }

  /// <summary>The app's own, once it has started (App Settings asks it whether this copy can update itself).</summary>
  public static UpdateCoordinator Current { get; set; }

  public UpdateState State { get; private set; }

  /// <summary>The new version the bar is about.</summary>
  public AppUpdate Update { get; private set; }

  /// <summary>0 to 100 while downloading.</summary>
  public int Progress { get; private set; }

  /// <summary>Why it failed, in a few words.</summary>
  public string Error { get; private set; }

  /// <summary>True once the user has put the bar away (Not Now, Later); it comes back with something new.</summary>
  public bool Dismissed { get; private set; }

  public bool CanUpdate => _updater.CanUpdate;

  public string CurrentVersion => _updater.CurrentVersion;

  public UpdateMode Mode => _mode();

  /// <summary>Raised whenever any of the above changes (on whatever thread the change happened).</summary>
  public event EventHandler Changed;

  /// <summary>
  ///   At start-up: says so if Recast was just updated, picks up an update downloaded before, then looks for a newer
  ///   one and goes on as the mode says. Quiet when there's nothing new or the check fails.
  /// </summary>
  public async Task StartAsync(CancellationToken cancel = default)
  {
    if (!string.IsNullOrEmpty(JustUpdatedTo))
    {
      Update = new AppUpdate(JustUpdatedTo);
      Set(UpdateState.Updated);
    }

    if (!_updater.CanUpdate) return;
    if (_updater.Downloaded is { } waiting)
    {
      Update = waiting;
      Set(UpdateState.Ready);
    }

    await CheckAsync(false, cancel);
  }

  /// <summary>
  ///   Looks for a new version. <paramref name="asked" /> (Help > Check for Updates): says what it found even when
  ///   there's nothing new, and offers to download whatever the mode.
  /// </summary>
  public async Task CheckAsync(bool asked, CancellationToken cancel = default)
  {
    if (!_updater.CanUpdate)
    {
      if (asked) Set(UpdateState.NotInstalled);
      return;
    }

    if (State is UpdateState.Downloading) return;
    var waiting = State == UpdateState.Ready ? Update : null;
    if (asked) Set(UpdateState.Checking);

    AppUpdate found;
    try
    {
      found = await _updater.CheckAsync(cancel);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      if (asked) Fail("Couldn't check for updates: " + ex.Message);
      else if (waiting == null && State == UpdateState.Checking) Set(UpdateState.None);
      return;
    }

    if (found == null || (waiting != null && SameVersion(found, waiting)))
    {
      if (waiting != null)
      {
        Update = waiting;
        Set(UpdateState.Ready);
      }
      else if (asked) Set(UpdateState.UpToDate);
      else if (State != UpdateState.Updated) Set(UpdateState.None);
      return;
    }

    Update = found;
    Dismissed = false;
    if (Mode == UpdateMode.Notify)
    {
      Set(UpdateState.Available);
      return;
    }

    await DownloadAsync(cancel);
  }

  /// <summary>Downloads the update the bar is about (Download, or by itself unless the mode is Notify).</summary>
  public async Task DownloadAsync(CancellationToken cancel = default)
  {
    if (Update == null || State == UpdateState.Downloading) return;
    var update = Update;
    Progress = 0;
    Set(UpdateState.Downloading);
    try
    {
      await _updater.DownloadAsync(update, percent =>
      {
        Progress = Math.Clamp(percent, 0, 100);
        Changed?.Invoke(this, EventArgs.Empty);
      }, cancel);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      Fail("Couldn't download Recast " + update.Version + ": " + ex.Message);
      return;
    }

    Set(UpdateState.Ready);
  }

  /// <summary>
  ///   Restart to Update: installs the update when Recast closes, and opens it again. The caller then closes the
  ///   window, which asks about unsaved changes as usual; if the user stays, it's installed when they do close.
  /// </summary>
  public void RestartToUpdate()
  {
    if (State != UpdateState.Ready) return;
    _restartWhenClosed = true;
  }

  /// <summary>Not Now / Later: puts the bar away. The update stays downloaded, for next time.</summary>
  public void Dismiss()
  {
    Dismissed = true;
    Changed?.Invoke(this, EventArgs.Empty);
  }

  /// <summary>True when closing Recast will install an update.</summary>
  public bool InstallsOnClose =>
    State == UpdateState.Ready && Update != null && (_restartWhenClosed || Mode == UpdateMode.Automatic);

  /// <summary>
  ///   As Recast closes: hands a downloaded update to the installer when asked to restart, or when updating for the
  ///   user; reopens Recast only after Restart to Update. Returns whether it did.
  /// </summary>
  public bool OnClosing()
  {
    if (!InstallsOnClose) return false;
    try
    {
      _updater.InstallWhenClosed(Update, _restartWhenClosed);
      return true;
    }
    catch (Exception)
    {
      // never stop Recast closing; the update is still downloaded for next time
      return false;
    }
  }

  private static bool SameVersion(AppUpdate a, AppUpdate b) =>
    string.Equals(a.Version, b.Version, StringComparison.OrdinalIgnoreCase);

  private void Fail(string error)
  {
    Error = error;
    Set(UpdateState.Failed);
  }

  private void Set(UpdateState state)
  {
    State = state;
    if (state != UpdateState.Failed) Error = null;
    Dismissed = false;
    Changed?.Invoke(this, EventArgs.Empty);
  }
}
