using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Trizbort.Domain.AppSettings;

namespace Trizbort.Avalonia.ViewModels;

public partial class AppSettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _invertMouseWheel;

    /// <summary>Index of <see cref="ScrollWheelMode" />: 0 Automatic, 1 Always zoom, 2 Always pan.</summary>
    [ObservableProperty]
    private int _scrollWheelBehavior;

    [ObservableProperty]
    private bool _showFullPathInTitleBar;

    [ObservableProperty]
    private bool _loadLastProjectOnStart;

    [ObservableProperty]
    private bool _specifyGenMargins;

    [ObservableProperty]
    private int _genHorizontalMargin;

    [ObservableProperty]
    private int _genVerticalMargin;

    [ObservableProperty]
    private bool _applyStyleToNewRooms;

    [ObservableProperty]
    private bool _doubleClickToAddRoom;

    [ObservableProperty]
    private int _portAdjustDetail;

    [ObservableProperty]
    private bool _saveAt100;

    [ObservableProperty]
    private bool _saveToPdf;

    [ObservableProperty]
    private bool _saveToImage;

    [ObservableProperty]
    private bool _saveTadsToAdv3Lite;

    [ObservableProperty]
    private int _defaultImageType;

    [ObservableProperty]
    private string _defaultFontName = "Arial";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditTooltipOptions))]
    [NotifyPropertyChangedFor(nameof(CanEditDescriptionLimits))]
    [NotifyPropertyChangedFor(nameof(CanEditRoomCharacterCount))]
    [NotifyPropertyChangedFor(nameof(CanEditConnectionCharacterCount))]
    private bool _showTooltips = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditDescriptionLimits))]
    private bool _showDescriptionsInTooltips = true;

    [ObservableProperty]
    private bool _showObjectsInTooltips = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditRoomCharacterCount))]
    private bool _limitRoomDescriptionCharactersInTooltip;

    [ObservableProperty]
    private int _toolTipRoomDescriptionCharactersToShow = 50;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditConnectionCharacterCount))]
    private bool _limitConnectionDescriptionCharactersInTooltip;

    [ObservableProperty]
    private int _toolTipConnectionDescriptionCharactersToShow = 50;

    // Children's values remembered while the master is unticked, so re-ticking restores them.
    private bool _savedShowDescriptionsInTooltips = true;
    private bool _savedShowObjectsInTooltips = true;

    /// <summary>Show Objects and Show Descriptions are editable only while the master is ticked.</summary>
    public bool CanEditTooltipOptions => ShowTooltips;

    /// <summary>Limit options are editable only while tooltips show descriptions.</summary>
    public bool CanEditDescriptionLimits => ShowTooltips && ShowDescriptionsInTooltips;

    public bool CanEditRoomCharacterCount => CanEditDescriptionLimits && LimitRoomDescriptionCharactersInTooltip;

    public bool CanEditConnectionCharacterCount => CanEditDescriptionLimits && LimitConnectionDescriptionCharactersInTooltip;

    partial void OnShowTooltipsChanged(bool value)
    {
        if (value)
        {
            ShowDescriptionsInTooltips = _savedShowDescriptionsInTooltips;
            ShowObjectsInTooltips = _savedShowObjectsInTooltips;
            if (!ShowDescriptionsInTooltips && !ShowObjectsInTooltips)
            {
                ShowDescriptionsInTooltips = true;
                ShowObjectsInTooltips = true;
            }
        }
        else
        {
            _savedShowDescriptionsInTooltips = ShowDescriptionsInTooltips;
            _savedShowObjectsInTooltips = ShowObjectsInTooltips;
            ShowDescriptionsInTooltips = false;
            ShowObjectsInTooltips = false;
        }
    }

    public void Initialize()
    {
        var settings = ApplicationSettingsController.AppSettings;
        InvertMouseWheel = settings.InvertMouseWheel;
        ScrollWheelBehavior = (int)settings.ScrollWheelBehavior;
        ShowFullPathInTitleBar = settings.ShowFullPathInTitleBar;
        LoadLastProjectOnStart = settings.LoadLastProjectOnStart;
        SpecifyGenMargins = settings.SpecifyGenMargins;
        GenHorizontalMargin = settings.GenHorizontalMargin;
        GenVerticalMargin = settings.GenVerticalMargin;
        ApplyStyleToNewRooms = settings.ApplyStyleToNewRooms;
        DoubleClickToAddRoom = settings.DoubleClickToAddRoom;
        PortAdjustDetail = settings.PortAdjustDetail;
        SaveAt100 = settings.SaveAt100;
        SaveToPdf = settings.SaveToPDF;
        SaveToImage = settings.SaveToImage;
        SaveTadsToAdv3Lite = settings.SaveTadstoAdv3Lite;
        DefaultImageType = settings.DefaultImageType;
        DefaultFontName = string.IsNullOrEmpty(settings.DefaultFontName) ? "Arial" : settings.DefaultFontName;
        // Children first, so the master hook never restores stale values from a previous Initialize.
        ShowDescriptionsInTooltips = settings.ShowDescriptionsInTooltips;
        ShowObjectsInTooltips = settings.ShowObjectsInTooltips;
        _savedShowDescriptionsInTooltips = ShowDescriptionsInTooltips;
        _savedShowObjectsInTooltips = ShowObjectsInTooltips;
        ShowTooltips = settings.ShowTooltips;
        LimitRoomDescriptionCharactersInTooltip = settings.LimitRoomDescriptionCharactersInTooltip;
        ToolTipRoomDescriptionCharactersToShow = settings.ToolTipRoomDescriptionCharactersToShow;
        LimitConnectionDescriptionCharactersInTooltip = settings.LimitConnectionDescriptionCharactersInTooltip;
        ToolTipConnectionDescriptionCharactersToShow = settings.ToolTipConnectionDescriptionCharactersToShow;
    }

    public void ApplyToSettings()
    {
        var settings = ApplicationSettingsController.AppSettings;
        settings.InvertMouseWheel = InvertMouseWheel;
        settings.ScrollWheelBehavior = Enum.IsDefined(typeof(ScrollWheelMode), ScrollWheelBehavior)
            ? (ScrollWheelMode)ScrollWheelBehavior
            : ScrollWheelMode.Automatic;
        settings.ShowFullPathInTitleBar = ShowFullPathInTitleBar;
        settings.LoadLastProjectOnStart = LoadLastProjectOnStart;
        settings.SpecifyGenMargins = SpecifyGenMargins;
        settings.GenHorizontalMargin = GenHorizontalMargin;
        settings.GenVerticalMargin = GenVerticalMargin;
        settings.ApplyStyleToNewRooms = ApplyStyleToNewRooms;
        settings.DoubleClickToAddRoom = DoubleClickToAddRoom;
        settings.PortAdjustDetail = PortAdjustDetail;
        settings.SaveAt100 = SaveAt100;
        settings.SaveToPDF = SaveToPdf;
        settings.SaveToImage = SaveToImage;
        settings.SaveTadstoAdv3Lite = SaveTadsToAdv3Lite;
        settings.DefaultImageType = DefaultImageType;
        settings.DefaultFontName = DefaultFontName;
        // The master is a UI convenience: unticked turns both flags off; ticked with both off turns both on.
        var showDescriptions = ShowDescriptionsInTooltips;
        var showObjects = ShowObjectsInTooltips;
        if (!ShowTooltips)
        {
            showDescriptions = false;
            showObjects = false;
        }
        else if (!showDescriptions && !showObjects)
        {
            showDescriptions = true;
            showObjects = true;
        }
        settings.ShowDescriptionsInTooltips = showDescriptions;
        settings.ShowObjectsInTooltips = showObjects;
        settings.LimitRoomDescriptionCharactersInTooltip = LimitRoomDescriptionCharactersInTooltip;
        settings.ToolTipRoomDescriptionCharactersToShow = ToolTipRoomDescriptionCharactersToShow;
        settings.LimitConnectionDescriptionCharactersInTooltip = LimitConnectionDescriptionCharactersInTooltip;
        settings.ToolTipConnectionDescriptionCharactersToShow = ToolTipConnectionDescriptionCharactersToShow;

        ApplicationSettingsController.SaveSettings();
    }
}
