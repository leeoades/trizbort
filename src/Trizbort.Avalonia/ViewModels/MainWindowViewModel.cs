using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Reflection;
using Trizbort.Automap;
using Trizbort.Editor;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Export;
using Trizbort.Export.Languages;
using AutomapEngine = Trizbort.Automap.Automap;

namespace Trizbort.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ICommandService _commandService;
    private readonly IStorageService _storageService;
    private readonly IDialogService _dialogService;
    private readonly IAutomapCanvas _automapCanvas;
    private static readonly string _appVersion = GetAppVersion();

    [ObservableProperty]
    private string _windowTitle = "Untitled - Trizbort";

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
    private UndoTransaction? _automapTransaction;
    private object? _currentRunToken;

    internal Task? CurrentAutomapTask => _automapTask;

    [ObservableProperty]
    private string _cursorPositionText = string.Empty;

    private Trizbort.Domain.Misc.Vector? _lastCursorPosition;

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
        _commandService.CanvasInvalidated += OnCanvasInvalidated;
        _commandService.PointerMoved += OnCanvasInvalidated;
        _commandService.DirtyChanged += OnDirtyChanged;

        if (_commandService.Session != null)
        {
            _commandService.Session.StatusMessagePosted += message => StatusText = message;
            // status text and failures come from the engine's events, not from polling it
            _commandService.Session.AutomapStatusChanged += OnAutomapStatusChanged;
            _commandService.Session.AutomapFailed += OnAutomapFailed;
        }

        UpdateState();
    }

    private void OnProjectChanged(object? sender, EventArgs e)
    {
        UpdateState();
    }

    private void OnDirtyChanged(object? sender, EventArgs e)
    {
        UpdateWindowTitle();
    }

    private static string GetAppVersion()
    {
        var assembly = typeof(MainWindowViewModel).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
               ?? assembly.GetName().Version?.ToString()
               ?? string.Empty;
    }

    private void UpdateWindowTitle()
    {
        var project = Project.Current;
        WindowTitle = TitleFormatter.Format(
            project.Name,
            project.FileName,
            project.IsDirty,
            ApplicationSettingsController.AppSettings.ShowFullPathInTitleBar,
            "Trizbort",
            _appVersion);
    }

    private void OnCanvasInvalidated(object? sender, EventArgs e)
    {
        // Only rebuild the readout when the map-space position actually changed.
        var position = _commandService.PointerMapPosition;
        if (position == _lastCursorPosition) return;

        _lastCursorPosition = position;
        // "+ 0.0" turns a rounded -0 into 0 so the readout never shows "-0".
        CursorPositionText = position is { } p
            ? $"X: {Math.Round(p.X) + 0.0:0}, Y: {Math.Round(p.Y) + 0.0:0}"
            : string.Empty;
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
        UpdateWindowTitle();

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

    /// <summary>Opens a map from the File &gt; Recent Maps menu (prompts about unsaved changes first).</summary>
    public async Task OpenRecentMapAsync(string path)
    {
        var service = new RecentFilesService(_storageService, _dialogService);
        if (!await service.OpenRecentAsync(path, GetActiveWindow(), CanLoseProjectAsync)) return;

        if (IsAutomapActive)
        {
            StopAutomap();
        }
        UpdateState();
        _commandService.ResetZoom();
        StatusText = $"Opened map: {Path.GetFileName(path)}";
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

    private static bool _emfNoteShown;

    [RelayCommand]
    public async Task ExportImageAsync()
    {
        var success = await _storageService.ExportImageAsync(GetActiveTopLevel());
        if (!success) return;

        var format = ImageFormats.FromExtension(
            Path.GetExtension(ApplicationSettingsController.AppSettings.LastExportImageFileName), ImageFormat.Png);
        StatusText = $"Exported map image ({ImageFormats.DisplayName(format)})";
        if (ApplicationSettingsController.AppSettings.DefaultImageType == ImageFormats.EmfSettingValue && !_emfNoteShown)
        {
            _emfNoteShown = true;
            StatusText += ". EMF is not supported on this platform; PNG is the default instead.";
        }
    }

    [RelayCommand]
    public async Task ExportPdfAsync()
    {
        var success = await _storageService.ExportPdfAsync(GetActiveTopLevel());
        if (success) StatusText = "Exported PDF";
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

        AutomapEngine.Instance.Stop();
        EndAutomapTransaction();
        IsAutomapActive = false;
        AutomapStatusText = "Automap is not running.";
    }

    // One undo step for the whole automap run (plan section 7).
    private void EndAutomapTransaction()
    {
        var transaction = _automapTransaction;
        _automapTransaction = null;
        transaction?.Dispose();
    }

    public Task StartAutomapAsync(AutomapSettings settings)
    {
        StopAutomap();

        var runToken = new object();
        _currentRunToken = runToken;

        IsAutomapActive = true;
        AutomapStatusText = "Automapping has started.";

        _automapTransaction = _commandService.Session.BeginTransaction("Automap");
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
        finally
        {
            if (ReferenceEquals(_currentRunToken, runToken))
            {
                EndAutomapTransaction();
                AutomapStatusText = AutomapEngine.Instance.Status;
                IsAutomapActive = false;
                UpdateState();
                _commandService.InvalidateCanvas();
            }
        }
    }

    private static void RunOnUiThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
        }
        else
        {
            Dispatcher.UIThread.Post(action);
        }
    }

    private void OnAutomapStatusChanged(string status)
    {
        RunOnUiThread(() => AutomapStatusText = status);
    }

    // The engine has already reported the error through IEditorPrompts; here the UI returns to "not running".
    private void OnAutomapFailed(Exception exception)
    {
        RunOnUiThread(() =>
        {
            if (!IsAutomapActive)
            {
                return;
            }

            EndAutomapTransaction();
            AutomapStatusText = AutomapEngine.Instance.Status;
            IsAutomapActive = false;
            UpdateState();
            _commandService.InvalidateCanvas();
        });
    }

    // Menu items such as Start/Stop Automap depend on the running state; ask the menus to re-evaluate.
    partial void OnIsAutomapActiveChanged(bool value)
    {
        _commandService.Session?.Invalidate(EditorChange.Adorners);
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
            UpdateWindowTitle();
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
