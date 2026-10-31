using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;

namespace Trizbort.Avalonia.ViewModels;

public partial class MapLabelPropertiesViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private Color _textColor = Color.Transparent;

    [ObservableProperty]
    private bool _hasOutline;

    /// <summary>Shape changes both the outline and the fill, so it is editable when either one is on.</summary>
    public bool ShapeEnabled => HasOutline || HasBackground;

    [ObservableProperty]
    private RoomShape _shape = RoomShape.SquareCorners;

    [ObservableProperty]
    private BorderDashStyle _borderStyle = BorderDashStyle.Solid;

    [ObservableProperty]
    private Color _borderColor = Color.Transparent;

    [ObservableProperty]
    private bool _hasBackground;

    [ObservableProperty]
    private Color _backgroundColor = Color.Transparent;

    partial void OnHasOutlineChanged(bool value)
    {
        OnPropertyChanged(nameof(ShapeEnabled));
    }

    partial void OnHasBackgroundChanged(bool value)
    {
        OnPropertyChanged(nameof(ShapeEnabled));
    }

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

    public void Initialize(MapLabel label)
    {
        Text = label.Text ?? string.Empty;
        TextColor = label.TextColor;
        HasOutline = label.BorderStyle != BorderDashStyle.None;
        Shape = label.Shape == RoomShape.NotARoom ? RoomShape.SquareCorners : label.Shape;
        BorderStyle = HasOutline ? label.BorderStyle : BorderDashStyle.Solid;
        BorderColor = label.BorderColor;
        HasBackground = label.HasBackground;
        BackgroundColor = label.BackgroundColor;
    }

    public void ApplyTo(MapLabel label)
    {
        label.Text = Text;
        label.TextColor = TextColor;
        label.Shape = Shape;
        label.BorderStyle = HasOutline ? BorderStyle : BorderDashStyle.None;
        label.BorderColor = BorderColor;
        label.HasBackground = HasBackground;
        label.BackgroundColor = BackgroundColor;
    }
}
