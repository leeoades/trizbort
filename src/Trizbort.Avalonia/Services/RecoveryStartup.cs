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
    public static string BuildMessage(RecoveryEntry entry)
    {
        var title = string.IsNullOrWhiteSpace(entry.Title) ? "Untitled" : entry.Title;
        var time = entry.SavedUtc.ToLocalTime().ToString("g");
        return $"{AppInfo.ProductName} didn't close properly. Recover your unsaved changes to {title} (from {time})?";
    }

    /// <summary>
    ///   Asks about each orphaned copy. Recover opens the copy as an unsaved map (only one per start; any further
    ///   copies are kept for the next start), Discard deletes it, Decide later keeps it. Returns true if a map was
    ///   recovered, so the caller should not replace it by reopening another file.
    /// </summary>
    public static async Task<bool> RunAsync(
        EditorSession session,
        RecoveryStore store,
        RecoveryService? service,
        IDialogService dialogs,
        Window? owner)
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
            var choice = await dialogs.ShowRecoverDiscardLaterAsync(owner, AppInfo.ProductName, BuildMessage(entry));
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
            session.OnMapLoaded();
            session.Undo.MarkUnsaved();
            project.IsDirty = true;
            session.CheckDirty();

            // our own copy first, so the work is never without one
            if (service != null) service.WriteNow(true);
            store.Delete(entry);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Recovery failed: {ex}");
            return false;
        }
    }
}
