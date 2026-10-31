using Avalonia.Controls;

namespace Trizbort.Avalonia.Services;

/// <summary>
///   Some Linux file-chooser portals (seen on COSMIC) ignore the picker's overwrite prompt, so on Linux the app
///   asks itself after a save path has been picked. Other platforms confirm in the picker.
/// </summary>
internal static class OverwriteGuard
{
    /// <summary>True when the platform save picker cannot be relied on to confirm an overwrite.</summary>
    internal static bool PickerMayNotConfirm => OperatingSystem.IsLinux();

    /// <summary>Returns true when it is fine to write <paramref name="path" /> (does not exist, or the user agreed).</summary>
    internal static async Task<bool> ConfirmAsync(IDialogService? dialogService, Window? owner, string path)
    {
        if (!PickerMayNotConfirm || dialogService == null || !File.Exists(path))
        {
            return true;
        }

        return await dialogService.ShowConfirmationAsync(
            owner,
            "Confirm Overwrite",
            $"'{Path.GetFileName(path)}' already exists. Replace it?");
    }
}
