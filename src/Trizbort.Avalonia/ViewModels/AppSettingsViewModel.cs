using System;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Trizbort.Avalonia.Drawing;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.AppSettings;

namespace Trizbort.Avalonia.ViewModels;

public partial class AppSettingsViewModel : ViewModelBase
{
    // Where you are, while automapping: a token on your room, and a glow round it (HereToken). Lee, 22 Nov 2026.

    /// <summary>The tokens to choose from, each shown with its picture; the last is your own.</summary>
    public IReadOnlyList<HereTokenTile> HereTokenTiles { get; } = HereToken.Choices
        .Select(choice => new HereTokenTile(choice.Key, choice.Name,
            choice.Key == HereToken.Own ? null : HereToken.Image(choice.Key, 128)))
        .ToList();

    /// <summary>The token is shown; unticked, the one chosen stays chosen for when it's ticked again.</summary>
    [ObservableProperty]
    private bool _hereTokenShown = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UsesOwnToken))]
    [NotifyPropertyChangedFor(nameof(SelectedHereToken))]
    private int _hereTokenIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HereTokenFileName))]
    private string _hereTokenFile = string.Empty;

    /// <summary>The tile chosen.</summary>
    public HereTokenTile? SelectedHereToken
    {
        get => HereTokenIndex >= 0 && HereTokenIndex < HereTokenTiles.Count ? HereTokenTiles[HereTokenIndex] : null;
        set
        {
            if (value != null) HereTokenIndex = HereTokenTiles.ToList().IndexOf(value);
        }
    }

    /// <summary>Your own picture is chosen.</summary>
    public bool UsesOwnToken => HereTokenKey == HereToken.Own;

    /// <summary>The file your own token is drawn from, by name, or a hint when there's none.</summary>
    public string HereTokenFileName =>
        string.IsNullOrWhiteSpace(HereTokenFile) ? "No picture chosen yet" : System.IO.Path.GetFileName(HereTokenFile);

    private string HereTokenKey =>
        HereTokenIndex >= 0 && HereTokenIndex < HereToken.Choices.Count ? HereToken.Choices[HereTokenIndex].Key : HereToken.WoodenDisc;

    /// <summary>A picture loaded for your own token: shown on its tile, and chosen.</summary>
    partial void OnHereTokenFileChanged(string value)
    {
        var own = HereTokenTiles.FirstOrDefault(tile => tile.Key == HereToken.Own);
        if (own != null) own.Image = string.IsNullOrWhiteSpace(value) ? null : HereToken.Image(value, 128);
    }

    /// <summary>Your own picture, loaded: shown on its tile, and chosen.</summary>
    public void UseOwnToken(string file)
    {
        HereTokenFile = file;
        HereTokenIndex = HereToken.Choices.ToList().FindIndex(choice => choice.Key == HereToken.Own);
    }

    [ObservableProperty]
    private bool _hereGlow = true;

    [ObservableProperty]
    private System.Drawing.Color _hereGlowColor = System.Drawing.Color.FromArgb(0x14, 0xB8, 0xA6);

    [ObservableProperty]
    private bool _invertMouseWheel;

    /// <summary>How Recast updates itself (<see cref="UpdateMode" />), as three radio buttons. Lee, 26 Nov 2026.</summary>
    [ObservableProperty]
    private bool _updatesNotify;

    [ObservableProperty]
    private bool _updatesDownload = true;

    [ObservableProperty]
    private bool _updatesAutomatic;

    /// <summary>
    ///   Get test versions (betas). Shown ticked while on a beta when it hasn't been chosen, as that's what happens
    ///   then; saved only once changed, so "not chosen" lasts until it is (Lee, 26 Nov 2026).
    /// </summary>
    [ObservableProperty]
    private bool _testVersions;

    private bool _testVersionsAsShown;

    /// <summary>True when OK turned Get test versions off: a tester on a beta may want to go back.</summary>
    public bool TurnedOffTestVersions => _testVersionsAsShown && !TestVersions;

    /// <summary>A beta is running (the installed version, or this build's).</summary>
    private static bool RunningTestVersion() =>
        Trizbort.Updates.UpdateCoordinator.Current is { CanUpdate: true } updates
            ? updates.OnTestVersion
            : Trizbort.Updates.UpdateCoordinator.IsTestVersion(
                typeof(AppSettingsViewModel).Assembly
                    .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                    .OfType<System.Reflection.AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion
                    ?.Split('+')[0]);

    public UpdateMode UpdateChoice =>
        UpdatesNotify ? UpdateMode.Notify : UpdatesAutomatic ? UpdateMode.Automatic : UpdateMode.DownloadAndAsk;

    /// <summary>Index of <see cref="ScrollWheelMode" />: 0 Automatic, 1 Always zoom, 2 Always pan.</summary>
    [ObservableProperty]
    private int _scrollWheelBehavior;

    [ObservableProperty]
    private bool _showFullPathInTitleBar;

    [ObservableProperty]
    private bool _loadLastProjectOnStart;

    [ObservableProperty]
    private bool _keepRecoveryCopy = true;

    [ObservableProperty]
    private bool _specifyGenMargins;

    [ObservableProperty]
    private int _genHorizontalMargin;

    [ObservableProperty]
    private int _genVerticalMargin;

    [ObservableProperty]
    private bool _applyStyleToNewRooms;

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

    /// <summary>
    ///   This copy wasn't installed with an update-ready download (Setup.exe, the AppImage, the Mac -Portable.zip),
    ///   so it can't update itself: App Settings says so, and where to get one. Lee, 26 Nov 2026.
    /// </summary>
    [ObservableProperty]
    private bool _cantUpdateItself;

    /// <summary>For a plain copy: what to install instead, on this computer.</summary>
    public string HowToGetUpdates { get; } =
        $"It wasn't installed, so these settings will only apply once it is. To get updates, {UpdateDownload.HowToInstall()}. Your settings are kept.";

    /// <summary>Where the update-ready downloads are.</summary>
    public Uri ReleasesPage { get; } = new(UpdateBarViewModel.ReleasesPage);

    public void Initialize()
    {
        var settings = ApplicationSettingsController.AppSettings;
        var token = HereToken.Choices.Select((choice, index) => (choice.Key, index))
            .FirstOrDefault(choice => choice.Key == settings.HereToken);
        HereTokenIndex = token.Key == null ? 0 : token.index;
        HereTokenFile = settings.HereTokenFile ?? string.Empty;
        HereTokenShown = settings.HereTokenShown;
        HereGlow = settings.HereGlow;
        HereGlowColor = global::Avalonia.Media.Color.TryParse(settings.HereGlowColor, out var glow)
            ? System.Drawing.Color.FromArgb(glow.R, glow.G, glow.B)
            : System.Drawing.Color.FromArgb(0x14, 0xB8, 0xA6);
        InvertMouseWheel = settings.InvertMouseWheel;
        CantUpdateItself = Trizbort.Updates.UpdateCoordinator.Current?.CanUpdate != true;
        UpdatesNotify = settings.Updates == UpdateMode.Notify;
        UpdatesAutomatic = settings.Updates == UpdateMode.Automatic;
        UpdatesDownload = !UpdatesNotify && !UpdatesAutomatic;
        TestVersions = _testVersionsAsShown = settings.TestVersions ?? RunningTestVersion();
        ScrollWheelBehavior = (int)settings.ScrollWheelBehavior;
        ShowFullPathInTitleBar = settings.ShowFullPathInTitleBar;
        LoadLastProjectOnStart = settings.LoadLastProjectOnStart;
        KeepRecoveryCopy = settings.KeepRecoveryCopy;
        SpecifyGenMargins = settings.SpecifyGenMargins;
        GenHorizontalMargin = settings.GenHorizontalMargin;
        GenVerticalMargin = settings.GenVerticalMargin;
        ApplyStyleToNewRooms = settings.ApplyStyleToNewRooms;
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
        settings.HereToken = HereTokenKey;
        settings.HereTokenFile = HereTokenFile ?? string.Empty;
        settings.HereTokenShown = HereTokenShown;
        settings.HereGlow = HereGlow;
        // the colour cleared (×): back to teal, rather than a black glow
        settings.HereGlowColor = HereGlowColor.A == 0
            ? "#14B8A6"
            : $"#{HereGlowColor.R:X2}{HereGlowColor.G:X2}{HereGlowColor.B:X2}";
        settings.InvertMouseWheel = InvertMouseWheel;
        settings.Updates = UpdateChoice;
        if (TestVersions != _testVersionsAsShown) settings.TestVersions = TestVersions;
        settings.ScrollWheelBehavior = Enum.IsDefined(typeof(ScrollWheelMode), ScrollWheelBehavior)
            ? (ScrollWheelMode)ScrollWheelBehavior
            : ScrollWheelMode.Automatic;
        settings.ShowFullPathInTitleBar = ShowFullPathInTitleBar;
        settings.LoadLastProjectOnStart = LoadLastProjectOnStart;
        var recoveryChanged = settings.KeepRecoveryCopy != KeepRecoveryCopy;
        settings.KeepRecoveryCopy = KeepRecoveryCopy;
        if (recoveryChanged) RecoveryService.Current?.Reevaluate();
        settings.SpecifyGenMargins = SpecifyGenMargins;
        settings.GenHorizontalMargin = GenHorizontalMargin;
        settings.GenVerticalMargin = GenVerticalMargin;
        settings.ApplyStyleToNewRooms = ApplyStyleToNewRooms;
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

/// <summary>A token in App Settings' panel of them: its picture, and its name.</summary>
public partial class HereTokenTile : ObservableObject
{
    public HereTokenTile(string key, string name, Bitmap? image)
    {
        Key = key;
        Name = name;
        _image = image;
    }

    public string Key { get; }

    public string Name { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoImage))]
    private Bitmap? _image;

    /// <summary>No picture: your own, before you've loaded one.</summary>
    public bool HasNoImage => Image == null;

    /// <summary>What shows instead of a picture.</summary>
    public string Placeholder => "+";
}
