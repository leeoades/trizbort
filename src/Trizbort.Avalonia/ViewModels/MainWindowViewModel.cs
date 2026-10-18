using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trizbort.Automap;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Export.Languages;
using AutomapEngine = Trizbort.Automap.Automap;

namespace Trizbort.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ICommandService _commandService;
    private readonly IStorageService _storageService;
    private readonly IDialogService _dialogService;
    private readonly IAutomapCanvas _automapCanvas;

    [ObservableProperty]
    private string _windowTitle = "Trizbort - Untitled";

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _elementCount = "Rooms: 0 | Connections: 0";

    [ObservableProperty]
    private string _zoomText = "100%";

    [ObservableProperty]
    private double _zoomFactor = 1.0;

    [ObservableProperty]
    private bool _isAutomapActive;

    [ObservableProperty]
    private string _automapStatusText = "Automap is not running.";

    private Task? _automapTask;
    private DispatcherTimer? _automapStatusTimer;
    private object? _currentRunToken;

    internal Task? CurrentAutomapTask => _automapTask;

    [ObservableProperty]
    private string _cursorPositionText = "X: 0, Y: 0";

    [ObservableProperty]
    private string _activeTool = "Select";

    public ICommandService CommandService => _commandService;

    public MainWindowViewModel(
        ICommandService commandService,
        IStorageService storageService,
        IDialogService dialogService,
        IAutomapCanvas automapCanvas)
    {
        _commandService = commandService;
        _storageService = storageService;
        _dialogService = dialogService;
        _automapCanvas = automapCanvas;

        _commandService.ProjectChanged += OnProjectChanged;
        _commandService.SelectionChanged += OnSelectionChanged;
        _commandService.ZoomChanged += OnZoomChanged;

        UpdateState();
    }

    private void OnProjectChanged(object? sender, EventArgs e)
    {
        UpdateState();
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        UpdateSelectionState();
    }

    private void OnZoomChanged(object? sender, float zoom)
    {
        ZoomFactor = zoom;
        ZoomText = $"{(int)Math.Round(zoom * 100)}%";
    }

    partial void OnZoomFactorChanged(double value)
    {
        if (Math.Abs(_commandService.ZoomFactor - (float)value) > 0.001f)
        {
            _commandService.ZoomFactor = (float)value;
        }
    }

    public void UpdateState()
    {
        var project = Project.Current;
        var name = string.IsNullOrEmpty(project.FileName)
            ? (string.IsNullOrEmpty(project.Title) ? "Untitled" : project.Title)
            : Path.GetFileNameWithoutExtension(project.FileName);

        var dirtyMarker = project.IsDirty ? " *" : "";
        WindowTitle = $"Trizbort - {name}{dirtyMarker}";

        var roomCount = project.Elements.OfType<Room>().Count();
        var connectionCount = project.Elements.OfType<Connection>().Count();
        var labelCount = project.Elements.OfType<MapLabel>().Count();

        ElementCount = $"Rooms: {roomCount} | Connections: {connectionCount}" + (labelCount > 0 ? $" | Labels: {labelCount}" : "");
    }

    private void UpdateSelectionState()
    {
        var selected = Project.Current.GetSelectedElements();
        if (selected.Count == 0 && Project.Current.ActiveSelectedElement != null)
        {
            selected.Add(Project.Current.ActiveSelectedElement);
        }

        if (selected.Count == 1)
        {
            var el = selected[0];
            StatusText = $"Selected: {el.GetType().Name} '{el.Name}'";
        }
        else if (selected.Count > 1)
        {
            StatusText = $"Selected {selected.Count} elements";
        }
        else
        {
            StatusText = "Ready";
        }
    }

    private static Window? GetActiveWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    private static TopLevel? GetActiveTopLevel() => GetActiveWindow();

    public async Task<bool> CanLoseProjectAsync()
    {
        if (!Project.Current.IsDirty) return true;

        var result = await _dialogService.ShowSaveDiscardCancelAsync(
            GetActiveWindow(),
            "Trizbort",
            "Save changes to current map before continuing?");

        return result switch
        {
            ConfirmationResult.Save => await _storageService.SaveMapAsync(GetActiveTopLevel(), false),
            ConfirmationResult.Discard => true,
            ConfirmationResult.Cancel => false,
            _ => false
        };
    }

    [RelayCommand]
    public async Task NewMapAsync()
    {
        if (!await CanLoseProjectAsync()) return;
        if (IsAutomapActive)
        {
            StopAutomap();
        }
        _commandService.NewMap();
        UpdateState();
        StatusText = "New map created";
    }

    [RelayCommand]
    public async Task OpenMapAsync()
    {
        if (!await CanLoseProjectAsync()) return;
        var success = await _storageService.OpenMapAsync(GetActiveTopLevel());
        if (success)
        {
            if (IsAutomapActive)
            {
                StopAutomap();
            }
            UpdateState();
            _commandService.ResetZoom();
            StatusText = $"Opened map: {Path.GetFileName(Project.Current.FileName)}";
        }
    }

    [RelayCommand]
    public async Task SaveMapAsync()
    {
        var success = await _storageService.SaveMapAsync(GetActiveTopLevel(), false);
        if (success)
        {
            UpdateState();
            StatusText = $"Map saved: {Path.GetFileName(Project.Current.FileName)}";
        }
    }

    [RelayCommand]
    public async Task SaveMapAsAsync()
    {
        var success = await _storageService.SaveMapAsync(GetActiveTopLevel(), true);
        if (success)
        {
            UpdateState();
            StatusText = $"Map saved as: {Path.GetFileName(Project.Current.FileName)}";
        }
    }

    [RelayCommand]
    public async Task ExportImageAsync()
    {
        var success = await _storageService.ExportImageAsync(GetActiveTopLevel());
        if (success) StatusText = "Exported map image (PNG)";
    }

    [RelayCommand]
    public async Task ExportInform7Async()
    {
        var success = await _storageService.ExportCodeAsync(GetActiveTopLevel(), new Inform7Exporter());
        if (success) StatusText = "Exported Inform 7 source code";
    }

    [RelayCommand]
    public async Task ExportInform6Async()
    {
        var success = await _storageService.ExportCodeAsync(GetActiveTopLevel(), new Inform6Exporter());
        if (success) StatusText = "Exported Inform 6 source code";
    }

    [RelayCommand]
    public async Task ExportTadsAsync()
    {
        var success = await _storageService.ExportCodeAsync(GetActiveTopLevel(), new TadsExporter());
        if (success) StatusText = "Exported TADS source code";
    }

    [RelayCommand]
    public async Task ExportHugoAsync()
    {
        var success = await _storageService.ExportCodeAsync(GetActiveTopLevel(), new HugoExporter());
        if (success) StatusText = "Exported Hugo source code";
    }

    [RelayCommand]
    public async Task ExportZilAsync()
    {
        var success = await _storageService.ExportCodeAsync(GetActiveTopLevel(), new ZilExporter());
        if (success) StatusText = "Exported ZIL source code";
    }

    [RelayCommand]
    public async Task ExportQuestAsync()
    {
        var success = await _storageService.ExportCodeAsync(GetActiveTopLevel(), new QuestExporter());
        if (success) StatusText = "Exported Quest source code";
    }

    [RelayCommand]
    public async Task ExportAlanAsync()
    {
        var success = await _storageService.ExportCodeAsync(GetActiveTopLevel(), new AlanExporter());
        if (success) StatusText = "Exported Alan source code";
    }

    [RelayCommand]
    public async Task ExportAdventuronAsync()
    {
        var success = await _storageService.ExportCodeAsync(GetActiveTopLevel(), new AdventuronExporter());
        if (success) StatusText = "Exported Adventuron source code";
    }

    [RelayCommand]
    public void Exit()
    {
        GetActiveWindow()?.Close();
    }

    [RelayCommand]
    public void SelectAll()
    {
        _commandService.SelectAll();
        var count = Project.Current.GetSelectedElements().Count;
        StatusText = count > 0 ? $"Selected all {count} elements" : "No elements to select";
    }

    [RelayCommand]
    public void DeleteSelected()
    {
        var count = _commandService.DeleteSelected();
        if (count > 0)
        {
            UpdateState();
            StatusText = $"Deleted {count} element(s)";
        }
        else
        {
            StatusText = "Nothing selected to delete";
        }
    }

    [RelayCommand]
    public async Task CopyAsync()
    {
        var text = _commandService.CopySelected();
        if (!string.IsNullOrEmpty(text))
        {
            var clipboard = GetActiveTopLevel()?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(text);
                StatusText = "Copied selected element(s) to clipboard";
            }
        }
    }

    [RelayCommand]
    public async Task CutAsync()
    {
        var text = _commandService.CutSelected();
        if (!string.IsNullOrEmpty(text))
        {
            var clipboard = GetActiveTopLevel()?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(text);
                UpdateState();
                StatusText = "Cut selected element(s) to clipboard";
            }
        }
    }

    [RelayCommand]
    public async Task PasteAsync()
    {
        var clipboard = GetActiveTopLevel()?.Clipboard;
        if (clipboard != null)
        {
            var text = await clipboard.TryGetTextAsync();
            if (!string.IsNullOrEmpty(text))
            {
                _commandService.Paste(text);
                UpdateState();
                StatusText = "Pasted element(s) from clipboard";
            }
        }
    }

    [RelayCommand]
    public void AddRoom()
    {
        var room = _commandService.AddRoom();
        UpdateState();
        StatusText = $"Added room '{room.Name}'";
    }

    [RelayCommand]
    public void AddMapLabel()
    {
        var label = _commandService.AddMapLabel();
        UpdateState();
        StatusText = $"Added label '{label.Name}'";
    }

    [RelayCommand]
    public void ZoomIn() => _commandService.ZoomIn();

    [RelayCommand]
    public void ZoomOut() => _commandService.ZoomOut();

    [RelayCommand]
    public void ResetZoom() => _commandService.ResetZoom();

    [RelayCommand]
    public void FitToMap() => _commandService.FitToMap();

    [RelayCommand]
    public void ToggleGrid() => _commandService.ToggleGrid();

    [RelayCommand]
    public async Task ToggleAutomapAsync()
    {
        if (IsAutomapActive)
        {
            StopAutomap();
            return;
        }

        var settings = await _dialogService.ShowAutomapSettingsAsync(GetActiveWindow());
        if (settings == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.Value.FileName))
        {
            await _dialogService.ShowMessageAsync(
                GetActiveWindow(),
                "Automap Error",
                "Please specify a transcript file to start automapping.");
            return;
        }

        await StartAutomapAsync(settings.Value);
    }

    [RelayCommand]
    public void StopAutomap()
    {
        _currentRunToken = null;
        _automapStatusTimer?.Stop();
        _automapStatusTimer = null;

        AutomapEngine.Instance.Stop();
        IsAutomapActive = false;
        AutomapStatusText = "Automap is not running.";
    }

    public Task StartAutomapAsync(AutomapSettings settings)
    {
        StopAutomap();

        var runToken = new object();
        _currentRunToken = runToken;

        IsAutomapActive = true;
        AutomapStatusText = "Automapping has started.";

        _automapStatusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _automapStatusTimer.Tick += (_, _) =>
        {
            if (IsAutomapActive && ReferenceEquals(_currentRunToken, runToken))
            {
                AutomapStatusText = AutomapEngine.Instance.Status;
            }
        };
        _automapStatusTimer.Start();

        _automapTask = RunAutomapLoopAsync(settings, runToken);
        return Task.CompletedTask;
    }

    private async Task RunAutomapLoopAsync(AutomapSettings settings, object runToken)
    {
        try
        {
            await AutomapEngine.Instance.Start(_automapCanvas, settings);
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation/stop
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync(
                GetActiveWindow(),
                "Automap Error",
                $"An unexpected error occurred during automapping:\n{ex.Message}");
        }
        finally
        {
            if (ReferenceEquals(_currentRunToken, runToken))
            {
                AutomapStatusText = AutomapEngine.Instance.Status;
                IsAutomapActive = false;
                _automapStatusTimer?.Stop();
                _automapStatusTimer = null;
                UpdateState();
                _commandService.InvalidateCanvas();
            }
        }
    }

    [RelayCommand]
    public async Task ShowAboutAsync()
    {
        await _dialogService.ShowAboutDialogAsync(GetActiveWindow());
    }

    [RelayCommand]
    public async Task ShowMapSettingsAsync()
    {
        var changed = await _dialogService.ShowMapSettingsAsync(GetActiveWindow());
        if (changed)
        {
            UpdateState();
            _commandService.InvalidateCanvas();
            StatusText = "Updated map settings";
        }
    }

    [RelayCommand]
    public async Task ShowMapThemeAsync()
    {
        var changed = await _dialogService.ShowMapThemeAsync(GetActiveWindow());
        if (changed)
        {
            UpdateState();
            _commandService.InvalidateCanvas();
            StatusText = "Applied map theme";
        }
    }

    [RelayCommand]
    public async Task ShowAppSettingsAsync()
    {
        var changed = await _dialogService.ShowAppSettingsAsync(GetActiveWindow());
        if (changed)
        {
            _commandService.InvalidateCanvas();
            StatusText = "Updated application settings";
        }
    }

    [RelayCommand]
    public async Task ShowSelectedElementPropertiesAsync()
    {
        var selected = Project.Current.GetSelectedElements();
        var element = selected.Count == 1 ? selected[0] : Project.Current.ActiveSelectedElement;
        if (element != null)
        {
            var changed = await _dialogService.ShowElementPropertiesAsync(GetActiveWindow(), element);
            if (changed)
            {
                UpdateState();
                _commandService.InvalidateCanvas();
                StatusText = $"Updated properties for {element.Name}";
            }
        }
    }
}
