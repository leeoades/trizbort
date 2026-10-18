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

public interface IDialogService
{
    Task ShowAboutDialogAsync(Window? owner);
    Task ShowMessageAsync(Window? owner, string title, string message);
    Task<bool> ShowConfirmationAsync(Window? owner, string title, string message);
    Task<ConfirmationResult> ShowSaveDiscardCancelAsync(Window? owner, string title, string message);

    Task<bool> ShowRoomPropertiesAsync(Window? owner, Room room);
    Task<bool> ShowConnectionPropertiesAsync(Window? owner, Connection connection);
    Task<bool> ShowMapLabelPropertiesAsync(Window? owner, MapLabel label);
    Task<bool> ShowElementPropertiesAsync(Window? owner, Element element);

    Task<bool> ShowMapSettingsAsync(Window? owner);
    Task<bool> ShowMapThemeAsync(Window? owner);
    Task<bool> ShowAppSettingsAsync(Window? owner);
    Task<AutomapSettings?> ShowAutomapSettingsAsync(Window? owner);
}
