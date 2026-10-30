using CommunityToolkit.Mvvm.ComponentModel;
using Trizbort.Domain.AppSettings;

namespace Trizbort.Avalonia.ViewModels;

public partial class AppSettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _invertMouseWheel;

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
    private bool _showTooltips = true;

    [ObservableProperty]
    private bool _showDescriptionsInTooltips = true;

    [ObservableProperty]
    private bool _showObjectsInTooltips = true;

    [ObservableProperty]
    private bool _limitRoomDescriptionCharactersInTooltip;

    [ObservableProperty]
    private int _toolTipRoomDescriptionCharactersToShow = 100;

    [ObservableProperty]
    private bool _limitConnectionDescriptionCharactersInTooltip;

    [ObservableProperty]
    private int _toolTipConnectionDescriptionCharactersToShow = 100;

    public void Initialize()
    {
        var settings = ApplicationSettingsController.AppSettings;
        InvertMouseWheel = settings.InvertMouseWheel;
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
        ShowTooltips = settings.ShowTooltips;
        ShowDescriptionsInTooltips = settings.ShowDescriptionsInTooltips;
        ShowObjectsInTooltips = settings.ShowObjectsInTooltips;
        LimitRoomDescriptionCharactersInTooltip = settings.LimitRoomDescriptionCharactersInTooltip;
        ToolTipRoomDescriptionCharactersToShow = settings.ToolTipRoomDescriptionCharactersToShow;
        LimitConnectionDescriptionCharactersInTooltip = settings.LimitConnectionDescriptionCharactersInTooltip;
        ToolTipConnectionDescriptionCharactersToShow = settings.ToolTipConnectionDescriptionCharactersToShow;
    }

    public void ApplyToSettings()
    {
        var settings = ApplicationSettingsController.AppSettings;
        settings.InvertMouseWheel = InvertMouseWheel;
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
        settings.ShowDescriptionsInTooltips = ShowDescriptionsInTooltips;
        settings.ShowObjectsInTooltips = ShowObjectsInTooltips;
        settings.LimitRoomDescriptionCharactersInTooltip = LimitRoomDescriptionCharactersInTooltip;
        settings.ToolTipRoomDescriptionCharactersToShow = ToolTipRoomDescriptionCharactersToShow;
        settings.LimitConnectionDescriptionCharactersInTooltip = LimitConnectionDescriptionCharactersInTooltip;
        settings.ToolTipConnectionDescriptionCharactersToShow = ToolTipConnectionDescriptionCharactersToShow;

        ApplicationSettingsController.SaveSettings();
    }
}
