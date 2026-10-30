using Avalonia.Controls;
using Avalonia.Threading;
using Trizbort.Automap;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views.Dialogs;
using Trizbort.Domain.Elements;
using Trizbort.Editor;

namespace Trizbort.Avalonia.Services;

/// <summary>
///     Interactive <see cref="IEditorPrompts" /> that asks the user with modal dialogs. Without an owner
///     window it behaves like <see cref="NonInteractiveEditorPrompts" />.
/// </summary>
public sealed class AvaloniaEditorPrompts(Func<Window?> getOwner) : IEditorPrompts
{
    private readonly Func<Window?> _getOwner = getOwner;

    public Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(
        Room existing,
        string newRoomName,
        CancellationToken token = default)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return ShowSameDirectionAsync(existing, newRoomName);
        }

        return Dispatcher.UIThread.InvokeAsync(() => ShowSameDirectionAsync(existing, newRoomName));
    }

    public Task<RoomDisambiguation> DisambiguateRoomAsync(
        string transcriptLine,
        string roomDescription,
        IReadOnlyList<Room> candidates,
        CancellationToken token = default)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return ShowDisambiguationAsync(transcriptLine, roomDescription, candidates);
        }

        return Dispatcher.UIThread.InvokeAsync(
            () => ShowDisambiguationAsync(transcriptLine, roomDescription, candidates));
    }

    private async Task<AutomapSameDirectionResult> ShowSameDirectionAsync(Room existing, string newRoomName)
    {
        var owner = _getOwner();
        if (owner == null)
        {
            return await NonInteractiveEditorPrompts.Instance.ChooseSameDirectionAsync(existing, newRoomName);
        }

        var viewModel = new AutomapSameDirectionViewModel(existing, newRoomName);
        var dialog = new AutomapSameDirectionDialog { DataContext = viewModel };
        await dialog.ShowDialog(owner);
        return viewModel.Result;
    }

    private async Task<RoomDisambiguation> ShowDisambiguationAsync(
        string transcriptLine,
        string roomDescription,
        IReadOnlyList<Room> candidates)
    {
        var owner = _getOwner();
        if (owner == null)
        {
            return await NonInteractiveEditorPrompts.Instance.DisambiguateRoomAsync(
                transcriptLine, roomDescription, candidates);
        }

        var viewModel = new DisambiguateRoomsViewModel(transcriptLine, roomDescription, candidates);
        var dialog = new DisambiguateRoomsDialog { DataContext = viewModel };
        await dialog.ShowDialog(owner);
        return viewModel.Result;
    }
}
