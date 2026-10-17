using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trizbort.Automap;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Export.Languages;

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

    [RelayCommand]
    public void NewMap()
    {
        _commandService.NewMap();
        UpdateState();
        StatusText = "New map created";
    }

    [RelayCommand]
    public async Task OpenMapAsync()
    {
        var success = await _storageService.OpenMapAsync(GetActiveTopLevel());
        if (success)
        {
            UpdateState();
            _commandService.ResetZoom();
            StatusText = $"Opened map: {Project.Current.FileName}";
        }
    }

    [RelayCommand]
    public async Task SaveMapAsync()
    {
        var success = await _storageService.SaveMapAsync(GetActiveTopLevel(), false);
        if (success)
        {
            UpdateState();
            StatusText = $"Map saved: {Project.Current.FileName}";
        }
    }

    [RelayCommand]
    public async Task SaveMapAsAsync()
    {
        var success = await _storageService.SaveMapAsync(GetActiveTopLevel(), true);
        if (success)
        {
            UpdateState();
            StatusText = $"Map saved as: {Project.Current.FileName}";
        }
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
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    [RelayCommand]
    public void SelectAll()
    {
        _commandService.SelectAll();
        StatusText = "Selected all elements";
    }

    [RelayCommand]
    public void DeleteSelected()
    {
        _commandService.DeleteSelected();
        UpdateState();
        StatusText = "Deleted selected elements";
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
    public void ToggleAutomap()
    {
        IsAutomapActive = !IsAutomapActive;
        StatusText = IsAutomapActive ? "Automapping active" : "Automapping stopped";
    }

    [RelayCommand]
    public async Task ShowAboutAsync()
    {
        await _dialogService.ShowAboutDialogAsync(GetActiveWindow());
    }

    [RelayCommand]
    public async Task ShowMapSettingsAsync()
    {
        await _dialogService.ShowMapSettingsAsync(GetActiveWindow());
    }

    [RelayCommand]
    public async Task ShowMapThemeAsync()
    {
        await _dialogService.ShowMapThemeAsync(GetActiveWindow());
    }

    [RelayCommand]
    public async Task ShowAppSettingsAsync()
    {
        await _dialogService.ShowAppSettingsAsync(GetActiveWindow());
    }
}
