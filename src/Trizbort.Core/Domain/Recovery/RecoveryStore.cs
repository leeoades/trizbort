using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;

namespace Trizbort.Domain.Recovery;

/// <summary>
///   A recovery copy left behind by an instance that ended without cleaning up (ticket 135).
/// </summary>
public sealed class RecoveryEntry
{
  public string SessionId { get; set; }

  /// <summary>The copy of the map, in the normal map format.</summary>
  public string CopyPath { get; set; }

  public string MetaPath { get; set; }

  /// <summary>The file the map was saved as, or empty if it had never been saved.</summary>
  public string OriginalFileName { get; set; }

  public string Title { get; set; }

  public DateTime SavedUtc { get; set; }

  public string AppVersion { get; set; }
}

/// <summary>
///   Keeps a copy of the unsaved map in a recovery folder so work survives a crash. Each running instance owns
///   <c>&lt;session&gt;.trizbort</c> (the map), <c>&lt;session&gt;.json</c> (details) and holds <c>&lt;session&gt;.lock</c>
///   open exclusively for its lifetime; a copy whose lock is not held belongs to an instance that is gone.
///   Nothing here throws into the app: failures return false / empty.
/// </summary>
public sealed class RecoveryStore : IDisposable
{
  public const string MapExtension = ".trizbort";
  public const string MetaExtension = ".json";
  public const string LockExtension = ".lock";
  public const string CrashLogFolderName = "crash-logs";
  public const string RecoveryFolderName = "recovery";

  private readonly object _gate = new();
  private FileStream _lock;

  public RecoveryStore(string folder, string sessionId = null)
  {
    Folder = folder;
    SessionId = string.IsNullOrEmpty(sessionId) ? Guid.NewGuid().ToString("N") : sessionId;
  }

  /// <summary>The store under the per-user config folder.</summary>
  public static RecoveryStore CreateDefault()
  {
    return new RecoveryStore(DefaultFolder);
  }

  public static string DefaultFolder => Path.Combine(AppPaths.ConfigFolder, RecoveryFolderName);

  public static string DefaultCrashLogFolder => Path.Combine(AppPaths.ConfigFolder, CrashLogFolderName);

  public string Folder { get; }

  public string SessionId { get; }

  public string CopyPath => Path.Combine(Folder, SessionId + MapExtension);

  public string MetaPath => Path.Combine(Folder, SessionId + MetaExtension);

  public string LockPath => Path.Combine(Folder, SessionId + LockExtension);

  /// <summary>True if this instance currently has a copy on disk.</summary>
  public bool HasCopy => File.Exists(CopyPath);

  /// <summary>
  ///   Writes the copy of <paramref name="project" /> atomically (temp file, then move over). Returns false on any
  ///   failure.
  /// </summary>
  public bool WriteCopy(Project project, string appVersion)
  {
    if (project == null) return false;
    try
    {
      lock (_gate)
      {
        Directory.CreateDirectory(Folder);
        AcquireLock();

        // MapSaver only writes names ending in .trizbort
        var tempMap = Path.Combine(Folder, SessionId + ".tmp" + MapExtension);
        if (!new MapSaver(project).SaveMap(tempMap)) return false;
        MoveOver(tempMap, CopyPath);

        var meta = new RecoveryEntry
        {
          SessionId = SessionId,
          OriginalFileName = project.FileName ?? string.Empty,
          Title = DescribeTitle(project),
          SavedUtc = DateTime.UtcNow,
          AppVersion = appVersion ?? string.Empty
        };
        var tempMeta = Path.Combine(Folder, SessionId + ".tmp" + MetaExtension);
        File.WriteAllText(tempMeta, JsonConvert.SerializeObject(meta, Formatting.Indented), new UTF8Encoding(false));
        MoveOver(tempMeta, MetaPath);
        return true;
      }
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Recovery copy failed: {ex}");
      return false;
    }
  }

  /// <summary>Deletes this instance's copy (the lock stays held until <see cref="Dispose" />).</summary>
  public void DeleteCopy()
  {
    lock (_gate)
    {
      TryDelete(CopyPath);
      TryDelete(MetaPath);
    }
  }

