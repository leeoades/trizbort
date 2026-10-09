using System;
using System.IO;
using System.Windows.Forms;
using Trizbort.Domain.Application;
using Trizbort.UI;

namespace Trizbort.Domain.Watchers;

public class TrizbortFileWatcher : IDisposable {
  private readonly FileSystemWatcher _watcher = new();

  internal string WatchedPath => _watcher.Path;
  internal string WatchedFilter => _watcher.Filter;
  internal bool IsWatching => _watcher.EnableRaisingEvents;

  public TrizbortFileWatcher()
  {
    _watcher.NotifyFilter = NotifyFilters.LastWrite;
    _watcher.Changed += Changed;
  }

  public void Dispose()
  {
    _watcher?.Dispose();
  }

  public void InitializeWatcher(string fileToWatch)
  {
    StopWatcher();
    _watcher.Path = Path.GetDirectoryName(fileToWatch);
    _watcher.Filter = Path.GetFileName(fileToWatch);
    StartWatcher();
  }

  public event EventHandler ReloadMap;

  public void StartWatcher()
  {
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
    Project.Current.Canvas.BeginInvoke(() => {
      if (UserInteraction.ShowMessage(
            TrizbortApplication.MainForm?.Canvas,
            $"This map has been modified by another program.{Environment.NewLine}Do you want to reload it{DirtyMessage()}?",
            "Reload",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) == DialogResult.Yes)
        OnReloadMap();
      else
        StartWatcher();
    });
  }

  private string DirtyMessage()
  {
    if (!Project.Current.IsDirty) return "";
    return " and lose changes made in Trizbort";
  }
}