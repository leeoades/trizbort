using System;
using System.Collections.Generic;

namespace Trizbort.Domain.AppSettings;

/// <summary>
///   One line of the recent maps list.
/// </summary>
public sealed class RecentFileEntry
{
  public RecentFileEntry(string path, string label)
  {
    Path = path;
    Label = label;
  }

  public string Path { get; }

  /// <summary>Menu text such as "&amp;1 C:\maps\a.trizbort".</summary>
  public string Label { get; }
}

/// <summary>
///   The most-recently-used list of maps, stored in <see cref="ApplicationSettings.RecentProjects" />.
/// </summary>
public sealed class RecentFiles
{
  public const int MaxLabelPathLength = 60;

  private readonly ApplicationSettings _settings;

  public RecentFiles(ApplicationSettings settings)
  {
    _settings = settings ?? throw new ArgumentNullException(nameof(settings));
  }

  /// <summary>The list of the live application settings.</summary>
  public static RecentFiles Current => new(ApplicationSettingsController.AppSettings);

  /// <summary>File names compare ignoring case on Windows and macOS, exactly on Linux.</summary>
  public static StringComparison PathComparison =>
    OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
      ? StringComparison.OrdinalIgnoreCase
      : StringComparison.Ordinal;

  public static bool IsUrl(string path)
  {
    return Uri.TryCreate(path, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
  }

  /// <summary>
  ///   Adds <paramref name="path" /> at the top (removing an earlier equal entry) and trims the list to the maximum.
  /// </summary>
  public void Add(string path)
  {
    if (string.IsNullOrEmpty(path)) return;
    RemoveFrom(_settings.RecentProjects, path);
    _settings.RecentProjects.Insert(0, path);
    if (_settings.RecentProjects.Count > ApplicationSettingsController.RecentProjectsMaxCount)
      _settings.RecentProjects.RemoveRange(
        ApplicationSettingsController.RecentProjectsMaxCount,
        _settings.RecentProjects.Count - ApplicationSettingsController.RecentProjectsMaxCount);
  }

  /// <summary>
  ///   Entries newest first with numbered labels. Local files for which <paramref name="exists" /> is false are hidden
  ///   (not removed); URLs are always kept.
  /// </summary>
  public IReadOnlyList<RecentFileEntry> GetEntries(Func<string, bool> exists)
  {
    var result = new List<RecentFileEntry>();
    foreach (var path in _settings.RecentProjects)
    {
      if (string.IsNullOrEmpty(path)) continue;
      if (!IsUrl(path) && exists != null && !exists(path)) continue;
      if (result.Count >= ApplicationSettingsController.RecentProjectsMaxCount) break;
      result.Add(new RecentFileEntry(path, $"&{result.Count + 1} {Shorten(path, MaxLabelPathLength)}"));
    }

    return result;
  }

  public void Remove(string path)
  {
    RemoveFrom(_settings.RecentProjects, path);
  }

  public void Clear()
  {
    _settings.RecentProjects.Clear();
  }

  /// <summary>Shortens <paramref name="path" /> to <paramref name="maxLength" /> characters, replacing the middle with "...".</summary>
  public static string Shorten(string path, int maxLength)
  {
    if (path == null || path.Length <= maxLength || maxLength < 5) return path;
    var keep = maxLength - 3;
    var head = keep / 2;
    var tail = keep - head;
    return path.Substring(0, head) + "..." + path.Substring(path.Length - tail);
  }

  internal static void RemoveFrom(List<string> list, string path)
  {
    list.RemoveAll(p => string.Equals(p, path, PathComparison));
  }
}
