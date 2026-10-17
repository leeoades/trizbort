using System;
using System.IO;
using Trizbort.Domain.Application;

namespace Trizbort.Domain.Watchers;

public class TrizbortFileWatcher : IDisposable
{
  private readonly FileSystemWatcher _watcher = new();

  public TrizbortFileWatcher()
  {
    _watcher.NotifyFilter = NotifyFilters.LastWrite;
    _watcher.Changed += Changed;
  }

  public string WatchedPath => _watcher.Path;
  public string WatchedFilter => _watcher.Filter;
  public bool IsWatching => _watcher.EnableRaisingEvents;

  public static Action<Action> UIThreadInvoker { get; set; }
  public static Func<bool, bool> ConfirmReloadPrompt { get; set; }

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
    if (!string.IsNullOrEmpty(_watcher.Path))
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

  private void Changed(object sender, FileSystemEventArgs e)
  {
    StopWatcher();
    void HandleChange()
    {
      if (ConfirmReloadPrompt != null && ConfirmReloadPrompt(Project.Current.IsDirty))
        OnReloadMap();
      else
        StartWatcher();
    }

    if (UIThreadInvoker != null)
      UIThreadInvoker(HandleChange);
    else
      HandleChange();
  }
}
