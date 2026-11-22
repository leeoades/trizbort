using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Trizbort.Avalonia.Drawing;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using CoreColors = Trizbort.Domain.Misc.Colors;
using DrawingContext = Avalonia.Media.DrawingContext;
using Point = Avalonia.Point;
using Rect = Avalonia.Rect;

namespace Trizbort.Avalonia.Controls;

/// <summary>
///     One of the two rooms in Split Room, drawn as the map draws it: the room, and its ways in and out as lines from
///     its sides, each with an arrow the way it goes, the way labelled at the end it leaves from ("west"), and the room
///     at the other end. Clicking a line moves the way to the other room. Lee, 22 Nov 2026: "a depiction of the rooms
///     with the exits", not words to match up with the map.
/// </summary>
public sealed class SplitRoomDiagram : Control
{
    public static readonly StyledProperty<bool> ShowsNewProperty =
        AvaloniaProperty.Register<SplitRoomDiagram, bool>(nameof(ShowsNew));

    private const double RoomWidth = 150;
    private const double RoomHeight = 70;
    private const double LineLength = 70;
    private const double Spacing = 12;

    private const double LoopWidth = 44;

    /// <summary>Each way drawn: the points along its line, the way, and the room named at its far end.</summary>
    private readonly List<(IReadOnlyList<Point> Points, SplitRoomJoin Join, string Other)> _lines = new();
    private SplitRoomViewModel? _viewModel;
    private SplitRoomJoin? _hover;

    public SplitRoomDiagram()
    {
        Width = 480;
        Height = 300;
        Cursor = new Cursor(StandardCursorType.Arrow);
        DataContextChanged += (_, _) => Attach(DataContext as SplitRoomViewModel);
    }

    /// <summary>The new room, with the ways moved to it; else the original, with the ways that stay.</summary>
    public bool ShowsNew
    {
        get => GetValue(ShowsNewProperty);
        set => SetValue(ShowsNewProperty, value);
    }

    private void Attach(SplitRoomViewModel? viewModel)
    {
        if (_viewModel != null)
            foreach (var join in _viewModel.Joins) join.PropertyChanged -= OnJoinChanged;
        _viewModel = viewModel;
        if (_viewModel != null)
            foreach (var join in _viewModel.Joins) join.PropertyChanged += OnJoinChanged;
        InvalidateVisual();
    }

    private void OnJoinChanged(object? sender, PropertyChangedEventArgs e) => InvalidateVisual();

    /// <summary>The ways drawn on this room: those that stay, or those moved here.</summary>
    internal IReadOnlyList<SplitRoomJoin> Shown =>
        _viewModel?.Joins.Where(join => join.Moves == ShowsNew || (join.Exit.Loop && join.Moves)).ToList()
        ?? new List<SplitRoomJoin>();

    /// <summary>
    ///   A loop that stays is drawn as a loop; moved, it leaves the original into the new room, one way, so it is
    ///   drawn on both: out of the original, and into the new one.
    /// </summary>
    private bool DrawnAsLoop(SplitRoomJoin join) => join.Exit.Loop && !join.Moves;

    private bool GoesIn(SplitRoomJoin join) => join.Exit.Loop ? join.Moves && ShowsNew : join.Exit.Inward;

    private string OtherOf(SplitRoomJoin join) =>
        !join.Exit.Loop ? join.Exit.Other : join.Moves ? ShowsNew ? "the original" : "the new one" : string.Empty;

