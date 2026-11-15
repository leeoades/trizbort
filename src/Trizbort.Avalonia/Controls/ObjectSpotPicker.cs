using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Trizbort.Domain.Elements;
using CompassPoint = Trizbort.Domain.Misc.CompassPoint;
using CoreRect = Trizbort.Domain.Misc.Rect;

namespace Trizbort.Avalonia.Controls;

/// <summary>
///     Map Settings' Default Room: a sample room with a sample object list, dragged (or clicked) to one of nine spots,
///     beside the room at a side or corner, or inside it. That's where object lists go unless placed by hand.
/// </summary>
public sealed class ObjectSpotPicker : Control
{
    public static readonly StyledProperty<CompassPoint> SideProperty =
        AvaloniaProperty.Register<ObjectSpotPicker, CompassPoint>(nameof(Side), CompassPoint.South,
            defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool> InsideProperty =
        AvaloniaProperty.Register<ObjectSpotPicker, bool>(nameof(Inside),
            defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    private const float RoomWidth = 96;
    private const float RoomHeight = 64;
    private const string Sample = "lamp\nskis";
    private bool _dragging;

    static ObjectSpotPicker()
    {
        AffectsRender<ObjectSpotPicker>(SideProperty, InsideProperty);
        FocusableProperty.OverrideDefaultValue<ObjectSpotPicker>(true);
    }

    public ObjectSpotPicker()
    {
        Width = 300;
        Height = 180;
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    public CompassPoint Side
    {
        get => GetValue(SideProperty);
        set => SetValue(SideProperty, value);
    }

    public bool Inside
    {
        get => GetValue(InsideProperty);
        set => SetValue(InsideProperty, value);
    }

    /// <summary>The nine spots, beside the room and inside it.</summary>
    public static IEnumerable<ObjectListSpot> Spots =>
        ObjectListPlacement.Sides.Select(side => new ObjectListSpot(side, false))
            .Append(new ObjectListSpot(CompassPoint.South, true));

    private CoreRect RoomBounds() =>
        new((float)(Bounds.Width - RoomWidth) / 2, (float)(Bounds.Height - RoomHeight) / 2, RoomWidth, RoomHeight);

    private FormattedText SampleText(IBrush brush) =>
        new(Sample, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 11, brush);

    /// <summary>The spot whose sample list is nearest <paramref name="point" />.</summary>
    public ObjectListSpot NearestSpot(Point point)
    {
        var text = SampleText(Brushes.Black);
        var room = RoomBounds();
        return Spots.OrderBy(spot =>
        {
            var at = ObjectListPlacement.Bounds(room, spot, (float)text.Width, (float)text.Height).Center;
            var dx = at.X - point.X;
            var dy = at.Y - point.Y;
            return dx * dx + dy * dy;
        }).First();
    }

    private void Pick(Point point)
    {
        var spot = NearestSpot(point);
        Inside = spot.Inside;
        Side = spot.Side;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _dragging = true;
        e.Pointer.Capture(this);
        Pick(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragging) Pick(e.GetPosition(this));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _dragging = false;
        e.Pointer.Capture(null);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var foreground = Brushes.Gray;
        var room = RoomBounds();
        var pen = new Pen(foreground, 1.5);
        var box = new Rect(room.Left, room.Top, room.Width, room.Height);
        context.DrawRectangle(Brushes.Transparent, pen, box);
        var name = new FormattedText("Default Room", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            Typeface.Default, 12, foreground);
        context.DrawText(name, new Point(room.Left + (room.Width - name.Width) / 2, room.Top + 8));

        // the other spots, faintly; the chosen one with the sample list
        var hint = new SolidColorBrush(Colors.Gray, 0.35);
        var text = SampleText(foreground);
        foreach (var spot in Spots)
        {
            var at = ObjectListPlacement.Bounds(room, spot, (float)text.Width, (float)text.Height);
            if (spot == new ObjectListSpot(Side, Inside) || (Inside && spot.Inside))
            {
                text.TextAlignment = !spot.Inside && spot.Side is CompassPoint.West or CompassPoint.NorthWest or CompassPoint.SouthWest
                    ? TextAlignment.Right
                    : TextAlignment.Left;
                text.MaxTextWidth = at.Width + 1;
                context.DrawText(text, new Point(at.Left, at.Top));
            }
            else
            {
                context.DrawEllipse(hint, null, new Point(at.Center.X, at.Center.Y), 3, 3);
            }
        }
    }
}
