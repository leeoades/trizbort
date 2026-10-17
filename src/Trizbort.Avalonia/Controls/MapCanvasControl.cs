using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Trizbort.Avalonia.Drawing;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using CoreRect = Trizbort.Domain.Misc.Rect;
using CoreVector = Trizbort.Domain.Misc.Vector;
using CoreColor = System.Drawing.Color;
using CoreColors = Trizbort.Domain.Misc.Colors;
using CoreLineSegment = Trizbort.Domain.Misc.LineSegment;
using AvaloniaRect = Avalonia.Rect;
using AvaloniaColor = Avalonia.Media.Color;
using AvaloniaDrawingContext = Avalonia.Media.DrawingContext;

namespace Trizbort.Avalonia.Controls;

public class MapCanvasControl : Control
{
    public static readonly StyledProperty<ICommandService?> CommandServiceProperty =
        AvaloniaProperty.Register<MapCanvasControl, ICommandService?>(nameof(CommandService));

    public ICommandService? CommandService
    {
        get => GetValue(CommandServiceProperty);
        set => SetValue(CommandServiceProperty, value);
    }

    private CoreVector _origin = CoreVector.Zero;
    private float _zoomFactor = 1.0f;
    private readonly CanvasTransform _transform = new();

    public CoreVector Origin
    {
        get => _origin;
        set
        {
            if (_origin != value)
            {
                _origin = value;
                _transform.Origin = value;
                InvalidateVisual();
            }
        }
    }

    public float ZoomFactor
    {
        get => _zoomFactor;
        set
        {
            var clamped = Math.Clamp(value, 0.1f, 10.0f);
            if (Math.Abs(_zoomFactor - clamped) > 0.0001f)
            {
                _zoomFactor = clamped;
                _transform.ZoomFactor = clamped;
                InvalidateVisual();
            }
        }
    }

