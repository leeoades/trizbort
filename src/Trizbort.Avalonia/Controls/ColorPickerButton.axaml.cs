using System.Drawing;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Trizbort.Avalonia.Converters;
using AvaloniaColor = global::Avalonia.Media.Color;

namespace Trizbort.Avalonia.Controls;

public partial class ColorPickerButton : UserControl
{
    public static readonly StyledProperty<Color> SelectedColorProperty =
        AvaloniaProperty.Register<ColorPickerButton, Color>(
            nameof(SelectedColor),
            defaultValue: Color.Transparent,
            defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string> NoColorTextProperty =
        AvaloniaProperty.Register<ColorPickerButton, string>(nameof(NoColorText), defaultValue: "No Color Set");

    private Color _colorAtOpen = Color.Transparent;
    private bool _syncing;

    public string NoColorText
    {
        get => GetValue(NoColorTextProperty);
        set => SetValue(NoColorTextProperty, value);
    }

    /// <summary>The text currently shown beside the swatch.</summary>
    public string DisplayText => ColorLabel.Text ?? string.Empty;

    public Color SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    public ColorPickerButton()
    {
        InitializeComponent();
        if (PickerButton.Flyout != null)
        {
            PickerButton.Flyout.Opened += (_, _) => _colorAtOpen = SelectedColor;
        }
        UpdateVisuals();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedColorProperty || change.Property == NoColorTextProperty)
        {
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        if (ColorLabel == null || NoColorHatch == null || ClearButton == null || FullColorView == null) return;
        var isSet = SelectedColor != Color.Transparent;
        ColorLabel.Text = isSet
            ? CoreColorToNameOrHexConverter.Instance.Convert(SelectedColor, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture) as string
            : NoColorText;
        NoColorHatch.IsVisible = !isSet;
        ClearButton.IsVisible = isSet;
        AutomationProperties.SetName(PickerButton, "Colour: " + ColorLabel.Text);

        if (isSet)
        {
            _syncing = true;
            try
            {
                FullColorView.Color = AvaloniaColor.FromRgb(SelectedColor.R, SelectedColor.G, SelectedColor.B);
            }
            finally
            {
                _syncing = false;
            }
        }
    }

    /// <summary>Clears the colour (sets Transparent, meaning default/none).</summary>
    public void ClearColor() => SelectedColor = Color.Transparent;

    /// <summary>Applies a hex colour (with or without leading #). Returns false if invalid.</summary>
    internal bool TryApplyHex(string? text)
    {
        text = text?.Trim();
        if (string.IsNullOrEmpty(text)) return false;
        try
        {
            if (!text.StartsWith('#')) text = "#" + text;
            SelectedColor = ColorTranslator.FromHtml(text);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Applies a colour chosen in the full colour view.</summary>
    internal void ApplyColorViewColor(AvaloniaColor color)
    {
        if (_syncing) return;
        SelectedColor = Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    private void OnColorViewChanged(object? sender, ColorChangedEventArgs e) => ApplyColorViewColor(e.NewColor);

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        SelectedColor = _colorAtOpen;
        PickerButton.Flyout?.Hide();
    }

    private void OnFlyoutKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SelectedColor = _colorAtOpen;
            PickerButton.Flyout?.Hide();
            e.Handled = true;
        }
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
        TryApplyHex(HexInput.Text);
        PickerButton.Flyout?.Hide();
    }
}
