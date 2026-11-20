using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Trizbort.Avalonia.Drawing;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using CoreColors = Trizbort.Domain.Misc.Colors;
using DrawingContext = Avalonia.Media.DrawingContext;

namespace Trizbort.Avalonia.Controls;

/// <summary>
///   A sample room with a join out of every side, labelled the way the Map Settings dialog's join label options
///   would draw them: which exits are labelled, and short or full direction words. Redraws whenever the bound
///   <see cref="MapSettingsViewModel" /> changes.
/// </summary>
public class JoinLabelsPreview : Control
{
    private MapSettingsViewModel? _viewModel;

    /// <summary>The joins drawn: the side each leaves from, and the label written on it ("" for none).</summary>
    internal static readonly (MappableDirection Side, string Written)[] Sample =
    {
        (MappableDirection.North, ""),
        (MappableDirection.NorthEast, ""),
        (MappableDirection.East, ""),
        (MappableDirection.SouthEast, ""),
        (MappableDirection.South, ""),
        // stairs: a corner join written down ("d") by hand, and one automap wrote as "west / up"
        (MappableDirection.SouthWest, "d"),
        (MappableDirection.West, "west / up"),
        (MappableDirection.NorthWest, "")
    };

    public JoinLabelsPreview()
    {
        Width = 260;
        Height = 150;
        DataContextChanged += (_, _) => Attach(DataContext as MapSettingsViewModel);
    }

    private void Attach(MapSettingsViewModel? viewModel)
    {
        if (_viewModel != null) _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = viewModel;
        if (_viewModel != null) _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        InvalidateVisual();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e) => InvalidateVisual();

    /// <summary>The label drawn on a sample join with these settings; empty for none.</summary>
    internal static string LabelFor(MappableDirection side, string written, MapSettingsViewModel vm)
    {
        if (written.Length > 0)
            return ConnectionEndLabels.Shaped(written, vm.LabelDirectionWords, vm.LabelUpDownExits) ?? string.Empty;
        var labelled = DirectionWords.IsCompass(side) && (side is MappableDirection.North or MappableDirection.East
          or MappableDirection.South or MappableDirection.West
          ? vm.LabelCompassExits
          : vm.LabelDiagonalExits);
        return labelled ? DirectionWords.Styled(side, vm.LabelDirectionWords == DirectionWordStyle.Full) : string.Empty;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_viewModel == null) return;

        var width = Math.Max(1, Bounds.Width);
        var height = Math.Max(1, Bounds.Height);
        context.FillRectangle(Settings.Color[CoreColors.Canvas].ToBrush(), new Rect(0, 0, width, height));

        var centre = new Point(width / 2, height / 2);
        var room = new Rect(centre.X - 36, centre.Y - 20, 72, 40);
        var linePen = Settings.Color[CoreColors.Line].ToPen(1.5);
        var textBrush = Settings.Color[CoreColors.LineText].ToBrush();
        var typeface = new Typeface(FontFamily.Default);
        const double stub = 34;

        foreach (var (side, written) in Sample)
        {
            var (dx, dy) = Offset(side);
            var from = new Point(centre.X + dx * room.Width / 2, centre.Y + dy * room.Height / 2);
            var length = Math.Sqrt(dx * dx + dy * dy);
            var to = new Point(from.X + dx / length * stub, from.Y + dy / length * stub);
            context.DrawLine(linePen, from, to);

            var label = LabelFor(side, written, _viewModel);
            if (label.Length == 0) continue;
            var text = new FormattedText(label, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 11,
              textBrush);
            // beside the line, near the room: to the right of a line going up or down, otherwise above it
            var middle = new Point((from.X + to.X) / 2, (from.Y + to.Y) / 2);
            var at = dx == 0
              ? new Point(middle.X + 4, middle.Y - text.Height / 2)
              : new Point(middle.X - (dx < 0 ? text.Width : 0), middle.Y - text.Height - 2);
            context.DrawText(text, at);
        }

        var fill = Settings.Regions.FirstOrDefault()?.RColor ?? System.Drawing.Color.White;
        context.DrawRectangle(fill.ToBrush(), Settings.Color[CoreColors.Border].ToPen(2), room);
        var name = new FormattedText("Hall", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 12,
          Settings.Color[CoreColors.Border].ToBrush());
        context.DrawText(name, new Point(centre.X - name.Width / 2, centre.Y - name.Height / 2));
    }

    private static (double X, double Y) Offset(MappableDirection side) => side switch
    {
        MappableDirection.North => (0, -1),
        MappableDirection.NorthEast => (1, -1),
        MappableDirection.East => (1, 0),
        MappableDirection.SouthEast => (1, 1),
        MappableDirection.South => (0, 1),
        MappableDirection.SouthWest => (-1, 1),
        MappableDirection.West => (-1, 0),
        _ => (-1, -1)
    };
}