    public MapCanvasControl()
    {
        ClipToBounds = true;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Project.ProjectChanged += OnProjectChanged;
        Settings.Changed += OnSettingsChanged;

        if (CommandService != null)
        {
            HookCommandService(CommandService);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Project.ProjectChanged -= OnProjectChanged;
        Settings.Changed -= OnSettingsChanged;

        if (CommandService != null)
        {
            UnhookCommandService(CommandService);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CommandServiceProperty)
        {
            if (change.OldValue is ICommandService oldService)
            {
                UnhookCommandService(oldService);
            }
            if (change.NewValue is ICommandService newService)
            {
                HookCommandService(newService);
                if (Bounds.Width > 0 && Bounds.Height > 0)
                {
                    newService.SetViewportSize(Bounds.Width, Bounds.Height);
                }
                Origin = newService.Origin;
                ZoomFactor = newService.ZoomFactor;
            }
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        _transform.ViewportWidth = e.NewSize.Width;
        _transform.ViewportHeight = e.NewSize.Height;
        if (CommandService != null && e.NewSize.Width > 0 && e.NewSize.Height > 0)
        {
            CommandService.SetViewportSize(e.NewSize.Width, e.NewSize.Height);
        }
    }

    private void HookCommandService(ICommandService service)
    {
        service.CanvasInvalidated += OnCanvasInvalidated;
        service.ZoomChanged += OnServiceZoomChanged;
        service.SelectionChanged += OnCanvasInvalidated;
    }

    private void UnhookCommandService(ICommandService service)
    {
        service.CanvasInvalidated -= OnCanvasInvalidated;
        service.ZoomChanged -= OnServiceZoomChanged;
        service.SelectionChanged -= OnCanvasInvalidated;
    }

    private void OnProjectChanged(object? sender, ProjectChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    private void OnCanvasInvalidated(object? sender, EventArgs e)
    {
        if (CommandService != null)
        {
            _origin = CommandService.Origin;
            _zoomFactor = CommandService.ZoomFactor;
            _transform.Origin = _origin;
            _transform.ZoomFactor = _zoomFactor;
        }
        InvalidateVisual();
    }

    private void OnServiceZoomChanged(object? sender, float zoom)
    {
        _zoomFactor = zoom;
        _transform.ZoomFactor = zoom;
        InvalidateVisual();
    }

    #region Coordinate Transformations

    public Point CanvasToClient(CoreVector world)
    {
        _transform.ViewportWidth = Bounds.Width;
        _transform.ViewportHeight = Bounds.Height;
        _transform.Origin = Origin;
        _transform.ZoomFactor = ZoomFactor;
        var p = _transform.CanvasToClient(world);
        return new Point(p.X, p.Y);
    }

    public CoreVector ClientToCanvas(Point screen)
    {
        _transform.ViewportWidth = Bounds.Width;
        _transform.ViewportHeight = Bounds.Height;
        _transform.Origin = Origin;
        _transform.ZoomFactor = ZoomFactor;
        return _transform.ClientToCanvas(new CoreVector((float)screen.X, (float)screen.Y));
    }

    public CoreRect GetVisibleWorldBounds()
    {
        _transform.ViewportWidth = Bounds.Width;
        _transform.ViewportHeight = Bounds.Height;
        _transform.Origin = Origin;
        _transform.ZoomFactor = ZoomFactor;
        return _transform.GetVisibleWorldBounds();
    }

    public void ZoomAt(Point cursorScreen, float newZoom)
    {
        _transform.ViewportWidth = Bounds.Width;
        _transform.ViewportHeight = Bounds.Height;
        _transform.Origin = Origin;
        _transform.ZoomFactor = ZoomFactor;
        _transform.ZoomAt(new CoreVector((float)cursorScreen.X, (float)cursorScreen.Y), newZoom);

        if (CommandService != null)
        {
            CommandService.SetView(_transform.Origin, _transform.ZoomFactor);
        }
        else
        {
            _origin = _transform.Origin;
            _zoomFactor = _transform.ZoomFactor;
            InvalidateVisual();
        }
    }

    public void FitToMap()
    {
        if (CommandService != null)
        {
            CommandService.FitToMap();
        }
        else
        {
            _transform.ViewportWidth = Bounds.Width;
            _transform.ViewportHeight = Bounds.Height;
            _transform.FitToMap(Project.Current.Elements);
            _origin = _transform.Origin;
            _zoomFactor = _transform.ZoomFactor;
            InvalidateVisual();
        }
    }

    #endregion

    #region Pointer & Interaction

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var delta = e.Delta.Y;
        if (Math.Abs(delta) > 0.001)
        {
            var factor = delta > 0 ? 1.15f : 1.0f / 1.15f;
            var currentPos = e.GetPosition(this);
            ZoomAt(currentPos, ZoomFactor * factor);
            e.Handled = true;
        }
    }

    #endregion

    #region Rendering Pipeline

    public override void Render(AvaloniaDrawingContext context)
    {
        base.Render(context);

        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;

        // 1. Draw Canvas Background
        context.DrawRectangle(DrawingAdapters.ToBrush(CoreColors.Canvas), null, new AvaloniaRect(0, 0, Bounds.Width, Bounds.Height));

        var visibleWorld = GetVisibleWorldBounds();
        var culledWorld = visibleWorld;
        culledWorld.Inflate(120f, 120f);

        // 2. Push Matrix Transform for World-to-Screen rendering
        var transformMatrix = Matrix.CreateTranslation(-Origin.X, -Origin.Y)
                              * Matrix.CreateScale(ZoomFactor, ZoomFactor)
                              * Matrix.CreateTranslation(Bounds.Width / 2.0, Bounds.Height / 2.0);

        using (context.PushTransform(transformMatrix))
        {
            // 3. Draw Grid
            if (Settings.IsGridVisible && (CommandService?.ShowGrid ?? true) && Settings.GridSize > 0)
            {
                DrawGrid(context, visibleWorld);
            }

            // 4. Viewport Culling & Element Collection
            var allElements = Project.Current.Elements;
            var selectedList = Project.Current.GetSelectedElements();
            var selectedSet = new HashSet<Element>(selectedList);
            if (selectedSet.Count == 0 && Project.Current.ActiveSelectedElement != null)
            {
                selectedSet.Add(Project.Current.ActiveSelectedElement);
            }

            var visibleElements = allElements.Where(e => e.Intersects(culledWorld)).ToList();
            visibleElements.Sort();

            // 5. Render elements in Depth / ZOrder sequence (Rooms, Labels, Connections)
            var linesDrawn = new List<CoreLineSegment>();
            foreach (var element in visibleElements)
            {
                var isSelected = selectedSet.Contains(element);
                if (element is Room room)
                {
                    room.DrawRoom(context, isSelected, false);
                }
                else if (element is MapLabel label)
                {
                    label.DrawMapLabel(context, isSelected, false);
                }
                else if (element is Connection conn)
                {
                    conn.DrawConnection(context, isSelected, false, linesDrawn);
                }
            }

            // 6. Render Selection Resize Handles
            if (selectedSet.Count == 1)
            {
                var singleSelected = selectedSet.First();
                if (singleSelected is ISizeable sizeable)
                {
                    DrawSelectionHandles(context, sizeable);
                }
            }
        }
    }

    private void DrawGrid(AvaloniaDrawingContext context, CoreRect visibleWorld)
    {
        var gridSize = Settings.GridSize;
        var minX = (float)(Math.Floor(visibleWorld.Left / gridSize) * gridSize);
        var maxX = (float)(Math.Ceiling(visibleWorld.Right / gridSize) * gridSize);
        var minY = (float)(Math.Floor(visibleWorld.Top / gridSize) * gridSize);
        var maxY = (float)(Math.Ceiling(visibleWorld.Bottom / gridSize) * gridSize);

        var gridBrush = DrawingAdapters.ToBrush(CoreColors.Grid);

        if (gridSize * ZoomFactor >= 4)
        {
            for (var x = minX; x <= maxX; x += gridSize)
            {
                for (var y = minY; y <= maxY; y += gridSize)
                {
                    context.DrawRectangle(gridBrush, null, new AvaloniaRect(x - 0.75, y - 0.75, 1.5, 1.5));
                }
            }
        }

        if (Settings.ShowOrigin)
        {
            var originPen = Settings.Color[CoreColors.Grid].ToPen(1.5 / ZoomFactor);
            context.DrawLine(originPen, new Point(-25, 0), new Point(25, 0));
            context.DrawLine(originPen, new Point(0, -25), new Point(0, 25));
        }
    }

    private static void DrawSelectionHandles(AvaloniaDrawingContext context, ISizeable sizeable)
    {
        var handleBrush = Brushes.White;
        var handlePen = new Pen(Brushes.Black, 1.0);

        CompassPoint[] points =
        [
            CompassPoint.NorthWest, CompassPoint.North, CompassPoint.NorthEast,
            CompassPoint.East, CompassPoint.SouthEast, CompassPoint.South,
            CompassPoint.SouthWest, CompassPoint.West
        ];

        foreach (var cp in points)
        {
            var handle = new ResizeHandle(cp, sizeable);
            var rect = handle.Bounds;
            context.DrawRectangle(handleBrush, handlePen, rect.ToRect());
        }
    }

    #endregion
}
