using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Trizbort.Avalonia.Drawing;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Setup;
using CoreRect = Trizbort.Domain.Misc.Rect;

namespace Trizbort.Avalonia.Controls;

/// <summary>
///   A sample label drawn in a style's look, as the map draws labels (shape, outline, background, font, colour), on
///   the map's canvas colour and grid. Lee, 24 Nov 2026.
/// </summary>
public sealed class LabelStylePreview : Control
{
    public static readonly StyledProperty<ResolvedLabelStyle?> LookProperty =
        AvaloniaProperty.Register<LabelStylePreview, ResolvedLabelStyle?>(nameof(Look));

    public static readonly StyledProperty<string> SampleProperty =
        AvaloniaProperty.Register<LabelStylePreview, string>(nameof(Sample), "The Lighthouse\nA label in this style");

    static LabelStylePreview()
    {
        AffectsRender<LabelStylePreview>(LookProperty, SampleProperty);
    }

    public ResolvedLabelStyle? Look
    {
        get => GetValue(LookProperty);
        set => SetValue(LookProperty, value);
    }

    public string Sample
    {
        get => GetValue(SampleProperty);
        set => SetValue(SampleProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        context.DrawRectangle(Settings.Color[Domain.Misc.Colors.Canvas].ToBrush(), null, new Rect(Bounds.Size));
        // the map's grid behind, so a label without a background is plainly see-through (Lee, 25 Nov 2026)
        var step = Math.Max(8, (double)Settings.GridSize);
        var gridPen = new Pen(Settings.Color[Domain.Misc.Colors.Grid].ToBrush(), 1);
        for (var x = step / 2; x < Bounds.Width; x += step)
            context.DrawLine(gridPen, new Point(Math.Round(x) + 0.5, 0), new Point(Math.Round(x) + 0.5, Bounds.Height));
        for (var y = step / 2; y < Bounds.Height; y += step)
            context.DrawLine(gridPen, new Point(0, Math.Round(y) + 0.5), new Point(Bounds.Width, Math.Round(y) + 0.5));
        if (Look is not { } look) return;

        var bounds = new CoreRect(16, 12, (float)Math.Max(1, Bounds.Width - 32), (float)Math.Max(1, Bounds.Height - 24));
        var geometry = look.Shape switch
        {
            RoomShape.RoundedCorners => DrawingAdapters.CreateRoundedRectangleGeometry(bounds, new CornerRadii(), Settings.HandDrawn, 7),
            RoomShape.Ellipse => DrawingAdapters.CreateEllipseGeometry(bounds, Settings.HandDrawn, 7),
            RoomShape.Octagonal => DrawingAdapters.CreateOctagonalGeometry(bounds, new CornerRadii(), Settings.HandDrawn, 7),
            _ => DrawingAdapters.CreateSquareGeometry(bounds, Settings.HandDrawn, 7)
        };
        var fill = look.HasBackground ? look.BackgroundColor.ToBrush() : null;
        var pen = look.BorderStyle == BorderDashStyle.None
            ? null
            : look.BorderColor.ToPen(Settings.LineWidth, look.BorderStyle.ToDashStyle());
        if (fill != null || pen != null) context.DrawGeometry(fill, pen, geometry);

        var font = new ThemeFont { Name = look.FontName, Size = look.FontSize, Style = look.FontStyle };
        var text = new FormattedText(Sample, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, font.ToTypeface(),
            look.FontSize, look.TextColor.ToBrush())
        {
            TextAlignment = TextAlignment.Center,
            MaxTextWidth = Math.Max(1, bounds.Width - 10)
        };
        context.DrawText(text, new Point(bounds.Left + 5, bounds.Top + Math.Max(0, (bounds.Height - text.Height) / 2)));
    }
}