  /// <summary>Lists copies left by instances that are no longer running, oldest first.</summary>
  public IList<RecoveryEntry> FindOrphans()
  {
    var result = new List<RecoveryEntry>();
    try
    {
      if (!Directory.Exists(Folder)) return result;
      foreach (var metaPath in Directory.GetFiles(Folder, "*" + MetaExtension))
      {
        try
        {
          var id = Path.GetFileNameWithoutExtension(metaPath);
          if (id.EndsWith(".tmp", StringComparison.Ordinal) || id == SessionId) continue;
          var copyPath = Path.Combine(Folder, id + MapExtension);
          if (!File.Exists(copyPath)) continue;
          if (IsLockHeld(Path.Combine(Folder, id + LockExtension))) continue;

          var entry = JsonConvert.DeserializeObject<RecoveryEntry>(File.ReadAllText(metaPath)) ?? new RecoveryEntry();
          entry.SessionId = id;
          entry.CopyPath = copyPath;
          entry.MetaPath = metaPath;
          entry.OriginalFileName ??= string.Empty;
          entry.Title ??= string.Empty;
          result.Add(entry);
        }
        catch (Exception ex)
        {
          Debug.WriteLine($"Skipping recovery entry {metaPath}: {ex.Message}");
        }
      }
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Listing recovery copies failed: {ex.Message}");
    }

    result.Sort((a, b) => a.SavedUtc.CompareTo(b.SavedUtc));
    return result;
  }

  /// <summary>Deletes an orphaned copy, its details and its stale lock file.</summary>
  public void Delete(RecoveryEntry entry)
  {
    if (entry == null) return;
    TryDelete(entry.CopyPath);
    TryDelete(entry.MetaPath);
    if (!string.IsNullOrEmpty(entry.SessionId)) TryDelete(Path.Combine(Folder, entry.SessionId + LockExtension));
  }

  public void Dispose()
  {
    lock (_gate)
    {
      DeleteCopy();
      if (_lock == null) return;
      try
      {
        _lock.Dispose();
      }
      catch (Exception)
      {
        // best effort
      }

      _lock = null;
      TryDelete(LockPath);
    }
  }

  /// <summary>
  ///   Writes a crash log to <paramref name="folder" /> (default: the config folder's crash-logs) and returns its
  ///   path, or null if it could not be written.
  /// </summary>
  public static string WriteCrashLog(Exception exception, string appVersion, string folder = null)
  {
    try
    {
      folder ??= DefaultCrashLogFolder;
      Directory.CreateDirectory(folder);
      var now = DateTime.UtcNow;
      var path = Path.Combine(folder, now.ToString("yyyyMMdd-HHmmssfff") + ".txt");
      for (var n = 1; File.Exists(path); n++)
        path = Path.Combine(folder, now.ToString("yyyyMMdd-HHmmssfff") + "-" + n + ".txt");

      var text = new StringBuilder();
      text.AppendLine("Version: " + appVersion);
      text.AppendLine("Time (UTC): " + now.ToString("o"));
      text.AppendLine("OS: " + RuntimeInformation.OSDescription);
      text.AppendLine("Runtime: " + RuntimeInformation.FrameworkDescription);
      text.AppendLine();
      text.AppendLine(exception?.ToString() ?? "(no exception)");
      File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
      return path;
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Crash log failed: {ex}");
      return null;
    }
  }

  private void AcquireLock()
  {
    if (_lock != null) return;
    _lock = new FileStream(LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
  }

  private static bool IsLockHeld(string lockPath)
  {
    if (!File.Exists(lockPath)) return false;
    try
    {
      using (new FileStream(lockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
      {
      }

      return false;
    }
    catch (IOException)
    {
      return true;
    }
    catch (UnauthorizedAccessException)
    {
      return true;
    }
  }

  private static string DescribeTitle(Project project)
  {
    if (!string.IsNullOrWhiteSpace(project.Title)) return project.Title;
    return project.HasFileName ? Path.GetFileNameWithoutExtension(project.FileName) : "Untitled";
  }

  private static void MoveOver(string temp, string target)
  {
    File.Move(temp, target, true);
  }

  private static void TryDelete(string path)
  {
    try
    {
      if (File.Exists(path)) File.Delete(path);
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Could not delete {path}: {ex.Message}");
    }
  }
}
