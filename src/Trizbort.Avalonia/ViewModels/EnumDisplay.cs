using System.Globalization;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Setup;

namespace Trizbort.Avalonia.ViewModels;

/// <summary>Readable text for enum values shown in dropdowns (legacy wording).</summary>
public static class EnumDisplay
{
    public static string For(RoomShape shape) => shape switch
    {
        RoomShape.SquareCorners => "Straight Edges",
        RoomShape.RoundedCorners => "Rounded Corners",
        RoomShape.Ellipse => "Ellipse",
        RoomShape.Octagonal => "Octagonal",
        _ => shape.ToString()
    };

    public static string For(HandDrawnStyle style) => style switch
    {
        HandDrawnStyle.MapDefault => Settings.HandDrawn ? "Map setting (hand-drawn)" : "Map setting (straight)",
        HandDrawnStyle.HandDrawn => "Hand-drawn",
        HandDrawnStyle.Straight => "Straight",
        _ => style.ToString()
    };

    public static string For(BorderDashStyle style) => style switch
    {
        BorderDashStyle.Solid => "Solid",
        BorderDashStyle.Dash => "Dash",
        BorderDashStyle.DashDot => "Dash-dot",
        BorderDashStyle.DashDotDot => "Dash-dot-dot",
        BorderDashStyle.Dot => "Dot",
        BorderDashStyle.None => "None",
        _ => style.ToString()
    };

    /// <summary>Converter for ComboBox item templates: <c>{Binding ., Converter={x:Static vm:EnumDisplay.Converter}}</c>.</summary>
    public static global::Avalonia.Data.Converters.IValueConverter Converter { get; } = new DisplayConverter();

    private sealed class DisplayConverter : global::Avalonia.Data.Converters.IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
        {
            RoomShape shape => For(shape),
            HandDrawnStyle style => For(style),
            BorderDashStyle dash => For(dash),
            null => string.Empty,
            _ => value.ToString()
        };

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
