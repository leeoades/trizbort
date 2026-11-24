using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using Trizbort.Domain.Enums;
using MapBackground = Trizbort.Domain.Elements.Background;

namespace Trizbort.Avalonia.ViewModels;

/// <summary>A background's colour, border, shape and lines. Lee, 24 Nov 2026.</summary>
public partial class BackgroundPropertiesViewModel : ViewModelBase
{
    [ObservableProperty]
    private Color _fillColor = MapBackground.DefaultFill;

    [ObservableProperty]
    private bool _hasBorder;

    [ObservableProperty]
    private BorderDashStyle _borderStyle = BorderDashStyle.Solid;

    [ObservableProperty]
    private Color _borderColor = MapBackground.DefaultBorder;

    [ObservableProperty]
    private RoomShape _shape = RoomShape.RoundedCorners;

    [ObservableProperty]
    private HandDrawnStyle _handDrawnStyle = HandDrawnStyle.MapDefault;

    public IReadOnlyList<RoomShape> AvailableShapes { get; } = [
        RoomShape.SquareCorners,
        RoomShape.RoundedCorners,
        RoomShape.Octagonal,
        RoomShape.Ellipse
    ];

    public IReadOnlyList<BorderDashStyle> AvailableBorderStyles { get; } = [
        BorderDashStyle.Solid,
        BorderDashStyle.Dash,
        BorderDashStyle.Dot,
        BorderDashStyle.DashDot,
        BorderDashStyle.DashDotDot
    ];

    public IReadOnlyList<HandDrawnStyle> AvailableHandDrawnStyles { get; } = [
        HandDrawnStyle.MapDefault,
        HandDrawnStyle.HandDrawn,
        HandDrawnStyle.Straight
    ];

    public void Initialize(MapBackground background)
    {
        FillColor = background.FillColor;
        HasBorder = background.BorderStyle != BorderDashStyle.None;
        BorderStyle = HasBorder ? background.BorderStyle : BorderDashStyle.Solid;
        BorderColor = background.BorderColor;
        Shape = background.Shape;
        HandDrawnStyle = background.HandDrawnStyle;
    }

    public void ApplyTo(MapBackground background)
    {
        background.FillColor = FillColor;
        background.BorderStyle = HasBorder ? BorderStyle : BorderDashStyle.None;
        background.BorderColor = BorderColor;
        background.Shape = Shape;
        background.HandDrawnStyle = HandDrawnStyle;
    }
}
