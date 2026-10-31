using Avalonia.Controls;
using Trizbort.Domain.AppSettings;

namespace Trizbort.Avalonia.Services;

public class RecentFilesService(IStorageService storage, IDialogService dialogs) : IRecentFilesService
{
    private readonly IStorageService _storage = storage;
    private readonly IDialogService _dialogs = dialogs;

    public IReadOnlyList<RecentFileEntry> GetEntries()
    {
        return RecentFiles.Current.GetEntries(File.Exists);
    }

    public async Task<bool> OpenRecentAsync(string path, Window? owner, Func<Task<bool>>? canLoseProject = null)
    {
        if (string.IsNullOrEmpty(path)) return false;

        if (!RecentFiles.IsUrl(path) && !File.Exists(path))
        {
            RecentFiles.Current.Remove(path);
            try
            {
                ApplicationSettingsController.SaveSettings();
            }
            catch (Exception)
            {
                // settings are best effort
            }

            await _dialogs.ShowMessageAsync(owner, "Open Recent Map", $"File not found: {path}");
            return false;
        }

        if (canLoseProject != null && !await canLoseProject()) return false;

        return await _storage.OpenMapFromPathAsync(path, owner);
    }

    public void Clear()
    {
        RecentFiles.Current.Clear();
        try
        {
            ApplicationSettingsController.SaveSettings();
        }
        catch (Exception)
        {
            // settings are best effort
        }
    }
}
