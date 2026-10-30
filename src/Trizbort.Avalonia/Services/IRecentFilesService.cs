using Avalonia.Controls;
using Trizbort.Domain.AppSettings;

namespace Trizbort.Avalonia.Services;

/// <summary>Owns the recent maps list and the "open recent" entry point.</summary>
public interface IRecentFilesService
{
    /// <summary>Entries newest first; local files that no longer exist are hidden.</summary>
    IReadOnlyList<RecentFileEntry> GetEntries();

    /// <summary>
    ///   Opens a recent map. A missing file shows a message and is dropped from the list. Otherwise
    ///   <paramref name="canLoseProject" /> (the unsaved-changes prompt) must allow it before the map is loaded.
    /// </summary>
    Task<bool> OpenRecentAsync(string path, Window? owner, Func<Task<bool>>? canLoseProject = null);

    void Clear();
}
