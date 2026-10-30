using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Trizbort.Domain.Application;

namespace Trizbort.Domain.Watchers;

public class TrizbortFileWatcher : IDisposable
{
  private readonly FileSystemWatcher _watcher = new();
  private DateTime _lastWriteUtc;
  private int _handling;

  public TrizbortFileWatcher()
  {
    // FileName is needed so that editors which save by writing a temp file and renaming it over the
    // target (Created/Renamed events, notably on Linux) are noticed as well.
    _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
    _watcher.Changed += Changed;
    _watcher.Created += Changed;
    _watcher.Renamed += Renamed;
  }

  public string WatchedPath => _watcher.Path;
  public string WatchedFilter => _watcher.Filter;
  public bool IsWatching => _watcher.EnableRaisingEvents;

  public static Action<Action> UIThreadInvoker { get; set; }

  /// <summary>
  ///   Delay after the first file event before the file is examined; editors typically raise
  ///   several events per save. Tests may lower this.
  /// </summary>
  public static int DebounceMilliseconds { get; set; } = 300;

  /// <summary>Asks whether to reload; the argument is whether the open project has unsaved changes.</summary>
  public static Func<bool, Task<bool>> ConfirmReloadPromptAsync { get; set; }

  /// <summary>Synchronous adapter over <see cref="ConfirmReloadPromptAsync" />, kept for legacy callers.</summary>
  public static Func<bool, bool> ConfirmReloadPrompt
  {
    get => _legacyPromptStatic;
    set
    {
      _legacyPromptStatic = value;
      ConfirmReloadPromptAsync = value == null ? null : dirty => Task.FromResult(value(dirty));
    }
  }

  private static Func<bool, bool> _legacyPromptStatic;

  public void Dispose()
  {
    _watcher?.Dispose();
  }

  public void InitializeWatcher(string fileToWatch)
  {
    StopWatcher();
    if (string.IsNullOrEmpty(fileToWatch)) return;
    _watcher.Path = Path.GetDirectoryName(fileToWatch);
    _watcher.Filter = Path.GetFileName(fileToWatch);
    StartWatcher();
  }

  public event EventHandler ReloadMap;

  public void StartWatcher()
  {
    if (string.IsNullOrEmpty(_watcher.Path)) return;
    _lastWriteUtc = GetWriteTime();
    _watcher.EnableRaisingEvents = true;
  }

  public void StopWatcher()
  {
    _watcher.EnableRaisingEvents = false;
  }

  protected virtual void OnReloadMap()
  {
    ReloadMap?.Invoke(this, EventArgs.Empty);
  }

  private DateTime GetWriteTime()
  {
    try
    {
      var path = Path.Combine(_watcher.Path, _watcher.Filter);
      return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
    }
    catch (Exception)
    {
      return DateTime.MinValue;
    }
  }

  private void Renamed(object sender, RenamedEventArgs e)
  {
    Changed(sender, e);
  }

  private void Changed(object sender, FileSystemEventArgs e)
  {
    // One prompt at a time; further events for the same save are swallowed.
    if (Interlocked.Exchange(ref _handling, 1) == 1) return;
    StopWatcher();
    _ = HandleChangeAsync();
  }

  private async Task HandleChangeAsync()
  {
    try
    {
      if (DebounceMilliseconds > 0) await Task.Delay(DebounceMilliseconds).ConfigureAwait(false);

      // Our own saves (and a modification the user already declined) leave the write time unchanged.
      var path = Path.Combine(_watcher.Path, _watcher.Filter);
      if (!File.Exists(path) || GetWriteTime() == _lastWriteUtc)
      {
        StartWatcher();
        return;
      }

      var prompt = ConfirmReloadPromptAsync;
      if (prompt == null)
      {
        StartWatcher();
        return;
      }

      var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

      void Run()
      {
        _ = PromptAndReloadAsync(prompt, done);
      }

      if (UIThreadInvoker != null)
        UIThreadInvoker(Run);
      else
        Run();

      await done.Task.ConfigureAwait(false);
    }
    catch (Exception)
    {
      StartWatcher();
    }
    finally
    {
      Interlocked.Exchange(ref _handling, 0);
    }
  }

  private async Task PromptAndReloadAsync(Func<bool, Task<bool>> prompt, TaskCompletionSource<bool> done)
  {
    try
    {
      var reload = await prompt(Project.Current.IsDirty);
      if (reload)
        OnReloadMap();
      else
        StartWatcher();
    }
    catch (Exception)
    {
      StartWatcher();
    }
    finally
    {
      done.TrySetResult(true);
    }
  }
}
