using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace Trizbort.Avalonia.Controls;

/// <summary>
///   A small ⓘ beside an option: hover over it (or focus it, for the keyboard) for more about the option than its
///   label says. Lee, 24 Nov 2026.
/// </summary>
public class InfoTip : TemplatedControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<InfoTip, string?>(nameof(Text));

    static InfoTip()
    {
        FocusableProperty.OverrideDefaultValue<InfoTip>(true);
        TextProperty.Changed.AddClassHandler<InfoTip>((tip, _) => tip.TextChanged());
    }

    public InfoTip()
    {
        Width = 16;
        Height = 16;
        Margin = new Thickness(6, 0, 0, 0);
        VerticalAlignment = VerticalAlignment.Center;
        Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Help);
        ToolTip.SetShowDelay(this, 150);
        ToolTip.SetBetweenShowDelay(this, 0);

        var letter = new TextBlock
        {
            Text = "i",
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            FontStyle = FontStyle.Normal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, -1, 0, 0)
        };
        letter[!TextBlock.ForegroundProperty] = this[!ForegroundProperty];
        Template = new global::Avalonia.Controls.Templates.FuncControlTemplate<InfoTip>((tip, _) => new Border
        {
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1.2),
            [!Border.BorderBrushProperty] = tip[!ForegroundProperty],
            Background = Brushes.Transparent,
            Child = letter
        });
        Foreground = new SolidColorBrush(Color.FromRgb(0x5B, 0x7F, 0xB8));
    }

    /// <summary>What's shown on hover.</summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private void TextChanged()
    {
        // wrapped, as a paragraph, rather than one long line
        ToolTip.SetTip(this, new TextBlock { Text = Text, TextWrapping = TextWrapping.Wrap, MaxWidth = 360 });
        AutomationProperties.SetHelpText(this, Text);
        AutomationProperties.SetName(this, "More information");
    }

    protected override void OnGotFocus(global::Avalonia.Input.GotFocusEventArgs e)
    {
        base.OnGotFocus(e);
        ToolTip.SetIsOpen(this, true);
    }

    protected override void OnLostFocus(global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        ToolTip.SetIsOpen(this, false);
    }
}
