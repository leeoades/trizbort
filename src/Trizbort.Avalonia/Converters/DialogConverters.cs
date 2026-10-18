using System;
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
