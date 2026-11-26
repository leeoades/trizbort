using Trizbort.Setup;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Trizbort.Automap;
using Trizbort.Avalonia.Drawing;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views;
using Trizbort.Avalonia.Views.Dialogs;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Elements;

namespace Trizbort.Avalonia.Services;

public class DialogService(IServiceScopeFactory serviceScopeFactory, ICommandService commandService) : IDialogService
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly ICommandService _commandService = commandService;

    /// <summary>The label dialogs open, by label: they're shown beside the map, so one could be opened twice.</summary>
    private readonly Dictionary<MapLabel, MapLabelPropertiesDialog> _openLabelDialogs = new();

    public async Task ShowAboutDialogAsync(Window? owner)
    {
        var dialog = new AboutDialog();
        if (owner != null)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            dialog.Show();
        }
    }

    public async Task ShowMessageAsync(Window? owner, string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 180,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false
        };

        var panel = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16
        };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap
        });

        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Right,
            Width = 80,
            IsDefault = true,
            IsCancel = true
        };
        okButton.Click += (_, _) => dialog.Close();
        panel.Children.Add(okButton);

        dialog.Content = panel;
        DialogFocus.Prepare(dialog);

        if (owner != null)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            dialog.Show();
        }
    }

    public async Task<bool> ShowConfirmationAsync(Window? owner, string title, string message)
    {
        var result = false;
        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 180,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false
        };

        var grid = new Grid
        {
            Margin = new Thickness(20),
            RowDefinitions = new RowDefinitions("*,Auto")
        };

        grid.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10
        };
        Grid.SetRow(buttonPanel, 1);

        var okButton = new Button
        {
            Content = "Yes",
            Width = 75,
            IsDefault = true
        };
        okButton.Click += (_, _) =>
        {
            result = true;
            dialog.Close();
        };

        var cancelButton = new Button
        {
            Content = "No",
            Width = 75,
            IsCancel = true
        };
        cancelButton.Click += (_, _) =>
        {
            result = false;
            dialog.Close();
        };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        grid.Children.Add(buttonPanel);

        dialog.Content = grid;
        DialogFocus.Prepare(dialog);

        if (owner != null)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            dialog.Show();
        }

        return result;
    }

    public async Task<ConfirmationResult> ShowSaveDiscardCancelAsync(Window? owner, string title, string message)
    {
        var result = ConfirmationResult.Cancel;
        var dialog = new Window
        {
            Title = title,
            Width = 440,
            Height = 160,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false
        };

        var grid = new Grid
        {
            Margin = new Thickness(20),
            RowDefinitions = new RowDefinitions("*,Auto")
        };

        grid.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        Grid.SetRow(buttonPanel, 1);

        var saveButton = new Button { Content = "Save", Width = 80, IsDefault = true };
        saveButton.Click += (_, _) =>
        {
            result = ConfirmationResult.Save;
            dialog.Close();
        };

        var discardButton = new Button { Content = "Don't Save", Width = 95 };
        discardButton.Click += (_, _) =>
        {
            result = ConfirmationResult.Discard;
            dialog.Close();
        };

        var cancelButton = new Button { Content = "Cancel", Width = 80, IsCancel = true };
        cancelButton.Click += (_, _) =>
        {
            result = ConfirmationResult.Cancel;
            dialog.Close();
        };

        buttonPanel.Children.Add(saveButton);
        buttonPanel.Children.Add(discardButton);
        buttonPanel.Children.Add(cancelButton);
        grid.Children.Add(buttonPanel);

        dialog.Content = grid;
        DialogFocus.Prepare(dialog);

        if (owner != null)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            dialog.Show();
        }

        return result;
    }

    public async Task<RecoveryChoice> ShowRecoverDiscardLaterAsync(Window? owner, string title, string message)
    {
        var result = RecoveryChoice.DecideLater;
        var dialog = new Window
        {
            Title = title,
            Width = 480,
            Height = 190,
            CanResize = false,
            WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
            ShowInTaskbar = false
        };

        var grid = new Grid
        {
            Margin = new Thickness(20),
            RowDefinitions = new RowDefinitions("*,Auto")
        };

        grid.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        Grid.SetRow(buttonPanel, 1);

        var recoverButton = new Button { Content = "_Open", MinWidth = 90, IsDefault = true };
        recoverButton.Click += (_, _) =>
        {
            result = RecoveryChoice.Recover;
            dialog.Close();
        };

        var discardButton = new Button { Content = "_Delete", MinWidth = 90 };
        discardButton.Click += (_, _) =>
        {
            result = RecoveryChoice.Discard;
            dialog.Close();
        };

        var laterButton = new Button { Content = "_Ask Me Next Time", MinWidth = 110, IsCancel = true };
        laterButton.Click += (_, _) =>
        {
            result = RecoveryChoice.DecideLater;
            dialog.Close();
        };

        buttonPanel.Children.Add(recoverButton);
        buttonPanel.Children.Add(discardButton);
        buttonPanel.Children.Add(laterButton);
        grid.Children.Add(buttonPanel);

        dialog.Content = grid;
        DialogFocus.Prepare(dialog);

        if (owner != null)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            dialog.Show();
        }

        return result;
    }

    public Task<bool> ShowRoomPropertiesAsync(Window? owner, Room room)
    {
        return ShowRoomPropertiesAsync(owner, room, null);
    }

    /// <summary>Shows the room dialog; <paramref name="startTab" /> (e.g. the Region tab) overrides the remembered tab.</summary>
    public async Task<bool> ShowRoomPropertiesAsync(Window? owner, Room room, int? startTab)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<RoomPropertiesViewModel>();
        viewModel.Initialize(room, startTab);

        var dialog = new RoomPropertiesDialog { DataContext = viewModel };
        dialog.Prompts = new DialogConfirmPrompts(this, dialog);
        viewModel.Regions.AddRegion = () => ShowNewRegionAsync(dialog);
        var result = owner != null ? await dialog.ShowDialog<bool>(owner) : false;
        if (result)
        {
            // a region made with Add New Region... is kept in the same undo step
            _commandService.Session.Apply("Edit room properties", room, () =>
            {
                viewModel.Regions.Keep();
                viewModel.ApplyTo(room);
            });
        }
        return result;
    }

    /// <summary>Routes the confirmation prompt of a dialog to a modal yes/no box owned by that dialog.</summary>
    private sealed class DialogConfirmPrompts(IDialogService dialogService, Window dialog) : Trizbort.Editor.IEditorPrompts
    {
        public Task<Trizbort.Editor.RoomDisambiguation> DisambiguateRoomAsync(string transcriptLine, string roomDescription, IReadOnlyList<Room> candidates, CancellationToken token = default)
            => Trizbort.Editor.NonInteractiveEditorPrompts.Instance.DisambiguateRoomAsync(transcriptLine, roomDescription, candidates, token);

        public Task<bool> ConfirmAsync(string message, string title, CancellationToken token = default)
            => dialogService.ShowConfirmationAsync(dialog, title, message);
    }

    public async Task<bool> ShowConnectionPropertiesAsync(Window? owner, Connection connection)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<ConnectionPropertiesViewModel>();
        viewModel.Initialize(connection);

        var dialog = new ConnectionPropertiesDialog { DataContext = viewModel };
        var result = owner != null ? await dialog.ShowDialog<bool>(owner) : false;
        if (result)
        {
            _commandService.Session.Apply("Edit connection properties", connection, () => viewModel.ApplyTo(connection));
        }
        return result;
    }

    public async Task<bool> ShowMapLabelPropertiesAsync(Window? owner, MapLabel label)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<MapLabelPropertiesViewModel>();
        viewModel.Initialize(label);

        // already open for this label: brought to the front, rather than a second one
        if (_openLabelDialogs.TryGetValue(label, out var open))
        {
            open.Activate();
            return false;
        }

        if (owner == null) return false;
        var dialog = new MapLabelPropertiesDialog { DataContext = viewModel };
        viewModel.EditStyles = (id, newFrom) => ShowLabelStylesAsync(dialog, id, newFrom);

        // Shown beside the map rather than over it, so a room clicked on the map can be chosen for a link (Lee,
        // 22 Nov 2026); it stays in front of the main window, as its owner.
        void OnSelectionChanged(object? sender, EventArgs e)
        {
            if (_commandService.SelectedElements.Count == 1 && _commandService.SelectedElements.First() is Room room)
                viewModel.ChooseRoomOnMap(room);
        }

        // The label on the map shows the text as it's typed, and a Markdown label grows (or shrinks back, as far as the
        // size it had) to fit it (Lee, 22 Nov 2026); put back as it was before OK applies it as one undo step, or on
        // Cancel.
        var (wasText, wasMarkdown, wasSize) = (label.Text, label.Markdown, label.Size);

        void ShowAsTyped()
        {
            label.Text = viewModel.Text ?? string.Empty;
            label.Markdown = viewModel.Markdown;
            label.Size = MapLabelDrawingAdapter.FitText(label, wasSize);
            _commandService.InvalidateCanvas();
        }

        void OnTyped(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(MapLabelPropertiesViewModel.Text) or nameof(MapLabelPropertiesViewModel.Markdown))
                ShowAsTyped();
        }

        var closed = new TaskCompletionSource<bool>();
        dialog.Closed += (_, _) => closed.TrySetResult(dialog.Accepted);
        _openLabelDialogs[label] = dialog;
        _commandService.SelectionChanged += OnSelectionChanged;
        viewModel.PropertyChanged += OnTyped;
        bool result;
        try
        {
            dialog.Show(owner);
            result = await closed.Task;
        }
        finally
        {
            _commandService.SelectionChanged -= OnSelectionChanged;
            viewModel.PropertyChanged -= OnTyped;
            _openLabelDialogs.Remove(label);
            (label.Text, label.Markdown, label.Size) = (wasText, wasMarkdown, wasSize);
            _commandService.InvalidateCanvas();
        }

        // the label taken off the map while its dialog was open: nothing to change
        if (result && Project.Current.Elements.Contains(label))
        {
            _commandService.Session.Apply("Edit label properties", label, () =>
            {
                viewModel.ApplyTo(label);
                // new text fits as typed; a new look alone keeps the label's width, and its lines
                var sameText = label.Text == wasText && label.Markdown == wasMarkdown;
                label.Size = MapLabelDrawingAdapter.FitText(label, wasSize, keepWidth: sameText);
            });
        }
        return result;
    }

    public async Task<bool> ShowRoomsPropertiesAsync(Window? owner, IReadOnlyList<Room> rooms)
    {
        var viewModel = new RoomsPropertiesViewModel();
        viewModel.Initialize(rooms);
        var dialog = new RoomsPropertiesDialog { DataContext = viewModel };
        viewModel.Regions.AddRegion = () => ShowNewRegionAsync(dialog);
        var result = owner != null && await dialog.ShowDialog<bool>(owner);
        if (result)
        {
            var edit = viewModel.ToEdit();
            _commandService.Session.Execute("Edit room properties", () =>
            {
                viewModel.Regions.Keep();
                edit.ApplyTo(rooms);
            }, rooms);
        }
        return result;
    }

    /// <summary>A background's colour, border, shape and lines; OK applies them as one undo step.</summary>
    public async Task<bool> ShowBackgroundPropertiesAsync(Window? owner, Trizbort.Domain.Elements.Background background)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<BackgroundPropertiesViewModel>();
        viewModel.Initialize(background);

        var dialog = new BackgroundPropertiesDialog { DataContext = viewModel };
        var result = owner != null && await dialog.ShowDialog<bool>(owner);
        if (result)
        {
            _commandService.Session.Apply("Edit background properties", background, () => viewModel.ApplyTo(background));
        }
        return result;
    }

    public Task<bool> ShowElementPropertiesAsync(Window? owner, Element element)
    {
        return element switch
        {
            Room room => ShowRoomPropertiesAsync(owner, room),
            Connection connection => ShowConnectionPropertiesAsync(owner, connection),
            MapLabel label => ShowMapLabelPropertiesAsync(owner, label),
            Trizbort.Domain.Elements.Background background => ShowBackgroundPropertiesAsync(owner, background),
            _ => Task.FromResult(false)
        };
    }

    public async Task<bool> ShowMapSettingsAsync(Window? owner)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<MapSettingsViewModel>();
        viewModel.Initialize();

        var dialog = new MapSettingsDialog { DataContext = viewModel };
        var result = owner != null ? await dialog.ShowDialog<bool>(owner) : false;
        if (result)
        {
            _commandService.Session.Execute("Edit map settings", viewModel.ApplyToSettings);
        }
        return result;
    }

    /// <summary>
    ///   Map Settings at the Styles tab, at label style <paramref name="styleId" /> (or at a new style based on it with
    ///   the look <paramref name="newFrom" />); OK applies it as one undo step and gives the style then selected.
    /// </summary>
    public async Task<string?> ShowLabelStylesAsync(Window owner, string styleId, LabelStyle? newFrom = null)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<MapSettingsViewModel>();
        viewModel.Initialize();
        viewModel.SelectedTabIndex = MapSettingsViewModel.StylesTab;
        viewModel.LabelStyles.Initialize(styleId, newFrom);

        var dialog = new MapSettingsDialog { DataContext = viewModel };
        if (!await dialog.ShowDialog<bool>(owner)) return null;
        var selected = viewModel.LabelStyles.SelectedId;
        _commandService.Session.Execute("Edit map settings", viewModel.ApplyToSettings);
        _commandService.InvalidateCanvas();
        return selected ?? styleId;
    }

    public async Task<Trizbort.Editor.NewRegionChoice?> ShowNewRegionAsync(Window owner)
    {
        var scope = _serviceScopeFactory.CreateScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<MapSettingsViewModel>();
        viewModel.Initialize();
        viewModel.SelectedTabIndex = MapSettingsViewModel.RegionsTab;
        viewModel.AddRegion();
        var added = viewModel.SelectedRegion;

        var dialog = new MapSettingsDialog { DataContext = viewModel };
        // the new region's name, ready to type over
        dialog.Opened += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            var box = dialog.FindControl<TextBox>("RegionNameBox");
            box?.Focus();
            box?.SelectAll();
        }, DispatcherPriority.Loaded);

        if (!await dialog.ShowDialog<bool>(owner))
        {
            scope.Dispose();
            return null;
        }

        // still there (it could have been deleted again): its name as finally typed
        var name = added != null && viewModel.Regions.Contains(added) ? added.Name : null;
        return new Trizbort.Editor.NewRegionChoice(name, () =>
        {
            viewModel.ApplyToSettings();
            scope.Dispose();
        });
    }

    public async Task<bool> ShowMapThemeAsync(Window? owner)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<MapThemeViewModel>();
        viewModel.Initialize();

        var dialog = new MapThemeDialog
        {
            DataContext = viewModel,
            ApplyInference = inference => _commandService.Session.ApplyRoomStyleInference(inference)
        };
        var result = owner != null ? await dialog.ShowDialog<bool>(owner) : false;
        if (result && viewModel.SelectedTheme != null)
        {
            _commandService.Session.ApplyTheme(viewModel.SelectedTheme, viewModel.ReplaceIndividualStyles);
        }
        return result;
    }

    public async Task<bool> ShowAppSettingsAsync(Window? owner)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<AppSettingsViewModel>();
        viewModel.Initialize();

        var dialog = new AppSettingsDialog { DataContext = viewModel };
        var result = owner != null ? await dialog.ShowDialog<bool>(owner) : false;
        if (result)
        {
            viewModel.ApplyToSettings();
        }
        return result;
    }

    public async Task<AutomapSettings?> ShowAutomapSettingsAsync(Window? owner)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<AutomapSettingsViewModel>();
        var currentSettings = ApplicationSettingsController.AppSettings.Automap;
        viewModel.Initialize(currentSettings);

        var dialog = new AutomapSettingsDialog { DataContext = viewModel };
        var result = owner != null ? await dialog.ShowDialog<bool>(owner) : false;
        if (result)
        {
            var settings = viewModel.GetSettings();
            if (!string.IsNullOrWhiteSpace(settings.FileName))
            {
                ApplicationSettingsController.AppSettings.Automap = settings;
                ApplicationSettingsController.SaveSettings();
            }
            return settings;
        }
        return null;
    }

    public async Task<AutomapSettings?> ShowAutomapRunSettingsAsync(Window? owner, AutomapSettings current)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<AutomapSettingsViewModel>();
        viewModel.Initialize(current);
        viewModel.WhileRunning = true;

        var dialog = new AutomapSettingsDialog { DataContext = viewModel };
        var result = owner != null && await dialog.ShowDialog<bool>(owner);
        if (!result) return null;

        // only what can change part way through; the transcript and how the run started stay
        var settings = viewModel.GetSettings();
        settings.FileName = current.FileName;
        settings.ContinueTranscript = current.ContinueTranscript;
        settings.MoveByMove = current.MoveByMove;
        settings.SingleStep = current.SingleStep;
        var saved = ApplicationSettingsController.AppSettings.Automap;
        saved.VerboseTranscript = settings.VerboseTranscript;
        saved.GuessExits = settings.GuessExits;
        saved.AutoTidy = settings.AutoTidy;
        saved.BestGuessWhichRoom = settings.BestGuessWhichRoom;
        saved.AbbreviateDirections = settings.AbbreviateDirections;
        saved.AssumeTwoWayConnections = settings.AssumeTwoWayConnections;
        saved.AddObjectCommand = settings.AddObjectCommand;
        saved.AddRegionCommand = settings.AddRegionCommand;
        ApplicationSettingsController.AppSettings.Automap = saved;
        ApplicationSettingsController.SaveSettings();
        return settings;
    }
}
