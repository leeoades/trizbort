using System.Collections.ObjectModel;
using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Editor;
using Trizbort.Setup;
using Region = Trizbort.Domain.Misc.Region;

namespace Trizbort.Avalonia.ViewModels;

/// <summary>
///   Several rooms' shared properties, edited together (Lee, 24 Nov 2026). A value the rooms differ on shows as mixed
///   (a blank choice, a half-ticked box, a colour marked Mixed) and is left as each room has it unless changed.
/// </summary>
public partial class RoomsPropertiesViewModel : ViewModelBase
{
    /// <summary>Stands for "the rooms differ" in a colour box: nothing a room would have, so any pick is a change.</summary>
    public static readonly Color Mixed = Color.FromArgb(1, 1, 2, 3);

    public int Count { get; private set; }

    public string Heading => $"{Count} rooms. Anything shown as mixed stays as each room has it unless you change it.";

    [ObservableProperty] private bool? _isDark;
    [ObservableProperty] private bool? _automapPause;
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
        [HandDrawnStyle.MapDefault, HandDrawnStyle.HandDrawn, HandDrawnStyle.Straight];

    public IReadOnlyList<string> AvailableSecondFillLocations { get; } =
        ["Bottom", "BottomRight", "BottomLeft", "Left", "Right", "TopRight", "TopLeft", "Top"];

    public void Initialize(IReadOnlyList<Room> rooms)
    {
        Count = rooms.Count;
        OnPropertyChanged(nameof(Heading));
        var shared = RoomsEdit.Of(rooms);

        AvailableRegions.Clear();
        foreach (var region in Settings.Regions.OrderBy(r => r.RegionName != Region.DefaultRegion).ThenBy(r => r.RegionName))
            AvailableRegions.Add(region.RegionName);

        IsDark = shared.IsDark;
        AutomapPause = shared.AutomapPause;
        Shape = shared.Shape;
        BorderStyle = shared.BorderStyle;
        HandDrawnStyle = shared.HandDrawnStyle;
        Region = shared.Region != null && AvailableRegions.Contains(shared.Region) ? shared.Region : null;
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
        Shape = Shape,
        BorderStyle = BorderStyle,
        HandDrawnStyle = HandDrawnStyle,
        Region = Region,
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
