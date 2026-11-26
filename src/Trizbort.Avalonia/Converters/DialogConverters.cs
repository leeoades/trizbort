using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Trizbort.Avalonia.Drawing;
using CoreColor = System.Drawing.Color;
using AvaloniaColor = Avalonia.Media.Color;

namespace Trizbort.Avalonia.Converters;

public class CoreColorToBrushConverter : IValueConverter
{
    public static readonly CoreColorToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is CoreColor coreColor)
        {
            if (coreColor == CoreColor.Transparent)
            {
                return new SolidColorBrush(AvaloniaColor.FromArgb(0, 255, 255, 255));
            }
            return coreColor.ToBrush();
        }
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class CoreColorToNameOrHexConverter : IValueConverter
{
    public static readonly CoreColorToNameOrHexConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is CoreColor coreColor)
        {
            if (coreColor == CoreColor.Transparent) return "Default / None";
            if (coreColor.IsNamedColor) return coreColor.Name;
            return $"#{coreColor.R:X2}{coreColor.G:X2}{coreColor.B:X2}";
        }
        return "Default";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>Maps a region name to a brush of that region's colour (for the Region combo swatches).</summary>
/// <summary>A region's name as shown: "No Region" for the default.</summary>
public class RegionDisplayNameConverter : IValueConverter
{
    public static readonly RegionDisplayNameConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value as string == Trizbort.Editor.NewRegionChoice.Chosen
            ? Trizbort.Avalonia.ViewModels.RegionChoices.AddNewText
            : Trizbort.Domain.Misc.Region.DisplayName(value as string ?? string.Empty);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class RegionNameToBrushConverter : IValueConverter
{
    public static readonly RegionNameToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value as string == Trizbort.Editor.NewRegionChoice.Chosen) return global::Avalonia.Media.Brushes.Transparent;
        var region = Trizbort.Setup.Settings.Regions.FirstOrDefault(r => r.RegionName == value as string);
        return (region?.RColor ?? CoreColor.White).ToBrush();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
