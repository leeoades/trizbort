using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Watchers;

namespace Trizbort.Core.Tests;

[TestFixture]
[Category("Unit")]
[NonParallelizable]
public class FileWatcherTests
{
  private string _directory;
  private string _file;
  private TrizbortFileWatcher _watcher;
  private int _prompts;
  private int _reloads;
  private bool _answer;
  private int _previousDebounce;

  [SetUp]
  public void SetUp()
  {
    _directory = Path.Combine(Path.GetTempPath(), "trizbort-fw-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(_directory);
    _file = Path.Combine(_directory, "map.trizbort");
    File.WriteAllText(_file, "one");
    File.SetLastWriteTimeUtc(_file, DateTime.UtcNow.AddMinutes(-5));

    _prompts = 0;
    _reloads = 0;
    _answer = true;
    _previousDebounce = TrizbortFileWatcher.DebounceMilliseconds;
    TrizbortFileWatcher.DebounceMilliseconds = 50;
    TrizbortFileWatcher.UIThreadInvoker = null;
    TrizbortFileWatcher.ConfirmReloadPromptAsync = _ =>
    {
      Interlocked.Increment(ref _prompts);
      return Task.FromResult(_answer);
    };

    _watcher = new TrizbortFileWatcher();
    _watcher.ReloadMap += (_, _) => Interlocked.Increment(ref _reloads);
    _watcher.InitializeWatcher(_file);
  }

  [TearDown]
  public void TearDown()
  {
    _watcher.Dispose();
    TrizbortFileWatcher.ConfirmReloadPromptAsync = null;
    TrizbortFileWatcher.UIThreadInvoker = null;
    TrizbortFileWatcher.DebounceMilliseconds = _previousDebounce;
    Directory.Delete(_directory, true);
  }

  private void ModifyExternally()
  {
    File.WriteAllText(_file, "two");
    File.SetLastWriteTimeUtc(_file, DateTime.UtcNow.AddMinutes(5));
  }

  private static bool WaitFor(Func<bool> condition, int timeoutMs = 5000)
  {
    var end = DateTime.UtcNow.AddMilliseconds(timeoutMs);
    while (DateTime.UtcNow < end)
    {
      if (condition()) return true;
      Thread.Sleep(20);
    }

    return condition();
  }

  [Test]
  public void ExternalChange_PromptsOnceAndReloads()
  {
    ModifyExternally();
    WaitFor(() => _reloads > 0).ShouldBeTrue();
    Thread.Sleep(300);
    _prompts.ShouldBe(1);
    _reloads.ShouldBe(1);
  }

  [Test]
  public void DecliningDoesNotReload_AndWatcherIsRearmed()
  {
    _answer = false;
    ModifyExternally();
    WaitFor(() => _prompts > 0).ShouldBeTrue();
    WaitFor(() => _watcher.IsWatching).ShouldBeTrue();
    _reloads.ShouldBe(0);
  }

  [Test]
  public void OwnSave_DoesNotPrompt()
  {
    _watcher.StopWatcher();
    ModifyExternally();
    _watcher.StartWatcher();
    Thread.Sleep(500);
    _prompts.ShouldBe(0);
    _reloads.ShouldBe(0);
  }

  [Test]
  public void SaveByRename_Prompts()
  {
    var temp = Path.Combine(_directory, "temp.tmp");
    File.WriteAllText(temp, "two");
    File.SetLastWriteTimeUtc(temp, DateTime.UtcNow.AddMinutes(5));
    File.Move(temp, _file, true);
    WaitFor(() => _reloads > 0).ShouldBeTrue();
    _prompts.ShouldBe(1);
  }

  [Test]
  public void LegacySyncPrompt_IsAdaptedToAsyncPrompt()
  {
    TrizbortFileWatcher.ConfirmReloadPrompt = _ => true;
    TrizbortFileWatcher.ConfirmReloadPromptAsync.ShouldNotBeNull();
    TrizbortFileWatcher.ConfirmReloadPromptAsync(false).Result.ShouldBeTrue();
    TrizbortFileWatcher.ConfirmReloadPrompt = null;
    TrizbortFileWatcher.ConfirmReloadPromptAsync.ShouldBeNull();
  }
}
