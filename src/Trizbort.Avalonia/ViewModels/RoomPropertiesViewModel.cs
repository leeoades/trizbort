using System.Collections.ObjectModel;
using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using Region = Trizbort.Domain.Misc.Region;

namespace Trizbort.Avalonia.ViewModels;

public record RoomReferenceItem(int Id, string Name) { public override string ToString() => Name; }

public partial class RoomPropertiesViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _subTitle = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _objects = string.Empty;

    [ObservableProperty]
    private CompassPoint _objectsPosition = CompassPoint.South;

    [ObservableProperty]
    private bool _useCustomPosition;

    [ObservableProperty]
    private int _customPositionDown;

    [ObservableProperty]
    private int _customPositionRight;

    [ObservableProperty]
    private bool _isDark;

    [ObservableProperty]
    private bool _isStartRoom;

    [ObservableProperty]
    private bool _isEndRoom;

    [ObservableProperty]
    private RoomShape _shape = RoomShape.SquareCorners;

    [ObservableProperty]
    private BorderDashStyle _borderStyle = BorderDashStyle.Solid;

    [ObservableProperty]
    private HandDrawnStyle _handDrawnStyle = HandDrawnStyle.MapDefault;

    [ObservableProperty]
    private string _selectedRegion = Region.DefaultRegion;

    [ObservableProperty]
    private bool _allCornersEqual = true;

    [ObservableProperty]
    private double _cornerTopLeft = 15.0;

    [ObservableProperty]
    private double _cornerTopRight = 15.0;

    [ObservableProperty]
    private double _cornerBottomRight = 15.0;

    [ObservableProperty]
    private double _cornerBottomLeft = 15.0;

    [ObservableProperty]
    private Color _roomFillColor = Color.Transparent;

    [ObservableProperty]
    private Color _roomBorderColor = Color.Transparent;

    [ObservableProperty]
    private Color _roomNameColor = Color.Transparent;

    [ObservableProperty]
    private Color _roomSubtitleColor = Color.Transparent;

    [ObservableProperty]
    private Color _roomObjectTextColor = Color.Transparent;

    [ObservableProperty]
    private Color _secondFillColor = Color.Transparent;

    [ObservableProperty]
    private string _secondFillLocation = "Bottom";

    [ObservableProperty]
    private RoomReferenceItem? _selectedReferenceRoom;

    public ObservableCollection<string> AvailableRegions { get; } = [];
    public ObservableCollection<RoomReferenceItem> AvailableReferenceRooms { get; } = [];
    public IReadOnlyList<RoomShape> AvailableShapes { get; } = [
        RoomShape.SquareCorners,
        RoomShape.RoundedCorners,
        RoomShape.Ellipse,
        RoomShape.Octagonal
    ];
    public IReadOnlyList<BorderDashStyle> AvailableBorderStyles { get; } = [
        BorderDashStyle.Solid,
        BorderDashStyle.Dash,
        BorderDashStyle.DashDot,
        BorderDashStyle.DashDotDot,
        BorderDashStyle.Dot,
        BorderDashStyle.None
    ];
    public IReadOnlyList<HandDrawnStyle> AvailableHandDrawnStyles { get; } = [
        HandDrawnStyle.MapDefault,
        HandDrawnStyle.HandDrawn,
        HandDrawnStyle.Straight
    ];
    public IReadOnlyList<CompassPoint> AvailableObjectPositions { get; } = [
        CompassPoint.North,
        CompassPoint.NorthEast,
        CompassPoint.East,
        CompassPoint.SouthEast,
        CompassPoint.South,
        CompassPoint.SouthWest,
        CompassPoint.West,
        CompassPoint.NorthWest
    ];
    public IReadOnlyList<string> AvailableSecondFillLocations { get; } = [
        "Bottom",
        "BottomRight",
        "BottomLeft",
        "Left",
        "Right",
        "TopRight",
        "TopLeft",
        "Top"
    ];

    partial void OnAllCornersEqualChanged(bool value)
    {
        if (value)
        {
            CornerTopRight = CornerTopLeft;
            CornerBottomRight = CornerTopLeft;
            CornerBottomLeft = CornerTopLeft;
        }
    }

    partial void OnCornerTopLeftChanged(double value)
    {
        if (AllCornersEqual)
        {
            CornerTopRight = value;
            CornerBottomRight = value;
            CornerBottomLeft = value;
        }
    }

    public void Initialize(Room room)
    {
        Name = room.Name ?? string.Empty;
        SubTitle = room.SubTitle ?? string.Empty;
        Description = room.PrimaryDescription ?? string.Empty;
        Objects = room.Objects ?? string.Empty;
        ObjectsPosition = room.ObjectsPosition;
        UseCustomPosition = room.ObjectsCustomPosition;
        CustomPositionDown = room.ObjectsCustomPositionDown;
        CustomPositionRight = room.ObjectsCustomPositionRight;
        IsDark = room.IsDark;
        IsStartRoom = room.IsStartRoom;
        IsEndRoom = room.IsEndRoom;
        Shape = room.Shape;
        BorderStyle = room.BorderStyle;
        HandDrawnStyle = room.HandDrawnStyle;
        AllCornersEqual = room.AllCornersEqual;

        if (room.Corners != null)
        {
            CornerTopLeft = room.Corners.TopLeft;
            CornerTopRight = room.Corners.TopRight;
            CornerBottomRight = room.Corners.BottomRight;
            CornerBottomLeft = room.Corners.BottomLeft;
        }

        RoomFillColor = room.RoomFillColor;
        RoomBorderColor = room.RoomBorderColor;
        RoomNameColor = room.RoomNameColor;
        RoomSubtitleColor = room.RoomSubtitleColor;
        RoomObjectTextColor = room.RoomObjectTextColor;
        SecondFillColor = room.SecondFillColor;
        SecondFillLocation = string.IsNullOrEmpty(room.SecondFillLocation) ? "Bottom" : room.SecondFillLocation;

        AvailableRegions.Clear();
        foreach (var reg in Settings.Regions.OrderBy(p => p.RegionName != Region.DefaultRegion).ThenBy(p => p.RegionName))
        {
            AvailableRegions.Add(reg.RegionName);
        }

        SelectedRegion = !string.IsNullOrEmpty(room.Region) && AvailableRegions.Contains(room.Region)
            ? room.Region
            : (AvailableRegions.FirstOrDefault() ?? Region.DefaultRegion);

        AvailableReferenceRooms.Clear();
        AvailableReferenceRooms.Add(new RoomReferenceItem(-1, string.Empty));
        foreach (var r in Project.Current.Elements.OfType<Room>().Where(p => p.Id != room.Id).OrderBy(p => p.Name))
        {
            AvailableReferenceRooms.Add(new RoomReferenceItem(r.Id, r.Name));
        }

        SelectedReferenceRoom = AvailableReferenceRooms.FirstOrDefault(x => x.Id == room.ReferenceRoomId) 
                                ?? AvailableReferenceRooms.First();
    }

    public void ApplyTo(Room room)
    {
        room.Name = Name;
        room.SubTitle = SubTitle;
        room.ClearDescriptions();
        if (!string.IsNullOrEmpty(Description))
        {
            room.AddDescription(Description);
        }
        room.Objects = Objects;
        room.ObjectsPosition = ObjectsPosition;
        room.ObjectsCustomPosition = UseCustomPosition;
        room.ObjectsCustomPositionDown = CustomPositionDown;
        room.ObjectsCustomPositionRight = CustomPositionRight;
        room.IsDark = IsDark;
        room.IsStartRoom = IsStartRoom;
        room.IsEndRoom = IsEndRoom;
        room.Shape = Shape;
        room.BorderStyle = BorderStyle;
        room.HandDrawnStyle = HandDrawnStyle;
        room.Region = SelectedRegion;
        room.AllCornersEqual = AllCornersEqual;

        room.Corners = new CornerRadii
        {
            TopLeft = CornerTopLeft,
            TopRight = AllCornersEqual ? CornerTopLeft : CornerTopRight,
            BottomRight = AllCornersEqual ? CornerTopLeft : CornerBottomRight,
            BottomLeft = AllCornersEqual ? CornerTopLeft : CornerBottomLeft
        };

        room.RoomFillColor = RoomFillColor;
        room.RoomBorderColor = RoomBorderColor;
        room.RoomNameColor = RoomNameColor;
        room.RoomSubtitleColor = RoomSubtitleColor;
        room.RoomObjectTextColor = RoomObjectTextColor;
        room.SecondFillColor = SecondFillColor;
        room.SecondFillLocation = SecondFillLocation;

        if (SelectedReferenceRoom != null && SelectedReferenceRoom.Id != -1)
        {
            room.ReferenceRoomId = SelectedReferenceRoom.Id;
        }
        else
        {
            room.ReferenceRoomId = -1;
        }
    }
}
