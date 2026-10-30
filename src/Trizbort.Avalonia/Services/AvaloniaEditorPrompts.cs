using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
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
public sealed class AvaloniaEditorPrompts(Func<Window?> getOwner, IDialogService? dialogService = null) : IEditorPrompts
{
    private readonly Func<Window?> _getOwner = getOwner;
    private readonly IDialogService? _dialogService = dialogService;

    /// <summary>Opens the properties dialog for the element (room, label or connection). Returns true when accepted.</summary>
    public Task<bool> EditElementAsync(Element element, CancellationToken token = default)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return ShowElementPropertiesAsync(element);
        }

        return Dispatcher.UIThread.InvokeAsync(() => ShowElementPropertiesAsync(element));
    }

    private async Task<bool> ShowElementPropertiesAsync(Element element)
    {
        var owner = _getOwner();
        var service = _dialogService ?? App.Services?.GetService<IDialogService>();
        if (owner == null || service == null)
        {
            return false;
        }

        return await service.ShowElementPropertiesAsync(owner, element);
    }

    /// <summary>Shows a message box and completes when it is dismissed.</summary>
    public Task ShowMessageAsync(string title, string message, CancellationToken token = default)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return ShowMessageCoreAsync(title, message);
        }

        return Dispatcher.UIThread.InvokeAsync(() => ShowMessageCoreAsync(title, message));
    }

    private async Task ShowMessageCoreAsync(string title, string message)
    {
        var service = _dialogService ?? App.Services?.GetService<IDialogService>();
        if (service == null)
        {
            return;
        }

        await service.ShowMessageAsync(_getOwner(), title, message);
    }

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

    public Task<string?> QuickFindAsync(IReadOnlyList<string> suggestions, CancellationToken token = default)
    {
        return OnUiThreadAsync(async () =>
        {
            var owner = _getOwner();
            if (owner == null)
            {
                return null;
            }

            var dialog = new QuickFindDialog();
            dialog.SetSuggestions(suggestions);
            return await dialog.ShowDialog<string?>(owner);
        });
    }

    public Task ShowTextReportAsync(string title, string text, CancellationToken token = default)
    {
        return OnUiThreadAsync<object?>(async () =>
        {
            var owner = _getOwner();
            if (owner != null)
            {
                var window = new MapStatisticsWindow { Title = title, ReportText = text };
                await window.ShowDialog(owner);
            }

            return null;
        });
    }

    public Task<string?> PickSaveFileAsync(string title, string suggestedPath, string extension, CancellationToken token = default)
    {
        return OnUiThreadAsync(async () =>
        {
            var owner = _getOwner();
            if (owner == null)
            {
                return null;
            }

            var options = new global::Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = title,
                DefaultExtension = extension.TrimStart('.'),
                SuggestedFileName = Path.GetFileName(suggestedPath),
                FileTypeChoices =
                [
                    new global::Avalonia.Platform.Storage.FilePickerFileType("Log files") { Patterns = ["*" + extension] },
                    global::Avalonia.Platform.Storage.FilePickerFileTypes.All
                ]
            };
            var folder = Path.GetDirectoryName(suggestedPath);
            if (!string.IsNullOrEmpty(folder))
            {
                options.SuggestedStartLocation = await owner.StorageProvider.TryGetFolderFromPathAsync(new Uri(Path.GetFullPath(folder)));
            }

            var file = await owner.StorageProvider.SaveFilePickerAsync(options);
            return file?.Path.LocalPath;
        });
    }

    public Task MessageAsync(string message, string title, CancellationToken token = default)
    {
        return OnUiThreadAsync<object?>(async () =>
        {
            var service = _dialogService ?? App.Services?.GetService<IDialogService>();
            if (service != null)
            {
                await service.ShowMessageAsync(_getOwner(), title, message);
            }

            return null;
        });
    }

    public Task<bool> OpenUriAsync(string uri, CancellationToken token = default)
    {
        return OnUiThreadAsync(async () =>
        {
            try
            {
                var launcher = _getOwner()?.Launcher;
                return launcher != null && await launcher.LaunchUriAsync(new Uri(uri));
            }
            catch (Exception)
            {
                return false;
            }
        });
    }

    private static Task<T> OnUiThreadAsync<T>(Func<Task<T>> action)
    {
        return Dispatcher.UIThread.CheckAccess() ? action() : Dispatcher.UIThread.InvokeAsync(action);
    }

    public Task<string?> ChooseRegionAsync(
        IReadOnlyList<string> regions,
        string current,
        CancellationToken token = default)
    {
        var owner = _getOwner();
        if (owner == null)
        {
            return Task.FromResult<string?>(null);
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            return ShowRegionPickerAsync(owner, regions, current);
        }

        return Dispatcher.UIThread.InvokeAsync(() => ShowRegionPickerAsync(owner, regions, current));
    }

    private static async Task<string?> ShowRegionPickerAsync(Window owner, IReadOnlyList<string> regions, string current)
    {
        var list = new ListBox { ItemsSource = regions.ToList(), SelectedItem = current, MinHeight = 160 };
        var ok = new Button { Content = "_OK", IsDefault = true, MinWidth = 80 };
        var cancel = new Button { Content = "_Cancel", IsCancel = true, MinWidth = 80 };
        var buttons = new StackPanel
        {
            Orientation = global::Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 8,
            Children = { ok, cancel }
        };
        var panel = new DockPanel { Margin = new global::Avalonia.Thickness(12), LastChildFill = true };
        DockPanel.SetDock(buttons, global::Avalonia.Controls.Dock.Bottom);
        buttons.Margin = new global::Avalonia.Thickness(0, 12, 0, 0);
        panel.Children.Add(buttons);
        panel.Children.Add(list);

        var dialog = new Window
        {
            Title = "Change Region",
            Width = 320,
            Height = 320,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = panel
        };
        ok.Click += (_, _) => dialog.Close(list.SelectedItem as string);
        cancel.Click += (_, _) => dialog.Close(null);
        list.DoubleTapped += (_, _) =>
        {
            if (list.SelectedItem is string)
            {
                dialog.Close(list.SelectedItem as string);
            }
        };
        return await dialog.ShowDialog<string?>(owner);
    }
}
