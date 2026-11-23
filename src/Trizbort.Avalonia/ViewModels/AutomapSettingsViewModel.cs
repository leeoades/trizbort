using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trizbort.Automap;
using Trizbort.Avalonia.Services;

namespace Trizbort.Avalonia.ViewModels;

public partial class AutomapSettingsViewModel : ViewModelBase
{
    private readonly IStorageService _storageService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    [NotifyPropertyChangedFor(nameof(IsFileNameMissing))]
    private string _fileName = string.Empty;

    public bool CanStart => WhileRunning || !string.IsNullOrWhiteSpace(FileName);

    /// <summary>True while no transcript file is chosen (Start is disabled); drives the visible reason.</summary>
    public bool IsFileNameMissing => !CanStart;

    /// <summary>
    ///     Opened from the automap panel while automap runs: only the options that can change part way through show,
    ///     and OK applies them from the next move.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    [NotifyPropertyChangedFor(nameof(IsFileNameMissing))]
    [NotifyPropertyChangedFor(nameof(IsStarting))]
    [NotifyPropertyChangedFor(nameof(ShowSingleStep))]
    [NotifyPropertyChangedFor(nameof(OkText))]
    [NotifyPropertyChangedFor(nameof(Title))]
    [NotifyPropertyChangedFor(nameof(Hint))]
    private bool _whileRunning;

    /// <summary>Starting automap: the transcript and how to start show.</summary>
    public bool IsStarting => !WhileRunning;

    public bool ShowSingleStep => IsDebugEnabled && IsStarting;

    public string OkText => WhileRunning ? "OK" : "Start Auto_mapping";

    public string Title => WhileRunning ? "Automap Settings" : "Automapping";

    public string Hint => WhileRunning
        ? "Changes take effect from the next move, and are kept for the next time you automap."
        : "Trizbort can automatically generate a map from a transcript of a game, even while the game is in progress.";

    [ObservableProperty]
    private bool _continueTranscript;

    [ObservableProperty]
    private bool _assumeTwoWayConnections = true;

    [ObservableProperty]
    private bool _guessExits = true;

    [ObservableProperty]
    private bool _autoTidy = true;

    // Not in the dialog (Lee, 23 Nov 2026), kept as they were: the game screen is shown or hidden from the automap panel;
    // how joins' directions are drawn is the map's Direction words setting; and rooms with the same name are always told
    // apart by their descriptions too, asking when they can't be (Don't Ask Again, in Which room does it mean?, lasts
    // the run it's chosen in).
    private bool _abbreviateDirections = true;
    private bool _bestGuessWhichRoom;

    /// <summary>Start paused, and play the transcript a move at a time from the automap bar.</summary>
    [ObservableProperty]
    private bool _moveByMove;

    [ObservableProperty]
    private string _addObjectCommand = "tb see";

    [ObservableProperty]
    private string _addRegionCommand = "tb region";

    /// <summary>Step through the transcript with F11 instead of running freely. Debug only.</summary>
    [ObservableProperty]
    private bool _singleStep;

    /// <summary>True when started with <c>--debug</c>; the single-step option is hidden otherwise.</summary>
    public bool IsDebugEnabled { get; set; } = AppOptions.Debug;

    public AutomapSettingsViewModel(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public void Initialize(AutomapSettings settings)
    {
        FileName = settings.FileName ?? string.Empty;
        ContinueTranscript = settings.ContinueTranscript;
        SingleStep = IsDebugEnabled && settings.SingleStep;
        AssumeTwoWayConnections = settings.AssumeTwoWayConnections;
        GuessExits = settings.GuessExits;
        AutoTidy = settings.AutoTidy;
        _bestGuessWhichRoom = settings.BestGuessWhichRoom;
        _abbreviateDirections = settings.AbbreviateDirections;
        MoveByMove = settings.MoveByMove;
        AddObjectCommand = string.IsNullOrEmpty(settings.AddObjectCommand) ? "tb see" : settings.AddObjectCommand;
        AddRegionCommand = string.IsNullOrEmpty(settings.AddRegionCommand) ? "tb region" : settings.AddRegionCommand;
    }

    [RelayCommand]
    public async Task BrowseTranscriptAsync(TopLevel? topLevel)
    {
        var path = await _storageService.OpenTranscriptFileAsync(topLevel, FileName);
        if (!string.IsNullOrWhiteSpace(path))
        {
            FileName = path;
        }
    }

    public AutomapSettings GetSettings()
    {
        var settings = AutomapSettings.Default;
        settings.FileName = FileName?.Trim() ?? string.Empty;
        settings.SingleStep = IsDebugEnabled && SingleStep;
        settings.VerboseTranscript = true;
        settings.GuessExits = GuessExits;
        settings.AutoTidy = AutoTidy;
        // a new run asks again; part way through, Don't Ask Again stays as it was chosen
        settings.BestGuessWhichRoom = WhileRunning && _bestGuessWhichRoom;
        settings.AbbreviateDirections = _abbreviateDirections;
        settings.MoveByMove = MoveByMove;
        settings.AddObjectCommand = string.IsNullOrEmpty(AddObjectCommand) ? AutomapSettings.Default.AddObjectCommand : AddObjectCommand;
        settings.AddRegionCommand = string.IsNullOrEmpty(AddRegionCommand) ? AutomapSettings.Default.AddRegionCommand : AddRegionCommand;
        settings.ContinueTranscript = ContinueTranscript;
        settings.AssumeTwoWayConnections = AssumeTwoWayConnections;
        return settings;
    }
}
