using System.Drawing;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Trizbort.Avalonia.Controls;

public partial class ColorPickerButton : UserControl
{
    public static readonly StyledProperty<Color> SelectedColorProperty =
        AvaloniaProperty.Register<ColorPickerButton, Color>(
            nameof(SelectedColor),
            defaultValue: Color.Transparent,
            defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    public Color SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    public ColorPickerButton()
    {
        InitializeComponent();
    }

    private void OnTransparentClick(object? sender, RoutedEventArgs e)
    {
        SelectedColor = Color.Transparent;
        PickerButton.Flyout?.Hide();
    }

    private void OnPaletteColorClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string colorName })
        {
            try
            {
                var color = Color.FromName(colorName);
                if (color.IsKnownColor)
                {
                    SelectedColor = color;
                }
                else
                {
                    SelectedColor = ColorTranslator.FromHtml(colorName);
                }
            }
            catch
            {
                // Fallback
            }
        }
        PickerButton.Flyout?.Hide();
    }

    private void OnHexSetClick(object? sender, RoutedEventArgs e)
    {
        var text = HexInput.Text?.Trim();
        if (!string.IsNullOrEmpty(text))
        {
            try
            {
                if (!text.StartsWith('#')) text = "#" + text;
                SelectedColor = ColorTranslator.FromHtml(text);
            }
            catch
            {
                // Ignore invalid hex
            }
        }
        PickerButton.Flyout?.Hide();
    }
}
