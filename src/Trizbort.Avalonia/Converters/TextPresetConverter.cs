using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Trizbort.Avalonia.ViewModels;

namespace Trizbort.Avalonia.Converters;

/// <summary>Maps a <see cref="TextPreset"/> to a RadioButton's IsChecked (parameter = the radio's preset).</summary>
public class TextPresetConverter : IValueConverter
{
    public static readonly TextPresetConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is TextPreset preset && parameter is TextPreset target && preset == target;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is TextPreset target) return target;
        return BindingOperations.DoNothing;
    }
}
