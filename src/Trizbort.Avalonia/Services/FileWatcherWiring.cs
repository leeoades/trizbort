using Avalonia.Controls;
using Avalonia.Threading;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;
using Trizbort.Domain.Watchers;

namespace Trizbort.Avalonia.Services;

/// <summary>
///   Connects Core's <see cref="TrizbortFileWatcher" /> to the Avalonia UI: prompts when the open map
///   changes on disk and reloads it (issue I-003).
/// </summary>
public static class FileWatcherWiring
{
    public const string ReloadMessage = "The map was modified by another program. Reload it?";
    public const string ReloadDirtyMessage = "The map was modified by another program. Reload it? Unsaved changes will be lost.";

    public static void Wire(IDialogService dialogService, Func<Window?> getOwner, Action? afterReload = null)
    {
        TrizbortFileWatcher.UIThreadInvoker = action => Dispatcher.UIThread.Post(action);
        TrizbortFileWatcher.ConfirmReloadPromptAsync = dirty =>
            dialogService.ShowConfirmationAsync(getOwner(), "Trizbort", dirty ? ReloadDirtyMessage : ReloadMessage);
        Project.MapReloader = path =>
        {
            Reload(path, dialogService, getOwner);
            afterReload?.Invoke();
        };
    }

    /// <summary>Loads <paramref name="path" /> without the unsaved-changes prompt and makes it the current project.</summary>
    internal static bool Reload(string path, IDialogService dialogService, Func<Window?> getOwner)
    {
        try
        {
            var project = new Project();
            var loader = new MapLoader(project);
            if (!loader.LoadMap(path)) return false;
            project.FileName = path;
            project.IsDirty = false;
            Project.Current = project;
            return true;
        }
        catch (Exception ex)
        {
            _ = dialogService.ShowMessageAsync(getOwner(), "Reload Map Error",
                $"Error reloading map file '{Path.GetFileName(path)}':\n{ex.Message}");
            return false;
        }
    }
}
