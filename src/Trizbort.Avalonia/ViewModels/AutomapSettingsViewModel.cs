using System.Threading.Tasks;
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
    private string _fileName = string.Empty;

    public bool CanStart => !string.IsNullOrWhiteSpace(FileName);

    [ObservableProperty]
    private bool _continueTranscript;

    [ObservableProperty]
    private bool _verboseTranscript = true;

    [ObservableProperty]
    private bool _assumeRoomsWithSameNameAreSameRoom;

    [ObservableProperty]
    private bool _isAssumeRoomsWithSameNameEnabled = true;

    [ObservableProperty]
    private bool _assumeTwoWayConnections = true;

    [ObservableProperty]
    private bool _guessExits = true;

    [ObservableProperty]
    private string _addObjectCommand = "tb see";

    [ObservableProperty]
    private string _addRegionCommand = "tb region";

    public AutomapSettingsViewModel(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public void Initialize(AutomapSettings settings)
    {
        FileName = settings.FileName ?? string.Empty;
        ContinueTranscript = settings.ContinueTranscript;
        VerboseTranscript = settings.VerboseTranscript;
        IsAssumeRoomsWithSameNameEnabled = settings.VerboseTranscript;
        AssumeRoomsWithSameNameAreSameRoom = !settings.VerboseTranscript || settings.AssumeRoomsWithSameNameAreSameRoom;
        AssumeTwoWayConnections = settings.AssumeTwoWayConnections;
        GuessExits = settings.GuessExits;
        AddObjectCommand = string.IsNullOrEmpty(settings.AddObjectCommand) ? "tb see" : settings.AddObjectCommand;
        AddRegionCommand = string.IsNullOrEmpty(settings.AddRegionCommand) ? "tb region" : settings.AddRegionCommand;
    }

    partial void OnVerboseTranscriptChanged(bool value)
    {
        IsAssumeRoomsWithSameNameEnabled = value;
        if (!value)
        {
            AssumeRoomsWithSameNameAreSameRoom = true;
        }
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
        settings.SingleStep = false;
        settings.VerboseTranscript = VerboseTranscript;
        settings.AssumeRoomsWithSameNameAreSameRoom = !VerboseTranscript || AssumeRoomsWithSameNameAreSameRoom;
        settings.GuessExits = GuessExits;
        settings.AddObjectCommand = string.IsNullOrEmpty(AddObjectCommand) ? AutomapSettings.Default.AddObjectCommand : AddObjectCommand;
        settings.AddRegionCommand = string.IsNullOrEmpty(AddRegionCommand) ? AutomapSettings.Default.AddRegionCommand : AddRegionCommand;
        settings.ContinueTranscript = ContinueTranscript;
        settings.AssumeTwoWayConnections = AssumeTwoWayConnections;
        return settings;
    }
}
