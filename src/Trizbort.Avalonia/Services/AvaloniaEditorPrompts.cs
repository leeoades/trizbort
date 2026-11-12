using Avalonia.Controls;
using Avalonia.Threading;
using Trizbort.Avalonia.Controls;
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

    public Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(
        Room from,
        Room existing,
        string newRoomName,
        CancellationToken token = default)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return ShowSameDirectionAsync(existing, newRoomName, from);
        }

        return Dispatcher.UIThread.InvokeAsync(() => ShowSameDirectionAsync(existing, newRoomName, from));
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

    private async Task<AutomapSameDirectionResult> ShowSameDirectionAsync(Room existing, string newRoomName, Room? from = null)
    {
        var owner = _getOwner();
        if (owner == null)
        {
            return await NonInteractiveEditorPrompts.Instance.ChooseSameDirectionAsync(existing, newRoomName);
        }

        var viewModel = new AutomapSameDirectionViewModel(existing, newRoomName) { FromName = from?.Name ?? string.Empty };
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
        // the room picked is shown on the map behind the dialog, with the rooms around it; the view and selection
        // go back as they were afterwards
        var session = App.Services?.GetService<ICommandService>()?.Session;
        var origin = session?.Viewport.TargetOrigin;
        var selection = session?.Selection.Items.ToList();
        if (session != null)
        {
            var newRoomName = candidates.FirstOrDefault()?.Name ?? transcriptLine;
            viewModel.Preview = room =>
            {
                Trizbort.Domain.Misc.Vector centre;
                if (room == null)
                {
                    // a new room: shown faintly where automap would put it (else the room the player came from)
                    Trizbort.Domain.Misc.Rect? where = null;
                    try
                    {
                        where = global::Trizbort.Automap.Automap.Instance.NewRoomPreview();
                    }
                    catch (Exception)
                    {
                        // no preview, but the question still works
                    }

                    MapCanvasControl.GhostRoom = where is { } ghost ? (ghost, newRoomName) : null;
                    session.Selection.Set(new List<Element>());
                    var from = global::Trizbort.Automap.Automap.Instance.CurrentRoom;
                    if (where == null && (from == null || !session.Project.Elements.Contains(from)))
                    {
                        session.Invalidate(EditorChange.Adorners);
                        return;
                    }

                    centre = where?.Center ?? from!.InnerBounds.Center;
                }
                else
                {
                    MapCanvasControl.GhostRoom = null;
                    if (!session.Project.Elements.Contains(room)) return;
                    session.Selection.Set(new List<Element> { room });
                    centre = room.InnerBounds.Center;
                }

                // left of the middle, clear of the dialog on the right
                var visible = session.Viewport.VisibleWorldBounds;
                session.Viewport.GlideTo(new Trizbort.Domain.Misc.Vector(centre.X + visible.Width / 5, centre.Y));
                session.Invalidate(EditorChange.Adorners);
            };
            viewModel.Preview(viewModel.Selected?.Room);
        }

        var dialog = new DisambiguateRoomsDialog { DataContext = viewModel };
        if (session != null)
        {
            // on the right of the window, so the map shows the room picked
            var scale = owner.RenderScaling;
            dialog.WindowStartupLocation = WindowStartupLocation.Manual;
            dialog.Position = new global::Avalonia.PixelPoint(
                owner.Position.X + Math.Max(0, (int)((owner.Bounds.Width - dialog.Width - 24) * scale)),
                owner.Position.Y + (int)(80 * scale));
        }
        Action? unlock = null;
        try
        {
            if (session != null)
            {
                // not modal: the map behind can be looked around while deciding (but not changed); the menus,
                // toolbar, automap panel and shortcuts are off until the question is answered
                MapCanvasControl.ViewOnly = true;
                unlock = LockAllButTheMap(owner);
                var closed = new TaskCompletionSource();
                dialog.Closed += (_, _) => closed.TrySetResult();
                dialog.Show(owner);
                await closed.Task;
            }
            else
            {
                await dialog.ShowDialog(owner);
            }
        }
        finally
        {
            unlock?.Invoke();
            MapCanvasControl.ViewOnly = false;
            MapCanvasControl.GhostRoom = null;
            if (session != null && origin != null)
            {
                session.Selection.Set(selection!.Where(element => session.Project.Elements.Contains(element)).ToList());
                session.Viewport.GlideTo(origin.Value);
            }
        }

        return viewModel.Result;
    }

    /// <summary>
    ///     Turns off everything in <paramref name="owner" /> but the map: the menu, the toolbar, the automap panel and
    ///     the window's shortcuts. Returns how to turn them back on as they were.
    /// </summary>
    private static Action LockAllButTheMap(Window owner)
    {
        var controls = new List<Control>();
        if (owner.FindControl<Control>("MainMenu") is { } menu) controls.Add(menu);
        if (owner.FindControl<Control>("Toolbar") is { } toolbar) controls.Add(toolbar);
        controls.AddRange(global::Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(owner).OfType<AutomapPanel>());
        var wasEnabled = controls.Select(control => control.IsEnabled).ToList();
        foreach (var control in controls) control.IsEnabled = false;
        var keys = owner.KeyBindings.ToList();
        owner.KeyBindings.Clear();
        return () =>
        {
            for (var index = 0; index < controls.Count; index++) controls[index].IsEnabled = wasEnabled[index];
            foreach (var binding in keys) owner.KeyBindings.Add(binding);
        };
    }

    /// <summary>Split Room...: which joins go to the new room, and where the player is. Without a window, the suggestion.</summary>
    public Task<RoomSplitChoice> ChooseRoomSplitAsync(Room room, string newName, IReadOnlyList<Connection> joins,
        IReadOnlyCollection<Connection> ticked, RoomSplitWhere? where, CancellationToken token = default)
    {
        return OnUiThreadAsync(async () =>
        {
            var owner = _getOwner();
            if (owner == null)
            {
                return new RoomSplitChoice(ticked.ToList(), where ?? RoomSplitWhere.Neither);
            }

            var viewModel = new SplitRoomViewModel(room, newName, joins, ticked, where);
            var dialog = new SplitRoomDialog { DataContext = viewModel };
            await dialog.ShowDialog(owner);
            return viewModel.Result!;
        });
    }

    /// <summary>What moved it?: which command moved a room that moves. Without a window, the suggestion.</summary>
    public Task<WhatMovedItChoice> ChooseWhatMovedItAsync(Room room, string comeOutIn, IReadOnlyList<DoneInRoom> done,
        int suggested, CancellationToken token = default)
    {
        return OnUiThreadAsync(async () =>
        {
            var owner = _getOwner();
            if (owner == null)
            {
                return suggested >= 0 && suggested < done.Count
                    ? new WhatMovedItChoice(done[suggested].Label)
                    : WhatMovedItChoice.Itself;
            }

            var viewModel = new WhatMovedItViewModel(room.Name, comeOutIn, done, suggested);
            var dialog = new WhatMovedItDialog { DataContext = viewModel };
            await dialog.ShowDialog(owner);
            return viewModel.Result!;
        });
    }

    /// <summary>Tidy Map...: asks which rooms and which way. Without an owner window, takes the suggestion.</summary>
    public Task<TidyChoice?> ChooseTidyAsync(TidyChoice suggested, int selectedRooms, CancellationToken token = default)
    {
        return OnUiThreadAsync(async () =>
        {
            var owner = _getOwner();
            if (owner == null)
            {
                return (TidyChoice?)suggested;
            }

            var dialog = new TidyMapDialog();
            dialog.Configure(suggested, selectedRooms);
            return await dialog.ShowDialog<TidyChoice?>(owner);
        });
    }

    /// <summary>
    ///     Runs the work on a background thread. If it takes more than a moment, shows a progress dialog whose Cancel
    ///     cancels it; the app stays responsive (repaints, the dialog moves) while it runs.
    /// </summary>
    public Task<T> RunWithProgressAsync<T>(
        string title, string message, int expectedMs, Func<CancellationToken, T> work, CancellationToken token = default)
    {
        return OnUiThreadAsync(async () =>
        {
            using var cancel = CancellationTokenSource.CreateLinkedTokenSource(token);
            var task = Task.Run(() => work(cancel.Token), CancellationToken.None);
            var owner = _getOwner();
            // quick work finishes without a dialog flashing up
            if (owner == null || await Task.WhenAny(task, Task.Delay(ProgressDelayMs)) == task)
            {
                return await task;
            }

            var dialog = new ProgressDialog();
            dialog.Configure(title, message, expectedMs);
            dialog.Cancelled += (_, _) => cancel.Cancel();
            var shown = dialog.ShowDialog(owner);
            T result;
            try
            {
                result = await task;
            }
            finally
            {
                if (!cancel.IsCancellationRequested)
                {
                    dialog.Finish();
                }

                await shown;
            }

            return cancel.IsCancellationRequested ? default! : result;
        });
    }

    /// <summary>How long work may take before the progress dialog appears.</summary>
    public const int ProgressDelayMs = 250;

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

    public Task<string?> PromptUrlAsync(string title, string label, CancellationToken token = default)
    {
        return OnUiThreadAsync(async () =>
        {
            var owner = _getOwner();
            if (owner == null)
            {
                return null;
            }

            var dialog = new OpenUrlDialog { Title = title };
            return await dialog.ShowDialog<string?>(owner);
        });
    }

    public Task<bool> ConfirmAsync(string message, string title, CancellationToken token = default)
    {
        return OnUiThreadAsync(async () =>
        {
            var service = _dialogService ?? App.Services?.GetService<IDialogService>();
            return service != null && await service.ShowConfirmationAsync(_getOwner(), title, message);
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
                ShowOverwritePrompt = true,
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
            if (file == null)
            {
                return null;
            }

            var chosen = file.Path.LocalPath;
            var guardService = _dialogService ?? App.Services?.GetService<IDialogService>();
            return await OverwriteGuard.ConfirmAsync(guardService, owner, chosen) ? chosen : null;
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
        DialogFocus.Prepare(dialog);
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
