using System.Collections.ObjectModel;
using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Editor;
using Trizbort.Setup;
using MapRegion = Trizbort.Domain.Misc.Region;

namespace Trizbort.Avalonia.ViewModels;

/// <summary>
///   Several rooms' shared properties, edited together (Lee, 24 Nov 2026). A value the rooms differ on shows as mixed
///   (a blank choice, a half-ticked box, a colour marked Mixed) and is left as each room has it unless changed.
/// </summary>
public partial class RoomsPropertiesViewModel : ViewModelBase
{
    /// <summary>Stands for "the rooms differ" in a colour box: nothing a room would have, so any pick is a change.</summary>
    public static readonly Color Mixed = RoomsEdit.MixedColor;

    public int Count { get; private set; }

    /// <summary>Where the room dialog has its name: how many rooms.</summary>
    public string CountText => $"{Count} rooms selected";

    public string Heading => "Anything shown as mixed stays as each room has it unless you change it.";

    /// <summary>The same tabs as one room's dialog, opening on the one last used there (Lee, 25 Nov 2026).</summary>
    [ObservableProperty] private int _selectedTabIndex;

    [ObservableProperty] private bool? _isDark;
    [ObservableProperty] private bool? _automapPause;
    [ObservableProperty] private bool? _moves;

    /// <summary>How the rooms' object lists are placed, and whether Reset will put them all back.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ObjectListText))]
    [NotifyPropertyChangedFor(nameof(CanResetObjectLists))]
    private bool _resetObjectLists;

    private int _placedObjectLists;

    public bool CanResetObjectLists => !ResetObjectLists && _placedObjectLists > 0;

    public string ObjectListText => ResetObjectLists || _placedObjectLists == 0
        ? "Automatic: at the map's default spot, or out of the way of joins and other rooms."
        : _placedObjectLists == Count
            ? "Placed by hand in each room (drag them on the map to move them)."
            : $"Placed by hand in {_placedObjectLists} of the rooms; automatic in the rest.";

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ResetObjectListPositions() => ResetObjectLists = true;

    /// <summary>The region list's Add New Region....</summary>
    public RegionChoices Regions { get; } = new();

    partial void OnRegionChanged(string? oldValue, string? newValue) =>
        Regions.Handle(newValue, oldValue, AvailableRegions, region => Region = region);

    public void SaveLastTab() =>
        Trizbort.Domain.AppSettings.ApplicationSettingsController.AppSettings.RoomDialogLastTab = SelectedTabIndex;
    [ObservableProperty] private RoomShape? _shape;
    [ObservableProperty] private BorderDashStyle? _borderStyle;
    [ObservableProperty] private HandDrawnStyle? _handDrawnStyle;
    [ObservableProperty] private string? _region;
    [ObservableProperty] private string? _secondFillLocation;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(FillMixed))] private Color _fillColor;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(SecondFillMixed))] private Color _secondFillColor;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(BorderMixed))] private Color _borderColor;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(NameMixed))] private Color _nameColor;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(SubtitleMixed))] private Color _subtitleColor;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ObjectTextMixed))] private Color _objectTextColor;

    public bool FillMixed => FillColor == Mixed;
    public bool SecondFillMixed => SecondFillColor == Mixed;
    public bool BorderMixed => BorderColor == Mixed;
    public bool NameMixed => NameColor == Mixed;
    public bool SubtitleMixed => SubtitleColor == Mixed;
    public bool ObjectTextMixed => ObjectTextColor == Mixed;

    public ObservableCollection<string> AvailableRegions { get; } = [];

    public IReadOnlyList<RoomShape> AvailableShapes { get; } =
        [RoomShape.SquareCorners, RoomShape.RoundedCorners, RoomShape.Ellipse, RoomShape.Octagonal];

    public IReadOnlyList<BorderDashStyle> AvailableBorderStyles { get; } =
    [
        BorderDashStyle.Solid, BorderDashStyle.Dash, BorderDashStyle.DashDot, BorderDashStyle.DashDotDot,
        BorderDashStyle.Dot, BorderDashStyle.None
    ];

    public IReadOnlyList<HandDrawnStyle> AvailableHandDrawnStyles { get; } =
        [Domain.Enums.HandDrawnStyle.MapDefault, Domain.Enums.HandDrawnStyle.HandDrawn, Domain.Enums.HandDrawnStyle.Straight];

    public IReadOnlyList<string> AvailableSecondFillLocations { get; } =
        ["Bottom", "BottomRight", "BottomLeft", "Left", "Right", "TopRight", "TopLeft", "Top"];

    public void Initialize(IReadOnlyList<Room> rooms)
    {
        Count = rooms.Count;
        OnPropertyChanged(nameof(CountText));
        SelectedTabIndex = Math.Clamp(
            Trizbort.Domain.AppSettings.ApplicationSettingsController.AppSettings.RoomDialogLastTab, 0, RoomPropertiesViewModel.TabCount - 1);
        _placedObjectLists = rooms.Count(room => room.ObjectsPlaced);
        ResetObjectLists = false;
        OnPropertyChanged(nameof(ObjectListText));
        OnPropertyChanged(nameof(CanResetObjectLists));
        var shared = RoomsEdit.Of(rooms);

        AvailableRegions.Clear();
        foreach (var region in Settings.Regions.OrderBy(r => r.RegionName != MapRegion.DefaultRegion).ThenBy(r => r.RegionName))
            AvailableRegions.Add(region.RegionName);

        IsDark = shared.IsDark;
        AutomapPause = shared.AutomapPause;
        Moves = shared.Moves;
        Shape = shared.Shape;
        BorderStyle = shared.BorderStyle;
        HandDrawnStyle = shared.HandDrawnStyle;
        Region = shared.Region != null && AvailableRegions.Contains(shared.Region) ? shared.Region : null;
        Regions.Finish(AvailableRegions);
        SecondFillLocation = shared.SecondFillLocation;
        FillColor = shared.FillColor ?? Mixed;
        SecondFillColor = shared.SecondFillColor ?? Mixed;
        BorderColor = shared.BorderColor ?? Mixed;
        NameColor = shared.NameColor ?? Mixed;
        SubtitleColor = shared.SubtitleColor ?? Mixed;
        ObjectTextColor = shared.ObjectTextColor ?? Mixed;
    }

    /// <summary>What to set on every room: what was shared or chosen; null for what's still mixed.</summary>
    public RoomsEdit ToEdit() => new()
    {
        IsDark = IsDark,
        AutomapPause = AutomapPause,
        Moves = Moves,
        ResetObjectLists = ResetObjectLists,
        Shape = Shape,
        BorderStyle = BorderStyle,
        HandDrawnStyle = HandDrawnStyle,
        Region = Region == NewRegionChoice.Chosen ? null : Region,
        SecondFillLocation = SecondFillLocation,
        FillColor = Or(FillColor),
        SecondFillColor = Or(SecondFillColor),
        BorderColor = Or(BorderColor),
        NameColor = Or(NameColor),
        SubtitleColor = Or(SubtitleColor),
        ObjectTextColor = Or(ObjectTextColor)
    };

    private static Color? Or(Color color) => color == Mixed ? null : color;
}
