using System.Threading.Tasks;
using Avalonia.Controls;

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
}