    /// <summary>The way whose line is at <paramref name="point" />, or null.</summary>
    internal SplitRoomJoin? JoinAt(Point point)
    {
        SplitRoomJoin? nearest = null;
        var best = 8.0;
        foreach (var (points, join, _) in _lines)
        {
            var distance = double.MaxValue;
            for (var i = 1; i < points.Count; i++) distance = Math.Min(distance, Distance(point, points[i - 1], points[i]));
            if (distance < best)
            {
                best = distance;
                nearest = join;
            }
        }

        return nearest;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var over = JoinAt(e.GetPosition(this));
        if (over == _hover) return;
        _hover = over;
        Cursor = new Cursor(over != null ? StandardCursorType.Hand : StandardCursorType.Arrow);
        ToolTip.SetTip(this, over == null ? null : over.Text + (ShowsNew ? ": click to keep it on the original" : ": click to move it to the new room"));
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hover = null;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (JoinAt(e.GetPosition(this)) is not { } join) return;
        join.ToggleCommand.Execute(null);
        e.Handled = true;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        _lines.Clear();
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(Settings.Color[CoreColors.Canvas].ToBrush(), bounds);
        if (_viewModel == null) return;

        var centre = bounds.Center;
        var room = new Rect(centre.X - RoomWidth / 2, centre.Y - RoomHeight / 2, RoomWidth, RoomHeight);
        var linePen = Settings.Color[CoreColors.Line].ToPen(1.5);
        var hoverPen = new Pen(new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3)), 3);
        var lineBrush = Settings.Color[CoreColors.Line].ToBrush();
        var textBrush = Settings.Color[CoreColors.LineText].ToBrush();
        var typeface = new Typeface(FontFamily.Default);

        foreach (var side in Shown.GroupBy(join => Cell(join.Side)))
        {
            var ways = side.ToList();
            var (dx, dy) = Direction(side.Key);
            var length = Math.Sqrt(dx * dx + dy * dy);
            var (ux, uy) = (dx / length, dy / length);
            // side by side across the line's direction
            var (px, py) = (-uy, ux);
            var anchor = Edge(room, side.Key);
            // side by side: a line takes a little room, a loop more
            var widths = ways.Select(join => DrawnAsLoop(join) ? LoopWidth : Spacing).ToList();
            var place = -widths.Sum() / 2;
            for (var i = 0; i < ways.Count; i++)
            {
                var join = ways[i];
                var offset = place + widths[i] / 2;
                place += widths[i];
                var from = new Point(anchor.X + px * offset, anchor.Y + py * offset);
                var pen = join == _hover ? hoverPen : linePen;
                var way = join.Exit.Way;
                var wayText = string.IsNullOrWhiteSpace(way) ? null : Text(way, typeface, 10, textBrush, FontStyle.Normal);

                if (DrawnAsLoop(join))
                {
                    // out of the side, round, and back in at the same side: the way leads back here
                    var half = LoopWidth / 2 - 6;
                    var start = new Point(from.X - px * half, from.Y - py * half);
                    var end = new Point(from.X + px * half, from.Y + py * half);
                    var c1 = new Point(start.X + ux * LineLength * 1.1 - px * 10, start.Y + uy * LineLength * 1.1 - py * 10);
                    var c2 = new Point(end.X + ux * LineLength * 1.1 + px * 10, end.Y + uy * LineLength * 1.1 + py * 10);
                    var points = Enumerable.Range(0, 25).Select(n => Bezier(start, c1, c2, end, n / 24.0)).ToList();
                    _lines.Add((points, join, string.Empty));
                    var curve = new StreamGeometry();
                    using (var g = curve.Open())
                    {
                        g.BeginFigure(start, false);
                        g.CubicBezierTo(c1, c2, end);
                        g.EndFigure(false);
                    }

                    context.DrawGeometry(null, pen, curve);
                    // the arrow at the top of the loop, going round from the way out to the way back
                    Arrow(context, lineBrush, points[11], points[13], 0.5);
                    Arrow(context, lineBrush, points[22], points[24], 0.5);
                    if (wayText != null)
                    {
                        var at = points[4];
                        context.DrawText(wayText, Beside(at, px, py, wayText, -1));
                    }

                    continue;
                }

                var to = new Point(from.X + ux * LineLength, from.Y + uy * LineLength);
                _lines.Add((new[] { from, to }, join, OtherOf(join)));
                context.DrawLine(pen, from, to);
                // the arrow the way it goes: out of the room, or in
                var goesIn = GoesIn(join);
                if (goesIn) Arrow(context, lineBrush, to, from);
                else Arrow(context, lineBrush, from, to);

                // the way at the end it leaves from, and the room at the other end, as the map labels them
                if (wayText != null)
                {
                    var at = goesIn ? 0.8 : 0.25;
                    var mid = new Point(from.X + (to.X - from.X) * at, from.Y + (to.Y - from.Y) * at);
                    context.DrawText(wayText, Beside(mid, px, py, wayText, offset));
                }
            }

            // the room at the other end, once for the ways to the same room (a way there and its way back)
            var ends = _lines.Where(line => ways.Contains(line.Join) && line.Points.Count == 2).ToList();
            foreach (var room2 in ends.GroupBy(line => line.Other))
            {
                if (string.IsNullOrWhiteSpace(room2.Key)) continue;
                var at = new Point(room2.Average(line => line.Points[1].X), room2.Average(line => line.Points[1].Y));
                var other = Text(room2.Key, typeface, 11, textBrush, FontStyle.Italic);
                context.DrawText(other, End(at, ux, uy, other));
            }
        }

        var fill = Settings.Regions.FirstOrDefault()?.RColor ?? System.Drawing.Color.White;
        context.DrawRectangle(fill.ToBrush(), Settings.Color[CoreColors.Border].ToPen(2), room);
        // centred within the room's width
        var name = Text(ShowsNew ? _viewModel.NewName : _viewModel.RoomName, typeface, 13,
            Settings.Color[CoreColors.Border].ToBrush(), FontStyle.Normal, RoomWidth - 8);
        context.DrawText(name, new Point(room.Left + 4, room.Top + 8));
        // the objects mark the original
        if (!ShowsNew && _viewModel.HasObjects)
        {
            var objects = Text(_viewModel.Objects, typeface, 11, Settings.Color[CoreColors.SmallText].ToBrush(),
                FontStyle.Italic, RoomWidth - 8);
            context.DrawText(objects, new Point(room.Left + 4, room.Bottom - objects.Height - 6));
        }
    }

    private static FormattedText Text(string text, Typeface typeface, double size, IBrush brush, FontStyle style,
        double? maxWidth = null)
    {
        var formatted = new FormattedText(text ?? string.Empty, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(typeface.FontFamily, style), size, brush);
        // a label is measured as written, so it can be placed beside its line; the room's text is centred in the room
        if (maxWidth is { } width)
        {
            formatted.MaxTextWidth = width;
            formatted.TextAlignment = TextAlignment.Center;
            formatted.MaxLineCount = 2;
            formatted.Trimming = TextTrimming.CharacterEllipsis;
        }

        return formatted;
    }

    /// <summary>Beside the line, on the side away from the other lines at that side of the room.</summary>
    private static Point Beside(Point at, double px, double py, FormattedText text, double place)
    {
        var sign = place < 0 ? -1 : 1;
        var x = at.X + px * sign * (text.Width / 2 + 4) - text.Width / 2;
        var y = at.Y + py * sign * (text.Height / 2 + 2) - text.Height / 2;
        return new Point(x, y);
    }

    /// <summary>Past the end of the line, centred on it.</summary>
    private static Point End(Point to, double ux, double uy, FormattedText text) =>
        new(to.X + ux * (text.Width / 2 + 4) - text.Width / 2, to.Y + uy * (text.Height / 2 + 4) - text.Height / 2);

    private static Point Bezier(Point a, Point b, Point c, Point d, double t)
    {
        var u = 1 - t;
        return new Point(
            u * u * u * a.X + 3 * u * u * t * b.X + 3 * u * t * t * c.X + t * t * t * d.X,
            u * u * u * a.Y + 3 * u * u * t * b.Y + 3 * u * t * t * c.Y + t * t * t * d.Y);
    }

    /// <summary>An arrowhead along from <paramref name="from" /> to <paramref name="to" />, <paramref name="along" /> the way.</summary>
    private static void Arrow(DrawingContext context, IBrush brush, Point from, Point to, double along = 0.6)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1) return;
        var (ux, uy) = (dx / length, dy / length);
        // half way along, pointing the way it goes
        var tip = new Point(from.X + dx * along, from.Y + dy * along);
        var back = new Point(tip.X - ux * 9, tip.Y - uy * 9);
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(tip, true);
            g.LineTo(new Point(back.X - uy * 4.5, back.Y + ux * 4.5));
            g.LineTo(new Point(back.X + uy * 4.5, back.Y - ux * 4.5));
            g.EndFigure(true);
        }

        context.DrawGeometry(brush, null, geometry);
    }

    private static double Distance(Point p, Point a, Point b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var lengthSquared = dx * dx + dy * dy;
        var t = lengthSquared == 0 ? 0 : Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSquared, 0, 1);
        var x = a.X + t * dx - p.X;
        var y = a.Y + t * dy - p.Y;
        return Math.Sqrt(x * x + y * y);
    }

    private static (int Row, int Column) Cell(CompassPoint side) => SplitRoomViewModel.Cell(side);

    private static (double X, double Y) Direction((int Row, int Column) cell) => (cell.Column - 1, cell.Row - 1);

    /// <summary>Where a side's lines leave the room: the middle of an edge, or a corner.</summary>
    private static Point Edge(Rect room, (int Row, int Column) cell) =>
        new(room.Left + room.Width * cell.Column / 2.0, room.Top + room.Height * cell.Row / 2.0);
}
