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
    [NotifyPropertyChangedFor(nameof(IsGameMonitorVisible))]
    private bool _isAutomapActive;

    /// <summary>Whether the game monitor is wanted (View > Automap Game Monitor, or the automap bar).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGameMonitorVisible))]
    private bool _isGameMonitorShown = true;

    /// <summary>The game monitor shows while automapping, unless it has been hidden.</summary>
    public bool IsGameMonitorVisible => IsAutomapActive && IsGameMonitorShown;

    [ObservableProperty]
    private string _automapStatusText = "Automap is not running.";

    private Task? _automapTask;
    private object? _currentRunToken;

    /// <summary>True while automap plays the transcript a move at a time; false while it plays freely.</summary>
    [ObservableProperty]
    private bool _isAutomapMoveByMove;

    /// <summary>
    ///     The automap panel's Tidy the map as I go: automap rearranges the rooms near the player after each move.
    ///     Kept in the automap settings, so the next run starts the same way.
    /// </summary>
    [ObservableProperty]
    private bool _isAutomapAutoTidy = ApplicationSettingsController.AppSettings.Automap.AutoTidy;

    partial void OnIsAutomapAutoTidyChanged(bool value)
    {
        AutomapEngine.Instance.AutoTidy = value;
        var settings = ApplicationSettingsController.AppSettings.Automap;
        if (settings.AutoTidy == value) return;
        settings.AutoTidy = value;
        ApplicationSettingsController.AppSettings.Automap = settings;
    }

    /// <summary>True while mapping is suspended: automap follows the player but doesn't change the map.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAutomapMapping))]
    private bool _isAutomapSuspended;

    /// <summary>The automap panel's Mapping / Tracking only choice: true while automap changes the map.</summary>
    public bool IsAutomapMapping
    {
        get => !IsAutomapSuspended;
        set => IsAutomapSuspended = !value;
    }

    partial void OnIsAutomapSuspendedChanged(bool value)
    {
        // chosen in the panel: tell the engine (when the engine changed, it already agrees)
        if (value == AutomapEngine.Instance.Suspended)
        {
            return;
        }

        if (value)
        {
            AutomapEngine.Instance.Suspend();
        }
        else
        {
            AutomapEngine.Instance.Resume();
        }

        RefreshAutomapPanel();
    }

    /// <summary>The room automap thinks the player is in, for the automap panel.</summary>
    [ObservableProperty]
    private string _automapRoomName = string.Empty;

    /// <summary>Where automap is in the transcript: following the game, or paused before a move.</summary>
    [ObservableProperty]
    private string _automapTranscriptNote = string.Empty;

    /// <summary>Paused move by move while the game goes on: the panel offers to go live.</summary>
    [ObservableProperty]
    private bool _isAutomapGameLive;

    /// <summary>Where automap is in the transcript: "Move 12 of 4,567" (transcript moves, not the game's).</summary>
    [ObservableProperty]
    private string _automapMoveCount = string.Empty;

    private bool _canGoBackAutomap;
    private bool _hasNextAutomapMove;

    /// <summary>Previous Move: there's a move to go back to.</summary>
    public bool CanGoBackAutomap => _canGoBackAutomap;

    /// <summary>Next Move: there's a move left to play.</summary>
    public bool HasNextAutomapMove => _hasNextAutomapMove;

    /// <summary>Play to the End: paused, with moves left to play.</summary>
    public bool CanPlayToEnd => _hasNextAutomapMove && IsAutomapMoveByMove;

    /// <summary>What automap is doing with what it reads: mapping, tracking only, not mapping here, in the dark.</summary>
    [ObservableProperty]
    private string _automapMappingNote = string.Empty;

    /// <summary>Green while mapping, amber while not changing the map, grey in the dark.</summary>
    [ObservableProperty]
    private global::Avalonia.Media.IBrush _automapStateBrush = global::Avalonia.Media.Brushes.Gray;

    private static readonly global::Avalonia.Media.IBrush _mappingBrush =
        new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.FromRgb(0x4C, 0xAF, 0x50));

    private static readonly global::Avalonia.Media.IBrush _notMappingBrush =
        new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.FromRgb(0xE0, 0xA0, 0x30));

    private DispatcherTimer? _automapPanelTimer;

    /// <summary>Brings the automap panel up to date with the engine: where you are, the transcript and mapping.</summary>
    internal void RefreshAutomapPanel()
    {
        var engine = AutomapEngine.Instance;
        var room = engine.CurrentRoom;
        AutomapRoomName = room?.Name ?? "Somewhere automap doesn't know";
        IsAutomapMoveByMove = engine.MoveByMove;
        IsAutomapSuspended = engine.Suspended;
        var (played, total) = engine.MoveCount();
        AutomapMoveCount = total == 0
            ? string.Empty
            : string.Format(System.Globalization.CultureInfo.CurrentCulture, "Move {0:N0} of {1:N0}", played, total);
        var canGoBack = engine.CanGoBack;
        var hasNext = played < total;
        if (canGoBack != _canGoBackAutomap || hasNext != _hasNextAutomapMove)
        {
            _canGoBackAutomap = canGoBack;
            _hasNextAutomapMove = hasNext;
            OnPropertyChanged(nameof(CanGoBackAutomap));
            OnPropertyChanged(nameof(HasNextAutomapMove));
        }

        OnPropertyChanged(nameof(CanPlayToEnd));
        GoBackAutomapCommand.NotifyCanExecuteChanged();
        PlayNextMoveCommand.NotifyCanExecuteChanged();
        PlayToEndCommand.NotifyCanExecuteChanged();
        IsAutomapGameLive = engine.MoveByMove && engine.GameWentOn;
        IsAutomapAwaitingCommand = engine.MoveByMove && engine.NextCommand != null;
        AutomapTranscriptNote = !engine.MoveByMove
            ? "Playing: following the game as it goes on."
            : IsAutomapGameLive
                ? string.IsNullOrWhiteSpace(engine.NextCommand)
                    ? "The game has gone on: paused before its next move."
                    : $"The game has gone on: paused before \"{engine.NextCommand}\"."
            : string.IsNullOrWhiteSpace(engine.NextCommand)
                ? "Paused before the next move."
                : $"Paused before \"{engine.NextCommand}\".";
        if (engine.Suspended)
        {
            AutomapMappingNote = "Tracking only: following where you are, without changing the map.";
            AutomapStateBrush = _notMappingBrush;
        }
        else if (engine.InDark)
        {
            AutomapMappingNote = "In the dark: the room will be mapped when it can be seen.";
            AutomapStateBrush = global::Avalonia.Media.Brushes.Gray;
        }
        else if (room?.AutomapPause == true)
        {
            AutomapMappingNote = $"Not mapping in {room.Name}: mapping carries on where you come out.";
            AutomapStateBrush = _notMappingBrush;
        }
        else
        {
            AutomapMappingNote = "Mapping: adding rooms and joins as you go.";
            AutomapStateBrush = _mappingBrush;
        }
    }

    private void StartAutomapPanelUpdates()
    {
        RefreshAutomapPanel();
        _automapPanelTimer ??= new DispatcherTimer(TimeSpan.FromMilliseconds(300), DispatcherPriority.Background,
            (_, _) => RefreshAutomapPanel());
        _automapPanelTimer.Start();
    }

    private void StopAutomapPanelUpdates()
    {
        _automapPanelTimer?.Stop();
    }

    /// <summary>
    ///     Paused before a command, move by move: the game monitor shows a flashing cursor after it, as if it had been
    ///     typed but not yet entered.
    /// </summary>
    [ObservableProperty]
    private bool _isAutomapAwaitingCommand;

    /// <summary>The end of the transcript read so far, as much as fits on the little game monitor.</summary>
    [ObservableProperty]
    private string _automapScreenText = string.Empty;

    /// <summary>The whole transcript read so far; kept up to date only while the transcript window is open.</summary>
    [ObservableProperty]
    private string _automapTranscriptText = string.Empty;

    private bool _isTranscriptWindowOpen;
    private bool _screenRefreshPending;
    private int _screenLineCount;
    private readonly List<string> _screenLines = new();

    /// <summary>Columns and rows of the little game monitor, for working out how much of the transcript fits.</summary>
    internal const int MonitorColumns = 50;
    internal const int MonitorRows = 16;

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
            // adding/removing elements raises EditorChange.Model on the same project, which ProjectChanged does not report (I-022)
            _commandService.Session.Changed += OnSessionChanged;
        }

        IsGameMonitorShown = _commandService.Session?.ShowGameMonitor ?? true;
        UpdateState();
    }

    private void OnProjectChanged(object? sender, EventArgs e)
    {
        UpdateState();
    }

    private void OnSessionChanged(EditorChange change)
    {
        if ((change & EditorChange.Model) != 0) UpdateElementCount();
        if (_commandService.Session != null) IsGameMonitorShown = _commandService.Session.ShowGameMonitor;
    }

    /// <summary>Show or hide the little game monitor; remembered between runs of the app.</summary>
    [RelayCommand]
    public void ToggleGameMonitor()
    {
        if (_commandService.Session == null)
        {
            IsGameMonitorShown = !IsGameMonitorShown;
            return;
        }

        _commandService.Session.ShowGameMonitor = !_commandService.Session.ShowGameMonitor;
        IsGameMonitorShown = _commandService.Session.ShowGameMonitor;
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
            AppInfo.ProductName,
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
        UpdateWindowTitle();
        UpdateElementCount();
    }

    private void UpdateElementCount()
    {
        var project = Project.Current;
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
            AppInfo.ProductName,
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
        AutomapEngine.Instance.ScreenChanged -= OnAutomapScreenChanged;
        IsAutomapActive = false;
        AutomapStatusText = "Automap is not running.";
        StopAutomapPanelUpdates();
    }

    /// <summary>Play the transcript's next move, then pause.</summary>
    [RelayCommand(CanExecute = nameof(HasNextAutomapMove))]
    public void PlayNextMove()
    {
        AutomapEngine.Instance.PlayNextMove();
        IsAutomapMoveByMove = true;
    }

    /// <summary>Play the rest of the transcript without pausing; asks first when there are many moves left.</summary>
    [RelayCommand(CanExecute = nameof(CanPlayToEnd))]
    public async Task PlayToEndAsync()
    {
        var session = _commandService.Session;
        if (session != null)
        {
            await Trizbort.Editor.Commands.CommandCatalogue.PlayToEndAsync(session);
        }
        else
        {
            AutomapEngine.Instance.PlayToEnd();
        }

        IsAutomapMoveByMove = AutomapEngine.Instance.MoveByMove;
    }

    /// <summary>Go back a move in the transcript, leaving the map as it is; Next Move plays it again.</summary>
    [RelayCommand(CanExecute = nameof(CanGoBackAutomap))]
    public void GoBackAutomap()
    {
        AutomapEngine.Instance.GoBack();
        IsAutomapMoveByMove = true;
    }

    /// <summary>Select the room automap thinks the player is in and centre the map on it.</summary>
    [RelayCommand]
    public void GoToAutomapRoom()
    {
        var session = _commandService.Session;
        if (session == null)
        {
            return;
        }

        Trizbort.Editor.Commands.CommandCatalogue.GoToAutomapRoom(session);
        _commandService.InvalidateCanvas();
    }

    /// <summary>Suspend mapping (automap follows the player but doesn't change the map), or map again.</summary>
    [RelayCommand]
    public void ToggleSuspendAutomap()
    {
        if (AutomapEngine.Instance.Suspended)
        {
            AutomapEngine.Instance.Resume();
        }
        else
        {
            AutomapEngine.Instance.Suspend();
        }

        IsAutomapSuspended = AutomapEngine.Instance.Suspended;
    }

    /// <summary>Pause before the transcript's next move.</summary>
    [RelayCommand]
    public void PauseAutomap()
    {
        AutomapEngine.Instance.Pause();
        IsAutomapMoveByMove = true;
    }

    /// <summary>The transcript window is open (or closed): keep its text up to date only while it is open.</summary>
    internal void SetTranscriptWindowOpen(bool open)
    {
        _isTranscriptWindowOpen = open;
        AutomapTranscriptText = open ? string.Join(Environment.NewLine, _screenLines) : string.Empty;
    }

    private void ResetAutomapScreen()
    {
        _screenLines.Clear();
        _screenLineCount = 0;
        AutomapScreenText = string.Empty;
        AutomapTranscriptText = string.Empty;
    }

    // the engine reads a line at a time; the screen catches up at most once per dispatcher pass
    private void OnAutomapScreenChanged()
    {
        lock (_screenLines)
        {
            if (_screenRefreshPending) return;
            _screenRefreshPending = true;
        }

        Dispatcher.UIThread.Post(RefreshAutomapScreen, DispatcherPriority.Background);
    }

    private void RefreshAutomapScreen()
    {
        lock (_screenLines) _screenRefreshPending = false;
        var engine = AutomapEngine.Instance;
        if (engine.ScreenLineCount < _screenLineCount)
        {
            // automap went back a move: start the screen again from what it has now
            _screenLines.Clear();
            _screenLineCount = 0;
            AutomapScreenText = string.Empty;
            if (_isTranscriptWindowOpen) AutomapTranscriptText = string.Empty;
        }

        var lines = engine.ScreenLines(_screenLineCount);
        if (lines.Count == 0) return;
        _screenLines.AddRange(lines);
        _screenLineCount += lines.Count;
        AutomapScreenText = MonitorText(_screenLines, MonitorColumns, MonitorRows);
        if (_isTranscriptWindowOpen) AutomapTranscriptText = string.Join(Environment.NewLine, _screenLines);
    }

    /// <summary>The last lines that fit on a screen of this many columns and rows, long lines wrapping.</summary>
    internal static string MonitorText(IReadOnlyList<string> lines, int columns, int rows)
    {
        var used = 0;
        var first = lines.Count;
        while (first > 0)
        {
            var length = lines[first - 1].Length;
            var height = Math.Max(1, (length + columns - 1) / columns);
            if (used + height > rows) break;
            used += height;
            first--;
        }

        return string.Join("\n", lines.Skip(first));
    }

    public Task StartAutomapAsync(AutomapSettings settings)
    {
        StopAutomap();

        var runToken = new object();
        _currentRunToken = runToken;

        IsAutomapActive = true;
        IsAutomapMoveByMove = settings.MoveByMove;
        IsAutomapAutoTidy = settings.AutoTidy;
        AutomapStatusText = "Automapping has started.";
        ResetAutomapScreen();
        AutomapEngine.Instance.ScreenChanged += OnAutomapScreenChanged;
        StartAutomapPanelUpdates();

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
                AutomapEngine.Instance.ScreenChanged -= OnAutomapScreenChanged;
                AutomapStatusText = AutomapEngine.Instance.Status;
                IsAutomapActive = false;
                StopAutomapPanelUpdates();
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
        RunOnUiThread(() =>
        {
            AutomapStatusText = status;
            if (IsAutomapActive)
            {
                RefreshAutomapPanel();
            }
            else
            {
                IsAutomapMoveByMove = AutomapEngine.Instance.MoveByMove;
                IsAutomapSuspended = AutomapEngine.Instance.Suspended;
            }
        });
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
