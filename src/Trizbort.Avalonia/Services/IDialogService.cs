using Avalonia.Controls;
using Trizbort.Automap;
using Trizbort.Domain.Elements;

namespace Trizbort.Avalonia.Services;

public enum ConfirmationResult
{
    Save,
    Discard,
    Cancel
}

public enum RecoveryChoice
{
    Recover,
    Discard,
    DecideLater
}

public interface IDialogService
{
    Task ShowAboutDialogAsync(Window? owner);
    Task ShowMessageAsync(Window? owner, string title, string message);
    Task<bool> ShowConfirmationAsync(Window? owner, string title, string message);
    Task<ConfirmationResult> ShowSaveDiscardCancelAsync(Window? owner, string title, string message);
    Task<RecoveryChoice> ShowRecoverDiscardLaterAsync(Window? owner, string title, string message);

    Task<bool> ShowRoomPropertiesAsync(Window? owner, Room room);
    Task<bool> ShowConnectionPropertiesAsync(Window? owner, Connection connection);
    Task<bool> ShowMapLabelPropertiesAsync(Window? owner, MapLabel label);
    Task<bool> ShowElementPropertiesAsync(Window? owner, Element element);

    Task<bool> ShowMapSettingsAsync(Window? owner);

    /// <summary>
    ///   Map Settings at the Regions tab with a new region selected to name. Null when cancelled; otherwise the
    ///   region's name and the edit that applies the settings (for the session to run as one undo step).
    /// </summary>
    Task<Trizbort.Editor.NewRegionChoice?> ShowNewRegionAsync(Window owner) =>
        Task.FromResult<Trizbort.Editor.NewRegionChoice?>(null);
    Task<bool> ShowMapThemeAsync(Window? owner);
    Task<bool> ShowAppSettingsAsync(Window? owner);
    Task<AutomapSettings?> ShowAutomapSettingsAsync(Window? owner);

    /// <summary>
    ///     Automap Settings while automap runs (the automap panel's cog): the options that can change part way through.
    ///     The settings chosen, or null when cancelled.
    /// </summary>
    Task<AutomapSettings?> ShowAutomapRunSettingsAsync(Window? owner, AutomapSettings current) =>
        Task.FromResult<AutomapSettings?>(null);
}
