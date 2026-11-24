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

        // drawn rather than a font's "i" or ⓘ, so the i sits in the middle of the circle whatever the font: a
        // dot and a stem, centred on the circle's centre (8, 8)
        Template = new global::Avalonia.Controls.Templates.FuncControlTemplate<InfoTip>((tip, _) => new Panel
        {
            Children =
            {
                new global::Avalonia.Controls.Shapes.Ellipse
                {
                    Width = 16,
                    Height = 16,
                    StrokeThickness = 1.2,
                    Fill = Brushes.Transparent,
                    [!global::Avalonia.Controls.Shapes.Shape.StrokeProperty] = tip[!ForegroundProperty]
                },
                new global::Avalonia.Controls.Shapes.Path
                {
                    Width = 16,
                    Height = 16,
                    Data = Glyph,
                    [!global::Avalonia.Controls.Shapes.Shape.FillProperty] = tip[!ForegroundProperty]
                }
            }
        });
        Foreground = new SolidColorBrush(Color.FromRgb(0x5B, 0x7F, 0xB8));
    }

    /// <summary>The i: a dot (centre 8, 4.85) and a stem (7.1..8.9 by 6.85..12.25): it spans 3.75..12.25, centred
    ///   on 8.</summary>
    internal static readonly Geometry Glyph = Geometry.Parse(
        "M 9.1,4.85 A 1.1,1.1 0 1 1 6.9,4.85 A 1.1,1.1 0 1 1 9.1,4.85 Z M 7.1,6.85 L 8.9,6.85 L 8.9,12.25 L 7.1,12.25 Z");

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

    // tabbed to: shown, as for the mouse; only where it can be (not in a window with no room for a popup)
    protected override void OnGotFocus(global::Avalonia.Input.GotFocusEventArgs e)
    {
        base.OnGotFocus(e);
        Show(true);
    }

    protected override void OnLostFocus(global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        Show(false);
    }

    private void Show(bool open)
    {
        try
        {
            ToolTip.SetIsOpen(this, open);
        }
        catch (InvalidOperationException)
        {
            // no popup host (a headless window): the text is still there on hover, and for screen readers
        }
    }
}
