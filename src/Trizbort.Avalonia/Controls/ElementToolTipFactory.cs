using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Trizbort.Editor;

namespace Trizbort.Avalonia.Controls;

/// <summary>
///   Builds the canvas element tooltip from a <see cref="TooltipModel" />: title, body and footer are styled
///   separately on a pale yellow background for connections and light blue for everything else (as the reference).
/// </summary>
internal static class ElementToolTipFactory
{
    private const double MaxTextWidth = 420;

    internal static readonly Color ConnectionBackground = Color.FromRgb(255, 250, 205); // LemonChiffon
    internal static readonly Color RoomBackground = Color.FromRgb(173, 216, 230); // LightBlue

    internal static Color BackgroundFor(TooltipElementKind kind)
    {
        return kind == TooltipElementKind.Connection ? ConnectionBackground : RoomBackground;
    }

    internal static Control Build(TooltipModel model)
    {
        var panel = new StackPanel { Spacing = 2, MaxWidth = MaxTextWidth };
        if (model.Title.Length > 0)
        {
            panel.Children.Add(CreateText(model.Title, FontWeight.Bold, FontStyle.Normal, 0));
        }

        if (model.Body.Length > 0)
        {
            panel.Children.Add(CreateText(model.Body, FontWeight.Normal, FontStyle.Normal, 0));
        }

        if (model.Footer.Length > 0)
        {
            panel.Children.Add(CreateText(model.Footer, FontWeight.Normal, FontStyle.Italic, -1));
        }

        return new Border
        {
            Background = new SolidColorBrush(BackgroundFor(model.Kind)),
            CornerRadius = new global::Avalonia.CornerRadius(3),
            Padding = new global::Avalonia.Thickness(6, 4),
            Child = panel
        };
    }

    /// <summary>The tooltip's texts joined by new lines; for tests and automation.</summary>
    internal static string PlainText(Control? tip)
    {
        if (tip is not Border { Child: StackPanel panel })
        {
            return string.Empty;
        }

        return string.Join("\n", panel.Children.OfType<TextBlock>().Select(t => t.Text));
    }

    private static TextBlock CreateText(string text, FontWeight weight, FontStyle style, double sizeDelta)
    {
        var block = new TextBlock
        {
            Text = text,
            FontWeight = weight,
            FontStyle = style,
            Foreground = Brushes.Black,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        if (sizeDelta != 0)
        {
            block.FontSize = 12 + sizeDelta;
        }

        return block;
    }
}
