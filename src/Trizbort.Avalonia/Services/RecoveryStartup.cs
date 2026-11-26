using System.Diagnostics;
using Avalonia.Controls;
using Trizbort.Domain.Application;
using Trizbort.Domain.Recovery;
using Trizbort.Editor;

namespace Trizbort.Avalonia.Services;

/// <summary>
///   At start-up, offers to recover unsaved work left behind by an instance that did not close properly (ticket 135).
/// </summary>
public static class RecoveryStartup
{
    /// <param name="opening">
    ///   The map Recast was started to open, if any (double-clicked, say): the question says opening the unsaved
    ///   changes is instead of it (Lee, 26 Nov 2026).
    /// </param>
    public static string BuildMessage(RecoveryEntry entry, string? opening = null)
    {
        var title = string.IsNullOrWhiteSpace(entry.Title) ? "Untitled" : entry.Title;
        var time = entry.SavedUtc.ToLocalTime().ToString("g");
        var instead = string.IsNullOrWhiteSpace(opening) ? string.Empty : $" instead of {Path.GetFileName(opening)}";
        return $"{AppInfo.ProductName} didn't close properly last time. Your unsaved changes to {title} (from {time}) "
               + $"were kept. Open them{instead}?";
    }

    /// <summary>
    ///   Asks about each orphaned copy, whatever map Recast was started to open. Open opens the copy as an unsaved map
    ///   (only one per start; any further copies are kept for the next start), Delete deletes it, Ask Me Next Time
    ///   keeps it. Returns true if a map was recovered, so the caller should not replace it by opening another file.
    /// </summary>
    public static async Task<bool> RunAsync(
        EditorSession session,
        RecoveryStore store,
        RecoveryService? service,
        IDialogService dialogs,
        Window? owner,
        string? opening = null)
    {
        IList<RecoveryEntry> orphans;
        try
        {
            orphans = store.FindOrphans();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Recovery scan failed: {ex.Message}");
            return false;
        }

        foreach (var entry in orphans)
        {
            var choice = await dialogs.ShowRecoverDiscardLaterAsync(owner, AppInfo.ProductName, BuildMessage(entry, opening));
            switch (choice)
            {
                case RecoveryChoice.Discard:
                    store.Delete(entry);
                    break;
                case RecoveryChoice.Recover:
                    if (Recover(session, store, service, entry)) return true;
                    await dialogs.ShowMessageAsync(
                        owner, AppInfo.ProductName, "The recovery copy could not be opened. It has been kept.");
                    break;
            }
        }

        return false;
    }

    private static bool Recover(EditorSession session, RecoveryStore store, RecoveryService? service, RecoveryEntry entry)
    {
        try
        {
            var project = new Project();
            if (!new MapLoader(project).LoadMap(entry.CopyPath)) return false;

            // the map is unsaved: Save writes to the original file, or asks for a name if there was none
            project.FileName = entry.OriginalFileName ?? string.Empty;
            Project.Current = project;
            // loading watched the recovery copy, which is about to go: watch the original file if it is still there
            if (project.HasFileName && File.Exists(project.FileName)) project.InitFileWWatcher(project.FileName);
            else Project.FileWatcher.InitializeWatcher(null);
            session.OnMapLoaded();
            session.Undo.MarkUnsaved();
            project.IsDirty = true;
            session.CheckDirty();

            // our own copy first, so the work is never without one: the orphan goes only once that copy exists,
            // otherwise (recovery off, or the write failed) when the map is saved or discarded
            if (service != null && service.WriteNow(true)) store.Delete(entry);
            else service?.DeleteWhenSafe(entry);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Recovery failed: {ex}");
            return false;
        }
    }
}
